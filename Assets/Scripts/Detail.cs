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
    public class Detail : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IDragHandler
    {
        private int _sizeX;
        private int _sizeZ;
        private Vector3[] _raysOrigins;
        private Vector3[] _connectorsLocalPos;
        private int _holdingConnector;
        private Vector3 _prevPointerPos;

        private void Awake()
        {
            // Определяем по размерам коллайдера тип детали и локальные координаты всех ее коннекторов
            var collider = GetComponent<Collider>();

            _sizeX = (int) (collider.bounds.size.x + 1) / 2;
            _sizeZ = (int) (collider.bounds.size.z + 1) / 2;

            var totalConnectors = _sizeX * _sizeZ + (_sizeX - 1) * (_sizeZ - 1);

            _raysOrigins = new Vector3[totalConnectors];
            _connectorsLocalPos = new Vector3[totalConnectors];

            for (var i = 0; i < _connectorsLocalPos.Length; i++)
            {
                var m = 2 * _sizeX - 1;
                var y = Index2Row(i);
                var offsetX = y.IsOdd() ? 1 : 0;
                var x = Index2Column(i) * 2 + offsetX;
                Debug.Log(x + " " + y);
                _connectorsLocalPos[i] = new Vector3(x - _sizeX + 1,
                                                  - (y - _sizeZ + 1));
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

        public void OnPointerDown(PointerEventData eventData)
        {
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
            gameObject.layer = LayerMask.NameToLayer("SelectedItem");

            //TODO переделать?
            FindObjectOfType<ApplicationController>().SelectedDetail = this;
        }

        public void Rotate(Vector3 axis)
        {
            transform.Rotate(axis, 90);

            var deltaFirst = transform.TransformPoint(_connectorsLocalPos[0]).y;
            var deltaLast = transform.TransformPoint(_connectorsLocalPos[_connectorsLocalPos.Length - 1]).y;
            var deltaMin = Mathf.Min(deltaFirst, deltaLast);

            if (deltaMin < 0)
            {
                transform.Translate(Vector3.up * (int) -deltaMin, Space.World);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
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
            var isCrossConnector = !Index2Row(minRayIndex).IsOdd(); Debug.Log(isCrossConnector + " " + minRayIndex);
            var newPos = isCrossConnector ? hitPoint.AlignAsCross() : hitPoint.AlignAsSquare()/* + minRayHitInfo.Value.normal*/;
            var offset = newPos - transform.TransformPoint(_connectorsLocalPos[minRayIndex]);

            transform.Translate(offset, Space.World);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            gameObject.layer = LayerMask.NameToLayer("Default");
        }

        // Update is called once per frame
        private void Update()
        {
            for (var i = 0; i < _raysOrigins.Length; i++) {
                Debug.DrawRay(_raysOrigins[i], transform.TransformPoint(_connectorsLocalPos[i]) - _raysOrigins[i], Color.red, 0.1f);
            }
//            Debug.DrawRay(transform.TransformPoint(-1, 1, 0), Vector3.up * 10, Color.red, 1);
            
        }
    }
}