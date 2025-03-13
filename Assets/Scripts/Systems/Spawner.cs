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

            // public void UpdateQueue<TRequest, TComponentData, TComponentQueue, TJob>(
            //     in WorldUnmanaged worldUnmanaged,
            //     //out EntityCommandBuffer.ParallelWriter ecb,
            //     out NativeArray<TRequest> array,
            //     //out TComponentData data,
            //     out TJob job)
            //     where TRequest : unmanaged 
            //     where TComponentData : unmanaged, IComponentData
            //     where TComponentQueue : unmanaged, IComponentData, Extensions.IQueue<TRequest>
            //     where TJob : IJobParallelFor, IQueueJob<TComponentData, TRequest>, new() {
            //     
            //     EntityCommandBuffer.ParallelWriter ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            //         .CreateCommandBuffer(worldUnmanaged).AsParallelWriter();
            //
            //     TComponentData data = SystemAPI.GetComponent<TComponentData>(root);
            //     
            //     SystemAPI.GetComponentRO<TComponentQueue>(root).ValueRO.GetQueue(out NativeQueue<TRequest> queue); 
            //     array = queue.ToArray(Allocator.TempJob);
            //     queue.Clear();
            //
            //     job = new TJob() {
            //         ecb = ecb,
            //         data = data,
            //         array = array
            //     };
            // }
            
            [BurstCompile]
            public void OnUpdate(ref SystemState state) {
                // UpdateQueue<SpawnPrefabRequest, Components.SpawnerData, Components.SpawnerQueue, SpawnerJob>(
                //     state.WorldUnmanaged,
                //     //out var ecb,
                //     out var spawnerArray,
                //     //out var spawnerData,
                //     out var spawnerJob
                // );
                
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

                // spawnerJob.random = new Random(random.NextUInt());
                // spawnerJob.playerFound = playerFound;
                // spawnerJob.playerPosition = playerPosition;
                // spawnerJob.Schedule(spawnerArray.Length, 32).Complete();
                
                new SpawnerJob() {
                    data = spawnerData,
                    array = spawnerArray,
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

        // public interface IQueueJob<TComponentData, TRequest> 
        //     where TComponentData : unmanaged, IComponentData 
        //     where TRequest : struct {
        //     public EntityCommandBuffer.ParallelWriter ecb { get; set; }
        //     public TComponentData data { get; set; }
        //     public NativeArray<TRequest> array { get; set;  }
        // }
        
        [BurstCompile]
        public struct SpawnerJob : IJobParallelFor {//, IQueueJob<Components.SpawnerData, SpawnPrefabRequest> {
            public EntityCommandBuffer.ParallelWriter ecb;
            // public EntityCommandBuffer.ParallelWriter ecb { get; set; }
            // public Components.SpawnerData data { get; set; }
            // public NativeArray<SpawnPrefabRequest> array { get; set; }
            public Components.SpawnerData data;
            public NativeArray<SpawnPrefabRequest> array;
            public bool playerFound;
            public float3 playerPosition;
            public Random random;

            [BurstCompile]
            public void Execute(int index) {
                SpawnPrefabRequest request = array[index];
                Entity newEntity = ecb.Instantiate(index, data.GetEntityPrefab(request.type));
                
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
                        (math.forward() * data.playerEnemySpawnRadius) + (math.up() * 3.0f)
                    );

                    request.spawnTransform.Rotation = quaternion.LookRotation(
                        -math.normalize(request.spawnTransform.Position - playerPosition),
                        math.up()
                    );

                    ecb.SetComponent(index, newEntity, request.spawnTransform);

                    if (data.spawnerLogging) {
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