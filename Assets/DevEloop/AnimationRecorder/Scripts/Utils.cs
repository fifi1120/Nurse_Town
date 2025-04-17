using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DevEloop.AnimationRecorder
{
	public static class Utils
	{
		public static string GetNodePath(Transform targetNode, Transform rootNode)
		{
			string nodePath = string.Empty;
			while (targetNode != rootNode && targetNode != null)
			{
				if (string.IsNullOrEmpty(nodePath))
					nodePath = targetNode.name;
				else
					nodePath = targetNode.name + "/" + nodePath;
				targetNode = targetNode.parent;
			}
			return nodePath;
		}

		public static Vector3 GetTranslationFromMatrix(Matrix4x4 m)
		{
			return m.GetColumn(3);
		}

		public static Quaternion GetRotationFromMatrix(Matrix4x4 m)
		{
			Matrix4x4 normalizedMatrix = m;
			normalizedMatrix.SetColumn(0, normalizedMatrix.GetColumn(0).normalized);
			normalizedMatrix.SetColumn(1, normalizedMatrix.GetColumn(1).normalized);
			normalizedMatrix.SetColumn(2, normalizedMatrix.GetColumn(2).normalized);

			return Quaternion.LookRotation(normalizedMatrix.GetColumn(2), normalizedMatrix.GetColumn(1));
		}

		public static Vector3 GetScaleFromMatrix(Matrix4x4 m)
		{
			Vector3 scale;
			scale.x = m.GetColumn(0).magnitude;
			scale.y = m.GetColumn(1).magnitude;
			scale.z = m.GetColumn(2).magnitude;
			return scale;
		}
	}
}
