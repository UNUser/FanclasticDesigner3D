using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Assets.Scripts 
{
    public class SelectedDetails : MonoBehaviour
    {
		private readonly HashSet<Detail> _details = new HashSet<Detail>();
	    private Material _axisMaterial;

		public HashSet<Detail> Selected { get { return new HashSet<Detail>(_details); } } 
		public Detail First { get { return _details.FirstOrDefault(); } }
		public int Count { get { return _details.Count; } }

	    void Awake()
	    {
		    AppController.Instance.ColorSetter.ActiveColorChangedEvent += SetColor;
			AppController.Instance.ColorSetter.gameObject.SetActive(false);
		    
		    IsValid = true;
	    }

	    public bool IsSelected(Detail detail)
	    {
		    return _details.Contains(detail);
	    }

	    public bool IsValid { get; set; }

	    public void Add(HashSet<Detail> selection)
	    {
		    foreach (var detail in selection) {
			    Add(detail);
		    }
	    }

	    public void Add(DetailBase detailBase)
	    {
		    var detailsGroup = detailBase as DetailsGroup;

		    if (detailsGroup != null) {
			    foreach (var detail in detailsGroup.Details) {
					detail.gameObject.layer = LayerMask.NameToLayer("SelectedItem");
			    }
			    _details.UnionWith(detailsGroup.Details);
		    } else {
				var detail = (Detail) detailBase;

				_details.Add(detail);
				detail.gameObject.layer = LayerMask.NameToLayer("SelectedItem");
		    }

			var colorSetter = AppController.Instance.ColorSetter;

			colorSetter.gameObject.SetActive(true);
		    colorSetter.CurrentColor.enabled = _details.Count == 1;
			colorSetter.ActiveColor = _details.Count == 1
				? First.Color
				: null;

	    }

	    public void Remove(Detail detail)
	    {
		    _details.Remove(detail);
			detail.gameObject.layer = LayerMask.NameToLayer("Default");

		    if (!_details.Any()) {
				AppController.Instance.ColorSetter.gameObject.SetActive(false);
		    }
		    if (_details.Count == 1) {
			    AppController.Instance.ColorSetter.CurrentColor.enabled = true;
			    AppController.Instance.ColorSetter.ActiveColor.Material = First.GetComponent<Renderer>().material;
		    }
	    }

	    public void Clear()
	    {
			// TODO update links учитывать, что детали могут удаляться
		    foreach (var detail in _details) {
				detail.gameObject.layer = LayerMask.NameToLayer("Default");
		    }
			_details.Clear();
		    IsValid = true;
			AppController.Instance.ColorSetter.gameObject.SetActive(false);
		    AppController.Instance.ColorSetter.CurrentColor.enabled = true;
	    }

	    public void AppRotate(Vector3 axis)
	    {
		    Vector3 pivot;
		    Vector3 alignment;

			Rotate(axis, true, out pivot, out alignment);
			AppController.Instance.ActionsLog.RegisterAction(new RotateAction(axis, pivot, alignment));
	    }


	    public void Rotate(Vector3 axis, bool clockwise, Vector3 pivot, Vector3 alignment)
	    {
			var targetDetail = Detach();
			var angle = clockwise ? 90 : -90;

		    if (clockwise) {
			    targetDetail.transform.RotateAround(pivot, axis, angle);
				targetDetail.transform.Translate(alignment, Space.World);
		    } else {
				targetDetail.transform.Translate(-alignment, Space.World);
				targetDetail.transform.RotateAround(pivot, axis, angle);
		    }
	    }

		public void Rotate(Vector3 axis, bool clockwise = true)
		{
			var dummy = new Vector3();

			Rotate(axis, clockwise, out dummy, out dummy);
		}

	    public void Rotate(Vector3 axis, bool clockwise, out Vector3 pivot, out Vector3 alignment)
	    {
		    if (!_details.Any()) {
				pivot = Vector3.zero;
				alignment = Vector3.zero;

			    return;
		    }

			var targetDetail = Detach();
			var boundingBox = targetDetail.Bounds;
			var angle = clockwise ? 90 : -90;

			pivot = boundingBox.center;
			targetDetail.transform.RotateAround(pivot, axis, angle);

			var bottomDetail = GetBottomDetail();
			var newPos = bottomDetail.transform.position;

			bottomDetail.AlignPosition(ref newPos);

			alignment = newPos - bottomDetail.transform.position;
			targetDetail.transform.Translate(alignment, Space.World);

		    var links = targetDetail.GetLinks();
			IsValid = links.IsValid;

			targetDetail.UpdateLinks(LinksMode.ExceptSelected, links);
	    }


		private Detail GetBottomDetail() {

			var targetGroup = Detach();
			var boundingBox = targetGroup.Bounds;

			boundingBox.center += Vector3.down * (boundingBox.extents.y);
			boundingBox.extents += Vector3.down * (boundingBox.extents.y - 0.25f);

			var details = Physics.OverlapBox(boundingBox.center, boundingBox.extents);

			if (details == null) {
				Debug.LogError("Wrong bounds in details group!");
				return null;
			}

			foreach (var detail in details) {

				if (detail.transform.root != targetGroup.transform) continue;

				return detail.GetComponent<Detail>();
			}

			Debug.LogError("Wrong bounds in details group!");
			return null;
		}




	    public LinksBase GetLinks(LinksMode linksMode, Vector3? offset)
	    {
		    if (_details.Count < 2)
		    {
			    return _details.Any()
					? _details.First().GetLinks(linksMode, offset) 
					: null;
		    }

			var groupLinks = new DetailsGroupLinks(linksMode);

			foreach (var detail in _details) {
				var detailLinks = (DetailLinks) detail.GetLinks(linksMode, offset);

//				Debug.Log(detailLinks.Data + " " + linksMode + " " + offset);

				if (!detailLinks.IsValid) {
					groupLinks.IsValid = false;
					break;
				}

				if (detailLinks.HasConnections) {
					groupLinks += detailLinks;
				}
			}

			return groupLinks;
	    }

	    public void SetRaycastOrigins(Vector3 offset)
	    {
			foreach (var detail in _details) {
				detail.SetRaycastOrigins(offset);
			}
	    }

	    public RaycastHit? CastDetailAndGetClosestHit(Vector3 direction, out Detail raycaster, out int rayOriginIndex)
	    {
			RaycastHit? minRayHitInfo = null;

			rayOriginIndex = -1;
			raycaster = null;

			foreach (var detail in _details) {
				Detail childRaycaster;
				int childRayOriginIndex;

				var childHitInfo = detail.CastDetailAndGetClosestHit(direction, out childRaycaster, out childRayOriginIndex);

				if (childHitInfo == null) continue;
				if (minRayHitInfo != null && minRayHitInfo.Value.distance <= childHitInfo.Value.distance) continue;

				minRayHitInfo = childHitInfo;
				raycaster = childRaycaster;
				rayOriginIndex = childRayOriginIndex;
			}

			return minRayHitInfo;
	    }


		// отсоединяться может только связанная группа деталей
	    public DetailBase Detach()
	    {
			if (!_details.Any()) {
				return null;
			}

		    var first = _details.First();
			var targetGroup = first.Group;

		    if (targetGroup == null) {
			    return first;
		    }

			return targetGroup.Detach(_details);
	    }

		public void Delete() 
		{
			if (!_details.Any()) {
				return;
			}

			var targetDetail = Detach();

			AppController.Instance.ActionsLog.RegisterAction(new DeleteAction(targetDetail));
			Hide();
			Clear();
//			Destroy(targetDetail.gameObject);
		}

	    public void Move(Vector3 offset)
	    {
		    var targetDetail = Detach();

			targetDetail.transform.Translate(offset, Space.World);
	    }

	    public void SetColor(DetailColor color)
	    {
			AppController.Instance.ActionsLog.RegisterAction(new SetColorAction(Selected, color));

		    foreach (var detail in _details) {
			    detail.Color = color;
		    }
		    AppController.Instance.ColorSetter.CurrentColor.enabled = _details.Count == 1;
	    }

	    public void Hide()
	    {
			var targetDetail = Detach();

			targetDetail.gameObject.SetActive(false);
	    }

	    public void Show()
	    {
			var targetDetail = Detach();

			targetDetail.gameObject.SetActive(true);
			targetDetail.UpdateLinks();
	    }

		private void CreateLineMaterial() {
			if (!_axisMaterial) {
				// Unity has a built-in shader that is useful for drawing
				// simple colored things.
				var shader = Shader.Find("Hidden/Internal-Colored");
				_axisMaterial = new Material(shader);
				_axisMaterial.hideFlags = HideFlags.HideAndDontSave;
				// Turn on alpha blending
				_axisMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
				_axisMaterial.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				// Turn backface culling off
				_axisMaterial.SetInt("_Cull", (int) UnityEngine.Rendering.CullMode.Off);
				// Turn off depth writes
				_axisMaterial.SetInt("_ZWrite", 0);
			}
		}

		private void OnRenderObject() {

			if (!_details.Any()) return;

            var combinedBounds = new Bounds();
            combinedBounds.center = First.transform.position;
            foreach (var child in _details) {
                combinedBounds.Encapsulate(child.Bounds);
            }

			var axisColors = new [] {Color.red, Color.green, Color.blue};
			var protrusion = 2f;

			CreateLineMaterial();
			_axisMaterial.SetPass(0);

			GL.PushMatrix();
			GL.Begin(GL.LINES);
			
			for (var i = 0; i < 3; i++)
			{
				var offset = (combinedBounds.extents[i] + protrusion) * new Vector3(i == 0 ? 1 : 0, i == 1 ? 1 : 0, i == 2 ? 1 : 0);

				GL.Color(axisColors[i]);
				GL.Vertex3(combinedBounds.center.x + offset.x, combinedBounds.center.y + offset.y, combinedBounds.center.z + offset.z);
				offset *= -1;
				GL.Vertex3(combinedBounds.center.x + offset.x, combinedBounds.center.y + offset.y, combinedBounds.center.z + offset.z);
			}

			GL.End();
			GL.PopMatrix();
		}
    }
}
