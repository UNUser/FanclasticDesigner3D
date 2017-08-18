using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Assets.Scripts {

    public class AddDetailPanel : MonoBehaviour
    {
        public GameObject ColorSetterPrefab;
        public Transform ColorsContent;

        public GameObject DetailButtonPrefab;
        public Transform DetailsContent;

        public DetailColor ActiveColor { get; private set; }



//        public static Material[] Materials;
        public static GameObject[] Details;

        private static readonly Dictionary<string, DetailColor> _name2Color = new Dictionary<string, DetailColor>();
        private static readonly Dictionary<string, GameObject> _name2Detail = new Dictionary<string, GameObject>();

        private void Awake()
        {
            var colors = AppController.Instance.Resources.Colors;

            for (var i = 0; i < colors.Length; i++)
            {
                var colorToggle = Instantiate(ColorSetterPrefab).GetComponent<Toggle>();
                var index = i;

                colorToggle.targetGraphic.color = colors[i].UiColor;
                colorToggle.isOn = i == 0;
                colorToggle.onValueChanged.AddListener(value => { if (value) ActiveColor = colors[index]; });
                colorToggle.group = ColorsContent.GetComponent<ToggleGroup>();
                colorToggle.transform.SetParent(ColorsContent, false);

                _name2Color.Add(colors[i].Material.name, colors[i]);
            }
            ActiveColor = colors[0];

            Details = Resources.LoadAll<GameObject>("Prefabs/Details");

            foreach (var detail in Details)
            {
                var detailButton = Instantiate(DetailButtonPrefab).GetComponent<DetailButton>();

                detailButton.GetComponentInChildren<Text>().text = detail.name;
                detailButton.SetDetail(detail);
                detailButton.transform.SetParent(DetailsContent, false);

                _name2Detail.Add(detail.name, detail);
            }
        }

        public static DetailColor GetColor(string name) {
            DetailColor color;

            if (!_name2Color.TryGetValue(name, out color))
            {
                Debug.LogError("No such color: " + name);
                return null;
            }

            return color;
        }

        public static Detail GetDetail(string name) {
            GameObject detail;

			if (!_name2Detail.TryGetValue(name, out detail) && !_name2Detail.TryGetValue(name[2] + "x" + name[0], out detail)) {
                Debug.LogError("No such detail: " + name);
                return null;
            }

            return Instantiate(detail).GetComponent<Detail>();
        }
    }
}
