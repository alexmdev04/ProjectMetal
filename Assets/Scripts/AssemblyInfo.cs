using Metal;
using Metal.Components;
using Metal.Systems;
using Unity.Entities;
using Unity.Jobs;

// Health
// Update
[assembly: RegisterGenericSystemType(typeof(AttributeUpdateSystem<AttHealthBase, AttHealthModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeUpdateJob<AttHealthBase, AttHealthModValue>))]
// Queue
[assembly: RegisterGenericSystemType(typeof(AttributeQueueSystem<AttHealthBase, AttHealthModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeQueueJob<AttHealthBase, AttHealthModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeManagerQueueJob<AttHealthBase, AttHealthModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueueFilterJob<AttHealthBase>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueuePerBufferJob<AttHealthBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueuePerBufferJob<AttHealthBase, AttributeModTypeMul>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueuePerBufferJob<AttHealthBase, AttributeModTypeExp>))]
// Mod Buffers
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttHealthBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttHealthBase, AttributeModTypeMul>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttHealthBase, AttributeModTypeExp>))]
// Request Queues
[assembly: RegisterGenericComponentType(typeof(AttributeQueueFiltered<AttHealthBase>))]
[assembly: RegisterGenericComponentType(typeof(AttributeManagerQueueFiltered<AttHealthBase>))]
[assembly: RegisterGenericComponentType(typeof(AttributeModManagerQueueFiltered<AttHealthBase>))]

// Damage
// Update
[assembly: RegisterGenericSystemType(typeof(AttributeUpdateSystem<AttDamageBase, AttDamageModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeUpdateJob<AttDamageBase, AttDamageModValue>))]
// Queue
[assembly: RegisterGenericSystemType(typeof(AttributeQueueSystem<AttDamageBase, AttDamageModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeQueueJob<AttDamageBase, AttDamageModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeManagerQueueJob<AttDamageBase, AttDamageModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueueFilterJob<AttDamageBase>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueuePerBufferJob<AttDamageBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueuePerBufferJob<AttDamageBase, AttributeModTypeMul>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueuePerBufferJob<AttDamageBase, AttributeModTypeExp>))]
// Mod Buffers
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttDamageBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttDamageBase, AttributeModTypeMul>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttDamageBase, AttributeModTypeExp>))]
// Request Queues
[assembly: RegisterGenericComponentType(typeof(AttributeQueueFiltered<AttDamageBase>))]
[assembly: RegisterGenericComponentType(typeof(AttributeManagerQueueFiltered<AttDamageBase>))]
[assembly: RegisterGenericComponentType(typeof(AttributeModManagerQueueFiltered<AttDamageBase>))]

// Fire Rate
// Update
[assembly: RegisterGenericSystemType(typeof(AttributeUpdateSystem<AttFireRateBase, AttFireRateModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeUpdateJob<AttFireRateBase, AttFireRateModValue>))]
// Queue
[assembly: RegisterGenericSystemType(typeof(AttributeQueueSystem<AttFireRateBase, AttFireRateModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeQueueJob<AttFireRateBase, AttFireRateModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeManagerQueueJob<AttFireRateBase, AttFireRateModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueueFilterJob<AttFireRateBase>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueuePerBufferJob<AttFireRateBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueuePerBufferJob<AttFireRateBase, AttributeModTypeMul>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueuePerBufferJob<AttFireRateBase, AttributeModTypeExp>))]
// Mod Buffers
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttFireRateBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttFireRateBase, AttributeModTypeMul>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttFireRateBase, AttributeModTypeExp>))]
// Request Queues
[assembly: RegisterGenericComponentType(typeof(AttributeQueueFiltered<AttFireRateBase>))]
[assembly: RegisterGenericComponentType(typeof(AttributeManagerQueueFiltered<AttFireRateBase>))]
[assembly: RegisterGenericComponentType(typeof(AttributeModManagerQueueFiltered<AttFireRateBase>))]

// Cooldown Rate
// Update
[assembly: RegisterGenericSystemType(typeof(AttributeUpdateSystem<AttCooldownRateBase, AttCooldownRateModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeUpdateJob<AttCooldownRateBase, AttCooldownRateModValue>))]
// Queue
[assembly: RegisterGenericSystemType(typeof(AttributeQueueSystem<AttCooldownRateBase, AttCooldownRateModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeQueueJob<AttCooldownRateBase, AttCooldownRateModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeManagerQueueJob<AttCooldownRateBase, AttCooldownRateModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueueFilterJob<AttCooldownRateBase>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueuePerBufferJob<AttCooldownRateBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueuePerBufferJob<AttCooldownRateBase, AttributeModTypeMul>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueuePerBufferJob<AttCooldownRateBase, AttributeModTypeExp>))]
// Mod Buffers
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttCooldownRateBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttCooldownRateBase, AttributeModTypeMul>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttCooldownRateBase, AttributeModTypeExp>))]
// Request Queues
[assembly: RegisterGenericComponentType(typeof(AttributeQueueFiltered<AttCooldownRateBase>))]
[assembly: RegisterGenericComponentType(typeof(AttributeManagerQueueFiltered<AttCooldownRateBase>))]
[assembly: RegisterGenericComponentType(typeof(AttributeModManagerQueueFiltered<AttCooldownRateBase>))]

// Acceleration Speed
// Update
[assembly: RegisterGenericSystemType(typeof(AttributeUpdateSystem<AttAccelerationSpeedBase, AttAccelerationSpeedModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeUpdateJob<AttAccelerationSpeedBase, AttAccelerationSpeedModValue>))]
// Queue
[assembly: RegisterGenericSystemType(typeof(AttributeQueueSystem<AttAccelerationSpeedBase, AttAccelerationSpeedModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeQueueJob<AttAccelerationSpeedBase, AttAccelerationSpeedModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeManagerQueueJob<AttAccelerationSpeedBase, AttAccelerationSpeedModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueueFilterJob<AttAccelerationSpeedBase>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueuePerBufferJob<AttAccelerationSpeedBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueuePerBufferJob<AttAccelerationSpeedBase, AttributeModTypeMul>))]
[assembly: RegisterGenericJobType(typeof(AttributeModManagerQueuePerBufferJob<AttAccelerationSpeedBase, AttributeModTypeExp>))]
// Mod Buffers
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttAccelerationSpeedBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttAccelerationSpeedBase, AttributeModTypeMul>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttAccelerationSpeedBase, AttributeModTypeExp>))]
// Request Queues
[assembly: RegisterGenericComponentType(typeof(AttributeQueueFiltered<AttAccelerationSpeedBase>))]
[assembly: RegisterGenericComponentType(typeof(AttributeManagerQueueFiltered<AttAccelerationSpeedBase>))]
[assembly: RegisterGenericComponentType(typeof(AttributeModManagerQueueFiltered<AttAccelerationSpeedBase>))]