using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DevEloop.AnimationRecorder
{
	public class MouseFollower : MonoBehaviour
	{
		public Camera activeCamera;
		public GameObject head;
		public GameObject rotationTarget;

		private Vector3 headProjectionPoint;
		private float dzx;
		private float dzy;
		private bool isMoving = false;

		private void Start()
		{
			headProjectionPoint = activeCamera.WorldToScreenPoint(head.transform.position);
			dzx = Screen.width / 2.0f;
			dzy = Screen.height / 2.0f;
		}

		void Update()
		{
			if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
			{
				if (!isMoving && !IsPointerOverUIObject())
					isMoving = true;

				if (isMoving)
				{
					Vector3 mousePosition = Input.mousePosition;
					Vector3 delta = headProjectionPoint - mousePosition;
					float alphaX = (float)Math.Atan(delta.x / dzx);
					float alphaY = (float)Math.Atan(delta.y / dzy);

					Vector3 angles = rotationTarget.transform.rotation.eulerAngles;
					angles.y = alphaX * 180.0f / (float)Math.PI;
					angles.x = alphaY * 180.0f / (float)Math.PI;
					rotationTarget.transform.rotation = Quaternion.Euler(angles);
				}
			}
			else
				isMoving = false;
		}

		protected bool IsPointerOverUIObject()
		{
			PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current)
			{
				position = new Vector2(Input.mousePosition.x, Input.mousePosition.y)
			};
			List<RaycastResult> results = new List<RaycastResult>();
			EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

			return results.Count > 0;
		}
	}
}
