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
		 
		public override bool IsSelected {
			get { return AppController.Instance.SelectedDetails.IsSelected(this); }
			set {
				var selected = AppController.Instance.SelectedDetails;

				if (value)
				{
					selected.Add(this);
				} else {
					selected.Remove(this);
				}
				
				gameObject.layer = LayerMask.NameToLayer(value ? "SelectedItem" : "Default");
			}
		}

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

        public LinksBase Links { get { return _links; } }
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

        private DetailLinks _links;

        private void Awake()
        {
	        _links = new DetailLinks(LinksMode.ExceptSelected, this);

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

            transform.RootParent().Detach();
        }

        public override void Detach()
        {
            var group = Group;

            if (group == null) return;


            group.UpdateContinuity(this); // TODO передавать Links
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


            if (newPos == raycaster.transform.position) return;  //Debug.Log("Old pos: " + transform.position + ", new pos: " + newPos);

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

			var offset = newPos - raycaster.transform.position;
            List<LinksBase> linksList;
			var hasConnections = transform.RootParent().GetLinks(out linksList, offset);


	        if (hitPoint.y > 0 && !hasConnections)
	        {
		        var hitDetail = minRayHitInfo.Value.transform.RootParentOld();
		        List<LinksBase> hitDetailLinksList;
				var hitDetailHasConnections = hitDetail.GetLinks(out hitDetailLinksList, -offset, LinksMode.SelectedOnly);

				if (!hitDetailHasConnections)
					return;
	        }

	        transform.RootParent().Detach();

//            var offset = newPos - raycaster.transform.position;
            transform.RootParent().transform.Translate(offset, Space.World);

			if (transform.RootParent() == this)
				transform.RootParent().UpdateLinks(linksList); ///// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
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
	        var selected = AppController.Instance.SelectedDetails;
            selected.Clear(); // нужно, чтобы с предыдущей детали снялось выделение и она сформировала группы
			selected.Add(this);

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
        public override bool GetLinks(out LinksBase links1, Vector3? offset = null, LinksMode linksMode = LinksMode.ExceptSelected) 
        {
            var detailPos = transform.position + (offset ?? Vector3.zero);
            var col = GetComponent<BoxCollider>();
            var overlapArea = col.bounds;
            var links = new LinksBase(TODO, this);

            overlapArea.center = detailPos;
            LayerMask layerMask;

	        if (linksMode == LinksMode.SelectedOnly) {
				layerMask = 1 << LayerMask.NameToLayer("SelectedItem");
	        } else {
				var selectedMask = linksMode == LinksMode.All ? 0 : 1 << LayerMask.NameToLayer("SelectedItem");

		        layerMask = ~(selectedMask | 1 << LayerMask.NameToLayer("Workspace"));
	        }

	        var neighbors = Physics.OverlapBox(overlapArea.center, overlapArea.extents, Quaternion.identity, layerMask);
                //TODO если будет жрать память, то можно заменить на NonAlloc версию
            var overlaps = new Dictionary<Detail, Bounds>();

            foreach (var neighbor in neighbors)
            {
                if (neighbor == col) continue;

                var detail = neighbor.GetComponent<Detail>();

                var overlap = Extentions.Overlap(overlapArea, neighbor.bounds);

                var size = overlap.size;

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

			links1 = new List <LinksBase> { links };

	        return links.HasConnections;
        }

        public void UpdateWeakNeighbours(HashSet<Detail> newWeakNeighbours, LinksMode linksMode)
        {
            var addWeakNeighbours = new HashSet<Detail>(newWeakNeighbours);
            addWeakNeighbours.ExceptWith(_links.Touches);

            var removeWeakNeighbours = new HashSet<Detail>(_links.Touches);
            removeWeakNeighbours.ExceptWith(newWeakNeighbours);

			if (linksMode != LinksMode.All) {
				Predicate<Detail> exceptSelected = detail => detail.IsSelected;
				Predicate<Detail> selectedOnly = detail => !detail.IsSelected;

				removeWeakNeighbours.RemoveWhere(linksMode == LinksMode.ExceptSelected ? exceptSelected : selectedOnly);
			}

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

        public void UpdateImplicitConnections(HashSet<HashSet<Detail>> implicitConnections, LinksMode linksMode)
        {
			//TODO тут надо учитывать linksMode
//			if (linksMode != LinksMode.All) {
//				Predicate<Detail> exceptSelected = detail => detail.IsSelected;
//				Predicate<Detail> selectedOnly = detail => !detail.IsSelected;
//
//				connectionsToRemove.RemoveWhere(linksMode == LinksMode.ExceptSelected ? exceptSelected : selectedOnly);
//			}

            _links.ImplicitConnections.Clear();
            _links.ImplicitConnections.UnionWith(implicitConnections);
        }

        private void GetImplicitConnections(LinksBase linksBase, Dictionary<Detail, Bounds> overlaps)
        {
            var groups = new Dictionary<DetailsGroup, HashSet<Detail>>();

            foreach (var weakNeighbour in linksBase.Touches)
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

                        if (!CheckPrerequisite(overlap12)) continue;

                        if (CheckForTwofoldImplicitConnection(overlap12))
                        {
                            linksBase.ImplicitConnections.Add(new HashSet<Detail> { weakNeighbour1, weakNeighbour2 });
                            continue;
                        }

//                        if (groupWeakNeighbours.Count - n1 < 4) continue;

                        // Неявные соединения более, чем из двух деталей, могут удерживать только детали вида 1 x n
                        // за один из двух концов. Это значит, что у таких соединение общая область пересечения ни 
                        // по одной координате не может быть больше двух. В противном случае в таком соединении есть 
                        // пара деталей, образующих неявное соединение из двух деталей.


                        for (var n3 = n2 + 1; n3 < groupWeakNeighbours.Count; n3++)
                        {
                            var weakNeighbour3 = groupWeakNeighbours[n3];

	                        var overlap123 = Extentions.Overlap(overlap12, overlaps[weakNeighbour3]);

							if (!CheckPrerequisite(overlap123) || FormStrongerImplicitConnectionWithLast(overlaps[weakNeighbour1], overlaps[weakNeighbour2], overlaps[weakNeighbour3])) {
		                        continue;
	                        }

	                        if (CheckForThreefoldImplicitConnection(overlaps[weakNeighbour1], overlaps[weakNeighbour2],
		                        overlaps[weakNeighbour3]))
	                        {
								linksBase.ImplicitConnections.Add(new HashSet<Detail> { weakNeighbour1, weakNeighbour2, weakNeighbour3 });
		                        continue;
	                        }

                            for (var n4 = n3 + 1; n4 < groupWeakNeighbours.Count; n4++)
                            {
                                var weakNeighbour4 = groupWeakNeighbours[n4];

								var overlap1234 = Extentions.Overlap(overlap123, overlaps[weakNeighbour4]);

								if (!CheckPrerequisite(overlap1234) || FormStrongerImplicitConnectionWithLast(overlaps[weakNeighbour1], overlaps[weakNeighbour2], 
																											  overlaps[weakNeighbour3], overlaps[weakNeighbour4])) {
									continue;
								}

                                linksBase.ImplicitConnections.Add(new HashSet<Detail> { weakNeighbour1, weakNeighbour2, weakNeighbour3, weakNeighbour4 });
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///  Минимально возможная область пересечения, необходимая для создания неявного соединения
        ///  должна быть положительна по всем координатам и хотя бы по одной иметь размер больше 1
        /// </summary>
        private bool CheckPrerequisite(Bounds overlap)
        {
            return overlap.Valid() && overlap.MaxSize() > 1;
        }

		/// <summary>
		///  Образует ли последняя деталь, переданная в параметрах, более сильные связи с предыдущими деталями
		/// </summary>
	    private bool FormStrongerImplicitConnectionWithLast (Bounds touch1, Bounds touch2, Bounds touch3, Bounds? touch4 = null)
		{
		    var checkList = new List<Bounds> {touch1, touch2};
			var last = touch4 ?? touch3;

		    if (touch4 != null) {
			    checkList.Add(touch3);
		    }

			for (var i = 0; i < checkList.Count; i++) {
				var commonOverlap = Extentions.Overlap(checkList[i], last);

				if (CheckForTwofoldImplicitConnection(commonOverlap)) {
					return true;
				}

				if (touch4 == null) {
					continue;
				}

				for (var j = i + 1; j < checkList.Count; j++)
				{
					commonOverlap = Extentions.Overlap(commonOverlap, checkList[j]);

					if (!commonOverlap.Valid()) {
						continue;
					}

					if (CheckForThreefoldImplicitConnection(checkList[i], checkList[j], last)) {
						return true;
					}
				}
			}

			return false;
		}


		private bool CheckForThreefoldImplicitConnection(Bounds detailOverlap1, Bounds detailOverlap2, Bounds detailOverlap3) {

	        var overlapNew1 = Extentions.Overlap(detailOverlap3, detailOverlap1);
	        var overlapNew2 = Extentions.Overlap(detailOverlap3, detailOverlap2);
	        var overlap12 = Extentions.Overlap(detailOverlap1, detailOverlap2);

			return ThreefoldPrerequisite(overlapNew1) && ThreefoldPrerequisite(overlapNew2) && ThreefoldPrerequisite(overlap12);
        }

	    private bool ThreefoldPrerequisite(Bounds overlap)
	    {
			return Mathf.Min(overlap.size.x, 1f) + Mathf.Min(overlap.size.y, 1f) + Mathf.Min(overlap.size.z, 1f) >= 2f;
	    }

        private bool CheckForFourfoldImplicitConnection (Bounds commonOverlap/*, Bounds detailOverlapNew*/)
        {
//            commonOverlap = Extentions.Overlap(commonOverlap, detailOverlapNew);

            return CheckPrerequisite(commonOverlap);
        }

        private bool CheckForTwofoldImplicitConnection(Bounds overlap)
        {
            // у одной координаты размер больше 1 и у еще одной больше 2
            return Mathf.Min(overlap.size.x, 2) + Math.Min(overlap.size.y, 2) + Math.Min(overlap.size.z, 2) > 3;
        }

		public void UpdateConnections(HashSet<Detail> newConnections, LinksMode linksMode)
        {
            var connectionsToAdd = new HashSet<Detail>(newConnections);
            connectionsToAdd.ExceptWith(_links.Connections);

            var connectionsToRemove = new HashSet<Detail>(_links.Connections);
            connectionsToRemove.ExceptWith(newConnections);

			if (linksMode != LinksMode.All)
			{
				Predicate<Detail> exceptSelected = detail => detail.IsSelected;
				Predicate<Detail> selectedOnly = detail => !detail.IsSelected;

				connectionsToRemove.RemoveWhere(linksMode == LinksMode.ExceptSelected ? exceptSelected : selectedOnly);
			}
			

            foreach (var newConnection in connectionsToAdd) {
                newConnection._links.Connections.Add(this);
            }

            _links.Connections.UnionWith(connectionsToAdd);

            foreach (var removeConnection in connectionsToRemove) {
                removeConnection._links.Connections.Remove(this);
            }

            _links.Connections.ExceptWith(connectionsToRemove);
        }

//        public override void UpdateLinks(List<DetailLinks> newLinks = null, LinksMode linksMode = LinksMode.ExceptSelected)
//        {


//        }

        //TODO сделать присоединение детали к группе и группы к детали одинаковым (без создания новой группы)


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
                var app = FindObjectOfType<AppController>();
                app.SelectedDetails = null; // нужно, чтобы с предыдущей детали снялось выделение и она сформировала группы
                app.SelectedDetails = Group ?? (DetailBase) this;
                
                _isLongClick = true;
                _isClick = false;
            }
        }
    }
}

/// 
///  Еще одна хрень с тем, что слабая связь может быть на стороне детали, которую мы "держим". Т.е. нужно корректно определять, что у сложной детали есть детали, образующие слабую 
///  связь и она может прикрепиться к другой детали.
/// 
/// 