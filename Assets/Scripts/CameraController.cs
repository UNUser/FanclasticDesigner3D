using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace Assets.Scripts {

    public class CameraController : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public Camera Camera;
        public float SpeedX = 1;
        public float SpeedY = 1;

        public float FocusingSpeed = 3f;

        public GameObject FocusSetter;
        public Image SetFocusButtonImage;

        public Vector3 Focus
        {
            get { return _focus; }
            set
            {
                _prevFocus = _focus;
                _focus = value;
                _focusingProgress = 0f;
            }
        }

        private Vector3 _focus;
        private Vector3 _prevFocus;
        private float _focusingProgress = 1f;

        private Vector3 _prevMousePos;


        private void Start()
        {
            Focus = Vector3.zero;
            _prevFocus = Focus;
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

            var v1 = Camera.transform.position - _focus;

            var axis = Vector3.Cross(v1, Vector3.up);
            var curr = Vector3.Angle(v1, Vector3.up);
            var angel = offset.y * SpeedY;
            var alpha = 180 - Mathf.Acos(Mathf.Min((_focus.y - 1) / v1.magnitude, 1)) * Mathf.Rad2Deg;

            if (Math.Abs(angel) > 0)
            {
                angel = angel > 0
                    ? Math.Min(Mathf.Min(170, alpha) - curr, angel)
                    : Math.Max(angel, 10 - curr);
            }
           

            Camera.transform.RotateAround(_focus, axis, -angel);
            Camera.transform.RotateAround(_focus, Vector3.up, offset.x * SpeedX);

            _prevMousePos = newMousePos;
        }

        public float PinchZoomSpeed = 0.5f;
        public float ScrollZoomSpeed = 0.5f;

        public float MaxDistance = 100f;
        public float MinDistance = 5f;

        private void Focusing()
        {
            if (_focusingProgress >= 1f) return;

            _focusingProgress += Time.deltaTime * FocusingSpeed;
            var currentPoint = Vector3.Lerp(_prevFocus, Focus, _focusingProgress); //TODO тут нужно лерпать угол, а то если из далека идет, то неравномерно получается, в конце быстро
            Camera.transform.LookAt(currentPoint);
        }

        private void Update() {

            Focusing();

            var oldCameraPos = Camera.transform.position - _focus;
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
            var curr = Vector3.Angle(oldCameraPos, Vector3.up);
            var maxDistance = curr > 90 ? (_focus.y - 1) / (Mathf.Cos((180 - curr) * Mathf.Deg2Rad)) : MaxDistance;
            var newDistance = Mathf.Clamp(oldCameraPos.magnitude + distanceDelta, MinDistance, maxDistance);

            Camera.transform.position = _focus + oldCameraPos.normalized * newDistance; //Debug.Log(distanceDelta + " " + oldCameraPos.magnitude + " " + _camera.transform.position.magnitude);
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
