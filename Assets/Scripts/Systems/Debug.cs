#if UNITY_EDITOR
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Logging;
using Unity.Mathematics;
using Unity.Transforms;

namespace Metal.Systems {
    [UpdateBefore(typeof(Spawner))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct Debug : ISystem, ISystemStartStop {
        private Entity
            root,
            player;
        
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Components.SpawnerData>();
            state.RequireForUpdate<Components.SpawnerQueue>();
            state.RequireForUpdate<Tags.Root>();
            state.RequireForUpdate<Components.Input>();
        }
        
        public void OnStartRunning(ref SystemState state) {
            Log.Debug("debug started");
            root = SystemAPI.GetSingletonEntity<Tags.Root>();
            SystemAPI.GetComponent<Components.SpawnerQueue>(root).Request(new SpawnPrefabRequest() {
                type = SpawnPrefabRequestType.Vehicle_Hilux,
                spawnTransform = new LocalTransform() {
                    Position = math.up() * 5.0f,
                    Rotation = quaternion.identity,
                    Scale = 1.0f
                },
                isPlayer = true,
                isEnemy = false,
            });
        }

        public void OnUpdate(ref SystemState state) {
            if (UnityEngine.InputSystem.Keyboard.current.f5Key.wasPressedThisFrame) {
                new DebugVehicleResetTransform().Schedule();
            }
            
            if (UnityEngine.InputSystem.Keyboard.current.f7Key.isPressed || UnityEngine.InputSystem.Keyboard.current.f6Key.wasPressedThisFrame) {
                SystemAPI.GetComponent<Components.SpawnerQueue>(root)
                    .Request(new SpawnPrefabRequest(SpawnPrefabRequestType.Vehicle_Hilux));
            }
        }

        public void OnStopRunning(ref SystemState state) {
            
        }

        public void OnDestroy(ref SystemState state) {
            
        }
    }
    [BurstCompile]
    public partial struct DebugVehicleResetTransform : IJobEntity {
        [BurstCompile]
        public void Execute(
            RefRO<Components.Vehicle> vehicle,
            RefRW<Unity.Transforms.LocalTransform> vehicleTransform,
            RefRW<Unity.Physics.PhysicsVelocity> vehicleVelocity) {
            
            Unity.Logging.Log.Debug("Vehicles Reset");
            if (!vehicle.IsValid) return;
            vehicleTransform.ValueRW.Position = math.up() * 7.5f;
            vehicleTransform.ValueRW.Rotation = quaternion.identity;
            vehicleVelocity.ValueRW.Linear = float3.zero;
            vehicleVelocity.ValueRW.Angular = float3.zero;
        }
    }
}
#endif