using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DG.Tweening;
using UnityEngine.SceneManagement;
#if UNITY_STANDALONE_WIN
using System.Windows.Forms;
#endif
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityFBXExporter;
using Newtonsoft.Json.Utilities;
using Application = UnityEngine.Application;
using Group = System.Collections.Generic.List<Assets.Scripts.Detail>;


namespace Assets.Scripts
{

    public enum AppMode
    {
        EditorMode,
        InstructionsMode
    }

    public class AppController : MonoBehaviour
    {
        public CameraController CameraController;
        public FileSelectionDialogLayer FileSelectionDialogLayer;
        public Text DebugTextGroups;
        public Text DebugTextDetail;
        public ColorSetter ColorSetter;
        public GameObject ExitButton;
        public GameObject ExportButton;
        public ActionsLog ActionsLog;
        public GameObject EditorLayer;
        public SelectionLayer SelectionLayer;
        public Dropdown ModeSwitcher;
        public Dropdown LanguageSwitcher;
        public InstructionsLayer InstructionsLayer;
        public GameObject TutorialLayer;
	    public ModelsSetsLayer ModelsSetsLayer;

	    public ToggleGroup ToolsToggleGroup;
        public Toggle MovementMode;
        public AxisMover AxisMover;
	    public GameObject MovementController;
	    public GameObject RotationController;

        public Session Session { get; private set; }
        public AppMode Mode { get; private set; }

        public AppResources Resources;

		public static string DemoModelsPath { get { return Application.persistentDataPath + "/DemoModels"; } }

        private string LastPath
        {
            get { return PlayerPrefs.GetString("LastPath", Application.persistentDataPath); }
            set { PlayerPrefs.SetString("LastPath", value); }
        }

        public static AppController Instance
        {
            get { return _instance ?? (_instance = FindObjectOfType<AppController>()); }
        }
        private static AppController _instance;

        //        public Material[] DetailColors;


//		private static int _screenshotNumber;
//        protected void Update()
//        {
//            if (!Input.GetKeyDown(KeyCode.P))
//            {
//				return;
//            }
//
//
//			var directoryPath = Path.Combine(Application.persistentDataPath, "Screenshots");
//			var langPrefix = UiLang.Lang == 1 ? "Ru" : "En";
//			var mainCamera = Camera.main;
//			string filePath;
//
//			if (!Directory.Exists(directoryPath)) {
//				Directory.CreateDirectory(directoryPath);
//			}
//
//			do {
//				var fileName = string.Format("{0}_{1}x{2}({3}).png",
//					langPrefix, mainCamera.pixelWidth, mainCamera.pixelHeight, ++_screenshotNumber);
//
//				filePath = Path.Combine(directoryPath, fileName);
//
//			} while (File.Exists(filePath));
//
//			ScreenCapture.CaptureScreenshot(filePath);
//
//			Debug.Log("Saved: " + filePath);
//        }

        public void OnMovementModeChanged(bool value)
        {
	        MovementController.SetActive(value);
			AxisMover.gameObject.SetActive(ToolsToggleGroup.AnyTogglesOn());
        }

		public void OnRotationModeChanged(bool value) 
		{
			RotationController.SetActive(value);
			AxisMover.gameObject.SetActive(ToolsToggleGroup.AnyTogglesOn());
		}

        public SelectedDetails SelectedDetails;

        public void RotateSelectedByX()
        {

            SelectedDetails.AppRotate(Vector3.right);
        }

        public void RotateSelectedByY()
        {

            SelectedDetails.AppRotate(Vector3.up);
        }

        public void RotateSelectedByZ()
        {

            SelectedDetails.AppRotate(Vector3.forward);
        }

        public void RemoveSelected()
        {

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

            if (Mode == AppMode.EditorMode)
            {
                var containers = GameObject.FindGameObjectsWithTag("Container");

                foreach (var container in containers)
                {
                    Destroy(container.gameObject);
                }

                InstructionsLayer.gameObject.SetActive(false);

                return;
            }

            ModeSwitcher.gameObject.SetActive(false);
            SetDetailsVisible(false);
            SelectionLayer.ShowSelection(Session.SceneData.ConnectedGroups, OnGroupSelected);
        }

        private void OnGroupSelected(ConnectedGroup selectedGroup)
        {
            ModeSwitcher.gameObject.SetActive(true);
            Destroy(ModeSwitcher.GetComponentInChildren<Canvas>().gameObject); ///// TODO проверить баг в новых версиях юнити!!!

            if (selectedGroup == null)
            {
                //              SetDetailsVisible(true);
                EditorLayer.SetActive(true);
                ModeSwitcher.value = (int) AppMode.EditorMode;
                return;
            }

            foreach (var detailData in selectedGroup.Details)
            {
                Session.GetDetail(detailData.Id).gameObject.SetActive(false);
            }

            InstructionsLayer.gameObject.SetActive(true);
            InstructionsLayer.ShowInstructions(selectedGroup.Instructions, Session);
        }

        private void SetDetailsVisible(bool visible)
        {
            var sceneData = Session.SceneData;

            foreach (var detailData in sceneData.SingleDetails)
            {
                Session.GetDetail(detailData.Id).gameObject.SetActive(visible);
            }

            foreach (var connectedGroup in sceneData.ConnectedGroups)
            {
                Session.GetDetail(connectedGroup.Details.First().Id).Group.gameObject.SetActive(visible);
            }
        }

        public void OnNewSceneButtonClicked()
        {
            Session = new Session();
            ModeSwitcher.interactable = false;
        }

        public void OnSaveButtonClicked()
        {

            if (!SelectedDetails.IsValid)
            {
                return;
            }

            SaveCrossVer(LastPath);
        }

        private void SaveCrossVer(string path)
        {

#if UNITY_STANDALONE_WIN

            ShowSaveFileDialog(path);
#else
            FileSelectionDialogLayer.ShowFileSelectionDialog(path, Save, true);
#endif

        }

        public event Action<int> LanguageChanged;
        private bool _isLangInited;

        public void OnLanguageChanged(int index)
        {
            if (!_isLangInited)
            {
                return;
            }

            UiLang.Lang = index;

            if (LanguageChanged != null)
            {
                LanguageChanged(index);
            }
            Destroy(LanguageSwitcher.GetComponentInChildren<Canvas>().gameObject); ///// TODO проверить баг в новых версиях юнити!!!
        }

#if UNITY_STANDALONE_WIN

        [DllImport("user32.dll")]
        private static extern void SaveFileDialog();
        [DllImport("user32.dll")]
        private static extern void OpenFileDialog();

	    [DllImport("user32.dll")]
	    public static extern IntPtr GetActiveWindow();
		[DllImport("User32.dll")]
		public static extern bool ShowWindow(IntPtr handle, int nCmdShow);

		private IntPtr _appWindowHandle;

        public void ShowSaveFileDialog(string path)
        {
            var saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = path,
                FileName = Instance.Session.FileName,
                AddExtension = true,
                DefaultExt = "fcl",
                Filter = "(*.fcl)|*.fcl"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                Save(saveFileDialog.FileName);
            }

			RestoreMainWindow();
        }

        public void ShowOpenFileDialog(string path)
        {

            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = path,
                AddExtension = true,
                DefaultExt = "fcl",
                Filter = "*.fcl|*.fcl|*.*|*.*"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadFile(openFileDialog.FileName);
            }

			RestoreMainWindow();
        }

        public void OnExportButtonClicked()
        {
            var openFileDialog = new SaveFileDialog
            {
                InitialDirectory = LastPath
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ExportScene(openFileDialog.FileName + ".fbx");
            }

			RestoreMainWindow();
        }

	    private void RestoreMainWindow()
	    {
#if !UNITY_EDITOR
			const int SW_RESTORE = 9;

			ShowWindow(_appWindowHandle, SW_RESTORE);
#endif
		}

        private void ExportScene(string path)
        {
            var groups = Session.SceneData.ConnectedGroups;
            ConnectedGroup targetGroup = null;


            foreach (var group in groups)
            {
                if (targetGroup == null || targetGroup.Details.Count < group.Details.Count)
                {
                    targetGroup = group;
                }
            }

            if (targetGroup == null) return;

            var detailId = targetGroup.Details.First().Id;
            var targetGameObject = Session.GetDetail(detailId).Group.gameObject;
            var success = FBXExporter.ExportGameObjToFBX(targetGameObject, path, true);

            if (success)
            {
                Debug.Log("Exported to " + path);
            }
            else
            {
                Debug.LogError("Export failed!");
            }
        }

#endif



        public void OnLoadButtonClicked()
        {
            LoadCrossVer(LastPath);
        }

        private void LoadCrossVer(string path)
        {

#if UNITY_STANDALONE_WIN

            ShowOpenFileDialog(path);
#else
            FileSelectionDialogLayer.ShowFileSelectionDialog(path, LoadFile, false);
#endif

        }

        public void OnModelsSetsButtonClicked()
        {
			ModelsSetsLayer.gameObject.SetActive(true);
        }

        private void Save(string fileName)
        {
            Session.Save(fileName);
            ModeSwitcher.interactable = Session.SceneData.ConnectedGroups.Count > 0;
            LastPath = Path.GetDirectoryName(fileName);
        }

	    public void LoadFile(string filePath)
	    {
			Session = new Session(new FileInfo(filePath));
			LastPath = Path.GetDirectoryName(filePath);
			OnLoad();
	    }

	    public void LoadModel(string modelName)
	    {
			Session = new Session(modelName);
			OnLoad();
	    }

        private void OnLoad()
        {
            ModeSwitcher.interactable = Session.SceneData.ConnectedGroups.Count > 0;

            if (Session.SceneData.ConnectedGroups.Count == 0)
            {
                return;
            }

            var detailsData = Session.SceneData.ConnectedGroups[0].Details;
            var bounds = detailsData[0].Detail.Bounds;

            foreach (var detailData in detailsData)
            {
                bounds.Encapsulate(detailData.Detail.Bounds);
            }

            CameraController.Focus = bounds.center;
        }

        public void Awake()
        {
            SelectedDetails = gameObject.AddComponent<SelectedDetails>();
            ExitButton.SetActive(!Application.isMobilePlatform);
            ExportButton.SetActive(!Application.isMobilePlatform);

            AotHelper.EnsureList<int>();
        }

        public void Start()
        {
            Session = new Session();
            ModeSwitcher.interactable = false;
            //            StartCoroutine(UpdateDebugInfo());

            LanguageSwitcher.value = UiLang.Lang;
            _isLangInited = true;

            CheckForFirstLaunch();

#if UNITY_STANDALONE_WIN
			_appWindowHandle = GetActiveWindow();
#endif
        }

        private bool _loadDemo;

        public void OnTutorialEnd()
        {
            if (_loadDemo)
            {
//                LoadModel("Logo");
                _loadDemo = false;
            }
        }

        private void CheckForFirstLaunch()
        {
            var isFirstLaunch = !PlayerPrefs.HasKey("WasLaunched");

            if (!isFirstLaunch)
            {
                return;
            }

            _loadDemo = true;

            PlayerPrefs.SetInt("WasLaunched", 1);
            TutorialLayer.SetActive(true);
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

        private void DebugUpdateGroupsInfo()
        {
            var roots = new List<GameObject>();
            SceneManager.GetActiveScene().GetRootGameObjects(roots);

            var groups = new List<Transform>();
            var details = new HashSet<Detail>();

            foreach (var root in roots)
            {
                var group = root.GetComponent<DetailsGroup>();
                if (group != null)
                {
                    groups.Add(group.transform);
                    continue;
                }
                var detail = root.GetComponent<Detail>();
                if (detail != null)
                {
                    details.Add(detail);
                }
            }

            var str = new StringBuilder();
            groups.Sort((t1, t2) => t2.childCount.CompareTo(t1.childCount));

            foreach (var detailsGroup in groups)
            {
                str.Append("[ ");
                var needComma = false;
                foreach (Transform detail in detailsGroup)
                {
                    AddComma(str, ref needComma);
                    str.Append(detail.gameObject.name.Remove(3));
                }
                str.Append(" ]");
                str.AppendLine();
            }

            foreach (var detail in details)
            {
                str.AppendLine(detail.gameObject.name.Remove(3));
            }

            DebugTextGroups.text = str.ToString();
        }

        private void DebugUpdateDetailInfo()
        {
            if (SelectedDetails.Count != 1)
            {
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

        [NonSerialized]
        protected Sequence Animation;
        [NonSerialized]
        protected GameObject AnimationContainer;
        [NonSerialized]
        protected const float BackwardsAnimationTime = 0.001f;

        public InstructionBase Copy(HashSet<int> targetDetails)
        {
            var instance = GetInstanceCopy();

            instance.TargetDetails = targetDetails;
            return instance;
        }

        public abstract void Do();
        public abstract void Undo();

        public void PlayAnimation()
        {
            if (Animation == null)
            {
                Animation = GetInstanceAnimation();
                AnimationContainer.tag = "Container";
                Animation.SetLoops(-1);
            }
            foreach (var id in TargetDetails)
            {
                var detail = AppController.Instance.Session.GetDetail(id);

                detail.transform.SetParent(AnimationContainer.transform);
            }
            CheckCamera();
            Animation.Play();
        }
        public void StopAnimation() { Animation.Rewind(); }

        protected abstract InstructionBase GetInstanceCopy();
        protected abstract Sequence GetInstanceAnimation();

        protected void CheckCamera()
        {
            var targetPoint = AnimationContainer.transform.position;
            var viewportTargetPoint = Camera.main.WorldToViewportPoint(targetPoint);

            if (viewportTargetPoint.x > 0.25 && viewportTargetPoint.x < 0.75 &&
                viewportTargetPoint.y > 0.25 && viewportTargetPoint.y < 0.75 &&
                viewportTargetPoint.z > 0)
            {
                return;
            }

            AppController.Instance.CameraController.Focus = targetPoint;
        }
    }


    [Serializable]
    public class InstructionMoveAndRotate : InstructionBase
    {
        public override string Action
        {
            get { return "MoveAndRotate"; }
        }

        public SerializableVector3 Pivot;
        public SerializableVector3Int Rotation;
        public SerializableVector3 Offset;



        public override void Do()
        {
            foreach (var id in TargetDetails)
            {
                var detail = AppController.Instance.Session.GetDetail(id);

                detail.transform.RotateAround(Pivot, Vector3.forward, Rotation.z);
                detail.transform.RotateAround(Pivot, Vector3.right, Rotation.x);
                detail.transform.RotateAround(Pivot, Vector3.up, Rotation.y);

                detail.transform.Translate(Offset, Space.World);
            }
        }

        public override void Undo()
        {
            foreach (var id in TargetDetails)
            {
                var detail = AppController.Instance.Session.GetDetail(id);

                detail.transform.Translate(-(Vector3) Offset, Space.World);

                detail.transform.RotateAround(Pivot, Vector3.up, -Rotation.y);
                detail.transform.RotateAround(Pivot, Vector3.right, -Rotation.x);
                detail.transform.RotateAround(Pivot, Vector3.forward, -Rotation.z);
            }
        }

        protected override Sequence GetInstanceAnimation()
        {
            var animation = DOTween.Sequence();

            AnimationContainer = new GameObject("Container");
            var container = AnimationContainer.transform;

	        if (Rotation == Vector3.zero)
	        {
		        Pivot = GetBounds().center - Offset;
	        }

	        SerializableVector3Int simplifiedRotation;
	        var simplificationKey = new SerializableVector3Int(Math.Abs(Rotation.x), 
															   Math.Abs(Rotation.y),
															   Math.Abs(Rotation.z));
			var rotationSimplification = new Dictionary<SerializableVector3Int, SerializableVector3Int>
	        {
				{new SerializableVector3Int(0, 180, 180), new SerializableVector3Int(180, 0, 0)},
				{new SerializableVector3Int(180, 0, 180), new SerializableVector3Int(0, 180, 0)},
				{new SerializableVector3Int(180, 180, 0), new SerializableVector3Int(0, 0, 180)},

//				{new SerializableVector3Int(x, 180, 180), new SerializableVector3Int(-x, 0, 0)},???

//				{new SerializableVector3Int(90, x, x), new SerializableVector3Int(90, 0, 0)},
//				{new SerializableVector3Int(-90, x, x), new SerializableVector3Int(-90, 0, 0)},
	        };

	        if (rotationSimplification.TryGetValue(simplificationKey, out simplifiedRotation))
	        {
		        Rotation = simplifiedRotation;
	        }

            container.position = Pivot + Offset;

			//backwards
            if (Offset != Vector3.zero)
            {
                animation.Append(container.DOMove(Pivot, BackwardsAnimationTime));
            }
			
	        var rotationX = new Vector3(GetMinRotationAngle(Rotation.x), 0, 0);
			var rotationY = new Vector3(0, GetMinRotationAngle(Rotation.y), 0);
			var rotationZ = new Vector3(0, 0, GetMinRotationAngle(Rotation.z));

	        if (rotationY != Vector3.zero) {
		        animation.Append(container.DORotate( - rotationY, BackwardsAnimationTime / 3, RotateMode.WorldAxisAdd));
	        }

			if (rotationX != Vector3.zero) {
				animation.Append(container.DORotate( - rotationX, BackwardsAnimationTime / 3, RotateMode.WorldAxisAdd));
			}

			if (rotationZ != Vector3.zero) {
				animation.Append(container.DORotate( - rotationZ, BackwardsAnimationTime / 3, RotateMode.WorldAxisAdd));
			}

			//forward
	        animation.AppendInterval(0.5f);

			if (rotationZ != Vector3.zero) {
				animation.Append(container.DORotate(rotationZ, 1f, RotateMode.WorldAxisAdd));
			}

			if (rotationX != Vector3.zero) {
				animation.Append(container.DORotate(rotationX, 1f, RotateMode.WorldAxisAdd));
			}

			if (rotationY != Vector3.zero) {
				animation.Append(container.DORotate(rotationY, 1f, RotateMode.WorldAxisAdd));
			}

            animation.AppendInterval(0.5f);
			
            if (Offset != Vector3.zero)
            {
                animation.Append(container.DOMove(Pivot + Offset, 1f))
                    .AppendInterval(1f);
            }

            return animation;
        }

	    private int GetMinRotationAngle(int angle)
	    {
			var normalized = angle % 360;

			return Mathf.Abs(normalized) > 180 
				? (normalized - Math.Sign(normalized) * 360)
				: normalized; 
	    }

        private Bounds GetBounds()
        {
            Bounds? sum = null;

            foreach (var id in TargetDetails)
            {
                var detail = AppController.Instance.Session.GetDetail(id);

                if (sum == null)
                {
                    sum = detail.Bounds;
                    continue;
                }
                sum.Value.Encapsulate(detail.Bounds);
            }
            return sum.Value;
        }

        protected override InstructionBase GetInstanceCopy()
        {
            return new InstructionMoveAndRotate { Pivot = Pivot, Rotation = Rotation, Offset = Offset };
        }

        public override string ToString()
        {
            return string.Format("MOVE_AND_ROTATE Offset: {0}, Rotation: {1}, Pivot: {2}", Offset, Rotation, Pivot);
        }
    }


    [Serializable]
    public class InstructionAdd : InstructionBase
    {
        public override string Action
        {
            get { return "Add"; }
        }

        public SerializableVector3Int Rotation;
        public SerializableVector3Int Position;

        [NonSerialized]
        protected Detail TargetDetail;
        [NonSerialized]
        private const float OffsetLength = 3f;

        public override void Do()
        {

            if (TargetDetail == null)
            {
                var id = TargetDetails.First();

                TargetDetail = AppController.Instance.Session.GetDetail(id);
            }

            TargetDetail.transform.eulerAngles = Rotation;
            TargetDetail.transform.position = Position;

            TargetDetail.gameObject.SetActive(true);
        }

        public override void Undo()
        {

            TargetDetail.gameObject.SetActive(false);
        }

        protected override Sequence GetInstanceAnimation()
        {
            var animation = DOTween.Sequence();

            var targetDetailPos = TargetDetail.transform.position;

            var connectedDetails = TargetDetail.GetLinks().Connections;
            var offsetDirection = connectedDetails.Any() ? Vector3.zero : Vector3.up;
            var hasAxleConnections = false;
            var axleOffsetLength = 0f;

            if (connectedDetails.Any())
            {
                if (TargetDetail.name.Contains("Axle"))
                {
                    var isThisAxleOrBevel = TargetDetail.name.Contains(".") ||
                                            TargetDetail.name.StartsWith("AxleBevelGear");
                    var sumBounds = TargetDetail.Bounds;

                    foreach (var connectedDetail in connectedDetails)
                    {
                        if (!connectedDetail.name.Contains("Axle"))
                        {
                            continue;
                        }

                        var isConnectedAxleOrBevel = connectedDetail.name.Contains(".") ||
                                                     connectedDetail.name.StartsWith("AxleBevelGear");

                        if (isThisAxleOrBevel == isConnectedAxleOrBevel)
                        {
                            continue;
                        }

                        sumBounds.Encapsulate(connectedDetail.Bounds);
                        hasAxleConnections = true;
                    }

                    if (hasAxleConnections)
                    {
                        var axleDirection = TargetDetail.transform.forward.normalized;
                        var directionIndex = Mathf.Abs(Mathf.RoundToInt(axleDirection[0])) == 1
                            ? 0
                            : Mathf.Abs(Mathf.RoundToInt(axleDirection[1])) == 1 ? 1 : 2;
                        var sumBoundsExtentOnDirection = sumBounds.extents[directionIndex];
                        var sumBoundsCenterOnDirection = sumBounds.center[directionIndex];
                        var detailCoordinateOnDirection = targetDetailPos[directionIndex];

                        offsetDirection = Vector3.zero;
                        offsetDirection[directionIndex] = detailCoordinateOnDirection > sumBoundsCenterOnDirection ? 1 : -1;

                        axleOffsetLength = sumBoundsExtentOnDirection - Mathf.Abs(detailCoordinateOnDirection - sumBoundsCenterOnDirection);
                        if (targetDetailPos.y + (offsetDirection * axleOffsetLength).y <= 1)
                        {
                            offsetDirection *= -1;
                            axleOffsetLength = 2 * sumBoundsExtentOnDirection - axleOffsetLength;
                        }
                    }
                }

                if (!hasAxleConnections)
                {
                    var straightDirections = new List<Vector3>();
                    var mixedDirections = new List<Vector3>();

                    foreach (var connectedDetail in connectedDetails)
                    {
                        var connectionDirection = GetConnectionDirection(connectedDetail);

                        if (connectionDirection.sqrMagnitude < 1f)
                        {
                            mixedDirections.Add(connectionDirection);
                        }
                        else
                        {
                            straightDirections.Add(connectionDirection);
                        }

                        //                  Debug.Log(connectionDirection);
                    }

                    foreach (var direction in straightDirections)
                    {
                        if (offsetDirection == Vector3.zero)
                        {
                            offsetDirection = direction;
                            continue;
                        }
                        if (direction == -offsetDirection)
                        {
                            continue;
                        }
                        if (direction != offsetDirection)
                        {
                            offsetDirection += direction;
                            break;
                        }
                    }

                    // mixed directions
                    if (offsetDirection.sqrMagnitude <= 1f && mixedDirections.Any())
                    {
                        var sum = Vector3.zero;

                        foreach (var direction in mixedDirections)
                        {
                            if (offsetDirection != Vector3.zero)
                            {
                                var test = direction - 0.5f * offsetDirection;
                                if (test.sqrMagnitude <= 0.25f)
                                {
                                    continue;
                                }
                            }
                            sum += direction;
                        }

                        var maxValue = 0f;
                        var maxIndex = 0;

                        for (var i = 0; i < 3; i++)
                        {
                            var currentValue = Mathf.Abs(sum[i]);

                            if (currentValue > maxValue)
                            {
                                maxValue = currentValue;
                                maxIndex = i;
                            }
                        }

                        offsetDirection[maxIndex] = Mathf.Sign(sum[maxIndex]);
                    }


                    //              offsetDirection = connectionDirection;
                }
            }

            var offset = offsetDirection.normalized * (OffsetLength + axleOffsetLength);
            var sourcePos = targetDetailPos + offset;
            var resultPos = targetDetailPos;

            AnimationContainer = new GameObject("Container");
            var container = AnimationContainer.transform;

            container.position = TargetDetail.transform.position;

            animation.Append(container.DOMove(sourcePos, BackwardsAnimationTime))
                .Append(container.DOMove(resultPos, 1f))
                .AppendInterval(1f);

            return animation;
        }

        private Vector3 GetConnectionDirection(Detail connectedDetail)
        {
            var targetDetailBounds = TargetDetail.Bounds;
            var connectionPoint = Extentions.Overlap(targetDetailBounds, connectedDetail.Bounds).center;
            var connectionPointLocal = connectionPoint - TargetDetail.transform.position;
            var diff = targetDetailBounds.extents -
                       new Vector3(Mathf.Abs(connectionPointLocal.x),
                           Mathf.Abs(connectionPointLocal.y),
                           Mathf.Abs(connectionPointLocal.z));

            var connectionDirection = Vector3.zero;
            var sum = 0;

            for (var i = 0; i < 3; i++)
            {
                connectionDirection[i] = (diff[i] < 0.5f ? 1 : 0) * Mathf.Sign(connectionPointLocal[i]);
                sum += (int) Mathf.Abs(connectionDirection[i]);
            }

            return connectionDirection / -sum;
        }

        protected override InstructionBase GetInstanceCopy()
        {
            return new InstructionAdd { Rotation = Rotation, Position = Position };
        }

        public override string ToString()
        {
            return string.Format("ADD Position: {0}, Rotation: {1}", Position, Rotation);
        }
    }


    [Serializable]
    public class SceneData
    {
        // 1.1 - добавлена группа вспомогательных деталей (скобочки, рельсы, переходники на Lego и т.п.)
        // 1.2 - добавлены оси, колеса и т.п.
        public string Version = "1.2";
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
                                  Id, Position, Rotation, Type, Connections, Color);
        }
    }
}
