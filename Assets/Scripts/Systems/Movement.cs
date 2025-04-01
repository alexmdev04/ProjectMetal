using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Metal.Systems {
    /// <summary>
    /// Executes calculations to move an entity, the entity must have a movement tag (e.g. Tags.Movement.Vehicle)
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct Movement : ISystem, ISystemStartStop {
        public enum CollisionLayer {
            all = ~0,
            ground = 1 << 0,
            vehicle = 1 << 1
        }
        private ComponentLookup<LocalTransform> transformLookup;
        private ComponentLookup<Components.AttAccelerationSpeedModValue> accelSpeedLookup;
        private BufferLookup<Authoring.WheelEntity> wheelEntitiesLookup;
        private const float fixedDeltaTime = 1.0f / 60.0f;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            transformLookup = state.GetComponentLookup<LocalTransform>();
            accelSpeedLookup = state.GetComponentLookup<Components.AttAccelerationSpeedModValue>();
            wheelEntitiesLookup = state.GetBufferLookup<Authoring.WheelEntity>();
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state) {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            transformLookup.Update(ref state);
            accelSpeedLookup.Update(ref state);
            wheelEntitiesLookup.Update(ref state);
            new VehicleJob {
                physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                transformLookup = transformLookup,
                accelSpeedLookup = accelSpeedLookup,
                wheelEntitiesLookup = wheelEntitiesLookup,
                fixedDeltaTime = fixedDeltaTime,
                wheelCollisionFilter = new CollisionFilter {
                    BelongsTo = CollisionFilter.Default.BelongsTo, 
                    CollidesWith = ~(uint)CollisionLayer.vehicle,
                    GroupIndex = CollisionFilter.Default.GroupIndex,
                },
                //fixedFrameCount = SystemAPI.GetSingleton<Components.Tick>().value
            }.ScheduleParallel();

            new HumanoidJob {
                deltaTime = SystemAPI.Time.DeltaTime,
                speed = 10.0f
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnStopRunning(ref SystemState state) {
            
        }
        
    }

    [BurstCompile]
    public partial struct VehicleJob : IJobEntity {
        [ReadOnly] public PhysicsWorld physicsWorld;
        [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;
        [ReadOnly] public ComponentLookup<Components.AttAccelerationSpeedModValue> accelSpeedLookup;
        [ReadOnly] public BufferLookup<Authoring.WheelEntity> wheelEntitiesLookup;
        [ReadOnly] public float fixedDeltaTime;
        [ReadOnly] public CollisionFilter wheelCollisionFilter;
        
        [BurstCompile]
        public void Execute(
            Entity vehicleEntity,
            RefRO<Components.Vehicle> vehicle,
            RefRO<LocalTransform> vehicleTransform,
            RefRO<PhysicsMass> vehicleMass, 
            RefRW<PhysicsVelocity> vehicleVelocity,
            RefRO<Components.Movement> movement) {

            float3 linearVelocityAccumulated = float3.zero;
            float3 angularVelocityAccumulated = float3.zero;
            float3 inputDirection = movement.ValueRO.input;
            // assumed vehicle is a uniformly scaled orphan object, so TransformHelpers.ComputeWorldTransformMatrix is not needed
            float4x4 vehicleTransformMatrix = vehicleTransform.ValueRO.ToMatrix();
            float3 vehicleWorldCenterOfMass = vehicleTransformMatrix.TransformPoint(vehicleMass.ValueRO.CenterOfMass);
            float3 vehicleWorldUp = vehicleTransformMatrix.Up();
            float3 wheelWorldUp = vehicleWorldUp;
            
            DynamicBuffer<Authoring.WheelEntity> wheelEntities = wheelEntitiesLookup[vehicleEntity];
            
            int groundedWheels = 0;

            foreach (Authoring.WheelEntity wheelEntity in wheelEntities) {
                
                float3 wheelWorldPosition = vehicleTransformMatrix.TransformPoint(transformLookup[wheelEntity.value].Position);
                
                if (!physicsWorld.CastRay(new RaycastInput {
                        Start = wheelWorldPosition,
                        End = wheelWorldPosition + wheelWorldUp * -1.0f * 
                            (vehicle.ValueRO.restLength + vehicle.ValueRO.springTravel + vehicle.ValueRO.wheelRadius),
                        Filter = wheelCollisionFilter
                    }, out RaycastHit hitData)) continue;
                
                groundedWheels++;

                // Suspension

                if (!vehicle.ValueRO.enableSuspension) { continue; }

                Extensions.GetPointVelocity(
                    vehicleVelocity.ValueRO.Linear,
                    vehicleVelocity.ValueRO.Angular,
                    vehicleWorldCenterOfMass,
                    wheelWorldPosition,
                    out float3 wheelWorldVelocity);
                
                float suspensionForce = 
                    vehicle.ValueRO.springStiffness * 
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

            float3 carLocalVelocity = vehicleTransformMatrix.InverseTransformDirection(vehicleVelocity.ValueRO.Linear);
            float carVelocityRatio = math.length(carLocalVelocity) / vehicle.ValueRO.maxSpeed;
            
            if (groundedWheels <= 1) { return; }

            // Accel/Decel

            float accelSpeed = accelSpeedLookup.TryGetComponent(vehicleEntity, out var modValue) ? 
                (float)modValue.value : vehicle.ValueRO.accelerationForce;

            if (vehicle.ValueRO.enableAcceleration) {
                Extensions.AddForceAtPosition(
                    ref linearVelocityAccumulated,
                    ref angularVelocityAccumulated,
                    vehicleTransformMatrix.Forward() * inputDirection.z * accelSpeed,
                    vehicleTransformMatrix.TransformPoint(vehicle.ValueRO.accelerationPointOffset),
                    vehicleMass.ValueRO.InverseMass,
                    vehicleMass.ValueRO.InverseInertia,
                    vehicleWorldCenterOfMass,
                    ForceMode.Acceleration);
            }
            
            // Steering
            
            if ((inputDirection.x >= vehicle.ValueRO.movementDeadzone ||
                 inputDirection.x <= vehicle.ValueRO.movementDeadzone * -1.0f) &&
                vehicle.ValueRO.enableSteering) {
                Extensions.AddRelativeTorque(
                    ref angularVelocityAccumulated,
                    (vehicle.ValueRO.steerStrength * 
                        inputDirection.x * 
                        vehicle.ValueRO.turningCurve.Value.value.Evaluate(math.abs(carVelocityRatio)) *
                        math.sign(carVelocityRatio)) * 
                        vehicleWorldUp,
                    vehicleMass.ValueRO.InverseInertia,
                    ForceMode.Acceleration);
            }
            
            // Sideways Drag

            if (vehicle.ValueRO.enableDrag) {
                Extensions.AddForceAtPosition(
                    ref linearVelocityAccumulated,
                    ref angularVelocityAccumulated,
                    vehicleTransformMatrix.Right() * (carLocalVelocity.x * -1.0f * vehicle.ValueRO.dragCoefficient),
                    vehicleWorldCenterOfMass,
                    vehicleMass.ValueRO.InverseMass,
                    vehicleMass.ValueRO.InverseInertia,
                    vehicleWorldCenterOfMass,
                    ForceMode.Acceleration);
            }
            
            vehicleVelocity.ValueRW.SetAngularVelocityWorldSpace(vehicleMass.ValueRO, vehicleTransformMatrix.Rotation(), vehicleVelocity.ValueRO.Angular + angularVelocityAccumulated);
            vehicleVelocity.ValueRW.Linear += linearVelocityAccumulated;
        }
    }
    
    [BurstCompile]
    public partial struct HumanoidJob : IJobEntity {
        [ReadOnly] public float deltaTime;
        [ReadOnly] public float speed;
        [BurstCompile]
        public void Execute(
            RefRO<Components.Movement> movementInput,
            RefRW<LocalTransform> transform,
            in Tags.Movement.Humanoid filter1) {
            
            transform.ValueRW.Position += movementInput.ValueRO.input * speed * deltaTime;
            if (!(math.lengthsq(movementInput.ValueRO.input.xz) > float.Epsilon)) { return; }
            float3 forwardDir = movementInput.ValueRO.input;
            forwardDir.y = 0.0f;
            transform.ValueRW.Rotation = quaternion.LookRotation(forwardDir, math.up());
        }
    }
}