using Unity.Entities;

namespace Metal.Tags { 
    public struct Player : IComponentData { }
    
    public struct Root : IComponentData { }

    namespace Controller {
        public struct Player : IComponentData { }
        
        public struct Pathed : IComponentData { }
    }

    namespace Movement {
        //public struct Vehicle : IComponentData { }
        
        public struct Humanoid : IComponentData { }
    }
}