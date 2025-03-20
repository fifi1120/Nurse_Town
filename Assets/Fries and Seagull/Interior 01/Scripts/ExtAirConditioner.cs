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
    public class ExtAirConditioner : TurnOnAble {
        [YureiButton("Turn On")] public UnityEvent onTurnOn;
        [YureiButton("Turn Off")] public UnityEvent onTurnOff;
        
        [SerializeField] private ParticleSystem windParticleSystem;
        [SerializeField] private Animator fanAnimator;
        
        public void turnOn() {
            ParticleSystem.EmissionModule em = windParticleSystem.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(2f, 1f);
            fanAnimator.SetBool("Status", true);
        }

        public void turnOff() {
            ParticleSystem.EmissionModule em = windParticleSystem.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(0, 0);
            fanAnimator.SetBool("Status", false);
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(ExtAirConditioner))]
    public class ExtAirConditionerInspector : YureiInspector { }
    #endif
}