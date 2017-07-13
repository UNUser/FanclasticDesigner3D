using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Assets.Scripts {

	public class Session
	{
		public SceneData SceneData { get; private set; }

		private readonly Dictionary<int, Detail> _id2Detail = new Dictionary<int, Detail>();
		private List<InstructionBase> _sourceInstructions;
		private string _fileName;

		public Detail GetDetail(int id)
		{
			return _id2Detail[id];
		}

		public Session()
		{
			AppController.Instance.ActionsLog.Clear();
			RemoveAll();
			_sourceInstructions = new List<InstructionBase>();
			SceneData = null;
		}

		public Session(string fileName)
		{
			AppController.Instance.ActionsLog.Clear();
			Load(fileName);
		}

		public void Reset()
		{
			//TODO тут сделать сброс деталей в последнее сохраненное состояние вместо повторной загрузки файла
			AppController.Instance.ActionsLog.Clear();
			Load(_fileName);
		}

		public void Save(string fileName)  //TODO добавить заголовок, чтобы определять свои файлы
		{
			var roots = new List<GameObject>();
			
			_id2Detail.Clear();
			UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(roots);

			SceneData = new SceneData();
			


//
//			var instructions = new List<InstructionBase>
//	        {
//		        new InstructionAdd
//		        {
//			        TargetDetails = new List<int> {-11},
//			        Rotation = new SerializableVector3(1f, 1f, 1f),
//			        Position = new SerializableVector3(2f, 2f, 2f)
//		        },
//		        new InstructionAdd
//		        {
//			        TargetDetails = new List<int> {-111},
//			        Rotation = new SerializableVector3(11f, 11f, 11f),
//			        Position = new SerializableVector3(22f, 22f, 22f)
//		        },
//		        new InstructionMoveAndRotate
//		        {
//			        TargetDetails = new List<int> {1, 2, 3},
//			        Offset = new SerializableVector3(-1f, -1f, -1f)
//		        },
//				new InstructionRotate
//		        {
//			        TargetDetails = new List<int> {-1, -2, -3},
//			        Pivot = new SerializableVector3(0f, 0f, 0f)
//
//		        },
//	        };


			AcceptChanges();




			foreach (var root in roots) {
				var detailBase = root.GetComponent<DetailBase>();

				if (detailBase == null) {
					continue;
				}

				if (!detailBase.gameObject.activeSelf) {
					Object.Destroy(detailBase.gameObject);
					continue;
				}

				var detail = detailBase as Detail;

				if (detail != null) {
					SceneData.SingleDetails.Add(detail.Data);
					_id2Detail.Add(detail.GetInstanceID(), detail);
					continue;
				}

				var detailsGroup = (DetailsGroup) detailBase;
				var connectedGroup = new ConnectedGroup();

				foreach (var child in detailsGroup.Details) {
					connectedGroup.Details.Add(child.Data);
					_id2Detail.Add(child.GetInstanceID(), child);
				}
//				connectedGroup.Instructions = instructions;

				SceneData.ConnectedGroups.Add(connectedGroup);
			}


			var group2Instructions = SplitInstructions(_id2Detail);

			foreach (var connectedGroup in SceneData.ConnectedGroups)
			{
				var id = connectedGroup.Details.First().Id;
				var group = _id2Detail[id].Group;

				connectedGroup.Instructions = group2Instructions[group];
			}

			var settings = new JsonSerializerSettings {
				TypeNameHandling = TypeNameHandling.Auto
			};
			var json = JsonConvert.SerializeObject(SceneData, Formatting.Indented, settings);

			File.WriteAllText(fileName, json);
			_fileName = fileName;

			Debug.Log("Saved: " + fileName);
		}

		private void AcceptChanges()
		{
			HashSet<Detail> invalidDetails;
			var actionsLog = AppController.Instance.ActionsLog;
			var newInstructions = actionsLog.GetInstructions(out invalidDetails);
			var mergedInstructions = new List<InstructionBase>();
			var invalidIds = new HashSet<int>(invalidDetails.Select(detail => detail.GetInstanceID()));

			foreach (var instruction in _sourceInstructions) {
				instruction.TargetDetails.ExceptWith(invalidIds);

				if (instruction.TargetDetails.Any()) {
					mergedInstructions.Add(instruction);
				}
			}

			mergedInstructions.AddRange(newInstructions);
			_sourceInstructions = mergedInstructions;

			actionsLog.Clear();
		}

		private Dictionary<DetailsGroup, List<InstructionBase>> SplitInstructions(Dictionary<int, Detail> id2Detail)
		{
			var group2Instructions = new Dictionary<DetailsGroup, List<InstructionBase>>();

			foreach (var instruction in _sourceInstructions)
			{
				if (instruction.TargetDetails.Count == 1)
				{
					var id = instruction.TargetDetails.First();
					var group = id2Detail[id].Group;

					if (group == null) {
						continue;
					}

					if (!group2Instructions.ContainsKey(group)) {
						group2Instructions.Add(group, new List<InstructionBase>());
					}
					group2Instructions[group].Add(instruction);
					continue;
				}

				var group2TargetDetails = instruction.TargetDetails.ToLookup(id => id2Detail[id].Group);//new Dictionary<DetailsGroup, HashSet<int>>();

//				foreach (var id in instruction.TargetDetails)
//				{
//					var group = id2Detail[id].Group;
//
//					if (group == null) {
//						continue;
//					}
//
//					if (!group2TargetDetails.ContainsKey(group)) {
//						group2TargetDetails.Add(group, new HashSet<int>());
//					}
//					group2TargetDetails[group].Add(id);
//				}

				foreach (var targetDetail in group2TargetDetails)
				{
					var group = targetDetail.Key;
					var targetDetails = new HashSet<int>(group2TargetDetails[group]);

					if (group == null) {
						continue;
					}

					if (!group2Instructions.ContainsKey(group)) {
						group2Instructions.Add(group, new List<InstructionBase>());
					}
					var instructionCopy = instruction.Copy(targetDetails);
					group2Instructions[group].Add(instructionCopy);
				}
			}

			return group2Instructions;
		}

		private void Load(string fileName) {
			if (!File.Exists(fileName)) {
				Debug.Log("File doesn't exist!");
				return;
			}

			_fileName = fileName;

			RemoveAll();

			var settings = new JsonSerializerSettings {
				TypeNameHandling = TypeNameHandling.Auto
			};
			var jsonString = File.ReadAllText(_fileName);
			SceneData = JsonConvert.DeserializeObject<SceneData>(jsonString, settings);

			CreateObjects(SceneData);



			Debug.Log("Loaded: " + _fileName);
		}

		private void RemoveAll() {
			var rootObjects = new List<GameObject>();
			UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(rootObjects);

			foreach (var rootObject in rootObjects) {
				var detailBase = rootObject.GetComponent<DetailBase>();

				if (detailBase != null) {
					Object.Destroy(rootObject);
				}
			}

			_id2Detail.Clear();
		}

		private void CreateObjects(SceneData sceneData) {
			var oldId2Detail = new Dictionary<int, Detail>();
			var links2Connections = new Dictionary<LinksBase, List<int>>();

			foreach (var detailData in sceneData.SingleDetails) {
				CreateDetail(detailData);
			}

			_sourceInstructions = new List<InstructionBase>();

			foreach (var connectedGroup in sceneData.ConnectedGroups) {
				var detailsGroup = DetailsGroup.CreateNewGroup();

				foreach (var detailData in connectedGroup.Details) {
					var oldId = detailData.Id;
					var newDetail = CreateDetail(detailData);

					links2Connections.Add(newDetail.Links, detailData.Connections);
					oldId2Detail.Add(oldId, newDetail);

					detailsGroup.Add(newDetail);
				}

				_sourceInstructions.AddRange(connectedGroup.Instructions);
			}

			foreach (var detailLinks in links2Connections.Keys) {
				var connections = links2Connections[detailLinks];

				foreach (var id in connections) {
					detailLinks.Connections.Add(oldId2Detail[id]);
				}
			}

			foreach (var instruction in _sourceInstructions)
			{
				var newTargetDetails = new HashSet<int>();

				foreach (var detailId in instruction.TargetDetails) {
					newTargetDetails.Add(oldId2Detail[detailId].GetInstanceID());
				}

				instruction.TargetDetails = newTargetDetails;
			}
		}

		private Detail CreateDetail(DetailData detailData) {
			var newDetail = AddDetailPanel.GetDetail(detailData.Type);
			var newId = newDetail.GetInstanceID();

			newDetail.transform.position = detailData.Position;
			newDetail.transform.eulerAngles = detailData.Rotation;
			newDetail.GetComponent<Renderer>().material = AddDetailPanel.GetMaterial(detailData.Color);

			_id2Detail.Add(newId, newDetail);
			detailData.Id = newId;

			return newDetail;
		}

	}
}
