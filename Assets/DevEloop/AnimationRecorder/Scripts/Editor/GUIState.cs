using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevEloop.AnimationRecorder
{
	public static class GUIState
	{
		private static Stack<bool> enablingStack = new Stack<bool>();

		public static void BeginEnabling(bool enabled)
		{
			enablingStack.Push(enabled);
			SetState();
		}

		public static void EndEnabling()
		{
			enablingStack.Pop();
			SetState();
		}

		private static void SetState()
		{
			if (enablingStack.Count > 0)
				GUI.enabled = enablingStack.All(isEnabled => isEnabled);
			else
				GUI.enabled = true;
		}
	}
}
