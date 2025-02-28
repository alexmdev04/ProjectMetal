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
        /// <param name="lv">Linear Velocity</param>
        /// <param name="av">Angular Velocity</param>
        /// <param name="com">Center of Mass</param>
        /// <param name="worldPos">Point World-Space Position</param>
        /// <param name="result">The resulting velocity</param>
        [BurstCompile]
        public static void GetPointVelocity(in float3 lv, in float3 av, in float3 com, in float3 worldPos, out float3 result) {
            result = lv + math.cross(av, worldPos - com);
        }
        
        /// <summary>
        /// Applies a force to a rigidbody at a position in world space
        /// </summary>
        /// <param name="velocity">Velocity Component that will receive the force</param>
        /// <param name="bodyPosition">World-space position of the target rigidbody</param>
        /// <param name="inverseMass">Inverse mass of the target rigidbody (usually found in PhysicsMass.InverseMass)</param>
        /// <param name="inverseInertia">Inverse inertia tensor of the target rigidbody (usually found in PhysicsMass.InverseInertia)</param>
        /// <param name="force">Force to apply</param>
        /// <param name="position">World-space position of the force origin</param>
        [BurstCompile]
        public static void AddForceAtPosition(
            ref PhysicsVelocity velocity, 
            in float3 bodyPosition,
            in float inverseMass,
            in float3 inverseInertia,
            in float3 force,
            in float3 position) {
            if (inverseMass <= float.Epsilon) { return; }
            
            // torque = math.cross(position - com, force)
            // add force, add torque
            
            velocity.Linear += force * inverseMass;
            velocity.Angular += math.cross(position - bodyPosition, force) * inverseInertia;
        }
    }
}