using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DevEloop.AnimationRecorder
{
	public class SavingSampleStep : SampleStep
	{
		public AnimationRecorderComponent animationRecorder;

		public GameObject sourceModel;

		public Button saveAnimationClipButton;

		public Button exportFbxButton;

		public Button openProVersionPluginButton;

		public Text fbxExportText;

		public Text fbxExportNotAvailableText;

		public Text statusText;

		public Button saveAnimationDataButton;

		public override void StartStep()
		{
			base.StartStep();

#if UNITY_EDITOR
			saveAnimationClipButton.interactable = true;
			saveAnimationDataButton.interactable = true;
#else
			saveAnimationClipButton.interactable = false;
			saveAnimationDataButton.interactable = false;
#endif

			if (FbxExporter.IsFbxExportSupported)
			{
				openProVersionPluginButton.gameObject.SetActive(false);
				fbxExportText.gameObject.SetActive(true);
				fbxExportNotAvailableText.gameObject.SetActive(false);
				exportFbxButton.interactable = true;
			}
			else
			{
				openProVersionPluginButton.gameObject.SetActive(true);
				fbxExportText.gameObject.SetActive(false);
				fbxExportNotAvailableText.gameObject.SetActive(true);
				exportFbxButton.interactable = false;
			}
		}

		public void OnSaveAnimationClipButtonClick()
		{
#if UNITY_EDITOR
			statusText.text = string.Empty;

			string outputAnimationClipFilename = Path.Combine("Assets", "DevEloop", "AnimationRecorder", "RecordedAnimations", "New Animation.anim");
			string outputDir = Path.GetDirectoryName(outputAnimationClipFilename);
			if (!Directory.Exists(outputDir))
				Directory.CreateDirectory(outputDir);

			AnimationClip outputAnimationClip = new AnimationClip();
			AssetDatabase.CreateAsset(outputAnimationClip, outputAnimationClipFilename);

			AnimationData animationData = animationRecorder.GetAnimationRecorder().GetCapturedData();
			AnimationClipConstructor.AddAnimationClipData(outputAnimationClip, animationData);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			statusText.text = "Animation clip saved to " + outputAnimationClipFilename;
#endif
		}

		public void OnFbxExportButtonClick()
		{
			if (FbxExporter.IsFbxExportSupported)
			{
				statusText.text = string.Empty;

				string outputFbxFilename = Path.Combine(Application.dataPath, "DevEloop", "AnimationRecorder", "RecordedAnimations", "New Animation.fbx");
				string outputDir = Path.GetDirectoryName(outputFbxFilename);
				if (!Directory.Exists(outputDir))
					Directory.CreateDirectory(outputDir);

				AnimationData animationData = animationRecorder.GetAnimationRecorder().GetCapturedData();
				FbxExporter fbxExporter = new FbxExporter();
				fbxExporter.ExportRotations = true;
				fbxExporter.ExportTranslations = true;
				fbxExporter.ExportScales = true;
				fbxExporter.ExportAnimationToFbx(sourceModel, animationData, outputFbxFilename);

#if UNITY_EDITOR
				UnityEditor.AssetDatabase.SaveAssets();
				UnityEditor.AssetDatabase.Refresh();
#endif

				statusText.text = "Animation clip saved to " + outputFbxFilename;
			}
		}

		public void OnSaveAnimationDataButtonClick()
		{
#if UNITY_EDITOR
			statusText.text = string.Empty;

			string outputAnimationDataFilename = Path.Combine("Assets", "DevEloop", "AnimationRecorder", "RecordedAnimations", "New Animation.asset");
			string outputDir = Path.GetDirectoryName(outputAnimationDataFilename);
			if (!Directory.Exists(outputDir))
				Directory.CreateDirectory(outputDir);

			AnimationData animationData = animationRecorder.GetAnimationRecorder().GetCapturedData();
			
			AnimationDataAsset animationDataAsset = ScriptableObject.CreateInstance<AnimationDataAsset>();
			animationDataAsset.animationData = animationData;
			animationDataAsset.animationData.animationName = "New Animation";
			AssetDatabase.CreateAsset(animationDataAsset, outputAnimationDataFilename);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			statusText.text = "Animation data saved to " + outputAnimationDataFilename;
#endif
		}

		public void OnSaveJsonButtonClick()
		{
			string outputJsonFilename = Path.Combine(Application.dataPath, "DevEloop", "AnimationRecorder", "RecordedAnimations", "New Animation.json");
			string outputDir = Path.GetDirectoryName(outputJsonFilename);
			if (!Directory.Exists(outputDir))
				Directory.CreateDirectory(outputDir);

			AnimationData animationData = animationRecorder.GetAnimationRecorder().GetCapturedData();
			File.WriteAllText(outputJsonFilename, JsonUtility.ToJson(animationData, true));

#if UNITY_EDITOR
			UnityEditor.AssetDatabase.SaveAssets();
			UnityEditor.AssetDatabase.Refresh();
#endif

			statusText.text = "Animation saved to " + outputJsonFilename;
		}

		public void OnOpenProVersionButtonClick()
		{
			Application.OpenURL("https://u3d.as/3hTq");
		}
	}
}
