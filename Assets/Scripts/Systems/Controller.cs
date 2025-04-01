using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Logging;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
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
        //private CollisionFilter aimCursorCastFilter;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<Components.Input>();
            state.RequireForUpdate<Tags.Player>();
            state.RequireForUpdate<Tags.Root>();
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
            RefRO<Components.Input> input = SystemAPI.GetComponentRO<Components.Input>(root);
            RefRW<Components.Controller> playerController = SystemAPI.GetComponentRW<Components.Controller>(player);
            playerController.ValueRW.movementInput = input.ValueRO.movement;
            
            LocalTransform playerTransform = SystemAPI.GetComponentRO<LocalTransform>(player).ValueRO;
            
            if (Hint.Likely(playerVehicleMountEntity != Entity.Null)) {
                RefRW<LocalTransform> mountTransform = SystemAPI.GetComponentRW<LocalTransform>(playerVehicleMountEntity);
                var mountPos = playerTransform.Position + math.rotate(playerTransform.Rotation, mountTransform.ValueRO.Position);
                
                SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CastRay(input.ValueRO.aimCursorRay, out RaycastHit hitData);
                var weaponToAimCursorWorldPosDir = -math.normalize(mountPos - hitData.Position);
                mountTransform.ValueRW.Rotation = math.mul(
                    math.inverse(playerTransform.Rotation),
                    quaternion.LookRotation(weaponToAimCursorWorldPosDir, math.up())
                );
                playerController.ValueRW.aimInput = weaponToAimCursorWorldPosDir;
            }
            
            // new PlayerJob {
            //     movementInput = SystemAPI.GetSingleton<Components.Input>().movement
            // }.ScheduleParallel();
            new EnemyJob {
                playerPos = playerTransform.Position,
            }.ScheduleParallel();
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
        [ReadOnly] public float3 playerPos;
        [BurstCompile]
        private void Execute(
            RefRO<LocalTransform> transform,
            RefRW<Components.Controller> controller,
            in Tags.Controller.Enemy filter1) {

            float4x4 transformMatrix = transform.ValueRO.ToMatrix();
            
            float3 localSelfToPlayerDir = transformMatrix.InverseTransformDirection( // localise
                math.normalize(playerPos - transform.ValueRO.Position) // world direction to player
            );
            
            //float3 
                
                
            controller.ValueRW.movementInput = new float3(localSelfToPlayerDir.z < 0.0f ? math.sign(localSelfToPlayerDir.x) : localSelfToPlayerDir.x, 0, 1.0f);
            //UnityEngine.Debug.DrawRay(transform.ValueRO.Position, movement.ValueRW.input, Color.green);
        }
    }
}