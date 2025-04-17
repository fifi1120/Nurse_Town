using UnityEngine;

namespace DevEloop.AnimationRecorder
{
	[CreateAssetMenu(fileName = "NewAnimationData", menuName = "DevEloop/AnimationData")]
	public class AnimationDataAsset : ScriptableObject
	{
		public AnimationData animationData;
	}
}