using UnityEngine;
using UnityEngine.UI;


namespace Assets.Scripts {

    class AddDetailPanel : MonoBehaviour
    {
        public GameObject ColorSetterPrefab;
        public Transform ColorsContent;

        public GameObject DetailButtonPrefab;
        public Transform DetailsContent;

        public Material ActiveColor { get; private set; }
        private Material[] _materials;

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
            }
            ActiveColor = _materials[0];

            var details = Resources.LoadAll<GameObject>("Prefabs/Details");

            foreach (var detail in details)
            {
                var detailButton = Instantiate(DetailButtonPrefab).GetComponent<DetailButton>();

                detailButton.GetComponentInChildren<Text>().text = detail.name;
                detailButton.SetDetail(detail);
                detailButton.transform.SetParent(DetailsContent, false);
            }
        }

    }
}
