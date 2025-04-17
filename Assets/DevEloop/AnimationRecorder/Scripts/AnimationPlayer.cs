using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DevEloop.AnimationRecorder
{
	public class AnimationPlayer
	{
		private AnimationData animationData;
		private Dictionary<string, Transform> transformsMap = new Dictionary<string, Transform>();
		private bool applyTranslations;
		private bool applyRotations;
		private bool applyScales;
		private bool localPoses;

		private float timePassed = 0;
		private int nextFrame = 0;

		public AnimationPlayer(GameObject animatedModel, bool applyTranslations = true, bool applyRotations = true, bool applyScales = true, 
			bool localPoses = true)
		{
			this.applyTranslations = applyTranslations;
			this.applyRotations = applyRotations;
			this.applyScales = applyScales;
			this.localPoses = localPoses;

			SetModelData(animatedModel);
		}

		public void SetAnimatedModel(GameObject animatedModel)
		{
			SetModelData(animatedModel);
		}

		public void SetAnimationData(AnimationData animationData)
		{
			this.animationData = animationData;
		}

		public void LoadAnimationData(string animationJsonFilename)
		{
			if (string.IsNullOrEmpty(animationJsonFilename))
			{
				Debug.LogError("AnimationPlayer: animation file isn't specified");
				return;
			}

			if (!File.Exists(animationJsonFilename))
			{
				Debug.LogErrorFormat("AnimationPlayer: animation file doesn't exist: {0}", animationJsonFilename);
				return;
			}

			animationData = JsonUtility.FromJson<AnimationData>(File.ReadAllText(animationJsonFilename));
		}

		public void StartPlaying()
		{
			StartPlaying(0);
		}

		public void StartPlaying(int frameNumber)
		{
			if (animationData == null)
			{
				Debug.LogError("AnimationPlayer: animation data isn't loaded");
				return;
			}

			if (frameNumber < 0 || frameNumber >= animationData.timeframes.Length)
			{
				Debug.LogErrorFormat("AnimationPlayer: frame number is out of range: {0}/{1}", frameNumber, animationData.timeframes.Length);
				return;
			}

			IsPlaying = true;
			timePassed = animationData.timeframes[frameNumber];
			nextFrame = frameNumber;
			ApplyFrame(frameNumber);
		}

		public void StopPlaying()
		{
			IsPlaying = false;
		}

		public void Update(float deltaTime)
		{
			if (IsPlaying)
			{
				timePassed += deltaTime;

				if (nextFrame >= animationData.timeframes.Length)
				{
					StopPlaying();
					return;
				}

				if (timePassed >= animationData.timeframes[nextFrame])
				{
					int frameToApply = nextFrame;
					for(int i=nextFrame+1; i< animationData.timeframes.Length; i++)
					{
						if (timePassed >= animationData.timeframes[i])
							frameToApply = i;
						else
							break;
					}

					ApplyFrame(frameToApply);
					nextFrame = frameToApply + 1;
				}
			}
		}

		public void ApplyFrame(int frameNumber)
		{
			if (animationData == null)
			{
				Debug.LogError("AnimationPlayer: animation data isn't loaded");
				return;
			}

			if (frameNumber < 0 || frameNumber >= animationData.timeframes.Length)
			{
				Debug.LogErrorFormat("AnimationPlayer: frame number is out of range: {0}/{1}", frameNumber, animationData.timeframes.Length);
				return;
			}

			foreach (NodeData nodeData in animationData.nodesData)
			{
				string nodeName = nodeData.nodePath;

				if (transformsMap.TryGetValue(nodeName, out Transform t))
				{
					if (applyTranslations)
					{
						if (localPoses)
							t.localPosition = nodeData.poses[frameNumber].translation;
						else
							t.position = nodeData.poses[frameNumber].translation;
					}
					if (applyRotations)
					{
						if (localPoses)
							t.localRotation = nodeData.poses[frameNumber].rotationQuaternion;
						else
							t.rotation = nodeData.poses[frameNumber].rotationQuaternion;
					}
					if (applyScales)
						t.localScale = nodeData.poses[frameNumber].scale;
				}
			}

			CurrentFrame = frameNumber;
		}

		public bool IsPlaying { get; private set; } = false;

		public int CurrentFrame { get; private set; } = -1;

		public bool ApplyTranslations
		{
			get { return applyTranslations; }
			set { applyTranslations = value; }
		}

		public bool ApplyRotations
		{
			get { return applyRotations; }
			set { applyRotations = value; }
		}

		public bool ApplyScales
		{
			get { return applyScales; }
			set { applyScales = value; }
		}

		public int GetFramesCount()
		{
			return animationData != null ? animationData.timeframes.Length : 0;
		}

		public void DeleteFrame(int frameNumber)
		{
			if (animationData == null)
			{
				Debug.LogError("AnimationPlayer: animation data isn't loaded");
				return;
			}

			if (frameNumber < 0 || frameNumber >= animationData.timeframes.Length)
			{
				Debug.LogErrorFormat("AnimationPlayer: frame number is out of range: {0}/{1}", frameNumber, animationData.timeframes.Length);
				return;
			}

			// Remove the frame from timeframes
			List<float> timeframesList = new List<float>(animationData.timeframes);
			timeframesList.RemoveAt(frameNumber);
			animationData.timeframes = timeframesList.ToArray();

			// Remove the frame from each node's poses
			foreach (NodeData nodeData in animationData.nodesData)
				nodeData.poses.RemoveAt(frameNumber);

			// Adjust the current frame if necessary
			if (CurrentFrame >= animationData.timeframes.Length)
			{
				CurrentFrame = animationData.timeframes.Length - 1;
			}
		}

		private void SetModelData(GameObject animatedModel)
		{
			transformsMap.Clear();

			if (animatedModel != null)
			{
				Transform[] transforms = animatedModel.GetComponentsInChildren<Transform>();
				foreach (Transform t in transforms)
					transformsMap.Add(Utils.GetNodePath(t, animatedModel.transform), t);
			}
		}
	}
}
