using UnityEngine;

namespace Assets.Scripts {

    class CameraZoomController : MonoBehaviour {
        public float PinchZoomSpeed = 0.5f;
        public float ScrollZoomSpeed = 0.5f;

        public float MaxDistance = 100f;
        public float MinDistance = 5f;


        private void Update() {

            var oldCameraPos = Camera.main.transform.position;
            float distanceDelta;

#if UNITY_STANDALONE || UNITY_EDITOR

            var scrollDelta = - Input.mouseScrollDelta.y;

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

            Camera.main.transform.position = oldCameraPos.normalized * newDistance;
        }
    }
}
