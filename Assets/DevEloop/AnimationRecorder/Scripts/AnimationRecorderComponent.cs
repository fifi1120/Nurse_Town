using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DevEloop.AnimationRecorder
{
	public class AnimationRecorderComponent : MonoBehaviour
	{
		public GameObject animatedModel;

		public float capturingFPS = 30.0f;

		public bool captureLocalPoses = false;

		public bool startCapturingAutomatically = false;

		public bool captureRootMotion = true;

		public bool saveToJson = true;

		public string outputAnimationJsonFilename = null;

		public bool saveToAnimationClip = true;

		public AnimationClip outputAnimationClip = null;

		public List<Transform> nodesToCapture = new List<Transform>();

		public bool includeChildNodes = false;

		public List<Transform> nodesToExclude = new List<Transform>();

		public bool excludeChildNodes = false;

		private AnimationRecorder recorder = new AnimationRecorder();

		private void Start()
		{
			if (animatedModel == null)
			{
				Debug.LogError("AnimationRecorder: humanoid object isn't specified");
				return;
			}

			Application.focusChanged += val => 
			{
				if (!val)
				{
					if (recorder.IsCapturing && saveToJson)
						WriteCapturedDataToJson();
				}
			};

			recorder.capturingFPS = capturingFPS;
			recorder.captureLocalPoses = captureLocalPoses;
			recorder.captureRootMotion = captureRootMotion;
			recorder.Initialize(animatedModel, nodesToCapture, includeChildNodes, nodesToExclude, excludeChildNodes);

			if (startCapturingAutomatically)
				StartCapturing();
		}

		private void LateUpdate()
		{
			if (recorder.IsCapturing)
				recorder.Update(Time.deltaTime);
		}

		private void OnDisable()
		{
			if (recorder.IsCapturing)
				StopCapturing();
		}

		public void StartCapturing()
		{
			recorder.StartCapturing();
		}

		public void StopCapturing()
		{
			if (recorder.IsCapturing)
			{
				recorder.StopCapturing();

				if (saveToJson)
					WriteCapturedDataToJson();

#if UNITY_EDITOR
				if (saveToAnimationClip)
					CreateAnimationClip();
#endif
			}
		}

		public void CaptureSingleFrame()
		{
			if (!recorder.IsCapturing)
			{
				StartCapturing();
				StopCapturing();
			}
		}

		public AnimationRecorder GetAnimationRecorder()
		{
			return recorder;
		}

		private void WriteCapturedDataToJson()
		{
			AnimationData animationData = recorder.GetCapturedData();

			if (string.IsNullOrEmpty(outputAnimationJsonFilename))
				outputAnimationJsonFilename = GetOutputJsonFilename();

			string outputDirectory = Path.GetDirectoryName(outputAnimationJsonFilename);
			if (!Directory.Exists(outputDirectory))
				Directory.CreateDirectory(outputDirectory);
			string json = JsonUtility.ToJson(animationData, true);
			File.WriteAllText(outputAnimationJsonFilename, json);
		}

		private string GetOutputJsonFilename()
		{
			string filename = string.Empty;
			int number = 0;
			do
			{
				filename = Path.Combine(Application.persistentDataPath, "AnimationRecorder", string.Format("animation_{0}.json", number));
				number++;
			}
			while (File.Exists(filename));
			return filename;
		}

		private void CreateAnimationClip()
		{
#if UNITY_EDITOR
			if (outputAnimationClip == null)
			{
				string outputAnimationClipFilename = Path.Combine("Assets", "DevEloop", "AnimationRecorder", "RecordedAnimations", "animation.anim");
				string outputDir = Path.GetDirectoryName(outputAnimationClipFilename);
				if (!Directory.Exists(outputDir))
					Directory.CreateDirectory(outputDir);

				outputAnimationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputAnimationClipFilename);
				if (outputAnimationClip == null)
				{
					outputAnimationClip = new AnimationClip();
					AssetDatabase.CreateAsset(outputAnimationClip, outputAnimationClipFilename);
				}
			};

			AnimationData animationData = recorder.GetCapturedData();

			AnimationClipConstructor.AddAnimationClipData(outputAnimationClip, animationData);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
#else
			Debug.LogError("AnimationRecorder: Animation Clip can be created only in Editor.");
#endif
		}
	}
}
