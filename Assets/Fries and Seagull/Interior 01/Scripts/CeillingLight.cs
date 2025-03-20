#if UNITY_EDITOR
using Seagull.Interior_01.Utility.Inspector;
#endif

using Seagull.Interior_01.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
#endif

namespace Seagull.Interior_01 {
    public class CeilingLight : TurnOnAble {
        [YureiButton("Turn On")] public UnityEvent onTurnOn;
        [YureiButton("Turn Off")] public UnityEvent onTurnOff;
        
        [SerializeField] private MeshRenderer lightBoxRenderer;
        
        public void turnOn() {
            lightBoxRenderer.material.EnableKeyword("_EMISSION");
        }

        public void turnOff() {
            lightBoxRenderer.material.DisableKeyword("_EMISSION");
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(CeilingLight))]
    public class CeilingLightInspector : YureiInspector { }
    #endif
}
