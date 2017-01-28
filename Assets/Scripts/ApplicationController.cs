using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts {
    public class ApplicationController : MonoBehaviour
    {
        public FileSelectionDialogLayer FileSelectionDialogLayer;
        public Text DebugTextGroups;
        public Text DebugTextDetail;


        public Material[] DetailColors;

        public DetailBase SelectedDetail
        {
            get { return _selectedDetail; }
            set
            {
                if (_selectedDetail != null)
                {
                    _selectedDetail.IsSelected = false;
                }
                _selectedDetail = value;
                if (value != null)
                {
                    _selectedDetail.IsSelected = true;
                }
            }
        }

        private DetailBase _selectedDetail = null;

        public void RotateSelectedByX() {
            if (SelectedDetail == null) return;
            
            SelectedDetail.Rotate(Vector3.right);
        }

        public void RotateSelectedByY() {
            if (SelectedDetail == null) return;

            SelectedDetail.Rotate(Vector3.up);
        }

        public void RotateSelectedByZ() {
            if (SelectedDetail == null) return;

            SelectedDetail.Rotate(Vector3.forward);
        }

        public void RemoveSelected() {
            if (SelectedDetail == null) return;

            SelectedDetail.Detach();
            Destroy(SelectedDetail.gameObject);
        }

        public void OnSaveButtonClicked()
        {
            FileSelectionDialogLayer.ShowFileSelectionDialog(Save, true);
        }

        private void Save(string fileName)
        {
            var bf = new BinaryFormatter();
            var file = File.Create(fileName);

            SelectedDetail.IsSelected = false;

            var roots = new List<GameObject>();
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(roots);

            var groups = new List<List<DetailData>>();

            foreach (var root in roots) {
                var detailBase = root.GetComponent<DetailBase>();

                if (detailBase == null) continue;

                var currentGroup = new List<DetailData>();
                groups.Add(currentGroup);

                var detail = detailBase as Detail;
                if (detail != null) {
                    currentGroup.Add(detail.Data);
                    continue;
                }

                foreach (Transform child in detailBase.transform)
                {
                    var childDetail = child.GetComponent<Detail>();
                    currentGroup.Add(childDetail.Data);
                }
            }

            bf.Serialize(file, groups);
            file.Close();
            
            Debug.Log("Saved: " + fileName);
        }

        public void OnLoadButtonClicked()
        {
            FileSelectionDialogLayer.ShowFileSelectionDialog(Load, false);
        }

        private void Load(string fileName)
        {
            if (!File.Exists(fileName))
            {
                Debug.Log("File doesn't exist!");
                return;
            }

            RemoveAll();

            var bf = new BinaryFormatter();
            var file = File.Open(fileName, FileMode.Open);

            var groups = (List<List<DetailData>>) bf.Deserialize(file);

            file.Close();
            CreateObjects(groups);

            Debug.Log("Loaded: " + fileName);
        }

        private void RemoveAll()
        {
            var rootObjects = new List<GameObject>();
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(rootObjects);

            foreach (var rootObject in rootObjects)
            {
                var detailBase = rootObject.GetComponent<DetailBase>();

                if (detailBase != null) {
                    Destroy(rootObject);
                }
            }
        }

        private void CreateObjects(List<List<DetailData>> groups)
        {
            var id2Detail = new Dictionary<int, Detail>();
            var links2Data= new Dictionary<DetailLinks, DetailLinksData>();

            foreach (var @group in groups)
            {
                var detailsGroup = group.Count > 1 
                    ? DetailsGroup.CreateNewGroup() 
                    : null;

                foreach (var detailData in group)
                {
                    var newDetail = AddDetailPanel.GetDetail(detailData.Type);

                    newDetail.transform.position = detailData.Position;
                    newDetail.transform.eulerAngles = detailData.Rotation;
                    newDetail.GetComponent<Renderer>().material = AddDetailPanel.GetMaterial(detailData.Material);

                    links2Data.Add(newDetail.Links, detailData.Links);
                    id2Detail.Add(detailData.Id, newDetail);

                    if (detailsGroup != null) {
                        detailsGroup.Add(newDetail);
                    }
                }
            }

            foreach (var detailLinks in links2Data.Keys)
            {
                var linksData = links2Data[detailLinks];

                foreach (var id in linksData.Connections) {
                    detailLinks.Connections.Add(id2Detail[id]);
                }

                foreach (var id in linksData.Touches) {
                    detailLinks.Touches.Add(id2Detail[id]);
                }

                foreach (var connection in linksData.ImplicitConnections) {
                    var newConnection = new HashSet<Detail>();

                    foreach (var id in connection) {
                        newConnection.Add(id2Detail[id]);
                    }
                    detailLinks.ImplicitConnections.Add(newConnection);
                }
            }
        }

        public void Start()
        {
            StartCoroutine(UpdateDebugInfo());
        }

        private IEnumerator UpdateDebugInfo()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.9f);

                if (!DebugTextGroups.IsActive()) continue;

                DebugUpdateGroupsInfo();
                DebugUpdateDetailInfo();
            }
        }

        private void DebugUpdateGroupsInfo ()
        {
            var roots = new List<GameObject>();
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(roots);

            var groups = new List<Transform>();
            var details = new HashSet<Detail>();

            foreach (var root in roots) {
                var group = root.GetComponent<DetailsGroup>();
                if (group != null) {
                    groups.Add(group.transform);
                    continue;
                }
                var detail = root.GetComponent<Detail>();
                if (detail != null) {
                    details.Add(detail);
                }
            }

            var str = new StringBuilder();
            groups.Sort((t1, t2) => t2.childCount.CompareTo(t1.childCount));

            foreach (var detailsGroup in groups) {
                str.Append("[ ");
                var needComma = false;
                foreach (Transform detail in detailsGroup) {
                    AddComma(str, ref needComma);
                    str.Append(detail.gameObject.name.Remove(3));
                }
                str.Append(" ]");
                str.AppendLine();
            }

            foreach (var detail in details) {
                str.AppendLine(detail.gameObject.name.Remove(3));
            }

            DebugTextGroups.text = str.ToString();
        }

        private void DebugUpdateDetailInfo()
        {
            var selected = SelectedDetail as Detail;

            if (selected == null) {
                DebugTextDetail.text = string.Empty;
                return;
            }

            var str = new StringBuilder();
            var needComma = false;

            str.AppendLine("Type: " + selected.gameObject.name.Remove(3));

            str.Append("Connections: ");
            foreach (var connection in selected.Connections)
            {
                AddComma(str, ref needComma);
                str.Append(connection.gameObject.name.Remove(3));
            }

            str.AppendLine();
            str.Append("Touches: ");
            needComma = false;

            foreach (var touch in selected.Touches) {
                AddComma(str, ref needComma);
                str.Append(touch.gameObject.name.Remove(3));
            }

            str.AppendLine();
            str.AppendLine("Implicit Connections: ");

            foreach (var @implicit in selected.ImplicitConnections) {
                str.Append("   [ ");
                needComma = false;
                foreach (var detail in @implicit)
                {
                    AddComma(str, ref needComma);
                    str.Append(detail.gameObject.name.Remove(3));
                }
                str.Append(" ]");
                str.AppendLine();
            }

            DebugTextDetail.text = str.ToString();
        }

        public static void AddComma(StringBuilder str, ref bool needComma)
        {
            if (needComma)
                str.Append(", ");
            else
                needComma = true;
        }
    }

    [Serializable]
    public struct DetailData
    {
        public int Id;
        public SerializableVector3 Position;
        public SerializableVector3 Rotation;

        public string Type;

        public int GroupId;
        public DetailLinksData Links;
        public string Material;

        public override string ToString()
        {
            return string.Format("Id: {0}, Position: {1}, Rotation{2}, Type: {3}, GroupId: {4}, Links: {{{5}}}, Material: {6}",
                                  Id,      Position,      Rotation,    Type,      GroupId,      Links,      Material);
        }
    }
}
