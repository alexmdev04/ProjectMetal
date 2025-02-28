using Unity.Entities;
using Unity.Mathematics;

namespace Metal.Components {
    public struct Vehicle : IComponentData {
        public float 
            restLength,
            springTravel,
            wheelRadius,
            springStiffness,
            damperStiffness,
            accelerationForce,
            maxSpeed,
            movementDeadzone,
            steerStrength,
            dragCoefficient,
            turningCurve;

        public bool
            enableSuspension, 
            enableAcceleration, 
            enableSteering, 
            enableDrag;

        public float3 accelerationPointOffset;
        //private AnimationCurve turningCurve;
    }
}