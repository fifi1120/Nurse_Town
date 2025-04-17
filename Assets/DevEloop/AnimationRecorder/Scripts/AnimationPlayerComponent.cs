using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DevEloop.AnimationRecorder
{
	public class AnimationPlayerComponent : MonoBehaviour
	{
		public GameObject model;

		public string animationJsonFilename = null;

		public bool applyTranslations = true;

		public bool applyRotations = true;

		public bool applyScales = true;

		public bool startPlayingAutomatically = true;

		public bool loopPlaying = false;

		public bool localPoses = true;

		private AnimationPlayer animationPlayer;

		private GameObject targetModel = null;

		private void Start()
		{
			targetModel = model;

			animationPlayer = new AnimationPlayer(model, applyTranslations, applyRotations, applyScales, localPoses);

			if (!string.IsNullOrEmpty(animationJsonFilename))
			{
				if (File.Exists(animationJsonFilename))
					animationPlayer.LoadAnimationData(animationJsonFilename);
				else
					Debug.LogErrorFormat("Unable to load animation from JSON. File doesn't exist: {0}", animationJsonFilename);
			}
			
			if (startPlayingAutomatically)
				StartPlaying();
		}

		private void Update()
		{
			if (targetModel != model)
			{
				targetModel = model;
				animationPlayer.SetAnimatedModel(targetModel);
			}

			animationPlayer.Update(Time.deltaTime);
		}

		public void StartPlaying()
		{
			animationPlayer.StartPlaying();
		}

		public void StopPlaying()
		{
			animationPlayer.StopPlaying();
		}

		public bool IsPlaying
		{
			get { return animationPlayer.IsPlaying; }
		}

		public void SetAnimationData(AnimationData animationData)
		{
			animationPlayer.SetAnimationData(animationData);
		}
	}
}
