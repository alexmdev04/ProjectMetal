using System;
using Metal.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

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

    public enum AttributeManagerRequestType {
        add,
        remove,
        edit
    }
    
    public struct AttributeManagerRequest {
        private AttributeManagerRequestType requestType;
        private Entity entity;
        private AttributeConstructor constructor; // when removing you only need to set "type"
        public AttributeType attType => constructor.type;

        public static AttributeManagerRequest AddTemplate(
            in Entity entity,
            in AttributeConstructor constructor) {
            return new AttributeManagerRequest {
                requestType = AttributeManagerRequestType.add,
                constructor = constructor,
                entity = entity
            };
        }

        public static AttributeManagerRequest EditTemplate(
            in Entity entity,
            in AttributeConstructor constructor) => new() {
            requestType = AttributeManagerRequestType.edit,
            constructor = constructor,
            entity = entity
        };

        public static AttributeManagerRequest RemoveTemplate(
            in Entity entity) => new() {
            requestType = AttributeManagerRequestType.remove,
            entity = entity
        };

        // public void ProcessByType(
        //     ref EntityCommandBuffer.ParallelWriter ecb,
        //     in int index,
        //     in AttributeType attType) {
        //
        //     switch (attType) {
        //         case AttributeType.none: {
        //             break;
        //         }
        //         case AttributeType.health: {
        //             Process<Components.AttHealthBase, Components.AttHealthModValue>(ref ecb, index);
        //             break;
        //         }
        //         default: {
        //             throw new ArgumentOutOfRangeException(nameof(attType), attType, null);
        //         }
        //     }
        // }
        
        public void Process<TAtt, TModAtt> (
            ref EntityCommandBuffer.ParallelWriter ecb,
            in int index) 
            where TAtt : unmanaged, Components.IAttribute 
            where TModAtt : unmanaged, Components.IAttributeModValue {

            switch (requestType) {
                case AttributeManagerRequestType.add: {
                    ecb.AddComponent(index, entity, new TAtt {
                        att = new Components.AttributeInternal(constructor.baseValue, constructor.minModValue, constructor.maxModValue),
                        modsChanged = true
                    });
                    var addMods = ecb.AddBuffer<Components.AttributeMod<TAtt, Components.AttributeModTypeAdd>>(index, entity);
                    var mulMods = ecb.AddBuffer<Components.AttributeMod<TAtt, Components.AttributeModTypeMul>>(index, entity);
                    var expMods = ecb.AddBuffer<Components.AttributeMod<TAtt, Components.AttributeModTypeExp>>(index, entity);
            
                    if (!constructor.modConstructors.HasValue) return;
                    foreach (AttributeModConstructor modConstructor in constructor.modConstructors) {
                        // attribute type is already known so its not used
                        switch (modConstructor.modType) {
                            case AttributeModType.addition: {
                                addMods.Add(new Components.AttributeMod<TAtt, Components.AttributeModTypeAdd>(modConstructor));
                                break;
                            }
                            case AttributeModType.multiplier: {
                                mulMods.Add(new Components.AttributeMod<TAtt, Components.AttributeModTypeMul>(modConstructor));
                                break;
                            }
                            case AttributeModType.exponent: {
                                expMods.Add(new Components.AttributeMod<TAtt, Components.AttributeModTypeExp>(modConstructor));
                                break;
                            }
                        }
                    }
                    new AttributeModSet<TAtt>(addMods, mulMods, expMods).CalculateModValue(constructor.baseValue, out double modValue);
                    ecb.AddComponent(index, entity, new TModAtt{ value = modValue, maxValue = modValue });
                    break;
                }
                case AttributeManagerRequestType.remove: {
                    ecb.RemoveComponent<TAtt>(index, entity);
                    ecb.RemoveComponent<TModAtt>(index, entity);
                    ecb.RemoveComponent<DynamicBuffer<Components.AttributeMod<TAtt, Components.AttributeModTypeAdd>>>(index, entity);
                    ecb.RemoveComponent<DynamicBuffer<Components.AttributeMod<TAtt, Components.AttributeModTypeMul>>>(index, entity);
                    ecb.RemoveComponent<DynamicBuffer<Components.AttributeMod<TAtt, Components.AttributeModTypeExp>>>(index, entity);
                    break;
                }
                case AttributeManagerRequestType.edit: {
                    ecb.SetComponent(index, entity, new TAtt() {
                        att = new Components.AttributeInternal(constructor.baseValue, constructor.minModValue,
                            constructor.maxModValue),
                        modsChanged = true
                    });
                    break;
                }
                default: {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    public struct AttributeModManagerRequest {
        private AttributeManagerRequestType requestType;
        private AttributeModRefPointer modRefPointer;
        private AttributeModConstructor constructor; // when removing you only need to set "attType", "modType" and "id"
        public AttributeType attType => constructor.attType;
        public AttributeModType modType => constructor.modType;
        
        public static AttributeModManagerRequest AddTemplate(
            in AttributeModConstructor constructor) {
            new AttributeModRefPointer().Allocate(out var modRefPointer);
            return new AttributeModManagerRequest {
                modRefPointer = modRefPointer,
                requestType = AttributeManagerRequestType.add,
                constructor = constructor
            };
        }

        public static AttributeModManagerRequest EditTemplate(
            in AttributeModRefPointer modRef,
            in AttributeModConstructor constructor) => new() {
            modRefPointer = modRef,
            requestType = AttributeManagerRequestType.edit,
            constructor = constructor
        };

        public static AttributeModManagerRequest RemoveTemplate(
            in AttributeModRefPointer modRef) => new() {
            modRefPointer = modRef,
            requestType = AttributeManagerRequestType.remove
        };

        public void ProcessByModType<TAtt>(
            ref ComponentLookup<TAtt> attLookup,
            ref BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeAdd>> addLookup,
            ref BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeMul>> mulLookup,
            ref BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeExp>> expLookup) 
            where TAtt : unmanaged, Components.IAttribute {

            switch (constructor.modType) {
                case AttributeModType.addition: {
                    Process(ref addLookup, ref attLookup);
                    break;
                }
                case AttributeModType.multiplier: {
                    Process(ref mulLookup, ref attLookup);
                    break;
                }
                case AttributeModType.exponent: {
                    Process(ref expLookup, ref attLookup);
                    break;
                }
            }
        }
        
        public void Process<TAtt, TModType> (
            ref BufferLookup<Components.AttributeMod<TAtt, TModType>> modLookup,
            ref ComponentLookup<TAtt> attLookup) 
            where TAtt : unmanaged, Components.IAttribute
            where TModType : unmanaged, Components.IAttributeModType {

            modRefPointer.Get(out var modRef);
            attLookup.GetRefRW(modRef.entity).ValueRW.modsChanged = true;
            switch (requestType) {
                case AttributeManagerRequestType.add: {
                    modRef.index = modLookup[modRef.entity].Add(new Components.AttributeMod<TAtt, TModType>(constructor));
                    modRefPointer.Set(modRef);
                    break;
                }
                case AttributeManagerRequestType.remove: {
                    modLookup[modRef.entity].ElementAt(modRef.index).data = Components.AttributeModInternal.Null;
                    modRefPointer.Dispose();
                    break;
                }
                case AttributeManagerRequestType.edit: {
                    var mod = modLookup[modRef.entity].ElementAt(modRef.index);
                    mod.data.value = constructor.value;
                    mod.data.lifeTime = constructor.lifeTime;
                    mod.data.expires = constructor.expires;
                    modLookup[modRef.entity].ElementAt(modRef.index) = mod;
                    break;
                }
                default: {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    namespace Systems {
        [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
        [UpdateAfter(typeof(Spawner))]
        [UpdateBefore(typeof(BeginSimulationEntityCommandBufferSystem))]
        public partial class AttributeUpdateSystemGroup : ComponentSystemGroup {
            [Preserve]
            public AttributeUpdateSystemGroup() {
                
            }
        }

        [UpdateInGroup(typeof(AttributeUpdateSystemGroup), OrderFirst = true)]
        [UpdateBefore(typeof(AttributeManagerQueuesFilterSystem))]
        // [UpdateAfter(typeof(Spawner))]
        // [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
        [BurstCompile]
        public partial struct AttributeQueueSystem<TAtt, TModAtt> : ISystem, ISystemStartStop
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

                NativeQueue<AttributeRequest> attributeQueue =
                    SystemAPI.GetComponentRO<Components.AttributeQueue>(root).ValueRO.q;
                if (attributeQueue.IsEmpty()) {
                    return;
                }
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
                recipientModAtt.ValueRW.value = math.clamp(recipientModAtt.ValueRW.value + 
                    request.outgoingAttribute switch {
                       AttributeType.none => request.isAddition
                           ? 1.0d
                           : -1.0d * request.GetOverrideValue(),
                       _ => request.isAddition
                           ? 1.0d
                           : -1.0d * modAttLookup[request.senderEntity].value
                   }, recipientAtt.att.minModValue, recipientAtt.att.maxModValue);
            }
        }
        

        
        [UpdateInGroup(typeof(AttributeUpdateSystemGroup))]
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
                state.EntityManager.AddComponentData(
                    root,
                    new Components.AttributeManagerQueue() {
                        q = new NativeQueue<AttributeManagerRequest>(Allocator.Persistent)
                    }
                );
                state.EntityManager.AddComponentData(
                    root,
                    new Components.AttributeModManagerQueue() {
                        q = new NativeQueue<AttributeModManagerRequest>(Allocator.Persistent)
                    }
                );
                state.EntityManager.AddComponentData(
                    root,
                    new Components.AttributeManagerRequests<TAtt>()
                );
                state.EntityManager.AddComponentData(
                    root,
                    new Components.AttributeModManagerRequests<TAtt>()
                );
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
                new AttributeUpdateJob<TAtt, TModAtt>() {
                    attLookup = attLookup,
                    modAttLookup = modAttLookup,
                    addModsLookup = addModsLookup,
                    mulModsLookup = mulModsLookup,
                    expModsLookup = expModsLookup,
                    entArr = entArr,
                }.Run(entArr.Length);
                entArr.Dispose();

                AttributeManagerRequests<TAtt> attributeManagerRequests = 
                    SystemAPI.GetComponentRO<Components.AttributeManagerRequests<TAtt>>(root).ValueRO;

                new AttributeManagerRequestJob<TAtt, TModAtt> {
                    requests = attributeManagerRequests.data.AsReader(),
                    ecb = ecb
                }.Run(attributeManagerRequests.bufferCount);
                attributeManagerRequests.data.Dispose();
                
                /////////////////////////////////////////////////////////////////////////////////////////////////
                
                var attributeModManagerRequests =
                    SystemAPI.GetComponentRO<Components.AttributeModManagerRequests<TAtt>>(root).ValueRO;
                
                if (attributeModManagerRequests.bufferCount == 0) { return; }
                
                OpenStream(attributeModManagerRequests.bufferCount, out var addModRequests, out var addModRequestsWriter);
                OpenStream(attributeModManagerRequests.bufferCount, out var mulModRequests, out var mulModRequestsWriter);
                OpenStream(attributeModManagerRequests.bufferCount, out var expModRequests, out var expModRequestsWriter);

                var filterJob = new AttributeModManagerRequestJob<TAtt>() {
                    requests = attributeModManagerRequests.data.AsReader(),
                    addModRequests = addModRequests.AsWriter(),
                    mulModRequests = mulModRequests.AsWriter(),
                    expModRequests = expModRequests.AsWriter()
                };
                filterJob.Run(attributeModManagerRequests.bufferCount);
                attributeModManagerRequests.data.Dispose();
                
                CloseStream(ref addModRequests, ref addModRequestsWriter, filterJob.addModRequestsCount, addModsLookup);
                CloseStream(ref mulModRequests, ref mulModRequestsWriter, filterJob.mulModRequestsCount, mulModsLookup);
                CloseStream(ref expModRequests, ref expModRequestsWriter, filterJob.expModRequestsCount, expModsLookup);
            }
            
            public void OpenStream(in int bufferCount, out NativeStream stream, out NativeStream.Writer writer) {
                stream = new NativeStream(bufferCount, Allocator.TempJob);
                writer = stream.AsWriter();
                writer.BeginForEachIndex(0);
            }

            public void CloseStream<TModType>(
                ref NativeStream stream,
                ref NativeStream.Writer writer,
                in int bufferCount,
                in BufferLookup<Components.AttributeMod<TAtt, TModType>> modsLookup)
                where TModType : unmanaged, IAttributeModType {
                
                writer.EndForEachIndex();
                if (bufferCount == 0) { return; }
                new AttributeModManagerRequestPerBufferJob<TAtt, TModType>() {
                    requests = stream.AsReader(),
                    modLookup = modsLookup,
                    attLookup = attLookup
                }.Run(bufferCount);
                stream.Dispose();
            }
            public void OnStopRunning(ref SystemState state) {

            }
        }

        // [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
        // [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
        // [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
        // public partial struct AttributeUpdateSystem<TAtt, TModAtt> : ISystem, ISystemStartStop
        //     where TAtt : unmanaged, Components.IAttribute
        //     where TModAtt : unmanaged, Components.IAttributeModValue {
        //
        //     public Entity root;
        //     public ComponentLookup<TAtt> attLookup;
        //     public ComponentLookup<TModAtt> modAttLookup;
        //     public BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeAdd>> addModsLookup;
        //     public BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeMul>> mulModsLookup;
        //     public BufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeExp>> expModsLookup;
        //     public EntityQuery entitiesWithAttributes;
        //
        //     public void OnCreate(ref SystemState state) {
        //         state.RequireForUpdate<Tags.Root>();
        //         state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        //         attLookup = state.GetComponentLookup<TAtt>();
        //         modAttLookup = state.GetComponentLookup<TModAtt>();
        //         addModsLookup = state.GetBufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeAdd>>();
        //         mulModsLookup = state.GetBufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeMul>>();
        //         expModsLookup = state.GetBufferLookup<Components.AttributeMod<TAtt, Components.AttributeModTypeExp>>();
        //
        //         entitiesWithAttributes = state.GetEntityQuery(new EntityQueryDesc {
        //             All = new[] {
        //                 ComponentType.ReadOnly<TAtt>(),
        //                 ComponentType.ReadWrite<TModAtt>(),
        //             }
        //         });
        //     }
        //
        //     public void OnStartRunning(ref SystemState state) {
        //         root = SystemAPI.GetSingletonEntity<Tags.Root>();
        //     }
        //
        //     public void OnUpdate(ref SystemState state) {
        //         attLookup.Update(ref state);
        //         modAttLookup.Update(ref state);
        //         addModsLookup.Update(ref state);
        //         mulModsLookup.Update(ref state);
        //         expModsLookup.Update(ref state);
        //
        //         var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
        //             .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        //
        //         NativeArray<Entity> entArr = entitiesWithAttributes.ToEntityArray(Allocator.TempJob);
        //         var attributeUpdateHandle = new AttributeUpdateJob<TAtt, TModAtt>() {
        //             attLookup = attLookup,
        //             modAttLookup = modAttLookup,
        //             addModsLookup = addModsLookup,
        //             mulModsLookup = mulModsLookup,
        //             expModsLookup = expModsLookup,
        //             entArr = entArr,
        //         }.Schedule(entArr.Length, 32);
        //         entArr.Dispose();
        //
        //         NativeQueue<AttributeManagerRequest<TAtt, TModAtt>> attributeManagerRequestsQueue =
        //             SystemAPI.GetComponentRO<Components.AttributeManagerQueue<TAtt, TModAtt>>(root).ValueRO.q;
        //         if (attributeManagerRequestsQueue.IsEmpty()) { return; }
        //
        //         NativeStream attributeManagerRequests =
        //             new NativeStream(attributeManagerRequestsQueue.Count, Allocator.TempJob);
        //         var attributeManagerRequestsWriter = attributeManagerRequests.AsWriter();
        //         attributeManagerRequestsWriter.BeginForEachIndex(0);
        //         for (int i = 0; i < attributeManagerRequestsQueue.Count; i++) {
        //             attributeManagerRequestsWriter.Write(attributeManagerRequestsQueue.Dequeue());
        //         }
        //
        //         attributeManagerRequestsWriter.EndForEachIndex();
        //
        //         var attributeManagerHandle = new AttributeManagerRequestJob<TAtt, TModAtt>() {
        //             requests = attributeManagerRequests.AsReader(),
        //             ecb = ecb
        //         }.Schedule(attributeManagerRequestsQueue.Count, 32, attributeUpdateHandle);
        //
        //         ///////////////////////////////////////////////////////////////////////////////////////
        //
        //         NativeQueue<AttributeModManagerRequest<TAtt>> attributeModManagerRequestsQueue =
        //                 SystemAPI.GetComponentRO<Components.AttributeModManagerQueue<TAtt>>(root).ValueRO.q;
        //         if (attributeModManagerRequestsQueue.IsEmpty()) { return; }
        //
        //         NativeStream attributeModManagerRequests =
        //             new NativeStream(attributeModManagerRequestsQueue.Count, Allocator.TempJob);
        //         var attributeModManagerRequestsWriter = attributeModManagerRequests.AsWriter();
        //         attributeModManagerRequestsWriter.BeginForEachIndex(0);
        //         for (int i = 0; i < attributeModManagerRequestsQueue.Count; i++) {
        //             attributeModManagerRequestsWriter.Write(attributeModManagerRequestsQueue.Dequeue());
        //         }
        //         attributeModManagerRequestsWriter.EndForEachIndex();
        //         
        //         NativeStream addModRequests = new (attributeManagerRequestsQueue.Count, Allocator.TempJob);
        //         NativeStream mulModRequests = new (attributeManagerRequestsQueue.Count, Allocator.TempJob);
        //         NativeStream expModRequests = new (attributeManagerRequestsQueue.Count, Allocator.TempJob);
        //         
        //         var addModRequestsWriter = addModRequests.AsWriter();
        //         var mulModRequestsWriter = mulModRequests.AsWriter();
        //         var expModRequestsWriter = expModRequests.AsWriter();
        //         
        //         addModRequestsWriter.BeginForEachIndex(0);
        //         mulModRequestsWriter.BeginForEachIndex(0);
        //         expModRequestsWriter.BeginForEachIndex(0);
        //
        //         var attributeModManagerRequestJobHandle = new AttributeModManagerRequestJob<TAtt>() {
        //             requests = attributeModManagerRequests.AsReader(),
        //             addModRequests = addModRequests.AsWriter(),
        //             mulModRequests = mulModRequests.AsWriter(),
        //             expModRequests = expModRequests.AsWriter()
        //         }.Schedule(attributeModManagerRequestsQueue.Count, 32);
        //         
        //         addModRequestsWriter.EndForEachIndex();
        //         mulModRequestsWriter.EndForEachIndex();
        //         expModRequestsWriter.EndForEachIndex();
        //         
        //         new AttributeModManagerRequestPerBufferJob<TAtt, Components.AttributeModTypeAdd>() {
        //             requests = addModRequests.AsReader(),
        //             modLookup = addModsLookup,
        //             attLookup = attLookup
        //         }.Schedule(addModRequests.Count(), attributeModManagerRequestJobHandle);
        //         new AttributeModManagerRequestPerBufferJob<TAtt, Components.AttributeModTypeMul>() {
        //             requests = mulModRequests.AsReader(),
        //             modLookup = mulModsLookup,
        //             attLookup = attLookup
        //         }.Schedule(mulModRequests.Count(), attributeModManagerRequestJobHandle);
        //         new AttributeModManagerRequestPerBufferJob<TAtt, Components.AttributeModTypeExp>() {
        //             requests = expModRequests.AsReader(),
        //             modLookup = expModsLookup,
        //             attLookup = attLookup
        //         }.Schedule(expModRequests.Count(), attributeModManagerRequestJobHandle);
        //     }
        //
        //     public void OnStopRunning(ref SystemState state) {
        //
        //     }
        // }

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
                AttributeModSet<TAtt> mods = new(addMods, mulMods, expMods);

                if (att.ValueRO.modsChanged) {
                    mods.CalculateModValue(att.ValueRW.att.baseValue, out double modValue);
                    RefRW<TModAtt> modAtt = modAttLookup.GetRefRW(entity);
                    modAtt.ValueRW.maxValue =
                        math.clamp(modValue, att.ValueRO.att.minModValue, att.ValueRO.att.maxModValue);
                    modAtt.ValueRW.value =
                        math.clamp(modAtt.ValueRO.value, double.MinValue, modAtt.ValueRO.maxValue);
                }
            }
        }

        public struct AttributeManagerRequestJob<TAtt, TModAtt> : IJobParallelFor
            where TAtt : unmanaged, Components.IAttribute
            where TModAtt : unmanaged, Components.IAttributeModValue {
            public EntityCommandBuffer.ParallelWriter ecb;
            public NativeStream.Reader requests;
            public void Execute(int index) {
                requests.Read<AttributeManagerRequest>().Process<TAtt, TModAtt>(ref ecb, index);
            }
        }

        public struct AttributeModManagerRequestJob<TAtt> : IJobParallelFor
            where TAtt : unmanaged, Components.IAttribute {

            public NativeStream.Reader requests;
            public NativeStream.Writer addModRequests;
            public int addModRequestsCount;
            public NativeStream.Writer mulModRequests;
            public int mulModRequestsCount;
            public NativeStream.Writer expModRequests;
            public int expModRequestsCount;
            public void Execute(int index) {
                var request = requests.Read<AttributeModManagerRequest>();
                switch (request.modType) {
                    case AttributeModType.addition: {
                        addModRequests.Write(request);
                        addModRequestsCount++;
                        break;
                    }
                    case AttributeModType.multiplier: {
                        mulModRequests.Write(request);
                        mulModRequestsCount++;
                        break;
                    }
                    case AttributeModType.exponent: {
                        expModRequests.Write(request);
                        expModRequestsCount++;
                        break;
                    } 
                }
            }
        }

        public struct AttributeModManagerRequestPerBufferJob<TAtt, TModType> : IJobFor
            where TAtt : unmanaged, Components.IAttribute
            where TModType : unmanaged, Components.IAttributeModType {

            public ComponentLookup<TAtt> attLookup;
            public BufferLookup<Components.AttributeMod<TAtt, TModType>> modLookup;
            public NativeStream.Reader requests;
            
            public void Execute(int index) {
                // var request = requests.Read<AttributeModManagerRequest<TAtt>>();
                requests.Read<AttributeModManagerRequest>().Process(ref modLookup, ref attLookup);
                //request.modRefPointer.Get(out var modRef);
                //
                //
                // switch (request.requestType) {
                //     case AttributeManagerRequestType.add: {
                //         Extensions.AddAttributeMod(modLookup, request, attLookup.GetRefRW(modRef.entity));
                //         break;
                //     }
                //     case AttributeManagerRequestType.remove: {
                //         Extensions.RemoveAttributeMod(modLookup, request, attLookup.GetRefRW(modRef.entity));
                //         break;
                //     }
                //     case AttributeManagerRequestType.edit: {
                //         Extensions.EditAttributeMod(modLookup, request, attLookup.GetRefRW(modRef.entity));
                //         break;
                //     }
                // }
            }
        } 
    }
}