using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts {

    class Base : MonoBehaviour, IPointerDownHandler, IDragHandler
    {

        public float SpeedX = 1;
        public float SpeedY = 1;

        private Vector3 _prevMousePos;


        private void Start()
        {
            Camera.main.transform.RotateAround(Vector3.zero, Vector3.right, 45);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _prevMousePos = Input.mousePosition;
        }

        public void OnDrag(PointerEventData eventData)
        {

            var newMousePos = Input.mousePosition;
            var offset = newMousePos - _prevMousePos;

            var v1 = Camera.main.transform.position;
            var v2 = Vector3.ProjectOnPlane(v1, Vector3.up);

            var normal = Vector3.Cross(v1, v2);
            var curr = Vector3.Angle(Camera.main.transform.position, Vector3.up);
            var angel = offset.y * SpeedY;

            if (Math.Abs(angel) > 0)
            {
                angel = angel > 0                    //Mathf.Clamp заменить
                    ? Math.Min(80 - curr, angel)
                    : Math.Max(angel, 10 - curr);
            }
   
            Camera.main.transform.RotateAround(Vector3.zero, normal, angel);
            Camera.main.transform.RotateAround(Vector3.zero, Vector3.up, offset.x * SpeedX);

            _prevMousePos = newMousePos;
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
