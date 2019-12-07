using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts
{
    /// <summary>
    ///
    ///   Z
    ///   ^
    ///   |
    ///   |   + + +
    ///   |    o o
    ///   |   + + +
    ///   +-------------- > X
    ///
    ///
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Detail : DetailBase, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        public BoxCollider SpaceBounds;
        public BoxCollider AxleSpaceBounds;
        public GameObject Linkage;
        public GameObject AxleLinkage;
        public Vector3 AlignmentPoint;
        public bool FixedColor;
        public bool IsAxle;

        public override bool IsSelected
        {
            get { return AppController.Instance.SelectedDetails.IsSelected(this); }
        }

        public override Bounds Bounds
        {
            get { return GetComponent<Renderer>().bounds; }
        }

        public override Bounds MeshBounds
        {
            get
            {
                var meshBounds = GetComponent<MeshFilter>().mesh.bounds;
                var group = Group;

                if (group == null)
                {
                    return meshBounds;
                }

                var transformedMax = group.transform.InverseTransformPoint(transform.TransformPoint(meshBounds.max));
                var transformedMin = group.transform.InverseTransformPoint(transform.TransformPoint(meshBounds.min));
                var resultBounds = new Bounds();

                resultBounds.SetMinMax(transformedMin, transformedMax);

                return resultBounds;
            }
        }

        public DetailsGroup Group
        {
            get
            {
                var parent = transform.parent;
                return parent == null ? null : parent.GetComponent<DetailsGroup>();
            }
        }

        public override Quaternion Orientation
        {
            get { return Group == null ? transform.rotation : Group.Orientation; }
        }

        public DetailColor Color
        {
            get { return _color; }
            set
            {
                if (FixedColor)
                {
                    return;
                }
                _color = value;
                GetComponent<Renderer>().material = _color.Material;
            }
        }
        private DetailColor _color;

        public DetailData Data
        {
            get
            {
                var materialName = GetComponent<Renderer>().material.name;
                var data = new DetailData
                {
                    Connections = Links.Data.Connections,
                    Id = GetInstanceID(),
                    Detail = this,
                    Position = transform.position,
                    Rotation = transform.rotation.eulerAngles,
                    Type = name.Remove(name.Length - "(Clone)".Length),
                    Color = FixedColor ? "None" : materialName.Remove(materialName.Length - " (Instance)".Length)
                };

                return data;
            }
        }

        public LinksBase Links { get { return _links; } }
        public HashSet<Detail> Connections { get { return _links.Connections; } }

        private int _sizeX;
        private int _sizeZ;
        private Vector3[] _raysOrigins;
        private Vector3[] _connectorsLocalPos;
        private BoxCollider[] _linkageColliders;
        private BoxCollider[] _axisLinkageColliders;
        private int _holdingConnector;
        private Vector3 _prevPointerPos;
        private bool _isClick;
        private bool _isLongClick;
        private Vector3 _sourcePosition;
        private Quaternion _sourceRotation;

        private static Material _selectedMaterial;

        private DetailLinks _links;

        private void Awake()
        {
            _links = new DetailLinks(this);

            // Определяем по размерам коллайдера тип детали и локальные координаты всех ее коннекторов
            var colliderSize = GetComponent<Collider>().bounds.size;
            var swapZY = colliderSize.y > colliderSize.z;

            var xInt = (int) colliderSize.x;
            var zInt = (int) (swapZY ? colliderSize.y : colliderSize.z);

            _sizeX = xInt.IsOdd() ? xInt : (xInt - 1);
            _sizeZ = zInt.IsOdd() ? zInt : (zInt - 1);

            var totalConnectors = _sizeX * _sizeZ;

            _raysOrigins = new Vector3[totalConnectors];
            _connectorsLocalPos = new Vector3[totalConnectors];

            for (var i = 0; i < _connectorsLocalPos.Length; i++)
            {
                var valueZ = -i / _sizeX + _sizeZ / 2;

                _connectorsLocalPos[i] = new Vector3(i % _sizeX - _sizeX / 2,
                                                    swapZY ? valueZ : 0,
                                                    swapZY ? 0 : valueZ);

            }

            if (Linkage != null)
            {
                _linkageColliders = Linkage.GetComponents<BoxCollider>();
            }
            if (AxleLinkage != null)
            {
                _axisLinkageColliders = AxleLinkage.GetComponents<BoxCollider>();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isLongClick = _isClick = false;

            if (!IsSelected) return;

#if !UNITY_STANDALONE && !UNITY_EDITOR

            if (Input.touchCount > 1) return;
#endif

            // Находим коннектор, за которой "держим" деталь:
            var pointerRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;

            // 1. Бросаем луч в то место, на которое нажал пользователь
            if (!Physics.Raycast(pointerRay, out hitInfo))
            {
                Debug.LogError("OnBeginDrag without raycast hit!");
                return;
            }

            // 2. Переводим точку попадания луча в нашу деталь в локальные координаты детали
            //    и ищем перебором ближайший к этой точке коннектор
            var localHitPoint = transform.InverseTransformPoint(hitInfo.point);
            var minDistance = float.MaxValue;

            for (var i = 0; i < _connectorsLocalPos.Length; i++)
            {
                var distance = Vector3.Distance(_connectorsLocalPos[i], localHitPoint);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    _holdingConnector = i;
                }
            }

            _prevPointerPos = Input.mousePosition;

            // Затем пересчитываем положения начала лучей, которые будем бросать от камеры через коннекторы детали
            // (при этом луч, исходящий из камеры должен попадать в тот коннектор, за который мы "держим" деталь)
            var cameraOffset = Camera.main.transform.position - transform.position;
            var holdingConnectorOffset = cameraOffset - transform.TransformDirection(_connectorsLocalPos[_holdingConnector]);

            AppController.Instance.SelectedDetails.SetRaycastOrigins(holdingConnectorOffset);

            _sourceRotation = transform.rotation;

            if (eventData != null)
            {
                _sourcePosition = transform.position;
            }

            //          AppController.Instance.SelectedDetails.Detach();
        }


        public void SetRaycastOrigins(Vector3 offset)
        {
            for (var i = 0; i < _raysOrigins.Length; i++)
            {
                _raysOrigins[i] = transform.TransformPoint(_connectorsLocalPos[i]) + offset;
            }
        }


        // TODO Кучу кода можно по идее заменить используя Physics.BoxCast!!!! (только не кидает ли он еще больше лучей?)
        public void OnDrag(PointerEventData eventData)
        {
            if (!IsSelected) return;

#if !UNITY_STANDALONE && !UNITY_EDITOR

            if (Input.touchCount > 1) return;
#endif

            var newPointerPos = Input.mousePosition;

            if (_prevPointerPos == newPointerPos)
                return;

            _prevPointerPos = newPointerPos;

            // Определяем новое направление, которое указывает пользователь
            var newDirection = Camera.main.ScreenPointToRay(newPointerPos).direction;
            Detail raycaster;
            int rayOriginIndex;

            var minRayHitInfo = AppController.Instance.SelectedDetails
                .CastDetailAndGetClosestHit(newDirection, out raycaster, out rayOriginIndex);

            // Этот луч указывает новое положение соответствующего коннектора
            if (minRayHitInfo == null)
                return;

            // Округляем координаты точки, вычисляем вектор переноса и перемещаем деталь
            var hitPoint = minRayHitInfo.Value.point;
            var hitConnectorCurrentPos = raycaster.transform.TransformPoint(raycaster._connectorsLocalPos[rayOriginIndex]);;

//                        _debugRay = new Ray(raycaster._raysOrigins[rayOriginIndex], hitPoint);

            var hitLattice = minRayHitInfo.Value.collider.GetComponent<Lattice>();
            var rotationDelta = raycaster.GetAligningRotation(hitLattice, raycaster._sourceRotation);

            var hitConnectorRotatedPos = Extentions.RotatePoint(hitConnectorCurrentPos, hitPoint, rotationDelta);

            var rotatedPosOffset = hitPoint - hitConnectorRotatedPos;

            var raycasterCurrentPos = raycaster.transform.position;
            var raycasterNewPos = Extentions.RotateAndTranslatePoint(raycasterCurrentPos, hitPoint, rotationDelta, rotatedPosOffset);

            var raycasterAlignmentPointNewPos = raycaster.transform.TransformPoint(raycaster.AlignmentPoint, hitPoint, rotationDelta, rotatedPosOffset);
            var alignmentOffset = hitLattice.GetCrossPointAlignmentOffset(raycasterAlignmentPointNewPos);

            raycasterNewPos += alignmentOffset;

            if (raycasterNewPos == raycaster.transform.position) return;  //Debug.Log("Old pos: " + transform.position + ", new pos: " + newPos);

            var offset = rotatedPosOffset + alignmentOffset;

//            Debug.Log(raycaster._sourceRotation.eulerAngles + " " + hitLattice.transform.rotation + " " + rotationDelta.eulerAngles);


//            Debug.Log(hitPoint
//                + " " + raycaster.transform.position
//                + " " + newPos
//                + " " + offset);

            var links = AppController.Instance.SelectedDetails.GetLinks(offset, rotationDelta, hitPoint, LinksMode.ExceptSelected);

            Debug.Log(links.IsValid + /*" " + (hitPoint.y > 0) +*/ " " + links.HasConnections);

//            var detached1 = AppController.Instance.SelectedDetails.Detach();
//            detached1.transform.rotation = rotationDelta * detached1.transform.rotation;
//            detached1.transform.position = Extentions.RotateAndTranslatePoint(detached1.transform.position, hitPoint, rotationDelta, offset);
//            return;

            if (!links.IsValid || (hitPoint.y > 0 && !links.HasConnections))
            {
                return;
            }

            var detached = AppController.Instance.SelectedDetails.Detach();

            AppController.Instance.SelectedDetails.IsValid = links.IsValid;
            detached.transform.RotateAndTranslate(hitPoint, rotationDelta, offset);
            detached.UpdateLinks(links.LinksMode, links); ///// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsSelected) return;

            var offset = transform.position - _sourcePosition;

            if (offset != Vector3.zero)
            {
                AppController.Instance.ActionsLog.RegisterAction(new MoveAction(offset));
            }
        }

        public RaycastHit? CastDetailAndGetClosestHit(Vector3 direction, out Detail raycaster, out int rayOriginIndex)
        {
            // Кидаем лучи, идущие  в новом направлении параллельно через все коннекторы детали и определяем самый короткий из них
            RaycastHit? minRayHitInfo = null;
            LayerMask layerMask = 1 << LayerMask.NameToLayer("Item") |
                                  1 << LayerMask.NameToLayer("Workspace");

            raycaster = this;
            rayOriginIndex = -1;

            for (var i = 0; i < _raysOrigins.Length; i++)
            {
                RaycastHit hitInfo;

                if (Physics.Raycast(_raysOrigins[i], direction, out hitInfo, Mathf.Infinity, layerMask))
                {
                    if (minRayHitInfo == null || hitInfo.distance < minRayHitInfo.Value.distance)
                    {
                        minRayHitInfo = hitInfo;
                        rayOriginIndex = i;
                    }
                }
            }

            if (minRayHitInfo == null)
            {
                return null;
            }

            var isFloorHit = Mathf.RoundToInt(minRayHitInfo.Value.point.y) == 0;

            if (isFloorHit || AxleSpaceBounds == null)
            {
                return minRayHitInfo;
            }

            LayerMask axisLayerMask = LayerMask.LayerToName(AxleSpaceBounds.gameObject.layer).Contains("Hub")
                                            ? 1 << LayerMask.NameToLayer("AxleItem")
                                            : 1 << LayerMask.NameToLayer("AxleHubItem");

            if (name.Contains("Wheel"))
            {
                RaycastHit axisHitInfo;
                var centerIndex = (_raysOrigins.Length - 1) / 2;

                if (Physics.Raycast(_raysOrigins[centerIndex], direction, out axisHitInfo, Mathf.Infinity, axisLayerMask))
                {
                    minRayHitInfo = axisHitInfo;
                    rayOriginIndex = centerIndex;

                    return minRayHitInfo;
                }
            }

            if (Linkage != null)
            {
                RaycastHit axleHitInfo;
                var maxDistance = minRayHitInfo.Value.distance + 0.5f;

                if (Physics.Raycast(_raysOrigins[rayOriginIndex], direction, out axleHitInfo, maxDistance, axisLayerMask))
                {
                    return axleHitInfo;
                }

                return minRayHitInfo;
            }

            rayOriginIndex = -1;
            minRayHitInfo = null;

            for (var i = 0; i < _raysOrigins.Length; i++)
            {
                RaycastHit axisHitInfo;

                if (Physics.Raycast(_raysOrigins[i], direction, out axisHitInfo, Mathf.Infinity, axisLayerMask))
                {
                    if (minRayHitInfo == null || axisHitInfo.distance < minRayHitInfo.Value.distance)
                    {
                        minRayHitInfo = axisHitInfo;
                        rayOriginIndex = i;
                    }
                }
            }

            return minRayHitInfo;
        }

        private Quaternion GetAligningRotation(Lattice lattice, Quaternion sourceRotation)
        {
            var dummy = Vector3.zero;
            var fromSourceToTarget = GetAligningRotation(sourceRotation, lattice.transform.rotation, out dummy);
            var fromCurrentToTarget = fromSourceToTarget * sourceRotation * Quaternion.Inverse(transform.rotation);

            return fromCurrentToTarget;
        }

        public static Quaternion GetAligningRotation(Quaternion from, Quaternion to, out Vector3 diff)
        {
            var toDirections = new HashSet<Vector3> { to * Vector3.right,
                                                      to * Vector3.up,
                                                      to * Vector3.forward };
            var currentFrom = from;
            var resultRotation = Quaternion.identity;

            diff = Vector3.zero;

            for (var j = 0; j < 3; j++)
            {
                var i = (j + 2) % 3;
                var fromDirection = currentFrom * (new Vector3(i == 0 ? 1 : 0,
                                                               i == 1 ? 1 : 0,
                                                               i == 2 ? 1 : 0));
                var minAngle = float.PositiveInfinity;
                var minAngleOriginDirection = Vector3.zero;
                var targetDirection = Vector3.zero;

                foreach (var toDirection in toDirections)
                {
                    var possibleTargetDirection = toDirection;
                    var angle = Vector3.Angle(fromDirection, possibleTargetDirection);

                    if (angle > 90)
                    {
                        angle = 180 - angle;
                        possibleTargetDirection = - toDirection;
                    }
//                    Debug.Log("!!! " + angle + " " + fromDirection + " " + possibleTargetDirection);
                    if (angle >= minAngle)
                    {
                        continue;
                    }

                    minAngle = angle;
                    minAngleOriginDirection = toDirection;
                    targetDirection = possibleTargetDirection;
                }

                toDirections.Remove(minAngleOriginDirection);

                resultRotation = Quaternion.FromToRotation(fromDirection, targetDirection) * resultRotation;
                currentFrom = resultRotation * from;

                diff[i] = minAngle;

//                Debug.Log("---" + resultRotation + " " + from + " " + currentFrom);

//                Debug.Log(i
//                        + ") target: " + to.eulerAngles
//                        + ", source: " + from.eulerAngles
//                        + ", min: " + minAngle
//                        + ", direction: " + fromDirection
//                        + ", targetDirection: " + targetDirection
//                        + ", currentFrom: " + currentFrom.eulerAngles
//                        + ", deltaRotation: " + resultRotation.eulerAngles
//                        + ", resultRotation: " + (resultRotation * from).eulerAngles);
            }

            return resultRotation;
        }

        public Vector3 GetFloorAlignment()
        {
            var heightFirst = transform.TransformPoint(_connectorsLocalPos[0]).y;
            var heightLast = transform.TransformPoint(_connectorsLocalPos[_connectorsLocalPos.Length - 1]).y;
            var heightMin = Mathf.Min(heightFirst, heightLast);
            var length = heightMin < 0 ? Mathf.Abs(heightMin) : 0;

            return Vector3.up * length;
        }

        // Update is called once per frame
//        private Ray _debugRay;
//        private void Update()
//        {
//            for (var i = 0; i < _raysOrigins.Length; i++) {
//                Debug.DrawRay(_raysOrigins[i], transform.TransformPoint(_connectorsLocalPos[i]) - _raysOrigins[i], UnityEngine.Color.red, 0.1f);
//            }
//            Debug.DrawRay(transform.TransformPoint(-1, 1, 0), Vector3.up * 10, UnityEngine.Color.red, 1);
//            if (IsSelected)
//            {
//                Debug.Log("valid " + _links.IsValid + " connections " + _links.HasConnections);
//            }
//            Debug.DrawRay(_debugRay.origin, _debugRay.direction, UnityEngine.Color.red, 0.1f);
//        }

        public void OnPointerUp(PointerEventData eventData)
        {
            var selected = AppController.Instance.SelectedDetails;
            var isMultipleSelection = selected.Count > 1;

            if (!_isClick && !_isLongClick) return;

            if (IsSelected && !isMultipleSelection && !_isClick)
            {
                _isClick = false;
                return;
            }

            if (_isLongClick)
            {
                _isLongClick = false;
                return;
            }

            _isClick = false;
            _isLongClick = false;

            if (!selected.IsValid)
            {
                return;
            }

            if (eventData != null)
            {
                AppController.Instance.ActionsLog.RegisterAction(new SelectAction(selected.Selected, new HashSet<Detail> { this }));
            }

            selected.Clear(); // нужно, чтобы с предыдущей детали снялось выделение и она сформировала группы
            selected.Add(this);
        }



        private void CreateLineMaterial()
        {
            if (!_selectedMaterial)
            {
                // Unity has a built-in shader that is useful for drawing
                // simple colored things.
                var shader = Shader.Find("Hidden/Internal-Colored");
                _selectedMaterial = new Material(shader);
                _selectedMaterial.hideFlags = HideFlags.HideAndDontSave;
                // Turn on alpha blending
                _selectedMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
                _selectedMaterial.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off
                _selectedMaterial.SetInt("_Cull", (int) UnityEngine.Rendering.CullMode.Off);
                // Turn off depth writes
                _selectedMaterial.SetInt("_ZWrite", 0);
            }
        }

        private void OnRenderObject()
        {

            if (!IsSelected) return;

            var mesh = GetComponent<MeshFilter>().mesh;
            var ext = mesh.bounds.extents;

            CreateLineMaterial();
            _selectedMaterial.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.LINES);
            GL.Color(AppController.Instance.SelectedDetails.IsValid ? UnityEngine.Color.green : UnityEngine.Color.red);
            for (var i = 0; i < 4; i++)
            {
                // - + +, + + -, - - -, + - +
                var begin = new Vector3(ext.x * (i.IsOdd() ? 1 : -1), ext.y * (i / 2 < 1 ? 1 : -1), ext.z * (i % 3 == 0 ? 1 : -1));
                var beginGL = transform.TransformPoint(begin + mesh.bounds.center);
                for (var j = 0; j < 3; j++)
                {
                    GL.Vertex3(beginGL.x, beginGL.y, beginGL.z);
                    var end = begin;
                    end[j] *= -1;
                    var endGL = transform.TransformPoint(end + mesh.bounds.center);
                    GL.Vertex3(endGL.x, endGL.y, endGL.z);
                }
            }
            GL.End();
            GL.PopMatrix();
        }

        /// <summary>
        /// Имеет ли эта деталь соединения с другими деталями когда находится в позиции pos.
        /// Если pos == null, то берется текущая позиция детали.
        /// </summary>
        public override LinksBase GetLinks(Vector3 offset, Quaternion rotationDelta, Vector3 pivot, LinksMode linksMode = LinksMode.ExceptSelected)
        {
            var links = new DetailLinks(this, linksMode);

            links.IsValid = CheckOverlapping(linksMode, offset, rotationDelta, pivot);

            if (!links.IsValid)
            {
                return links;
            }

            GetConnections(_linkageColliders, offset, rotationDelta, pivot, linksMode, links);

            if (_axisLinkageColliders != null)
            {
                var layerPrefix = LayerMask.LayerToName(AxleLinkage.layer).Contains("Hub")
                    ? "Axle"
                    : "AxleHub";

                GetConnections(_axisLinkageColliders, offset, rotationDelta, pivot, linksMode, links, layerPrefix);
            }

            return links;
        }

        private void GetConnections(BoxCollider[] linkageColliders, Vector3 offset, Quaternion rotationDelta, Vector3 pivot, LinksMode linksMode, DetailLinks links, string layerPrefix = "")
        {
            if (linkageColliders == null)
            {
                return;
            }

            foreach (var linkageCollider in linkageColliders)
            {
                var overlapArea = linkageCollider;
                var overlapAreaCenter = overlapArea.transform.TransformPoint(overlapArea.center, pivot, rotationDelta, offset);
                var rotation = rotationDelta * overlapArea.transform.rotation;
                LayerMask layerMask;

                if (linksMode == LinksMode.SelectedOnly)
                {
                    layerMask = 1 << LayerMask.NameToLayer(layerPrefix + "SelectedLinkage");
                }
                else
                {
                    var selectedMask = linksMode == LinksMode.All
                        ? 1 << LayerMask.NameToLayer(layerPrefix + "SelectedLinkage")
                        : 0;

                    layerMask = 1 << LayerMask.NameToLayer(layerPrefix + "Linkage") | selectedMask;
                }
//                Debug.Log(offset + " " + rotation.eulerAngles + " " + overlapAreaCenter + " " + overlapArea.size);
                var neighbors = Physics.OverlapBox(overlapAreaCenter, overlapArea.size / 2, rotation, layerMask);
                //TODO если будет жрать память, то можно заменить на NonAlloc версию

                foreach (var neighbor in neighbors)
                {
                    if (neighbor.gameObject == linkageCollider.gameObject) continue;

                    var neighborDetail = neighbor.GetComponentInParent<Detail>();

                    links.Connections.Add(neighborDetail);
                }
            }
        }

        private bool CheckOverlapping(LinksMode linksMode, Vector3 offset, Quaternion rotationDelta, Vector3 pivot)
        {
            var overlapArea = SpaceBounds;
            var overlapAreaCenter = overlapArea.transform.TransformPoint(overlapArea.center, pivot, rotationDelta, offset);
            var rotation = rotationDelta * overlapArea.transform.rotation;

            LayerMask layerMask;

            if (linksMode == LinksMode.SelectedOnly)
            {
                layerMask = 1 << LayerMask.NameToLayer("SelectedItem");
            }
            else
            {
                var selectedMask = linksMode == LinksMode.All
                    ? 1 << LayerMask.NameToLayer("SelectedItem")
                    : 0;

                layerMask = 1 << LayerMask.NameToLayer("Item") | selectedMask;
            }

            var neighbors = Physics.OverlapBox(overlapAreaCenter, overlapArea.size / 2, rotation, layerMask);

            foreach (var neighbor in neighbors)
            {
                if (neighbor == SpaceBounds) continue;

                var overlapAreaRelativeBounds = Extentions.RelativeBounds(neighbor.transform, overlapArea, offset, rotationDelta, pivot);
                var neighborBoxCollider = neighbor.GetComponent<BoxCollider>();
                var neighborAreaRelativeBounds = new Bounds(neighborBoxCollider.center, neighborBoxCollider.size);

                var overlap = Extentions.Overlap(overlapAreaRelativeBounds, neighborAreaRelativeBounds);
                var size = overlap.size;

                // все координаты больше 1.1
                var invalidTest = Mathf.Min(size.x, 1.2f) + Mathf.Min(size.y, 1.2f) + Mathf.Min(size.z, 1.2f) > 3.599;

                Debug.Log(invalidTest + " " + (Mathf.Min(size.x, 1.2f) + Mathf.Min(size.y, 1.2f) + Mathf.Min(size.z, 1.2f))
                    + " " + offset + " " + overlap + " " + overlapAreaRelativeBounds + " " + neighborAreaRelativeBounds);



                // TODO тут надо добавить получение и анализ двух параметров: вращение деталей относительно друг друга
                // TODO и их смещение относительно друг друга (насколько оно соответствует узлам решетки)

                if (invalidTest)
                {
                    var neighborDetail = neighbor.GetComponent<Detail>();
                    var relativePos = neighborDetail.transform.TransformPoint(neighborDetail.AlignmentPoint)
                                                   - transform.TransformPoint(AlignmentPoint, pivot, rotationDelta, offset);

                    // исключения для деталей, которые могут проходить "сквозь" друг друга
                    var isNeighbourAxleOrBevel = neighborDetail.name.Contains(".") ||
                                                 neighborDetail.name.StartsWith("AxleBevelGear");
                    var isThisAxleOrBevel = name.Contains(".") ||
                                            name.StartsWith("AxleBevelGear");
                    var isNeighbourAxleHub = neighborDetail.name.Contains("Axle") &&
                                            !neighborDetail.name.Contains(".") &&
                                            !neighborDetail.name.StartsWith("AxleBevelGear");
                    var isThisAxleHub = name.Contains("Axle") &&
                                       !name.Contains(".") &&
                                       !name.StartsWith("AxleBevelGear");
                    var isNeighbourCommon = char.IsDigit(neighborDetail.name[0]);
                    var isThisCommon = char.IsDigit(name[0]);

                    // common & "BraceLine"
                    if (neighborDetail.name.StartsWith("BraceLine") && isThisCommon ||
                        isNeighbourCommon && name.StartsWith("BraceLine"))
                    {

                        if (!Mathf.RoundToInt(relativePos.x).IsOdd())
                        {
                            return false;
                        }

                        continue;
                    }

                    // (common | "BraceSquare") & "Axle"
                    if (isNeighbourAxleOrBevel && (isThisCommon || name.StartsWith("BraceSquare")) ||
                       (isNeighbourCommon || neighborDetail.name.StartsWith("BraceSquare")) && isThisAxleOrBevel)
                    {
                        // проверяем относительное расположение плоскостей деталей
                        var dot = Vector3.Dot(neighborDetail.transform.up.normalized, transform.forward.normalized);
                        var cohesionTest = Mathf.Abs(dot) < 0.00001f;

                        if (cohesionTest)
                        {
                            return false;
                        }

                        // проверяем, что ось проходит через квадратное отверстие детали, а не через крест
                        var axleDetail = char.IsDigit(name[0]) ? neighborDetail : this;
                        var axleDirection = (SerializableVector3) axleDetail.transform.forward.normalized;


                        if (!Mathf.RoundToInt(axleDirection.x == 0 ? relativePos.x : relativePos.y).IsOdd()) // TODO ???
                        {
                            return false;
                        }

                        continue;
                    }

                    // "AxleHub" & "Axle"
                    if (isNeighbourAxleOrBevel && isThisAxleHub ||
                        isNeighbourAxleHub && isThisAxleOrBevel)
                    {
                        // если плоскости деталей параллельны, то они проходят друг через друга
                        var dot = Vector3.Dot(neighborDetail.transform.forward.normalized, transform.forward.normalized);
                        var cohesionTest = Mathf.Abs(dot) < 0.00001f;

                        if (cohesionTest)
                        {
                            return false;
                        }

                        // проверяем, что ось проходит через крест
                        var axleDetail = neighborDetail.name.Contains(".") ? neighborDetail : this;
                        var axleDirection = (SerializableVector3) axleDetail.transform.forward.normalized;

                        if (Mathf.RoundToInt(axleDirection.x == 0 ? relativePos.x : relativePos.y).IsOdd())
                        {
                            return false;
                        }

                        var sumOverlap = size.x + size.y + size.z;

                        if (neighborDetail.name.StartsWith("AxleBevelGear") || name.StartsWith("AxleBevelGear"))
                        {
                            if (sumOverlap > 4f)
                            {
                                return false;
                            }

                            var bevelGearDetail = neighborDetail.name.StartsWith("AxleBevelGear")
                                ? neighborDetail
                                : this;
                            var bevelGearTransform = bevelGearDetail.transform;
                            var directionsDot = Vector3.Dot((bevelGearTransform.position + (bevelGearDetail == this ? offset : Vector3.zero) - overlap.center).normalized,
                                                           -(bevelGearTransform.forward.normalized));
                            var directionTest = directionsDot > 0;

                            if (!directionTest)
                            {
                                return false;
                            }
                        }

                        if (neighborDetail.name.StartsWith("AxleTip") || name.StartsWith("AxleTip"))
                        {
                            if (sumOverlap > 4f)
                            {
                                return false;
                            }

                            var tipDetail = neighborDetail.name.StartsWith("AxleTip")
                                ? neighborDetail
                                : this;
                            var tipTransform = tipDetail.transform;
                            var directionsDot = Vector3.Dot((tipTransform.position + (tipDetail == this ? offset : Vector3.zero) - overlap.center).normalized,
                                                            -(tipTransform.forward.normalized));
                            var directionTest = directionsDot > 0;

                            if (!directionTest)
                            {
                                return false;
                            }
                        }

                        continue;
                    }

                    return false;
                }
            }

            return true;
        }

        protected override void UpdateConnections(LinksBase newLinks)
        {
            var connectionsToAdd = new HashSet<Detail>(newLinks.Connections);
            connectionsToAdd.ExceptWith(_links.Connections);

            var connectionsToRemove = new HashSet<Detail>(_links.Connections);
            connectionsToRemove.ExceptWith(newLinks.Connections);

            if (newLinks.LinksMode != LinksMode.All)
            {
                Predicate<Detail> exceptSelected = detail => detail.IsSelected;
                Predicate<Detail> selectedOnly = detail => !detail.IsSelected;

                connectionsToRemove.RemoveWhere(newLinks.LinksMode == LinksMode.ExceptSelected ? exceptSelected : selectedOnly);
            }

            foreach (var newConnection in connectionsToAdd)
            {
                newConnection._links.Connections.Add(this);
            }

            _links.Connections.UnionWith(connectionsToAdd);

            foreach (var removeConnection in connectionsToRemove)
            {
                removeConnection._links.Connections.Remove(this);
            }

            _links.Connections.ExceptWith(connectionsToRemove);

            if (!_links.HasConnections && Group != null)
            {
                Group.Remove(this);
            }
        }


        public void OnPointerDown(PointerEventData eventData)
        {
            if (AppController.Instance.Mode == AppMode.InstructionsMode)
            {
                return;
            }

            _isClick = true;
            StartCoroutine(LongPress(++_callId));
        }

        private int _callId;

        private IEnumerator LongPress(int callId)
        {
            yield return new WaitForSeconds(1f);
            if (_isClick && _callId == callId)
            {
                var selected = AppController.Instance.SelectedDetails;

                if (selected.IsValid)
                {
                    var prevSelection = selected.Selected;

                    selected.Clear(); // нужно, чтобы с предыдущей детали снялось выделение и она сформировала группы
                    selected.Add(Group ?? (DetailBase) this);
                    AppController.Instance.ActionsLog.RegisterAction(new SelectAction(prevSelection, selected.Selected));
                }
                _isLongClick = true;
                _isClick = false;
            }
        }
    }
}
