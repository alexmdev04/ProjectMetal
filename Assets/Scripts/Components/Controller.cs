using Unity.Entities;
using Unity.Mathematics;

namespace Metal {
    namespace Components {
        /// <summary>
        /// Receives values from the controller system to be used by other systems that need input, the entity must have a tag defining what it's controlled by (e.g. Tags.Controller.Pathed).
        /// </summary>
        public struct Controller : IComponentData {
            public float3 movementInput;
            public float3 aimInput;
        }
    }
}