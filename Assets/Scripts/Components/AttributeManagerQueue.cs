using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Metal.Components {
    [BurstCompile]
    public struct AttributeManagerQueue : IComponentData {
        public NativeQueue<AttributeManagerRequest> q;
    }
    
    [BurstCompile]
    public struct AttributeModManagerQueue : IComponentData  {
        public NativeQueue<AttributeModManagerRequest> q;
    }

    public struct AttributeManagerQueueFiltered<TAtt> : IComponentData
        where TAtt : unmanaged, IAttribute {
        public NativeQueue<AttributeManagerRequest> q;
    }
    
    public struct AttributeModManagerQueueFiltered<TAtt> : IComponentData
        where TAtt : unmanaged, IAttribute {
        public NativeQueue<AttributeModManagerRequest> q;
    }
}