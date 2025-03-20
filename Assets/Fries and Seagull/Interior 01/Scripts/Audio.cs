using System;
using Seagull.Interior_01.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using Seagull.Interior_01.Utility.Inspector;
#endif

#if UNITY_EDITOR
#endif

namespace Seagull.Interior_01 {
    public class Audio : TurnOnAble {
        
        [YureiButton("Turn On")] public UnityEvent onTurnOn;
        [YureiButton("Turn Off")] public UnityEvent onTurnOff;
        
        [SerializeField] private MeshRenderer lightBoxRenderer;
        
        public void turnOn() {
            lightBoxRenderer.material.color = new Color(110/255f, 110/255f, 1, 0);
            lightBoxRenderer.material.EnableKeyword("_EMISSION");
        }

        public void turnOff() {
            lightBoxRenderer.material.color = new Color(22/255f,22/255f,22/255f,1);
            lightBoxRenderer.material.DisableKeyword("_EMISSION");
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(Audio))]
    public class AudioInspector : YureiInspector { }
    #endif
}
