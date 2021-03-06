﻿using System.Linq;
using UnityEngine;

namespace Assets.Scripts 
{
	public class AxisMover : MonoBehaviour
	{
		public GameObject Root;
		public Canvas Canvas;

		private SelectedDetails Selected { get { return _selected ?? (_selected = AppController.Instance.SelectedDetails); } }
		private SelectedDetails _selected;
		private readonly Vector3 _screenOriginOffset = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);

		protected void OnDisable()
		{
			Root.SetActive(false);
		}

		public Vector3 Correction { get; set; }


		public void Update()
		{
			if (!Selected.Selected.Any()) {
				Root.SetActive(false);
				return;
			}

			var bounds = Selected.First.Bounds;

			foreach (var detail in Selected.Selected) {
				bounds.Encapsulate(detail.Bounds);
			}

			var centerToScreenPoint = Camera.main.WorldToScreenPoint(bounds.center - Correction);

			if (centerToScreenPoint.z < 0) {
				Root.SetActive(false);
				return;
			}

			var rootLocalPos = centerToScreenPoint - _screenOriginOffset;

			rootLocalPos.z = 0;

			Root.transform.localPosition = rootLocalPos;
			Root.transform.rotation = Quaternion.identity;

			Root.SetActive(true);
		}
	}
}
