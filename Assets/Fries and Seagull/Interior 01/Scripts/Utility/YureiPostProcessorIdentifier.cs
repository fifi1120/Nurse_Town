using UnityEngine;

namespace Seagull.Interior_01.Utility {
    public class YureiPostProcessorIdentifier : MonoBehaviour {

        private YureiManagerBRP _yureiManagerBrp;
        
        private void Start() {
            _yureiManagerBrp = GameObject.Find("Yurei Manager").GetComponent<YureiManagerBRP>();
            gameObject.name = "Yurei Post Process Volume";
            gameObject.layer = _yureiManagerBrp.yureiLayer;
        }

        private void FixedUpdate() {
            gameObject.name = "Yurei Post Process Volume";
            if (gameObject.layer != _yureiManagerBrp.yureiLayer) gameObject.layer = _yureiManagerBrp.yureiLayer;
        }
    }
}