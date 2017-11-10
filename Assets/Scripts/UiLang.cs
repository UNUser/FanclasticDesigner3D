

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts {

	public class UiLang : MonoBehaviour
	{
		public static readonly Dictionary<string, string>[] Dictionaries = {
			new Dictionary<string, string>
			{
				{"EditorLayer.ActiveColor", "Active color"},
				{"EditorLayer.Details", "Details"},
				

				{"Menu.Menu", "Menu"},
				{"Menu.Save", "Save..."},
				{"Menu.Load", "Load..."},
				{"Menu.Export", "Export..."},
				{"Menu.SceneInfo", "Scene Info"},
				{"Menu.Exit", "Exit"},

				{"ModeSwitcher.Editor", "Editor mode"},
				{"ModeSwitcher.Instructions", "Instructions mode"},

				{"SceneInfoLayer.Copy", "Copy"},
				{"SceneInfoLayer.Height", "Height:"},
				{"SceneInfoLayer.Length", "Length:"},
				{"SceneInfoLayer.Width", "Width:"},
				{"SceneInfoLayer.cm", "cm"},
				{"SceneInfoLayer.Black", "Black"},
				{"SceneInfoLayer.Blue", "Blue"},
				{"SceneInfoLayer.Brown", "Brown"},
				{"SceneInfoLayer.Gray", "Gray"},
				{"SceneInfoLayer.Green", "Green"},
				{"SceneInfoLayer.Red", "Red"},
				{"SceneInfoLayer.White", "White"},
				{"SceneInfoLayer.Yellow", "Yellow"},

				{"FileSelectionDialogLayer.SelectFile", "Select file..."},
				{"FileSelectionDialogLayer.EnterName", "Enter name..."},
				{"FileSelectionDialogLayer.Up", "Up"},
				{"FileSelectionDialogLayer.Downloads", "Downloads"},
				{"FileSelectionDialogLayer.Save", "Save"},
				{"FileSelectionDialogLayer.Cancel", "Cancel"},
			}, 
			new Dictionary<string, string>
			{
				{"EditorLayer.ActiveColor", "Активный цвет"},
				{"EditorLayer.Details", "Детали"},

				{"Menu.Menu", "Меню"},
				{"Menu.Save", "Сохранить..."},
				{"Menu.Load", "Загрузить..."},
				{"Menu.Export", "Экспорт..."},
				{"Menu.SceneInfo", "Инфо сцены"},
				{"Menu.Exit", "Выход"},

				{"ModeSwitcher.Editor", "Редактор"},
				{"ModeSwitcher.Instructions", "Инструкции"},

				{"SceneInfoLayer.Copy", "Копир."},
				{"SceneInfoLayer.Height", "Высота:"},
				{"SceneInfoLayer.Length", "Длина:"},
				{"SceneInfoLayer.Width", "Ширина:"},
				{"SceneInfoLayer.cm", "см"},
				{"SceneInfoLayer.Black", "Черный"},
				{"SceneInfoLayer.Blue", "Синий"},
				{"SceneInfoLayer.Brown", "Коричневый"},
				{"SceneInfoLayer.Gray", "Серый"},
				{"SceneInfoLayer.Green", "Зеленый"},
				{"SceneInfoLayer.Red", "Красный"},
				{"SceneInfoLayer.White", "Белый"},
				{"SceneInfoLayer.Yellow", "Желтый"},

				{"FileSelectionDialogLayer.SelectFile", "Выберите файл..."},
				{"FileSelectionDialogLayer.EnterName", "Введите имя..."},
				{"FileSelectionDialogLayer.Up", "Наверх"},
				{"FileSelectionDialogLayer.Downloads", "Загрузки"},
				{"FileSelectionDialogLayer.Save", "Сохран."},
				{"FileSelectionDialogLayer.Cancel", "Отмена"},
			} 
		};

		public static int Lang
		{
			get
			{
				if (_lang != -1) {
					return _lang;
				}

				if (!PlayerPrefs.HasKey("Language")) {
					PlayerPrefs.SetInt("Language", 0);
				}

				_lang = PlayerPrefs.GetInt("Language");

				return _lang;
			}

			set
			{
				_lang = value;
				PlayerPrefs.SetInt("Language", value);
			}
		}
		private static int _lang = -1;

		public string TextKey;

		private Text _text;

		public void Awake()
		{
			AppController.Instance.LanguageChanged += SetLanguage;
			SetLanguage(Lang);
		}

		public void SetLanguage(int index)
		{
			if (_text == null) {

				_text = GetComponent<Text>();

				if (_text == null) {
					Debug.LogError("Text component not found: " + gameObject.name);
					return;
				}
			}

			string newText;

			if (!Dictionaries[index].TryGetValue(TextKey, out newText))
			{
				Debug.LogError("Key " + TextKey + " not found in dictionary " + index);
				return;
			}

			_text.text = newText;
		}
	}
}
