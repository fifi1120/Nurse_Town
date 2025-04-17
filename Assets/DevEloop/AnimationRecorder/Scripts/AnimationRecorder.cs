using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevEloop.AnimationRecorder
{
	public class AnimationRecorder
	{
		public float capturingFPS = 30.0f;

		public bool captureLocalPoses = true;

		public bool captureRootMotion = true;

		private Transform rootTransform = null;

		private Matrix4x4 rootInitialTRSMatrix = Matrix4x4.identity;

		private List<float> capturedTimeframes = new List<float>();
		private Dictionary<Transform, NodeData> capturedTransformPoses = new Dictionary<Transform, NodeData>();

		private float framesInterval = 0;
		private float timePassed = 0;
		private float lastCapturedFrameTime = 0;

		public bool IsCapturing { get; private set; } = false;

		public void Initialize(GameObject animatedModel, List<Transform> additionalNodesToCapture = null, bool includeChildNodes = false,
			List<Transform> nodesToExclude = null, bool excludeChildNodes = false)
		{
			rootTransform = animatedModel.transform;
			rootInitialTRSMatrix = Matrix4x4.TRS(rootTransform.position, rootTransform.rotation, rootTransform.lossyScale);

			List<Transform> extendedTransformsToIncludeList = new List<Transform>();
			if (additionalNodesToCapture != null)
			{
				foreach (var t in additionalNodesToCapture)
				{
					if (includeChildNodes)
					{
						var childTransformsToInclude = t.GetComponentsInChildren<Transform>(true);
						foreach (Transform child in childTransformsToInclude)
						{
							if (!extendedTransformsToIncludeList.Contains(child))
								extendedTransformsToIncludeList.Add(child);
						}
					}
					else
						extendedTransformsToIncludeList.Add(t);
				}
			}

			List<Transform> extendedTransformsToExcludeList = new List<Transform>();
			if (nodesToExclude != null)
			{
				foreach (var t in nodesToExclude)
				{
					if (excludeChildNodes)
					{
						var childTransformsToExclude = t.GetComponentsInChildren<Transform>(true);
						foreach (Transform child in childTransformsToExclude)
						{
							if (!extendedTransformsToExcludeList.Contains(child))
								extendedTransformsToExcludeList.Add(child);
						}
					}
					else
						extendedTransformsToExcludeList.Add(t);
				}
			}

			List<Transform> childTransforms = new List<Transform>();
			animatedModel.GetComponentsInChildren(true, childTransforms);
			childTransforms.Remove(animatedModel.transform);

			capturedTransformPoses.Clear();
			if (extendedTransformsToIncludeList.Count > 0)
			{
				foreach (Transform t in extendedTransformsToIncludeList)
				{
					if (!extendedTransformsToExcludeList.Contains(t))
					{
						NodeData nodeData = new NodeData();
						nodeData.nodePath = Utils.GetNodePath(t, animatedModel.transform);
						capturedTransformPoses.Add(t, nodeData);
					}
				}
			}
			else
			{
				foreach (Transform t in childTransforms)
				{
					if (!extendedTransformsToExcludeList.Contains(t))
					{
						NodeData nodeData = new NodeData();
						nodeData.nodePath = Utils.GetNodePath(t, animatedModel.transform);
						capturedTransformPoses.Add(t, nodeData);
					}
				}
			}
		}

		public void StartCapturing()
		{
			if (capturingFPS <= 0)
			{
				capturingFPS = 30.0f;
				Debug.LogWarningFormat("AnimationRecorder: capturing FPS was set to {0}", capturingFPS);
			}

			framesInterval = 1.0f / capturingFPS;
			timePassed = 0;
			lastCapturedFrameTime = 0;

			capturedTimeframes.Clear();
			foreach (var pair in capturedTransformPoses)
				pair.Value.poses.Clear();

			IsCapturing = true;

			CaptureFrame();
		}

		public void StopCapturing() 
		{ 
			IsCapturing = false;
		}

		public void Update(float deltaTime)
		{
			if (IsCapturing)
			{
				timePassed += deltaTime;
				float timeSinceLastFrame = timePassed - lastCapturedFrameTime;
				if (timeSinceLastFrame >= framesInterval)
					CaptureFrame();
			}
		}

		public AnimationData GetCapturedData()
		{
			AnimationData animationData = new AnimationData();
			animationData.timeframes = capturedTimeframes.ToArray();
			animationData.nodesData = capturedTransformPoses.Values.ToArray();
			return animationData;
		}

		private void CaptureFrame()
		{
			lastCapturedFrameTime = timePassed;
			capturedTimeframes.Add(lastCapturedFrameTime);
			foreach (var pair in capturedTransformPoses)
			{
				Transform t = pair.Key;
				if (captureLocalPoses)
				{
					if (captureRootMotion && t.parent == rootTransform)
					{
						Matrix4x4 rootCurrentTRSMatrix = Matrix4x4.TRS(rootTransform.localPosition, rootTransform.localRotation, rootTransform.localScale);
						Matrix4x4 nodeTRSMatrix = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
						nodeTRSMatrix = rootInitialTRSMatrix.inverse * rootCurrentTRSMatrix * nodeTRSMatrix;

						PoseData poseData = new PoseData();
						poseData.translation = Utils.GetTranslationFromMatrix(nodeTRSMatrix);
						poseData.rotationQuaternion = Utils.GetRotationFromMatrix(nodeTRSMatrix);
						poseData.scale = Utils.GetScaleFromMatrix(nodeTRSMatrix);

						pair.Value.poses.Add(poseData);
					}
					else
					{
						PoseData poseData = new PoseData();
						if (captureLocalPoses)
						{
							poseData.translation = t.localPosition;
							poseData.rotationQuaternion = t.localRotation;
							poseData.scale = t.localScale;
						}
						else
						{
							poseData.translation = t.position;
							poseData.rotationQuaternion = t.rotation;
							poseData.scale = t.lossyScale;
						}

						pair.Value.poses.Add(poseData);
					}
				}
				else
				{
					Matrix4x4 nodeTRSMatrix = Matrix4x4.TRS(t.position, t.rotation, t.lossyScale);
					nodeTRSMatrix = rootInitialTRSMatrix.inverse * nodeTRSMatrix;

					PoseData poseData = new PoseData();
					poseData.translation = Utils.GetTranslationFromMatrix(nodeTRSMatrix);
					poseData.rotationQuaternion = Utils.GetRotationFromMatrix(nodeTRSMatrix);
					poseData.scale = Utils.GetScaleFromMatrix(nodeTRSMatrix);

					pair.Value.poses.Add(poseData);
				}
			}
		}
	}
}
