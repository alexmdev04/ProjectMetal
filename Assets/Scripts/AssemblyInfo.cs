using Metal;
using Metal.Components;
using Metal.Systems;
using Unity.Entities;
using Unity.Jobs;
using static Metal.Extensions;

// Health
// Update
[assembly: RegisterGenericSystemType(typeof(AttributeUpdateSystem<AttHealthBase, AttHealthModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeUpdateJob<AttHealthBase, AttHealthModValue>))]
// Queue
[assembly: RegisterGenericSystemType(typeof(AttributeQueueSystem<AttHealthBase, AttHealthModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeQueueJob<AttHealthBase, AttHealthModValue>))]
// Mod Buffers
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttHealthBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttHealthBase, AttributeModTypeMul>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttHealthBase, AttributeModTypeExp>))]
// Request Queues
[assembly: RegisterGenericComponentType(typeof(AttributeManagerRequests<AttHealthBase>))]
[assembly: RegisterGenericComponentType(typeof(AttributeModManagerRequests<AttHealthBase>))]

// Damage
// Update
[assembly: RegisterGenericSystemType(typeof(AttributeUpdateSystem<AttDamageBase, AttDamageModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeUpdateJob<AttDamageBase, AttDamageModValue>))]
// Queue
[assembly: RegisterGenericSystemType(typeof(AttributeQueueSystem<AttDamageBase, AttDamageModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeQueueJob<AttDamageBase, AttDamageModValue>))]
// Mod Buffers
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttDamageBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttDamageBase, AttributeModTypeMul>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttDamageBase, AttributeModTypeExp>))]
// Request Queues
[assembly: RegisterGenericComponentType(typeof(AttributeManagerRequests<AttDamageBase>))]
[assembly: RegisterGenericComponentType(typeof(AttributeModManagerRequests<AttDamageBase>))]

// Fire Rate
// Update
[assembly: RegisterGenericSystemType(typeof(AttributeUpdateSystem<AttFireRateBase, AttFireRateModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeUpdateJob<AttFireRateBase, AttFireRateModValue>))]
// Queue
[assembly: RegisterGenericSystemType(typeof(AttributeQueueSystem<AttFireRateBase, AttFireRateModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeQueueJob<AttFireRateBase, AttFireRateModValue>))]
// Mod Buffers
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttFireRateBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttFireRateBase, AttributeModTypeMul>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttFireRateBase, AttributeModTypeExp>))]
// Request Queues
[assembly: RegisterGenericComponentType(typeof(AttributeManagerRequests<AttFireRateBase>))]
[assembly: RegisterGenericComponentType(typeof(AttributeModManagerRequests<AttFireRateBase>))]

// Cooldown Rate
// Update
[assembly: RegisterGenericSystemType(typeof(AttributeUpdateSystem<AttCooldownRateBase, AttCooldownRateModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeUpdateJob<AttCooldownRateBase, AttCooldownRateModValue>))]
// Queue
[assembly: RegisterGenericSystemType(typeof(AttributeQueueSystem<AttCooldownRateBase, AttCooldownRateModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeQueueJob<AttCooldownRateBase, AttCooldownRateModValue>))]
// Mod Buffers
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttCooldownRateBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttCooldownRateBase, AttributeModTypeMul>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttCooldownRateBase, AttributeModTypeExp>))]
// Request Queues
[assembly: RegisterGenericComponentType(typeof(AttributeManagerRequests<AttCooldownRateBase>))]
[assembly: RegisterGenericComponentType(typeof(AttributeModManagerRequests<AttCooldownRateBase>))]

// Acceleration Speed
// Update
[assembly: RegisterGenericSystemType(typeof(AttributeUpdateSystem<AttAccelerationSpeedBase, AttAccelerationSpeedModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeUpdateJob<AttAccelerationSpeedBase, AttAccelerationSpeedModValue>))]
// Queue
[assembly: RegisterGenericSystemType(typeof(AttributeQueueSystem<AttAccelerationSpeedBase, AttAccelerationSpeedModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeQueueJob<AttAccelerationSpeedBase, AttAccelerationSpeedModValue>))]
// Mod Buffers
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttAccelerationSpeedBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttAccelerationSpeedBase, AttributeModTypeMul>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttAccelerationSpeedBase, AttributeModTypeExp>))]
// Request Queues
[assembly: RegisterGenericComponentType(typeof(AttributeManagerRequests<AttAccelerationSpeedBase>))]
[assembly: RegisterGenericComponentType(typeof(AttributeModManagerRequests<AttAccelerationSpeedBase>))]

// Traction
// Update
[assembly: RegisterGenericSystemType(typeof(AttributeUpdateSystem<AttTractionBase, AttTractionModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeUpdateJob<AttTractionBase, AttTractionModValue>))]
// Queue
[assembly: RegisterGenericSystemType(typeof(AttributeQueueSystem<AttTractionBase, AttTractionModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeQueueJob<AttTractionBase, AttTractionModValue>))]
// Mod Buffers
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttTractionBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttTractionBase, AttributeModTypeMul>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttTractionBase, AttributeModTypeExp>))]
// Request Queues
[assembly: RegisterGenericComponentType(typeof(AttributeManagerRequests<AttTractionBase>))]
[assembly: RegisterGenericComponentType(typeof(AttributeModManagerRequests<AttTractionBase>))]