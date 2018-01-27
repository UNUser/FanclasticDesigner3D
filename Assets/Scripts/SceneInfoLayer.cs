
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

		public Transform OtherDetailsCount;
		public GameObject LinePrefab;

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

			str.Append("\n");
			isPrefab = true;

			foreach (Transform child in OtherDetailsCount)
			{
				if (isPrefab) {
					isPrefab = false;
					continue;
				}

				var lineText = child.gameObject.GetComponent<Text>().text;

				str.Append(lineText);
				str.Append("\n");
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
			LinePrefab.SetActive(false);
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
			var types = AddDetailPanel.Details.Where(obj => char.IsDigit(obj.name[0])).Select(o => o.name).ToArray();
			var counts = new Dictionary<Color, Dictionary<string, int>>();

			var details = GetAll();
			var bounds = new Bounds(details.Any() ? details[0].Bounds.center : Vector3.zero, Vector3.zero);

			var otherTypes = AddDetailPanel.Details.Where(obj => !char.IsDigit(obj.name[0])).Select(o => o.name).ToArray();
			var otherCounts = otherTypes.ToDictionary(s => s, s => 0);

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

				if (char.IsDigit(name[0])) {
					counts[detail.Color.Material.color][type] += 1;
				} else {
					otherCounts[type] += 1;
				}
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
				AddCell(("SceneInfoLayer." + materialName).Lang(), TextAnchor.MiddleRight);

				foreach (var type in types)
				{
					AddCell(counts[material.Material.color][type].ToString());
				}
			}

			foreach (var type in otherTypes)
			{
				var line = Instantiate(LinePrefab).GetComponent<Text>();

				line.transform.SetParent(OtherDetailsCount.transform, false);

				line.text = string.Format("{0}: {1}", ("SceneInfoLayer." + type).Lang(), otherCounts[type]);

				line.gameObject.SetActive(true);
			}

			var roundedSize = (SerializableVector3Int) bounds.size;

			HeightValue.text = string.Format("{0} ", roundedSize.y * UnitPhysicalSize) + "SceneInfoLayer.cm".Lang();
			LengthValue.text = string.Format("{0} ", Mathf.Max(roundedSize.x, roundedSize.z) * UnitPhysicalSize) + "SceneInfoLayer.cm".Lang();
			WidthValue.text = string.Format("{0} ", Mathf.Min(roundedSize.x, roundedSize.z) * UnitPhysicalSize) + "SceneInfoLayer.cm".Lang();

			CopyButton.SetActive(!Application.isMobilePlatform);
		}

		private void Clear()
		{
			for (var childIndex = Grid.transform.childCount - 1; childIndex > 0; childIndex--) {
				var child = Grid.transform.GetChild(childIndex);

				child.SetParent(null);
				Destroy(child.gameObject);
			}

			for (var childIndex = OtherDetailsCount.transform.childCount - 1; childIndex > 0; childIndex--) {
				var child = OtherDetailsCount.transform.GetChild(childIndex);

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
