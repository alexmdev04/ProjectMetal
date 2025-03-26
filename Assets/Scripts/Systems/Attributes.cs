using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Metal {
    [BurstCompile]
    public struct AttributeRequest {
        public Entity senderEntity;
        public Entity recipientEntity;
        public AttributeType outgoingAttribute;
        public AttributeType targetAttribute;
        public bool isAddition; // default is to subtract
        private readonly double valueOverride; // hidden from constructors
        public double GetOverrideValue() => valueOverride;

        public enum Template {
            normalAttack,
            heal,
        }

        // default constructor
        public AttributeRequest(
            in Entity senderEntity,
            in Entity recipientEntity,
            in AttributeType outgoingAttribute,
            in AttributeType targetAttribute,
            in bool isAddition = false) {
            this.senderEntity = senderEntity;
            this.recipientEntity = recipientEntity;
            this.outgoingAttribute = outgoingAttribute;
            this.targetAttribute = targetAttribute;
            this.valueOverride = 0.0d;
            this.isAddition = isAddition;
        }
        
        // for manual values from the game instead of an entity
        public AttributeRequest(
            in double value,
            in Entity recipientEntity,
            in AttributeType targetAttribute,
            in bool isAddition = false) {
            this.senderEntity = Entity.Null;
            this.recipientEntity = recipientEntity;
            this.outgoingAttribute = AttributeType.none;
            this.targetAttribute = targetAttribute;
            this.valueOverride = value;
            this.isAddition = isAddition;
        }
        
        // uses templates to keep consistency with uses
        public AttributeRequest(
            in Template template,
            in Entity sender,
            in Entity recipient,
            in bool isAddition = false) {
            switch (template) {
                case Template.normalAttack: {
                    this.outgoingAttribute = AttributeType.damage;
                    this.targetAttribute = AttributeType.health;
                    break;
                }
                case Template.heal: {
                    this.outgoingAttribute = AttributeType.damage;
                    this.targetAttribute = AttributeType.health;
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(template), template, null);
            }
            this.senderEntity = sender;
            this.recipientEntity = recipient;
            this.valueOverride = 0.0d;
            this.isAddition = isAddition;
        }
    }

    public enum AttributeManagerRequestType { add, remove, edit }
    
    public struct AttributeManagerRequest {
        public AttributeManagerRequestType requestType;
        public AttributeType attType;
        public Entity entity;
    }
    
    public struct AttributeModManagerRequest {
        public AttributeManagerRequestType requestType;
        public AttributeModType modType;
        public Entity entity;
        public int modIndex;
    }
    
    namespace Systems {

        [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
        [UpdateAfter(typeof(Spawner))]
        [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
        [BurstCompile]
        public partial struct AttributeQueue<TAtt, TModAtt> : ISystem, ISystemStartStop
            where TAtt : unmanaged, Components.IAttribute
            where TModAtt : unmanaged, Components.IAttributeModValue {
            
            public Entity root;
            public ComponentLookup<TAtt> attLookup;
            public ComponentLookup<TModAtt> modAttLookup;

            [BurstCompile]
            public void OnCreate(ref SystemState state) {
                state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
                state.RequireForUpdate<Tags.Root>();
                attLookup = state.GetComponentLookup<TAtt>();
                modAttLookup = state.GetComponentLookup<TModAtt>();
            }

            [BurstCompile]
            public void OnStartRunning(ref SystemState state) {
                root = SystemAPI.GetSingletonEntity<Tags.Root>();
                state.EntityManager.AddComponentData(
                    root,
                    new Components.AttributeQueue() {
                        q = new NativeQueue<AttributeRequest>(Allocator.Persistent)
                    }
                );
            }

            [BurstCompile]
            public void OnUpdate(ref SystemState state) {
                attLookup.Update(ref state);
                modAttLookup.Update(ref state);
                
                NativeQueue<AttributeRequest> attributeQueue = SystemAPI.GetComponentRO<Components.AttributeQueue>(root).ValueRO.q;
                if (attributeQueue.IsEmpty()) { return; }
                // var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                //     .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
                
                Components.AttributeData attributeData = SystemAPI.GetComponent<Components.AttributeData>(root);
                NativeArray<AttributeRequest> attributeRequests = attributeQueue.ToArray(Allocator.TempJob);
                attributeQueue.Clear();
                
                var attributeQueueHandle = new AttributeQueueJob<TAtt, TModAtt>() {
                    //ecb = ecb,
                    attLookup = attLookup,
                    modAttLookup = modAttLookup,
                    attributeData = attributeData,
                    attributeRequests = attributeRequests
                }.Schedule(attributeRequests.Length, state.Dependency);
                attributeRequests.Dispose();
            }

            public void OnStopRunning(ref SystemState state) {

            }

            [BurstCompile]
            public void OnDestroy(ref SystemState state) {
                if (SystemAPI.TryGetSingleton(out Components.AttributeQueue attributeQueue)) {
                    attributeQueue.q.Dispose();
                }
            }
        }
        
        [BurstCompile]
        public partial struct AttributeQueueJob<TAtt, TModAtt> : IJobFor
            where TAtt : unmanaged, Components.IAttribute
            where TModAtt : unmanaged, Components.IAttributeModValue {
            
            //public EntityCommandBuffer.ParallelWriter ecb;
            public Components.AttributeData attributeData;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<AttributeRequest> attributeRequests;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<TAtt> attLookup;
            
            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<TModAtt> modAttLookup;
            
            public void Execute(int index) {
                AttributeRequest request = attributeRequests[index];
                TAtt recipientAtt = attLookup[request.recipientEntity];
                RefRW<TModAtt> recipientModAtt = modAttLookup.GetRefRW(request.recipientEntity);
                
                // edits working value and clamps it to the attribute clamp values
                recipientModAtt.ValueRW.value = math.clamp(recipientModAtt.ValueRW.value + request.outgoingAttribute switch {
                    AttributeType.none => request.isAddition ? 1.0d : -1.0d * request.GetOverrideValue(),
                    _ => request.isAddition ? 1.0d : -1.0d * modAttLookup[request.senderEntity].value
                }, recipientAtt.att.minModValue, recipientAtt.att.maxModValue);
            }
        }

        [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
        [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
        [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
        public partial struct AttributeUpdateSystem<TAtt, TModAtt> : ISystem, ISystemStartStop
            where TAtt : unmanaged, Components.IAttribute
            where TModAtt : unmanaged, Components.IAttributeModValue {

            public Entity root;
            public ComponentLookup<TAtt> attLookup;
            public ComponentLookup<TModAtt> modAttLookup;
            public BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeAdd>> addModsLookup;
            public BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeMul>> mulModsLookup;
            public BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeExp>> expModsLookup;
            public EntityQuery entitiesWithAttributes;

            public void OnCreate(ref SystemState state) {
                state.RequireForUpdate<Tags.Root>();
                state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
                attLookup = state.GetComponentLookup<TAtt>();
                modAttLookup = state.GetComponentLookup<TModAtt>();
                addModsLookup = state.GetBufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeAdd>>();
                mulModsLookup = state.GetBufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeMul>>();
                expModsLookup = state.GetBufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeExp>>();

                entitiesWithAttributes = state.GetEntityQuery(new EntityQueryDesc {
                    All = new[] {
                        ComponentType.ReadOnly<TAtt>(),
                        ComponentType.ReadWrite<TModAtt>(),
                    }
                });
            }

            public void OnStartRunning(ref SystemState state) {
                root = SystemAPI.GetSingletonEntity<Tags.Root>();
            }
            
            public void OnUpdate(ref SystemState state) {
                attLookup.Update(ref state);
                modAttLookup.Update(ref state);
                addModsLookup.Update(ref state);
                mulModsLookup.Update(ref state);
                expModsLookup.Update(ref state);
                
                var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
                
                NativeArray<Entity> entArr = entitiesWithAttributes.ToEntityArray(Allocator.TempJob);
                var attributeUpdateHandle = new AttributeUpdateJob<TAtt, TModAtt>() {
                    attLookup = attLookup,
                    modAttLookup = modAttLookup,
                    addModsLookup = addModsLookup,
                    mulModsLookup = mulModsLookup,
                    expModsLookup = expModsLookup,
                    entArr = entArr,
                }.Schedule(entArr.Length, 32);
                entArr.Dispose();
                
                NativeQueue<AttributeManagerRequest> attributeManagerRequestsQueue = 
                    SystemAPI.GetComponentRO<Components.AttributeManagerQueue>(root).ValueRO.q;
                if (attributeManagerRequestsQueue.IsEmpty()) { return; }

                NativeStream attributeManagerRequests = new NativeStream(attributeManagerRequestsQueue.Count, Allocator.TempJob);
                attributeManagerRequests.AsWriter().Allocate<AttributeManagerRequest>();
                
                var attributeManagerHandle = new AttributeManagerRequestJob<TAtt, TModAtt>() {
                    attLookup = attLookup,
                    addModsLookup = addModsLookup,
                    mulModsLookup = mulModsLookup,
                    expModsLookup = expModsLookup,
                }.Schedule(0, 32, attributeUpdateHandle);
                
                ///////////////////////////////////////////////////////////////////////////////////////
                
                NativeQueue<AttributeModManagerRequest> attributeModManagerRequests = 
                    SystemAPI.GetComponentRO<Components.AttributeModManagerQueue>(root).ValueRO.q;
                if (attributeModManagerRequests.IsEmpty()) { return; }
                
                var attributeModManagerHandle = new AttributeModManagerRequestJob<TAtt, TModAtt>() {
                    attLookup = attLookup,
                    addModsLookup = addModsLookup,
                    mulModsLookup = mulModsLookup,
                    expModsLookup = expModsLookup,
                }.Schedule(0, 32, attributeUpdateHandle);
            }

            public void OnStopRunning(ref SystemState state) {
                
            }
        }

        public partial struct AttributeUpdateJob<TAtt, TModAtt> : IJobParallelFor
            where TAtt : unmanaged, Components.IAttribute
            where TModAtt : unmanaged, Components.IAttributeModValue {

            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<TAtt> attLookup;
            
            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<TModAtt> modAttLookup;

            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeAdd>> addModsLookup;
            
            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeMul>> mulModsLookup;
            
            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeExp>> expModsLookup;

            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public NativeArray<Entity> entArr;

            public void Execute(int index) {
                Entity entity = entArr[index];
                RefRW<TAtt> att = attLookup.GetRefRW(entity);
                //TModAtt modAtt = modAttLookup[entity];
                DynamicBuffer<Components.AttributeMod<TAtt, Components.AttributeModTypeAdd>> addMods = addModsLookup[entity];
                DynamicBuffer<Components.AttributeMod<TAtt, Components.AttributeModTypeMul>> mulMods = mulModsLookup[entity];
                DynamicBuffer<Components.AttributeMod<TAtt, Components.AttributeModTypeExp>> expMods = expModsLookup[entity];
                AttributeModSet<TAtt> mods = new (addMods, mulMods, expMods);
                
                if (att.ValueRO.modsChanged) {
                    mods.CalculateModValue(att.ValueRW.att.baseValue, out double modValue);
                    RefRW<TModAtt> modAtt = modAttLookup.GetRefRW(entity);
                    modAtt.ValueRW.maxValue =
                        math.clamp(modValue, att.ValueRO.att.minModValue, att.ValueRO.att.maxModValue);
                    modAtt.ValueRW.value = 
                        math.clamp(modAtt.ValueRO.value, double.MinValue, modAtt.ValueRO.maxValue);
                    
                }
                
                // if (att.ValueRO.isBaseDirty) {
                //     mods.CalculateBaseValue(modAtt.maxValue, out double baseValue);
                //     if (baseValue != modAtt.maxValue) {
                //         att.ValueRW.SetBaseValue(baseValue);
                //     }
                // }
            }
        }

        public struct AttributeManagerRequestJob<TAtt, TModAtt> : IJobParallelFor 
            where TAtt : unmanaged, Components.IAttribute 
            where TModAtt : unmanaged, Components.IAttributeModValue {
            
            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<TAtt> attLookup;

            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeAdd>> addModsLookup;
            
            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeMul>> mulModsLookup;
            
            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeExp>> expModsLookup;
            
            //public BufferLookup<Components.AttributeMod<TAtt, TModType>> modLookup;
            public NativeStream requests;
            public NativeQueue<AttributeManagerRequest>.ParallelWriter requestsQ;
            
            public void Execute(int index) {
                //requests.AsReader().Read<AttributeModRequest<TAtt, TModType>>();
            }
        }
        
        public struct AttributeModManagerRequestJob<TAtt, TModAtt> : IJobParallelFor 
            where TAtt : unmanaged, Components.IAttribute 
            where TModAtt : unmanaged, Components.IAttributeModValue {
            
            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<TAtt> attLookup;

            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeAdd>> addModsLookup;
            
            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeMul>> mulModsLookup;
            
            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeExp>> expModsLookup;
            
            //public BufferLookup<Components.AttributeMod<TAtt, TModType>> modLookup;
            public NativeStream requests;
            public NativeQueue<AttributeManagerRequest>.ParallelWriter requestsQ;
            
            public void Execute(int index) {
                //requests.AsReader().Read<AttributeModRequest<TAtt, TModType>>();
            }
        }
    }
}