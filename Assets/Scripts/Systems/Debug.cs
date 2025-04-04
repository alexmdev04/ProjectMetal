#if UNITY_EDITOR
using Metal.Components;
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
        public Entity player;
        
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<AttributeQueue>();
            state.RequireForUpdate<Components.SpawnerData>();
            state.RequireForUpdate<Components.SpawnerQueue>();
            state.RequireForUpdate<Components.Input>();
        }
        
        public void OnStartRunning(ref SystemState state) {
            state.EntityManager.AddComponentData(state.SystemHandle, new Components.Debug());
            
            // Spawns default player
            NativeArray<AttributeModConstructor> mods = new (3, Allocator.Temp);
            mods[0] = new (AttributeType.health, AttributeModType.addition, 100.0d, 15.0f);
            mods[1] = new (AttributeType.health, AttributeModType.multiplier, 10.0d);
            mods[2] = new (AttributeType.health, AttributeModType.exponent, 2.0d);
            
            NativeArray<AttributeConstructor> attributes = new(5, Allocator.Temp);
            attributes[0] = new (AttributeType.health, 100.0d, mods);
            attributes[1] = new AttributeConstructor(AttributeType.damage, 10.0d);
            attributes[2] = new AttributeConstructor(AttributeType.accelerationSpeed, 15.0d);
            attributes[3] = new AttributeConstructor(AttributeType.cooldownRate, 1.0d);
            attributes[4] = new AttributeConstructor(AttributeType.fireRate, 1.0d);
            
            var spawnRequest = new SpawnPrefabRequest() {
                type = SpawnPrefabRequestType.Vehicle_Hilux,
                spawnTransform = new LocalTransform() {
                    Position = math.up() * 5.0f,
                    Rotation = quaternion.identity,
                    Scale = 1.0f
                },
                quantity = 1,
                isPlayer = true,
                isEnemy = false,
                attributeConstructors = attributes
            };
            //mods.Dispose();
            //attributes.Dispose();
            SystemAPI.GetSingleton<Components.SpawnerQueue>().Request(spawnRequest);
        }

        public void OnUpdate(ref SystemState state) {
            //Log.Debug("[Debug] //////////// Update Begin ////////////");
            if (UnityEngine.InputSystem.Keyboard.current.f5Key.wasPressedThisFrame) {
                new DebugVehicleResetTransform().Schedule();
            }
            
            if (UnityEngine.InputSystem.Keyboard.current.f7Key.isPressed || UnityEngine.InputSystem.Keyboard.current.f6Key.wasPressedThisFrame) {
                SystemAPI.GetSingleton<Components.SpawnerQueue>()
                    .Request(new SpawnPrefabRequest(SpawnPrefabRequestType.Vehicle_Hilux, 10));
            }

            if (SystemAPI.TryGetSingletonEntity<Tags.Player>(out player) &&
                (UnityEngine.InputSystem.Keyboard.current.lKey.isPressed ||
                 UnityEngine.InputSystem.Keyboard.current.kKey.wasPressedThisFrame)) {
                SystemAPI.GetSingleton<Components.AttributeQueue>()
                    .Request(new(100.0d, player, AttributeType.health));
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