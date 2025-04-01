using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Logging;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Metal.Systems {
    /// <summary>
    /// Sets movement component values, will only control entities with a controlled tag (e.g. Tags.Controller.Pathed)
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(Movement))]
    public partial struct Controller : ISystem, ISystemStartStop {
        private Entity player, playerVehicleMountEntity, root;
        public ComponentLookup<LocalTransform> transformLookup;
        //private CollisionFilter aimCursorCastFilter;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<Components.Input>();
            state.RequireForUpdate<Tags.Player>();
            state.RequireForUpdate<Tags.Root>();
            transformLookup = state.GetComponentLookup<LocalTransform>();
            // aimCursorCastFilter = new CollisionFilter {
            //     BelongsTo = CollisionFilter.Default.BelongsTo,
            //     CollidesWith = ~(uint)Movement.CollisionLayer.vehicle,
            //     GroupIndex = CollisionFilter.Default.GroupIndex,
            // };
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state) {
             player = SystemAPI.GetSingletonEntity<Tags.Player>();
             playerVehicleMountEntity = Entity.Null;
             if (SystemAPI.HasComponent<Components.Vehicle>(player)) {
                 playerVehicleMountEntity = SystemAPI.GetComponentRO<Components.Vehicle>(player).ValueRO.weaponMountEntity;
             }
             root = SystemAPI.GetSingletonEntity<Tags.Root>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            transformLookup.Update(ref state);
            RefRO<Components.Input> input = SystemAPI.GetComponentRO<Components.Input>(root);
            RefRW<Components.Controller> playerController = SystemAPI.GetComponentRW<Components.Controller>(player);
            playerController.ValueRW.movementInput = input.ValueRO.movement;
            
            Log.Warning("Using ComponentLookup<LocalTransform> in OnUpdate crashes a Unity Job sometimes -_-");
            LocalTransform playerTransform = transformLookup[player];
            if (Hint.Likely(playerVehicleMountEntity != Entity.Null)) {
                SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CastRay(
                    input.ValueRO.aimCursorRay, out RaycastHit hitData);
                
                GetMountDirection(
                    transformLookup[playerVehicleMountEntity].Position,
                    playerTransform, 
                    hitData.Position,
                    out playerController.ValueRW.aimInput
                );
            }
            
            // new PlayerJob {
            //     movementInput = SystemAPI.GetSingleton<Components.Input>().movement
            // }.ScheduleParallel();
            new EnemyJob {
                playerTransform = playerTransform,
                transformLookup = transformLookup
            }.ScheduleParallel();
        }
        
        [BurstCompile]
        public static void AimMountToPoint(
            in float3 mountLocalPos,
            in LocalTransform parentTransform, 
            in float3 targetPoint,
            out quaternion localRotation) {
            GetMountDirection(mountLocalPos, parentTransform, targetPoint, out var aimDir);
            GetMountRotation(aimDir, parentTransform.Rotation, out localRotation);
        }
        
        [BurstCompile]
        public static void GetMountDirection(
            in float3 mountLocalPos,
            in LocalTransform parentTransform, 
            in float3 targetPoint,
            out float3 aimDir) {
            Extensions.GetChildRelativePosition(parentTransform.Position, parentTransform.Rotation, mountLocalPos, out var mountPos);
            Extensions.PointToPointDirection(mountPos, targetPoint, out aimDir);
        }
        
        [BurstCompile]
        public static void GetMountRotation(
            in float3 direction,
            in quaternion parentRotation,
            out quaternion localRotation) {
            localRotation = math.mul(
                math.inverse(parentRotation),
                quaternion.LookRotation(direction, math.up())
            );
        }

        [BurstCompile]
        public void OnStopRunning(ref SystemState state) {

        }
    }

    [BurstCompile]
    internal partial struct PlayerJob : IJobEntity { 
        [ReadOnly] public float3 movementInput;
        [BurstCompile]
        private void Execute(
            RefRW<Components.Controller> controller,
            in Tags.Controller.Player filter1) {
            
            controller.ValueRW.movementInput = movementInput;
            //controller.ValueRW.aimInput = 
        }
    }

    [BurstCompile]
    internal partial struct EnemyJob : IJobEntity {
        [ReadOnly] public LocalTransform playerTransform;
        [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;
        [BurstCompile]
        private void Execute(
            in Entity entity,
            RefRW<Components.Controller> controller,
            RefRO<Components.Vehicle> vehicle,
            in Tags.Controller.Enemy filter1) {

            var transform = transformLookup[entity];
            float4x4 transformMatrix = transform.ToMatrix();
            
            float3 localSelfToPlayerDir = transformMatrix.InverseTransformDirection( // localise
                math.normalize(playerTransform.Position - transform.Position) // world direction to player
            );
                
            controller.ValueRW.movementInput = new float3(localSelfToPlayerDir.z < 0.0f ? math.sign(localSelfToPlayerDir.x) : localSelfToPlayerDir.x, 0, 1.0f);
            if (Hint.Likely(vehicle.ValueRO.weaponMountEntity != Entity.Null)) {
                Controller.GetMountDirection(
                    transformLookup[vehicle.ValueRO.weaponMountEntity].Position,
                    transform,
                    playerTransform.Position,
                    out controller.ValueRW.aimInput
                );
            }
        }
    }
}