using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts 
{
	public class SetsTreeItemController : MonoBehaviour
	{
		public Image Icon;
		public Text Text;

		public string Name { get; private set; }
		public bool IsCategory { get; private set; }

		public void Set(string itemName, string itemText, Sprite itemIcon, bool isCategory = true)
		{
			Icon.sprite = itemIcon;
			Text.text = itemText;
			Name = itemName;
			IsCategory = isCategory;

			Icon.gameObject.SetActive(itemIcon != null);
		}


	}
}
