
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

		public Material ActiveColor
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

					toggle.isOn = value != null && toggle.targetGraphic.color == value.color;
				}
			}
		}

		public GameObject ColorSetterPrefab;

		public event Action<Material> ActiveColorChangedEvent;

		private Material _activeColor;

		public void Start()
		{
			var materials = AddDetailPanel.Materials;

			for (var i = 0; i < materials.Length; i++) {
				var colorToggle = Instantiate(ColorSetterPrefab).GetComponent<Toggle>();
				var index = i;

				colorToggle.targetGraphic.color = materials[i].color;
				colorToggle.isOn = i == 0;
				colorToggle.onValueChanged.AddListener(value => {
					if (value) { OnToggleOn(index); }
				});
				colorToggle.group = ColorSettersGroup;
				colorToggle.transform.SetParent(AvailableColors, false);

			}
			ActiveColor = materials[0];
			CurrentColor.color = materials[0].color;
			OnToggleOn(0);
		}

		private void OnToggleOn(int toggleIndex)
		{
			var materials = AddDetailPanel.Materials;
			var selected = EventSystem.current.currentSelectedGameObject;
			var userClicked = selected != null && selected.GetComponent<Toggle>() != null;

			ActiveColor = materials[toggleIndex];

			CurrentColor.color = materials[toggleIndex].color;
			ColorsPanelOpener.isOn = false;
			if (ActiveColorChangedEvent != null && userClicked) {
				ActiveColorChangedEvent(materials[toggleIndex]);
			}
		}
	}
}
