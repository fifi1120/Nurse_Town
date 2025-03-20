using System.Collections.Generic;
using UnityEngine;

namespace Seagull.Interior_01 {
    public class Suitcase : MonoBehaviour {
        [SerializeField] private Rigidbody mainBody;
        [SerializeField] private List<Transform> wheelTransforms;
        [SerializeField] private List<WheelCollider> wheelColliders;

        private void FixedUpdate() {
            Vector3 velocity = mainBody.velocity;
            Vector2 velocity2 = new Vector2(velocity.x, velocity.z);
            float angleRadians = Mathf.Atan2(velocity2.y, velocity2.x);
            float angleDegrees = angleRadians * Mathf.Rad2Deg;
            float drivingAngle = (angleDegrees + 360) % 360;
            float magnitude = velocity2.magnitude;
            foreach (var t in wheelTransforms) {
                t.localEulerAngles = new Vector3(0, Mathf.LerpAngle(t.localEulerAngles.y, drivingAngle, 0.01f * magnitude), 0);
            }
            foreach (var c in wheelColliders) {
                c.steerAngle = Mathf.LerpAngle(c.steerAngle, drivingAngle, 0.01f * magnitude);
            }
        }
    }
}