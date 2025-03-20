using Seagull.Interior_01.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using Seagull.Interior_01.Utility.Inspector;
#endif

namespace Seagull.Interior_01 {
    public class AirConditioner : TurnOnAble {
        [YureiButton("Turn On")] public UnityEvent onTurnOn;
        [YureiButton("Turn Off")] public UnityEvent onTurnOff;
        
        [SerializeField] private ParticleSystem windParticleSystem;
        [SerializeField] private MeshRenderer lightBoxRenderer;
        
        public void turnOn() {
            ParticleSystem.EmissionModule em = windParticleSystem.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(1.5f, 0.025f);
            lightBoxRenderer.material.color = new Color(96/255f, 109/255f, 1, 0);
            lightBoxRenderer.material.EnableKeyword("_EMISSION");
        }

        public void turnOff() {
            ParticleSystem.EmissionModule em = windParticleSystem.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(0, 0);
            lightBoxRenderer.material.color = new Color(41/255f,41/255f,41/255f,1);
            lightBoxRenderer.material.DisableKeyword("_EMISSION");
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(AirConditioner))]
    public class AirConditionerInspector : YureiInspector { }
    #endif
}
