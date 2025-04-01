using System;
using Metal.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Logging;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Metal {
    namespace Systems {
        [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
        [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
        [BurstCompile]
        public partial struct Spawner : ISystem, ISystemStartStop {
            public Random random;
            
            [BurstCompile]
            public void OnCreate(ref SystemState state) {
                state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
                state.RequireForUpdate<Components.SpawnerData>();
                state.RequireForUpdate<Components.AttributeManagerQueue>();
                state.RequireForUpdate<Components.AttributeModManagerQueue>();
                random = Random.CreateFromIndex((uint)math.lerp(0.0f, 9999999.9f, SystemAPI.Time.DeltaTime));
            }

            [BurstCompile]
            public void OnStartRunning(ref SystemState state) {
                state.EntityManager.AddComponentData(
                    state.SystemHandle,
                    new Components.SpawnerQueue() {
                        q = new NativeQueue<SpawnPrefabRequest>(Allocator.Persistent)
                    }
                );
            }

            [BurstCompile]
            public void OnUpdate(ref SystemState state) {
                NativeQueue<SpawnPrefabRequest> spawnerQueue = SystemAPI.GetComponent<Components.SpawnerQueue>(state.SystemHandle).q;
                int spawnerQueueLength = spawnerQueue.Count;
                if (spawnerQueueLength == 0) { return; }
                
                var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

                Components.SpawnerData spawnerData = SystemAPI.GetSingleton<Components.SpawnerData>();
                NativeArray<SpawnPrefabRequest> spawnerArray = spawnerQueue.ToArray(Allocator.TempJob);
                spawnerQueue.Clear();

                float3 playerPosition = float3.zero;
                bool playerFound = SystemAPI.TryGetSingletonEntity<Tags.Player>(out Entity player);
                if (playerFound) {
                    playerPosition = SystemAPI.GetComponentRO<LocalTransform>(player).ValueRO.ToMatrix().c3.xyz;
                }
                
                new SpawnerJob() {
                    // attributeManagerQueue = SystemAPI.GetSingleton<AttributeManagerQueue>().q.AsParallelWriter(),
                    // attributeModManagerQueue = SystemAPI.GetSingleton<AttributeModManagerQueue>().q.AsParallelWriter(),
                    spawnerData = spawnerData,
                    spawnRequests = spawnerArray,
                    random = new Random(random.NextUInt()),
                    ecb = ecb,
                    playerFound = playerFound,
                    playerPosition = playerPosition
                }.Run(spawnerQueueLength);
                spawnerArray.Dispose();
            }

            [BurstCompile]
            public void OnStopRunning(ref SystemState state) {
                
            }
            
            [BurstCompile]
            public void OnDestroy(ref SystemState state) { 
                SystemAPI.GetComponent<SpawnerQueue>(state.SystemHandle).q.Dispose();
            }
        }
        
        [BurstCompile]
        public struct SpawnerJob : IJobParallelFor {
            public EntityCommandBuffer.ParallelWriter ecb;
            // [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            // public NativeQueue<AttributeManagerRequest>.ParallelWriter attributeManagerQueue;
            // [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            // public NativeQueue<AttributeModManagerRequest>.ParallelWriter attributeModManagerQueue;
            public Components.SpawnerData spawnerData;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<SpawnPrefabRequest> spawnRequests;
            public bool playerFound;
            public float3 playerPosition;
            public Random random;

            [BurstCompile]
            public void Execute(int index) {
                SpawnPrefabRequest request = spawnRequests[index];
                for (int i = 0; i < request.quantity; i++) {
                    
                    Entity newEntity = ecb.Instantiate(index, spawnerData.GetEntityPrefab(request.type));

                    if (request.spawnTransform.Scale <= float.Epsilon) {
                        Log.Warning(
                            "[Systems.Spawner] A spawn request was made with a transform scale <= 0, this is probably bad.");
                    }

                    if (request.isPlayer) {
                        ecb.AddComponent<Tags.Player>(index, newEntity);
                    }

                    if (request.isEnemy) {
                        ecb.AddComponent<Tags.Controller.Enemy>(index, newEntity);
                    }

                    if (request.isEnemy && playerFound) {
                        request.spawnTransform.Position = math.mul(
                            quaternion.Euler(0.0f, random.NextFloat(0.0f, 360.0f), 0.0f),
                            (math.forward() * spawnerData.playerEnemySpawnRadius) + (math.up() * 3.0f)
                        );

                        request.spawnTransform.Rotation = quaternion.LookRotation(
                            -math.normalize(request.spawnTransform.Position - playerPosition),
                            math.up()
                        );

                        ecb.SetComponent(index, newEntity, request.spawnTransform);
                    }

                    if (request.attributeConstructors.HasValue) {
                        foreach (AttributeConstructor attributeConstructor in request.attributeConstructors) {
                            ecb.AttributeManagerRequest(index, AttributeManagerRequest.AddTemplate(newEntity, attributeConstructor));
                        }
                        break;
                    }
                    
                    if (spawnerData.spawnerLogging) {
                        LogSpawnRequest(request);
                    }
                }
            }

            [BurstCompile]
            public void LogSpawnRequest(in SpawnPrefabRequest request) {
                Extensions.ToString(request.type, out FixedString32Bytes typeName);
                //Extensions.SpawnFlagsToString(request, out FixedString32Bytes flags);
                Log.Debug(
                    "[Systems.Spawner] New Spawn Request; {0}, pos: {1}, rot: {2}",
                    typeName,
                    request.spawnTransform.Position,
                    math.Euler(request.spawnTransform.Rotation)
                );
            }
        }
    }
    
    
    [BurstCompile]
    public struct SpawnPrefabRequest {
        public SpawnPrefabRequestType type;
        public uint quantity;
        public bool isEnemy;
        public bool isPlayer;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<AttributeConstructor>? attributeConstructors;
        public LocalTransform spawnTransform;
        
        public SpawnPrefabRequest(
            SpawnPrefabRequestType type,
            bool isEnemy,
            LocalTransform spawnTransform,
            NativeArray<AttributeConstructor> attributeConstructors,
            uint quantity = 1,
            bool isPlayer = false) {
            this.type = type;
            this.spawnTransform = spawnTransform;
            this.quantity = quantity;
            this.isEnemy = isEnemy;
            this.isPlayer = isPlayer;
            this.attributeConstructors = attributeConstructors;
        }
        public SpawnPrefabRequest(
            SpawnPrefabRequestType type,
            bool isEnemy,
            LocalTransform spawnTransform,
            uint quantity = 1,
            bool isPlayer = false) {
            this.type = type;
            this.spawnTransform = spawnTransform;
            this.quantity = quantity;
            this.isEnemy = isEnemy;
            this.isPlayer = isPlayer;
            this.attributeConstructors = null;
        }
        
        // this constructor represents most spawn requests
        public SpawnPrefabRequest(SpawnPrefabRequestType type, uint quantity = 1) {
            this.type = type;
            this.quantity = quantity;
            this.spawnTransform = LocalTransform.Identity;
            this.isPlayer = false;
            this.isEnemy = true;
            var tempConstructors = new NativeArray<AttributeConstructor>(5, Allocator.Temp);
            tempConstructors[0] = new AttributeConstructor(AttributeType.health, 100.0d);
            tempConstructors[1] = new AttributeConstructor(AttributeType.damage, 10.0d);
            tempConstructors[2] = new AttributeConstructor(AttributeType.accelerationSpeed, 15.0d);
            tempConstructors[3] = new AttributeConstructor(AttributeType.cooldownRate, 1.0d);
            tempConstructors[4] = new AttributeConstructor(AttributeType.fireRate, 1.0d);
            this.attributeConstructors = tempConstructors;
        }
    }
}