using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DevEloop.AnimationRecorder
{
	public class AnimationDataEditor
	{
		private AnimationPlayer animationPlayer;

		private Transform selectedTransform;
		private int currentFrame;
		private AnimationData animationData;
		private List<Transform> availableTransforms = new List<Transform>();
		private string[] transformNames;
		private int selectedTransformIndex = -1;
		private Transform rootTransform;
		private bool frameEditorFolder = true;
		private GUIStyle foldoutStyle;

		private bool advancedFoldout = false;

		private float startInterpolationFrame;
		private float endInterpolationFrame;

		private float startApplyFrame;
		private float endApplyFrame;

		// Reorderable list for multiple transform selection
		private ReorderableList transformsListToInterpolate;
		private List<Transform> selectedTransformsToInterpolate = new List<Transform>();
		private bool isTransformsSelectionLocked = false;

		public AnimationDataEditor(GUIStyle foldoutStyle)
		{
			this.foldoutStyle = foldoutStyle;
			Selection.selectionChanged += OnSelectionChanged;
		}

		public void SetAnimationPlayer(AnimationPlayer player)
		{
			animationPlayer = player;
		}

		public void SetAnimationData(AnimationData data, Transform root)
		{
			animationData = data;
			rootTransform = root;
			startInterpolationFrame = 0;
			startApplyFrame = 0;
			endInterpolationFrame = animationData.timeframes.Length - 1;
			endApplyFrame = animationData.timeframes.Length - 1;
			PopulateTransformList();
			SyncSelectionWithHierarchy();
			InitializeTransformList();
		}

		public AnimationData GetAnimationData()
		{
			return animationData.DeepCopy();
		}

		public bool HasAnimationData()
		{
			return animationData != null;
		}

		private void PopulateTransformList()
		{
			availableTransforms.Clear();
			if (rootTransform != null)
			{
				foreach (Transform child in rootTransform.GetComponentsInChildren<Transform>())
				{
					availableTransforms.Add(child);
				}
				availableTransforms.Remove(rootTransform);
			}
			transformNames = availableTransforms.ConvertAll(t => t.name).ToArray();

			if (selectedTransformIndex < 0)
				selectedTransformIndex = 0;
			selectedTransform = availableTransforms[selectedTransformIndex];
			Selection.activeTransform = selectedTransform;
		}

		private void InitializeTransformList()
		{
			transformsListToInterpolate = new ReorderableList(selectedTransformsToInterpolate, typeof(Transform), true, true, true, true);
			transformsListToInterpolate.drawHeaderCallback = (Rect rect) =>
			{
				int currentIndent = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;
				EditorGUI.LabelField(rect, "Selected Transforms");

				Rect buttonRect = new Rect(rect.x + rect.width - 60, rect.y, 60, EditorGUIUtility.singleLineHeight);
				if (GUI.Button(buttonRect, isTransformsSelectionLocked ? "Unlock" : "Lock"))
				{
					isTransformsSelectionLocked = !isTransformsSelectionLocked;
				}

				EditorGUI.indentLevel = currentIndent;
			};
			transformsListToInterpolate.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				GUIState.BeginEnabling(!isTransformsSelectionLocked);
				selectedTransformsToInterpolate[index] = (Transform)EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), selectedTransformsToInterpolate[index], typeof(Transform), true);
				GUIState.EndEnabling();
			};
			transformsListToInterpolate.onAddCallback = (ReorderableList list) =>
			{
				selectedTransformsToInterpolate.Add(null);
			};
			transformsListToInterpolate.onRemoveCallback = (ReorderableList list) =>
			{
				selectedTransformsToInterpolate.RemoveAt(list.index);
			};
		}

		public void DrawControls()
		{
			// Sync selection with Hierarchy window
			SyncSelectionWithHierarchy();

			EditorGUILayout.Space();

			EditorGUILayout.BeginVertical("Box");
			frameEditorFolder = EditorGUILayout.Foldout(frameEditorFolder, "Editor", true, foldoutStyle);
			EditorGUILayout.EndVertical();
			if (frameEditorFolder)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.BeginVertical("HelpBox");
				{
					DrawFramesNavigationControls();

					EditorGUILayout.Space();

					// Transform Selection
					if (transformNames != null && transformNames.Length > 0)
					{
						int newSelectedTransformIndex = EditorGUILayout.Popup("Select Transform", selectedTransformIndex, transformNames);
						if (newSelectedTransformIndex != selectedTransformIndex)
						{
							selectedTransformIndex = newSelectedTransformIndex;
							selectedTransform = availableTransforms[selectedTransformIndex];
							Selection.activeTransform = selectedTransform; // Update Hierarchy window selection
						}
					}

					if (selectedTransform != null && animationData != null)
					{
						// Record the current state of the transform for undo
						Undo.RecordObject(selectedTransform, "Modify Transform");

						// Position Controls
						Vector3 position = selectedTransform.localPosition;
						position = EditorGUILayout.Vector3Field("Position", position);
						selectedTransform.localPosition = position;

						// Rotation Controls
						Quaternion rotation = selectedTransform.localRotation;
						Vector3 eulerRotation = EditorGUILayout.Vector3Field("Rotation", rotation.eulerAngles);
						selectedTransform.localRotation = Quaternion.Euler(eulerRotation);

						// Mark the transform as dirty to ensure changes are saved
						EditorUtility.SetDirty(selectedTransform);

						// Apply changes to AnimationData
						ApplyChangesToAnimationData();
					}

					// Interpolation Controls
					EditorGUILayout.Space();
					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.Space(EditorGUI.indentLevel * 16);
						EditorGUILayout.BeginVertical("Box");
						EditorGUI.indentLevel--;
						advancedFoldout = EditorGUILayout.Foldout(advancedFoldout, "Advanced", true, foldoutStyle);
						EditorGUI.indentLevel++;
						EditorGUILayout.EndVertical();
					}
					EditorGUILayout.EndHorizontal();
					if (advancedFoldout)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.BeginVertical();
						{
							GUIState.BeginEnabling(transformsListToInterpolate.count > 0);
							EditorGUILayout.BeginHorizontal();
							{
								GUILayout.Space(EditorGUI.indentLevel * 16);
								if (GUILayout.Button("Apply Frame to Range", GUILayout.Width(150)))
								{
									ApplyCurrentFrameToRange((int)startApplyFrame, (int)endApplyFrame);
								}

								startApplyFrame = EditorGUILayout.IntField("", (int)startApplyFrame, GUILayout.Width(70));
								EditorGUILayout.MinMaxSlider(ref startApplyFrame, ref endApplyFrame, 0, animationData.timeframes.Length - 1);
								endApplyFrame = EditorGUILayout.IntField("", (int)endApplyFrame, GUILayout.Width(70));

								if (startApplyFrame > currentFrame)
									startApplyFrame = currentFrame;
								if (endApplyFrame < currentFrame)
									endApplyFrame = currentFrame;
							}
							EditorGUILayout.EndHorizontal();

							EditorGUILayout.BeginHorizontal();
							{
								GUILayout.Space(EditorGUI.indentLevel * 16); // Add space for indentation
								if (GUILayout.Button("Interpolate", GUILayout.Width(150)))
								{
									InterpolateBetweenFrames((int)startInterpolationFrame, (int)endInterpolationFrame);
								}

								startInterpolationFrame = EditorGUILayout.IntField("", (int)startInterpolationFrame, GUILayout.Width(70));
								EditorGUILayout.MinMaxSlider(ref startInterpolationFrame, ref endInterpolationFrame, 0, animationData.timeframes.Length - 1);
								endInterpolationFrame = EditorGUILayout.IntField("", (int)endInterpolationFrame, GUILayout.Width(70));

								if (startInterpolationFrame != currentFrame && endInterpolationFrame != currentFrame)
								{
									if (Mathf.Abs(startInterpolationFrame-currentFrame) < Mathf.Abs(endInterpolationFrame-currentFrame))
										startInterpolationFrame = currentFrame;
									else
										endInterpolationFrame = currentFrame;
								}
							}
							EditorGUILayout.EndHorizontal();
							GUIState.EndEnabling();

							EditorGUILayout.BeginHorizontal();
							{
								GUILayout.Space(EditorGUI.indentLevel * 16);
								transformsListToInterpolate.displayAdd = !isTransformsSelectionLocked;
								transformsListToInterpolate.displayRemove = !isTransformsSelectionLocked;
								transformsListToInterpolate.DoLayoutList();
							}
							EditorGUILayout.EndHorizontal();
						}
						EditorGUILayout.EndVertical();
						EditorGUI.indentLevel--;
					}
				}
				EditorGUILayout.EndVertical();
				EditorGUI.indentLevel--;
			}
		}

		public void DrawFramesNavigationControls()
		{
			int framesCount = animationPlayer.GetFramesCount();
			if (animationPlayer != null && framesCount > 0)
			{
				currentFrame = animationPlayer.CurrentFrame;
				EditorGUILayout.BeginHorizontal();
				{
					currentFrame = EditorGUILayout.IntSlider("Frame Number", currentFrame, 0, framesCount - 1);

					if (GUILayout.Button("<<<", GUILayout.Width(50)))
						currentFrame = Mathf.Max(0, currentFrame - 1);

					if (GUILayout.Button(">>>", GUILayout.Width(50)))
						currentFrame = Mathf.Min(framesCount - 1, currentFrame + 1);

					if (GUILayout.Button("X", GUILayout.Width(25)))
					{
						animationPlayer.DeleteFrame(currentFrame);
						currentFrame = animationPlayer.CurrentFrame;
						animationPlayer.ApplyFrame(currentFrame);
					}

					if (currentFrame != animationPlayer.CurrentFrame && !animationPlayer.IsPlaying)
						animationPlayer.ApplyFrame(currentFrame);
				}
				EditorGUILayout.EndHorizontal();
			}
		}

		private void ApplyChangesToAnimationData()
		{
			if (selectedTransform == null || animationData == null)
				return;

			foreach (var nodeData in animationData.nodesData)
			{
				if (nodeData.nodePath == GetTransformPath(selectedTransform))
				{
					if (currentFrame >= 0 && currentFrame < nodeData.poses.Count)
					{
						nodeData.poses[currentFrame].translation = selectedTransform.localPosition;
						nodeData.poses[currentFrame].rotationQuaternion = selectedTransform.localRotation;
					}
					break;
				}
			}
		}

		private void InterpolateBetweenFrames(int startFrame, int endFrame)
		{
			if (selectedTransformsToInterpolate == null || selectedTransformsToInterpolate.Count == 0 || animationData == null || startFrame == endFrame)
				return;

			foreach (var transform in selectedTransformsToInterpolate)
			{
				if (transform == null)
					continue;

				foreach (var nodeData in animationData.nodesData)
				{
					if (nodeData.nodePath == GetTransformPath(transform))
					{
						if (startFrame >= 0 && startFrame < nodeData.poses.Count && endFrame >= 0 && endFrame < nodeData.poses.Count)
						{
							var startPose = nodeData.poses[startFrame];
							var endPose = nodeData.poses[endFrame];

							if (startFrame < endFrame)
							{
								for (int i = startFrame + 1; i < endFrame; i++)
								{
									float t = (float)(i - startFrame) / (endFrame - startFrame);
									nodeData.poses[i].translation = Vector3.Lerp(startPose.translation, endPose.translation, t);
									nodeData.poses[i].rotationQuaternion = Quaternion.Slerp(startPose.rotationQuaternion, endPose.rotationQuaternion, t);
								}
							}
							else
							{
								for (int i = startFrame - 1; i > endFrame; i--)
								{
									float t = (float)(startFrame - i) / (startFrame - endFrame);
									nodeData.poses[i].translation = Vector3.Lerp(startPose.translation, endPose.translation, t);
									nodeData.poses[i].rotationQuaternion = Quaternion.Slerp(startPose.rotationQuaternion, endPose.rotationQuaternion, t);
								}
							}
						}
						break;
					}
				}
			}
		}

		private void ApplyCurrentFrameToRange(int startFrame, int endFrame)
		{
			if (selectedTransformsToInterpolate == null || selectedTransformsToInterpolate.Count == 0 || animationData == null)
				return;

			foreach (var transform in selectedTransformsToInterpolate)
			{
				if (transform == null)
					continue;

				foreach (var nodeData in animationData.nodesData)
				{
					if (nodeData.nodePath == GetTransformPath(transform))
					{
						if (currentFrame >= 0 && currentFrame < nodeData.poses.Count)
						{
							var currentPose = nodeData.poses[currentFrame];

							for (int i = startFrame; i <= endFrame; i++)
							{
								if (i >= 0 && i < nodeData.poses.Count)
								{
									nodeData.poses[i].translation = currentPose.translation;
									nodeData.poses[i].rotationQuaternion = currentPose.rotationQuaternion;
								}
							}
						}
						break;
					}
				}
			}
		}

		private string GetTransformPath(Transform transform)
		{
			if (transform == rootTransform)
				return "";

			string path = transform.name;
			while (transform.parent != null && transform.parent != rootTransform)
			{
				transform = transform.parent;
				path = transform.name + "/" + path;
			}
			return path;
		}

		private void SyncSelectionWithHierarchy()
		{
			if (Selection.activeTransform != null && availableTransforms.Contains(Selection.activeTransform))
			{
				selectedTransform = Selection.activeTransform;
				selectedTransformIndex = availableTransforms.IndexOf(selectedTransform);
			}
		}

		private void OnSelectionChanged()
		{
			if (isTransformsSelectionLocked)
				return;

			if (Selection.objects != null && Selection.objects.Length > 0)
			{
				selectedTransformsToInterpolate.Clear();
				foreach (var obj in Selection.objects)
				{
					GameObject go = obj as GameObject;
					if (go != null)
					{
						Transform transform = go.transform;
						if (availableTransforms.Contains(transform))
						{
							selectedTransformsToInterpolate.Add(transform);
						}
					}
				}
			}
		}
	}
}
