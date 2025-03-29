using System;
using System.Runtime.InteropServices;
using Metal.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Metal {
    public static class Attributes<TAtt> where TAtt : unmanaged, Components.IAttribute {





    }
    
    public enum AttributeType {
        none,
        health,
        damage,
        fireRate,
        cooldownRate,
        accelerationSpeed,
        traction
    }
    
    public enum AttributeModType {
        addition,
        multiplier,
        exponent,
    }

    public struct AttributeConstructor{
        public AttributeType type;
        public double baseValue;
        public double minModValue;
        public double maxModValue;
        public NativeArray<AttributeModConstructor>? modConstructors;

        public AttributeConstructor(
            in AttributeType type,
            in double baseValue,
            in double minModValue = Double.MinValue,
            in double maxModValue = Double.MaxValue) {
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
            in double minModValue = Double.MinValue,
            in double maxModValue = Double.MaxValue) {
            this.type = type;
            this.baseValue = baseValue;
            this.minModValue = minModValue;
            this.maxModValue = maxModValue;
            this.modConstructors = modConstructors;
        }
    }
    
    // this just makes passing these around way cleaner since the type is so long
    public struct AttributeModSet<TAtt> where TAtt : unmanaged, Components.IAttribute {
        public DynamicBuffer<Components.AttributeMod<TAtt, Components.AttributeModTypeAdd>> add;
        public DynamicBuffer<Components.AttributeMod<TAtt, Components.AttributeModTypeMul>> mul;
        public DynamicBuffer<Components.AttributeMod<TAtt, Components.AttributeModTypeExp>> exp;
        public AttributeModSet(
        DynamicBuffer<Components.AttributeMod<TAtt, Components.AttributeModTypeAdd>> add,
        DynamicBuffer<Components.AttributeMod<TAtt, Components.AttributeModTypeMul>> mul,
        DynamicBuffer<Components.AttributeMod<TAtt, Components.AttributeModTypeExp>> exp) {
            this.add = add;
            this.mul = mul;
            this.exp = exp;
        }
        
        public void Tick(in float delta) {
            add.Tick(delta);
            mul.Tick(delta);
            exp.Tick(delta);
        }
        public void CalculateModValue(in double baseValue, out double value) {
            value = baseValue;
            foreach (var mod in add) { value += mod.data.value; } 
            foreach (var mod in mul) { value *= mod.data.value; } 
            foreach (var mod in exp) { value = math.pow(value, mod.data.value); }
        }
        public void CalculateModValue(in TAtt att, out double value) {
            value = att.att.baseValue;
            foreach (var mod in add) { value += mod.data.value; } 
            foreach (var mod in mul) { value *= mod.data.value; } 
            foreach (var mod in exp) { value = math.pow(value, mod.data.value); }
        }
        public void CalculateBaseValue(in double modValue, out double baseValue) {
            baseValue = modValue;
            foreach (var mod in exp) { baseValue = Extensions.InversePow(modValue, mod.data.value); }
            foreach (var mod in mul) { baseValue /= mod.data.value; } 
            foreach (var mod in add) { baseValue -= mod.data.value; } 
        }
        public void CalculateBaseValue<TModAtt>(in TModAtt modAtt, out double baseValue)
            where TModAtt : unmanaged, Components.IAttributeModValue {
                
            baseValue = modAtt.maxValue;
            foreach (var mod in exp) { baseValue = Extensions.InversePow(modAtt.maxValue, mod.data.value); }
            foreach (var mod in mul) { baseValue /= mod.data.value; } 
            foreach (var mod in add) { baseValue -= mod.data.value; } 
        }
    }

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

    public struct AttributeModRef {
        public Entity entity;
        public AttributeType attributeType;
        public AttributeModType modType;
        public int index;
    }
    
    namespace Components {
        #region Base Attributes
        
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
        
        [Serializable]
        public struct AttributeInternal {
            public double baseValue;
            public double maxModValue;
            public double minModValue;

            public AttributeInternal(in double baseValue, in double minModValue = Double.MinValue, in double maxModValue = Double.MaxValue) {
                this.baseValue = baseValue;
                this.minModValue = minModValue;
                this.maxModValue = maxModValue;
            }
        }
        
        public struct AttHealthBase : IAttribute {
            [field: SerializeField] public AttributeInternal att { get; set; }
            [field: SerializeField] public bool modsChanged { get; set; }
        }
        public struct AttDamageBase : IAttribute {
            [field: SerializeField] public AttributeInternal att { get; set; }
            [field: SerializeField] public bool modsChanged { get; set; }
        }
        public struct AttFireRateBase : IAttribute {
            [field: SerializeField] public AttributeInternal att { get; set; }
            [field: SerializeField] public bool modsChanged { get; set; }
        }
        public struct AttCooldownRateBase : IAttribute {
            [field: SerializeField] public AttributeInternal att { get; set; }
            [field: SerializeField] public bool modsChanged { get; set; }
        }
        public struct AttAccelerationSpeedBase : IAttribute {
            [field: SerializeField] public AttributeInternal att { get; set; }
            [field: SerializeField] public bool modsChanged { get; set; }
        }
        public struct AttTractionBase : IAttribute {
            [field: SerializeField] public AttributeInternal att { get; set; }
            [field: SerializeField] public bool modsChanged { get; set; }
        }
        #endregion
        
        #region Modified Attributes
        [RequireImplementors]
        public interface IAttributeModValue : IComponentData {
            public double maxValue { get; set; }
            public double value { get; set; }
        }
        
        [Serializable]
        public struct AttributeModInternal {
            public double value;
            public bool expires;
            public bool dead;
            public float lifeTime;

            public static AttributeModInternal Null = new() {
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
            
            public void Tick(in float delta) {
                if (expires) { lifeTime += delta; dead = lifeTime <= 0.0f; }
                dead = false;
            }
        }
        
        public struct AttHealthModValue : IAttributeModValue {
            [field: SerializeField] public double maxValue { get; set; }
            [field: SerializeField] public double value { get; set; }
        }
        public struct AttDamageModValue : IAttributeModValue {
            [field: SerializeField] public double maxValue { get; set; }
            [field: SerializeField] public double value { get; set; }
        }
        public struct AttFireRateModValue : IAttributeModValue {
            [field: SerializeField] public double maxValue { get; set; }
            [field: SerializeField] public double value { get; set; }
        }
        public struct AttCooldownRateModValue : IAttributeModValue {
            [field: SerializeField] public double maxValue { get; set; }
            [field: SerializeField] public double value { get; set; }
        }
        public struct AttAccelerationSpeedModValue : IAttributeModValue {
            [field: SerializeField] public double maxValue { get; set; }
            [field: SerializeField] public double value { get; set; }
        }
        public struct AttTractionModValue : IAttributeModValue {
            [field: SerializeField] public double maxValue { get; set; }
            [field: SerializeField] public double value { get; set; }
        }
        
        #endregion

        #region Attribute Mod Values and Mods
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
        public struct AttributeModTypeAdd : IAttributeModType { }
        public struct AttributeModTypeMul : IAttributeModType { }
        public struct AttributeModTypeExp : IAttributeModType { }
        #endregion
    }
    
    public unsafe struct AttributeModRefPointer {
        private AttributeModRef* data;
        
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
        
        public void Allocate(in AttributeModRef modRef, out AttributeModRefPointer pointer) {
            data = (AttributeModRef*)Marshal.AllocHGlobal(sizeof(AttributeModRef));
            *data = modRef;
            pointer = this;
        }

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
}