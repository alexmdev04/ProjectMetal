using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Physics.Extensions;

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
    }
}