using System;
using System.Collections;
using System.Collections.Generic;
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
    [RequireComponent(typeof (BoxCollider))]
    public class Detail : DetailBase, IBeginDragHandler, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        public override Bounds Bounds
        {
            get { return GetComponent<Renderer>().bounds; }
        }

        public DetailsGroup Group {
            get
            {
                var parent = transform.parent;
                return parent == null ? null : parent.GetComponent<DetailsGroup>();
            }
            set
            {
                if (Group == value) return;

                if (Group != null)
                {
                    Group.Remove(this);
                }

                if (value == null)
                {
                    transform.SetParent(null);
                    return;
                }
                value.Add(this);
            }
        }

        public DetailData Data
        {
            get
            {
                var materialName = GetComponent<Renderer>().material.name;
                var data = new DetailData
                {
                    Links = Links.Data,
                    GroupId = Group == null ? 0 : Group.GetInstanceID(),
                    Id = GetInstanceID(),
                    Position = transform.position,
                    Rotation = transform.rotation.eulerAngles,
                    Type = name.Remove(name.Length - "(Clone)".Length),
                    Material = materialName.Remove(materialName.Length - " (Instance)".Length)
                };

                return data;
            }
        }

        public DetailLinks Links { get { return _links; } }
        public HashSet<Detail> Connections { get { return _links.Connections; } }
        public HashSet<Detail> Touches { get { return _links.Touches; } }
        public HashSet<HashSet<Detail>> ImplicitConnections { get { return _links.ImplicitConnections; } }

        private int _sizeX;
        private int _sizeZ;
        private Vector3[] _raysOrigins;
        private Vector3[] _connectorsLocalPos;
//        private List<Transform> _currentConnections; 
        private int _holdingConnector;
        private Vector3 _prevPointerPos;
        private bool _isClick;
        private bool _isLongClick;

        private static Material _selectedMaterial;

//        private readonly HashSet<Detail> _connections = new HashSet<Detail>();
//        private readonly HashSet<Detail> _touches = new HashSet<Detail>();
//        private readonly HashSet<HashSet<Detail>> _implicitConnections = new HashSet<HashSet<Detail>>();

        private readonly DetailLinks _links = new DetailLinks();

        private void Awake()
        {
            // Определяем по размерам коллайдера тип детали и локальные координаты всех ее коннекторов
            var collider = GetComponent<Collider>();

            _sizeX = (int) collider.bounds.size.x - 1;
            _sizeZ = (int) collider.bounds.size.z - 1;

            var totalConnectors = _sizeX*_sizeZ;

            _raysOrigins = new Vector3[totalConnectors];
            _connectorsLocalPos = new Vector3[totalConnectors];

            for (var i = 0; i < _connectorsLocalPos.Length; i++)
            {
                _connectorsLocalPos[i] = new Vector3(i % _sizeX - _sizeX / 2,
                                                    -i / _sizeX + _sizeZ / 2);

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

            transform.RootParent().SetRaycastOrigins(holdingConnectorOffset);
        }

        public override void Detach()
        {
            var group = Group;

            if (group == null) return;

            var emptyLinks = new DetailLinks();
            var lostConnections = new HashSet<Detail>(_links.Connections);

            foreach (var detail in _links.Touches)
            {
                if (detail._links.HasWeakConnectionsWith(this, true))
                {
                    lostConnections.Add(detail);
//                    detail.UpdateLinks();
                }
            }

            UpdateLinks(emptyLinks);
            group.UpdateContinuity(lostConnections); // TODO передавать Links
        }

        public override void SetRaycastOrigins(Vector3 offset)
        {
            for (var i = 0; i < _raysOrigins.Length; i++) {
                _raysOrigins[i] = transform.TransformPoint(_connectorsLocalPos[i]) + offset;
            }
        }

        public override void Rotate(Vector3 axis)
        {
            transform.Rotate(axis, 90, Space.World);

            var newPos = transform.position;

            AlignPosition(ref newPos);
            var offset = newPos - transform.position;
            transform.Translate(offset, Space.World);
        }

        // TODO Кучу кода можно по идее заменить используя Physics.BoxCast!!!! (только не кидает ли он еще больше лучей?)
        public void OnDrag(PointerEventData eventData)
            // TODO коллайдеры, которые сейчас больше реальных размеров модели, хоть и помогают округлять в нужную сторону положение
        {
            // TODO коннекторов, влияют на область, за которую деталь можно "схватить" указателем, так что возможно лучше просто делать приращение

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

            var minRayHitInfo = transform.RootParent()
                .CastDetailAndGetClosestHit(newDirection, out raycaster, out rayOriginIndex);

            // Этот луч указывает новое положение соответствующего коннектора
            if (minRayHitInfo == null)
                return;

            // Округляем координаты точки, вычисляем вектор переноса и перемещаем деталь
            var hitPoint = minRayHitInfo.Value.point;
//            var centerOffset = sender.transform.position - sourcePoint;
            var curroffset = hitPoint -
                             raycaster.transform.TransformPoint(raycaster._connectorsLocalPos[rayOriginIndex]);
            var newPos = raycaster.transform.position + curroffset; //hitPoint + centerOffset;

//            _debugRay = new Ray(raycaster._raysOrigins[rayOriginIndex], hitPoint);

            raycaster.AlignPosition(ref newPos);


            if (newPos == transform.position) return;  //Debug.Log("Old pos: " + transform.position + ", new pos: " + newPos);

//            UpdateConnections(connections);


////            int i = 1;
////            foreach (var weakConnection in _weakConnections) {
////                Debug.Log("Connection " + i++ + ":");
////
////                foreach (var detail in weakConnection) {
////                    Debug.Log("    " + detail.name);
////                }
////            }
//
//
//            Debug.Log("Weak neighbours:");
//
//
//            foreach (var weakNeighgour in _touches) {
//                Debug.Log("    " + weakNeighgour.name);
//                
//            }
            var connections = transform.RootParent().GetLinks(newPos);

            if (hitPoint.y > 0 && !connections.HasConnections) return;

            transform.RootParent().Detach();

            var offset = newPos - raycaster.transform.position;
            transform.RootParent().transform.Translate(offset, Space.World);

            transform.RootParent().UpdateLinks(connections); ///// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        }


        public override RaycastHit? CastDetailAndGetClosestHit(Vector3 direction, out Detail raycaster, out int rayOriginIndex)
        {
            // Кидаем лучи, идущие  в новом направлении параллельно через все коннекторы детали и определяем самый короткий из них
            RaycastHit? minRayHitInfo = null;
            LayerMask layerMask = ~(1 << LayerMask.NameToLayer("SelectedItem"));

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

            return minRayHitInfo;
        }

        public void AlignPosition(ref Vector3 pos)
        {
            var bottomPointIndex = CorrectHeightAboveFloor(ref pos);

            var offsetFromCurrentPos = pos - transform.position;
            var bottomPointPos = transform.TransformPoint(_connectorsLocalPos[bottomPointIndex]) + offsetFromCurrentPos;
            var alignment = bottomPointPos.AlignAsCross() - bottomPointPos;

            pos += alignment;
        }

        private int CorrectHeightAboveFloor(ref Vector3 pos)
        {
            var offsetFromCurrentPos = pos - transform.position;
            var heightFirst = transform.TransformPoint(_connectorsLocalPos[0]).y + offsetFromCurrentPos.y;
            var heightLast = transform.TransformPoint(_connectorsLocalPos[_connectorsLocalPos.Length - 1]).y +
                             offsetFromCurrentPos.y;

            var bottomPointIndex = heightLast < heightFirst ? _connectorsLocalPos.Length - 1 : 0;
            var heightMin = Mathf.Min(heightFirst, heightLast);

            if (heightMin < 0)
            {
                pos.y += Mathf.Abs(heightMin);
            }

            return bottomPointIndex;
        }

        // Update is called once per frame
//        private void Update()
//        {
//            for (var i = 0; i < _raysOrigins.Length; i++) {
//                Debug.DrawRay(_raysOrigins[i], transform.TransformPoint(_connectorsLocalPos[i]) - _raysOrigins[i], Color.red, 0.1f);
//            }
//            Debug.DrawRay(transform.TransformPoint(-1, 1, 0), Vector3.up * 10, Color.red, 1);
//            if (IsSelected)
//                Debug.Log(HasConnection());
//            Debug.DrawRay(_debugRay.origin, _debugRay.direction, Color.red, 0.1f);
//        }

        public void OnPointerUp(PointerEventData eventData)
        {
            var isGroupSelected = Group != null && Group.IsSelected;

            if (!_isClick && !_isLongClick) return;

            if (IsSelected && !isGroupSelected && !_isClick)
            {
                _isClick = false;
                return;
            }

            if (_isLongClick)
            {
                _isLongClick = false;
                return;
            }

//            if (IsSelected) return;

            _isClick = false;
            _isLongClick = false;
//
//            IsSelected = true;

            //TODO переделать?
            var app = FindObjectOfType<ApplicationController>();
            app.SelectedDetail = null; // нужно, чтобы с предыдущей детали снялось выделение и она сформировала группы
            app.SelectedDetail = this;

//            mat.SetFloat("_intencity");
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
            GL.Color(Color.green);
            for (var i = 0; i < 4; i++)
            {
                // - + +, + + -, - - -, + - +
                var begin = new Vector3(ext.x*(i.IsOdd() ? 1 : -1), ext.y*(i/2 < 1 ? 1 : -1), ext.z*(i%3 == 0 ? 1 : -1));
                var beginGL = transform.TransformPoint(begin);
                for (var j = 0; j < 3; j++)
                {
                    GL.Vertex3(beginGL.x, beginGL.y, beginGL.z);
                    var end = begin;
                    end[j] *= -1;
                    var endGL = transform.TransformPoint(end);
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
        public override DetailLinks GetLinks(Vector3? pos = null, bool respectSelected = false) 
        {
            var detailPos = pos ?? transform.position;
            var col = GetComponent<BoxCollider>();
            var overlapArea = col.bounds;
            var links = new DetailLinks();

            var selectedMask = respectSelected ? 0 : 1 << LayerMask.NameToLayer("SelectedItem");

            overlapArea.center = detailPos;
            LayerMask layerMask = ~(selectedMask | 1 << LayerMask.NameToLayer("Workspace"));

            var neighbors = Physics.OverlapBox(overlapArea.center, overlapArea.extents, Quaternion.identity, layerMask);
                //TODO если будет жрать память, то можно заменить на NonAlloc версию
            var overlaps = new Dictionary<Detail, Bounds>();

            foreach (var neighbor in neighbors)
            {
                if (neighbor == col) continue;

                var detail = neighbor.GetComponent<Detail>();

                var overlap = Extentions.Overlap(overlapArea, neighbor.bounds);

                var size = overlap.size; //intersectionMax - intersectionMin;

                // у всех координат размеры больше 1 и минимум у двух координат размеры больше 2
                var test = Mathf.Min(size.x, 2) + Math.Min(size.y, 2) + Math.Min(size.z, 2) > 5;

                // у всех координат размеры больше 1
                var weakTest = Mathf.Min(size.x, 1) + Math.Min(size.y, 1) + Math.Min(size.z, 1) >= 3;
                
                if (test)
                {
                    links.Connections.Add(detail);
                }
                else if (weakTest)
                {
                    links.Touches.Add(detail);
                    overlaps[detail] = overlap;
                }
            }

            GetImplicitConnections(links, overlaps);

            return links;
        }

        private void UpdateWeakNeighbours(HashSet<Detail> newWeakNeighbours)
        {
            var addWeakNeighbours = new HashSet<Detail>(newWeakNeighbours);
            addWeakNeighbours.ExceptWith(_links.Touches);

            var removeWeakNeighbours = new HashSet<Detail>(_links.Touches);
            removeWeakNeighbours.ExceptWith(newWeakNeighbours);

            foreach (var newWeakNeighbour in addWeakNeighbours)
            {
                newWeakNeighbour._links.Touches.Add(this);
            }

            _links.Touches.UnionWith(addWeakNeighbours);

            foreach (var removeWeakNeighbour in removeWeakNeighbours) {
                removeWeakNeighbour._links.Touches.Remove(this);
            }

            _links.Touches.ExceptWith(removeWeakNeighbours);
        }

        private void UpdateImplicitConnections(HashSet<HashSet<Detail>> implicitConnections)
        {
            _links.ImplicitConnections.Clear();
            _links.ImplicitConnections.UnionWith(implicitConnections);
        }

        private void GetImplicitConnections(DetailLinks links, Dictionary<Detail, Bounds> overlaps)
        {
            var groups = new Dictionary<DetailsGroup, HashSet<Detail>>();

            foreach (var weakNeighbour in links.Touches)
            {
                var neighbourGroup = weakNeighbour.Group;

                if (neighbourGroup == null) continue;
                if (!groups.ContainsKey(neighbourGroup))
                    groups[neighbourGroup] = new HashSet<Detail>();
                groups[neighbourGroup].Add(weakNeighbour);
            }

//            foreach (var overlap in overlaps)
//            {
//                Debug.Log("overlaps (MaxMinCenter): " + overlap.Value.max + " " + overlap.Value.min + " " + overlap.Value.center);
//            }

            foreach (var group in groups)
            {
                if (group.Value.Count < 2) continue;

                var groupWeakNeighbours = group.Value.ToList();

                for (var n1 = 0; n1 < groupWeakNeighbours.Count; n1++)
                {
                    var weakNeighbour1 = groupWeakNeighbours[n1];

                    for (var n2 = n1 + 1; n2 < groupWeakNeighbours.Count; n2++)
                    {
                        var weakNeighbour2 = groupWeakNeighbours[n2];

                        var overlap12 = Extentions.Overlap(overlaps[weakNeighbour1], overlaps[weakNeighbour2]);

//                        Debug.Log("overlap12 (MaxMinCenter): " + overlap12.max + " " + overlap12.min + " " + overlap12.center);

                        // у всех координат размеры больше 0
                        if (!overlap12.Valid()) continue;

                        if (CheckOverlapForWeakTwoDetailsConnection(overlap12))
                        {
                            links.ImplicitConnections.Add(new HashSet<Detail> { weakNeighbour1, weakNeighbour2 });
                            continue;
                        }

                        if (groupWeakNeighbours.Count - n1 < 4) continue;

                        // проверка на слабую связь из 4 деталей
                        var oversizedDetails = new List<Detail>();
                        var commonOverlap = overlap12;

                        if (overlaps[weakNeighbour1].MaxSize() > 2) 
                            oversizedDetails.Add(weakNeighbour1);

                        if (overlaps[weakNeighbour2].MaxSize() > 2)
                            oversizedDetails.Add(weakNeighbour2);

                        for (var n3 = n2 + 1; n3 < groupWeakNeighbours.Count; n3++)
                        {
                            var weakNeighbour3 = groupWeakNeighbours[n3];

                            if (!CheckDetailForWeakFourDetailsConnection(ref commonOverlap, overlaps[weakNeighbour3],
                                oversizedDetails, weakNeighbour3)) continue;

                            for (var n4 = n3 + 1; n4 < groupWeakNeighbours.Count; n4++)
                            {
                                var weakNeighbour4 = groupWeakNeighbours[n4];

                                if (!CheckDetailForWeakFourDetailsConnection(ref commonOverlap, overlaps[weakNeighbour4],
                                    oversizedDetails, weakNeighbour4)) continue;

                                if (oversizedDetails.Count > 1)
                                {
                                    var oversizedDetailsOverlap = Extentions.Overlap(overlaps[oversizedDetails[0]],
                                                                                     overlaps[oversizedDetails[1]]);

                                    if (CheckOverlapForWeakTwoDetailsConnection(oversizedDetailsOverlap)) continue;
                                }

                                links.ImplicitConnections.Add(new HashSet<Detail> { weakNeighbour1, weakNeighbour2, weakNeighbour3, weakNeighbour4 });
                            }
                        }
                    }
                }
            }
        }

        private bool CheckDetailForWeakFourDetailsConnection(ref Bounds commonOverlap, Bounds detailOverlap,
            List<Detail> oversizedDetails, Detail detail)
        {
            if (detailOverlap.MaxSize() > 2 && oversizedDetails.Count > 1) return false;

            oversizedDetails.Add(detail);

            commonOverlap = Extentions.Overlap(commonOverlap, detailOverlap);

            return commonOverlap.Valid() && commonOverlap.MaxSize() > 1;
        }

        private bool CheckOverlapForWeakTwoDetailsConnection(Bounds overlap)
        {
            // у одной координаты размер больше 1 и у еще одной больше 2
            return Mathf.Min(overlap.size.x, 2) + Math.Min(overlap.size.y, 2) + Math.Min(overlap.size.z, 2) > 3;
        }

        private void UpdateConnections(HashSet<Detail> newConnections)
        {
            var connectionsToAdd = new HashSet<Detail>(newConnections);
            connectionsToAdd.ExceptWith(_links.Connections);

            var connectionsToRemove = new HashSet<Detail>(_links.Connections);
            connectionsToRemove.ExceptWith(newConnections);

            foreach (var newConnection in connectionsToAdd) {
                newConnection._links.Connections.Add(this);
            }

            _links.Connections.UnionWith(connectionsToAdd);

            foreach (var removeConnection in connectionsToRemove) {
                removeConnection._links.Connections.Remove(this);
            }

            _links.Connections.ExceptWith(connectionsToRemove);
        }

        /// <summary>
        /// Метод должен вызываться только у перемещаемых элементов. 
        /// </summary>
        public override void UpdateLinks(DetailLinks newLinks = null, bool respectSelected = false)
        {
            newLinks = newLinks ?? GetLinks(null, respectSelected);

            UpdateConnections(newLinks.Connections);
            UpdateWeakNeighbours(newLinks.Touches);
            UpdateImplicitConnections(newLinks.ImplicitConnections);


            var connectionsGroups = new HashSet<DetailsGroup>();
            var freeDetails = new HashSet<Detail>();

            foreach (var detail in newLinks.Connections) {
                if (detail.Group == null) {
                    freeDetails.Add(detail);
                } else { 
                    connectionsGroups.Add(detail.Group);
                }
            }

            foreach (var connection in newLinks.ImplicitConnections) {
                connectionsGroups.Add(connection.First().Group);
            }

            if (connectionsGroups.Count < 2 && freeDetails.Count == 0 && Group != null)
            {
                Group = connectionsGroups.Count == 0 ? null : connectionsGroups.Single();
                return;
            }

            // Не надо себя отхерачивать! Сначала проверь, что твоей группы нет в объединяемых группах! !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            if (Group == null || !connectionsGroups.Contains(Group)) {
                Group = null;
                freeDetails.Add(this);
            }

//            Debug.Log("UpdateLinks Merge: " + gameObject.name + " groups: " + connectionsGroups.Count + ", free: " + freeDetails.Count + ", touches: " + Touches.Count);
            DetailsGroup.Merge(connectionsGroups, freeDetails);

        }

        //TODO сделать присоединение детали к группе и группы к детали одинаковым (без создания новой группы)
//        public void ConnectWith(Transform detail)
//        {
////            Debug.Log("Connect! other: " + detail.gameObject.name + ", this:" + gameObject.name);
//
////             group = detail.RootParent() ?? transform.RootParent();
//
//            if (detail.RootParent().transform != detail)
//            {
//                var root = detail.root;
//                transform.root.SetParent(root);
//                return;
//            }
//
//            if (transform.RootParent().transform != transform) {
//                var root = transform.root;
//                detail.root.SetParent(root);
//                return;
//            }
//
//            var newGroup = new GameObject("DetailsGroup");
//            newGroup.AddComponent<DetailsGroup>();
//            newGroup.transform.position = (detail.position + transform.position) / 2f;
//
//            detail.SetParent(newGroup.transform);
//            transform.SetParent(newGroup.transform);
//        }

//        public void OnTriggerEnter(Collider collider) {
//            Debug.Log("Contact!");
//        }
//        public void OnCollisionEnter(Collision collision) {
//            Debug.Log("Contact!");
//        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isClick = true;
            StartCoroutine(LongPress(++_callId));
        }

        private int _callId;

        private IEnumerator LongPress(int callId)
        {
            yield return new WaitForSeconds(1f);
            if (_isClick && _callId == callId)
            {
                var app = FindObjectOfType<ApplicationController>();
                app.SelectedDetail = null; // нужно, чтобы с предыдущей детали снялось выделение и она сформировала группы
                app.SelectedDetail = Group ?? (DetailBase) this;
                
                _isLongClick = true;
                _isClick = false;
            }
        }
    }
}
///
/// Обычная деталь:
/// во время снятия выделения проверяем соединения со всеми деталями: 
/// если группа деталей у всех одна и она равна текущей группе выделенной детали, то все ок, просто обновляем списки связей в группе
/// если группы отличаются, то идем по всем группам и сливаем их в одну
/// 
/// 
///  Алгоритм проверки связности: берем все детали, которые соединяются с удаляемой деталью. Они образуют начальные множества несвязанных частей. Проверяем в этих множествах наличие пересечений.
///  Если они есть, то объединяем множества. Если нет, то дополняем множества деталями, которые соединены с теми деталями, которые уже присутствуют в множестве и снова ищем пересечения.
///  Если множества в итоге объединятся в одно, то деталь связная. Если нет, то множества образуют новые группы несвязных деталей.
/// 
///  Дополнение множеств просходит сначала деталями с жесткими связями, затем проверяется наличие мягких связей у добавленных деталей. Если они есть, то проверяется, что все части мягкой связи
///  помечены одним маркером и если это так, то деталь тоже помечается этим маркером и добавляется в множество.
/// 
///  Храним, короче слабые связи в самих деталях и при перемещениях / удалениях обрабатываем прежде всего их. Детали сами добавляют и удаляют себя в группы. Т.е. внутри группы не делается 
///  различия между слабыми и сильными связями, ими полностью управляют сами детали.
/// 
/// 
///  Еще одна хрень с тем, что слабая связь может быть на стороне детали, которую мы "держим". Т.е. нужно корректно определять, что у сложной детали есть детали, образующие слабую 
///  связь и она может прикрепиться к другой детали.
/// 
/// 