using UnityEngine;

namespace Metal.Config {
    [CreateAssetMenu(fileName = "StartupSettings", menuName = "Scriptable Objects/Startup Settings", order = 1)]
    public class StartupSettings : ScriptableObject {
        public bool skipToGame;
    }
}