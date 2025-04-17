using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace DevEloop.AnimationRecorder
{
	public class AnimationRecorderSample : MonoBehaviour
	{
		public SampleStep capturingSampleStep;
		public SampleStep playingSampleStep;
		public SampleStep savingSampleStep;

		private void Start()
		{
			capturingSampleStep.stepCompleted += () => playingSampleStep.StartStep();
			playingSampleStep.stepCompleted += () => savingSampleStep.StartStep();

			capturingSampleStep.StartStep();
		}
	}
}
