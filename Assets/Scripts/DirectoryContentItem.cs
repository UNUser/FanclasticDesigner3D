using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts 
{
	public class DirectoryContentItem : MonoBehaviour
	{
		public Image Icon;
		public Text Name;

		public Sprite DirectoryIcon;
		public Sprite FileIcon;

		public bool IsDirectory { get; private set; }

//		public event Action<DirectoryContentItem> ItemSelectedEvent;
//		public event Action<DirectoryContentItem> ItemRemovedEvent;

		public void Set (string itemName, bool isDirectory = false)
		{
			Icon.sprite = isDirectory ? DirectoryIcon : FileIcon;
			Name.text = itemName;
			IsDirectory = isDirectory;
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
