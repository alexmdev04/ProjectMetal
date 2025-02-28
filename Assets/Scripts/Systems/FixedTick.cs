using Unity.Burst;
using Unity.Entities;

// Credit: Ash Villaschi
// Source: https://github.com/cephen/foxglove/blob/main/Foxglove/Assets/Scripts/Core/FixedTickSystem.cs

namespace Metal {
    namespace Systems {
        /// <summary>
        /// This system tracks how many fixed updates have happened since the start of the game
        /// </summary>
        [BurstCompile]
        [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
        public partial struct FixedTickSystem : ISystem {
            public void OnCreate(ref SystemState state)
                => state.EntityManager.AddComponent<Components.Tick>(state.SystemHandle);

            [BurstCompile]
            public void OnUpdate(ref SystemState state)
                => SystemAPI.GetComponentRW<Components.Tick>(state.SystemHandle).ValueRW.value++;

            public void OnDestroy(ref SystemState state) { }
        }
    }

    namespace Components {
        public struct Tick : IComponentData {
            public uint value;

            // Allow Ticks to be implicitly converted to and from uint
            public static implicit operator uint(Tick t) => t.value;
            public static implicit operator Tick(uint t) => new() { value = t };
        }
    }
}
