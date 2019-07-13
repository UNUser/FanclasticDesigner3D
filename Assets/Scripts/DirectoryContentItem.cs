
using UnityEngine;
using UnityEngine.UI;
using Path = System.IO.Path;

namespace Assets.Scripts 
{
	public class DirectoryContentItem : MonoBehaviour
	{
		public Image Icon;
		public Text Name;

		public Sprite DirectoryIcon;
		public Sprite FileIcon;

		public string ItemPath { get; private set; }
		public string ItemRawName { get; private set; }
		public bool IsDirectory { get; private set; }

//		public event Action<DirectoryContentItem> ItemSelectedEvent;
//		public event Action<DirectoryContentItem> ItemRemovedEvent;

		public void Set (string itemFullPath, bool isDirectory = false)
		{
			Icon.sprite = isDirectory ? DirectoryIcon : FileIcon;
			ItemPath = itemFullPath;
			IsDirectory = isDirectory;

			ItemRawName = Path.GetFileName(itemFullPath);
			Name.text = ItemRawName;
		}

//		public void OnItemButtonClicked()
//		{
//			ItemSelectedEvent.SafeInvoke(this);
//		}
//
//		public void OnRemoveItemButtonClicked()
//		{
//			ItemRemovedEvent.SafeInvoke(this);
//		}
	}
}
