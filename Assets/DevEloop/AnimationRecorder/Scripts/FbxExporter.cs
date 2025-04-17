using AOT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DevEloop.AnimationRecorder
{
	public class FbxExporter
	{
		public static bool IsFbxExportSupported
		{
			get
			{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
				return FbxExporterNative.IsExportSupported();
#else
				return false;
#endif
			}
		}

		public bool ExportTranslations { get; set; } = true;

		public bool ExportRotations { get; set; } = true;

		public bool ExportScales { get; set; } = true;

		public float ModelScale { get; set; } = 1.0f;

		public FbxFileFormat FileFormat { get; set; } = FbxFileFormat.Binary;

		public bool ExportMeshes { get; set; } = true;

		public bool ExportTextures { get; set; } = true;

		public void ExportAnimationToFbx(GameObject model, string animationJsonFilename, string outputFbxFilename)
		{
			AnimationData animationData = LoadAnimationDataFromJson(animationJsonFilename);
			ExportAnimationToFbx(model, animationData, outputFbxFilename);
		}

		public void ExportAnimationToFbx(GameObject model, AnimationData animationData, string outputFbxFilename)
		{
			if (IsFbxExportSupported)
			{
				DateTime startTime = DateTime.Now;

				if (model == null)
				{
					Debug.LogError("FbxExporter: model isn't specified");
					return;
				}

				if (string.IsNullOrEmpty(outputFbxFilename))
				{
					Debug.LogError("FbxExporter: output file isn't specified");
					return;
				}

				FbxExporterNative.Initialize();

				Dictionary<Transform, int> transformsIndicesMap = new Dictionary<Transform, int>();
				Transform modelTransform = model.transform;
				for (int i = 0; i < modelTransform.childCount; i++)
				{
					Transform child = modelTransform.GetChild(i);
					AddNodesRecursively(child, -1, transformsIndicesMap);
				}


				if (ExportMeshes)
					FindAndAddMeshes(transformsIndicesMap);

				AddAnimation(animationData);

				int result = FbxExporterNative.Export(outputFbxFilename, FileFormat);
				if (result == 0)
					Debug.LogFormat("FbxExporter: model exported to {0}.", outputFbxFilename);
				else
					Debug.LogErrorFormat("FbxExporter: export error: {0}", result);

				Debug.LogFormat("FbxExporter: animation export time: {0} sec", (DateTime.Now - startTime).TotalSeconds);
			}
			else
				Debug.LogError("FbxExporter: export to fbx doesn't work on your platfrom, it supports only Windows.");
		}

		private void AddNodesRecursively(Transform t, int parentNodeIdx, Dictionary<Transform, int> transformsIndicesMap)
		{
			int currentNodeIdx = FbxExporterNative.AddNode(t.name, parentNodeIdx, t.localPosition.MirrorX() * ModelScale, t.localRotation.MirrorX(), t.localScale);
			transformsIndicesMap.Add(t, currentNodeIdx);

			for (int i=0; i<t.childCount; i++)
			{
				Transform child = t.GetChild(i);
				AddNodesRecursively(child, currentNodeIdx, transformsIndicesMap);
			}
		}

		private AnimationData LoadAnimationDataFromJson(string animationJsonFilename)
		{
			if (string.IsNullOrEmpty(animationJsonFilename))
			{
				Debug.LogError("FbxExporter: animation json file is empty");
				return null;
			}

			if (!File.Exists(animationJsonFilename))
			{
				Debug.LogErrorFormat("FbxExporter: animation json file doesn't exist: {0}", animationJsonFilename);
				return null;
			}

			AnimationData animationData = null;
			try
			{
				animationData = JsonUtility.FromJson<AnimationData>(File.ReadAllText(animationJsonFilename));
				if (string.IsNullOrEmpty(animationData.animationName))
					animationData. animationName = Path.GetFileNameWithoutExtension(animationJsonFilename);
			}
			catch(Exception exc)
			{
				Debug.LogErrorFormat("FbxExporter: json parsing exception {0}", exc);
			}

			return animationData;
		}

		private void FindAndAddMeshes(Dictionary<Transform, int> transformsIndicesMap)
		{
			foreach (var pair in transformsIndicesMap)
			{
				FindAndAddMesh(transformsIndicesMap, pair.Key, pair.Value);
			}
		}

		private void FindAndAddMesh(Dictionary<Transform, int> transformsIndicesMap, Transform t, int nodeIdx)
		{
			Mesh foundMesh = null;

			SkinnedMeshRenderer skinnedMeshRenderer = t.GetComponent<SkinnedMeshRenderer>();
			MeshFilter meshFilter = t.GetComponent<MeshFilter>();
			if (skinnedMeshRenderer != null)
				foundMesh = skinnedMeshRenderer.sharedMesh;
			else if (meshFilter != null)
					foundMesh = meshFilter.sharedMesh;

			if (foundMesh != null)
			{
				if (!foundMesh.isReadable)
				{
					Debug.LogErrorFormat("FbxExporter: the {0} mesh isn't readable. It can't be exported into FBX", foundMesh.name);
					return;
				}

					Vector3[] vertices = foundMesh.vertices;
				for (int i = 0; i < vertices.Length; i++)
					vertices[i] = vertices[i].MirrorX() * ModelScale;

				Vector3[] normals = foundMesh.normals;
				for (int i = 0; i < normals.Length; i++)
					normals[i] = normals[i].MirrorX();

				int[] triangles = foundMesh.triangles;
				for (int i = 0; i < triangles.Length; i += 3)
				{
					int temp = triangles[i];
					triangles[i] = triangles[i + 2];
					triangles[i + 2] = temp;
				}

				Vector2[] uv = foundMesh.uv;

				int meshIdx = FbxExporterNative.AddMesh(nodeIdx, foundMesh.name, vertices, normals, uv, triangles);
				if (meshIdx < 0)
				{
					Debug.LogErrorFormat("FbxExporter: addMesh error: {0}, node: {1}", meshIdx, t.name);
					return;
				}

				Material material = null;
				if (skinnedMeshRenderer != null)
					material = skinnedMeshRenderer.sharedMaterial;
				else if (meshFilter != null)
					material = meshFilter.GetComponent<Renderer>().material;
				if (material != null)
				{
					byte[] textureData = null;
					string textureName = string.Empty;

					if (ExportTextures)
					{
						Texture2D texture = material.mainTexture as Texture2D;
						if (texture != null)
						{
							textureName = string.IsNullOrEmpty(texture.name) ? "Color_Tex" : texture.name;
							if (texture.isReadable)
							{
								try
								{
									textureData = texture.EncodeToPNG();
								}
								catch (Exception exc)
								{
									Debug.LogErrorFormat("FbxExporter: unable to convert texture to PNG: {0}", exc);
									textureData = null;
								}
							}
							else
								Debug.LogErrorFormat("FbxExporter: texture isn't readable: {0}", texture.name);
						}
					}

					int result = FbxExporterNative.AddTextureAndMaterial(meshIdx, material.name, string.Format("{0}.png", textureName), textureData);
					if (result != 0)
						Debug.LogErrorFormat("FbxExporter: addTextureAndMaterial error: {0}, node: {1}", result, t.name);
				}

				if (skinnedMeshRenderer != null)
				{
					Transform[] bones = skinnedMeshRenderer.bones;
					Matrix4x4[] bindPoses = foundMesh.bindposes;

					BoneWeight[] boneWeights = foundMesh.boneWeights;
					float[] boneWeightsArray = new float[boneWeights.Length * 4];
					int[] boneIndicesArray = new int[boneWeights.Length * 4];
					for (int i = 0; i < boneWeights.Length; i++)
					{
						boneWeightsArray[i * 4] = boneWeights[i].weight0;
						boneWeightsArray[i * 4 + 1] = boneWeights[i].weight1;
						boneWeightsArray[i * 4 + 2] = boneWeights[i].weight2;
						boneWeightsArray[i * 4 + 3] = boneWeights[i].weight3;

						boneIndicesArray[i * 4] = boneWeights[i].boneIndex0 < 0 ? -1 : transformsIndicesMap[bones[boneWeights[i].boneIndex0]];
						boneIndicesArray[i * 4 + 1] = boneWeights[i].boneIndex1 < 0 ? -1 : transformsIndicesMap[bones[boneWeights[i].boneIndex1]];
						boneIndicesArray[i * 4 + 2] = boneWeights[i].boneIndex2 < 0 ? -1 : transformsIndicesMap[bones[boneWeights[i].boneIndex2]];
						boneIndicesArray[i * 4 + 3] = boneWeights[i].boneIndex3 < 0 ? -1 : transformsIndicesMap[bones[boneWeights[i].boneIndex3]]; ;
					}
					int result = FbxExporterNative.AddSkinning(meshIdx, boneWeights.Length, boneWeightsArray, boneIndicesArray);
					if (result != 0)
						Debug.LogErrorFormat("FbxExporter: addSkinning error: {0}, node: {1}", result, t.name);

					for (int i = 0; i < bones.Length; i++)
					{
						if (!transformsIndicesMap.ContainsKey(bones[i]))
						{
							Debug.LogWarningFormat("FbxExporter: bone wasn't added to model: {0}", bones[i].name);
							continue;
						}
						Matrix4x4 boneMatrix = bindPoses[i].inverse;
						Vector3 localPosition = Utils.GetTranslationFromMatrix(boneMatrix).MirrorX();
						Quaternion localRotation = Utils.GetRotationFromMatrix(boneMatrix).MirrorX();
						Vector3 localScale = Utils.GetScaleFromMatrix(boneMatrix);
						result = FbxExporterNative.SetNodeBindPose(meshIdx, transformsIndicesMap[bones[i]], localPosition * ModelScale, localRotation, localScale);
						if (result != 0)
							Debug.LogErrorFormat("FbxExporter: setNodeBindPose error: {0}, node: {1}", result, bones[i].name);
					}
				}
			}
		}

		private void AddAnimation(AnimationData animationData)
		{
			if (animationData == null)
			{
				Debug.LogError("FbxExporter: animationData is null");
				return;
			}

			if (animationData.timeframes == null || animationData.timeframes.Length <= 0)
			{
				Debug.LogError("FbxExporter: animationData is empty");
				return;
			}

			try
			{
				string animationName = animationData.animationName;
				if (string.IsNullOrEmpty(animationName))
					animationName = "animation";

				int result = FbxExporterNative.CreateAnimation(animationName, animationData.timeframes, ExportTranslations, ExportRotations, ExportScales);

				if (result != 0)
				{
					Debug.LogErrorFormat("FbxExporter: createAnimation error: {0}", result);
					return;
				}

				int framesCount = animationData.timeframes.Length;
				float[] positionsArray = new float[framesCount * 3];
				float[] rotationsArray = new float[framesCount * 4];
				float[] scalesArray = new float[framesCount * 3];

				foreach (NodeData nodeData in animationData.nodesData)
				{
					if (nodeData.poses.Count != framesCount)
					{
						Debug.LogErrorFormat("FbxExpoter: invalid poses number (actual: {0}, expected: {1}) for node: {2}", nodeData.poses.Count, framesCount, nodeData.nodePath);
						continue;
					}

					for (int i = 0; i < framesCount; i++)
					{
						PoseData poseData = nodeData.poses[i];

						Vector3 translation = poseData.translation.MirrorX() * ModelScale;
						positionsArray[i * 3] = translation.x;
						positionsArray[i * 3 + 1] = translation.y;
						positionsArray[i * 3 + 2] = translation.z;
						
						Quaternion rotation = poseData.rotationQuaternion.MirrorX();
						rotationsArray[i * 4] = rotation.x;
						rotationsArray[i * 4 + 1] = rotation.y;
						rotationsArray[i * 4 + 2] = rotation.z;
						rotationsArray[i * 4 + 3] = rotation.w;

						scalesArray[i * 3] = poseData.scale.x;
						scalesArray[i * 3 + 1] = poseData.scale.y;
						scalesArray[i * 3 + 2] = poseData.scale.z;
					}

					result = FbxExporterNative.AddNodeAnimation(nodeData.nodePath, positionsArray, rotationsArray, scalesArray, framesCount);
					if (result != 0)
						Debug.LogErrorFormat("FbxExporter: addNodeAnimation error: {0}, node: {1}", result, nodeData.nodePath);
				}
			}
			catch (Exception exc)
			{
				Debug.LogErrorFormat("FbxExporter: add animation exception: {0}", exc);
			}
		}
	}
}
