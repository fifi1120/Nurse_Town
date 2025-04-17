using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DevEloop.AnimationRecorder
{
	public static class Extensions
	{
		public static Vector3 MirrorX(this Vector3 v)
		{
			return new Vector3(-v.x, v.y, v.z);
		}

		public static Quaternion MirrorX(this Quaternion q)
		{
			return new Quaternion(-q.x, q.y, q.z, -q.w);
		}
	}
}
