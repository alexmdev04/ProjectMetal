using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Metal {
    public struct button {
        public bool wasPressedThisFrame;
        public bool isPressed;
        public bool wasReleasedThisFrame;
    }

    namespace Components {
        public struct Input : IComponentData {
            public float3
                movement;
            
            public RaycastInput
                aimCursorRay;

            public float2
                aimDirectional;

            public button
                brake,
                shoot,
                ability1,
                ability2,
                ability3,
                ability4,
                turbo,
                interact,
                stats,
                pause;
        }
    }
}