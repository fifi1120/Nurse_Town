#if UNITY_EDITOR
using Seagull.Interior_01.Utility.Inspector;
#endif

using System.Collections;
using Seagull.Interior_01.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;


namespace Seagull.Interior_01 {
    public class Kettle : TurnOnAble {
        [Tooltip("After how many seconds the water could be fully boiled")]
        public int boilSeconds = 120;

        private int turnOnCount = 0;
        private bool isInterrupted = false;
        
        [YureiButton("Turn On")] public UnityEvent onTurnOn;
        [YureiButton("Turn Off")] public UnityEvent onTurnOff;
        
        [SerializeField] private MeshRenderer lightBoxRenderer;
        
        public void turnOn() {
            lightBoxRenderer.material.color = new Color(255/255f, 243/255f, 0, 0);
            lightBoxRenderer.material.SetColor("_EmissionColor", new Color(205/255f, 12/255f, 0) * 2.1f * 1.806f);
            lightBoxRenderer.material.EnableKeyword("_EMISSION");
            turnOnCount++;
            StartCoroutine(readyToDrink(turnOnCount, boilSeconds));
            isInterrupted = false;
        }

        public void turnOff() {
            lightBoxRenderer.material.color = new Color(22/255f,22/255f,22/255f,1);
            lightBoxRenderer.material.DisableKeyword("_EMISSION");
            isInterrupted = true;
        }

        private IEnumerator readyToDrink(int currentTurnOnCount, int delay) {
            yield return new WaitForSeconds(delay);
            if (isInterrupted) yield break;
            if (currentTurnOnCount != turnOnCount) yield break;
            lightBoxRenderer.material.color = new Color(110/255f, 110/255f, 1, 0);
            lightBoxRenderer.material.SetColor("_EmissionColor", new Color(25/255f, 23/255f, 191/255f) * 2.1f * 1.806f);
            lightBoxRenderer.material.EnableKeyword("_EMISSION");
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(Kettle))]
    public class KettleInspector : YureiInspector { }
    #endif
}
