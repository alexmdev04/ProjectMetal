using UnityEngine;

namespace Metal.Settings {
    [CreateAssetMenu(fileName = "StartupSettings", menuName = "Scriptable Objects/Startup Settings")]
    public class StartupSettings : ScriptableObject {
        public bool skipToGame;
    }
}