using System;
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
            foreach (var mod in add) { value += mod.mod.value; } 
            foreach (var mod in mul) { value *= mod.mod.value; } 
            foreach (var mod in exp) { value = math.pow(value, mod.mod.value); }
        }
        public void CalculateModValue(in TAtt att, out double value) {
            value = att.att.baseValue;
            foreach (var mod in add) { value += mod.mod.value; } 
            foreach (var mod in mul) { value *= mod.mod.value; } 
            foreach (var mod in exp) { value = math.pow(value, mod.mod.value); }
        }
        public void CalculateBaseValue(in double modValue, out double baseValue) {
            baseValue = modValue;
            foreach (var mod in exp) { baseValue = Extensions.InversePow(modValue, mod.mod.value); }
            foreach (var mod in mul) { baseValue /= mod.mod.value; } 
            foreach (var mod in add) { baseValue -= mod.mod.value; } 
        }
        public void CalculateBaseValue<TModAtt>(in TModAtt modAtt, out double baseValue)
            where TModAtt : unmanaged, Components.IAttributeModValue {
                
            baseValue = modAtt.maxValue;
            foreach (var mod in exp) { baseValue = Extensions.InversePow(modAtt.maxValue, mod.mod.value); }
            foreach (var mod in mul) { baseValue /= mod.mod.value; } 
            foreach (var mod in add) { baseValue -= mod.mod.value; } 
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
            
            // public void AddModifier(in AttributeModInternal mod, in AttributeModType type, out int modIndex);
            // public void _AddModifier(in AttributeModInternal mod, in AttributeModType type, out int modIndex) {
            //     _GetListFromType(type, out NativeList<AttributeModInternal> modList);
            //     modIndex = modList.Length;
            //     modList.Add(mod);
            //     isDirty = true;
            // }
            //
            // public void RemoveModifier(in AttributeModType type, in int modIndex);
            // public void _RemoveModifier(in AttributeModType type, in int modIndex) {
            //     _GetListFromType(type, out NativeList<AttributeModInternal> modList);
            //     modList[modIndex] = modList[^1];
            //     modList.TrimExcess();
            //     isDirty = true;
            // }
            //
            // public void GetListFromType(in AttributeModType type, out NativeList<AttributeModInternal> modList);
            // public void _GetListFromType(in AttributeModType type, out NativeList<AttributeModInternal> modList) {
            //     modList = type switch {
            //         AttributeModType.addition => att.addMods,
            //         AttributeModType.multiplier => att.mulMods,
            //         AttributeModType.exponent => att.expMods,
            //         _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            //     };
            // }
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
            
            // public void AddModifier(in AttributeModInternal mod, in AttributeModType type, out int modIndex) {
            //     (this as IAttribute)._AddModifier(mod, type, out modIndex);
            // }
            // public void RemoveModifier(in AttributeModType type, in int modIndex) {
            //     (this as IAttribute)._RemoveModifier(type, modIndex);
            // }
            // public void GetListFromType(in AttributeModType type, out NativeList<AttributeModInternal> modList) {
            //     (this as IAttribute)._GetListFromType(type, out modList);
            // }
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
            public uint id;
            public float lifeTime;

            public AttributeModInternal(
                in uint id,
                in double value,
                in bool expires = false,
                in float lifeTime = 0.0f) {
                this.id = id;
                this.expires = expires;
                this.lifeTime = lifeTime;
                this.value = value;
                dead = false;
            }

            public AttributeModInternal(AttributeModConstructor constructor) {
                this.id = constructor.id;
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
        #endregion

        #region Attribute Mod Values and Mods
        public struct AttributeMod<TAtt, TModType> : IBufferElementData
            where TAtt : unmanaged, IAttribute 
            where TModType : unmanaged, IAttributeModType {
            public AttributeModInternal mod;

            public AttributeMod(AttributeModConstructor constructor) {
                mod = new AttributeModInternal(constructor);
            }
        }
        
        [RequireImplementors]
        public interface IAttributeModType { }
        public struct AttributeModTypeAdd : IAttributeModType { }
        public struct AttributeModTypeMul : IAttributeModType { }
        public struct AttributeModTypeExp : IAttributeModType { }
        #endregion
    }
}