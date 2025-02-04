using Unity.Entities;
using Unity.Mathematics;

namespace Metal {
    public struct button {
        public bool wasPressedThisFrame;
        public bool isPressed;
        public bool wasReleasedThisFrame;
    }

    namespace Components {
        public struct Input : IComponentData {
            public float2
                movement,
                aimDirectional,
                aimCursor;

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