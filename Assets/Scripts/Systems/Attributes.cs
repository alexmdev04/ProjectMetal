using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Logging;
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
    
    [BurstCompile]
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

        [BurstCompile]
        public void ProcessByType(ref EntityCommandBuffer.ParallelWriter ecb, in int index, bool logging = false) {
            switch (attType) {
                case AttributeType.health: { Process<Components.AttHealthBase, Components.AttHealthModValue>(ref ecb, index, logging); break; }
                case AttributeType.damage: { Process<Components.AttDamageBase, Components.AttDamageModValue>(ref ecb, index, logging); break; }
                case AttributeType.fireRate: { Process<Components.AttFireRateBase, Components.AttFireRateModValue>(ref ecb, index, logging); break; }
                case AttributeType.cooldownRate: { Process<Components.AttCooldownRateBase, Components.AttCooldownRateModValue>(ref ecb, index, logging); break; }
                case AttributeType.accelerationSpeed: { Process<Components.AttAccelerationSpeedBase, Components.AttAccelerationSpeedModValue>(ref ecb, index, logging); break; }
            }
        }
        
        [BurstCompile]
        public void Process<TAtt, TModAtt> (
            ref EntityCommandBuffer.ParallelWriter ecb,
            in int index,
            in bool logging = false) 
            where TAtt : unmanaged, IAttribute 
            where TModAtt : unmanaged, IAttributeModValue {

            if (logging) {
                Extensions.ToString(requestType, out var requestTypeName);
                Extensions.ToString(attType, out var attName);
                Log.Debug("[AttributeManagerRequest] Processing Request: requestType={0}, attributeType={1}, hasMods={2}",
                    requestTypeName, attName, constructor.modConstructors.HasValue);
                //Log.Debug("Att={0}, ModAtt={1}", typeof(TAtt).Name, typeof(TModAtt).Name);
            }
            
            switch (requestType) {
                case AttributeManagerRequestType.add: {
                    ecb.AddComponent(index, entity, new TAtt {
                        att = new AttributeInternal(constructor.baseValue, constructor.minModValue, constructor.maxModValue),
                        modsChanged = true
                    });
                    var addMods = ecb.AddBuffer<AttributeMod<TAtt, AttributeModTypeAdd>>(index, entity);
                    var mulMods = ecb.AddBuffer<AttributeMod<TAtt, AttributeModTypeMul>>(index, entity);
                    var expMods = ecb.AddBuffer<AttributeMod<TAtt, AttributeModTypeExp>>(index, entity);
            
                    if (constructor.modConstructors.HasValue) {
                        foreach (AttributeModConstructor modConstructor in constructor.modConstructors) {
                            // attribute type is already known so its not used
                            switch (modConstructor.modType) {
                                case AttributeModType.addition: {
                                    addMods.Add(new AttributeMod<TAtt, AttributeModTypeAdd>(modConstructor));
                                    break;
                                }
                                case AttributeModType.multiplier: {
                                    mulMods.Add(new AttributeMod<TAtt, AttributeModTypeMul>(modConstructor));
                                    break;
                                }
                                case AttributeModType.exponent: {
                                    expMods.Add(new AttributeMod<TAtt, AttributeModTypeExp>(modConstructor));
                                    break;
                                }
                            }
                        }
                        constructor.modConstructors.Value.Dispose();
                    }
                    new AttributeModSet<TAtt>(addMods, mulMods, expMods).CalculateModValue(constructor.baseValue, out double modValue);
                    ecb.AddComponent(index, entity, new TModAtt{ valueStatic = modValue, value = modValue });
                    break;
                }
                case AttributeManagerRequestType.remove: {
                    ecb.RemoveComponent<TAtt>(index, entity);
                    ecb.RemoveComponent<TModAtt>(index, entity);
                    ecb.RemoveComponent<DynamicBuffer<AttributeMod<TAtt, AttributeModTypeAdd>>>(index, entity);
                    ecb.RemoveComponent<DynamicBuffer<AttributeMod<TAtt, AttributeModTypeMul>>>(index, entity);
                    ecb.RemoveComponent<DynamicBuffer<AttributeMod<TAtt, AttributeModTypeExp>>>(index, entity);
                    break;
                }
                case AttributeManagerRequestType.edit: {
                    ecb.SetComponent(index, entity, new TAtt() {
                        att = new AttributeInternal(constructor.baseValue, constructor.minModValue,
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

    [BurstCompile]
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

        [BurstCompile]
        public void ProcessByModType<TAtt>(
            ref ComponentLookup<TAtt> attLookup,
            ref BufferLookup<AttributeMod<TAtt, AttributeModTypeAdd>> addLookup,
            ref BufferLookup<AttributeMod<TAtt, AttributeModTypeMul>> mulLookup,
            ref BufferLookup<AttributeMod<TAtt, AttributeModTypeExp>> expLookup) 
            where TAtt : unmanaged, IAttribute {

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
        
        [BurstCompile]
        public void Process<TAtt, TModType> (
            ref BufferLookup<AttributeMod<TAtt, TModType>> modLookup,
            ref ComponentLookup<TAtt> attLookup,
            in bool logging = false) 
            where TAtt : unmanaged, IAttribute
            where TModType : unmanaged, IAttributeModType {

            if (logging) {
                Extensions.ToString(requestType, out var requestTypeName);
                Extensions.ToString(attType, out var attName);
                Extensions.ToString(modType, out var modTypeName);
                Log.Debug("[AttributeModManagerRequest] Processing Request: requestType={0}, attributeType={1}, modType={2}",
                    requestTypeName, attName, modTypeName);
            }
            
            modRefPointer.Get(out var modRef);
            attLookup.GetRefRW(modRef.entity).ValueRW.modsChanged = true;
            switch (requestType) {
                case AttributeManagerRequestType.add: {
                    modRef.index = modLookup[modRef.entity].Add(new AttributeMod<TAtt, TModType>(constructor));
                    modRefPointer.Set(modRef);
                    break;
                }
                case AttributeManagerRequestType.remove: {
                    modLookup[modRef.entity].ElementAt(modRef.index).data = AttributeModInternal.Null;
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
        [UpdateAfter(typeof(AttributeQueuesFilterSystem))]
        [BurstCompile]
        // [UpdateAfter(typeof(Spawner))]
        // [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
        public partial struct AttributeQueueSystem<TAtt, TModAtt> : ISystem
            where TAtt : unmanaged, IAttribute
            where TModAtt : unmanaged, IAttributeModValue {
            
            public ComponentLookup<TAtt> attLookup;
            public ComponentLookup<TModAtt> modAttLookup;

            [BurstCompile]
            public void OnCreate(ref SystemState state) {
                state.EntityManager.AddComponentData(state.SystemHandle, new Components.AttributeQueueFiltered<TAtt>() {
                    q = new NativeQueue<AttributeRequest>(Allocator.Persistent)
                });
                state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
                state.RequireForUpdate<Components.AttributeQueueFiltered<TAtt>>();
                state.RequireForUpdate<Components.AttributeData>();
                attLookup = state.GetComponentLookup<TAtt>();
                modAttLookup = state.GetComponentLookup<TModAtt>();
            }

            [BurstCompile]
            public void OnUpdate(ref SystemState state) {
                //Log.Debug("[AttributeQueueSystem<" + typeof(TAtt).Name + ">] Update");
                attLookup.Update(ref state);
                modAttLookup.Update(ref state);

                var attributeRequests = SystemAPI.GetSingleton<Components.AttributeQueueFiltered<TAtt>>();
                int attributeRequestsCount = attributeRequests.q.Count;
                if (attributeRequestsCount == 0) { return; }

                Components.AttributeData attributeData = SystemAPI.GetSingleton<Components.AttributeData>();

                new AttributeQueueJob<TAtt, TModAtt>() {
                    //ecb = ecb,
                    attLookup = attLookup,
                    modAttLookup = modAttLookup,
                    attributeData = attributeData,
                    attributeRequests = attributeRequests.q
                }.Schedule(attributeRequestsCount, state.Dependency);
            }

            [BurstCompile]
            public void OnDestroy(ref SystemState state) {
                SystemAPI.GetComponent<Components.AttributeQueueFiltered<TAtt>>(state.SystemHandle).q.Dispose();
            }
        }

        [BurstCompile]
        public struct AttributeQueueJob<TAtt, TModAtt> : IJobFor
            where TAtt : unmanaged, IAttribute
            where TModAtt : unmanaged, IAttributeModValue {

            //public EntityCommandBuffer.ParallelWriter ecb;
            public Components.AttributeData attributeData;

            [NativeDisableContainerSafetyRestriction]
            public NativeQueue<AttributeRequest> attributeRequests;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<TAtt> attLookup;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<TModAtt> modAttLookup;

            [BurstCompile]
            public void Execute(int index) {
                AttributeRequest request = attributeRequests.Dequeue();
                TAtt recipientAtt = attLookup[request.recipientEntity];
                RefRW<TModAtt> recipientModAtt = modAttLookup.GetRefRW(request.recipientEntity);

                // edits working value and clamps it to the attribute clamp values
                recipientModAtt.ValueRW.valueStatic = math.clamp(recipientModAtt.ValueRW.valueStatic + 
                    request.outgoingAttribute switch {
                       AttributeType.none => request.isAddition
                           ? 1.0d
                           : -1.0d * request.GetOverrideValue(),
                       _ => request.isAddition
                           ? 1.0d
                           : -1.0d * modAttLookup[request.senderEntity].valueStatic
                   }, recipientAtt.att.minModValue, recipientAtt.att.maxModValue);
            }
        }
        

        
        [UpdateInGroup(typeof(AttributeUpdateSystemGroup))]
        [BurstCompile]
        public partial struct AttributeUpdateSystem<TAtt, TModAtt> : ISystem
            where TAtt : unmanaged, IAttribute
            where TModAtt : unmanaged, IAttributeModValue {

            //public Entity root;
            public ComponentLookup<TAtt> attLookup;
            public ComponentLookup<TModAtt> modAttLookup;
            public BufferLookup<AttributeMod<TAtt, AttributeModTypeAdd>> addModsLookup;
            public BufferLookup<AttributeMod<TAtt, AttributeModTypeMul>> mulModsLookup;
            public BufferLookup<AttributeMod<TAtt, AttributeModTypeExp>> expModsLookup;
            public EntityQuery entitiesWithAttributes;

            [BurstDiscard]
            public void OnCreate(ref SystemState state) {
                //state.RequireForUpdate<Components.AttributeManagerQueueFiltered<TAtt>>();
                //state.RequireForUpdate<Components.AttributeModManagerQueueFiltered<TAtt>>();          
                state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
                attLookup = state.GetComponentLookup<TAtt>();
                modAttLookup = state.GetComponentLookup<TModAtt>();
                addModsLookup = state.GetBufferLookup<AttributeMod<TAtt, AttributeModTypeAdd>>();
                mulModsLookup = state.GetBufferLookup<AttributeMod<TAtt, AttributeModTypeMul>>();
                expModsLookup = state.GetBufferLookup<AttributeMod<TAtt, AttributeModTypeExp>>();

                entitiesWithAttributes = state.GetEntityQuery(new EntityQueryDesc {
                    All = new[] {
                        ComponentType.ReadOnly<TAtt>(),
                        ComponentType.ReadWrite<TModAtt>(),
                    }
                });
                state.EntityManager.AddComponentData(
                    state.SystemHandle,
                    new Components.AttributeManagerQueueFiltered<TAtt>() {
                        q = new NativeQueue<AttributeManagerRequest>(Allocator.Persistent)
                    }
                );
                state.EntityManager.AddComponentData(
                    state.SystemHandle,
                    new Components.AttributeModManagerQueueFiltered<TAtt>(){
                        q = new NativeQueue<AttributeModManagerRequest>(Allocator.Persistent)
                    }
                );
            }

            [BurstCompile]
            public void OnUpdate(ref SystemState state) {
                //Log.Debug("[AttributeUpdateSystem<" + typeof(TAtt).Name + ">] Update");
                attLookup.Update(ref state);
                modAttLookup.Update(ref state);
                addModsLookup.Update(ref state);
                mulModsLookup.Update(ref state);
                expModsLookup.Update(ref state);

                var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

                NativeArray<Entity> entArr = entitiesWithAttributes.ToEntityArray(Allocator.TempJob);
                new AttributeUpdateJob<TAtt, TModAtt>() {
                    deltaTime = SystemAPI.Time.DeltaTime,
                    attLookup = attLookup,
                    modAttLookup = modAttLookup,
                    addModsLookup = addModsLookup,
                    mulModsLookup = mulModsLookup,
                    expModsLookup = expModsLookup,
                    entArr = entArr,
                }.Run(entArr.Length);
                entArr.Dispose();

                Components.AttributeManagerQueueFiltered<TAtt> attributeManagerQueueFiltered = 
                    SystemAPI.GetComponent<Components.AttributeManagerQueueFiltered<TAtt>>(state.SystemHandle);

                NativeArray<AttributeManagerRequest> requests = attributeManagerQueueFiltered.q.ToArray(Allocator.TempJob);
                attributeManagerQueueFiltered.q.Clear();
                
                new AttributeManagerQueueJob<TAtt, TModAtt> {
                    requests = requests,
                    ecb = ecb
                }.Run(requests.Length);
                requests.Dispose();
                
                /////////////////////////////////////////////////////////////////////////////////////////////////
                
                var attributeModManagerQueueFiltered =
                    SystemAPI.GetComponent<Components.AttributeModManagerQueueFiltered<TAtt>>(state.SystemHandle);
                
                if (attributeModManagerQueueFiltered.q.Count == 0) { return; }

                NativeQueue<AttributeModManagerRequest> addModRequests = new (Allocator.TempJob);
                NativeQueue<AttributeModManagerRequest> mulModRequests = new (Allocator.TempJob);
                NativeQueue<AttributeModManagerRequest> expModRequests = new (Allocator.TempJob);

                var modRequests = attributeModManagerQueueFiltered.q.ToArray(Allocator.TempJob);
                var filterJob = new AttributeModManagerQueueFilterJob<TAtt>() {
                    requests = modRequests,
                    addModRequests = addModRequests.AsParallelWriter(),
                    mulModRequests = mulModRequests.AsParallelWriter(),
                    expModRequests = expModRequests.AsParallelWriter()
                };
                filterJob.Run(modRequests.Length);
                modRequests.Dispose();

                addModRequests.Dispose(new AttributeModManagerQueuePerBufferJob<TAtt, AttributeModTypeAdd> {
                    requests = addModRequests,
                    attLookup = attLookup,
                    modLookup = addModsLookup
                }.Schedule(filterJob.addModRequestsCount, state.Dependency));
                mulModRequests.Dispose(new AttributeModManagerQueuePerBufferJob<TAtt, AttributeModTypeMul> {
                    requests = mulModRequests,
                    attLookup = attLookup,
                    modLookup = mulModsLookup
                }.Schedule(filterJob.mulModRequestsCount, state.Dependency));
                expModRequests.Dispose(new AttributeModManagerQueuePerBufferJob<TAtt, AttributeModTypeExp> {
                    requests = expModRequests,
                    attLookup = attLookup,
                    modLookup = expModsLookup
                }.Schedule(filterJob.expModRequestsCount, state.Dependency));
            }
            
            [BurstCompile]
            public void OnDestroy(ref SystemState state) {
                SystemAPI.GetComponent<Components.AttributeManagerQueueFiltered<TAtt>>(state.SystemHandle).q.Dispose();
                SystemAPI.GetComponent<Components.AttributeModManagerQueueFiltered<TAtt>>(state.SystemHandle).q.Dispose();
            }
        }

        [BurstCompile]
        public struct AttributeUpdateJob<TAtt, TModAtt> : IJobParallelFor
            where TAtt : unmanaged, IAttribute
            where TModAtt : unmanaged, IAttributeModValue {

            public float deltaTime;

            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<TAtt> attLookup;

            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<TModAtt> modAttLookup;

            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public BufferLookup<AttributeMod<TAtt, AttributeModTypeAdd>> addModsLookup;

            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public BufferLookup<AttributeMod<TAtt, AttributeModTypeMul>> mulModsLookup;

            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public BufferLookup<AttributeMod<TAtt, AttributeModTypeExp>> expModsLookup;

            [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
            public NativeArray<Entity> entArr;

            [BurstCompile]
            public void Execute(int index) {
                Entity entity = entArr[index];
                RefRW<TAtt> att = attLookup.GetRefRW(entity);
                //TModAtt modAtt = modAttLookup[entity];
                DynamicBuffer<AttributeMod<TAtt, AttributeModTypeAdd>> addMods = addModsLookup[entity];
                DynamicBuffer<AttributeMod<TAtt, AttributeModTypeMul>> mulMods = mulModsLookup[entity];
                DynamicBuffer<AttributeMod<TAtt, AttributeModTypeExp>> expMods = expModsLookup[entity];
                AttributeModSet<TAtt> mods = new(addMods, mulMods, expMods);

                mods.Tick(deltaTime, att);
                
                if (att.ValueRO.modsChanged) {
                    mods.CalculateModValue(att.ValueRW.att.baseValue, out double modValue);
                    RefRW<TModAtt> modAtt = modAttLookup.GetRefRW(entity);
                    modAtt.ValueRW.value =
                        math.clamp(modValue, att.ValueRO.att.minModValue, att.ValueRO.att.maxModValue);
                    modAtt.ValueRW.valueStatic =
                        math.clamp(modAtt.ValueRO.valueStatic, double.MinValue, modAtt.ValueRO.value);
                    att.ValueRW.modsChanged = false;
                }
            }
        }

        [BurstCompile]
        public struct AttributeManagerQueueJob<TAtt, TModAtt> : IJobParallelFor
            where TAtt : unmanaged, IAttribute
            where TModAtt : unmanaged, IAttributeModValue {
            public EntityCommandBuffer.ParallelWriter ecb;
            [NativeDisableContainerSafetyRestriction] [NativeDisableParallelForRestriction]
            public NativeArray<AttributeManagerRequest> requests;
            [BurstCompile]
            public void Execute(int index) {
                requests[index].Process<TAtt, TModAtt>(ref ecb, index);
            }
        }

        [BurstCompile]
        public struct AttributeModManagerQueueFilterJob<TAtt> : IJobParallelFor
            where TAtt : unmanaged, IAttribute {

            public NativeArray<AttributeModManagerRequest> requests;
            public NativeQueue<AttributeModManagerRequest>.ParallelWriter addModRequests;
            public int addModRequestsCount;
            public NativeQueue<AttributeModManagerRequest>.ParallelWriter mulModRequests;
            public int mulModRequestsCount;
            public NativeQueue<AttributeModManagerRequest>.ParallelWriter expModRequests;
            public int expModRequestsCount;
            [BurstCompile]
            public void Execute(int index) {
                var request = requests[index];
                switch (request.modType) {
                    case AttributeModType.addition: {
                        addModRequests.Enqueue(request);
                        addModRequestsCount++;
                        break;
                    }
                    case AttributeModType.multiplier: {
                        mulModRequests.Enqueue(request);
                        mulModRequestsCount++;
                        break;
                    }
                    case AttributeModType.exponent: {
                        expModRequests.Enqueue(request);
                        expModRequestsCount++;
                        break;
                    } 
                }
            }
        }

        [BurstCompile]
        public struct AttributeModManagerQueuePerBufferJob<TAtt, TModType> : IJobFor
            where TAtt : unmanaged, IAttribute
            where TModType : unmanaged, IAttributeModType {

            public ComponentLookup<TAtt> attLookup;
            public BufferLookup<AttributeMod<TAtt, TModType>> modLookup;
            public NativeQueue<AttributeModManagerRequest> requests;
            
            [BurstCompile]
            public void Execute(int index) {
                requests.Dequeue().Process(ref modLookup, ref attLookup);
            }
        } 
    }
}