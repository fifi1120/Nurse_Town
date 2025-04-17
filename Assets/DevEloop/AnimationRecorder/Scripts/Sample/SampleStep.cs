using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DevEloop.AnimationRecorder
{
	public class SampleStep : MonoBehaviour
	{
		public GameObject uiControls;

		public event Action stepCompleted;

		public virtual void StartStep()
		{
			uiControls.SetActive(true);
		}

		public virtual void CompleteStep()
		{
			uiControls.SetActive(false);
			stepCompleted?.Invoke();
		}
	}
}
