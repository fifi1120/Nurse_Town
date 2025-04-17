using UnityEngine.UI;

namespace DevEloop.AnimationRecorder
{
	public class CapturingSampleStep : SampleStep
	{
		public Button startCapturingButton;

		public Button stopCapturingButton;

		public AnimationRecorderComponent animationRecorder;

		public override void StartStep()
		{
			base.StartStep();

			startCapturingButton.interactable = true;
			stopCapturingButton.interactable = false;
		}

		public void OnStartCapturingButtonClick()
		{
			animationRecorder.StartCapturing();

			startCapturingButton.interactable = false;
			stopCapturingButton.interactable = true;
		}

		public void OnStopCapturingButtonClick()
		{
			animationRecorder.StopCapturing();

			CompleteStep();
		}
	}
}
