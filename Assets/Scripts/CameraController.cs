using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace Assets.Scripts {

    public class CameraController : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public Camera Camera;
	    public Transform DirectionalLight;
	    public Transform DirectionalLight1;
	    public Vector3 LightRotation;
	    public Vector3 LightRotation1;

        public float RotationFloorSpeed = 0.3f;
        public float TiltingFloorSpeed = 0.3f;

        public float TiltingFloorAngelMax = 170f;
        public float TiltingFloorAngelMin = 10f;

        public float FocusingSpeed = 3f;
        public float MovementSpeed = 600f;

        public float PinchZoomSpeed = 0.5f;
        public float ScrollZoomSpeed = 0.5f;

        public float MaxDistance = 100f;
        public float MinDistance = 5f;
        public float MinHeight = 1f;

        public GameObject FocusSetter;
        public Image SetFocusButtonImage;

        public Vector3 Focus
        {
            get { return _focus; }
            set
            {
                _focus = value;
                _isFocusing = true;
            }
        }

        private Vector3 _focus;
        private bool _isFocusing;

        private Vector3 _prevMousePos;


        private void Start()
        {
            Focus = Vector3.zero;
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

        public void OnDrag(PointerEventData eventData) {

#if !UNITY_STANDALONE && !UNITY_EDITOR

            if (Input.touchCount > 1) return;
#endif

            var newMousePos = Input.mousePosition;
            var offset = newMousePos - _prevMousePos;

            var v1 = Camera.transform.position - _focus;

            var axis = Vector3.Cross(v1, Vector3.up);
            var curr = Vector3.Angle(v1, Vector3.up);
            var angel = offset.y * TiltingFloorSpeed;
            var alpha = 180 - Mathf.Acos(Mathf.Min((_focus.y - MinHeight) / v1.magnitude, 1)) * Mathf.Rad2Deg;

            if (Math.Abs(angel) > 0)
            {
                angel = angel > 0
                    ? Math.Min(Mathf.Min(TiltingFloorAngelMax, alpha) - curr, angel)
                    : Math.Max(angel, TiltingFloorAngelMin - curr);
            }
           

            Camera.transform.RotateAround(_focus, axis, -angel);
            Camera.transform.RotateAround(_focus, Vector3.up, offset.x * RotationFloorSpeed);

			SetLight();

            _prevMousePos = newMousePos;
        }

	    private void SetLight()
	    {
			var sightDirection = Camera.transform.forward;
		    var lookRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(sightDirection, Vector3.up));

			DirectionalLight.rotation = lookRotation *  Quaternion.Euler(LightRotation);
			DirectionalLight1.rotation = lookRotation * Quaternion.Euler(LightRotation1);
	    }

        private void Focusing()
        {
            if (!_isFocusing) return;

            var cameraPos = Camera.transform.position;
            var targetDirection = Focus - cameraPos;
            var newDirection = Vector3.RotateTowards(Camera.transform.forward, targetDirection,
                Time.deltaTime * FocusingSpeed, Mathf.Infinity);

            Camera.transform.LookAt(cameraPos + newDirection);
			SetLight();

            var distanceExcess = Mathf.Max((Focus - cameraPos).magnitude - MaxDistance, 0f);
            var targetPosition = cameraPos + Camera.transform.forward * distanceExcess;

            targetPosition.y = Mathf.Max(MinHeight, targetPosition.y);

            var newPosition = Vector3.MoveTowards(cameraPos, targetPosition, Time.deltaTime * MovementSpeed);

            Camera.transform.position = newPosition;

            if (newDirection == targetDirection && newPosition == targetPosition)
            {
                _isFocusing = false;
            }
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
            var maxDistance = curr > 90 ? (_focus.y - MinHeight) / (Mathf.Cos((180 - curr) * Mathf.Deg2Rad)) : MaxDistance;
            var newDistance = Mathf.Clamp(oldCameraPos.magnitude + distanceDelta, MinDistance, maxDistance);

            Camera.transform.position = _focus + oldCameraPos.normalized * newDistance;
        }
    }
}
