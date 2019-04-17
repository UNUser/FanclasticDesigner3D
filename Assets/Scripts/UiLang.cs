
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{

    //	public enum Languages
    //	{
    //		English,
    //		Russian,
    //	}

    public class UiLang : MonoBehaviour
    {

        //		public static readonly Dictionary<int, SystemLanguage>[] Lang2SystemLang = {
        //			new Dictionary<Languages, SystemLanguage>
        //			{
        //				{"EditorLayer.ActiveColor", "Active color"},
        //			}

        public static readonly Dictionary<string, string>[] Dictionaries = {
			new Dictionary<string, string>
			{
				{"EditorLayer.ActiveColor", "Active color"},
				{"EditorLayer.Details", "Elements"},

				{"Menu.Menu", "Menu"},
				{"Menu.NewScene", "New scene"},
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
				{"SceneInfoLayer.Weight", "Weight:"},
				{"SceneInfoLayer.TotalDetails", "Total details:"},
				{"SceneInfoLayer.cm", "cm"},
				{"SceneInfoLayer.gram", "g"},

				{"SceneInfoLayer.Black", "Black"},
				{"SceneInfoLayer.Blue", "Blue"},
				{"SceneInfoLayer.Brown", "Brown"},
				{"SceneInfoLayer.Gray", "Gray"},
				{"SceneInfoLayer.Green", "Green"},
				{"SceneInfoLayer.Red", "Red"},
				{"SceneInfoLayer.White", "White"},
				{"SceneInfoLayer.Yellow", "Yellow"},

				{"SceneInfoLayer.BraceLineX2", "Brace Line (2x1): "},
				{"SceneInfoLayer.BraceLineX3", "Brace Line (3x1): "},
				{"SceneInfoLayer.BraceSquareX2", "Brace Square (2x1): "},
				{"SceneInfoLayer.BraceSquareX3", "Brace Square (3x1): "},
				{"SceneInfoLayer.Lego1x1", "Lego 1x1: "},
				{"SceneInfoLayer.Lego2x1", "Lego 2x1: "},
				{"SceneInfoLayer.Roll", "Roll: "},
				{"SceneInfoLayer.RailX2", "Rail: "},

				{"SceneInfoLayer.Axle1.0", "Axle 1.0: "},
				{"SceneInfoLayer.Axle2.0", "Axle 2.0: "},
				{"SceneInfoLayer.Axle2.5", "Axle 2.5: "},
				{"SceneInfoLayer.Axle3.0", "Axle 3.0: "},
				{"SceneInfoLayer.Axle3.5", "Axle 3.5: "},
				{"SceneInfoLayer.Axle8.0", "Axle 8.0: "},

                {"SceneInfoLayer.AxleBevelGear", "Bevel Gear: "},
                {"SceneInfoLayer.AxleCoupling", "Coupling: "},
                {"SceneInfoLayer.AxleHub3x1", "Hub (3x1): "},
                {"SceneInfoLayer.AxleHub5x1", "Hub (5x1): "},
                {"SceneInfoLayer.AxleTip", "Axle Tip: "},
                {"SceneInfoLayer.AxleWasher", "Washer: "},
                {"SceneInfoLayer.AxleWasherToothed", "Toothed Washer: "},
                {"SceneInfoLayer.AxleWheelBig", "Big Wheel: "},
                {"SceneInfoLayer.AxleWheelSmall", "Small Wheel: "},

				{"FileSelectionDialogLayer.SelectFile", "Select file..."},
				{"FileSelectionDialogLayer.EnterName", "Enter name..."},
				{"FileSelectionDialogLayer.Up", "Up"},
				{"FileSelectionDialogLayer.Downloads", "Downloads"},
				{"FileSelectionDialogLayer.Save", "Save"},
				{"FileSelectionDialogLayer.Cancel", "Cancel"},

				{"TutorialLayer.Next", "Next"},
				{"TutorialLayer.Welcome", "Welcome to\nFanclastic Kids!\n\nThis program helps you to build Fanclastic models, view assembly instructions and create your own."},
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
				{"Menu.NewScene", "Новая сцена"},
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
				{"SceneInfoLayer.Weight", "Вес:"},
				{"SceneInfoLayer.TotalDetails", "Всего деталей:"},
				{"SceneInfoLayer.cm", "см"},
				{"SceneInfoLayer.gram", "г"},

				{"SceneInfoLayer.Black", "Черный"},
				{"SceneInfoLayer.Blue", "Синий"},
				{"SceneInfoLayer.Brown", "Коричневый"},
				{"SceneInfoLayer.Gray", "Серый"},
				{"SceneInfoLayer.Green", "Зеленый"},
				{"SceneInfoLayer.Red", "Красный"},
				{"SceneInfoLayer.White", "Белый"},
				{"SceneInfoLayer.Yellow", "Желтый"},

				{"SceneInfoLayer.Axle1.0", "Ось 1.0: "},
				{"SceneInfoLayer.Axle2.0", "Ось 2.0: "},
				{"SceneInfoLayer.Axle2.5", "Ось 2.5: "},
				{"SceneInfoLayer.Axle3.0", "Ось 3.0: "},
				{"SceneInfoLayer.Axle3.5", "Ось 3.5: "},
				{"SceneInfoLayer.Axle8.0", "Ось 8.0: "},

                {"SceneInfoLayer.AxleBevelGear", "Конусная шестеренка: "},
                {"SceneInfoLayer.AxleCoupling", "Муфта: "},
                {"SceneInfoLayer.AxleHub3x1", "Ступица (3x1): "},
                {"SceneInfoLayer.AxleHub5x1", "Ступица (5x1): "},
                {"SceneInfoLayer.AxleTip", "Осевой наконечник: "},
                {"SceneInfoLayer.AxleWasher", "Шайба: "},
                {"SceneInfoLayer.AxleWasherToothed", "Зубчатая шайба: "},
                {"SceneInfoLayer.AxleWheelBig", "Большое колесо: "},
                {"SceneInfoLayer.AxleWheelSmall", "Малое колесо: "},

				{"SceneInfoLayer.BraceLineX2", "Скобка тонкая (2x1): "},
				{"SceneInfoLayer.BraceLineX3", "Скобка тонкая (3x1): "},
				{"SceneInfoLayer.BraceSquareX2", "Скобка квадратная (2x1): "},
				{"SceneInfoLayer.BraceSquareX3", "Скобка квадратная (3x1): "},
				{"SceneInfoLayer.Lego1x1", "Лего 1x1: "},
				{"SceneInfoLayer.Lego2x1", "Лего 2x1: "},
				{"SceneInfoLayer.Roll", "Валик: "},
				{"SceneInfoLayer.RailX2", "Рельса: "},

				{"FileSelectionDialogLayer.SelectFile", "Выберите файл..."},
				{"FileSelectionDialogLayer.EnterName", "Введите имя..."},
				{"FileSelectionDialogLayer.Up", "Наверх"},
				{"FileSelectionDialogLayer.Downloads", "Загрузки"},
				{"FileSelectionDialogLayer.Save", "Сохран."},
				{"FileSelectionDialogLayer.Cancel", "Отмена"},

				{"TutorialLayer.Next", "Далее"},
				{"TutorialLayer.Welcome", "Добро пожаловать в\nFanclastic Kids!\n\nВ этой программе вы можете строить модели из конструктора Fanclastic, просматривать инструкции по сборке и создавать свои собственные!"},
				{"TutorialLayer.DetailsHint", "Чтобы добавить на сцену новую деталь, перетащите соответствующую иконку с панели деталей на сцену."},
				{"TutorialLayer.SetFocusHint", "Чтобы изменить точку привязки камеры, нажмите на кнопку установки фокуса, а затем на любую деталь или точку на сцене."},
				{"TutorialLayer.ChangeColorHint", "Используйте кнопку выбора цвета, чтобы задать цвет выделенным деталям."},
				{"TutorialLayer.RotateHint", "Для поворота выделенных деталей в пространстве используйте кнопки поворота. Каждая кнопка поворачивает детали вокруг нарисованной оси соответствующего цвета."},
				{"TutorialLayer.DeleteHint", "Используйте эту кнопку, чтобы удалить выделенные детали со сцены."},
				{"TutorialLayer.ZoomHint.Base", "Используйте {0}, чтобы приблизить или удалить камеру."},
				{"TutorialLayer.ZoomHint.Wheel", "колесо мыши"},
				{"TutorialLayer.ZoomHint.Pinch", "жест двумя пальцами"},
				{"TutorialLayer.LinkedSelection", "Нажмите на деталь и удерживайте, чтобы выделить одновременно все связанные с ней детали."},
			},
			new Dictionary<string, string>
			{
				{"EditorLayer.ActiveColor", "Aktive Farbe"},
				{"EditorLayer.Details", "Details"},

				{"Menu.Menu", "Menü"},
				{"Menu.NewScene", "Neue Szene"},
				{"Menu.Save", "Speichern..."},
				{"Menu.Load", "Herunterladen..."},
				{"Menu.Export", "Export..."},
				{"Menu.SceneInfo", "Szenen info"},
				{"Menu.Demo", "Demo"},
				{"Menu.Help", "Hilfe"},
				{"Menu.Exit", "Beenden"},

				{"ModeSwitcher.Editor", "Editor"},
				{"ModeSwitcher.Instructions", "Anleitungen"},

				{"SceneInfoLayer.Copy", "Kopieren"},
				{"SceneInfoLayer.Height", "Höhe:"},
				{"SceneInfoLayer.Length", "Länge:"},
				{"SceneInfoLayer.Width", "Breite:"},
				{"SceneInfoLayer.Weight", "Gewicht:"},
				{"SceneInfoLayer.TotalDetails", "Gesamtzahl der Details"},
				{"SceneInfoLayer.cm", "cm"},
				{"SceneInfoLayer.gram", "g"},

				{"SceneInfoLayer.Black", "Schwarz"},
				{"SceneInfoLayer.Blue", "Blau"},
				{"SceneInfoLayer.Brown", "Braun"},
				{"SceneInfoLayer.Gray", "Grau"},
				{"SceneInfoLayer.Green", "Grün"},
				{"SceneInfoLayer.Red", "Rot"},
				{"SceneInfoLayer.White", "Weiß"},
				{"SceneInfoLayer.Yellow", "Gelb"},

				{"SceneInfoLayer.BraceLineX2", "Dünne Klammer (2x1): "},
				{"SceneInfoLayer.BraceLineX3", "Dünne Klammer (3x1): "},
				{"SceneInfoLayer.BraceSquareX2", "Quadratische Klammer (2x1): "},
				{"SceneInfoLayer.BraceSquareX3", "Quadratische Klammer (3x1): "},
				{"SceneInfoLayer.Lego1x1", "Lego 1x1: "},
				{"SceneInfoLayer.Lego2x1", "Lego 2x1: "},
				{"SceneInfoLayer.Roll", "Rolle: "},
				{"SceneInfoLayer.RailX2", "Schiene: "},

				{"SceneInfoLayer.Axle1.0", "Achse 1.0: "},
				{"SceneInfoLayer.Axle2.0", "Achse 2.0: "},
				{"SceneInfoLayer.Axle2.5", "Achse 2.5: "},
				{"SceneInfoLayer.Axle3.0", "Achse 3.0: "},
				{"SceneInfoLayer.Axle3.5", "Achse 3.5: "},
				{"SceneInfoLayer.Axle8.0", "Achse 8.0: "},

                {"SceneInfoLayer.AxleBevelGear", "Kegelradgetriebe: "},
                {"SceneInfoLayer.AxleCoupling", "Kupplung: "},
                {"SceneInfoLayer.AxleHub3x1", "Nabe (3x1): "},
                {"SceneInfoLayer.AxleHub5x1", "Nabe (5x1): "},
                {"SceneInfoLayer.AxleTip", "Spitze: "},
                {"SceneInfoLayer.AxleWasher", "Scheibe: "},
                {"SceneInfoLayer.AxleWasherToothed", "Zahnscheibe: "},
                {"SceneInfoLayer.AxleWheelBig", "Großes Rad: "},
                {"SceneInfoLayer.AxleWheelSmall", "Kleines Rad: "},

				{"FileSelectionDialogLayer.SelectFile", "Datei auswählen..."},
				{"FileSelectionDialogLayer.EnterName", "Namen eingeben..."},
				{"FileSelectionDialogLayer.Up", "Nach oben "},
				{"FileSelectionDialogLayer.Downloads", "Herunterladen"},
				{"FileSelectionDialogLayer.Save", "Speichern"},
				{"FileSelectionDialogLayer.Cancel", "Abbrechen"},

				{"TutorialLayer.Next", "Nächster"},
				{"TutorialLayer.Welcome", "Willkommen bei\nFanclastic Kids!\n\nIn diesem Programm können Sie Modelle aus dem Fanclastic-Baukasten erstellen, Bauanleitungen ansehen und eigene erstellen!"},
				{"TutorialLayer.DetailsHint", "Um der Szene ein neues Detail hinzuzufügen, ziehen Sie das entsprechende Symbol aus der Detail-Palette in die Szene"},
				{"TutorialLayer.SetFocusHint", "Um den Fangpunkt der Kamera zu ändern, klicken Sie auf die Schaltfläche für die Fokuseinstellung und dann auf jeden Punkt oder jedes Detail auf der Bühne.."},
				{"TutorialLayer.ChangeColorHint", "Verwenden Sie die Farbauswahltaste, um die Farbe für die markierten Elemente festzulegen."},
				{"TutorialLayer.RotateHint", "Verwenden Sie die Drehschaltflächen, um die ausgewählten Details im Raum zu drehen. Jede Schaltfläche dreht die Details um die gezeichnete Achse der entsprechenden Farbe."},
				{"TutorialLayer.DeleteHint", "Verwenden Sie diese Schaltfläche, um ausgewählte Details aus der Szene zu löschen.."},
				{"TutorialLayer.ZoomHint.Base", "Verwenden Sie {0}, um die Kamera zu vergrößern oder zu verkleinern."},
				{"TutorialLayer.ZoomHint.Wheel", "Mausrad"},
				{"TutorialLayer.ZoomHint.Pinch", "Zwei-Finger-Geste"},
				{"TutorialLayer.LinkedSelection", "Klicken Sie auf das Detail und halten Sie es gedrückt, um alle zugehörigen Details gleichzeitig auszuwählen."},
			},
			new Dictionary<string, string>
			{
				{"EditorLayer.ActiveColor", "Kolory"},
				{"EditorLayer.Details", "Elementy"},

				{"Menu.Menu", "Menu"},
				{"Menu.NewScene", "Nowy projekt"},
				{"Menu.Save", "Zapisz..."},
				{"Menu.Load", "Wczytaj..."},
				{"Menu.Export", "Eksportuj..."},
				{"Menu.SceneInfo", "Szczegóły projektu"},
				{"Menu.Demo", "Demo"},
				{"Menu.Help", "Pomoc"},
				{"Menu.Exit", "Wyjście"},

				{"ModeSwitcher.Editor", "Tryb edycji"},
				{"ModeSwitcher.Instructions", "Tryb instrukcji"},

				{"SceneInfoLayer.Copy", "Kopiuj"},
				{"SceneInfoLayer.Height", "Wysokość:"},
				{"SceneInfoLayer.Length", "Długość:"},
				{"SceneInfoLayer.Width", "Szerokość:"},
				{"SceneInfoLayer.Weight", "Waga:"},
				{"SceneInfoLayer.TotalDetails", "Wszystkich elementów:"},
				{"SceneInfoLayer.cm", "cm"},
				{"SceneInfoLayer.gram", "g"},

				{"SceneInfoLayer.Black", "Czarny"},
				{"SceneInfoLayer.Blue", "Niebieski"},
				{"SceneInfoLayer.Brown", "Brązowy"},
				{"SceneInfoLayer.Gray", "Szary"},
				{"SceneInfoLayer.Green", "Zielony"},
				{"SceneInfoLayer.Red", "Czerwony"},
				{"SceneInfoLayer.White", "Biały"},
				{"SceneInfoLayer.Yellow", "Żółty"},

				{"SceneInfoLayer.BraceLineX2", "Łacznik wewnętrzny (2x1): "},
				{"SceneInfoLayer.BraceLineX3", "Łącznik wewnętrzny (3x1): "},
				{"SceneInfoLayer.BraceSquareX2", "Łącznik zewnętrzny (2x1): "},
				{"SceneInfoLayer.BraceSquareX3", "Łącznik zewnętrzny (3x1): "},
				{"SceneInfoLayer.Lego1x1", "Lego adapter 1x1: "},
				{"SceneInfoLayer.Lego2x1", "Lego adapter 2x1: "},
				{"SceneInfoLayer.Roll", "Zawias: "},
				{"SceneInfoLayer.RailX2", "Szyna: "},

				{"SceneInfoLayer.Axle1.0", "Oś 1.0: "},
				{"SceneInfoLayer.Axle2.0", "Oś 2.0: "},
				{"SceneInfoLayer.Axle2.5", "Oś 2.5: "},
				{"SceneInfoLayer.Axle3.0", "Oś 3.0: "},
				{"SceneInfoLayer.Axle3.5", "Oś 3.5: "},
				{"SceneInfoLayer.Axle8.0", "Oś 8.0: "},

                {"SceneInfoLayer.AxleBevelGear", "Przekładnia stożkowa: "},
                {"SceneInfoLayer.AxleCoupling", "Sprzęganie: "},
                {"SceneInfoLayer.AxleHub3x1", "Piasta (3x1): "},
                {"SceneInfoLayer.AxleHub5x1", "Piasta (5x1): "},
                {"SceneInfoLayer.AxleTip", "Wskazówka: "},
                {"SceneInfoLayer.AxleWasher", "Podkładka: "},
                {"SceneInfoLayer.AxleWasherToothed", "Podkładka zębata: "},
                {"SceneInfoLayer.AxleWheelBig", "Wielkie koło: "},
                {"SceneInfoLayer.AxleWheelSmall", "Małe koło: "},

				{"FileSelectionDialogLayer.SelectFile", "Wybierz projekt..."},
				{"FileSelectionDialogLayer.EnterName", "Nazwij projekt..."},
				{"FileSelectionDialogLayer.Up", "Do góry"},
				{"FileSelectionDialogLayer.Downloads", "Pobrane"},
				{"FileSelectionDialogLayer.Save", "Zapisz"},
				{"FileSelectionDialogLayer.Cancel", "Anuluj"},

				{"TutorialLayer.Next", "Dalej"},
				{"TutorialLayer.Welcome", "Witaj w\nFanclastic Kids™!\n\nTen program umożliwi Ci projektowanie własnych budowli Fanclastic i generowanie do nich animowanych instrukcji oraz przeglądanie już istniejących projektów. \n\nwww.fanclastic.pl"},
				{"TutorialLayer.DetailsHint", "Aby dodać element do obecnego projektu, przeciągnij odpowiednią ikonę z panelu elementów na wybrane miejsce na planie."},
				{"TutorialLayer.SetFocusHint", "Aby zmienić punkt widzenia kamery, kliknij przycisk ustawienia ostrości, a następnie na dowolny element lub punkt na planie."},
				{"TutorialLayer.ChangeColorHint", "Użyj przycisku wyboru koloru, aby zmienić barwę dla użytych elementów."},
				{"TutorialLayer.RotateHint", "Aby obrócić wybrany element, użyj przycisków obrotu, każdy z nich obraca elementy wokół danej osi odpowiedniego koloru."},
				{"TutorialLayer.DeleteHint", "Użyj tego przycisku, aby usunąć wybrane elementy z planu."},
				{"TutorialLayer.ZoomHint.Base", "Użyj  {0} aby powiększyć i pomniejszyć zbliżenie kamery."},
				{"TutorialLayer.ZoomHint.Wheel", "kółka w myszce"},
				{"TutorialLayer.ZoomHint.Pinch", "uszczypnięcia"},
				{"TutorialLayer.LinkedSelection", "Kliknij i przytrzymaj element, aby zobaczyć wszystkie połączone części i obrócić budowlę przyciskami obrotu."},
			},
			new Dictionary<string, string>
			{
				{"EditorLayer.ActiveColor", "גוון פעיל"},
				{"EditorLayer.Details", "רכיבים"},

				{"Menu.Menu", "תפריט"},
				{"Menu.NewScene", "פעולה חדשה"},
				{"Menu.Save", "שמירה..."},
				{"Menu.Load", "פתיחת קובץ..."},
				{"Menu.Export", "ייצוא..."},
				{"Menu.SceneInfo", "מידת הזירה"},
				{"Menu.Demo", "מודלים לדוגמה"},
				{"Menu.Help", "עזרה"},
				{"Menu.Exit", "יציאה"},

                {"ModeSwitcher.Editor", "עריכה"},
				{"ModeSwitcher.Instructions", "הוראות"},

				{"SceneInfoLayer.Copy", "העתק"},
				{"SceneInfoLayer.Height", "גובה"},
				{"SceneInfoLayer.Length", "אורך"},
				{"SceneInfoLayer.Width", "עובי"},
				{"SceneInfoLayer.Weight", "משקל"},
				{"SceneInfoLayer.TotalDetails", "סה\"כ רכיבים"},
				{"SceneInfoLayer.cm", "ס\"מ"},
				{"SceneInfoLayer.gram", "גרם"},

				{"SceneInfoLayer.Black", "שחור"},
				{"SceneInfoLayer.Blue", "כחול"},
				{"SceneInfoLayer.Brown", "חום"},
				{"SceneInfoLayer.Gray", "אפור"},
				{"SceneInfoLayer.Green", "ירוק"},
				{"SceneInfoLayer.Red", "אדום"},
				{"SceneInfoLayer.White", "לבן"},
				{"SceneInfoLayer.Yellow", "צהוב"},

				{"SceneInfoLayer.BraceLineX2","סוגר מסולסל" + " (2x1) "},
				{"SceneInfoLayer.BraceLineX3", "סוגר מסולסל" + " (3x1) "},
				{"SceneInfoLayer.BraceSquareX2", "סוגר מלבני" + " (2x1) "},
				{"SceneInfoLayer.BraceSquareX3", "סוגר מלבני" + " (3x1) "},
				{"SceneInfoLayer.Lego1x1", "לגו" + " 1x1 "},
				{"SceneInfoLayer.Lego2x1", "לגו" + " 2x1 "},
				{"SceneInfoLayer.Roll", "רול "},
				{"SceneInfoLayer.RailX2", "מסילה "},

				{"SceneInfoLayer.Axle1.0", "סרן" + " 1.0 "},
				{"SceneInfoLayer.Axle2.0", "סרן" + " 2.0 "},
				{"SceneInfoLayer.Axle2.5", "סרן" + " 2.5 "},
				{"SceneInfoLayer.Axle3.0", "סרן" + " 3.0 "},
				{"SceneInfoLayer.Axle3.5", "סרן" + " 3.5 "},
				{"SceneInfoLayer.Axle8.0", "סרן" + " 8.0 "},

                {"SceneInfoLayer.AxleBevelGear", "ציוד שפוע "},
                {"SceneInfoLayer.AxleCoupling", "מצמד "},
                {"SceneInfoLayer.AxleHub3x1", "רכזת" + " (3x1) "},
                {"SceneInfoLayer.AxleHub5x1", "רכזת" + " (5x1) "},
                {"SceneInfoLayer.AxleTip", "חוֹד "},
                {"SceneInfoLayer.AxleWasher", "דיסקוס "},
                {"SceneInfoLayer.AxleWasherToothed", "דיסקוס מחורץ "},
                {"SceneInfoLayer.AxleWheelBig", "גלגל גדול "},
                {"SceneInfoLayer.AxleWheelSmall", "גלגל קטן "},

				{"FileSelectionDialogLayer.SelectFile", "בחר קובץ..."},
				{"FileSelectionDialogLayer.EnterName", "הזן שם..."},
				{"FileSelectionDialogLayer.Up", "למעלה"},
				{"FileSelectionDialogLayer.Downloads", "הורדות"},
				{"FileSelectionDialogLayer.Save", "שמירה"},
				{"FileSelectionDialogLayer.Cancel", "ביטול"},

				{"TutorialLayer.Next", "הבא"},
				{"TutorialLayer.Welcome", "ברוכים הבאים ל\nFanclastic Kids!\n\nתוכנית זו מסייעת לך לבנות מודלים ,להציג הוראות הרכבה וליצור מודלים משלך."},
				{"TutorialLayer.DetailsHint", "כדי לזירה, גרור את הסמל המתאים מהלוח רכיבים לזירה."},
				{"TutorialLayer.SetFocusHint", "לשינוי נקודת עיגון המצלמה לחצו על לחצן הגדרת המוקד ולאחר מכן על כל רכיב או מקום בזירה."},
				{"TutorialLayer.ChangeColorHint", "השתמש בלחצן בחירת צבע על מנת להגדיר צבע עבור הרכיבים שנבחרו."},
				{"TutorialLayer.RotateHint", "כדי לסובב את הרכיבים הנבחרים, השתמש בלחצני הסיבוב. כל לחצן הצבוע בצבע אחר מסובב את הרכיב בכיוון שונה."},
				{"TutorialLayer.DeleteHint", "השתמש בלחצן זה כדי למחוק את הרכיבים הנבחרים מהזירה."},
			
                {"TutorialLayer.ZoomHint.Base", "כדי לקרב ולהרחיק את המצלמה השתמש" + " {0}."},
                {"TutorialLayer.ZoomHint.Wheel", "השתמש בגלגל של עכבר"},
				{"TutorialLayer.ZoomHint.Pinch", "באצבעות"},
				{"TutorialLayer.LinkedSelection", "הלחץ על רכיב והחזק כדי לבחור את כל החלקים הקשורים אליו."},
			}
		};

        public static int Lang
        {
            get
            {
                if (_lang != -1)
                {
                    return _lang;
                }

                if (!PlayerPrefs.HasKey("Language"))
                {
                    int defaultLang;

                    switch (Application.systemLanguage)
                    {
                        case SystemLanguage.Russian:
                            defaultLang = 1;
                            break;
                        case SystemLanguage.German:
                            defaultLang = 2;
                            break;
                        case SystemLanguage.Polish:
                            defaultLang = 3;
                            break;
                        case SystemLanguage.Hebrew:
                            defaultLang = 4;
                            break;
                        default:
                            defaultLang = 0;
                            break;
                    }

                    PlayerPrefs.SetInt("Language", defaultLang);
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
        private bool _updateOnEnable; // чтобы RTL тексты отображались правильно, 
        // их нужно передавать только в активные текстовые компоненты

        public void Awake()
        {
            AppController.Instance.LanguageChanged += SetLanguage;
        }

        public void Start()
        {
            SetLanguage(Lang);
        }

        protected void OnEnable()
        {
            if (!_updateOnEnable)
            {
                return;
            }

            SetLanguage(Lang);
            _updateOnEnable = false;
        }

        public void SetLanguage(int index)
        {
            if (_text == null)
            {

                _text = GetComponent<Text>();

                if (_text == null)
                {
                    Debug.LogError("Text component not found: " + gameObject.name);
                    return;
                }
            }

            if (!isActiveAndEnabled)
            {
                _updateOnEnable = true;
                return;
            }

            string newText;

            if (!Dictionaries[index].TryGetValue(TextKey, out newText))
            {
                Debug.LogError("Key " + TextKey + " not found in dictionary " + index);
                return;
            }

            _text.TextRespectingRtl(newText);
        }
    }
}
