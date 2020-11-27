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

        private Bounds GetMeshBounds(out Transform parent)
        {
            var first = First;

            if (_details.Count == 1)
            {
                parent = first.transform;
                return first.GetComponent<MeshFilter>().mesh.bounds;
            }

            var firstGroup = first.Group;
            var combinedBounds = first.MeshBounds;

            parent = firstGroup == null ? first.transform : firstGroup.transform;

            foreach (var child in _details) {
                combinedBounds.Encapsulate(child.MeshBounds);
            }

            return combinedBounds;
        }

        public void Add(HashSet<Detail> selection)
        {
            foreach (var detail in selection)
            {
                Add(detail);
            }
        }

        public void Add(DetailBase detailBase)
        {
            var detailsGroup = detailBase as DetailsGroup;
            var fixedColor = true;

            if (detailsGroup != null)
            {
                foreach (var detail in detailsGroup.Details)
                {
                    detail.gameObject.layer = LayerMask.NameToLayer("SelectedItem");
                    if (detail.Linkage != null)
                    {
                        detail.Linkage.layer = LayerMask.NameToLayer("SelectedLinkage");
                    }
                    if (detail.AxleLinkage != null)
                    {
                        var layerName = LayerMask.LayerToName(detail.AxleLinkage.layer).Contains("Hub")
                            ? "AxleHubSelectedLinkage"
                            : "AxleSelectedLinkage";

                        detail.AxleLinkage.layer = LayerMask.NameToLayer(layerName);
                    }
                    if (detail.AxleSpaceBounds != null)
                    {
                        var layerName = LayerMask.LayerToName(detail.AxleSpaceBounds.gameObject.layer).Contains("Hub")
                            ? "AxleHubSelectedItem"
                            : "AxleSelectedItem";

                        detail.AxleSpaceBounds.gameObject.layer = LayerMask.NameToLayer(layerName);
                    }

                    if (!detail.FixedColor)
                    {
                        fixedColor = false;
                    }
                }
                _details.UnionWith(detailsGroup.Details);
            }
            else
            {
                var detail = (Detail) detailBase;

                _details.Add(detail);
                detail.gameObject.layer = LayerMask.NameToLayer("SelectedItem");
                if (detail.Linkage != null)
                {
                    detail.Linkage.layer = LayerMask.NameToLayer("SelectedLinkage");
                }
                if (detail.AxleLinkage != null)
                {
                    var layerName = LayerMask.LayerToName(detail.AxleLinkage.layer).Contains("Hub")
                        ? "AxleHubSelectedLinkage"
                        : "AxleSelectedLinkage";

                    detail.AxleLinkage.layer = LayerMask.NameToLayer(layerName);
                }
                if (detail.AxleSpaceBounds != null)
                {
                    var layerName = LayerMask.LayerToName(detail.AxleSpaceBounds.gameObject.layer).Contains("Hub")
                        ? "AxleHubSelectedItem"
                        : "AxleSelectedItem";

                    detail.AxleSpaceBounds.gameObject.layer = LayerMask.NameToLayer(layerName);
                }

                fixedColor = detail.FixedColor;
            }

            var colorSetter = AppController.Instance.ColorSetter;

            if (fixedColor)
            {
                colorSetter.gameObject.SetActive(false);
                return;
            }

            colorSetter.gameObject.SetActive(true);
            colorSetter.CurrentColor.enabled = _details.Count == 1;
            colorSetter.ActiveColor = _details.Count == 1
                ? First.Color
                : null;

        }

        //TODO этот код нигде не используется
        //      public void Remove(Detail detail)
        //      {
        //          _details.Remove(detail);
        //          detail.gameObject.layer = LayerMask.NameToLayer("Item");
        //          detail.Linkage.layer = LayerMask.NameToLayer("Linkage");
        //
        //          if (!_details.Any()) {
        //              AppController.Instance.ColorSetter.gameObject.SetActive(false);
        //          }
        //          if (_details.Count == 1) {
        //              AppController.Instance.ColorSetter.CurrentColor.enabled = true;
        //              AppController.Instance.ColorSetter.ActiveColor.Material = First.GetComponent<Renderer>().material;
        //          }
        //      }

        public void Clear()
        {
            // TODO update links учитывать, что детали могут удаляться
            foreach (var detail in _details)
            {
                detail.gameObject.layer = LayerMask.NameToLayer("Item");
                if (detail.Linkage != null)
                {
                    detail.Linkage.layer = LayerMask.NameToLayer("Linkage");
                }
                if (detail.AxleLinkage != null)
                {
                    var layerName = LayerMask.LayerToName(detail.AxleLinkage.layer).Contains("Hub")
                        ? "AxleHubLinkage"
                        : "AxleLinkage";

                    detail.AxleLinkage.layer = LayerMask.NameToLayer(layerName);
                }
                if (detail.AxleSpaceBounds != null)
                {
                    var layerName = LayerMask.LayerToName(detail.AxleSpaceBounds.gameObject.layer).Contains("Hub")
                        ? "AxleHubItem"
                        : "AxleItem";

                    detail.AxleSpaceBounds.gameObject.layer = LayerMask.NameToLayer(layerName);
                }
            }
            _details.Clear();
            IsValid = true;
            AppController.Instance.ColorSetter.gameObject.SetActive(false);
            AppController.Instance.ColorSetter.CurrentColor.enabled = true;
        }

        public void AppRotate(Vector3 axisLocal)
        {
            if (!_details.Any()) {
                return;
            }

            var targetDetail = Detach();
            var boundingBoxLocal = targetDetail.MeshBounds;

            var pivot = targetDetail.transform.TransformPoint(boundingBoxLocal.center);
            var axis = targetDetail.transform.TransformVector(axisLocal);
            var rotationDelta = Quaternion.AngleAxis(90, axis);

            targetDetail.transform.Rotate(pivot, rotationDelta);

            var bottomDetail = GetBottomDetail();
            var floorOffset = bottomDetail.GetBottomPointFloorOffset();
            var floorAlignment = Vector3.up * (floorOffset < 0 ? Mathf.Abs(floorOffset) : 0);
            var alignmentPoint = bottomDetail.transform.TransformPoint(bottomDetail.AlignmentPoint) + floorAlignment;
            var latticeAlignment = AppController.Instance.WorkspaceLattice.GetCrossPointAlignmentOffset(alignmentPoint, floorOffset <= 0);
            var resultAlignment = floorAlignment + latticeAlignment;

            targetDetail.transform.Translate(resultAlignment, Space.World);
            targetDetail.UpdateLinks();

            AppController.Instance.ActionsLog.RegisterAction(new RotateAction(rotationDelta, pivot, resultAlignment));
        }

        public Detail GetBottomDetail()
        {

            var targetGroup = Detach();
            var boundingBox = targetGroup.Bounds;

            boundingBox.center += Vector3.down * (boundingBox.extents.y);
            boundingBox.extents += Vector3.down * (boundingBox.extents.y - 0.25f);

            var details = Physics.OverlapBox(boundingBox.center, boundingBox.extents);

            if (details == null)
            {
                Debug.LogError("Wrong bounds in details group!");
                return null;
            }

            foreach (var detail in details)
            {

                if (detail.transform.root != targetGroup.transform) continue;

                var result = detail.GetComponent<Detail>();

                if (result == null) continue;

                return result;
            }

            Debug.LogError("Wrong bounds in details group!");
            return null;
        }




        public LinksBase GetLinks(Vector3 offset, Quaternion rotationDelta, Vector3 pivot, LinksMode linksMode)
        {
            if (_details.Count < 2)
            {
                return _details.Any()
                    ? _details.First().GetLinks(offset, rotationDelta, pivot, linksMode)
                    : null;
            }

            var groupLinks = new DetailsGroupLinks(linksMode);

            foreach (var detail in _details)
            {
                var detailLinks = (DetailLinks) detail.GetLinks(offset, rotationDelta, pivot, linksMode);

                //              Debug.Log(detailLinks.Data + " " + linksMode + " " + offset);

                if (!detailLinks.IsValid)
                {
                    groupLinks.IsValid = false;
                    break;
                }

                if (detailLinks.HasConnections)
                {
                    groupLinks += detailLinks;
                }
            }

            return groupLinks;
        }

        public void SetRaycastOrigins(Vector3 offset)
        {
            foreach (var detail in _details)
            {
                detail.SetRaycastOrigins(offset);
            }
        }

        public RaycastHit? CastDetailAndGetClosestHit(Vector3 direction, out Detail raycaster, out int rayOriginIndex)
        {
            RaycastHit? minRayHitInfo = null;

            rayOriginIndex = -1;
            raycaster = null;

            foreach (var detail in _details)
            {
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
            if (!_details.Any())
            {
                return null;
            }

            var first = _details.First();
            var targetGroup = first.Group;

            if (targetGroup == null)
            {
                return first;
            }

            return targetGroup.Detach(_details);
        }

        public void Delete()
        {
            if (!_details.Any())
            {
                return;
            }

            var targetDetail = Detach();

            AppController.Instance.ActionsLog.RegisterAction(new DeleteAction(targetDetail));
            Hide();
            Clear();
            //          Destroy(targetDetail.gameObject);
        }

        public void Move(Vector3 offset)
        {
            var targetDetail = Detach();

            targetDetail.transform.Translate(offset, Space.World);
        }

        public Quaternion Rotation
        {
            get
            {
                var targetDetail = Detach();

                return targetDetail.transform.rotation;
            }
            set
            {
                var targetDetail = Detach();

                targetDetail.transform.rotation = value;
            }
        }



        public void SetColor(DetailColor color)
        {
            AppController.Instance.ActionsLog.RegisterAction(new SetColorAction(Selected, color));

            foreach (var detail in _details)
            {
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

        private void CreateLineMaterial()
        {
            if (!_axisMaterial)
            {
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

        private void OnRenderObject()
        {

            if (!_details.Any()) return;

            Transform parentTransform;
//            var dummy = Vector3.zero;
            var combinedBounds = GetMeshBounds(out parentTransform);
//            var rotation = Detail.GetAligningRotation(Quaternion.identity, parentTransform.rotation, out dummy);

//            foreach (var child in _details)
//            {
//                combinedBounds.Encapsulate(child.MeshBounds);
//            }

            var axisColors = new[] { Color.red, Color.green, Color.blue };
            var protrusion = 2f;

            CreateLineMaterial();
            _axisMaterial.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.LINES);

            for (var i = 0; i < 3; i++)
            {
                var offset = (combinedBounds.extents[i] + protrusion) * new Vector3(i == 0 ? 1 : 0, i == 1 ? 1 : 0, i == 2 ? 1 : 0);
                var point1 = new Vector3(combinedBounds.center.x + offset.x,
                                         combinedBounds.center.y + offset.y,
                                         combinedBounds.center.z + offset.z);
                var point2 = new Vector3(combinedBounds.center.x - offset.x,
                                         combinedBounds.center.y - offset.y,
                                         combinedBounds.center.z - offset.z);
                //TODO Важно чтобы ориентация осей, которую мы показываем тут, соответствовала ориентации после Detach(). Касается случаев, когда в
                //TODO группе выделена одна деталь. Сейчас это решается костылями в GetMeshBounds, надо будет переделать.
//                var transformedPoint1 = parentTransform.TransformPoint(Extentions.RotatePoint(point1, combinedBounds.center, rotation));
//                var transformedPoint2 = parentTransform.TransformPoint(Extentions.RotatePoint(point2, combinedBounds.center, rotation));
                var transformedPoint1 = parentTransform.TransformPoint(point1);
                var transformedPoint2 = parentTransform.TransformPoint(point2);

                GL.Color(axisColors[i]);

                GL.Vertex3(transformedPoint1.x, transformedPoint1.y, transformedPoint1.z);
                GL.Vertex3(transformedPoint2.x, transformedPoint2.y, transformedPoint2.z);
            }

            GL.End();
            GL.PopMatrix();
        }
    }
}
