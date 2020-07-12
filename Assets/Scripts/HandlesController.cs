
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts
{
    public class HandlesController : GizmoBase, IPointerDownHandler, IPointerUpHandler
    {
        protected override GameObject RootObj
        {
            get { return gameObject; }
        }

        protected override Vector3 TargetPos
        {
            get { return _targetDetail.transform.TransformPoint(_pivotLocalPos);
}
        }

        protected override Quaternion TargetRotation
        {
            get { return _targetDetail.transform.rotation; }
        }

        private Detail _targetDetail;
        private Vector3 _pivotLocalPos;

        public void SetConnection(Detail detail, AxisConnection connection)
        {
            _targetDetail = detail;
            _pivotLocalPos = connection.PivotLocalPos;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            throw new System.NotImplementedException();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            throw new System.NotImplementedException();
        }
    }
}
