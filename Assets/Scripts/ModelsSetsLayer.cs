
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    /// <summary>
    /// Содержимое окна генерируется на основе файла ModelsSets/ModelsSetsTree.json в ресурсах приложения.
    /// Это файл, в свою очередь, генерируется перед билдом на основе содержимого директории DemoModels в корне проекта.
    /// Имена моделей (без порядкового окончания) / названий наборов (без артикула), их иконок и ключа для локализации должны совпадать.
    /// </summary>
    public abstract class SetsTreeItem
    {
        public string Name;

        public virtual string GetLocalizedText()
        {
            var key = _localizationKey ?? (_localizationKey = GetLocalizationKey());

            return key.Lang();
        }

        public void Print()
        {
            Debug.Log(ToTextView(0, new StringBuilder()));
        }

        public virtual StringBuilder ToTextView(int level, StringBuilder stringBuilder)
        {
            var indent = string.Empty.PadLeft(level * 2);

            return stringBuilder.AppendLine(indent + Name);
        }

        protected abstract string GetLocalizationKey();

        [NonSerialized]
        private string _localizationKey;
    }

    public class SetsTreeModel : SetsTreeItem
    {
        protected override string GetLocalizationKey()
        {
            var suffixMatch = Regex.Match(Name, "_[0-9]*$");
            var suffex = suffixMatch.Success ? suffixMatch.Value : string.Empty;
            var itemKeyName = Name.Remove(Name.Length - suffex.Length, suffex.Length);

            return string.Format("ModelsSetsLayer.ModelsNames.{0}", itemKeyName);
        }
    }

    public class SetsTreeCategory : SetsTreeItem
    {
        public List<SetsTreeItem> Content = new List<SetsTreeItem>();

        [NonSerialized]
        public float ScrollPosition = 1f;
        [JsonIgnore]
        public string VendorCodePrefix { get; private set; }

        public override StringBuilder ToTextView(int level, StringBuilder stringBuilder)
        {
            base.ToTextView(level, stringBuilder);

            foreach (var setsTreeItem in Content)
            {
                setsTreeItem.ToTextView(level + 1, stringBuilder);
            }

            return stringBuilder;
        }

        public string GetLocalizedTextWithoutVendor()
        {
            return base.GetLocalizedText();
        }

        public override string GetLocalizedText()
        {
            var localizedText = base.GetLocalizedText();

            return string.Format("{0}{1}", VendorCodePrefix.Replace('_', '\n'), localizedText);
        }

        protected override string GetLocalizationKey() //TODO вынести все эти штуки в конструктор с именем айтема
        {
            var vendorCodePrefixMatch = Regex.Match(Name, "^F1[0-9]{3}_");
            VendorCodePrefix = vendorCodePrefixMatch.Success ? vendorCodePrefixMatch.Value : string.Empty;
            var itemKeyName = Name.Remove(0, VendorCodePrefix.Length);

            return string.Format("ModelsSetsLayer.SetsNames.{0}", itemKeyName);
        }
    }

    public class ModelsSetsLayer : MonoBehaviour
    {
        public Text Path;
        public ScrollRect ScrollRect;
        public GameObject BackButton;

        private GameObject ItemPrefab { get { return ScrollRect.content.GetChild(0).gameObject; } }

        private Dictionary<string, Sprite> _itemNameToIcon;
        private SetsTreeCategory _setsTree;

        public readonly string ModelsFilesPath = "ModelsSets/ModelsFiles";

        private SetsTreeCategory CurrentCategory
        {
            get { return _currentCategoryPath[_currentCategoryPath.Count - 1]; }
        }
        private readonly List<SetsTreeCategory> _currentCategoryPath = new List<SetsTreeCategory>();

        protected void Awake()
        {
            ItemPrefab.SetActive(false);
            LoadData();
        }

        protected void OnEnable()
        {
            UpdateWindowContent();
        }

        private void LoadData()
        {
            const string setsTreeFilePath = "ModelsSets/ModelsSetsTree";
            const string iconsPath = "ModelsSets/Icons";

            var setsTreeText = Resources.Load<TextAsset>(setsTreeFilePath).text;
            var settings = new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.Auto
            };

            _setsTree = JsonConvert.DeserializeObject<SetsTreeCategory>(setsTreeText, settings);
            _itemNameToIcon = Resources.LoadAll<Sprite>(iconsPath).ToDictionary(sprite => sprite.name, sprite => sprite);
            _currentCategoryPath.Add(_setsTree);
        }

        private void UpdatePathString()
        {
            var pathString = new StringBuilder("/");

            for (var i = 1; i < _currentCategoryPath.Count; i++)
            {
                var setsTreeCategory = _currentCategoryPath[i];
                var categoryName = string.Format(" {0}{1} /", setsTreeCategory.VendorCodePrefix.Replace('_', ' '),
                                                              setsTreeCategory.GetLocalizedTextWithoutVendor());
                pathString.Append(categoryName);
            }

            Path.text = pathString.ToString();
        }

        private SetsTreeItemController GetItemByIndex(int index)
        {
            return ScrollRect.content.GetChild(index + 1).GetComponent<SetsTreeItemController>();
        }

        private void UpdateWindowContent(float scrollPosition = float.NaN)
        {
            var currentCategory = CurrentCategory;
            var items = currentCategory.Content;

            var createdItems = ScrollRect.content.childCount - 1;
            var neededItems = items.Count;
            var loopEnd = Math.Max(createdItems, neededItems);

            UpdatePathString();

            for (var i = 0; i < loopEnd; i++)
            {
                SetsTreeItemController newItem;

                if (i < createdItems) {
                    newItem = GetItemByIndex(i);
                } else {
                    newItem = Instantiate(ItemPrefab).GetComponent<SetsTreeItemController>();
                    newItem.transform.SetParent(ScrollRect.content, false);
                }

                var isExcessItem = i >= neededItems;

                newItem.gameObject.SetActive(!isExcessItem);

                if (isExcessItem) {
                    continue;
                }

                var setsTreeItem = items[i];
                var isCategory = setsTreeItem is SetsTreeCategory;
                var itemName = setsTreeItem.Name;
                var itemText = setsTreeItem.GetLocalizedText();
                Sprite itemIcon;

                _itemNameToIcon.TryGetValue(itemName, out itemIcon);

                newItem.Set(itemName, itemText, itemIcon, isCategory);
            }

            if (!float.IsNaN(scrollPosition))
            {
                Canvas.ForceUpdateCanvases();
                ScrollRect.verticalNormalizedPosition = scrollPosition;
            }

            BackButton.SetActive(currentCategory != _setsTree);
        }

        public void OnSetsTreeItemClicked(SetsTreeItemController item)
        {
            if (item.IsCategory)
            {
                var prevCategory = CurrentCategory;
                var nextCategory = prevCategory.Content.Find(treeItem => treeItem.Name == item.Name) as SetsTreeCategory;

                prevCategory.ScrollPosition = ScrollRect.verticalNormalizedPosition;
                _currentCategoryPath.Add(nextCategory);
                UpdateWindowContent(1f);

                return;
            }

            gameObject.SetActive(false);

            AppController.Instance.LoadModel(item.Name);
        }

        public void OnBackButtonClicked()
        {
            var currentCategoryIndex = _currentCategoryPath.Count - 1;

            _currentCategoryPath.RemoveAt(currentCategoryIndex);
            UpdateWindowContent(CurrentCategory.ScrollPosition);
        }

        public void OnCancelButtonClicked()
        {
            gameObject.SetActive(false);
        }
    }
}
