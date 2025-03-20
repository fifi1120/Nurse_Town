#if UNITY_EDITOR
using Seagull.Interior_01.Utility.Inspector;
#endif

using System;
using UnityEditor;
using UnityEngine;

namespace Seagull.Interior_01 {
    public class Clock : MonoBehaviour {

        [Tooltip("Indicate how many hours, minutes, and seconds represent circling a circle on the clock")]
        public Vector3 hourMinuteAndSecondUnit = new Vector3(12, 60, 60);
        
        [Tooltip("Indicate the hour and minute the clock reflects")]
        public Vector2 currentHourAndMinute = new Vector2(0,0);

        [Tooltip("Should this script control the rotating of the clock")]
        public bool controlByScript = true;

        [Tooltip("Should the clock update time automatically")]
        public bool shouldUpdateTime = true;

        [SerializeField] private Transform hourHand;
        [SerializeField] private Transform minuteHand;

        private void Start() {
            if (!controlByScript) return;

            if (!shouldUpdateTime) return;
            calculateTime();
        }

        private void FixedUpdate() {
            if (!controlByScript) return;
            rotateHands();
            if (!shouldUpdateTime) return;
            calculateTime();
        }

        private void rotateHands() {
            float hourHandRatio = currentHourAndMinute.x / hourMinuteAndSecondUnit.x;
            float minuteHandRatio = currentHourAndMinute.y / hourMinuteAndSecondUnit.y;
            float hourAngle = hourHandRatio * -360;
            float minuteAngle = minuteHandRatio * -360;
            hourHand.eulerAngles = new Vector3(hourAngle, 0, 0);
            minuteHand.eulerAngles = new Vector3(minuteAngle, 0, 0);
        }

        private void calculateTime() {
            DateTime dateTimeNow = DateTime.Now;

            DateTime historicalTime = new DateTime(2000, 1, 1);
            TimeSpan timeSpan = dateTimeNow - historicalTime;
            double totalSecondsCount = timeSpan.TotalSeconds;
            double totalMinutesCount = totalSecondsCount / hourMinuteAndSecondUnit.z;
            double totalHoursCount = totalMinutesCount / hourMinuteAndSecondUnit.y;

            float displayedHour = (float)(totalHoursCount % hourMinuteAndSecondUnit.x);
            float displayedMinute = (float)(totalMinutesCount % hourMinuteAndSecondUnit.y);

            currentHourAndMinute = new Vector2(displayedHour, displayedMinute);
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(Clock))]
    public class ClockInspector : YureiInspector { }
    #endif
}