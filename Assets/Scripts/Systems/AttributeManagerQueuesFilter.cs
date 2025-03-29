using System;
using Metal.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Metal.Systems {
    [UpdateInGroup(typeof(AttributeUpdateSystemGroup), OrderFirst = true)]
    public partial struct AttributeManagerQueuesFilterSystem : ISystem, ISystemStartStop {
        public Entity root;

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Tags.Root>();
            state.RequireForUpdate<AttributeManagerQueue>();
            state.RequireForUpdate<AttributeModManagerQueue>();
            state.RequireForUpdate<AttributeManagerRequests<AttHealthBase>>();
            state.RequireForUpdate<AttributeModManagerRequests<AttHealthBase>>();            
            state.RequireForUpdate<AttributeManagerRequests<AttDamageBase>>();
            state.RequireForUpdate<AttributeModManagerRequests<AttDamageBase>>();            
            state.RequireForUpdate<AttributeManagerRequests<AttFireRateBase>>();
            state.RequireForUpdate<AttributeModManagerRequests<AttFireRateBase>>();            
            state.RequireForUpdate<AttributeManagerRequests<AttCooldownRateBase>>();
            state.RequireForUpdate<AttributeModManagerRequests<AttCooldownRateBase>>();            
            state.RequireForUpdate<AttributeManagerRequests<AttAccelerationSpeedBase>>();
            state.RequireForUpdate<AttributeModManagerRequests<AttAccelerationSpeedBase>>();            
            state.RequireForUpdate<AttributeManagerRequests<AttTractionBase>>();
            state.RequireForUpdate<AttributeModManagerRequests<AttTractionBase>>();
        }

        public void OnStartRunning(ref SystemState state) {
            root = SystemAPI.GetSingletonEntity<Tags.Root>();
        }

        public void OnUpdate(ref SystemState state) {
            NativeQueue<AttributeManagerRequest> attributeManagerRequestsQueue =
                SystemAPI.GetComponentRO<Components.AttributeManagerQueue>(root).ValueRO.q;
            NativeArray<AttributeManagerRequest> attributeManagerRequests =
                attributeManagerRequestsQueue.ToArray(Allocator.TempJob);
            if (attributeManagerRequests.Length == 0) {
                attributeManagerRequests.Dispose();
                return;
            }
            attributeManagerRequestsQueue.Clear();
            OpenStream(attributeManagerRequests.Length, out var healthRequests);
            OpenStream(attributeManagerRequests.Length, out var damageRequests);
            OpenStream(attributeManagerRequests.Length, out var fireRateRequests);
            OpenStream(attributeManagerRequests.Length, out var cooldownRateRequests);
            OpenStream(attributeManagerRequests.Length, out var accelerationSpeedRequests);
            OpenStream(attributeManagerRequests.Length, out var tractionRequests);
            var filterJob = new AttributeManagerQueueFilterJob() {
                requests = attributeManagerRequests,
                healthRequests = healthRequests,
                damageRequests = damageRequests,
                fireRateRequests = fireRateRequests,
                cooldownRateRequests = cooldownRateRequests,
                accelerationSpeedRequests = accelerationSpeedRequests,
                tractionRequests = tractionRequests,
            };
            filterJob.Run(attributeManagerRequests.Length);
            //attributeManagerRequests.Dispose();
            
            var healthRequestsComponent = SystemAPI.GetComponentRW<AttributeManagerRequests<AttHealthBase>>(root);
            healthRequestsComponent.ValueRW.data = healthRequests;
            healthRequestsComponent.ValueRW.bufferCount = filterJob.healthRequestsCount;
            
            var damageRequestsComponent = SystemAPI.GetComponentRW<AttributeManagerRequests<AttDamageBase>>(root);
            damageRequestsComponent.ValueRW.data = damageRequests;
            damageRequestsComponent.ValueRW.bufferCount = filterJob.damageRequestsCount;
            
            var fireRateRequestsComponent = SystemAPI.GetComponentRW<AttributeManagerRequests<AttFireRateBase>>(root);
            fireRateRequestsComponent.ValueRW.data = fireRateRequests;
            fireRateRequestsComponent.ValueRW.bufferCount = filterJob.fireRateRequestsCount;
            
            var cooldownRateRequestsComponent = SystemAPI.GetComponentRW<AttributeManagerRequests<AttCooldownRateBase>>(root);
            cooldownRateRequestsComponent.ValueRW.data = cooldownRateRequests;
            cooldownRateRequestsComponent.ValueRW.bufferCount = filterJob.cooldownRateRequestsCount;
            
            var accelerationSpeedRequestsComponent = SystemAPI.GetComponentRW<AttributeManagerRequests<AttAccelerationSpeedBase>>(root);
            accelerationSpeedRequestsComponent.ValueRW.data = accelerationSpeedRequests;
            accelerationSpeedRequestsComponent.ValueRW.bufferCount = filterJob.accelerationSpeedRequestsCount;
            
            var tractionRequestsComponent = SystemAPI.GetComponentRW<AttributeManagerRequests<AttTractionBase>>(root);
            tractionRequestsComponent.ValueRW.data = tractionRequests;
            tractionRequestsComponent.ValueRW.bufferCount = filterJob.tractionRequestsCount;

            NativeQueue<AttributeModManagerRequest> attributeModManagerRequestsQueue =
                SystemAPI.GetComponentRO<Components.AttributeModManagerQueue>(root).ValueRO.q;
            NativeArray<AttributeModManagerRequest> attributeModManagerRequests =
                attributeModManagerRequestsQueue.ToArray(Allocator.TempJob);
            if (attributeModManagerRequests.Length == 0) {
                attributeModManagerRequests.Dispose();
                return;
            }
            attributeModManagerRequestsQueue.Clear();
            OpenStream(attributeModManagerRequests.Length, out var healthModRequests);
            OpenStream(attributeModManagerRequests.Length, out var damageModRequests);
            OpenStream(attributeModManagerRequests.Length, out var fireRateModRequests);
            OpenStream(attributeModManagerRequests.Length, out var cooldownRateModRequests);
            OpenStream(attributeModManagerRequests.Length, out var accelerationSpeedModRequests);
            OpenStream(attributeModManagerRequests.Length, out var tractionModRequests);
            var modFilterJob = new AttributeModManagerQueueFilterJob() {
                requests = attributeModManagerRequests,
                healthModRequests = healthModRequests,
                damageModRequests = damageModRequests,
                fireRateModRequests = fireRateModRequests,
                cooldownRateModRequests = cooldownRateModRequests,
                accelerationSpeedModRequests = accelerationSpeedModRequests,
                tractionModRequests = tractionModRequests
            };
            modFilterJob.Run(attributeModManagerRequests.Length);
            //attributeModManagerRequests.Dispose();
            
            var healthModRequestsComponent = SystemAPI.GetComponentRW<AttributeModManagerRequests<AttHealthBase>>(root);
            healthModRequestsComponent.ValueRW.data = healthModRequests;
            healthModRequestsComponent.ValueRW.bufferCount = modFilterJob.healthModRequestsCount;
            
            var damageModRequestsComponent = SystemAPI.GetComponentRW<AttributeModManagerRequests<AttDamageBase>>(root);
            damageModRequestsComponent.ValueRW.data = damageModRequests;
            damageModRequestsComponent.ValueRW.bufferCount = modFilterJob.damageModRequestsCount;
            
            var fireRateModRequestsComponent = SystemAPI.GetComponentRW<AttributeModManagerRequests<AttFireRateBase>>(root);
            fireRateModRequestsComponent.ValueRW.data = fireRateModRequests;
            fireRateModRequestsComponent.ValueRW.bufferCount = modFilterJob.fireRateModRequestsCount;
            
            var cooldownRateModRequestsComponent = SystemAPI.GetComponentRW<AttributeModManagerRequests<AttCooldownRateBase>>(root);
            cooldownRateModRequestsComponent.ValueRW.data = cooldownRateModRequests;
            cooldownRateModRequestsComponent.ValueRW.bufferCount = modFilterJob.cooldownRateModRequestsCount;
            
            var accelerationSpeedModRequestsComponent = SystemAPI.GetComponentRW<AttributeModManagerRequests<AttAccelerationSpeedBase>>(root);
            accelerationSpeedModRequestsComponent.ValueRW.data = accelerationSpeedModRequests;
            accelerationSpeedModRequestsComponent.ValueRW.bufferCount = modFilterJob.accelerationSpeedModRequestsCount;
            
            var tractionModRequestsComponent = SystemAPI.GetComponentRW<AttributeModManagerRequests<AttTractionBase>>(root);
            tractionModRequestsComponent.ValueRW.data = tractionModRequests;
            tractionModRequestsComponent.ValueRW.bufferCount = modFilterJob.tractionModRequestsCount;
        }

        public void OpenStream(in int length, out NativeStream stream) {
            stream = new NativeStream(length, Allocator.TempJob);
        }

        // public void CloseStream<TRequests>(in int bufferCount, ref NativeStream stream, ref NativeStream.Writer writer, ref SystemState state)
        //     where TRequests : unmanaged, IAttributeRequests, IComponentData {
        //     writer.EndForEachIndex();
        //     RefRW<TRequests> comp = SystemAPI.GetComponentRW<TRequests>(root);
        //     comp.ValueRW.data = stream;
        //     comp.ValueRW.bufferCount = bufferCount;
        // }
        //
        // public void CloseModStream<TRequests>(in int bufferCount, ref NativeStream stream, ref NativeStream.Writer writer, ref SystemState state)
        //     where TRequests : unmanaged, IAttributeRequests, IComponentData {
        //     writer.EndForEachIndex();
        //     RefRW<TRequests> comp =
        //         SystemAPI.GetComponentRW<TRequests>(root);
        //     comp.ValueRW.data = stream;
        //     comp.ValueRW.bufferCount = bufferCount;
        // }

        public void OnStopRunning(ref SystemState state) {

        }
    }

    #region Request Filter Jobs

    public struct AttributeManagerQueueFilterJob : IJobParallelFor {
        [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
        public NativeArray<AttributeManagerRequest> requests;
        [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
        public NativeStream healthRequests;
        public int healthRequestsCount;
        [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
        public NativeStream damageRequests;
        public int damageRequestsCount;
        [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
        public NativeStream fireRateRequests;
        public int fireRateRequestsCount;
        [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
        public NativeStream cooldownRateRequests;
        public int cooldownRateRequestsCount;
        [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
        public NativeStream accelerationSpeedRequests;
        public int accelerationSpeedRequestsCount;
        [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
        public NativeStream tractionRequests;
        public int tractionRequestsCount;

        public void Execute(int index) {
            var request = requests[index];
            switch (request.attType) {
                case AttributeType.health: {
                    Write(index, ref healthRequests, request, ref healthRequestsCount);
                    break;
                }
                case AttributeType.damage: {
                    Write(index, ref damageRequests, request, ref damageRequestsCount);
                    break;
                }
                case AttributeType.fireRate: {
                    Write(index, ref fireRateRequests, request, ref fireRateRequestsCount);
                    break;
                }
                case AttributeType.cooldownRate: {
                    Write(index, ref cooldownRateRequests, request, ref cooldownRateRequestsCount);
                    break;
                }
                case AttributeType.accelerationSpeed: {
                    Write(index, ref accelerationSpeedRequests, request, ref accelerationSpeedRequestsCount);
                    break;
                }
                case AttributeType.traction: {
                    Write(index, ref tractionRequests, request, ref tractionRequestsCount);
                    break;
                }
            }
        }

        public void Write(in int index, ref NativeStream stream, in AttributeManagerRequest request, ref int count) {
            var writer = stream.AsWriter();
            writer.BeginForEachIndex(index);
            writer.Write(request);
            writer.EndForEachIndex();
            count++;
        }
    }

    public struct AttributeModManagerQueueFilterJob : IJobParallelFor {
        [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
        public NativeArray<AttributeModManagerRequest> requests;
        [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
        public NativeStream healthModRequests;
        public int healthModRequestsCount;
        [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
        public NativeStream damageModRequests;
        public int damageModRequestsCount;
        [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
        public NativeStream fireRateModRequests;
        public int fireRateModRequestsCount;
        [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
        public NativeStream cooldownRateModRequests;
        public int cooldownRateModRequestsCount;
        [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
        public NativeStream accelerationSpeedModRequests;
        public int accelerationSpeedModRequestsCount;
        [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
        public NativeStream tractionModRequests;
        public int tractionModRequestsCount;

        public void Execute(int index) {
            var request = requests[index];
            switch (request.attType) {
                case AttributeType.health: {
                    Write(index, ref healthModRequests, request, ref healthModRequestsCount);
                    break;
                }
                case AttributeType.damage: {
                    Write(index, ref damageModRequests, request, ref damageModRequestsCount);
                    break;
                }
                case AttributeType.fireRate: {
                    Write(index, ref fireRateModRequests, request, ref fireRateModRequestsCount);
                    break;
                }
                case AttributeType.cooldownRate: {
                    Write(index, ref cooldownRateModRequests, request, ref cooldownRateModRequestsCount);
                    break;
                }
                case AttributeType.accelerationSpeed: {
                    Write(index, ref accelerationSpeedModRequests, request, ref accelerationSpeedModRequestsCount);
                    break;
                }
                case AttributeType.traction: {
                    Write(index, ref tractionModRequests, request, ref tractionModRequestsCount);
                    break;
                }
            }
        }
        
        public void Write(in int index, ref NativeStream stream, in AttributeModManagerRequest request, ref int count) {
            var writer = stream.AsWriter();
            writer.BeginForEachIndex(index);
            writer.Write(request);
            writer.EndForEachIndex();
            count++;
        }
    }
    #endregion
}