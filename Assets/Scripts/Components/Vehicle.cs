using Unity.Entities;
using Unity.Mathematics;

namespace Metal.Components {
    public struct Vehicle : IComponentData {
        public float restLength,
            springTravel,
            wheelRadius,
            springStiffness,
            damperStiffness,
            accelerationForce,
            maxSpeed,
            decelerationForce,
            movementDeadzone,
            steerStrength,
            dragCoefficient,
            turningCurve;

        public float suspensionMultiplier;
        public bool enableForceLinear;
        public bool enableForceAngular;
        public bool enableSuspension;
        public bool enableAcceleration;
        public bool enableSteering;
        public bool enableDrag;
        
        public float3 accelerationPointOffset;
        //private AnimationCurve turningCurve;
    }
}