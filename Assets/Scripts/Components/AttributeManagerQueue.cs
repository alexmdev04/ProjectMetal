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

    public interface IAttributeRequests {
        public NativeStream data { get; set; }
        public int bufferCount { get; set; }
    }

    public struct AttributeManagerRequests<TAtt> : IComponentData, IAttributeRequests
        where TAtt : unmanaged, IAttribute {
        public NativeStream data { get; set; }
        public int bufferCount { get; set; }
    }
    
    public struct AttributeModManagerRequests<TAtt> : IComponentData, IAttributeRequests
        where TAtt : unmanaged, IAttribute {
        public NativeStream data { get; set; }
        public int bufferCount { get; set; }
    }
}