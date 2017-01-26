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

        public Material ActiveColor { get; private set; }

        private static Material[] _materials;
        private static GameObject[] _details;

        private static readonly Dictionary<string, Material> _name2Material = new Dictionary<string, Material>();
        private static readonly Dictionary<string, GameObject> _name2Detail = new Dictionary<string, GameObject>();

        private void Start()
        {
            _materials = Resources.LoadAll<Material>("Materials");

            for (var i = 0; i < _materials.Length; i++)
            {
                var colorToggle = Instantiate(ColorSetterPrefab).GetComponent<Toggle>();
                var index = i;

                colorToggle.targetGraphic.color = _materials[i].color;
                colorToggle.isOn = i == 0;
                colorToggle.onValueChanged.AddListener(value => { if (value) ActiveColor = _materials[index]; });
                colorToggle.group = ColorsContent.GetComponent<ToggleGroup>();
                colorToggle.transform.SetParent(ColorsContent, false);

                _name2Material.Add(_materials[i].name, _materials[i]);
            }
            ActiveColor = _materials[0];

            _details = Resources.LoadAll<GameObject>("Prefabs/Details");

            foreach (var detail in _details)
            {
                var detailButton = Instantiate(DetailButtonPrefab).GetComponent<DetailButton>();

                detailButton.GetComponentInChildren<Text>().text = detail.name;
                detailButton.SetDetail(detail);
                detailButton.transform.SetParent(DetailsContent, false);

                _name2Detail.Add(detail.name, detail);
            }
        }

        public static Material GetMaterial(string name) {
            Material material;

            if (!_name2Material.TryGetValue(name, out material))
            {
                Debug.LogError("No such material: " + name);
                return null;
            }

            return material;
        }

        public static Detail GetDetail(string name) {
            GameObject detail;

            if (!_name2Detail.TryGetValue(name, out detail)) {
                Debug.LogError("No such detail: " + name);
                return null;
            }

            return Instantiate(detail).GetComponent<Detail>();
        }
    }
}
