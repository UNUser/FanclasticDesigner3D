using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Assets.Scripts 
{
    public class SelectedDetails : MonoBehaviour
    {
		private readonly HashSet<Detail> _details = new HashSet<Detail>();
	    private Material _axisMaterial;

		public Detail First { get { return _details.FirstOrDefault(); } }
		public int Count { get { return _details.Count; } }

	    void Start()
	    {
		    AppController.Instance.ColorSetter.ActiveColorChangedEvent += SetColor;
			AppController.Instance.ColorSetter.gameObject.SetActive(false);
	    }

	    public bool IsSelected(Detail detail)
	    {
		    return _details.Contains(detail);
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
				? First.GetComponent<Renderer>().material
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
			    AppController.Instance.ColorSetter.ActiveColor = First.GetComponent<Renderer>().material;
		    }
	    }

	    public void Clear()
	    {
			// TODO update links учитывать, что детали могут удаляться
		    foreach (var detail in _details) {
				detail.gameObject.layer = LayerMask.NameToLayer("Default");
		    }
			_details.Clear();
			AppController.Instance.ColorSetter.gameObject.SetActive(false);
		    AppController.Instance.ColorSetter.CurrentColor.enabled = true;
	    }

	    public void Rotate(Vector3 axis)
	    {
		    if (!_details.Any()) {
			    return;
		    }

		    var targetDetail = Detach();

			targetDetail.Rotate(axis);
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

			Clear();
			Destroy(targetDetail.gameObject);
		}

	    private void SetColor(Material material)
	    {
		    foreach (var detail in _details) {
			    detail.GetComponent<Renderer>().material = material;
		    }
		    AppController.Instance.ColorSetter.CurrentColor.enabled = _details.Count == 1;
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
