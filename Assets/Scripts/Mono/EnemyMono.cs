using Unity.Entities;
using UnityEngine;

namespace Metal.Mono {
    public class EnemyMono : MonoBehaviour {
        
    }
    
    public class EnemyBaker : Baker<EnemyMono> {
        public override void Bake(EnemyMono authoring) {
            //AddComponent(new ExampleComponent { });
        }
    }
}