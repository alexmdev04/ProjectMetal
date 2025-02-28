using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Metal {
    namespace Settings {
        [CreateAssetMenu(fileName = "VehicleAssets", menuName = "Scriptable Objects/Vehicle Assets Dictionary")]
        public class VehicleAssets : ScriptableObject {
            [SerializeField] private Editor.VehicleAssetDict assets = new();
            public Dictionary<Authoring.Vehicle.VehicleType, GameObject> dict;

            void OnEnable() { // populates the dictionary with inspector values
                dict = new();
                foreach (var vehicleAsset in assets.entries) {
                    dict.Add(vehicleAsset.type, vehicleAsset.prefab);
                }
            }
        }
    }

    namespace Editor {
        [Serializable]
        public class VehicleAssetDict {
            public VehicleAsset[] entries;
        }

        [Serializable]
        public struct VehicleAsset {
            public Authoring.Vehicle.VehicleType type;
            public GameObject prefab;
        }
    }
}
