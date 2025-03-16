using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;


namespace Metal {
    [BurstCompile]
    public static class StatValue {
        [BurstCompile]
        public static void Construct<T>(in ConstructStatValue constructor, out T statValue) where T : unmanaged, Components.IStatValue {
            statValue = new T {
                value = constructor.value,
                clamped = constructor.clamped,
                locked = constructor.locked,
                defaultValue = constructor.value,
                statValueType = constructor.statValueType,
                valueMax = constructor.valueMax,
                valueMin = constructor.valueMin
            };
        }

        [BurstCompile]
        public static void Get<T>(RefRO<T> statValue, out double value) where T : unmanaged, Components.IStatValue, IComponentData {
            value = statValue.ValueRO.value;
        }
        
        [BurstCompile]
        public static void Get<T>(RefRW<T> statValue, out double value) where T : unmanaged, Components.IStatValue, IComponentData {
            value = statValue.ValueRO.value;
        }
        
        [BurstCompile]
        public static void Get<T>(in T statValue, out double value) where T : unmanaged, Components.IStatValue {
            value = statValue.value;
        }

        [BurstCompile]
        public static void Set<T>(RefRW<T> statValue, in double value) where T : unmanaged, Components.IStatValue, IComponentData {
            if (statValue.ValueRO.locked) {
                return;
            }

            statValue.ValueRW.value = statValue.ValueRO.clamped
                ? math.clamp(value, statValue.ValueRO.valueMin, statValue.ValueRO.valueMax)
                : value;
        }
        
        [BurstCompile]
        public static void Set<T>(ref T statValue, in double value) where T : unmanaged, Components.IStatValue, IComponentData {
            if (statValue.locked) {
                return;
            }

            statValue.value = statValue.clamped
                ? math.clamp(value, statValue.valueMin, statValue.valueMax)
                : value;
        }

        [BurstCompile]
        public static void Add<T>(RefRW<T> statValue, in double value) where T : unmanaged, Components.IStatValue, IComponentData {
            Get(statValue, out double valGet);
            Set(statValue, valGet + value);
        }
        
        [BurstCompile]
        public static void Add<T>(ref T statValue, in double value) where T : unmanaged, Components.IStatValue, IComponentData {
            Get(statValue, out double valGet);
            Set(ref statValue, valGet + value);
        }
    }

    namespace Components {
        [UnityEngine.Scripting.RequireImplementors]
        public interface IStatValue {
            public StatValueType statValueType { get; set; } // readonly
            /// <summary>
            /// Do not directly get or set this value, use the Get() or Set() methods
            /// </summary>
            public double value { get; set; }
            public double defaultValue { get; set; }
            public double valueMax { get; set; }
            public double valueMin { get; set; }
            public bool clamped { get; set; }
            public bool locked { get; set; }
        }
        
        namespace StatValues {
            [BurstCompile]
            public struct Health : IComponentData, IStatValue {
                public StatValueType statValueType { get; set; }
                public double value { get; set; }
                public double defaultValue { get; set; }
                public double valueMax { get; set; }
                public double valueMin { get; set; }
                public bool clamped { get; set; }
                public bool locked { get; set; }
            }
            
            [BurstCompile]
            public struct CooldownRate : IComponentData, IStatValue {
                public StatValueType statValueType { get; set; }
                public double value { get; set; }
                public double defaultValue { get; set; }
                public double valueMax { get; set; }
                public double valueMin { get; set; }
                public bool clamped { get; set; }
                public bool locked { get; set; }
            }

            [BurstCompile]
            public struct FireRate : IComponentData, IStatValue {
                public StatValueType statValueType { get; set; }
                public double value { get; set; }
                public double defaultValue { get; set; }
                public double valueMax { get; set; }
                public double valueMin { get; set; }
                public bool clamped { get; set; }
                public bool locked { get; set; }
            }

            [BurstCompile]
            public struct MovementSpeed : IComponentData, IStatValue {
                public StatValueType statValueType { get; set; }
                public double value { get; set; }
                public double defaultValue { get; set; }
                public double valueMax { get; set; }
                public double valueMin { get; set; }
                public bool clamped { get; set; }
                public bool locked { get; set; }
            }
        }
    }
}