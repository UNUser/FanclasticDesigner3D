﻿using System;
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
    [RequireComponent(typeof (BoxCollider))]
	public class Detail : DetailBase, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
	    public BoxCollider SpaceBounds;
	    public GameObject Linkage;
	    public Vector3 AlignmentPoint;
	    public bool FixedColor;

		public override bool IsSelected {
			get { return AppController.Instance.SelectedDetails.IsSelected(this); }
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
        }

	    public DetailColor Color
	    {
		    get { return _color; }
		    set {
			    if (FixedColor) {
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
        private int _holdingConnector;
        private Vector3 _prevPointerPos;
        private bool _isClick;
        private bool _isLongClick;
	    private Vector3 _sourcePosition;

        private static Material _selectedMaterial;

        private DetailLinks _links;

        private void Awake()
        {
	        _links = new DetailLinks(this);

            // Определяем по размерам коллайдера тип детали и локальные координаты всех ее коннекторов
            var colliderSize = GetComponent<Collider>().bounds.size;
	        var swapZY = colliderSize.y > colliderSize.z;

			_sizeX = (int) colliderSize.x - 1;
			_sizeZ = (int) (swapZY ? colliderSize.y : colliderSize.z) - 1;

            var totalConnectors = _sizeX*_sizeZ;

            _raysOrigins = new Vector3[totalConnectors];
            _connectorsLocalPos = new Vector3[totalConnectors];

            for (var i = 0; i < _connectorsLocalPos.Length; i++)
            {
				var valueZ = -i / _sizeX + _sizeZ / 2;

                _connectorsLocalPos[i] = new Vector3(i % _sizeX - _sizeX / 2,
													swapZY ? valueZ : 0,
                                                    swapZY ? 0 : valueZ);

            }

	        _linkageColliders = Linkage.GetComponents<BoxCollider>();
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

	        if (eventData != null) {
		        _sourcePosition = transform.position;
	        }
	        
//			AppController.Instance.SelectedDetails.Detach();
        }


        public void SetRaycastOrigins(Vector3 offset)
        {
            for (var i = 0; i < _raysOrigins.Length; i++) {
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
//            var centerOffset = sender.transform.position - sourcePoint;
            var curroffset = hitPoint -
                             raycaster.transform.TransformPoint(raycaster._connectorsLocalPos[rayOriginIndex]);
            var newPos = raycaster.transform.position + curroffset; //hitPoint + centerOffset;

//            _debugRay = new Ray(raycaster._raysOrigins[rayOriginIndex], hitPoint);

            raycaster.AlignPosition(ref newPos);

            if (newPos == raycaster.transform.position) return;  //Debug.Log("Old pos: " + transform.position + ", new pos: " + newPos);


			var offset = newPos - raycaster.transform.position;
			var links = AppController.Instance.SelectedDetails.GetLinks(LinksMode.ExceptSelected, offset);

	        if (!links.IsValid || (hitPoint.y > 0 && !links.HasConnections)) {
				return;
	        }

	        var detached = AppController.Instance.SelectedDetails.Detach();

			AppController.Instance.SelectedDetails.IsValid = links.IsValid;
            detached.transform.Translate(offset, Space.World);
			detached.UpdateLinks(links.LinksMode, links); ///// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        }

	    public void OnEndDrag(PointerEventData eventData)
	    {
			if (!IsSelected) return;

		    var offset = transform.position - _sourcePosition;

		    if (offset != Vector3.zero) {
			    AppController.Instance.ActionsLog.RegisterAction(new MoveAction(offset));
		    }
	    }

        public RaycastHit? CastDetailAndGetClosestHit(Vector3 direction, out Detail raycaster, out int rayOriginIndex)
        {
            // Кидаем лучи, идущие  в новом направлении параллельно через все коннекторы детали и определяем самый короткий из них
            RaycastHit? minRayHitInfo = null;
			LayerMask layerMask = ~(1 << LayerMask.NameToLayer("SelectedItem") | 1 << LayerMask.NameToLayer("SelectedLinkage"));

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
            var alignmentPoint = transform.TransformPoint(AlignmentPoint) + offsetFromCurrentPos;
			var bottomPoint = transform.TransformPoint(_connectorsLocalPos[bottomPointIndex]) + offsetFromCurrentPos;

			var alignment = bottomPoint.AlignByCrossPoint(alignmentPoint) - bottomPoint;

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

	        if (!selected.IsValid) {
		        return;
	        }

	        if (eventData != null) {
		        AppController.Instance.ActionsLog.RegisterAction(new SelectAction(selected.Selected, new HashSet<Detail>{this}));
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
                var begin = new Vector3(ext.x*(i.IsOdd() ? 1 : -1), ext.y*(i/2 < 1 ? 1 : -1), ext.z*(i%3 == 0 ? 1 : -1));
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
        public override LinksBase GetLinks(LinksMode linksMode = LinksMode.ExceptSelected, Vector3? offset = null) 
        {
            var offsetValue = offset ?? Vector3.zero;
			var links = new DetailLinks(this, linksMode);

	        links.IsValid = CheckOverlapping(linksMode, offsetValue);

	        if (!links.IsValid) {
		        return links;
	        }

	        foreach (var linkageCollider in _linkageColliders)
	        {
		        var overlapArea = linkageCollider.bounds;

		        overlapArea.center += offsetValue;
		        LayerMask layerMask;

		        if (linksMode == LinksMode.SelectedOnly)
		        {
			        layerMask = 1 << LayerMask.NameToLayer("SelectedLinkage");
		        }
		        else
		        {
			        var selectedMask = linksMode == LinksMode.All
						? 1 << LayerMask.NameToLayer("SelectedLinkage")
				        : 0;

			        layerMask = 1 << LayerMask.NameToLayer("Linkage") | selectedMask;
		        }

		        var neighbors = Physics.OverlapBox(overlapArea.center, overlapArea.extents, Quaternion.identity, layerMask);
		        //TODO если будет жрать память, то можно заменить на NonAlloc версию

		        foreach (var neighbor in neighbors)
		        {
			        if (neighbor.gameObject == linkageCollider.gameObject) continue;

			        var neighborDetail = neighbor.GetComponentInParent <Detail>();

//					if (links.Connections.Contains(neighborDetail)) continue;

//					var neighborShell = neighborDetail.GetComponent<BoxCollider>().bounds;
//			        var thisShell = GetComponent<BoxCollider>().bounds;
//
//			        thisShell.center += offsetValue;
//
//			        var overlap = Extentions.Overlap(thisShell, neighborShell);
//			        var size = overlap.size;
//
//			        // все координаты больше 2.25
//			        var invalidTest = Mathf.Min(size.x, 2.25f) + Mathf.Min(size.y, 2.25f) + Mathf.Min(size.z, 2.25f) > 6.5;
//
//			        if (invalidTest)
//			        {
//				        links.IsValid = false;
//				        return links;
//			        }

				    links.Connections.Add(neighborDetail);
		        }

	        }

	        return links;
        }

	    private bool CheckOverlapping(LinksMode linksMode, Vector3 offset)
	    {
			var overlapArea = SpaceBounds.bounds;
			LayerMask layerMask;

			overlapArea.center += offset;
			
			if (linksMode == LinksMode.SelectedOnly) {
				layerMask = 1 << LayerMask.NameToLayer("SelectedItem");
			} else {
				var selectedMask = linksMode == LinksMode.All
					? 1 << LayerMask.NameToLayer("SelectedItem")
					: 0;

				layerMask = 1 << LayerMask.NameToLayer("Item") | selectedMask;
			}

			var neighbors = Physics.OverlapBox(overlapArea.center, overlapArea.extents, Quaternion.identity, layerMask);

		    foreach (var neighbor in neighbors)
		    {
				if (neighbor == SpaceBounds) continue;

				var overlap = Extentions.Overlap(overlapArea, neighbor.bounds);
				var size = overlap.size;

				// все координаты больше 1.1 
				var invalidTest = Mathf.Min(size.x, 1.2f) + Mathf.Min(size.y, 1.2f) + Mathf.Min(size.z, 1.2f) >= 3.6;

				if (invalidTest) {
//					Debug.Log(gameObject.name + " " + neighbor.gameObject.name + " " + overlap + " " + linksMode);
					var neighborDetail = neighbor.GetComponent<Detail>();

					if (neighborDetail.name.StartsWith("BraceLine") && char.IsDigit(name[0]) ||
					    char.IsDigit(neighborDetail.name[0]) && name.StartsWith("BraceLine"))
					{
						var relativePos = neighborDetail.transform.TransformPoint(neighborDetail.AlignmentPoint) - transform.TransformPoint(AlignmentPoint) + offset;

						if (Mathf.RoundToInt(relativePos.x).IsOdd()) {
							continue;
						}
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
			

            foreach (var newConnection in connectionsToAdd) {
                newConnection._links.Connections.Add(this);
            }

            _links.Connections.UnionWith(connectionsToAdd);

            foreach (var removeConnection in connectionsToRemove) {
                removeConnection._links.Connections.Remove(this);
            }

            _links.Connections.ExceptWith(connectionsToRemove);

			if (!_links.HasConnections && Group != null) {
				Group.Remove(this);
			}
        }


        public void OnPointerDown(PointerEventData eventData)
        {
			if (AppController.Instance.Mode == AppMode.InstructionsMode) {
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

	            if (selected.IsValid) {
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
