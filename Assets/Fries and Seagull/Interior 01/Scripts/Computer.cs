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
    public class Computer : TurnOnAble {
        [YureiButton("Turn On")] public UnityEvent onTurnOn;
        [YureiButton("Turn Off")] public UnityEvent onTurnOff;
        
        [SerializeField] private MeshRenderer lightBoxRenderer;
        
        public void turnOn() {
            lightBoxRenderer.material.color = new Color(230/255f, 49/255f, 55/255f, 0);
            lightBoxRenderer.material.EnableKeyword("_EMISSION");
        }

        public void turnOff() {
            lightBoxRenderer.material.color = new Color(22/255f,22/255f,22/255f,1);
            lightBoxRenderer.material.DisableKeyword("_EMISSION");
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(Computer))]
    public class ComputerInspector : YureiInspector { }
    #endif
}
