using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DevEloop.AnimationRecorder
{
	public class PlayingSampleStep : SampleStep
	{
		public AnimationRecorderComponent animationRecorderComponent;

		public AnimationPlayerComponent animationPlayerComponent;

		public Button playButton;

		public Button stopButton;

		public override void StartStep()
		{
			base.StartStep();

			AnimationData animationData = animationRecorderComponent.GetAnimationRecorder().GetCapturedData();
			animationPlayerComponent.SetAnimationData(animationData);
		}

		public void OnPlayAnimationButtonClick()
		{
			animationPlayerComponent.StartPlaying();
		}

		public void OnStopAnimationButtonClick()
		{
			animationPlayerComponent.StopPlaying();
		}

		public void OnNextStepButtonClick()
		{
			animationPlayerComponent.StopPlaying();
			CompleteStep();
		}

		protected void Update()
		{
			playButton.interactable = !animationPlayerComponent.IsPlaying;
			stopButton.interactable = animationPlayerComponent.IsPlaying;
		}
	}
}
