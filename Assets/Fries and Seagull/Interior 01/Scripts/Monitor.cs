#if UNITY_EDITOR
using Seagull.Interior_01.Utility.Inspector;
#endif

using System.Collections;
using Seagull.Interior_01.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
#endif

namespace Seagull.Interior_01 {
    public class Monitor : TurnOnAble {
        [Tooltip("How many seconds it takes to boost the monitor")]
        public int boostSeconds = 2;
        
        [YureiButton("Turn On")] public UnityEvent onTurnOn;
        [YureiButton("Turn Off")] public UnityEvent onTurnOff;
        
        private int turnOnCount = 0;
        private bool isInterrupted = false;
        
        [SerializeField] private MeshRenderer indicatorLightBoxRenderer;
        [SerializeField] private MeshRenderer spotLightBoxRenderer;
        [SerializeField] private GameObject spotLight;
        [SerializeField] private GameObject screenQuad;
        
        public void turnOn() {
            spotLightBoxRenderer.material.color = new Color(159 / 255f, 117 / 255f, 248 / 255f, 1);
            spotLightBoxRenderer.material.EnableKeyword("_EMISSION");
            indicatorLightBoxRenderer.material.color = new Color(115/255f, 94/255f, 188/255f, 1);
            indicatorLightBoxRenderer.material.EnableKeyword("_EMISSION");
            spotLight.SetActive(true);
            turnOnCount++;
            StartCoroutine(boost(turnOnCount, boostSeconds));
            isInterrupted = false;
        }

        public void turnOff() {
            spotLightBoxRenderer.material.color = new Color(118 / 255f, 118 / 255f, 118 / 255f, 1);
            spotLightBoxRenderer.material.DisableKeyword("_EMISSION");
            indicatorLightBoxRenderer.material.color = new Color(12/255f, 12/255f, 12/255f,1);
            indicatorLightBoxRenderer.material.DisableKeyword("_EMISSION");
            spotLight.SetActive(false);
            screenQuad.SetActive(false);
            isInterrupted = true;
        }
        
        private IEnumerator boost(int currentTurnOnCount, int delay) {
            yield return new WaitForSeconds(delay);
            if (isInterrupted) yield break;
            if (currentTurnOnCount != turnOnCount) yield break;
            screenQuad.SetActive(true);
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(Monitor))]
    public class MonitorInspector : YureiInspector { }
    #endif
}
