using System.Linq;
using UnityEngine;

namespace Assets.Scripts 
{
	public class AxisMover : MonoBehaviour
	{
		public GameObject Root;


		private SelectedDetails _selected;
		private readonly Vector3 _screenSourceOffset = new Vector3(Screen.width / 2, Screen.height / 2, 0);

		public void Start()
		{
			_selected = AppController.Instance.SelectedDetails;
		}


		public void UpdatePos()
		{
			
		}

		protected void Update()
		{
			Root.SetActive(_selected.Selected.Any());

			if (!Root.activeSelf) {
				return;
			}

			Root.transform.rotation = Quaternion.identity;

			var bounds =_selected.First.Bounds;

			foreach (var detail in _selected.Selected) {
				bounds.Encapsulate(detail.Bounds);
			}

			var rootPos = Camera.main.WorldToScreenPoint(bounds.center);

			Root.transform.localPosition = rootPos - _screenSourceOffset;
		}
	}
}
