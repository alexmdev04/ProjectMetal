using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Metal.Systems {
    /// <summary>
    /// Sets movement component values, will only control entities with a controlled tag (e.g. Tags.Controller.Pathed)
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(Input))]
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
            root = SystemAPI.GetSingletonEntity<Tags.Root>();
            player = SystemAPI.GetSingletonEntity<Tags.Player>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            new PlayerJob {
                //deltaTime = SystemAPI.Time.DeltaTime,
                movementInput = SystemAPI.GetComponentRO<Components.Input>(root).ValueRO.movement
            }.Schedule();
            new PathedJob {
                playerPos = SystemAPI.GetComponentRO<LocalTransform>(player).ValueRO.Position,
                //deltaTime = SystemAPI.Time.DeltaTime,
            }.Schedule();
        }

        [BurstCompile]
        public void OnStopRunning(ref SystemState state) {

        }
    }

    [BurstCompile]
    internal partial struct PlayerJob : IJobEntity { 
        [ReadOnly] public float3 movementInput;
        [BurstCompile]
        private void Execute(RefRW<Components.Movement> movement, in Tags.Controller.Player filter1) {
            movement.ValueRW.input = movementInput;
        }
    }

    [BurstCompile]
    internal partial struct PathedJob : IJobEntity {
        [ReadOnly] public float3 playerPos;
        [BurstCompile]
        private void Execute(RefRO<LocalTransform> transform, RefRW<Components.Movement> movement, in Tags.Controller.Pathed filter1) {
            movement.ValueRW.input = math.normalize(-(transform.ValueRO.Position - playerPos));
        }
    }
}