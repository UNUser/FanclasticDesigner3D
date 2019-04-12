
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Path = System.IO.Path;

namespace Assets.Scripts 
{
	public class DirectoryContentItem : MonoBehaviour
	{
		public Image Icon;
		public Text Name;
		public GameObject DeleteButton;

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

			var isDemoModelsPath = itemFullPath.StartsWith(AppController.DemoModelsPath);

			ItemRawName = Path.GetFileName(itemFullPath);
			DeleteButton.SetActive(!isDemoModelsPath);

			if (isDemoModelsPath)
			{
				var vendorCodePrefixMatch = Regex.Match(ItemRawName, "^F1[0-9]{3}_");
				var vendorCodePrefix = vendorCodePrefixMatch.Success ? vendorCodePrefixMatch.Value : string.Empty;
				var vendorCode = vendorCodePrefix.Replace('_', ' ');
				var itemKeyName = isDirectory 
					? ItemRawName.Remove(0, vendorCodePrefix.Length)
					: Path.GetFileNameWithoutExtension(itemFullPath);
				var key = string.Format("DemoModelsLayer.{0}Names.{1}", isDirectory ? "Sets" : "Models", itemKeyName);
				
				var value = key.Lang(); 

				if (value != null)  {
					Name.text = isDirectory ? vendorCode + value : value;
				} else {
					gameObject.SetActive(false);
				}

				return; 
			}

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
