﻿using System.Collections.Generic;
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



        public static Material[] Materials;
        public static GameObject[] Details;

        private static readonly Dictionary<string, Material> _name2Material = new Dictionary<string, Material>();
        private static readonly Dictionary<string, GameObject> _name2Detail = new Dictionary<string, GameObject>();

        private void Awake()
        {
            Materials = Resources.LoadAll<Material>("Materials");

            for (var i = 0; i < Materials.Length; i++)
            {
                var colorToggle = Instantiate(ColorSetterPrefab).GetComponent<Toggle>();
                var index = i;

                colorToggle.targetGraphic.color = Materials[i].color;
                colorToggle.isOn = i == 0;
                colorToggle.onValueChanged.AddListener(value => { if (value) ActiveColor = Materials[index]; });
                colorToggle.group = ColorsContent.GetComponent<ToggleGroup>();
                colorToggle.transform.SetParent(ColorsContent, false);

                _name2Material.Add(Materials[i].name, Materials[i]);
            }
            ActiveColor = Materials[0];

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

			if (!_name2Detail.TryGetValue(name, out detail) && !_name2Detail.TryGetValue(name[2] + "x" + name[0], out detail)) {
                Debug.LogError("No such detail: " + name);
                return null;
            }

            return Instantiate(detail).GetComponent<Detail>();
        }
    }
}
