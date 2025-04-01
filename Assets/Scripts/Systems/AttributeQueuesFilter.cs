using Metal.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Logging;

namespace Metal.Systems {
    [UpdateInGroup(typeof(AttributeUpdateSystemGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct AttributeQueuesFilterSystem : ISystem {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            #region Update Requirements
            state.RequireForUpdate<AttributeQueueFiltered<AttHealthBase>>();            
            state.RequireForUpdate<AttributeManagerQueueFiltered<AttHealthBase>>();
            state.RequireForUpdate<AttributeModManagerQueueFiltered<AttHealthBase>>();            
            state.RequireForUpdate<AttributeQueueFiltered<AttDamageBase>>();            
            state.RequireForUpdate<AttributeManagerQueueFiltered<AttDamageBase>>();
            state.RequireForUpdate<AttributeModManagerQueueFiltered<AttDamageBase>>();            
            state.RequireForUpdate<AttributeQueueFiltered<AttFireRateBase>>();            
            state.RequireForUpdate<AttributeManagerQueueFiltered<AttFireRateBase>>();
            state.RequireForUpdate<AttributeModManagerQueueFiltered<AttFireRateBase>>();            
            state.RequireForUpdate<AttributeQueueFiltered<AttCooldownRateBase>>();            
            state.RequireForUpdate<AttributeManagerQueueFiltered<AttCooldownRateBase>>();
            state.RequireForUpdate<AttributeModManagerQueueFiltered<AttCooldownRateBase>>();            
            state.RequireForUpdate<AttributeQueueFiltered<AttAccelerationSpeedBase>>();            
            state.RequireForUpdate<AttributeManagerQueueFiltered<AttAccelerationSpeedBase>>();
            state.RequireForUpdate<AttributeModManagerQueueFiltered<AttAccelerationSpeedBase>>();            
            #endregion
            state.EntityManager.AddComponentData(
                state.SystemHandle,
                new AttributeQueue() {
                    q = new NativeQueue<AttributeRequest>(Allocator.Persistent)
                }
            );
            state.EntityManager.AddComponentData(state.SystemHandle,
                new AttributeManagerQueue() {
                    q = new NativeQueue<AttributeManagerRequest>(Allocator.Persistent)
                }
            );
            state.EntityManager.AddComponentData(
                state.SystemHandle,
                new AttributeModManagerQueue() {
                    q = new NativeQueue<AttributeModManagerRequest>(Allocator.Persistent)
                }
            );
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            //Log.Debug("[AttributeQueuesFilterSystem] Update");
            
            NativeQueue<AttributeRequest> attributeQueue = SystemAPI.GetComponent<AttributeQueue>(state.SystemHandle).q;
            for (int i = 0; i < attributeQueue.Count; i++) {
                var request = attributeQueue.Dequeue();
                switch (request.targetAttribute) {
                    case AttributeType.health: { SystemAPI.GetSingleton<AttributeQueueFiltered<AttHealthBase>>().q.Enqueue(request); break; }
                    case AttributeType.damage: { SystemAPI.GetSingleton<AttributeQueueFiltered<AttDamageBase>>().q.Enqueue(request); break; }
                    case AttributeType.fireRate: { SystemAPI.GetSingleton<AttributeQueueFiltered<AttFireRateBase>>().q.Enqueue(request); break; }
                    case AttributeType.cooldownRate: { SystemAPI.GetSingleton<AttributeQueueFiltered<AttCooldownRateBase>>().q.Enqueue(request); break; }
                    case AttributeType.accelerationSpeed: { SystemAPI.GetSingleton<AttributeQueueFiltered<AttAccelerationSpeedBase>>().q.Enqueue(request); break; }
                }
            }
            
            NativeQueue<AttributeManagerRequest> attributeManagerQueue = SystemAPI.GetComponent<AttributeManagerQueue>(state.SystemHandle).q;
            for (int i = 0; i < attributeManagerQueue.Count; i++) {
                var request = attributeManagerQueue.Dequeue();
                switch (request.attType) {
                    case AttributeType.health: { SystemAPI.GetSingleton<AttributeManagerQueueFiltered<AttHealthBase>>().q.Enqueue(request); break; }
                    case AttributeType.damage: { SystemAPI.GetSingleton<AttributeManagerQueueFiltered<AttDamageBase>>().q.Enqueue(request); break; }
                    case AttributeType.fireRate: { SystemAPI.GetSingleton<AttributeManagerQueueFiltered<AttFireRateBase>>().q.Enqueue(request); break; }
                    case AttributeType.cooldownRate: { SystemAPI.GetSingleton<AttributeManagerQueueFiltered<AttCooldownRateBase>>().q.Enqueue(request); break; }
                    case AttributeType.accelerationSpeed: { SystemAPI.GetSingleton<AttributeManagerQueueFiltered<AttAccelerationSpeedBase>>().q.Enqueue(request); break; }
                }
            }
            
            NativeQueue<AttributeModManagerRequest> attributeModManagerQueue = SystemAPI.GetComponent<AttributeModManagerQueue>(state.SystemHandle).q;
            for (int i = 0; i < attributeModManagerQueue.Count; i++) { 
                var request = attributeModManagerQueue.Dequeue();
                switch (request.attType) {
                    case AttributeType.health: { SystemAPI.GetSingleton<AttributeModManagerQueueFiltered<AttHealthBase>>().q.Enqueue(request); break; }
                    case AttributeType.damage: { SystemAPI.GetSingleton<AttributeModManagerQueueFiltered<AttDamageBase>>().q.Enqueue(request); break; }
                    case AttributeType.fireRate: { SystemAPI.GetSingleton<AttributeModManagerQueueFiltered<AttFireRateBase>>().q.Enqueue(request); break; }
                    case AttributeType.cooldownRate: { SystemAPI.GetSingleton<AttributeModManagerQueueFiltered<AttCooldownRateBase>>().q.Enqueue(request); break; }
                    case AttributeType.accelerationSpeed: { SystemAPI.GetSingleton<AttributeModManagerQueueFiltered<AttAccelerationSpeedBase>>().q.Enqueue(request); break; }
                }
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) {
            SystemAPI.GetComponent<AttributeQueue>(state.SystemHandle).q.Dispose();
            SystemAPI.GetComponent<AttributeManagerQueue>(state.SystemHandle).q.Dispose();
            SystemAPI.GetComponent<AttributeModManagerQueue>(state.SystemHandle).q.Dispose();
        }
    }
}