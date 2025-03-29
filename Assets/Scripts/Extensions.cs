using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using System.Linq;
using JetBrains.Annotations;
using Metal.Components;
using UnityEditor;
using UnityEngine;
using ForceMode = Unity.Physics.Extensions.ForceMode;

namespace Metal {
    [BurstCompile]
    public static class Extensions {
        /// <summary>
        /// Gets the velocity of a point relative to a rigidbody
        /// </summary>
        /// <param name="linearVelocity">Current Linear Velocity</param>
        /// <param name="angularVelocity">Current Angular Velocity</param>
        /// <param name="rigidbodyWorldCenterOfMass">Rigidbody World Center of Mass</param>
        /// <param name="worldPoint">Point World-Space Position</param>
        /// <param name="result">The resulting velocity</param>
        [BurstCompile]
        public static void GetPointVelocity(in float3 linearVelocity, in float3 angularVelocity, in float3 rigidbodyWorldCenterOfMass, in float3 worldPoint, out float3 result) {
            result = linearVelocity + math.cross(angularVelocity, worldPoint - rigidbodyWorldCenterOfMass);
        }
        
        /// <summary>
        /// Gets the velocity of a point relative to a rigidbody
        /// </summary>
        /// <param name="velocity">Current Velocity</param>
        /// <param name="rigidbodyWorldCenterOfMass">Rigidbody World Center of Mass</param>
        /// <param name="worldPoint">Point World-Space Position</param>
        /// <param name="result">The resulting velocity</param>
        [BurstCompile]
        public static void GetPointVelocity(in PhysicsVelocity velocity, in float3 rigidbodyWorldCenterOfMass, in float3 worldPoint, out float3 result) {
            result = velocity.Linear + math.cross(velocity.Angular, worldPoint - rigidbodyWorldCenterOfMass);
        }
        
        /// <summary>
        /// Applies a force to a rigidbody at a position in world space
        /// </summary>
        /// <param name="linearVelocity">Linear Velocity value that will receive the force</param>
        /// <param name="angularVelocity">Angular Velocity value that will receive the force</param>
        /// <param name="force">Force to apply</param>
        /// <param name="forceWorldPosition">World-space position of the force origin</param>
        /// <param name="inverseMass">Inverse mass of the target rigidbody (usually found in PhysicsMass.InverseMass)</param>
        /// <param name="inverseInertia">Inverse inertia tensor of the target rigidbody (usually found in PhysicsMass.InverseInertia)</param>
        /// <param name="rigidbodyWorldCenterOfMass">World-space position of the target rigidbody's center of mass</param>
        /// <param name="mode">The force mode to use, only Force & Acceleration are implemented</param>
        /// <param name="fixedDeltaTime">The time step to use, defaults to FixedStepSimulationSystemGroup.TimeStep (1/60)</param>
        [BurstCompile]
        public static void AddForceAtPosition(
            ref float3 linearVelocity,
            ref float3 angularVelocity,
            in float3 force,
            in float3 forceWorldPosition,
            in float inverseMass,
            in float3 inverseInertia,
            in float3 rigidbodyWorldCenterOfMass,
            in ForceMode mode = ForceMode.Force,
            in float fixedDeltaTime = 1.0f/60.0f) {
            linearVelocity += mode switch {
                ForceMode.Force => force * inverseMass * fixedDeltaTime,
                ForceMode.Acceleration => force * fixedDeltaTime,
                _ => throw new NotImplementedException()
            };
            AddRelativeTorque(
                ref angularVelocity,
                math.cross(forceWorldPosition - rigidbodyWorldCenterOfMass, force),
                inverseInertia,
                mode);
        }

        /// <summary>
        /// Applies a force to a rigidbody at a position in world space
        /// </summary>
        /// <param name="velocity">Velocity Component that will receive the force</param>
        /// <param name="force">Force to apply</param>
        /// <param name="forceWorldPosition">World-space position of the force origin</param>
        /// <param name="inverseMass">Inverse mass of the target rigidbody (usually found in PhysicsMass.InverseMass)</param>
        /// <param name="inverseInertia">Inverse inertia tensor of the target rigidbody (usually found in PhysicsMass.InverseInertia)</param>        /// <param name="rigidbodyWorldCenterOfMass">World-space position of the target rigidbody's center of mass</param>
        /// <param name="mode">The force mode to use, only Force and Acceleration are implemented</param>
        /// <param name="fixedDeltaTime">The time step to use, defaults to FixedStepSimulationSystemGroup.TimeStep (1/60)</param>
        /// <exception cref="NotImplementedException">Occurs if Impulse or VelocityChange force modes are used</exception>
        [BurstCompile]
        public static void AddForceAtPosition(
            ref PhysicsVelocity velocity,
            in float3 force,
            in float3 forceWorldPosition,
            in float inverseMass,
            in float3 inverseInertia,
            in float3 rigidbodyWorldCenterOfMass,
            in ForceMode mode = ForceMode.Force,
            in float fixedDeltaTime = 1.0f/60.0f) {
            velocity.Linear += mode switch {
                ForceMode.Force => force * inverseMass * fixedDeltaTime,
                ForceMode.Acceleration => force * fixedDeltaTime,
                _ => throw new NotImplementedException()
            };
            AddRelativeTorque(
                ref velocity.Angular,
                math.cross(forceWorldPosition - rigidbodyWorldCenterOfMass, force),
                inverseInertia,
                mode);
        }
        
        /// <summary>
        /// Applies a force to a rigidbody at a position in world space
        /// </summary>
        /// <param name="velocity">Velocity Component that will receive the force</param>
        /// <param name="force">Force to apply</param>
        /// <param name="forceWorldPosition">World-space position of the force origin</param>
        /// <param name="mass">Component that contains both InverseMass and InverseInertia of the target rigidbody</param>
        /// <param name="rigidbodyWorldCenterOfMass">World-space position of the target rigidbody's center of mass</param>
        /// <param name="mode">The force mode to use, only Force and Acceleration are implemented</param>
        /// <param name="fixedDeltaTime">The time step to use, defaults to FixedStepSimulationSystemGroup.TimeStep (1/60)</param>
        /// <exception cref="NotImplementedException">Occurs if Impulse or VelocityChange force modes are used</exception>
        [BurstCompile]
        public static void AddForceAtPosition(
            ref PhysicsVelocity velocity,
            in float3 force,
            in float3 forceWorldPosition,
            in PhysicsMass mass,
            in float3 rigidbodyWorldCenterOfMass,
            in ForceMode mode = ForceMode.Force,
            in float fixedDeltaTime = 1.0f/60.0f) {
            velocity.Linear += mode switch {
                ForceMode.Force => force * mass.InverseMass * fixedDeltaTime,
                ForceMode.Acceleration => force * fixedDeltaTime,
                _ => throw new NotImplementedException()
            };
            AddRelativeTorque(
                ref velocity.Angular,
                math.cross(forceWorldPosition - rigidbodyWorldCenterOfMass, force),
                mass.InverseInertia,
                mode);
        }
        
        /// <summary>
        /// Applies torque relative to a rigidbody
        /// </summary>
        /// <param name="angularVelocity">Angular Velocity value that will receive the torque</param>
        /// <param name="torque">Torque to apply</param>
        /// <param name="inverseInertia">Inverse inertia tensor of the target rigidbody (usually found in PhysicsMass.InverseInertia)</param>
        /// <param name="mode">The force mode to use, only Force and Acceleration are implemented</param>
        /// <param name="fixedDeltaTime">The time step to use, defaults to FixedStepSimulationSystemGroup.TimeStep (1/60)</param>
        /// <exception cref="NotImplementedException">Occurs if Impulse or VelocityChange force modes are used</exception>
        [BurstCompile]
        public static void AddRelativeTorque(
            ref float3 angularVelocity,
            in float3 torque, 
            in float3 inverseInertia, 
            in ForceMode mode = ForceMode.Force,
            in float fixedDeltaTime = 1.0f/60.0f)
            => angularVelocity += mode switch {
                ForceMode.Force => torque * inverseInertia * fixedDeltaTime,
                ForceMode.Acceleration => torque * fixedDeltaTime,
                _ => throw new NotImplementedException()
            };
        
        /// <summary>
        /// Applies torque relative to a rigidbody
        /// </summary>
        /// <param name="angularVelocity">Angular Velocity value that will receive the torque</param>
        /// <param name="torque">Torque to apply</param>
        /// <param name="mass">Component that contains the InverseInertia of the target rigidbody</param>
        /// <param name="mode">The force mode to use, only Force and Acceleration are implemented</param>
        /// <param name="fixedDeltaTime">The time step to use, defaults to FixedStepSimulationSystemGroup.TimeStep (1/60)</param>
        /// <exception cref="NotImplementedException">Occurs if Impulse or VelocityChange force modes are used</exception>
        [BurstCompile]
        public static void AddRelativeTorque(
            ref float3 angularVelocity,
            in float3 torque, 
            in PhysicsMass mass, 
            in ForceMode mode = ForceMode.Force,
            in float fixedDeltaTime = 1.0f/60.0f)
            => angularVelocity += mode switch {
                ForceMode.Force => torque * mass.InverseInertia * fixedDeltaTime,
                ForceMode.Acceleration => torque * fixedDeltaTime,
                _ => throw new NotImplementedException()
            };
        
        /// <summary>
        /// Applies torque relative to a rigidbody
        /// </summary>
        /// <param name="velocity">Velocity Component that will receive the force</param>
        /// <param name="torque">Torque to apply</param>
        /// <param name="inverseInertia">Inverse inertia tensor of the target rigidbody (usually found in PhysicsMass.InverseInertia)</param>
        /// <param name="mode">The force mode to use, only Force and Acceleration are implemented</param>
        /// <param name="fixedDeltaTime">The time step to use, defaults to FixedStepSimulationSystemGroup.TimeStep (1/60)</param>
        /// <exception cref="NotImplementedException">Occurs if Impulse or VelocityChange force modes are used</exception>
        [BurstCompile]
        public static void AddRelativeTorque(
            ref PhysicsVelocity velocity,
            in float3 torque, 
            in float3 inverseInertia, 
            in ForceMode mode = ForceMode.Force,
            in float fixedDeltaTime = 1.0f/60.0f)
            => velocity.Angular += mode switch {
                ForceMode.Force => torque * inverseInertia * fixedDeltaTime,
                ForceMode.Acceleration => torque * fixedDeltaTime,
                _ => throw new NotImplementedException()
            };
        
        /// <summary>
        /// Applies torque relative to a rigidbody
        /// </summary>
        /// <param name="velocity">Velocity Component that will receive the force</param>
        /// <param name="torque">Torque to apply</param>
        /// <param name="mass">Component that contains the InverseInertia of the target rigidbody</param>
        /// <param name="mode">The force mode to use, only Force and Acceleration are implemented</param>
        /// <param name="fixedDeltaTime">The time step to use, defaults to FixedStepSimulationSystemGroup.TimeStep (1/60)</param>
        /// <exception cref="NotImplementedException">Occurs if Impulse or VelocityChange force modes are used</exception>
        [BurstCompile]
        public static void AddRelativeTorque(
            ref PhysicsVelocity velocity,
            in float3 torque, 
            in PhysicsMass mass, 
            in ForceMode mode = ForceMode.Force,
            in float fixedDeltaTime = 1.0f/60.0f)
            => velocity.Angular += mode switch {
                ForceMode.Force => torque * mass.InverseInertia * fixedDeltaTime,
                ForceMode.Acceleration => torque * fixedDeltaTime,
                _ => throw new NotImplementedException()
            };
        
        public static float[] ToPointArray(this UnityEngine.AnimationCurve graph, int sampleCount) {
            float sampleStep = 1.0f / sampleCount;
            
            List<Keyframe> sampledPoints = new (sampleCount);
            for (int i = 0; i < sampleCount; i++) {
                float time = i * sampleStep;
                sampledPoints.Add(new Keyframe(time, graph.Evaluate(time)));
            }
            
            return graph.keys
                .Concat(sampledPoints)
                .OrderBy(element => element.time)
                .Select(element => element.value)
                .ToArray();
        }

        [BurstCompile]
        public static float Evaluate(this ref BlobArray<float> points, float value) {
            int highestIndex = points.Length - 1;
            switch (value) {
                case <= 0.0001f: {
                    return points[0];
                }
                case >= 0.9999f: {
                    return points[highestIndex];
                }
                default: {
                    float decimalIndex = highestIndex * value; // identical to lerping with lower bound of 0
                    int indexFloor = (int)decimalIndex;
                    return math.lerp(points[indexFloor], points[indexFloor + 1], decimalIndex - indexFloor);
                }
            }
        }
        
        public static float DivRem(float a, float b, out float remainder) {
            remainder = a % b;
            return a / b;
        }

        [BurstCompile]
        public static void ToString(in SpawnPrefabRequestType type, out FixedString32Bytes name) => name = type switch {
            SpawnPrefabRequestType.Vehicle_Hilux => (FixedString32Bytes)"Vehicle_Hilux",
            _ => (FixedString32Bytes)"Unknown SpawnPrefabRequestType"
        };

        [BurstCompile]
        public static void Tick<TAtt, TModType>(this in DynamicBuffer<AttributeMod<TAtt, TModType>> modifierList, in float delta) 
            where TAtt : unmanaged, IAttribute
            where TModType : unmanaged, IAttributeModType {
            
            var attributeModifiers = modifierList;
            int length = attributeModifiers.Length, 
                i = 0;
            while (i < length) {
                AttributeModInternal mod = attributeModifiers[i].data;
                mod.Tick(delta);
                if (!mod.dead) { i++; continue; }
                attributeModifiers[i] = attributeModifiers[^1];
                attributeModifiers.RemoveAt(attributeModifiers.Length - 1);
                length--;
            }
        }

        public static void AttributeManagerRequestCreator() {
            
        }
        
        
        // [BurstCompile]
        // public static void AddAttribute<TAtt, TModAtt>(
        //     this ref EntityCommandBuffer.ParallelWriter ecb,
        //     in int index,
        //     in Entity entity,
        //     in AttributeConstructor constructor)
        //     where TAtt : unmanaged, IAttribute
        //     where TModAtt : unmanaged, IAttributeModValue {
        //
        //     ecb.AddComponent(index, entity, new TAtt {
        //         att = new Components.AttributeInternal(constructor.baseValue, constructor.minModValue, constructor.maxModValue),
        //         modsChanged = true
        //     });
        //     var addMods = ecb.AddBuffer<AttributeMod<TAtt, AttributeModTypeAdd>>(index, entity);
        //     var mulMods = ecb.AddBuffer<AttributeMod<TAtt, AttributeModTypeMul>>(index, entity);
        //     var expMods = ecb.AddBuffer<AttributeMod<TAtt, AttributeModTypeExp>>(index, entity);
        //     
        //     if (!constructor.modConstructors.HasValue) return;
        //     foreach (AttributeModConstructor modConstructor in constructor.modConstructors) {
        //         // attribute type is already known so its not used
        //         switch (modConstructor.modType) {
        //             case AttributeModType.addition: {
        //                 addMods.Add(new AttributeMod<TAtt, AttributeModTypeAdd>(modConstructor));
        //                 break;
        //             }
        //             case AttributeModType.multiplier: {
        //                 mulMods.Add(new AttributeMod<TAtt, AttributeModTypeMul>(modConstructor));
        //                 break;
        //             }
        //             case AttributeModType.exponent: {
        //                 expMods.Add(new AttributeMod<TAtt, AttributeModTypeExp>(modConstructor));
        //                 break;
        //             }
        //         }
        //     }
        //     new AttributeModSet<TAtt>(addMods, mulMods, expMods).CalculateModValue(constructor.baseValue, out double modValue);
        //     ecb.AddComponent(index, entity, new TModAtt{ value = modValue, maxValue = modValue });
        // }
        //
        // [BurstCompile]
        // public static void AddAttribute<TAtt, TModAtt>(
        //     this in EntityCommandBuffer ecb,
        //     in Entity entity,
        //     in AttributeConstructor constructor)
        //     where TAtt : unmanaged, IAttribute
        //     where TModAtt : unmanaged, IAttributeModValue {
        //
        //     ecb.AddComponent(entity, new TAtt {
        //         att = new Components.AttributeInternal(constructor.baseValue, constructor.minModValue, constructor.maxModValue),
        //         modsChanged = true
        //     });
        //     var addMods = ecb.AddBuffer<AttributeMod<TAtt, AttributeModTypeAdd>>(entity);
        //     var mulMods = ecb.AddBuffer<AttributeMod<TAtt, AttributeModTypeMul>>(entity);
        //     var expMods = ecb.AddBuffer<AttributeMod<TAtt, AttributeModTypeExp>>(entity);
        //     
        //     if (!constructor.modConstructors.HasValue) return;
        //     foreach (AttributeModConstructor modConstructor in constructor.modConstructors) {
        //         // attribute type is already known so its not used
        //         switch (modConstructor.modType) {
        //             case AttributeModType.addition: {
        //                 addMods.Add(new AttributeMod<TAtt, AttributeModTypeAdd>(modConstructor));
        //                 break;
        //             }
        //             case AttributeModType.multiplier: {
        //                 mulMods.Add(new AttributeMod<TAtt, AttributeModTypeMul>(modConstructor));
        //                 break;
        //             }
        //             case AttributeModType.exponent: {
        //                 expMods.Add(new AttributeMod<TAtt, AttributeModTypeExp>(modConstructor));
        //                 break;
        //             }
        //         }
        //     }
        //     new AttributeModSet<TAtt>(addMods, mulMods, expMods).CalculateModValue(constructor.baseValue, out double modValue);
        //     ecb.AddComponent(entity, new TModAtt{ value = modValue, maxValue = modValue });
        // }
        //
        // public static void AddAttribute(
        //     this ref EntityCommandBuffer.ParallelWriter ecb,
        //     in int index,
        //     in Entity entity,
        //     in AttributeConstructor constructor) {
        //     switch (constructor.type) {
        //         case AttributeType.health: {
        //             AddAttribute<AttHealthBase, AttHealthModValue>(ref ecb, index, entity, constructor);
        //             break;
        //         }
        //         default:
        //             throw new ArgumentOutOfRangeException();
        //     }
        // }
        //
        // public static void AddAttribute(
        //     this in EntityCommandBuffer ecb,
        //     in Entity entity,
        //     in AttributeConstructor constructor) {
        //     switch (constructor.type) {
        //         case AttributeType.health: {
        //             AddAttribute<AttHealthBase, AttHealthModValue>(ecb, entity, constructor);
        //             break;
        //         }
        //         default:
        //             throw new ArgumentOutOfRangeException();
        //     }
        // }
        //
        // public static void EditAttribute(
        //     this ref EntityCommandBuffer.ParallelWriter ecb,
        //     in int index,
        //     in Entity entity,
        //     in AttributeConstructor constructor) {
        //     switch (constructor.type) {
        //         case AttributeType.health: {
        //             ecb.SetComponent<AttHealthBase>(index, entity, new () {
        //                 att = new AttributeInternal(constructor.baseValue, constructor.minModValue, constructor.maxModValue),
        //                 modsChanged = true
        //             });
        //             break;
        //         }
        //         default:
        //             throw new ArgumentOutOfRangeException();
        //     }
        // }
        //
        // public static void EditAttribute(
        //     this in EntityCommandBuffer ecb,
        //     in Entity entity,
        //     in AttributeConstructor constructor) {
        //     switch (constructor.type) {
        //         case AttributeType.health: {
        //             ecb.SetComponent<AttHealthBase>(entity, new () {
        //                 att = new AttributeInternal(constructor.baseValue, constructor.minModValue, constructor.maxModValue),
        //                 modsChanged = true
        //             });
        //             break;
        //         }
        //         default:
        //             throw new ArgumentOutOfRangeException();
        //     }
        // }
        //
        // public static void RemoveAttribute(
        //     this ref EntityCommandBuffer.ParallelWriter ecb,
        //     in int index,
        //     in Entity entity,
        //     in AttributeType type) {
        //     switch (type) {
        //         case AttributeType.health: {
        //             ecb.RemoveAttribute<AttHealthBase>(index, entity);
        //             break;
        //         }
        //         default:
        //             throw new ArgumentOutOfRangeException();
        //     }
        // }
        //
        // public static void RemoveAttribute(
        //     this in EntityCommandBuffer ecb,
        //     in Entity entity,
        //     in AttributeType type) {
        //     switch (type) {
        //         case AttributeType.health: {
        //             ecb.RemoveAttribute<AttHealthBase>(entity);
        //             break;
        //         }
        //         default:
        //             throw new ArgumentOutOfRangeException();
        //     }
        // }
        //
        // public static void RemoveAttribute<TAtt>(
        //     this ref EntityCommandBuffer.ParallelWriter ecb,
        //     in int index,
        //     in Entity entity)
        //     where TAtt : unmanaged, IAttribute {
        //     ecb.RemoveComponent<TAtt>(index, entity);
        //     ecb.RemoveComponent<DynamicBuffer<AttributeMod<TAtt, AttributeModTypeAdd>>>(index, entity);
        //     ecb.RemoveComponent<DynamicBuffer<AttributeMod<TAtt, AttributeModTypeMul>>>(index, entity);
        //     ecb.RemoveComponent<DynamicBuffer<AttributeMod<TAtt, AttributeModTypeExp>>>(index, entity);
        // }
        //
        // public static void RemoveAttribute<TAtt>(
        //     this in EntityCommandBuffer ecb,
        //     in Entity entity)
        //     where TAtt : unmanaged, IAttribute {
        //     ecb.RemoveComponent<TAtt>(entity);
        //     ecb.RemoveComponent<DynamicBuffer<AttributeMod<TAtt, AttributeModTypeAdd>>>(entity);
        //     ecb.RemoveComponent<DynamicBuffer<AttributeMod<TAtt, AttributeModTypeMul>>>(entity);
        //     ecb.RemoveComponent<DynamicBuffer<AttributeMod<TAtt, AttributeModTypeExp>>>(entity);
        // }
        //
        // [BurstCompile]
        // public static void AddAttributeMod<TAtt, TModType>(
        //     in BufferLookup<AttributeMod<TAtt, TModType>> modLookup,
        //     in AttributeModManagerRequest<TAtt> request,
        //     RefRW<TAtt> att) 
        //     where TAtt : unmanaged, IAttribute 
        //     where TModType : unmanaged, IAttributeModType {
        //
        //     request.modRefPointer.Get(out var modRef);
        //     modRef.index = modLookup[modRef.entity].Add(new AttributeMod<TAtt, TModType>(request.constructor));
        //     request.modRefPointer.Set(modRef);
        //     att.ValueRW.modsChanged = true;
        // }
        //
        // public static void EditAttributeMod<TAtt, TModType>(
        //     in BufferLookup<AttributeMod<TAtt, TModType>> modLookup,
        //     in AttributeModManagerRequest<TAtt> request,
        //     RefRW<TAtt> att) 
        //     where TAtt : unmanaged, IAttribute 
        //     where TModType : unmanaged, IAttributeModType {
        //
        //     request.modRefPointer.Get(out var modRef);
        //     var mod = modLookup[modRef.entity].ElementAt(modRef.index);
        //     mod.data.value = request.constructor.value;
        //     mod.data.lifeTime = request.constructor.lifeTime;
        //     mod.data.expires = request.constructor.expires;
        //     modLookup[modRef.entity].ElementAt(modRef.index) = mod;
        //     att.ValueRW.modsChanged = true;
        // }
        //
        // public static void RemoveAttributeMod<TAtt, TModType>(
        //     in BufferLookup<AttributeMod<TAtt, TModType>> modLookup,
        //     in AttributeModManagerRequest<TAtt> request,
        //     RefRW<TAtt> att)
        //     where TAtt : unmanaged, IAttribute 
        //     where TModType : unmanaged, IAttributeModType {
        //     
        //     request.modRefPointer.Get(out var modRef);
        //     modLookup[modRef.entity].ElementAt(modRef.index).data = AttributeModInternal.Null;
        //     request.modRefPointer.Dispose();
        //     att.ValueRW.modsChanged = true;
        // }
        
        // [BurstDiscard]
        // public static AttributeType AttributeTypeToEnum<TAtt>() where TAtt : unmanaged, IAttribute {
        //     return typeof(TAtt).Name switch {
        //         "AttHealthBase" => AttributeType.health,
        //         _ => throw new ArgumentOutOfRangeException()
        //     };
        // }
        //
        // [BurstDiscard]
        // public static AttributeModType AttributeModTypeToEnum<TModType>() where TModType : unmanaged, IAttributeModType {
        //     return typeof(TModType).Name switch {
        //         "AttributeModTypeAdd" => AttributeModType.addition,
        //         "AttributeModTypeMul" => AttributeModType.multiplier,
        //         "AttributeModTypeExp" => AttributeModType.exponent,
        //         _ => throw new ArgumentOutOfRangeException()
        //     };
        // }

        
        // [BurstCompile]
        // public static void SquashList<T>(this in NativeList<T> list) where T : unmanaged {
        //     for (int i = list.Length - 1; i >= 0; i--) {
        //         if (list[i].Equals(null)) { continue; }
        //         list.SetCapacity(i + 1);
        //         return;
        //     }
        // }

        public static float InversePow(in float originalResult, in float originalExponent) {
            return math.pow(originalResult, 1.0f / originalExponent);
        }
        public static double InversePow(in double originalResult, in double originalExponent) {
            return math.pow(originalResult, 1.0d / originalExponent);
        }
    }

    #if UNITY_EDITOR
    namespace Editor {
        
    }
    #endif
}