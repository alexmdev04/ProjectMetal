using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Metal.Components {
    [BurstCompile]
    public struct AttributeQueue : IComponentData {
        public NativeQueue<AttributeRequest> q;

        [BurstCompile]
        public void Request(AttributeRequest request) {
            q.Enqueue(request);
        }

        [BurstCompile]
        public AttributeRequest Dequeue() {
            return q.Dequeue();
        }
    }
    
    [BurstCompile]
    public struct AttributeQueueFiltered<TAtt> : IComponentData 
        where TAtt : unmanaged, IAttribute {
        public NativeQueue<AttributeRequest> q;

        [BurstCompile]
        public void Request(AttributeRequest request) {
            q.Enqueue(request);
        }

        [BurstCompile]
        public AttributeRequest Dequeue() {
            return q.Dequeue();
        }
    }
}