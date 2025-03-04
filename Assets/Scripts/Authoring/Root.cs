using System;
using Unity.Entities;
using Unity.Logging;
using UnityEngine;

namespace Metal.Authoring {
    public class Root : MonoBehaviour {
        public GameObject debugVehiclePrefab;
    }
    
    public class RootBaker : Baker<Root> {
        public override void Bake(Root authoring) {
            Entity debugVehiclePrefab = GetEntity(authoring.debugVehiclePrefab, TransformUsageFlags.Dynamic);
            Entity root = GetEntity(TransformUsageFlags.WorldSpace);
            
            AddComponent(root, new Components.Input {
                debugVehiclePrefab = debugVehiclePrefab
            });
            
            AddComponent<Tags.Root>(root);

            #if UNITY_EDITOR
            AddComponent<Components.Debug>(root);
            #endif
            
            //AddComponent<Components.SharedBlobAssets>(root);
        }
    }
}