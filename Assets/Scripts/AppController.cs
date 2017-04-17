using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
#if UNITY_STANDALONE_WIN
using System.Windows.Forms;
#endif
using UnityEngine;
using UnityEngine.UI;
using Application = UnityEngine.Application;

namespace Assets.Scripts {
    public class AppController : MonoBehaviour
    {
        public FileSelectionDialogLayer FileSelectionDialogLayer;
        public Text DebugTextGroups;
        public Text DebugTextDetail;
	    public ColorSetter ColorSetter;
	    public GameObject ExitButton;

	    public static AppController Instance {
			get { return _instance ?? (_instance = FindObjectOfType <AppController>()); }
	    }
	    private static AppController _instance;

        public Material[] DetailColors;

		public SelectedDetails SelectedDetails;

        public void RotateSelectedByX() {
            
            SelectedDetails.Rotate(Vector3.right);
        }

        public void RotateSelectedByY() {

            SelectedDetails.Rotate(Vector3.up);
        }

        public void RotateSelectedByZ() {

            SelectedDetails.Rotate(Vector3.forward);
        }

        public void RemoveSelected() {

			SelectedDetails.Delete();

        }

	    public void OnExitButtonClicked()
	    {
		    Application.Quit();
	    }

        public void OnSaveButtonClicked() {

	        if (!SelectedDetails.IsValid) {
		        return;
	        }

#if UNITY_STANDALONE_WIN

			ShowSaveFileDialog();
#else
            FileSelectionDialogLayer.ShowFileSelectionDialog(Save, true);
#endif
        }


#if UNITY_STANDALONE_WIN

		[DllImport("user32.dll")]
		private static extern void SaveFileDialog();
		[DllImport("user32.dll")]
		private static extern void OpenFileDialog(); 

		public void ShowSaveFileDialog() {

			var saveFileDialog = new SaveFileDialog {
				InitialDirectory = Application.persistentDataPath
			};


			if (saveFileDialog.ShowDialog() == DialogResult.OK) {
				Save(saveFileDialog.FileName);
			}
		}

		public void ShowOpenFileDialog() {

			var openFileDialog = new OpenFileDialog {
				InitialDirectory = Application.persistentDataPath
			};


			if (openFileDialog.ShowDialog() == DialogResult.OK) {
				Load(openFileDialog.FileName);
			}
		}
#endif

        private void Save(string fileName)  //TODO добавить заголовок, чтобы определять свои файлы
        {
            var bf = new BinaryFormatter();
            var file = File.Create(fileName);

            SelectedDetails.Clear();

            var roots = new List<GameObject>();
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(roots);

			var sceneData = new SceneData { Components = new List<ComponentBase>() };
//            var groups = new List<List<DetailData>>();

            foreach (var root in roots) {
                var detailBase = root.GetComponent<DetailBase>();

                if (detailBase == null) continue;

//                var currentGroup = new List<DetailData>();
//                groups.Add(currentGroup);

                var detail = detailBase as Detail;
                if (detail != null) {
					sceneData.Components.Add(new SimpleComponent{Data = detail.Data});
//                    currentGroup.Add(detail.Data);
                    continue;
                }

				var complexComponent = new ComplexComponent();

                foreach (Transform child in detailBase.transform)
                {
                    var childDetail = child.GetComponent<Detail>();
					complexComponent.Components.Add(new SimpleComponent{Data = childDetail.Data});
//                    currentGroup.Add(childDetail.Data);
                }

				sceneData.Components.Add(complexComponent);
            }

	        

            bf.Serialize(file, sceneData);
            file.Close();
            
            Debug.Log("Saved: " + fileName);
        }

        public void OnLoadButtonClicked() {

#if UNITY_STANDALONE_WIN

			ShowOpenFileDialog();
#else
            FileSelectionDialogLayer.ShowFileSelectionDialog(Load, false);
#endif

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

            var sceneData = (SceneData) bf.Deserialize(file);

            file.Close();
            CreateObjects(sceneData.Components);

            Debug.Log("Loaded: " + fileName);
        }

        private void RemoveAll()
        {
            var rootObjects = new List<GameObject>();
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(rootObjects);

			SelectedDetails.Clear();

            foreach (var rootObject in rootObjects)
            {
                var detailBase = rootObject.GetComponent<DetailBase>();

                if (detailBase != null) {
                    Destroy(rootObject);
                }
            }
        }

        private void CreateObjects(List<ComponentBase> sceneData)
        {
            var id2Detail = new Dictionary<int, Detail>();
            var links2Data= new Dictionary<LinksBase, DetailLinksData>();

            foreach (var componentBase in sceneData)
            {
				var detailsGroup = componentBase is ComplexComponent
                    ? DetailsGroup.CreateNewGroup() 
                    : null;

                foreach (var component in componentBase is ComplexComponent ? (componentBase as ComplexComponent).Components : new List<ComponentBase>{componentBase})
                {
					var detailData = ((SimpleComponent) component).Data;
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

//                foreach (var id in linksData.Touches) {
//                    detailLinks.Touches.Add(id2Detail[id]);
//                }
//
//                foreach (var connection in linksData.ImplicitConnections) {
//                    var newConnection = new HashSet<Detail>();
//
//                    foreach (var id in connection) {
//                        newConnection.Add(id2Detail[id]);
//                    }
//                    detailLinks.ImplicitConnections.Add(newConnection);
//                }
            }
        }

	    public void Awake()
	    {
		    SelectedDetails = gameObject.AddComponent<SelectedDetails>();
			ExitButton.SetActive(!Application.isMobilePlatform);
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
			if (SelectedDetails.Count != 1) {
                DebugTextDetail.text = string.Empty;
                return;
            }

	        var selected = SelectedDetails.First;

            var str = new StringBuilder();
            var needComma = false;

            str.AppendLine("Type: " + selected.gameObject.name.Remove(3));

            str.Append("Connections: ");
            foreach (var connection in selected.Connections)
            {
                AddComma(str, ref needComma);
                str.Append(connection.gameObject.name.Remove(3));
            }

//            str.AppendLine();
//            str.Append("Touches: ");
//            needComma = false;
//
//            foreach (var touch in selected.Touches) {
//                AddComma(str, ref needComma);
//                str.Append(touch.gameObject.name.Remove(3));
//            }
//
//            str.AppendLine();
//            str.AppendLine("Implicit Connections: ");
//
//            foreach (var @implicit in selected.ImplicitConnections) {
//                str.Append("   [ ");
//                needComma = false;
//                foreach (var detail in @implicit)
//                {
//                    AddComma(str, ref needComma);
//                    str.Append(detail.gameObject.name.Remove(3));
//                }
//                str.Append(" ]");
//                str.AppendLine();
//            }

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
	public struct SceneData
	{
		public const string Version = "0.1.0";
		public List<ComponentBase> Components;
	}

	[Serializable]
	public class ComponentBase { }

	[Serializable]
	public class ComplexComponent : ComponentBase
	{
		public List<ComponentBase> Components = new List<ComponentBase>();
	}

	[Serializable]
	public class SimpleComponent : ComponentBase
	{
		public DetailData Data;
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
