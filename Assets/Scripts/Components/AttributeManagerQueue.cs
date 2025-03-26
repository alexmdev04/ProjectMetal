using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Metal.Components {
    [BurstCompile]
    public struct AttributeManagerQueue : IComponentData {
        public NativeQueue<AttributeManagerRequest> q;

        [BurstCompile]
        public void Request(AttributeManagerRequest request) {
            q.Enqueue(request);
        }

        [BurstCompile]
        public AttributeManagerRequest Dequeue() {
            return q.Dequeue();
        }
        
        public readonly int length => q.Count;
    }
    public struct AttributeModManagerQueue : IComponentData {
        public NativeQueue<AttributeModManagerRequest> q;

        [BurstCompile]
        public void Request(AttributeModManagerRequest request) {
            q.Enqueue(request);
        }

        [BurstCompile]
        public AttributeModManagerRequest Dequeue() {
            return q.Dequeue();
        }
        
        public readonly int length => q.Count;
    }
}