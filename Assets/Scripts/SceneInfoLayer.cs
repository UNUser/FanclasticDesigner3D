﻿
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts {
	public class SceneInfoLayer : MonoBehaviour
	{
		public GridLayoutGroup Grid;
		public GameObject CellPrefab;

		public void OnCopyButtonClicked()
		{
			var str = new StringBuilder();
			var isPrefab = true;

			foreach (Transform child in Grid.transform)
			{
				if (isPrefab) {
					isPrefab = false;
					continue;
				}

				var cellText = child.gameObject.GetComponent<Text>().text;
				var isNewRow = child.GetSiblingIndex()%Grid.constraintCount > 0;

				str.Append(cellText);
				str.Append(isNewRow ? "\t" : "\n");
			}

			GUIUtility.systemCopyBuffer = str.ToString();
		}

		public void OnBackButtonClicked()
		{
			gameObject.SetActive(false);
		}

		private void Start()
		{
			CellPrefab.SetActive(false);
		}

		private void AddCell(string text, TextAnchor alignment = TextAnchor.MiddleCenter)
		{
			var cell = Instantiate(CellPrefab).GetComponent<Text>();

			cell.transform.SetParent(Grid.transform, false);

			cell.text = text;
			cell.alignment = alignment;

			cell.gameObject.SetActive(true);
		}

		private void OnEnable()
		{
			var materials = AddDetailPanel.Materials;
			var types = AddDetailPanel.Details.Select(o => o.name).ToArray();
			var counts = new Dictionary<Color, Dictionary<string, int>>();

			var details = GetAll();

			Clear();

			foreach (var material in materials) {
				var color = material.color;

				counts.Add(color, new Dictionary<string, int>());

				foreach (var type in types) {
					counts[color].Add(type, 0);
				}
			}

			foreach (var detail in details)
			{
				var name = detail.gameObject.name;
				var type = name.Remove(name.Length - "(Clone)".Length);

				counts[detail.Material.color][type] += 1;
			}

			Grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			Grid.constraintCount = types.Length + 1;

			AddCell(string.Empty);

			foreach (var type in types)
			{
				AddCell(type);
			}

			foreach (var material in materials)
			{
				var materialName = material.name;
				AddCell(materialName.Remove(materialName.Length - "Material".Length), TextAnchor.MiddleRight);

				foreach (var type in types)
				{
					AddCell(counts[material.color][type].ToString());
				}
			}
		}

		private void Clear()
		{
			for (var childIndex = Grid.transform.childCount - 1; childIndex > 0; childIndex--)
			{
				var child = Grid.transform.GetChild(childIndex);

				child.SetParent(null);
				Destroy(child.gameObject);
			}
		}

		private List<Detail> GetAll()
		{
			var roots = new List<GameObject>();
			var details = new List<Detail>();
			UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(roots);

			foreach (var root in roots)
			{
				if (!root.activeSelf) continue;

				var detail = root.GetComponent<Detail>();

				if (detail != null) {
					if (detail.gameObject.activeSelf)
						details.Add(detail);
					continue;
				}

				var group = root.GetComponent<DetailsGroup>();

				if (group != null) {
					details.AddRange(group.Details);
				}
			}

			return details;
		}

	}
}
