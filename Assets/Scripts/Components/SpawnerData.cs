using Unity.Burst;
using Unity.Entities;

namespace Metal {
    namespace Components {
        [BurstCompile]
        public struct SpawnerData : IComponentData {
            public float playerEnemySpawnRadius;
            public bool spawnerLogging;
            public Entity vehicleHilux;
            
            public readonly Entity GetEntityPrefab(SpawnPrefabRequestType spawnRequestType) => spawnRequestType switch {
                SpawnPrefabRequestType.Vehicle_Hilux => vehicleHilux,
                _ => Entity.Null
            };
        }
    }

    public enum SpawnPrefabRequestType {
        Vehicle_Hilux,
    }
}
