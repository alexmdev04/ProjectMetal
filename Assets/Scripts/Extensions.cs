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
        public static void ToString(in AttributeManagerRequestType type, out FixedString32Bytes name) => name = type switch {
            AttributeManagerRequestType.add => (FixedString32Bytes)"Add",
            AttributeManagerRequestType.edit => (FixedString32Bytes)"Edit",
            AttributeManagerRequestType.remove => (FixedString32Bytes)"Remove",
            _ => (FixedString32Bytes)"Unknown AttributeManagerRequestType"
        };
        
        [BurstCompile]
        public static void ToString(in AttributeType type, out FixedString32Bytes name) => name = type switch {
            AttributeType.none => (FixedString32Bytes)"None",
            AttributeType.health => (FixedString32Bytes)"Health",
            AttributeType.damage => (FixedString32Bytes)"Damage",
            AttributeType.fireRate => (FixedString32Bytes)"FireRate",
            AttributeType.cooldownRate => (FixedString32Bytes)"CooldownRate",
            AttributeType.accelerationSpeed => (FixedString32Bytes)"AccelerationSpeed",
            _ => (FixedString32Bytes)"Unknown AttributeType"
        };
        
        [BurstCompile]
        public static void ToString(in AttributeModType type, out FixedString32Bytes name) => name = type switch {
            AttributeModType.addition => (FixedString32Bytes)"Addition",
            AttributeModType.multiplier => (FixedString32Bytes)"Multiplier",
            AttributeModType.exponent => (FixedString32Bytes)"Exponent",
            _ => (FixedString32Bytes)"Unknown AttributeModType"
        };

        [BurstCompile]
        public static void Tick<TAtt, TModType>(this in DynamicBuffer<AttributeMod<TAtt, TModType>> modifierList, RefRW<TAtt> att, in float delta) 
            where TAtt : unmanaged, IAttribute
            where TModType : unmanaged, IAttributeModType {
            
            DynamicBuffer<AttributeMod<TAtt, TModType>> attributeMods = modifierList;
            int length = attributeMods.Length, 
                i = 0;
            while (i < length) {
                var mod = attributeMods[i];
                mod.data.Tick(delta);
                if (mod.data.dead) {
                    attributeMods.RemoveAtSwapBack(i);
                    length--;
                    att.ValueRW.modsChanged = true;
                }
                else {
                    attributeMods[i] = mod;
                    i++;
                }
            }
        }

        public static void AttributeManagerRequest(
            this ref EntityCommandBuffer.ParallelWriter ecb,
            in int index,
            in AttributeManagerRequest request) {
            request.ProcessByType(ref ecb, index);
        }

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