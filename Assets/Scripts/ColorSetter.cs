
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts {

	public class ColorSetter : MonoBehaviour
	{

		public Transform AvailableColors;
		public ToggleGroup ColorSettersGroup;
		public Image CurrentColor;
		public Toggle ColorsPanelOpener;

		public DetailColor ActiveColor
		{
			get {
				return _activeColor;
			}
			set {
				if (value == _activeColor) {
					return;
				}

				_activeColor = value;
				foreach (Transform child in AvailableColors) {
					var toggle = child.GetComponent<Toggle>();

					toggle.isOn = value != null && toggle.targetGraphic.color == value.UiColor;
				}
			}
		}

		public GameObject ColorSetterPrefab;

		public event Action<DetailColor> ActiveColorChangedEvent;

		private DetailColor _activeColor;

		public void Start()
		{
			var colors = AppController.Instance.Resources.Colors;

			for (var i = 0; i < colors.Length; i++) {
				var colorToggle = Instantiate(ColorSetterPrefab).GetComponent<Toggle>();
				var index = i;

				colorToggle.targetGraphic.color = colors[i].UiColor;
				colorToggle.isOn = i == 0;
				colorToggle.onValueChanged.AddListener(value => {
					if (value) { OnToggleOn(index); }
				});
				colorToggle.group = ColorSettersGroup;
				colorToggle.transform.SetParent(AvailableColors, false);

			}
			ActiveColor = colors[0];
			CurrentColor.color = colors[0].UiColor;
			OnToggleOn(0);
		}

		private void OnToggleOn(int toggleIndex)
		{
			var colors = AppController.Instance.Resources.Colors;
			var selected = EventSystem.current.currentSelectedGameObject;
			var userClicked = selected != null && selected.GetComponent<Toggle>() != null;

			ActiveColor = colors[toggleIndex];

			CurrentColor.color = colors[toggleIndex].UiColor;
			ColorsPanelOpener.isOn = false;
			if (ActiveColorChangedEvent != null && userClicked) {
				ActiveColorChangedEvent(colors[toggleIndex]);
			}
		}
	}
}
