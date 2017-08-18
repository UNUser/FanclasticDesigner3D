using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
#if UNITY_STANDALONE_WIN
using System.Windows.Forms;
#endif
using UnityEngine;
using UnityEngine.UI;
using Application = UnityEngine.Application;
using Group = System.Collections.Generic.List<Assets.Scripts.Detail>;

namespace Assets.Scripts {

	public enum AppMode
	{
		EditorMode,
		InstructionsMode
	}

    public class AppController : MonoBehaviour
    {
        public FileSelectionDialogLayer FileSelectionDialogLayer;
        public Text DebugTextGroups;
        public Text DebugTextDetail;
	    public ColorSetter ColorSetter;
	    public GameObject ExitButton;
	    public ActionsLog ActionsLog;
	    public GameObject EditorLayer;
	    public Dropdown ModeSwitcher;
	    public InstructionsLayer InstructionsLayer;

	    public Session Session { get; private set; }
		public AppMode Mode { get; private set; }

	    public AppResources Resources;

	    private string LastPath
	    {
		    get { return PlayerPrefs.GetString("LastPath", Application.persistentDataPath); }
		    set { PlayerPrefs.SetString("LastPath", value); }
	    }

	    public static AppController Instance {
			get { return _instance ?? (_instance = FindObjectOfType <AppController>()); }
	    }
	    private static AppController _instance;

//        public Material[] DetailColors;

		public SelectedDetails SelectedDetails;

        public void RotateSelectedByX() {
            
            SelectedDetails.AppRotate(Vector3.right);
        }

        public void RotateSelectedByY() {

            SelectedDetails.AppRotate(Vector3.up);
        }

        public void RotateSelectedByZ() {

            SelectedDetails.AppRotate(Vector3.forward);
        }

        public void RemoveSelected() {

			SelectedDetails.Delete();
        }

	    public void OnExitButtonClicked()
	    {
		    Application.Quit();
	    }

	    public void OnFixButtonClicked()
	    {
		    Session.Fix();
	    }

	    public void OnModeChanged(int modeInt)
	    {
		    Mode = (AppMode) modeInt;

			Session.Reset();

			EditorLayer.SetActive(Mode == AppMode.EditorMode);
			InstructionsLayer.gameObject.SetActive(Mode == AppMode.InstructionsMode);

		    if (Mode == AppMode.EditorMode) {
			    return;
		    }

		    var sceneData = Session.SceneData;

		    foreach (var detailData in sceneData.SingleDetails) {
			    Session.GetDetail(detailData.Id).gameObject.SetActive(false);
		    }

		    ConnectedGroup selectedGroup = null;

		    foreach (var connectedGroup in sceneData.ConnectedGroups)
		    {
			    if (selectedGroup == null) {
				    selectedGroup = connectedGroup;
					continue;
			    }

			    ConnectedGroup unselectedGroup = connectedGroup;

			    if (selectedGroup.Details.Count < connectedGroup.Details.Count)
			    {
				    unselectedGroup = selectedGroup;
				    selectedGroup = connectedGroup;
			    }

				var id = unselectedGroup.Details.First().Id;
				var detailsGroup = Session.GetDetail(id).Group;

				detailsGroup.gameObject.SetActive(false);
		    }

		    if (selectedGroup == null) {
			    return;
		    }

		    foreach (var detailData in selectedGroup.Details)
		    {
			    Session.GetDetail(detailData.Id).gameObject.SetActive(false);
		    }

			InstructionsLayer.ShowInstructions(selectedGroup.Instructions, Session);
	    }

        public void OnSaveButtonClicked() {

	        if (!SelectedDetails.IsValid) {
		        return;
	        }

#if UNITY_STANDALONE_WIN

			ShowSaveFileDialog(LastPath);
#else
            FileSelectionDialogLayer.ShowFileSelectionDialog(LastPath, Save, true);
#endif
        }


#if UNITY_STANDALONE_WIN

		[DllImport("user32.dll")]
		private static extern void SaveFileDialog();
		[DllImport("user32.dll")]
		private static extern void OpenFileDialog(); 

		public void ShowSaveFileDialog(string path) {

			var saveFileDialog = new SaveFileDialog {
				InitialDirectory = path
			};


			if (saveFileDialog.ShowDialog() == DialogResult.OK) {
				Save(saveFileDialog.FileName);
			}
		}

		public void ShowOpenFileDialog(string path) {

			var openFileDialog = new OpenFileDialog {
				InitialDirectory = path
			};


			if (openFileDialog.ShowDialog() == DialogResult.OK) {
				Load(openFileDialog.FileName);
			}
		}
#endif



        public void OnLoadButtonClicked() {


#if UNITY_STANDALONE_WIN

			ShowOpenFileDialog(LastPath);
#else
            FileSelectionDialogLayer.ShowFileSelectionDialog(LastPath, Load, false);
#endif

		}

	    private void Save(string fileName)
	    {
		    Session.Save(fileName);
		    ModeSwitcher.interactable = Session.SceneData.ConnectedGroups.Count > 0;
			LastPath = Path.GetDirectoryName(fileName);
	    }

	    private void Load(string fileName)
	    {
		    Session = new Session(fileName);
			ModeSwitcher.interactable = Session.SceneData.ConnectedGroups.Count > 0;
		    LastPath = Path.GetDirectoryName(fileName);
	    }

	    public void Awake()
	    {
		    SelectedDetails = gameObject.AddComponent<SelectedDetails>();
			ExitButton.SetActive(!Application.isMobilePlatform);
	    }

        public void Start()
        {
			Session = new Session();
	        ModeSwitcher.interactable = false;
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
	public abstract class InstructionBase
	{
		public abstract string Action { get; }
		public HashSet<int> TargetDetails;

		public InstructionBase Copy(HashSet<int> targetDetails)
		{
			var instance = GetInstanceCopy();

			instance.TargetDetails = targetDetails;
			return instance;
		}

		public abstract void Do();
		public abstract void Undo();

		protected abstract InstructionBase GetInstanceCopy();
	}


	[Serializable]
	public class InstructionMoveAndRotate : InstructionBase
	{
		public override string Action {
			get { return "MoveAndRotate"; }
		}

		public SerializableVector3 Pivot;
		public SerializableVector3Int Rotation;
		public SerializableVector3 Offset;

		public override void Do() {
			foreach (var id in TargetDetails)
			{
				var detail = AppController.Instance.Session.GetDetail(id);
				

				detail.transform.RotateAround(Pivot, Vector3.forward, Rotation.z);
				detail.transform.RotateAround(Pivot, Vector3.right, Rotation.x);
				detail.transform.RotateAround(Pivot, Vector3.up, Rotation.y);

				detail.transform.Translate(Offset, Space.World);
			}
		}

		public override void Undo() {
			foreach (var id in TargetDetails) {
				var detail = AppController.Instance.Session.GetDetail(id);

				detail.transform.Translate( - (Vector3) Offset, Space.World);

				detail.transform.RotateAround(Pivot, Vector3.up, - Rotation.y);
				detail.transform.RotateAround(Pivot, Vector3.right, - Rotation.x);
				detail.transform.RotateAround(Pivot, Vector3.forward, - Rotation.z);
			}
		}

		protected override InstructionBase GetInstanceCopy() {
			return new InstructionMoveAndRotate{ Pivot = Pivot, Rotation = Rotation, Offset = Offset };
		}

		public override string ToString() {
			return string.Format("MOVE_AND_ROTATE Offset: {0}, Rotation: {1}, Pivot: {2}", Offset, Rotation, Pivot);
		}
	}


	[Serializable]
	public class InstructionAdd : InstructionBase
	{
		public override string Action {
			get { return "Add"; }
		}

		public SerializableVector3Int Rotation;
		public SerializableVector3Int Position;

		public override void Do() {
			var id = TargetDetails.First();
			var detail = AppController.Instance.Session.GetDetail(id);

			detail.transform.eulerAngles = Rotation;
			detail.transform.position = Position;

			detail.gameObject.SetActive(true);
		}

		public override void Undo() {
			var id = TargetDetails.First();
			var detail = AppController.Instance.Session.GetDetail(id);

			detail.gameObject.SetActive(false);
		}

		protected override InstructionBase GetInstanceCopy() {
			return new InstructionAdd { Rotation = Rotation, Position = Position };
		}

		public override string ToString() {
			return string.Format("ADD Position: {0}, Rotation: {1}", Position, Rotation);
		}
	}


	[Serializable]
	public class SceneData
	{
		public string Version = "1.0";
		public List<DetailData> SingleDetails = new List<DetailData>();
		public List<ConnectedGroup> ConnectedGroups = new List<ConnectedGroup>();
	}

	[Serializable]
	public class ConnectedGroup
	{
		public List<DetailData> Details = new List<DetailData>();
		public List<InstructionBase> Instructions = new List<InstructionBase>();
	}

    [Serializable]
    public class DetailData
    {
		[NonSerialized]
	    public Detail Detail;

        public int Id;
		public string Type;
		public string Color;

        public SerializableVector3Int Position;
        public SerializableVector3Int Rotation;

		public List<int> Connections;

        public override string ToString()
        {
            return string.Format("Id: {0}, Position: {1}, Rotation{2}, Type: {3}, Connections: {{{4}}}, Color: {5}",
                                  Id,      Position,      Rotation,    Type,      Connections,          Color);
        }
    }
}
