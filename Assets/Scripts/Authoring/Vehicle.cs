using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Authoring;
using Unity.Transforms;
using UnityEngine;

namespace Metal.Authoring {
    public class Vehicle : MonoBehaviour {
        [SerializeField] public Settings.VehicleAssets vehicleAssets;
        public enum VehicleType {
            v207,
            v505,
            fiorino,
            hilux,
            meriva,
            traffic
        }
        public VehicleType vehicleType;
        public Transform[]
            wheelTransforms;
        public float restLength = 1f;
        public float springTravel = 0.5f;
        public float wheelRadius = 0.33f;
        public float springStiffness = 30000.0f;
        public float damperStiffness = 3000.0f;
        public float accelerationForce = 25.0f;
        public float maxSpeed = 100.0f;
        public float decelerationForce = 10.0f;
        public Transform accelerationPoint;
        public float movementDeadzone = 0.1f;
        public float steerStrength = 30.0f;
        public AnimationCurve turningCurve;
        public float turningCurveValue = 1.0f;
        public float dragCoefficient = 1.0f;
        
    }

    [InternalBufferCapacity(4)]
    public struct WheelEntity : IBufferElementData {
        public Entity value;
    }
    
    public class VehicleBaker : Baker<Vehicle> {
        public override void Bake(Vehicle authoring) {
            Entity vehicle = GetEntity(TransformUsageFlags.Dynamic);
            Entity vehicleAsset = GetEntity(authoring.vehicleAssets.dict[authoring.vehicleType], TransformUsageFlags.Dynamic);
            AddComponent<Components.Movement>(vehicle);
            AddComponent(vehicle, new Components.Vehicle {
                restLength = authoring.restLength,
                springTravel = authoring.springTravel,
                wheelRadius = authoring.wheelRadius,
                springStiffness = authoring.springStiffness,
                damperStiffness = authoring.damperStiffness,
                accelerationForce = authoring.accelerationForce,
                maxSpeed = authoring.maxSpeed,
                decelerationForce = authoring.decelerationForce,
                movementDeadzone = authoring.movementDeadzone,
                steerStrength = authoring.steerStrength,
                dragCoefficient = authoring.dragCoefficient,
                accelerationPointOffset = authoring.accelerationPoint.position,
                enableAcceleration = true,
                enableSteering = true,
                enableDrag = true,
                enableSuspension = true,
                enableForceAngular = true,
                enableForceLinear = true,
                turningCurve = authoring.turningCurveValue,
                suspensionMultiplier = 1.0f
            });
            
            DynamicBuffer<WheelEntity> wheelEntities = AddBuffer<WheelEntity>(vehicle);
            wheelEntities.EnsureCapacity(authoring.wheelTransforms.Length);
            
            foreach (Transform wheelTransform in authoring.wheelTransforms) {
                //Entity wheelEntity = ;
                //AddComponent<Components.Wheel>(wheelEntity);
                wheelEntities.Add(new WheelEntity { value = GetEntity(wheelTransform, TransformUsageFlags.Dynamic) });
            }
        }
    }
}