using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts {
    public class FileSelectionDialogLayer : MonoBehaviour
    {
        public InputField InputField;
        public GameObject SaveButton;
        public ScrollRect ScrollRect;

        private bool _isSaveFileDialog;
        private Action<string> _fileSelectedAction;

        public void ShowFileSelectionDialog(Action<string> fileSelectedAction, bool isSaveFileDialog) {
            _isSaveFileDialog = isSaveFileDialog;
            _fileSelectedAction = fileSelectedAction;

            InputField.gameObject.SetActive(_isSaveFileDialog);
            SaveButton.SetActive(_isSaveFileDialog);

            if (_isSaveFileDialog) {
                InputField.text = string.Empty;
            }

            UpdateFilesList();
            ScrollRect.verticalNormalizedPosition = 0;

            gameObject.SetActive(true);
        }

        private void UpdateFilesList()
        {
            var path = Application.persistentDataPath;
            var files = Directory.GetFiles(Application.persistentDataPath);
            var listItem = ScrollRect.content.GetChild(0).gameObject;

            var createdItems = ScrollRect.content.childCount - 1;
            var neededItems = files.Length;
            var loopEnd = Math.Max(createdItems, neededItems);

            for (var i = 0; i < loopEnd; i++) {
                GameObject newItem;

                if (i < createdItems) {
                    newItem = ScrollRect.content.GetChild(i + 1).gameObject;
                } else {
                    newItem = Instantiate(listItem);
                    newItem.transform.SetParent(ScrollRect.content, false);
                }


                if (i < neededItems) {
                    newItem.GetComponentInChildren<Text>().text = files[i].Remove(0, path.Length + 1);
                    newItem.SetActive(true);
                } else {
                    newItem.SetActive(false);
                }
            }
        }

        public void OnSaveButtonClicked() {
            gameObject.SetActive(false);

            DoFileSelectedAction(InputField.text);
        }

        public void OnCancelButtonClicked() {
            gameObject.SetActive(false);
        }

        public void OnFileNameButtonClicked(Text fileName) {

            if (_isSaveFileDialog)
            {
                InputField.text = fileName.text;
                return;
            }

            gameObject.SetActive(false);

            DoFileSelectedAction(fileName.text);
        }

        public void OnRemoveFileButtonClick(Text fileName)
        {
            File.Delete(GetFileFullName(fileName.text));
            UpdateFilesList();
        }

        private void DoFileSelectedAction(string fileName)
        {
            if (_fileSelectedAction != null) {
                _fileSelectedAction(GetFileFullName(fileName));
            }
        }

        private string GetFileFullName(string fileName)
        {
            return string.Format("{0}/{1}", Application.persistentDataPath, fileName);
        }
    }
}
