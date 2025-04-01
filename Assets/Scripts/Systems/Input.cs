using Unity.Entities;
using UnityEngine;
using Metal.Input;
using Unity.Logging;
using Unity.Mathematics;
using Unity.Physics;

namespace Metal.Systems {
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(Controller))]
    public partial class Input : SystemBase {
        private GameInput input;
        private Camera mainCamera;
        private CollisionFilter aimCursorRayFilter;

        protected override void OnCreate() {
            input = new GameInput();
        }

        protected override void OnStartRunning() {
            input.Enable();
            mainCamera = Camera.main;
            aimCursorRayFilter = new CollisionFilter {
                BelongsTo = CollisionFilter.Default.BelongsTo,
                CollidesWith = ~(uint)Movement.CollisionLayer.vehicle,
                GroupIndex = CollisionFilter.Default.GroupIndex,
            };
        }

        protected override void OnUpdate() {
            Vector2 movementValue = input.Player.Movement.ReadValue<Vector2>();
            RefRW<Components.Input> inputComponent = SystemAPI.GetSingletonRW<Components.Input>();

            inputComponent.ValueRW.movement = new float3(movementValue.x, 0.0f, movementValue.y);
            inputComponent.ValueRW.aimDirectional = input.Player.AimDirectional.ReadValue<Vector2>();
            var aimCursorRay = mainCamera.ScreenPointToRay(input.Player.AimCursor.ReadValue<Vector2>());
            inputComponent.ValueRW.aimCursorRay = new RaycastInput { 
                    Start = aimCursorRay.origin, 
                    End = aimCursorRay.origin + aimCursorRay.direction * 250.0f,
                    Filter = aimCursorRayFilter
            };
            inputComponent.ValueRW.brake = new button {
                isPressed = input.Player.Brake.IsPressed(),
                wasReleasedThisFrame = input.Player.Brake.WasReleasedThisFrame(),
                wasPressedThisFrame = input.Player.Brake.WasPressedThisFrame()
            };
            inputComponent.ValueRW.shoot = new button {
                isPressed = input.Player.Shoot.IsPressed(),
                wasReleasedThisFrame = input.Player.Shoot.WasReleasedThisFrame(),
                wasPressedThisFrame = input.Player.Shoot.WasPressedThisFrame()
            };
            inputComponent.ValueRW.ability1 = new button {
                isPressed = input.Player.Ability1.IsPressed(),
                wasReleasedThisFrame = input.Player.Ability1.WasReleasedThisFrame(),
                wasPressedThisFrame = input.Player.Ability1.WasPressedThisFrame()
            };
            inputComponent.ValueRW.ability2 = new button {
                isPressed = input.Player.Ability2.IsPressed(),
                wasReleasedThisFrame = input.Player.Ability2.WasReleasedThisFrame(),
                wasPressedThisFrame = input.Player.Ability2.WasPressedThisFrame()
            };
            inputComponent.ValueRW.ability3 = new button {
                isPressed = input.Player.Ability3.IsPressed(),
                wasReleasedThisFrame = input.Player.Ability3.WasReleasedThisFrame(),
                wasPressedThisFrame = input.Player.Ability3.WasPressedThisFrame()
            };
            inputComponent.ValueRW.ability4 = new button {
                isPressed = input.Player.Ability4.IsPressed(),
                wasReleasedThisFrame = input.Player.Ability4.WasReleasedThisFrame(),
                wasPressedThisFrame = input.Player.Ability4.WasPressedThisFrame()
            };
            inputComponent.ValueRW.turbo = new button {
                isPressed = input.Player.Turbo.IsPressed(),
                wasReleasedThisFrame = input.Player.Turbo.WasReleasedThisFrame(),
                wasPressedThisFrame = input.Player.Turbo.WasPressedThisFrame()
            };
            inputComponent.ValueRW.interact = new button {
                isPressed = input.Player.Interact.IsPressed(),
                wasReleasedThisFrame = input.Player.Interact.WasReleasedThisFrame(),
                wasPressedThisFrame = input.Player.Interact.WasPressedThisFrame()
            };
            inputComponent.ValueRW.stats = new button {
                isPressed = input.Player.Stats.IsPressed(),
                wasReleasedThisFrame = input.Player.Stats.WasReleasedThisFrame(),
                wasPressedThisFrame = input.Player.Stats.WasPressedThisFrame()
            };
            inputComponent.ValueRW.pause = new button {
                isPressed = input.Player.Pause.IsPressed(),
                wasReleasedThisFrame = input.Player.Pause.WasReleasedThisFrame(),
                wasPressedThisFrame = input.Player.Pause.WasPressedThisFrame()
            };
        }

        protected override void OnStopRunning() {
            input.Disable();
        }
    }
}
