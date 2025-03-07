using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Metal.Components {
    [BurstCompile]
    public partial struct SpawnerQueue : IComponentData {
        public NativeQueue<SpawnPrefabRequest> q;

        [BurstCompile]
        public void Request(SpawnPrefabRequest request) {
            q.Enqueue(request);
        }

        [BurstCompile]
        public SpawnPrefabRequest Dequeue() {
            return q.Dequeue();
        }
        
        public readonly int length => q.Count;
    }
}
