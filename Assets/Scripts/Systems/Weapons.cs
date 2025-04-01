using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Metal.Systems {
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(Controller))]
    [BurstCompile]
    public partial struct WeaponSystem : ISystem, ISystemStartStop {
        public ComponentLookup<LocalTransform> transformLookup;
        public Entity player;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Tags.Player>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            transformLookup = state.GetComponentLookup<LocalTransform>();
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state) {
            player = SystemAPI.GetSingletonEntity<Tags.Player>();
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            transformLookup.Update(ref state);
            new VehicleWeaponJob {
                //physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                transformLookup = transformLookup,
                //playerPosition = transformLookup[player].Position
            }.Schedule();
        }

        [BurstCompile]
        public void OnStopRunning(ref SystemState state) {
            
        }
        
        [BurstCompile]
        public void OnDestroy(ref SystemState state) {

        }
    }

    [BurstCompile]
    public partial struct VehicleWeaponJob : IJobEntity {
        //[ReadOnly] public PhysicsWorld physicsWorld;
        public ComponentLookup<LocalTransform> transformLookup;
        //public float3 playerPosition;

        [BurstCompile]
        public void Execute(
            in Entity entity,
            RefRO<Components.Vehicle> vehicle,
            RefRO<Components.Controller> controller) {

            if (Hint.Likely(vehicle.ValueRO.weaponMountEntity != Entity.Null)) {
                // var mountTransform = transformLookup.GetRefRW(vehicle.ValueRO.weaponMountEntity);
                //
                // Controller.AimMountToPoint(
                //     mountTransform.ValueRO.Position,
                //     transformLookup[entity],
                //     playerPosition,
                //     out mountTransform.ValueRW.Rotation
                // );
                
                Controller.GetMountRotation(
                    controller.ValueRO.aimInput,
                    transformLookup[entity].Rotation,
                    out transformLookup.GetRefRW(vehicle.ValueRO.weaponMountEntity).ValueRW.Rotation
                );
            }
        }
    }
}