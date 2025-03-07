using Unity.Entities;

namespace Metal {
    namespace Components {
        public struct SpawnerData : IComponentData {
            public float playerEnemySpawnRadius;
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
