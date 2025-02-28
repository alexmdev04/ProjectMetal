using System;
using Unity.Entities;
using Unity.Logging;
using UnityEngine;

namespace Metal.Authoring {
    public class Root : MonoBehaviour {
        
    }
    
    public class RootBaker : Baker<Root> {
        public override void Bake(Root authoring) {
            Entity root = GetEntity(TransformUsageFlags.None);
            AddComponent<Components.Input>(root);
            AddComponent<Tags.Root>(root);
        }
    }
}