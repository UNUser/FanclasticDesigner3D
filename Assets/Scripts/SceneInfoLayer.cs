
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts {
	public class SceneInfoLayer : MonoBehaviour
	{
		public GameObject CopyButton;
		public GridLayoutGroup Grid;
		public GameObject CellPrefab;

		public Text HeightValue;
		public Text LengthValue;
		public Text WidthValue;

		private const float UnitPhysicalSize = 0.8f; //cm

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
			var colors = AppController.Instance.Resources.Colors;
			var types = AddDetailPanel.Details.Select(o => o.name).ToArray();
			var counts = new Dictionary<Color, Dictionary<string, int>>();

			var details = GetAll();
			var bounds = new Bounds(details.Any() ? details[0].Bounds.center : Vector3.zero, Vector3.zero);

			Clear();

			foreach (var material in colors) {
				var color = material.Material.color;

				counts.Add(color, new Dictionary<string, int>());

				foreach (var type in types) {
					counts[color].Add(type, 0);
				}
			}

			foreach (var detail in details)
			{
				var name = detail.gameObject.name;
				var type = name.Remove(name.Length - "(Clone)".Length);

				counts[detail.Color.Material.color][type] += 1;
				bounds.Encapsulate(detail.Bounds);
			}

			Grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			Grid.constraintCount = types.Length + 1;

			AddCell(string.Empty);

			foreach (var type in types)
			{
				AddCell(type);
			}

			foreach (var material in colors)
			{
				var materialName = material.Material.name;
				AddCell(materialName, TextAnchor.MiddleRight);

				foreach (var type in types)
				{
					AddCell(counts[material.Material.color][type].ToString());
				}
			}

			var roundedSize = (SerializableVector3Int) bounds.size;

			HeightValue.text = string.Format("{0} cm", roundedSize.y * UnitPhysicalSize);
			LengthValue.text = string.Format("{0} cm", Mathf.Max(roundedSize.x, roundedSize.z) * UnitPhysicalSize);
			WidthValue.text = string.Format("{0} cm", Mathf.Min(roundedSize.x, roundedSize.z) * UnitPhysicalSize);

			CopyButton.SetActive(!Application.isMobilePlatform);
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
