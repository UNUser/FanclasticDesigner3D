using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Application = UnityEngine.Application;


namespace Assets.Scripts {
    public class FileSelectionDialogLayer : MonoBehaviour
    {
	    public Text Path;
        public InputField InputField;
        public GameObject SaveButton;
	    public GameObject UpButton;
	    public GameObject DownloadsButton;
        public ScrollRect ScrollRect;


	    private void Awake()
	    {
		    DownloadsButton.SetActive(Application.platform == RuntimePlatform.Android);
	    }


	    public void OnDownloadsButtonClick()
	    {
		    var plugin = new AndroidJavaClass("ru.fanclastic.androidplugin.AndroidPlugin");
			var downloadsPath = plugin.CallStatic<string>("GetDownloadsPath");

		    CurrentPath = downloadsPath;
	    }

	    public string CurrentPath
	    {
		    get {
			    return (_currentPath ?? (CurrentPath = Application.persistentDataPath));
		    }
			private set {
				if (value == null || value == _currentPath) {
					return;
				}

				var prevPath = _currentPath;
				var parent = Directory.GetParent(value);

				_currentPath = value.Replace('\\', '/');
				_currentPathParent = parent != null ? parent.FullName : null;
				try {
					UpdateFilesList();
				}
				catch (Exception) {
					Debug.LogError("Access denied: " + _currentPath);
					CurrentPath = prevPath;
				}
				UpButton.gameObject.SetActive(_currentPathParent != null);
			}
	    }

	    private string _currentPath;
	    private string _currentPathParent;
        private bool _isSaveFileDialog;
        private Action<string> _fileSelectedAction;

	    public void ShowFileSelectionDialog(Action<string> fileSelectedAction, bool isSaveFileDialog)
	    {
		    _isSaveFileDialog = isSaveFileDialog;
		    _fileSelectedAction = fileSelectedAction;

		    InputField.gameObject.SetActive(_isSaveFileDialog);
		    SaveButton.SetActive(_isSaveFileDialog);

		    if (_isSaveFileDialog)
		    {
			    InputField.text = string.Empty;
		    }

		    if (CurrentPath != Application.persistentDataPath) {
			    CurrentPath = Application.persistentDataPath;
		    } else {
			    UpdateFilesList();
		    }

		    gameObject.SetActive(true);
        }

        private void UpdateFilesList(bool resetScrollPosition = true)
        {
            var files = Directory.GetFiles(CurrentPath);
	        var directories = Directory.GetDirectories(CurrentPath);
            var itemPrefab = ScrollRect.content.GetChild(0).gameObject;

            var createdItems = ScrollRect.content.childCount - 1;
            var neededItems = files.Length + directories.Length;
            var loopEnd = Math.Max(createdItems, neededItems);

			Path.text = CurrentPath;

            for (var i = 0; i < loopEnd; i++) {
                DirectoryContentItem newItem;

                if (i < createdItems) {
                    newItem = ScrollRect.content.GetChild(i + 1).GetComponent<DirectoryContentItem>();
                } else {
                    newItem = Instantiate(itemPrefab).GetComponent<DirectoryContentItem>();
                    newItem.transform.SetParent(ScrollRect.content, false);
                }

                if (i < neededItems) {
	                var isDirectory = i < directories.Length;
					var source = isDirectory ? directories : files;
					var sourceIndex = isDirectory ? i : i - directories.Length;
					var itemName = source[sourceIndex].Remove(0, CurrentPath.Length).TrimStart('\\', '/');

					newItem.Set(itemName, isDirectory);
                    newItem.gameObject.SetActive(true);
                } else {
                    newItem.gameObject.SetActive(false);
                }
            }

	        if (resetScrollPosition) {
				ScrollRect.verticalNormalizedPosition = 1;
	        }
        }

        public void OnSaveButtonClicked()
        {
	        var fileName = InputField.text;

	        if (!IsValidFileName(fileName)) {
				Debug.LogError("Invalid file name: " + fileName);
				return;
	        }

            gameObject.SetActive(false);
            DoFileSelectedAction(fileName);
        }

        public void OnCancelButtonClicked() {
            gameObject.SetActive(false);
        }

	    public void OnUpButtonClicked()
	    {
			CurrentPath = _currentPathParent;
	    }

        public void OnItemButtonClicked(DirectoryContentItem item)
        {
	        var itemName = item.Name.text;

	        if (item.IsDirectory) {
		        CurrentPath = GetFullPath(itemName);
				return;
	        }

            if (_isSaveFileDialog) {
                InputField.text = itemName;
                return;
            }

            gameObject.SetActive(false);

            DoFileSelectedAction(itemName);
        }

		public void OnRemoveItemButtonClick(DirectoryContentItem item)
		{
			var fullPath = GetFullPath(item.Name.text);

			try {
				if (item.IsDirectory) {
					Directory.Delete(fullPath, true);
				} else {
					File.Delete(fullPath);
				}
			}
			catch (Exception)
			{
				Debug.LogError("Access denied: " + fullPath);
				return;
			}

			UpdateFilesList(false);
        }

	    private bool IsValidFileName(string fileName)
	    {
		    return fileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0;
	    }

        private void DoFileSelectedAction(string fileName)
        {
	        var path = GetFullPath(fileName);
//			var directory = File.e
//
//	        if (!Directory.Exists(path)) {
//		        Debug.LogError("Path doesn't exist: " + path);
//				return;
//	        }

			_fileSelectedAction.SafeInvoke(path);
        }

        private string GetFullPath(string itemName)
        {
            return string.Format("{0}/{1}", CurrentPath, itemName);
        }
    }
}
