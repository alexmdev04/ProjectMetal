using System;
using Unity.Entities;
using UnityEngine;

namespace Metal.Authoring {
    public enum EntityType {
        vehicle,
        humanoid
    }
    
    public class Player : MonoBehaviour {
        public EntityType entityType;
    }
    
    public class PlayerBaker : Baker<Player> {
        public override void Bake(Player authoring) {
            Entity playerEntity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<Tags.Player>(playerEntity);
        }
    }
}