using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Logging;
using UnityEngine;

namespace Metal.Authoring {
    public class Root : MonoBehaviour {
        [Header("Spawner Config")]
        public GameObject vehicleHilux;
        public float playerEnemySpawnRadius = 25.0f;
        public bool spawnerLogging;
    }
    
    public class RootBaker : Baker<Root> {
        public override void Bake(Root authoring) {
            Entity root = GetEntity(TransformUsageFlags.None);
            AddComponent<Tags.Root>(root);
            AddComponent(root, new Components.Input { });
            
            // assign prefabs
            AddComponent(root, new Components.SpawnerData {
                vehicleHilux = GetEntity(authoring.vehicleHilux, TransformUsageFlags.Dynamic),
                playerEnemySpawnRadius = authoring.playerEnemySpawnRadius,
                spawnerLogging = authoring.spawnerLogging
            });

            #if UNITY_EDITOR
            AddComponent(root, new Components.Debug(){});
            #endif
            
            //AddComponent<Components.SharedBlobAssets>(root);
        }
    }
}