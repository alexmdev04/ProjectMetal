using Unity.Burst;
using Unity.Entities;
using Unity.Logging;

namespace Metal {
    namespace Systems {
        [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
        [UpdateAfter(typeof(Spawner))]
        [BurstCompile]
        public partial struct Stats : ISystem {

            [BurstCompile]
            public void OnCreate(ref SystemState state) {

            }

            [BurstCompile]
            public void OnUpdate(ref SystemState state) {
                new TestBob().Run();
            }

            [BurstCompile]
            public void Initialise() {
                
            }
        }

    }

    [BurstCompile]
    public partial struct TestBob : IJobEntity {
        public void Execute(RefRW<Components.StatValues.Health> health) {
            //StatValue.Get(health, out double healthValue);
            //Log.Debug($"{ healthValue }");
            StatValue.Add(health, 10.0d);
        }
    } 
    
    [BurstCompile]
    public struct ConstructStatValue {
        public StatValueType statValueType;
        public double value;
        public double valueMax;
        public double valueMin;
        public bool clamped;
        public bool locked;
    }

    public enum StatValueType {
        none,
        health,
        cooldownRate,
        fireRate,
        movementSpeed
    }

    public enum StatType {
        none,
        damageDealt,
        damageReceived,
        movementSpeed,
        fireRate,
        cooldownRate
    }
}