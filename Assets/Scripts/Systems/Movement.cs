using Metal.Authoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using RaycastHit = Unity.Physics.RaycastHit;
namespace Metal.Systems {
    /// <summary>
    /// Executes calculations to move an entity, the entity must have a movement tag (e.g. Tags.Movement.Vehicle)
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct Movement : ISystem, ISystemStartStop {
        public enum CollisionLayer {
            all = ~0,
            ground = 1 << 0,
            vehicle = 1 << 1
        }
        private EntityQuery vehicleQuery;
        private CollisionWorld collisionWorld;
        private ComponentLookup<LocalTransform> transformLookup;
        private BufferLookup<Authoring.WheelEntity> wheelEntitiesLookup;
        private const float fixedDeltaTime = 1.0f / 60.0f;
        
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Components.Tick>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<Components.Input>();
            transformLookup = state.GetComponentLookup<LocalTransform>();
            wheelEntitiesLookup = state.GetBufferLookup<Authoring.WheelEntity>();
        }

        public void OnStartRunning(ref SystemState state) {
            
        }

        public void OnUpdate(ref SystemState state) {
            transformLookup.Update(ref state);
            wheelEntitiesLookup.Update(ref state);
            
            state.Dependency = new VehicleJob {
                physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                transformLookup = transformLookup,
                wheelEntitiesLookup = wheelEntitiesLookup,
                inputDirection = SystemAPI.GetSingleton<Components.Input>().movement,
                //deltaTime = SystemAPI.Time.DeltaTime,
                fixedDeltaTime = fixedDeltaTime,
                wheelCollisionFilter = new CollisionFilter {
                    BelongsTo = CollisionFilter.Default.BelongsTo, 
                    CollidesWith = ~(uint)CollisionLayer.vehicle,
                    GroupIndex = CollisionFilter.Default.GroupIndex,
                },
                //fixedFrameCount = SystemAPI.GetSingleton<Components.Tick>().value
            }.Schedule(state.Dependency);
        }

        public void OnStopRunning(ref SystemState state) {
            
        }
        
    }

    [BurstCompile]
    public partial struct VehicleJob : IJobEntity {
        [ReadOnly] public PhysicsWorld physicsWorld;
        [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;
        [ReadOnly] public BufferLookup<Authoring.WheelEntity> wheelEntitiesLookup;
        [ReadOnly] public float3 inputDirection;
        //[ReadOnly] public float deltaTime;
        [ReadOnly] public float fixedDeltaTime;
        [ReadOnly] public CollisionFilter wheelCollisionFilter;
        [ReadOnly] public float3 linearVelocityAccumulated;
        [ReadOnly] public float3 angularVelocityAccumulated;
        //[ReadOnly] public uint fixedFrameCount;
        
        [BurstCompile]
        public void Execute(Entity vehicleEntity,
            RefRO<Components.Vehicle> vehicle,
            RefRO<PhysicsMass> vehicleMass, 
            RefRW<PhysicsVelocity> vehicleVelocity) {
            
            // assumed vehicle is a uniformly scaled orphan object, so TransformHelpers.ComputeWorldTransformMatrix is not needed
            float4x4 vehicleTransformMatrix = transformLookup[vehicleEntity].ToMatrix();
            //float3 vehicleWorldPosition = vehicleTransformMatrix.c3.xyz;
            float3 vehicleWorldCenterOfMass = vehicleTransformMatrix.TransformPoint(vehicleMass.ValueRO.CenterOfMass);
            quaternion vehicleWorldRotation = vehicleTransformMatrix.Rotation();
            //quaternion wheelWorldRotation = vehicleWorldRotation;
            float3 vehicleWorldUp = vehicleTransformMatrix.Up();
            float3 wheelWorldUp = vehicleWorldUp;
            float3 vehicleWorldRight = vehicleTransformMatrix.Right();
            //float3 wheelWorldRight = vehicleWorldRight;
            float3 vehicleWorldForward = vehicleTransformMatrix.Forward();
            //float3 wheelWorldForward = vehicleWorldForward;
            
            #region Wheel Init & Suspension
            
            DynamicBuffer<Authoring.WheelEntity> wheelEntities = wheelEntitiesLookup[vehicleEntity];
            
            int groundedWheels = 0;

            foreach (WheelEntity wheelEntity in wheelEntities) {
                
                float3 wheelWorldPosition = vehicleTransformMatrix.TransformPoint(transformLookup[wheelEntity.value].Position);
                
                if (!physicsWorld.CastRay(new RaycastInput {
                        Start = wheelWorldPosition,
                        End = wheelWorldPosition + wheelWorldUp * -1.0f * 
                            (vehicle.ValueRO.restLength + vehicle.ValueRO.springTravel + vehicle.ValueRO.wheelRadius),
                        Filter = wheelCollisionFilter
                    }, out RaycastHit hitData)) continue;
                
                groundedWheels++;

                #region Suspension
                
                if (vehicle.ValueRO.enableSuspension) {
                    Extensions.GetPointVelocity(
                        vehicleVelocity.ValueRO.Linear,
                        vehicleVelocity.ValueRO.Angular,
                        vehicleWorldCenterOfMass,
                        wheelWorldPosition,
                        out float3 wheelWorldVelocity);
                    
                    float suspensionForce = vehicle.ValueRO.springStiffness * 
                                            ((vehicle.ValueRO.restLength - (math.distance(wheelWorldPosition, hitData.Position) - vehicle.ValueRO.wheelRadius)) / vehicle.ValueRO.springTravel)
                                            - 
                                            vehicle.ValueRO.damperStiffness * 
                                            math.dot(wheelWorldVelocity, wheelWorldUp);
                        
                    Extensions.AddForceAtPosition(
                        ref linearVelocityAccumulated,
                        ref angularVelocityAccumulated,
                        suspensionForce * wheelWorldUp,
                        wheelWorldPosition,
                        vehicleMass.ValueRO.InverseMass,
                        vehicleMass.ValueRO.InverseInertia,
                        vehicleWorldCenterOfMass);
                }
                
                #endregion
            }
            

            #endregion
            
            float3 carLocalVelocity = vehicleTransformMatrix.InverseTransformDirection(vehicleVelocity.ValueRO.Linear);
            float carVelocityRatio = math.length(carLocalVelocity) / vehicle.ValueRO.maxSpeed;

            #region Movement

            if (groundedWheels > 1) {
                #region Accel/Decel
                
                float3 accelerationPoint = vehicleTransformMatrix.TransformPoint(vehicle.ValueRO.accelerationPointOffset);
                float3 accelForce = vehicleWorldForward * inputDirection.z;
                
                if (vehicle.ValueRO.enableAcceleration) {
                    
                    Extensions.AddForceAtPosition(
                        ref linearVelocityAccumulated,
                        ref angularVelocityAccumulated,
                        accelForce * vehicle.ValueRO.accelerationForce, 
                        accelerationPoint,
                        vehicleMass.ValueRO.InverseMass,
                        vehicleMass.ValueRO.InverseInertia,
                        vehicleWorldCenterOfMass,
                        ForceMode.Acceleration);
                    
                    Extensions.AddForceAtPosition(
                        ref linearVelocityAccumulated,
                        ref angularVelocityAccumulated,
                        accelForce * -1.0f * vehicle.ValueRO.decelerationForce, 
                        accelerationPoint,
                        vehicleMass.ValueRO.InverseMass,
                        vehicleMass.ValueRO.InverseInertia,
                        vehicleWorldCenterOfMass,
                        ForceMode.Acceleration);
                }
                
                #endregion

                #region Steering

                if ((inputDirection.x >= vehicle.ValueRO.movementDeadzone || inputDirection.x <= vehicle.ValueRO.movementDeadzone * -1.0f) &&
                    vehicle.ValueRO.enableSteering) {
                    Extensions.AddRelativeTorque(
                        ref angularVelocityAccumulated,
                        (vehicle.ValueRO.steerStrength * inputDirection.x * vehicle.ValueRO.turningCurve * math.sign(carVelocityRatio)) * vehicleWorldUp,
                        vehicleMass.ValueRO.InverseInertia,
                        ForceMode.Acceleration);
                }
                #endregion
                
                #region Sideways Drag
                
                if (vehicle.ValueRO.enableDrag) { 
                    Extensions.AddForceAtPosition(
                        ref linearVelocityAccumulated,
                        ref angularVelocityAccumulated,
                        vehicleWorldRight * (carLocalVelocity.x * -1.0f * vehicle.ValueRO.dragCoefficient),
                        vehicleWorldCenterOfMass,
                        vehicleMass.ValueRO.InverseMass,
                        vehicleMass.ValueRO.InverseInertia,
                        vehicleWorldCenterOfMass,
                        ForceMode.Acceleration);
                }
                #endregion
            }
            else {
                return;
            }

            #endregion
            
            vehicleVelocity.ValueRW.SetAngularVelocityWorldSpace(vehicleMass.ValueRO, vehicleWorldRotation, vehicleVelocity.ValueRO.Angular + angularVelocityAccumulated);
            vehicleVelocity.ValueRW.Linear += linearVelocityAccumulated;
        }
    }
}