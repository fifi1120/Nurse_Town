using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DevEloop.AnimationRecorder
{
	[Serializable]
	public class AnimationData
	{
		public string animationName;
		public float[] timeframes;
		public NodeData[] nodesData;

		public AnimationData DeepCopy()
		{
			AnimationData copy = new AnimationData
			{
				animationName = this.animationName,
				timeframes = (float[])this.timeframes.Clone(),
				nodesData = new NodeData[this.nodesData.Length]
			};

			for (int i = 0; i < this.nodesData.Length; i++)
			{
				copy.nodesData[i] = new NodeData
				{
					nodePath = this.nodesData[i].nodePath,
					poses = new List<PoseData>()
				};

				foreach (var pose in this.nodesData[i].poses)
				{
					copy.nodesData[i].poses.Add(new PoseData
					{
						translation = pose.translation,
						rotationQuaternion = pose.rotationQuaternion,
						scale = pose.scale
					});
				}
			}

			return copy;
		}
	}

	[Serializable]
	public class NodeData
	{
		public string nodePath;
		public List<PoseData> poses = new List<PoseData>();
	}

	[Serializable]
	public class PoseData
	{
		public Vector3 translation;
		public Quaternion rotationQuaternion;
		public Vector3 scale;
	}
}
