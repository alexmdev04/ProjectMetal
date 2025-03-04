#if UNITY_EDITOR
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Metal.Systems {
    //[UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    public partial struct Debug : ISystem, ISystemStartStop {
        private Entity
            root,
            player;
        
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Tags.Player>();
            state.RequireForUpdate<Tags.Root>();
            state.RequireForUpdate<Components.Input>();
        }
        
        public void OnStartRunning(ref SystemState state) {
            root = SystemAPI.GetSingletonEntity<Tags.Root>();
        }

        public void OnUpdate(ref SystemState state) {
            RefRO<Components.Input> input = SystemAPI.GetComponentRO<Components.Input>(root);
            
            if (UnityEngine.InputSystem.Keyboard.current.f5Key.wasPressedThisFrame) {
                new DebugPlayerReset().Schedule();
            }
            
            if (UnityEngine.InputSystem.Keyboard.current.f7Key.isPressed || UnityEngine.InputSystem.Keyboard.current.f6Key.wasPressedThisFrame) {
                EntityCommandBuffer ecb = new (Allocator.Temp);
                ecb.Instantiate(input.ValueRO.debugVehiclePrefab);
                ecb.Playback(state.EntityManager);
                ecb.Dispose();
            }
        }


        public void OnStopRunning(ref SystemState state) {
            
        }
    }
    [BurstCompile]
    public partial struct DebugPlayerReset : IJobEntity {
        [BurstCompile]
        public void Execute(RefRO<Components.Vehicle> vehicle, RefRW<Unity.Transforms.LocalTransform> playerTransform, RefRW<Unity.Physics.PhysicsVelocity> playerVelocity) {
            Unity.Logging.Log.Debug("Vehicles Reset");
            if (!vehicle.IsValid) return;
            playerTransform.ValueRW.Position = math.up() * 7.5f;
            playerTransform.ValueRW.Rotation = quaternion.identity;
            playerVelocity.ValueRW.Linear = float3.zero;
            playerVelocity.ValueRW.Angular = float3.zero;
        }
    }
}
#endif