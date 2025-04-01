using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Metal {
    public enum AttributeType {
        none,
        health,
        damage,
        fireRate,
        cooldownRate,
        accelerationSpeed,
    }
    
    public enum AttributeModType {
        addition,
        multiplier,
        exponent,
    }

    [BurstCompile]
    public struct AttributeConstructor{
        public AttributeType type;
        public double baseValue;
        public double minModValue;
        public double maxModValue;
        public NativeArray<AttributeModConstructor>? modConstructors;

        public AttributeConstructor(
            in AttributeType type,
            in double baseValue,
            in double minModValue = double.MinValue,
            in double maxModValue = double.MaxValue) {
            this.type = type;
            this.baseValue = baseValue;
            this.minModValue = minModValue;
            this.maxModValue = maxModValue;
            this.modConstructors = null;
        }
        public AttributeConstructor(
            in AttributeType type,
            in double baseValue,
            in NativeArray<AttributeModConstructor> modConstructors,
            in double minModValue = double.MinValue,
            in double maxModValue = double.MaxValue) {
            this.type = type;
            this.baseValue = baseValue;
            this.minModValue = minModValue;
            this.maxModValue = maxModValue;
            this.modConstructors = modConstructors;
        }
    }
    
    // this just makes passing these around way cleaner since the type is so long
    [BurstCompile]
    public struct AttributeModSet<TAtt> where TAtt : unmanaged, IAttribute {
        public DynamicBuffer<AttributeMod<TAtt, AttributeModTypeAdd>> add;
        public DynamicBuffer<AttributeMod<TAtt, AttributeModTypeMul>> mul;
        public DynamicBuffer<AttributeMod<TAtt, AttributeModTypeExp>> exp;
        public AttributeModSet(
        DynamicBuffer<AttributeMod<TAtt, AttributeModTypeAdd>> add,
        DynamicBuffer<AttributeMod<TAtt, AttributeModTypeMul>> mul,
        DynamicBuffer<AttributeMod<TAtt, AttributeModTypeExp>> exp) {
            this.add = add;
            this.mul = mul;
            this.exp = exp;
        }
        [BurstCompile]
        public void Tick(in float delta, RefRW<TAtt> att) {
            add.Tick(att, delta);
            mul.Tick(att, delta);
            exp.Tick(att, delta);
        }
        [BurstCompile]
        public void CalculateModValue(in double baseValue, out double value) {
            value = baseValue;
            foreach (var mod in add) { value += mod.data.value; } 
            foreach (var mod in mul) { value *= mod.data.value; } 
            foreach (var mod in exp) { value = math.pow(value, mod.data.value); }
        }
        [BurstCompile]
        public void CalculateModValue(in TAtt att, out double value) {
            value = att.att.baseValue;
            foreach (var mod in add) { value += mod.data.value; } 
            foreach (var mod in mul) { value *= mod.data.value; } 
            foreach (var mod in exp) { value = math.pow(value, mod.data.value); }
        }
        [BurstCompile]
        public void CalculateBaseValue(in double modValue, out double baseValue) {
            baseValue = modValue;
            foreach (var mod in exp) { baseValue = Extensions.InversePow(modValue, mod.data.value); }
            foreach (var mod in mul) { baseValue /= mod.data.value; } 
            foreach (var mod in add) { baseValue -= mod.data.value; } 
        }
        [BurstCompile]
        public void CalculateBaseValue<TModAtt>(in TModAtt modAtt, out double baseValue)
            where TModAtt : unmanaged, IAttributeModValue {
                
            baseValue = modAtt.value;
            foreach (var mod in exp) { baseValue = Extensions.InversePow(modAtt.value, mod.data.value); }
            foreach (var mod in mul) { baseValue /= mod.data.value; } 
            foreach (var mod in add) { baseValue -= mod.data.value; } 
        }
    }

    [BurstCompile]
    public struct AttributeModConstructor {
        public AttributeType attType;
        public AttributeModType modType;
        public double value;
        public bool expires;
        public float lifeTime;
        public uint id;

        public AttributeModConstructor(
            in AttributeType attType,
            in AttributeModType modType,
            in double value) {
            this.attType = attType;
            this.modType = modType;
            this.value = value;
            expires = false;
            lifeTime = 0.0f;
            id = 0;
        }
        public AttributeModConstructor(
            in AttributeType attType,
            in AttributeModType modType,
            in double value,
            in float lifeTime) {
            this.attType = attType;
            this.modType = modType;
            this.value = value;
            expires = true;
            this.lifeTime = lifeTime;
            id = 0;
        }
    }

    [BurstCompile]
    public struct AttributeModRef {
        public Entity entity;
        public AttributeType attributeType;
        public AttributeModType modType;
        public int index;
    }
    
    [BurstCompile]
    [Serializable]
    public struct AttributeInternal {
        public double baseValue;
        public double maxModValue;
        public double minModValue;

        public AttributeInternal(in double baseValue, in double minModValue = double.MinValue, in double maxModValue = double.MaxValue) {
            this.baseValue = baseValue;
            this.minModValue = minModValue;
            this.maxModValue = maxModValue;
        }
    }
    
    [BurstCompile]
    [Serializable]
    public struct AttributeModInternal {
        public double value;
        public bool expires;
        public bool dead;
        public float lifeTime;

        public static readonly AttributeModInternal Null = new() {
            value = 0.0d,
            expires = false,
            dead = true,
            lifeTime = 0.0f
        };
            
        public AttributeModInternal(
            in double value,
            in bool expires = false,
            in float lifeTime = 0.0f) {
            this.expires = expires;
            this.lifeTime = lifeTime;
            this.value = value;
            dead = false;
        }

        public AttributeModInternal(AttributeModConstructor constructor) {
            this.value = constructor.value;
            this.expires = constructor.expires;
            this.lifeTime = constructor.lifeTime;
            dead = false;
        }
            
        [BurstCompile]
        public void Tick(in float delta) {
            if (expires && !dead) { lifeTime -= delta; dead = lifeTime <= 0.0f; }
            else { dead = false; }
        }
    }
    
    #region Attribute Mods and Mod Types
    [BurstCompile]
    public struct AttributeMod<TAtt, TModType> : IBufferElementData
        where TAtt : unmanaged, IAttribute 
        where TModType : unmanaged, IAttributeModType {
        public AttributeModInternal data;

        public AttributeMod(AttributeModConstructor constructor) {
            data = new AttributeModInternal(constructor);
        }
    }
        
    [RequireImplementors]
    public interface IAttributeModType { }
    [BurstCompile]
    public struct AttributeModTypeAdd : IAttributeModType { }
    [BurstCompile]
    public struct AttributeModTypeMul : IAttributeModType { }
    [BurstCompile]
    public struct AttributeModTypeExp : IAttributeModType { }
    #endregion
    
    public unsafe struct AttributeModRefPointer {
        private AttributeModRef* data;
        
        [BurstDiscard]
        public void Allocate(out AttributeModRefPointer pointer) {
            data = (AttributeModRef*)Marshal.AllocHGlobal(sizeof(AttributeModRef));
            *data = new AttributeModRef() {
                entity = Entity.Null,
                index = -1,
                attributeType = AttributeType.none,
                modType = (AttributeModType)(-1)
            };
            pointer = this;
        }
        
        [BurstDiscard]
        public void Allocate(in AttributeModRef modRef, out AttributeModRefPointer pointer) {
            data = (AttributeModRef*)Marshal.AllocHGlobal(sizeof(AttributeModRef));
            *data = modRef;
            pointer = this;
        }

        [BurstDiscard]
        public void Dispose() {
            if (data == null) return;
            Marshal.FreeHGlobal((IntPtr)data);
            data = null;
        }
        
        public void Get(out AttributeModRef _data) {
            if (data == null) throw new NullReferenceException();
            _data = *data;
        }

        public void Set(in AttributeModRef _data) {
            if (data == null) throw new NullReferenceException();
            *data = _data;
        }
    }

    #region Interfaces

    [RequireImplementors]
    public interface IAttribute : IComponentData {
        public AttributeInternal att { get; set; }
        public bool modsChanged { get; set; }
        public void SetBaseValue(in double value) {
            var @internal = att;
            @internal.baseValue = value;
            att = @internal;
        }
    }
    [RequireImplementors]
    public interface IAttributeModValue : IComponentData {
        public double value { get; set; }
        /// <summary>
        /// This value is only changed with AttributeRequests, its also clamped to ModValue.value when its owner's Attribute.modsChanged == true 
        /// </summary>
        public double valueStatic { get; set; }
    }
    #endregion
    
    namespace Components {
        #region Base Attributes
        
        [BurstCompile]
        public struct AttHealthBase : IAttribute {
            [field: SerializeField] public AttributeInternal att { get; set; }
            [field: SerializeField] public bool modsChanged { get; set; }
        }
        [BurstCompile]
        public struct AttDamageBase : IAttribute {
            [field: SerializeField] public AttributeInternal att { get; set; }
            [field: SerializeField] public bool modsChanged { get; set; }
        }
        [BurstCompile]
        public struct AttFireRateBase : IAttribute {
            [field: SerializeField] public AttributeInternal att { get; set; }
            [field: SerializeField] public bool modsChanged { get; set; }
        }
        [BurstCompile]
        public struct AttCooldownRateBase : IAttribute {
            [field: SerializeField] public AttributeInternal att { get; set; }
            [field: SerializeField] public bool modsChanged { get; set; }
        }
        [BurstCompile]
        public struct AttAccelerationSpeedBase : IAttribute {
            [field: SerializeField] public AttributeInternal att { get; set; }
            [field: SerializeField] public bool modsChanged { get; set; }
        }
        #endregion
        
        #region Modified Attributes

        
        [BurstCompile]
        public struct AttHealthModValue : IAttributeModValue {
            [field: SerializeField] public double value { get; set; }
            [field: SerializeField] public double valueStatic { get; set; }
        }
        [BurstCompile]
        public struct AttDamageModValue : IAttributeModValue {
            [field: SerializeField] public double value { get; set; }
            [field: SerializeField] public double valueStatic { get; set; }
        }
        [BurstCompile]
        public struct AttFireRateModValue : IAttributeModValue {
            [field: SerializeField] public double value { get; set; }
            [field: SerializeField] public double valueStatic { get; set; }
        }
        [BurstCompile]
        public struct AttCooldownRateModValue : IAttributeModValue {
            [field: SerializeField] public double value { get; set; }
            [field: SerializeField] public double valueStatic { get; set; }
        }
        [BurstCompile]
        public struct AttAccelerationSpeedModValue : IAttributeModValue {
            [field: SerializeField] public double value { get; set; }
            [field: SerializeField] public double valueStatic { get; set; }
        }
        #endregion
    }
}