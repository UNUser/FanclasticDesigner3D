using System;
using System.Collections.Generic;
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
    public class Detail : DetailBase, IBeginDragHandler, IDragHandler, IPointerClickHandler
    {
        public override Bounds Bounds
        {
            get { return GetComponent<Renderer>().bounds; }
        }

        private int _sizeX;
        private int _sizeZ;
        private Vector3[] _raysOrigins;
        private Vector3[] _connectorsLocalPos;
//        private List<Transform> _currentConnections; 
        private int _holdingConnector;
        private Vector3 _prevPointerPos;

        private static Material _selectedMaterial;


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
            var rootParent = transform.RootParent();

            if (rootParent != null)
                rootParent.SetRaycastOrigins(holdingConnectorOffset);
            else
                SetRaycastOrigins(holdingConnectorOffset);
            
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
            var rootParent = transform.RootParent();

            var minRayHitInfo = rootParent != null 
                ? rootParent.CastDetailAndGetClosestHit(newDirection, out raycaster, out rayOriginIndex) 
                : CastDetailAndGetClosestHit(newDirection, out raycaster, out rayOriginIndex);

            // Этот луч указывает новое положение соответствующего коннектора
            if (minRayHitInfo == null)
                return;

            // Округляем координаты точки, вычисляем вектор переноса и перемещаем деталь
            var hitPoint = minRayHitInfo.Value.point;
//            var centerOffset = sender.transform.position - sourcePoint;
            var curroffset = hitPoint - raycaster.transform.TransformPoint(raycaster._connectorsLocalPos[rayOriginIndex]);
            var newPos = raycaster.transform.position + curroffset;//hitPoint + centerOffset;

            _debugRay = new Ray(raycaster._raysOrigins[rayOriginIndex], hitPoint);

            raycaster.AlignPosition(ref newPos);


            var connections = GetConnections(newPos);
//            UpdateConnections(connections);
            if (hitPoint.y > 0 && connections.Count == 0)
                return;

            var offset = newPos - raycaster.transform.position;
            if (rootParent != null)
                rootParent.transform.Translate(offset, Space.World);
            else
                transform.Translate(offset, Space.World);
        }


        private Ray _debugRay;


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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsSelected) return; 
//
//            IsSelected = true;

            //TODO переделать?
            var app = FindObjectOfType<ApplicationController>();
            app.SelectedDetail = null; // нужно, чтобы с предыдущей детали снялось выделение и она сформировала группы

            var rootParent = transform.RootParent();
            app.SelectedDetail = rootParent != null ? (DetailBase) rootParent : this;

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
        public override List<Transform> GetConnections(Vector3? pos = null)
        {
            var detailPos = pos ?? transform.position;
            var col = GetComponent<BoxCollider>();
            var overlapArea = col.bounds;
            var connections = new List<Transform>();

            overlapArea.center = detailPos;
            LayerMask layerMask =
                ~(1 << LayerMask.NameToLayer("SelectedItem") | 1 << LayerMask.NameToLayer("Workspace"));

            var neighbors = Physics.OverlapBox(overlapArea.center, overlapArea.extents, Quaternion.identity, layerMask);
                //TODO если будет жрать память, то можно заменить на NonAlloc версию
            
            foreach (var neighbor in neighbors)
            {
                if (neighbor == col) continue;

                var colMin = overlapArea.min;
                var neighborMin = neighbor.bounds.min;
                var colMax = overlapArea.max;
                var neighborMax = neighbor.bounds.max;

                var intersectionMin = new Vector3(Mathf.Max(colMin.x, neighborMin.x), Mathf.Max(colMin.y, neighborMin.y),
                    Mathf.Max(colMin.z, neighborMin.z));
                var intersectionMax = new Vector3(Mathf.Min(colMax.x, neighborMax.x), Mathf.Min(colMax.y, neighborMax.y),
                    Mathf.Min(colMax.z, neighborMax.z));

                var v = intersectionMax - intersectionMin;
                    // у всех координат размеры больше 1 и минимум у двух координат размеры больше 2
                var test = Mathf.Min(v.x, 2) + Math.Min(v.y, 2) + Math.Min(v.z, 2) > 5;

                if (test)
                    connections.Add(neighbor.transform);
            }

            return connections;
        }

        public override void UpdateConnections()
        {
            var connections = GetConnections();
            var parent = transform.parent;

            if (connections.Count == 0)
            {
                if (parent != null)
                {
                    transform.SetRootParent(null);
                    if (parent.childCount == 1)
                    {
                        var lastChild = parent.GetChild(0);

                        lastChild.parent = parent.parent;
                        Destroy(parent.gameObject);
                    }
                }

                return;
            }

            foreach (var connection in connections)
            {
                if (connection.root == transform.root) continue;

//                if (parent == null)
//                {
                    ConnectWith(connection);
//                }
            }
        }

        //TODO сделать присоединение детали к группе и группы к детали одинаковым (без создания новой группы)
        public void ConnectWith(Transform detail)
        {
//            Debug.Log("Connect! other: " + detail.gameObject.name + ", this:" + gameObject.name);

//             group = detail.RootParent() ?? transform.RootParent();

            if (detail.RootParent() != null)
            {
                var root = detail.root;
                transform.root.SetParent(root);
                return;
            }

            if (transform.RootParent() != null) {
                var root = transform.root;
                detail.root.SetParent(root);
                return;
            }

            var newGroup = new GameObject("DetailsGroup");
            newGroup.AddComponent<DetailsGroup>();
            newGroup.transform.position = (detail.position + transform.position) / 2f;

            detail.SetParent(newGroup.transform);
            transform.SetParent(newGroup.transform);
        }


    }
}