using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DevEloop.AnimationRecorder
{
	public class AnimationClipSettings
	{
		public bool active = false;

		public bool saveRotations = true;
		public bool saveTranslations = true;
		public bool saveScales = true;
	}

	public class FbxSettings
	{
		public bool active = false;
		public float modelScale = 1.0f;
		public FbxFileFormat fileFormat = FbxFileFormat.Binary;
		public bool exportMeshes = true;
		public bool exportTextures = true;
		public bool exportRotations = true;
		public bool exportTranslations = true;
		public bool exportScales = true;
	}

	[InitializeOnLoad]
	public class AnimationRecorderWindow : EditorWindow
	{
		private static bool isInitialized;

		private Animator animator;

		private bool startRecordingManually = false;

		private bool isAnimatorPlaying = false;

		private Pose initialPose = null;

		private AnimationRecorder animationRecorder;

		private AnimationClip outputAnimationClip;

		private AnimationDataAsset animationDataAsset;

		private AnimationPlayer animationPlayer;

		private bool showRecordedAnimationFoldout = true;

		private bool recordingFoldout = true;

		private Object fbxOutputFolder = null;

		private bool fbxFoldout = false;

		private FbxSettings fbxSettings = new FbxSettings();

		private bool animationClipFoldout = false;

		private AnimationClipSettings animationClipSettings = new AnimationClipSettings();

		private GUIStyle foldoutStyle;

		private GUIStyle textAreaLabelStyle;

		private GUIStyle linkStyle;

		private Vector2 scrollPosition;

		private string animationName = "New Animation";

		private AnimationDataEditor animationDataEditor;

		private bool useRotations = true;

		private bool useTranslations = true;

		private bool useScales = true;

		[MenuItem("Window/Animation/AnimationRecorder")]
		private static void Init()
		{
			if (isInitialized)
				return;

			AnimationRecorderWindow window = (AnimationRecorderWindow)GetWindow(typeof(AnimationRecorderWindow), false, "Animation Recorder", true);
			window.minSize = new Vector2(450, 450);
			window.Show();

			isInitialized = true;
		}

		private void OnEnable()
		{
			if (foldoutStyle == null)
			{
				foldoutStyle = new GUIStyle(EditorStyles.foldout)
				{
					fontStyle = FontStyle.Bold,
					fontSize = 12
				};
			}

			if (textAreaLabelStyle == null)
			{
				textAreaLabelStyle = new GUIStyle();
				textAreaLabelStyle.wordWrap = true;
				textAreaLabelStyle.alignment = TextAnchor.MiddleCenter;
			}

			if (linkStyle == null)
			{
				linkStyle = new GUIStyle(EditorStyles.boldLabel);
				linkStyle.normal.textColor = new Color(0.2f, 0.4f, 0.73f);
				linkStyle.fontSize = 14;
				linkStyle.alignment = TextAnchor.MiddleCenter;
			}

			if (animationDataEditor == null)
				animationDataEditor = new AnimationDataEditor(foldoutStyle);

			if (animator != null)
				OnAnimatorSet();

			Selection.selectionChanged += Repaint;
			EditorApplication.hierarchyChanged += Repaint;
		}

		private void OnDisable()
		{
			// Unsubscribe from selection and hierarchy change events
			Selection.selectionChanged -= Repaint;
			EditorApplication.hierarchyChanged -= Repaint;
		}

		private void OnDestroy()
		{
			isInitialized = false;
			if (initialPose != null)
				initialPose.ApplyPose();
		}

		private void OnGUI()
		{
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			DrawRecordingControls();

			DrawRecordedAnimationControls();

			EditorGUILayout.EndScrollView();
		}

		private void DrawRecordingControls()
		{
			EditorGUILayout.Space();

			EditorGUILayout.BeginVertical("Box");
			recordingFoldout = EditorGUILayout.Foldout(recordingFoldout, "Recording", true, foldoutStyle);
			EditorGUILayout.EndVertical();
			if (recordingFoldout)
			{
				// Animator Selection
				EditorGUILayout.BeginVertical("HelpBox");
				{
					EditorGUILayout.LabelField("Select an object with Animator", EditorStyles.boldLabel);
					EditorGUILayout.BeginHorizontal();
					{
						Animator newAnimator = (Animator)EditorGUILayout.ObjectField("Animator", animator, typeof(Animator), true);
						if (newAnimator != animator)
						{
							animator = newAnimator;
							if (animator != null)
								OnAnimatorSet();
						}
						if (animator != null)
						{
							GUIState.BeginEnabling(!isAnimatorPlaying && !animationPlayer.IsPlaying);
							if (GUILayout.Button("Reset Pose", GUILayout.Width(100)))
								initialPose.ApplyPose();
							GUIState.EndEnabling();
						}
					}
					EditorGUILayout.EndHorizontal();

					// Animation Controls
					if (animator != null)
					{
						GUIState.BeginEnabling(!animationPlayer.IsPlaying);
						if (isAnimatorPlaying)
						{
							if (GUILayout.Button("Stop Animator"))
								StopAnimation();
						}
						else
						{
							if (GUILayout.Button("Run Animator"))
								StartAnimation();
						}
						GUIState.EndEnabling();

						EditorGUILayout.Space();

						GUIState.BeginEnabling(!isAnimatorPlaying);
						startRecordingManually = GUILayout.Toggle(startRecordingManually, "Start recording manually", GUILayout.Width(155));
						GUIState.EndEnabling();
						if (startRecordingManually)
						{
							EditorGUILayout.BeginHorizontal();
							{
								GUIState.BeginEnabling(isAnimatorPlaying && !animationRecorder.IsCapturing);
								if (GUILayout.Button("Start Recording"))
									StartRecordAnimation();
								GUIState.EndEnabling();

								GUIState.BeginEnabling(isAnimatorPlaying && animationRecorder.IsCapturing);
								if (GUILayout.Button("Stop Recording"))
									StopRecordAnimation();
								GUIState.EndEnabling();
							}
							EditorGUILayout.EndHorizontal();
						}

						EditorGUILayout.Space(10);
						EditorGUILayout.BeginVertical("Box");
						GUILayout.TextArea("Need seamless blendshapes capture? Try", textAreaLabelStyle);
						if (GUILayout.Button("Animation Recorder Pro", linkStyle))
						{
							Application.OpenURL("https://u3d.as/3hTq");
						}
						EditorGUILayout.EndVertical();
					}
				}
				EditorGUILayout.EndVertical();
			}
		}

		private void DrawRecordedAnimationControls()
		{
			EditorGUILayout.Space();

			// Recorded Animation Controls
			GUIState.BeginEnabling(!isAnimatorPlaying);
			if (animationPlayer != null)
			{
				EditorGUILayout.BeginVertical("Box");
				{
					showRecordedAnimationFoldout = EditorGUILayout.Foldout(showRecordedAnimationFoldout, "Recorded Animation", true, foldoutStyle);
				}
				EditorGUILayout.EndVertical();
				
				if (showRecordedAnimationFoldout)
				{
					EditorGUILayout.BeginVertical("HelpBox");
					{
						EditorGUILayout.LabelField("Load/Save animation data", EditorStyles.boldLabel);
						EditorGUILayout.BeginHorizontal();
						{
							AnimationDataAsset selectedAnimationData  = (AnimationDataAsset)EditorGUILayout.ObjectField("Animation Data", animationDataAsset, typeof(AnimationDataAsset), false);
							if (selectedAnimationData != animationDataAsset)
							{
								animationDataAsset = selectedAnimationData;
								if (animationDataAsset != null)
								{
									AnimationData animationData = animationDataAsset.animationData.DeepCopy();
									UseAnimationData(animationData);
									animationName = animationDataAsset.animationData.animationName;
								}
							}

							GUIState.BeginEnabling(animationDataEditor.HasAnimationData());
							if (GUILayout.Button("Save", GUILayout.Width(100)))
							{
								SaveAnimationData();
							}
							GUIState.EndEnabling();

							if (GUILayout.Button("Load JSON", GUILayout.Width(100)))
							{
								LoadAnimationDataFromJson();
							}
						}
						EditorGUILayout.EndHorizontal();

						if (animationPlayer.GetFramesCount() > 0)
						{
							EditorGUILayout.Space();
							EditorGUILayout.Space();
							// Add input field for animation name
							animationName = EditorGUILayout.TextField("Animation Name", animationName);

							if (animationPlayer.IsPlaying)
							{
								if (GUILayout.Button("Stop"))
									animationPlayer.StopPlaying();
							}
							else
							{
								if (GUILayout.Button("Play"))
								{
									animationPlayer.StartPlaying(animationPlayer.CurrentFrame >= animationPlayer.GetFramesCount() -1 ? 0 : animationPlayer.CurrentFrame);
								}
							}

							EditorGUILayout.BeginHorizontal();
							{
								useRotations = EditorGUILayout.Toggle(useRotations, GUILayout.Width(10));
								GUILayout.Label("Rotation", GUILayout.Width(70));

								useTranslations = EditorGUILayout.Toggle(useTranslations, GUILayout.Width(10));
								GUILayout.Label("Translation", GUILayout.Width(90));

								useScales = EditorGUILayout.Toggle(useScales, GUILayout.Width(10));
								GUILayout.Label("Scale", GUILayout.Width(60));

								if (animationPlayer.ApplyRotations != useRotations ||
									animationPlayer.ApplyTranslations != useTranslations ||
									animationPlayer.ApplyScales != useScales)
								{
									animationPlayer.ApplyRotations = useRotations;
									animationPlayer.ApplyTranslations = useTranslations;
									animationPlayer.ApplyScales = useScales;

									initialPose.ApplyPose();
									animationPlayer.ApplyFrame(animationPlayer.CurrentFrame);
								}
							}
							EditorGUILayout.EndHorizontal();

							EditorGUILayout.Space();

							GUIState.BeginEnabling(!isAnimatorPlaying && !animationPlayer.IsPlaying);
							animationDataEditor.DrawControls();
							DrawAnimationClipControls();
							DrawFbxExportControls();
							GUIState.EndEnabling();
						}
					}
					EditorGUILayout.EndVertical();
				}
			}
			GUIState.EndEnabling();

			EditorGUILayout.Space();
		}

		private void DrawAnimationClipControls()
		{
			EditorGUILayout.Space();

			EditorGUILayout.BeginVertical("Box");
			animationClipFoldout = EditorGUILayout.Foldout(animationClipFoldout, "Animation Clip", true, foldoutStyle);
			EditorGUILayout.EndVertical();
			if (animationClipFoldout)
			{
				EditorGUI.indentLevel++;
				animationClipSettings.active = EditorGUILayout.Foldout(animationClipSettings.active, "Settings", true, foldoutStyle);
				if (animationClipSettings.active)
				{
					EditorGUI.indentLevel++;
					animationClipSettings.saveRotations = EditorGUILayout.Toggle("Save Rotations", animationClipSettings.saveRotations);
					animationClipSettings.saveTranslations = EditorGUILayout.Toggle("Save Translations", animationClipSettings.saveTranslations);
					animationClipSettings.saveScales = EditorGUILayout.Toggle("Save Scales", animationClipSettings.saveScales);
					EditorGUI.indentLevel--;
				}

				EditorGUILayout.BeginHorizontal();
				{
					outputAnimationClip = (AnimationClip)EditorGUILayout.ObjectField("Output Animation Clip", outputAnimationClip, typeof(AnimationClip), false);
					if (GUILayout.Button("Save", GUILayout.Width(100)))
					{
						SaveAnimationClip();
					}
				}
				EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel--;
			}
		}

		private void DrawFbxExportControls()
		{
			EditorGUILayout.Space();

			EditorGUILayout.BeginVertical("Box");
			fbxFoldout = EditorGUILayout.Foldout(fbxFoldout, "FBX Export", true, foldoutStyle);
			EditorGUILayout.EndVertical();
			if (fbxFoldout)
			{
				EditorGUI.indentLevel++;

				if (FbxExporter.IsFbxExportSupported)
				{
					fbxSettings.active = EditorGUILayout.Foldout(fbxSettings.active, "Settings", true, foldoutStyle);
					if (fbxSettings.active)
					{
						EditorGUI.indentLevel++;
						fbxSettings.modelScale = EditorGUILayout.FloatField("Model Scale", fbxSettings.modelScale, GUILayout.Width(250));
						if (fbxSettings.modelScale <= 0)
							fbxSettings.modelScale = 1.0f;
						fbxSettings.fileFormat = (FbxFileFormat)EditorGUILayout.EnumPopup("File Format", fbxSettings.fileFormat, GUILayout.Width(250));
						fbxSettings.exportMeshes = EditorGUILayout.Toggle("Export Meshes", fbxSettings.exportMeshes);
						fbxSettings.exportTextures = EditorGUILayout.Toggle("Export Textures", fbxSettings.exportTextures);
						fbxSettings.exportRotations = EditorGUILayout.Toggle("Export Rotations", fbxSettings.exportRotations);
						fbxSettings.exportTranslations = EditorGUILayout.Toggle("Export Translations", fbxSettings.exportTranslations);
						fbxSettings.exportScales = EditorGUILayout.Toggle("Export Scales", fbxSettings.exportScales);
						EditorGUI.indentLevel--;
					}

					EditorGUILayout.BeginHorizontal();
					{
						fbxOutputFolder = EditorGUILayout.ObjectField("Output FBX Folder", fbxOutputFolder, typeof(DefaultAsset), false);
						if (GUILayout.Button("Export", GUILayout.Width(100)))
							ExportToFbx();
					}
					EditorGUILayout.EndHorizontal();
				}
				else
				{
					EditorGUILayout.BeginVertical();
					GUILayout.TextArea("This version of the plugin does not support exporting animations to FBX. For full export capabilities, including FBX export, consider upgrading to the", textAreaLabelStyle);

					if (GUILayout.Button("Animation Recorder Pro", linkStyle))
					{
						Application.OpenURL("https://u3d.as/3hTq");
					}
					EditorGUILayout.EndVertical();
				}
				EditorGUI.indentLevel--;
			}
		}


		private double lastUpdateTime = 0;
		private void Update()
		{
			if (lastUpdateTime == 0)
			{
				lastUpdateTime = EditorApplication.timeSinceStartup;
				return;
			}

			double currentTime = EditorApplication.timeSinceStartup;
			float deltaTime = (float)(currentTime - lastUpdateTime);
			lastUpdateTime = currentTime;

			if (animator != null && isAnimatorPlaying)
			{
				if (!Application.isPlaying)
					animator.Update(deltaTime);
				animationRecorder.Update(deltaTime);
			}

			if (animationPlayer != null && animationPlayer.IsPlaying)
			{
				animationPlayer.Update(deltaTime);
				Repaint();
			}
		}

		private void OnAnimatorSet()
		{
			animationRecorder = new AnimationRecorder();
			animationRecorder.Initialize(animator.gameObject);
			animationPlayer = new AnimationPlayer(animator.gameObject, useTranslations, useRotations, useScales, true);
			animationDataEditor.SetAnimationPlayer(animationPlayer);
			initialPose = new Pose(animator.transform);
		}

		private void StartAnimation()
		{
			animator.Play(0);
			isAnimatorPlaying = true;
			if (!startRecordingManually)
				animationRecorder.StartCapturing();
		}

		private void StopAnimation()
		{
			animator.StopPlayback();
			isAnimatorPlaying = false;

			if (animationRecorder.IsCapturing)
				StopRecordAnimation();
		}

		private void StartRecordAnimation()
		{
			animationRecorder.StartCapturing();
		}

		private void StopRecordAnimation()
		{
			animationRecorder.StopCapturing();

			AnimationData animationData = animationRecorder.GetCapturedData();
			UseAnimationData(animationData);
			animationDataAsset = null;

			if (animator != null && animator.GetCurrentAnimatorClipInfo(0).Length > 0)
				animationName = animator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
		}

		private void SaveAnimationData()
		{
			if (animationDataAsset == null)
			{
				if (string.IsNullOrEmpty(animationName))
					animationName = "New Animation";
				string outputAnimationDataFilename = Path.Combine("Assets", "DevEloop", "AnimationRecorder", "RecordedAnimations", animationName + ".asset");
				string outputDir = Path.GetDirectoryName(outputAnimationDataFilename);
				if (!Directory.Exists(outputDir))
					Directory.CreateDirectory(outputDir);

				animationDataAsset = AssetDatabase.LoadAssetAtPath<AnimationDataAsset>(outputAnimationDataFilename);
				if (animationDataAsset == null)
				{
					animationDataAsset = ScriptableObject.CreateInstance<AnimationDataAsset>();
					AssetDatabase.CreateAsset(animationDataAsset, outputAnimationDataFilename);
				}
			}

			animationDataAsset.animationData = animationDataEditor.GetAnimationData();
			animationDataAsset.animationData.animationName = animationName;

			EditorUtility.SetDirty(animationDataAsset);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private void SaveAnimationClip()
		{
			if (outputAnimationClip == null)
			{
				if (string.IsNullOrEmpty(animationName))
					animationName = "New Animation";
				string outputAnimationClipFilename = Path.Combine("Assets", "DevEloop", "AnimationRecorder", "RecordedAnimations", animationName + ".anim");
				string outputDir = Path.GetDirectoryName(outputAnimationClipFilename);
				if (!Directory.Exists(outputDir))
					Directory.CreateDirectory(outputDir);

				outputAnimationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputAnimationClipFilename);
				if (outputAnimationClip == null)
				{
					outputAnimationClip = new AnimationClip();
					AssetDatabase.CreateAsset(outputAnimationClip, outputAnimationClipFilename);
				}
			}

			AnimationClipConstructor.AddAnimationClipData(outputAnimationClip, animationDataEditor.GetAnimationData(),
				animationClipSettings.saveTranslations, animationClipSettings.saveRotations, animationClipSettings.saveScales);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private void LoadAnimationDataFromJson()
		{
			string path = EditorUtility.OpenFilePanel("Select Animation Data JSON", "", "json");
			if (!string.IsNullOrEmpty(path))
			{
				try
				{
					string jsonContent = File.ReadAllText(path);
					AnimationData animationData = JsonUtility.FromJson<AnimationData>(jsonContent);
					UseAnimationData(animationData);
					animationDataAsset = null;

				}
				catch (System.Exception ex)
				{
					Debug.LogError("Failed to load animation data from JSON: " + ex.Message);
				}
			}
		}

		private void UseAnimationData(AnimationData animationData)
		{
			animationDataEditor.SetAnimationData(animationData, animator.transform);
			animationPlayer.SetAnimationData(animationData);
			animationPlayer.ApplyFrame(0);
		}

		private void ExportToFbx()
		{
			string folderPath = AssetDatabase.GetAssetPath(fbxOutputFolder);
			if (string.IsNullOrEmpty(folderPath))
			{
				folderPath = Path.Combine("Assets", "DevEloop", "AnimationRecorder", "RecordedAnimations");
				if (!Directory.Exists(folderPath))
				{
					Directory.CreateDirectory(folderPath);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}
				fbxOutputFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
			}

			if (string.IsNullOrEmpty(animationName))
				animationName = "New Animation";

			string outputFbxPath = Path.Combine(folderPath, animationName + ".fbx");

			FbxExporter fbxExporter = new FbxExporter();
			fbxExporter.ModelScale = fbxSettings.modelScale;
			fbxExporter.FileFormat = fbxSettings.fileFormat;
			fbxExporter.ExportMeshes = fbxSettings.exportMeshes;
			fbxExporter.ExportTextures = fbxSettings.exportTextures;
			fbxExporter.ExportRotations = fbxSettings.exportRotations;
			fbxExporter.ExportTranslations = fbxSettings.exportTranslations;
			fbxExporter.ExportScales = fbxSettings.exportScales;

			fbxExporter.ExportAnimationToFbx(animator.gameObject, animationDataEditor.GetAnimationData(), outputFbxPath);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			Debug.Log("FBX Exported to: " + outputFbxPath);
		}
	}
}
