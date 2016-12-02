using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts {

    class Base : MonoBehaviour, IPointerDownHandler, IDragHandler
    {

        public float SpeedX = 1;
        public float SpeedY = 1;

        public GameObject FocusSetter;
        public Image SetFocusButtonImage;

        public Vector3 Focus
        {
            get { return _focus; }
            set
            {
                _focus = value;
                _camera.transform.LookAt(_focus);
            }
        }

        private Vector3 _focus;
        private Vector3 _prevMousePos;
        private Camera _camera;


        private void Start()
        {
            _camera = Camera.main;
            _camera.transform.RotateAround(Vector3.zero, Vector3.right, 45);


            var mesh = GetComponent<MeshFilter>();
            var normals = new List<Vector3>();

//            foreach (var normal in mesh.mesh.normals) {
//                Debug.Log("Before: " + normal);
//            }

            foreach (var normal in mesh.mesh.normals)
            {
                normals.Add(-normal);
            }
            mesh.mesh.SetNormals(normals);
//            mesh.mesh.RecalculateNormals();

//            foreach (var normal in mesh.mesh.normals) {
//                Debug.Log("After: " + normal);
//            }
//            foreach (var normal in normals) {
//                Debug.Log("Normals: " + normal);
//            } 
        }

        public void OnSetFocusButtonClick()
        {
            FocusSetter.SetActive(true);
            SetFocusButtonImage.color = Color.red;
        }

        public void OnFocusPointed()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if (!Physics.Raycast(ray, out hitInfo))
                return;

            Focus = hitInfo.point;
            FocusSetter.SetActive(false);
            SetFocusButtonImage.color = Color.white;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _prevMousePos = Input.mousePosition;
        }

        public void OnDrag(PointerEventData eventData)
        {

            var newMousePos = Input.mousePosition;
            var offset = newMousePos - _prevMousePos;

            var v1 = _camera.transform.position - _focus;
            var v2 = Vector3.ProjectOnPlane(v1, Vector3.up);

            var axis = Vector3.Cross(v1, v2);
            var curr = Vector3.Angle(v1, Vector3.up);
            var angel = offset.y * SpeedY;

            if (Math.Abs(angel) > 0)
            {
                angel = angel > 0                    //Mathf.Clamp заменить
                    ? Math.Min(80 - curr, angel)
                    : Math.Max(angel, 10 - curr);
            }
   
            _camera.transform.RotateAround(_focus, axis, angel);
            _camera.transform.RotateAround(_focus, Vector3.up, offset.x * SpeedX);

            _prevMousePos = newMousePos;
        }

        public float PinchZoomSpeed = 0.5f;
        public float ScrollZoomSpeed = 0.5f;

        public float MaxDistance = 100f;
        public float MinDistance = 5f;


        private void Update() {

            var oldCameraPos = _camera.transform.position - _focus;
            float distanceDelta;

#if UNITY_STANDALONE || UNITY_EDITOR

            var scrollDelta = -Input.mouseScrollDelta.y;

            if (scrollDelta == 0) return;

            distanceDelta = scrollDelta * ScrollZoomSpeed;

#else

            // If there are two touches on the device...
            if (Input.touchCount != 2) return;
            
            // Store both touches.
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Find the position in the previous frame of each touch.
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // Find the magnitude of the vector (the distance) between the touches in each frame.
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // Find the difference in the distances between each frame.
            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            distanceDelta = deltaMagnitudeDiff * PinchZoomSpeed;
            
#endif

            var newDistance = Mathf.Clamp(oldCameraPos.magnitude + distanceDelta, MinDistance, MaxDistance);

            _camera.transform.position = _focus + oldCameraPos.normalized * newDistance; //Debug.Log(distanceDelta + " " + oldCameraPos.magnitude + " " + _camera.transform.position.magnitude);
        }


//        void OnRenderObject() {
////            CreateLineMaterial();
////            lineMaterial.SetPass(0);
//
//            var rows = 64;
//            var columns = 64;
//
//            GL.PushMatrix();
//            GL.Begin(GL.LINES);
//            GL.Color(Color.grey);
//            /* Horizontal lines. */
//            for (int i = -rows / 2; i <= rows / 2; i++) {
//                GL.Vertex3(-columns / 2, 0, i);
//                GL.Vertex3(columns / 2, 0, i);
//            }
//            /* Vertical lines. */
//            for (int i = -columns / 2; i <= columns / 2; i++) {
//                GL.Vertex3(i, 0, -rows);
//                GL.Vertex3(i, 0, rows);
//            }
//            GL.End();
//            GL.PopMatrix();
//        }


    }
}
