using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
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
            //public NativeQueue<SpawnPrefabRequest> spawnQueue; 
            
            [BurstCompile]
            public void OnCreate(ref SystemState state) {
                state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
                state.RequireForUpdate<Components.SpawnerData>();
                state.RequireForUpdate<Tags.Root>();
                random = Random.CreateFromIndex((uint)math.lerp(0.0f, 9999999.9f, SystemAPI.Time.DeltaTime));
                //random = Random.CreateFromIndex((uint)System.DateTime.UtcNow.GetHashCode());
            }

            [BurstCompile]
            public void OnStartRunning(ref SystemState state) {
                root = SystemAPI.GetSingletonEntity<Tags.Root>();
                //player = SystemAPI.GetSingletonEntity<Tags.Player>();
                state.EntityManager.AddComponentData(
                    root,
                    new Components.SpawnerQueue() {
                        q = new NativeQueue<SpawnPrefabRequest>(Allocator.Persistent)
                    }
                );
            }

            [BurstCompile]
            public void OnUpdate(ref SystemState state) {
                EntityCommandBuffer ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged);
                
                RefRO<Components.SpawnerData> spawner = SystemAPI.GetComponentRO<Components.SpawnerData>(root);
                Components.SpawnerQueue spawnerQueue = SystemAPI.GetComponent<Components.SpawnerQueue>(root);
                
                while (spawnerQueue.length > 0) {
                    SpawnPrefabRequest request = spawnerQueue.Dequeue();
                    Entity newEntity = ecb.Instantiate(spawner.ValueRO.GetEntityPrefab(request.type));

                    if (request.spawnTransform.Scale <= float.Epsilon) {
                        Log.Warning("[Systems.Spawner] A spawn request was made with a transform scale <= 0, this is probably bad.");
                    }
                    
                    if (request.isPlayer) {
                        ecb.AddComponent<Tags.Player>(newEntity);
                    }
                    
                    if (request.randomEnemyPosition && SystemAPI.TryGetSingletonEntity<Tags.Player>(out Entity player)) {
                        request.spawnTransform.Position = math.mul(
                           quaternion.Euler(0.0f, random.NextFloat(0.0f, 360.0f), 0.0f),
                           (math.forward() * spawner.ValueRO.playerEnemySpawnRadius) + (math.up() * 3.0f)
                        );
                    
                        request.spawnTransform.Rotation = quaternion.LookRotation(
                            -math.normalize(request.spawnTransform.Position - SystemAPI.GetComponent<LocalTransform>(player).ToMatrix().c3.xyz),
                            math.up()
                        );
                    }
                    
                    ecb.SetComponent(newEntity, request.spawnTransform);
                    LogSpawnRequest(request);
                }
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

            [BurstCompile]
            public void LogSpawnRequest(in SpawnPrefabRequest request) {
                Extensions.ToString(request.type, out FixedString32Bytes typeName);
                Log.Debug(
                    "[Systems.Spawner] New Spawn Request; {0}, pos: {1}, rot: {2}, flags: {3}",
                    typeName,
                    request.spawnTransform.Position,
                    math.Euler(request.spawnTransform.Rotation),
                    request.isPlayer ? "Player, " : ""
                );
            }
        }

    }

    [BurstCompile]
    public struct SpawnPrefabRequest {
        public SpawnPrefabRequestType type;
        public LocalTransform spawnTransform;
        public bool randomEnemyPosition;
        public bool isPlayer;

        public SpawnPrefabRequest(
            SpawnPrefabRequestType type,
            LocalTransform spawnTransform,
            bool randomEnemyPosition,
            bool isPlayer = false) {
            this.type = type;
            this.spawnTransform = spawnTransform;
            this.randomEnemyPosition = randomEnemyPosition;
            this.isPlayer = isPlayer;
        }

        public static SpawnPrefabRequest PlayerPreset = new SpawnPrefabRequest() {
            type = SpawnPrefabRequestType.Vehicle_Hilux,
            spawnTransform = new LocalTransform() {
                Position = math.up() * 5.0f,
                Rotation = quaternion.identity,
                Scale = 1.0f
            },
            isPlayer = true,
            randomEnemyPosition = false,
        };
        
        // this constructor represents most spawn requests
        public SpawnPrefabRequest(SpawnPrefabRequestType type) {
            this.type = type;
            spawnTransform = LocalTransform.Identity;
            isPlayer = false;
            randomEnemyPosition = true;
        }
    }
}