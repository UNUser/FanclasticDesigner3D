﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Assets.Scripts {

	public class Session
	{
		public SceneData SceneData { get; private set; }

		public string FileName { get { return _fileName; } }

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

		//TODO убрать после фикса файлов
		public void Fix()
		{
			if (string.IsNullOrEmpty(_fileName)) return;

			Reset();
			foreach (var detail in _id2Detail.Values) {
				detail.transform.rotation *= Quaternion.AngleAxis(-90, Vector3.right);
			}

			foreach (var instruction in _sourceInstructions) {
				var instructionAdd = instruction as InstructionAdd;

				if (instructionAdd == null) continue;

				var newRotation = Quaternion.Euler(instructionAdd.Rotation) * Quaternion.AngleAxis(-90, Vector3.right);

				instructionAdd.Rotation = newRotation.eulerAngles;
			}
			Save(_fileName);

			Debug.Log("Fixed: " + _fileName);
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

			var mergedInstructions = GetMergedInstructions();

			foreach (var root in roots) {
				var detailBase = root.GetComponent<DetailBase>();

				if (detailBase == null) {
					continue;
				}

				if (!detailBase.gameObject.activeSelf) {
//					Object.Destroy(detailBase.gameObject);
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

				SceneData.ConnectedGroups.Add(connectedGroup);
			}


			var group2Instructions = SplitInstructions(_id2Detail, mergedInstructions);

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
			var checkedFileName = fileName.EndsWith(".fcl") ? fileName : (fileName + ".fcl");

			File.WriteAllText(checkedFileName, json);
			_fileName = checkedFileName;

			Debug.Log("Saved: " + checkedFileName);
		}

		private List<InstructionBase> GetMergedInstructions()
		{
			HashSet<Detail> invalidDetails;
			var actionsLog = AppController.Instance.ActionsLog;
			var newInstructions = actionsLog.GetInstructions(out invalidDetails);
			var mergedInstructions = new List<InstructionBase>();
			var invalidIds = new HashSet<int>(invalidDetails.Select(detail => detail.GetInstanceID()));

			foreach (var instruction in _sourceInstructions)
			{
				var mergedTargetDetails = new HashSet<int>(instruction.TargetDetails);

				mergedTargetDetails.ExceptWith(invalidIds);
				
				var mergedInstruction = instruction.Copy(mergedTargetDetails);

//				instruction.TargetDetails.ExceptWith(invalidIds);

				if (mergedInstruction.TargetDetails.Any()) {
					mergedInstructions.Add(mergedInstruction);
				}
			}

			mergedInstructions.AddRange(newInstructions);
			return mergedInstructions;
//			_sourceInstructions = mergedInstructions;

//			actionsLog.Clear();
		}

		private Dictionary<DetailsGroup, List<InstructionBase>> SplitInstructions(Dictionary<int, Detail> id2Detail, List<InstructionBase> mergedInstrucions)
		{
			var group2Instructions = new Dictionary<DetailsGroup, List<InstructionBase>>();

			foreach (var instruction in mergedInstrucions)
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

				var group2TargetDetails = instruction.TargetDetails.ToLookup(id => id2Detail[id].Group);

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
					Object.DestroyImmediate(rootObject);
				}
			}

			_id2Detail.Clear();
			_sourceInstructions = null;
			SceneData = null;
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
			if (!newDetail.FixedColor) {
				newDetail.Color = AddDetailPanel.GetColor(detailData.Color);
			}

			_id2Detail.Add(newId, newDetail);
			detailData.Id = newId;
			detailData.Detail = newDetail;

			return newDetail;
		}

	}
}
