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
[assembly: RegisterGenericSystemType(typeof(AttributeQueue<AttHealthBase, AttHealthModValue>))]
[assembly: RegisterGenericJobType(typeof(AttributeQueueJob<AttHealthBase, AttHealthModValue>))]
// Mod Buffers
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttHealthBase, AttributeModTypeAdd>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttHealthBase, AttributeModTypeMul>))]
[assembly: RegisterGenericComponentType(typeof(AttributeMod<AttHealthBase, AttributeModTypeExp>))]