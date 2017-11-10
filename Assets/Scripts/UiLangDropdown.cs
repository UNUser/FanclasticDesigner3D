

using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts {
	public class UiLangDropdown : MonoBehaviour
	{
		public string[] OptionsKeys;

		private Dropdown _dropdown;

		public void Awake() {
			AppController.Instance.LanguageChanged += SetLanguage;
			SetLanguage(UiLang.Lang);
		}

		public void SetLanguage(int index)
		{
			if (_dropdown == null) {

				_dropdown = GetComponent<Dropdown>();

				if (_dropdown == null) {
					Debug.LogError("Dropdown component not found: " + gameObject.name);
					return;
				}
			}

			for (var i = 0; i <_dropdown.options.Count; i++)
			{
				var optionData = _dropdown.options[i];
				string newText;

				if (!UiLang.Dictionaries[index].TryGetValue(OptionsKeys[i], out newText)) {
					Debug.LogError("Key " + OptionsKeys[i] + " not found in dictionary " + index);
					continue;
				}

				optionData.text = newText;

				if (_dropdown.value == i) {
					_dropdown.captionText.text = newText;
				}
			}

		}
	}
}
