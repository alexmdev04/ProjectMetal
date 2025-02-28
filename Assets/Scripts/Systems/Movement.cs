using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Logging;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using ForceMode = UnityEngine.ForceMode;
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
        private ComponentLookup<Parent> parentLookup;
        private ComponentLookup<PostTransformMatrix> scaleLookup;
        //private ComponentLookup<PhysicsVelocity> velocityLookup;
        private BufferLookup<Authoring.WheelEntity> wheelEntitiesLookup;
        
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Components.Tick>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<Components.Input>();
            transformLookup = state.GetComponentLookup<LocalTransform>();
            parentLookup = state.GetComponentLookup<Parent>();
            scaleLookup = state.GetComponentLookup<PostTransformMatrix>();
            wheelEntitiesLookup = state.GetBufferLookup<Authoring.WheelEntity>();
        }

        public void OnStartRunning(ref SystemState state) {
            
        }

        public void OnUpdate(ref SystemState state) {
            transformLookup.Update(ref state);
            parentLookup.Update(ref state);
            scaleLookup.Update(ref state);
            wheelEntitiesLookup.Update(ref state);
            
            state.Dependency = new VehicleJob {
                physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                transformLookup = transformLookup,
                wheelEntitiesLookup = wheelEntitiesLookup,
                parentLookup = parentLookup,
                scaleLookup = scaleLookup,
                inputDirection = SystemAPI.GetSingleton<Components.Input>().movement,
                deltaTime = SystemAPI.Time.DeltaTime,
                fixedDeltaTime = SystemAPI.Time.fixedDeltaTime,
                wheelCollisionFilter = new CollisionFilter {
                    BelongsTo = CollisionFilter.Default.BelongsTo, 
                    CollidesWith = ~(uint)CollisionLayer.vehicle,
                    GroupIndex = CollisionFilter.Default.GroupIndex,
                },
                fixedFrameCount = SystemAPI.GetSingleton<Components.Tick>().value
            }.Schedule(state.Dependency);
        }

        public void OnStopRunning(ref SystemState state) {
            
        }
        
    }

    [BurstCompile]
    public partial struct VehicleJob : IJobEntity {
        public PhysicsWorld physicsWorld;
        [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;
        [ReadOnly] public ComponentLookup<Parent> parentLookup;
        [ReadOnly] public ComponentLookup<PostTransformMatrix> scaleLookup;
        [ReadOnly] public BufferLookup<Authoring.WheelEntity> wheelEntitiesLookup;
        [ReadOnly] public float3 inputDirection;
        [ReadOnly] public float deltaTime;
        [ReadOnly] public float fixedDeltaTime;
        [ReadOnly] public CollisionFilter wheelCollisionFilter;
        [ReadOnly] public bool enableForceLinear;
        [ReadOnly] public bool enableForceAngular;
        [ReadOnly] public float3 linearVelocity;
        [ReadOnly] public float3 angularVelocity;
        [ReadOnly] public float3 vehicleWorldPosition;
        [ReadOnly] public float3 vehicleCenterOfMass;
        [ReadOnly] public float vehicleInverseMass;
        [ReadOnly] public float3 vehicleInverseInertia;
        [ReadOnly] public uint fixedFrameCount;
        
        [BurstCompile]
        public void Execute(
            Entity vehicleEntity, 
            RefRO<Components.Vehicle> vehicle,
            RefRO<LocalTransform> vehicleTransform, 
            RefRO<PhysicsMass> vehicleMass, 
            RefRW<PhysicsVelocity> vehicleVelocity) {
            
            enableForceLinear = vehicle.ValueRO.enableForceLinear;
            enableForceAngular = vehicle.ValueRO.enableForceAngular;
            vehicleInverseMass = vehicleMass.ValueRO.InverseMass;
            vehicleInverseInertia = vehicleMass.ValueRO.InverseInertia;
            
            // assumed vehicle is a uniformly scaled orphan object, so TransformHelpers.ComputeWorldTransformMatrix is not needed
            float4x4 vehicleTransformMatrix = transformLookup[vehicleEntity].ToMatrix();
            vehicleWorldPosition = vehicleTransformMatrix.c3.xyz;
            vehicleCenterOfMass = vehicleTransformMatrix.TransformPoint(vehicleMass.ValueRO.CenterOfMass);
            quaternion vehicleWorldRotation = vehicleTransformMatrix.Rotation();
            quaternion wheelWorldRotation = vehicleWorldRotation;
            float3 vehicleWorldUp = vehicleTransformMatrix.Up();
            float3 wheelWorldUp = vehicleWorldUp;
            float3 vehicleWorldRight = vehicleTransformMatrix.Right();
            float3 wheelWorldRight = vehicleWorldRight;
            float3 vehicleWorldForward = vehicleTransformMatrix.Forward();
            float3 wheelWorldForward = vehicleWorldForward;
            DynamicBuffer<Authoring.WheelEntity> wheelEntities = wheelEntitiesLookup[vehicleEntity];
            int groundedWheels = 0;
            
            foreach (Authoring.WheelEntity wheelEntity in wheelEntities) {
                
                float3 wheelWorldPosition = vehicleTransformMatrix.TransformPoint(transformLookup[wheelEntity.value].Position);
                
                RaycastInput ray = new RaycastInput() {
                    Start = wheelWorldPosition,
                    End = wheelWorldPosition + wheelWorldUp * -1.0f * 
                        (vehicle.ValueRO.restLength + vehicle.ValueRO.springTravel + vehicle.ValueRO.wheelRadius),
                    Filter = wheelCollisionFilter
                };
                
                bool wheelRaycastHit = physicsWorld.CastRay(ray, out RaycastHit hitData);
                float wheelRaycastHitDistance = wheelRaycastHit ? math.distance(wheelWorldPosition, hitData.Position) : 0.0f;
                
                if (!wheelRaycastHit) continue;
                
                groundedWheels++;
                
                if (vehicle.ValueRO.enableSuspension) {
                    float3 wheelWorldVelocity = GetPointVelocity(wheelWorldPosition, vehicleVelocity.ValueRO.Angular, vehicleVelocity.ValueRO.Linear);
     
                    float springCurrentLength = wheelRaycastHitDistance - vehicle.ValueRO.wheelRadius;
                    float springCompression = (vehicle.ValueRO.restLength - springCurrentLength) / vehicle.ValueRO.springTravel;
                    float springVelocity = math.dot(wheelWorldVelocity, wheelWorldUp);
                    float dampForce = vehicle.ValueRO.damperStiffness * springVelocity;
                    float springForce = vehicle.ValueRO.springStiffness * springCompression;
                    float netForce = springForce - dampForce;
                        
                    AddForceAtPosition(netForce * wheelWorldUp * vehicle.ValueRO.suspensionMultiplier, wheelWorldPosition);
                }

                //UnityEngine.Debug.DrawLine(ray.Start, ray.End);
            }

            float3 carLocalVelocity = vehicleTransformMatrix.InverseTransformDirection(vehicleVelocity.ValueRO.Linear);
            float carVelocityRatio = math.length(carLocalVelocity) / vehicle.ValueRO.maxSpeed;
            
            if (groundedWheels > 1) {
                #region Accel/Decel
                
                float3 accelerationPoint = vehicleTransformMatrix.TransformPoint(vehicle.ValueRO.accelerationPointOffset);
                float3 accelForce = vehicleWorldForward * inputDirection.z;
                
                if (vehicle.ValueRO.enableAcceleration) {
                    AddForceAtPosition(accelForce * vehicle.ValueRO.accelerationForce, accelerationPoint,
                        ForceMode.Acceleration);
                    AddForceAtPosition(accelForce * -1.0f * vehicle.ValueRO.decelerationForce, accelerationPoint,
                        ForceMode.Acceleration);
                }
                
                #endregion

                #region Steering
                
                bool isMoving = inputDirection.x >= vehicle.ValueRO.movementDeadzone || inputDirection.x <= -vehicle.ValueRO.movementDeadzone;
                bool isMovingRight = inputDirection.z > 0.0f;

                if (isMoving && vehicle.ValueRO.enableSteering) {
                    AddRelativeTorque(
                        (vehicle.ValueRO.steerStrength * 
                         inputDirection.x * 
                         vehicle.ValueRO.turningCurve *
                         math.sign(carVelocityRatio)) *
                        vehicleWorldUp,
                        ForceMode.Acceleration);
                }
                #endregion
                
                #region Sideways Drag

                float dragMagnitude = -carLocalVelocity.x * vehicle.ValueRO.dragCoefficient;
                float3 dragForce = vehicleWorldRight * dragMagnitude;
                
                //Log.Debug($"Drag +{ dragForce }");
                
                if (vehicle.ValueRO.enableDrag) { 
                    AddForceAtPosition(dragForce,
                        vehicleCenterOfMass,
                        ForceMode.Acceleration);
                }
                #endregion
            }
            
            vehicleVelocity.ValueRW.SetAngularVelocityWorldSpace(vehicleMass.ValueRO, vehicleWorldRotation, vehicleVelocity.ValueRO.Angular + angularVelocity);
            vehicleVelocity.ValueRW.Linear += linearVelocity;
        }
        
        [BurstCompile]
        public void AddForceAtPosition(in float3 force, in float3 forcePosition, in ForceMode mode = ForceMode.Force) {
            linearVelocity += mode switch {
                ForceMode.Force => force * vehicleInverseMass * fixedDeltaTime,
                ForceMode.Acceleration => force * fixedDeltaTime,
                _ => throw new NotImplementedException()
            };
            AddRelativeTorque(math.cross(forcePosition - vehicleCenterOfMass, force), mode);
        }

        [BurstCompile]
        public void AddRelativeTorque(in float3 torque, in ForceMode mode = ForceMode.Force)
            => angularVelocity += mode switch {
                ForceMode.Force => torque * vehicleInverseInertia * fixedDeltaTime,
                ForceMode.Acceleration => torque * fixedDeltaTime,
                _ => throw new NotImplementedException()
            };

        [BurstCompile]
        public float3 GetPointVelocity(in float3 point, in float3 currentAngularVelocity, in float3 currentLinearVelocity) {
            float3 r = point - vehicleWorldPosition;
            float3 angularContribution = math.cross(currentAngularVelocity, r);
            return currentLinearVelocity + angularContribution;
        }
    }


    
    /*
     
    public (float3, float3) AddAccelerationAtPosition(
        in float3 force,
        in float3 forcePosition,
        in float3 transformPosition,
        in float3 inverseInertia
    ) {
        //float3 relativePosition = forcePosition - transformPosition;
        float3 torque = math.cross(forcePosition, force);
        float3 angularAcceleration = AddRelativeTorque(torque, inverseInertia);
        return (force * fixedDeltaTime, angularAcceleration);
    }
     
     
     
     
     
     
    [BurstCompile]
    public struct VehicleJob : IJobParallelFor {
        [ReadOnly] public Entity vehicleEntity;
        [ReadOnly] public CollisionWorld physicsWorld;
        [ReadOnly] public float3 vehiclePosition;
        [ReadOnly] public PhysicsMass vehicleMass;
        [NativeDisableParallelForRestriction] public ComponentLookup<PhysicsVelocity> vehicleVelocityLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> wheelTransformLookup;
        [ReadOnly] public DynamicBuffer<Authoring.WheelEntity> wheelEntities;
        [ReadOnly] public float 
            wheelRaycastDistance,
            springDampening,
            springStrength,
            suspensionRestDistance;
        
        [BurstCompile]
        public void Execute(int index) {
            PhysicsVelocity vehicleVelocity = vehicleVelocityLookup[vehicleEntity];
            
            Unity.Logging.Log.Debug(" ");
            int wheelsRan = 0;
            
            foreach (Authoring.WheelEntity wheelEntity in wheelEntities) {
                LocalTransform wheelTransform = wheelTransformLookup[wheelEntity.value];
                wheelTransform.Position *= 100.0f; // manual local to world conversion, assumes parent is 100x scale and position zeroed
                
                wheelsRan++;
                
                RaycastInput ray = new RaycastInput() {
                    Start = wheelTransform.Position,
                    End = wheelTransform.Position + (math.down() * wheelRaycastDistance),
                    Filter = CollisionFilter.Default
                };
                
                UnityEngine.Debug.DrawLine(ray.Start, ray.End, UnityEngine.Color.cyan, 0.1f);

                bool wheelRaycastHit = physicsWorld.CastRay(ray, out RaycastHit hitData);
                float wheelRaycastHitDistance = wheelRaycastHit ? math.distance(wheelTransform.Position, hitData.Position) : 0.0f;

                Unity.Logging.Log.Debug($"ray #{ wheelsRan }:\n start: { ray.Start }\n end: { ray.End }\n hit: { wheelRaycastHit }\n distance: { wheelRaycastHitDistance }");
                
                if (!wheelRaycastHit) continue;
                
                float3 springDir = math.rotate(wheelTransform.Rotation, math.up());
                
                Extensions.GetPointVelocity(
                    vehicleVelocity.Linear,
                    vehicleVelocity.Angular,
                    vehicleMass.CenterOfMass,
                    wheelTransform.Position,
                    out float3 tireWorldVel);

                float offset = suspensionRestDistance - wheelRaycastHitDistance;
                float vel = math.dot(springDir, tireWorldVel);
                float force = (offset * springStrength) - (vel * springDampening);

                Extensions.AddForceAtPosition(
                    ref vehicleVelocity,
                    vehiclePosition,
                    vehicleMass.InverseMass,
                    vehicleMass.InverseInertia,
                    springDir * force,
                    wheelTransform.Position);

            }
            vehicleVelocityLookup[vehicleEntity] = vehicleVelocity;
        }
    }
    
    [BurstCompile]
    public partial struct WheelJob : IJobEntity {
        [ReadOnly] public PhysicsWorld physicsWorld;
        private void Execute(ref LocalTransform wheelTransform, ref Components.Wheel wheel) {
            RaycastInput ray = new RaycastInput {
                Start = wheelTransform.Position,
                End = wheelTransform.Position + (math.down() * wheel.raycastInputDistance),
            };

            wheel.rayHitGround = physicsWorld.CastRay(ray, out RaycastHit hit);
            wheel.raycastOutputDistance = wheel.rayHitGround ? math.distance(wheelTransform.Position, hit.Position) : 0.0f;

            // get wheels -> get transform -> raycast -> return data(bool hit, float distance) to wheel
        }
    }
    
    
    // gets vehicle, gets associated wheel transforms, starts vehicle job
    
    [BurstCompile]
    public partial struct VehicleJobOld : IJobEntity {
        // vehicle has references to wheel entities -> get wheel components and associated transforms -> apply calcs to vehicle
        private float suspensionRestDist;
        private float springStrength;
        private float springDamper;
        private float tireRayDist;
        private float3 vehiclePos;
        
        [NativeDisableParallelForRestriction]
        public BufferLookup<Authoring.WheelEntity> wheelEntityLookup;
        
        public NativeArray<LocalTransform> wheelTransforms;
        [ReadOnly] public PhysicsWorld world;
        
        [BurstCompile]
        private void Execute(Entity entity, RefRO<LocalTransform> wheelTransform, RefRW<PhysicsVelocity> velocity, RefRO<PhysicsMass> mass) {
            // if raycast
            
            // this will be deleted since the array will be satisfied before the job
            DynamicBuffer<Authoring.WheelEntity> wheels = wheelEntityLookup[entity]; 
            
            
            for (int i = 0; i < wheels.Length; i++) {
                
                //wheels[i].value
            }
            
            float3 springDir = math.rotate(wheelTransform.ValueRO.Rotation, math.up());

            Extensions.GetPointVelocity(
                velocity.ValueRO.Linear,
                velocity.ValueRO.Angular,
                mass.ValueRO.CenterOfMass,
                wheelTransform.ValueRO.Position,
                out float3 tireWorldVel);
            
            float offset = suspensionRestDist - tireRayDist;
            float vel = math.dot(springDir, tireWorldVel);
            float force = (offset * springStrength) - (vel * springDamper);
            
            Extensions.AddForceAtPosition(
                ref velocity.ValueRW,
                vehiclePos,
                mass.ValueRO.InverseMass,
                mass.ValueRO.InverseInertia,
                springDir * force,
                wheelTransform.ValueRO.Position);
        }
    }
    
    public partial struct HumanoidJob : IJobEntity {
        private void Execute(ref LocalTransform transform) {
            
        }
    }*/
    
    // vehicleVelocity.ValueRW.Angular += vehicle.ValueRO.steerStrength * inputDirection.x * 1.0f * math.sign(carVelocityRatio) *
    //               vehicleTransform.ValueRO.Up() * deltaTime;
}