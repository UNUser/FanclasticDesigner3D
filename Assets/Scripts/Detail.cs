using System;
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
    public class Detail : MonoBehaviour, IPointerUpHandler, IBeginDragHandler, IDragHandler, IPointerClickHandler
    {
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
//                _renderer.material.shader = _isSelected ? _selectedShader : _originalShader;
                gameObject.layer = LayerMask.NameToLayer(_isSelected ? "SelectedItem" : "Default");;
            }
        }

        private int _sizeX;
        private int _sizeZ;
        private Vector3[] _raysOrigins;
        private Vector3[] _connectorsLocalPos;
        private int _holdingConnector;
        private Vector3 _prevPointerPos;
        private bool _isSelected;

        private MeshRenderer _renderer;
//        private Shader _originalShader;
//        private Shader _selectedShader;
        private static Material _selectedMaterial;


        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();

//            _selectedShader = Shader.Find("Outlined/Diffuse");
//            _originalShader = Shader.Find("Standard");

//            Shader shader;
//
//            try
//            {
//                shader = ShaderUtil.CreateShaderAsset("Shader \"Lines/Colored Blended\" {" +
//                                          "SubShader { Pass { " +
//                                          "    Blend SrcAlpha OneMinusSrcAlpha " +
//                                          "    ZWrite Off Cull Off Fog { Mode Off } " +
//                                          "    BindChannels {" +
//                                          "      Bind \"vertex\", vertex Bind \"color\", color }" +
//                                          "} } }");
//            }
//            catch (Exception)
//            {
//                
//                Debug.Log("Catch!");
//                return;
//            }

//            if (_selectedMaterial == null)
//            {
//                _selectedMaterial = new Material(Shader.Find("Standard"/*"SelectedBox/Colored Blended"*/))
//                {
//                    hideFlags = HideFlags.HideAndDontSave,
//                    shader = {hideFlags = HideFlags.HideAndDontSave}
//                };
//            }

            // Определяем по размерам коллайдера тип детали и локальные координаты всех ее коннекторов
            var collider = GetComponent<Collider>();

            _sizeX = (int) collider.bounds.size.x - 1;
            _sizeZ = (int) collider.bounds.size.z - 1;

            var totalConnectors = _sizeX * _sizeZ;

            _raysOrigins = new Vector3[totalConnectors];
            _connectorsLocalPos = new Vector3[totalConnectors];

            for (var i = 0; i < _connectorsLocalPos.Length; i++)
            {
                _connectorsLocalPos[i] = new Vector3((i + _sizeX) % _sizeX - _sizeX / 2,
                                                              -i / _sizeX + _sizeZ / 2);

            }
        }

        private int Index2Row(int i)
        {
            var m = 2 * _sizeX - 1;
            return 2 * (i / m) + ((i + m) % m) / _sizeX;
        }

        private int Index2Column(int i) {
            var m = 2 * _sizeX - 1;
            return (((i + m) % m) % _sizeX);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isSelected) return;

#if !UNITY_STANDALONE && !UNITY_EDITOR

            if (Input.touchCount > 1) return;
#endif

            // Находим коннектор, за которой "держим" деталь:
            var pointerRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;

            // 1. Бросаем луч в то место, на которое нажал пользователь
            if (!Physics.Raycast(pointerRay, out hitInfo))
            {
                Debug.LogError("OnMouseDown without raycast hit!");
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

            // Затем пересчитываем положения начала лучей, которые будем бросать от камеры через коннекторы детали
            // (при этом луч, исходящий из камеры должен попадать в тот коннектор, за который мы "держим" деталь)
            var holdingConnectorOffset = _connectorsLocalPos[_holdingConnector];
            var cameraOffset = Camera.main.transform.position - transform.position;

            for (var i = 0; i < _raysOrigins.Length; i++) {
                _raysOrigins[i] = transform.TransformPoint(_connectorsLocalPos[i] - holdingConnectorOffset) + cameraOffset;
            }

            _prevPointerPos = Input.mousePosition;
        }

        public void Rotate(Vector3 axis)  // TODO при поворотах коннекторы могут встать в запрещенные позиции, нужно проверять
        {
            transform.Rotate(axis, 90);

            var deltaFirst = transform.TransformPoint(_connectorsLocalPos[0]).y;
            var deltaLast = transform.TransformPoint(_connectorsLocalPos[_connectorsLocalPos.Length - 1]).y;
            var deltaMin = Mathf.Min(deltaFirst, deltaLast);

            if (deltaMin < 0)
            {
                transform.Translate(Vector3.up * (int) -deltaMin, Space.World);
            }

            var alignment = GetAlignment(transform.TransformPoint(_connectorsLocalPos[0]));

            transform.Translate(alignment, Space.World);
        }

        // TODO Кучу кода можно по идее заменить используя Physics.BoxCast!!!! (только не кидает ли он еще больше лучей?)
        public void OnDrag(PointerEventData eventData) // TODO коллайдеры, которые сейчас больше реальных размеров модели, хоть и помогают округлять в нужную сторону положение
        {                                              // TODO коннекторов, влияют на область, за которую деталь можно "схватить" указателем, так что возможно лучше просто делать приращение

           if (!_isSelected) return;

#if !UNITY_STANDALONE && !UNITY_EDITOR

            if (Input.touchCount > 1) return;
#endif

           var newPointerPos = Input.mousePosition;   

            if (_prevPointerPos == newPointerPos)
                return;

            _prevPointerPos = newPointerPos;

            // Определяем новое направление, которое указывает пользователь
            var newDirection = Camera.main.ScreenPointToRay(newPointerPos).direction;

            // Кидаем лучи, идущие  в новом направлении параллельно через все коннекторы детали и определяем самый короткий из них
            var minRayIndex = -1;
            RaycastHit? minRayHitInfo = null;
            LayerMask layerMask = ~(1 << LayerMask.NameToLayer("SelectedItem"));

            for (var i = 0; i < _raysOrigins.Length; i++)
            {
                RaycastHit hitInfo;

                if (Physics.Raycast(_raysOrigins[i], newDirection, out hitInfo, Mathf.Infinity, layerMask))
                {
                    if (minRayHitInfo == null || hitInfo.distance < minRayHitInfo.Value.distance)
                    {
                        minRayHitInfo = hitInfo;
                        minRayIndex = i;
                    }
                }
            }
            
            // Этот луч указывает новое положение соответствующего коннектора
            if (minRayHitInfo == null) 
                return;

            // Округляем координаты точки, вычисляем вектор переноса и перемещаем деталь
            var hitPoint = minRayHitInfo.Value.point;
            //var isCrossConnector = !(minRayIndex / _sizeX).IsOdd() && !(minRayIndex % _sizeZ).IsOdd(); Debug.Log(isCrossConnector + " " + minRayIndex);
//            var one =  _sizeX * ((minRayIndex / _sizeX) & 1);
//            var two = (minRayIndex - one) & 1;
//            var index = minRayIndex - one - two;
//            Debug.Log(minRayIndex + " " + " " + one + " " + two + " " + index);
//            var toNearest = _connectorsLocalPos[index] - _connectorsLocalPos[minRayIndex];
            var toZero = transform.TransformPoint(_connectorsLocalPos[0]) -
                         transform.TransformPoint(_connectorsLocalPos[minRayIndex]);
            var alignment = GetAlignment(hitPoint + toZero);
//            alignment += hitPoint.y < 0.5 ? new Vector3(0f, -alignment.y, 0f) : Vector3.zero;
//            if (hitPoint.y < 0.5)
//                alignment.y = 0;
            var newPos = hitPoint + alignment/* + minRayHitInfo.Value.normal*/;
//            Debug.Log("Hit: " + hitPoint + ", Pos: " + newPos + ", Alignment: " + alignment + ", connector: " + minRayIndex + ", toZero: " + toZero);
            var offset = newPos - transform.TransformPoint(_connectorsLocalPos[minRayIndex]);

            if (offset == Vector3.zero) return;
//            else Debug.Log("Offset: " + offset);

//            Debug.Log("Before: " + transform.position);

            transform.Translate(offset, Space.World);

//            Debug.Log("After: " + transform.position + " " + HasConnection() + " " + alignment.y);

            if (!HasConnection() && hitPoint.y > 0)
            {
                transform.Translate(-offset, Space.World);
//                Debug.Log("Done! " + transform.position);
            }
        }

        private Vector3 GetAlignment(Vector3 vector)
        {
            return vector.AlignAsCross() - vector;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            
        }

        // Update is called once per frame
        private void Update()
        {
//            for (var i = 0; i < _raysOrigins.Length; i++) {
//                Debug.DrawRay(_raysOrigins[i], transform.TransformPoint(_connectorsLocalPos[i]) - _raysOrigins[i], Color.red, 0.1f);
//            }
//            Debug.DrawRay(transform.TransformPoint(-1, 1, 0), Vector3.up * 10, Color.red, 1);
//            if (IsSelected)
//                Debug.Log(HasConnection());
        }

        public void OnPointerClick(PointerEventData eventData)
        {
//            if (IsSelected) return; 
//
//            IsSelected = true;

            //TODO переделать?
            FindObjectOfType<ApplicationController>().SelectedDetail = this;

//            mat.SetFloat("_intencity");
        }

        void CreateLineMaterial() {
            if (!_selectedMaterial) {
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

        void OnRenderObject() {

            if (!_isSelected) return;

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
                    var begin = new Vector3(ext.x * (i.IsOdd() ? 1 : -1), ext.y * (i / 2 < 1 ? 1 : -1), ext.z * ((3 + i) % 3 == 0 ? 1 : -1));
                    var beginGL = transform.TransformPoint(begin);
                    for (var j = 0; j < 3; j++) {
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

        public bool HasConnection()
        {
            var col = GetComponent<BoxCollider>();
            LayerMask layerMask = ~(1 << LayerMask.NameToLayer("SelectedItem") | 1 << LayerMask.NameToLayer("Workspace"));

            var neighbors = Physics.OverlapBox(col.bounds.center, col.bounds.extents, Quaternion.identity, layerMask); //TODO если будет жрать память, то можно заменить на NonAlloc версию
            string list = string.Empty;
            foreach (var neighbor in neighbors)
            {
                if (neighbor == col) continue;

                var colMin = col.bounds.min;
                var neighborMin = neighbor.bounds.min;
                var colMax = col.bounds.max;
                var neighborMax = neighbor.bounds.max;

                var intersectionMin = new Vector3(Mathf.Max(colMin.x, neighborMin.x), Mathf.Max(colMin.y, neighborMin.y), Mathf.Max(colMin.z, neighborMin.z));
                var intersectionMax = new Vector3(Mathf.Min(colMax.x, neighborMax.x), Mathf.Min(colMax.y, neighborMax.y), Mathf.Min(colMax.z, neighborMax.z));

                var v = intersectionMax - intersectionMin; // у всех координат размеры больше 1 и минимум у двух координат размеры больше 2
                var test = Mathf.Min(v.x, 2) + Math.Min(v.y, 2) + Math.Min(v.z, 2) > 5;

//                Debug.DrawLine(intersectionMin, intersectionMax, Color.red);
//                Debug.Log(v + " " + test);

                if (test)
                    return true;



                list += neighbor.gameObject.name + " ";
            }
//            if (!string.IsNullOrEmpty(list))
//                Debug.Log(list + ", center = " + col.bounds.center + ", extents = " + col.bounds.extents);

            return false;
        }
    }
}