using Unity.Entities;
using Unity.Mathematics;

namespace Metal {
    namespace Components {
        /// <summary>
        /// Receives values from the controller system to be used by the movement system, the entity must have a tag defining what it's controlled by (e.g. Tags.Controller.Pathed).
        /// </summary>
        public struct Movement : IComponentData {
            public float3 input;
        }
    }
}