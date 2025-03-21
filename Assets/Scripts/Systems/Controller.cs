using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Metal.Systems {
    /// <summary>
    /// Sets movement component values, will only control entities with a controlled tag (e.g. Tags.Controller.Pathed)
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(Movement))]
    public partial struct Controller : ISystem, ISystemStartStop {
        private Entity
            root,
            player;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Tags.Root>();
            state.RequireForUpdate<Tags.Player>();
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state) {
            SystemAPI.TryGetSingletonEntity<Tags.Root>(out root);
            SystemAPI.TryGetSingletonEntity<Tags.Player>(out player);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            new PlayerJob {
                movementInput = SystemAPI.GetComponentRO<Components.Input>(root).ValueRO.movement
            }.ScheduleParallel();
            new PathedJob {
                playerPos = SystemAPI.GetComponentRO<LocalTransform>(player).ValueRO.Position,
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
            RefRW<Components.Movement> movement,
            in Tags.Controller.Player filter1) {
            
            movement.ValueRW.input = movementInput;
        }
    }

    [BurstCompile]
    internal partial struct PathedJob : IJobEntity {
        [ReadOnly] public float3 playerPos;
        [BurstCompile]
        private void Execute(
            RefRO<LocalTransform> transform,
            RefRW<Components.Movement> movement,
            in Tags.Controller.Pathed filter1) {

            float3 localDirToPlayer = transform.ValueRO.ToMatrix().InverseTransformDirection( // localise
                math.normalize(playerPos - transform.ValueRO.Position) // world direction to player
            );
            movement.ValueRW.input = new float3(localDirToPlayer.z < 0.0f ? math.sign(localDirToPlayer.x) : localDirToPlayer.x, 0, 1.0f);
            //UnityEngine.Debug.DrawRay(transform.ValueRO.Position, movement.ValueRW.input, Color.green);
        }
    }
}