using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DevEloop.AnimationRecorder
{
	public static class AnimationClipConstructor
	{
		public static AnimationClip CreateAnimationClip(string inputAnimationJsonFilename, bool useTranslations = true, bool useRotations = true, bool useScales = true)
		{
			if (string.IsNullOrEmpty(inputAnimationJsonFilename))
			{
				Debug.LogError("AnimationClipConstructor: input json file is empty");
				return null;
			}

			if (!File.Exists(inputAnimationJsonFilename))
			{
				Debug.LogErrorFormat("AnimationClipConstructor: input json file doesn't exist: {0}", inputAnimationJsonFilename);
				return null;
			}

			try
			{
				AnimationData animationData = JsonUtility.FromJson<AnimationData>(File.ReadAllText(inputAnimationJsonFilename));
				return CreateAnimationClip(animationData, useTranslations, useRotations, useScales);
			}
			catch(Exception exc)
			{
				Debug.LogErrorFormat("AnimationClipConstructor: exception occured: {0}", exc);
				return null;
			}
		}

		public static AnimationClip CreateAnimationClip(AnimationData animationData, bool useTranslations = true, bool useRotations = true, bool useScales = true)
		{
			AnimationClip animationClip = new AnimationClip();
			AddAnimationClipData(animationClip, animationData, useTranslations, useRotations, useScales);
			return animationClip;
		}

		public static void AddAnimationClipData(AnimationClip animationClip, AnimationData animationData,
			bool useTranslations = true, bool useRotations = true, bool useScales = true)
		{
			animationClip.ClearCurves();

			foreach (NodeData nodeData in animationData.nodesData)
			{
				AnimationCurve positionXCurve = new AnimationCurve();
				AnimationCurve positionYCurve = new AnimationCurve();
				AnimationCurve positionZCurve = new AnimationCurve();

				AnimationCurve rotationXCurve = new AnimationCurve();
				AnimationCurve rotationYCurve = new AnimationCurve();
				AnimationCurve rotationZCurve = new AnimationCurve();
				AnimationCurve rotationWCurve = new AnimationCurve();

				AnimationCurve scaleXCurve = new AnimationCurve();
				AnimationCurve scaleYCurve = new AnimationCurve();
				AnimationCurve scaleZCurve = new AnimationCurve();

				for (int frameIdx = 0; frameIdx < nodeData.poses.Count; frameIdx++)
				{
					float timeframe = animationData.timeframes[frameIdx];
					PoseData poseData = nodeData.poses[frameIdx];

					if (useTranslations)
					{
						positionXCurve.AddKey(timeframe, poseData.translation.x);
						positionYCurve.AddKey(timeframe, poseData.translation.y);
						positionZCurve.AddKey(timeframe, poseData.translation.z);
					}

					if (useRotations)
					{
						rotationXCurve.AddKey(timeframe, poseData.rotationQuaternion.x);
						rotationYCurve.AddKey(timeframe, poseData.rotationQuaternion.y);
						rotationZCurve.AddKey(timeframe, poseData.rotationQuaternion.z);
						rotationWCurve.AddKey(timeframe, poseData.rotationQuaternion.w);
					}

					if (useScales)
					{
						scaleXCurve.AddKey(timeframe, poseData.scale.x);
						scaleYCurve.AddKey(timeframe, poseData.scale.y);
						scaleZCurve.AddKey(timeframe, poseData.scale.z);
					}
				}

				if (useTranslations)
				{
					animationClip.SetCurve(nodeData.nodePath, typeof(Transform), "localPosition.x", positionXCurve);
					animationClip.SetCurve(nodeData.nodePath, typeof(Transform), "localPosition.y", positionYCurve);
					animationClip.SetCurve(nodeData.nodePath, typeof(Transform), "localPosition.z", positionZCurve);
				}

				if (useRotations)
				{
					animationClip.SetCurve(nodeData.nodePath, typeof(Transform), "localRotation.x", rotationXCurve);
					animationClip.SetCurve(nodeData.nodePath, typeof(Transform), "localRotation.y", rotationYCurve);
					animationClip.SetCurve(nodeData.nodePath, typeof(Transform), "localRotation.z", rotationZCurve);
					animationClip.SetCurve(nodeData.nodePath, typeof(Transform), "localRotation.w", rotationWCurve);
				}

				if (useScales)
				{
					animationClip.SetCurve(nodeData.nodePath, typeof(Transform), "localScale.x", scaleXCurve);
					animationClip.SetCurve(nodeData.nodePath, typeof(Transform), "localScale.y", scaleYCurve);
					animationClip.SetCurve(nodeData.nodePath, typeof(Transform), "localScale.z", scaleZCurve);
				}
			}
		}
	}
}
