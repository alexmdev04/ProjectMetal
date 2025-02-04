using Unity.Entities;
using UnityEngine;

namespace Metal.Systems {
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class Input : SystemBase {
        private GameInput input;
        
        protected override void OnCreate() {
            input = new GameInput();
        }

        protected override void OnStartRunning() {
            input.Enable();
        }

        protected override void OnUpdate() {
            SystemAPI.SetSingleton(new Components.Input {
                movement = input.Player.Movement.ReadValue<Vector2>(),
                aimDirectional = input.Player.AimDirectional.ReadValue<Vector2>(),
                aimCursor = input.Player.AimCursor.ReadValue<Vector2>(),
                brake = new button {
                    isPressed = input.Player.Brake.IsPressed(),
                    wasReleasedThisFrame = input.Player.Brake.WasReleasedThisFrame(),
                    wasPressedThisFrame = input.Player.Brake.WasPressedThisFrame()
                },
                shoot = new button {
                    isPressed = input.Player.Shoot.IsPressed(),
                    wasReleasedThisFrame = input.Player.Shoot.WasReleasedThisFrame(),
                    wasPressedThisFrame = input.Player.Shoot.WasPressedThisFrame()
                },
                ability1 = new button {
                    isPressed = input.Player.Ability1.IsPressed(),
                    wasReleasedThisFrame = input.Player.Ability1.WasReleasedThisFrame(),
                    wasPressedThisFrame = input.Player.Ability1.WasPressedThisFrame()
                },
                ability2 = new button {
                    isPressed = input.Player.Ability2.IsPressed(),
                    wasReleasedThisFrame = input.Player.Ability2.WasReleasedThisFrame(),
                    wasPressedThisFrame = input.Player.Ability2.WasPressedThisFrame()
                },
                ability3 = new button {
                    isPressed = input.Player.Ability3.IsPressed(),
                    wasReleasedThisFrame = input.Player.Ability3.WasReleasedThisFrame(),
                    wasPressedThisFrame = input.Player.Ability3.WasPressedThisFrame()
                },
                ability4 = new button {
                    isPressed = input.Player.Ability4.IsPressed(),
                    wasReleasedThisFrame = input.Player.Ability4.WasReleasedThisFrame(),
                    wasPressedThisFrame = input.Player.Ability4.WasPressedThisFrame()
                },
                turbo = new button {
                    isPressed = input.Player.Turbo.IsPressed(),
                    wasReleasedThisFrame = input.Player.Turbo.WasReleasedThisFrame(),
                    wasPressedThisFrame = input.Player.Turbo.WasPressedThisFrame()
                },
                interact = new button {
                    isPressed = input.Player.Interact.IsPressed(),
                    wasReleasedThisFrame = input.Player.Interact.WasReleasedThisFrame(),
                    wasPressedThisFrame = input.Player.Interact.WasPressedThisFrame()
                },
                stats = new button {
                    isPressed = input.Player.Stats.IsPressed(),
                    wasReleasedThisFrame = input.Player.Stats.WasReleasedThisFrame(),
                    wasPressedThisFrame = input.Player.Stats.WasPressedThisFrame()
                },
                pause = new button {
                    isPressed = input.Player.Pause.IsPressed(),
                    wasReleasedThisFrame = input.Player.Pause.WasReleasedThisFrame(),
                    wasPressedThisFrame = input.Player.Pause.WasPressedThisFrame()
                }
            });
        }
        
        protected override void OnStopRunning() {
            input.Disable();
        }
    }
}
