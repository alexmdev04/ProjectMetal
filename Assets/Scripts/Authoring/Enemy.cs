using Unity.Entities;
using UnityEngine;

namespace Metal.Authoring {
    public class Enemy : MonoBehaviour {
        
    }
    
    public class EnemyBaker : Baker<Enemy> {
        public override void Bake(Enemy authoring) {
            //AddComponent(new ExampleComponent { });
        }
    }
}