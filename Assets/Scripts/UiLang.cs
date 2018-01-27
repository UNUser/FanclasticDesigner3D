

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
				{"EditorLayer.Details", "Elements"},

				{"Menu.Menu", "Menu"},
				{"Menu.Save", "Save..."},
				{"Menu.Load", "Load..."},
				{"Menu.Export", "Export..."},
				{"Menu.SceneInfo", "Scene Info"},
				{"Menu.Demo", "Demo"},
				{"Menu.Help", "Help"},
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
				{"SceneInfoLayer.BraceLineX2", "Brace Line (2x1)"},
				{"SceneInfoLayer.BraceLineX3", "Brace Line (3x1)"},
				{"SceneInfoLayer.BraceSquareX2", "Brace Square (2x1)"},
				{"SceneInfoLayer.BraceSquareX3", "Brace Square (3x1)"},
				{"SceneInfoLayer.Lego1x1", "Lego 1x1"},
				{"SceneInfoLayer.Lego2x1", "Lego 2x1"},
				{"SceneInfoLayer.Roll", "Roll"},

				{"FileSelectionDialogLayer.SelectFile", "Select file..."},
				{"FileSelectionDialogLayer.EnterName", "Enter name..."},
				{"FileSelectionDialogLayer.Up", "Up"},
				{"FileSelectionDialogLayer.Downloads", "Downloads"},
				{"FileSelectionDialogLayer.Save", "Save"},
				{"FileSelectionDialogLayer.Cancel", "Cancel"},

				{"TutorialLayer.Next", "Next"},
				{"TutorialLayer.Welcome", "Welcome to Fanclastic 3D Designer!\n\nThis program helps you to build Fanclastic models, view assembly instructions and create your own."},
				{"TutorialLayer.DetailsHint", "To add an element to the scene drag a corresponding icon from the elements' panel to the scene."},
				{"TutorialLayer.SetFocusHint", "To change camera anchor point click on the focus setting button and then on any element or spot on the scene."},
				{"TutorialLayer.ChangeColorHint", "Use the color select button to set a color for selected elements."},
				{"TutorialLayer.RotateHint", "To rotate selected elements use rotation buttons. Each button rotates elements around a drawn axis of a corresponding color."},
				{"TutorialLayer.DeleteHint", "Use this button to delete selected elements from the scene."},
				{"TutorialLayer.ZoomHint.Base", "Use {0} to zoom in and out the camera."},
				{"TutorialLayer.ZoomHint.Wheel", "mouse wheel"},
				{"TutorialLayer.ZoomHint.Pinch", "pinch"},
				{"TutorialLayer.LinkedSelection", "Click on an element and hold to select all linked parts."},
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
				{"Menu.Demo", "Демо"},
				{"Menu.Help", "Помощь"},
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
				{"SceneInfoLayer.BraceLineX2", "Скобка тонкая (2x1)"},
				{"SceneInfoLayer.BraceLineX3", "Скобка тонкая (3x1)"},
				{"SceneInfoLayer.BraceSquareX2", "Скобка квадратная (2x1)"},
				{"SceneInfoLayer.BraceSquareX3", "Скобка квадратная (3x1)"},
				{"SceneInfoLayer.Lego1x1", "Лего 1x1"},
				{"SceneInfoLayer.Lego2x1", "Лего 2x1"},
				{"SceneInfoLayer.Roll", "Валик"},

				{"FileSelectionDialogLayer.SelectFile", "Выберите файл..."},
				{"FileSelectionDialogLayer.EnterName", "Введите имя..."},
				{"FileSelectionDialogLayer.Up", "Наверх"},
				{"FileSelectionDialogLayer.Downloads", "Загрузки"},
				{"FileSelectionDialogLayer.Save", "Сохран."},
				{"FileSelectionDialogLayer.Cancel", "Отмена"},

				{"TutorialLayer.Next", "Далее"},
				{"TutorialLayer.Welcome", "Добро пожаловать в Fanclastic 3D Designer!\n\nВ этой программе вы можете строить модели из конструктора Fanclastic, просматривать инструкции по сборке и создавать свои собственные!"},
				{"TutorialLayer.DetailsHint", "Чтобы добавить на сцену новую деталь, перетащите соответствующую иконку с панели деталей на сцену."},
				{"TutorialLayer.SetFocusHint", "Чтобы изменить точку привязки камеры, нажмите на кнопку установки фокуса, а затем на любую деталь или точку на сцене."},
				{"TutorialLayer.ChangeColorHint", "Используйте кнопку выбора цвета, чтобы задать цвет выделенным деталям."},
				{"TutorialLayer.RotateHint", "Для поворота выделенных деталей в пространстве используйте кнопки поворота. Каждая кнопка поворачивает детали вокруг нарисованной оси соответствующего цвета."},
				{"TutorialLayer.DeleteHint", "Используйте эту кнопку, чтобы удалить выделенные детали со сцены."},
				{"TutorialLayer.ZoomHint.Base", "Используйте {0}, чтобы приблизить или удалить камеру."},
				{"TutorialLayer.ZoomHint.Wheel", "колесо мыши"},
				{"TutorialLayer.ZoomHint.Pinch", "жест двумя пальцами"},
				{"TutorialLayer.LinkedSelection", "Нажмите на деталь и удерживайте, чтобы выделить одновременно все связанные с ней детали."},
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
