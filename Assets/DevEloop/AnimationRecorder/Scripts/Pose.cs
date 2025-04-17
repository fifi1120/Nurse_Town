using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DevEloop.AnimationRecorder
{
	public class Pose
	{
		private Dictionary<Transform, PoseData> transformDataMap = new Dictionary<Transform, PoseData>();

		public Pose(Transform root)
		{
			var transforms = root.GetComponentsInChildren<Transform>(true);
			foreach (var t in transforms)
			{
				PoseData poseData = new PoseData()
				{
					translation = t.localPosition,
					rotationQuaternion = t.localRotation,
					scale = t.localScale
				};
				transformDataMap.Add(t, poseData);
			}
		}

		public void ApplyPose()
		{
			foreach (var kvp in transformDataMap)
			{
				Transform t = kvp.Key;
				PoseData poseData = kvp.Value;

				t.localPosition = poseData.translation;
				t.localRotation = poseData.rotationQuaternion;
				t.localScale = poseData.scale;
			}
		}
	}
}
