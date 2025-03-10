using Unity.Burst;
using Unity.Collections;
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
            public Entity 
                root;
            public Random random;
            
            [BurstCompile]
            public void OnCreate(ref SystemState state) {
                state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
                state.RequireForUpdate<Components.SpawnerData>();
                state.RequireForUpdate<Tags.Root>();
                random = Random.CreateFromIndex((uint)math.lerp(0.0f, 9999999.9f, SystemAPI.Time.DeltaTime));
            }

            [BurstCompile]
            public void OnStartRunning(ref SystemState state) {
                root = SystemAPI.GetSingletonEntity<Tags.Root>();
                state.EntityManager.AddComponentData(
                    root,
                    new Components.SpawnerQueue() {
                        q = new NativeQueue<SpawnPrefabRequest>(Allocator.Persistent)
                    }
                );
            }

            [BurstCompile]
            public void OnUpdate(ref SystemState state) {
                var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

                Components.SpawnerData spawnerData = SystemAPI.GetComponent<Components.SpawnerData>(root);
                NativeQueue<SpawnPrefabRequest> spawnerQueue = SystemAPI.GetComponentRO<Components.SpawnerQueue>(root).ValueRO.q; 
                NativeArray<SpawnPrefabRequest> spawnerArray = spawnerQueue.ToArray(Allocator.TempJob);
                spawnerQueue.Clear();

                float3 playerPosition = float3.zero;
                bool playerFound = SystemAPI.TryGetSingletonEntity<Tags.Player>(out Entity player);
                if (playerFound) {
                    playerPosition = SystemAPI.GetComponentRO<LocalTransform>(player).ValueRO.ToMatrix().c3.xyz;
                }
                
                new SpawnerJob() {
                    spawnerData = spawnerData,
                    spawnerArray = spawnerArray,
                    random = new Random(random.NextUInt()),
                    ecb = ecb,
                    playerFound = playerFound,
                    playerPosition = playerPosition
                }.Schedule(spawnerArray.Length, 32).Complete();

                spawnerArray.Dispose();
            }

            [BurstCompile]
            public void OnDestroy(ref SystemState state) {
                if (SystemAPI.TryGetSingleton(out Components.SpawnerQueue spawnerQueue)) {
                    spawnerQueue.q.Dispose();
                }
            }

            [BurstCompile]
            public void OnStopRunning(ref SystemState state) {
                
            }
        }
        
        [BurstCompile]
        public struct SpawnerJob : IJobParallelFor {
            public EntityCommandBuffer.ParallelWriter ecb;
            public Components.SpawnerData spawnerData;
            public NativeArray<SpawnPrefabRequest> spawnerArray;
            public bool playerFound;
            public float3 playerPosition;
            public Random random;

            [BurstCompile]
            public void Execute(int index) {
                SpawnPrefabRequest request = spawnerArray[index];
                Entity newEntity = ecb.Instantiate(index, spawnerData.GetEntityPrefab(request.type));
                
                if (request.spawnTransform.Scale <= float.Epsilon) {
                    Log.Warning(
                        "[Systems.Spawner] A spawn request was made with a transform scale <= 0, this is probably bad.");
                }

                if (request.isPlayer) {
                    ecb.AddComponent<Tags.Player>(index, newEntity);
                }

                if (request.isEnemy) {
                    ecb.AddComponent<Tags.Controller.Pathed>(index, newEntity);
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
        public bool isEnemy;
        public bool isPlayer;
        public LocalTransform spawnTransform;
        
        public SpawnPrefabRequest(
            SpawnPrefabRequestType type,
            LocalTransform spawnTransform,
            bool isEnemy,
            bool isPlayer = false) {
            this.type = type;
            this.spawnTransform = spawnTransform;
            this.isEnemy = isEnemy;
            this.isPlayer = isPlayer;
        }
        
        // this constructor represents most spawn requests
        public SpawnPrefabRequest(SpawnPrefabRequestType type) {
            this.type = type;
            spawnTransform = LocalTransform.Identity;
            isPlayer = false;
            isEnemy = true;
        }
    }
}