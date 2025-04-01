using Unity.Collections;
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
            turningCurveValue;
        
        public bool
            enableSuspension, 
            enableAcceleration, 
            enableSteering, 
            enableDrag;

        public Entity weaponMountEntity;

        public BlobAssetReference<FloatArrayBlob> turningCurve;
        
        public float3 accelerationPointOffset;
        
        //public NativeArray<float3> wheelPositions;
    }
}

public struct FloatArrayBlob {
    public BlobArray<float> value;
}
