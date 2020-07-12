using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public class AxisMover : GizmoBase
    {
        public GameObject Root;
        public Canvas Canvas;

        public GameObject Rotator;
        public GameObject Mover;

        public int PointsCount;
        public FixedAxisRotator[] FixedAxisRotators;


        public Vector3 Correction { get; set; }

        protected override GameObject RootObj
        {
            get { return Root; }
        }

        protected override Vector3 TargetPos
        {
            get
            {
                var first = Selected.First;
                var bounds = first.Bounds;

                foreach (var detail in Selected.Selected) {
                    bounds.Encapsulate(detail.Bounds);
                }

                return bounds.center - Correction;
            }
        }

        protected override Quaternion TargetRotation
        {
            get
            {
                if (Mover.activeSelf)
                {
                    return Quaternion.identity;
                }

                var first = Selected.First;
                var firstGroup = first.Group;

                Transform targetTransform;

                if (Selected.Count == 1) {
                    targetTransform = first.transform;
                } else {
                    targetTransform = firstGroup == null ? first.transform : firstGroup.transform;
                }

                return targetTransform.rotation;
            }
        }

#if UNITY_EDITOR

        private int _prevPointsCount;

        protected void OnValidate() {
            if (_prevPointsCount == PointsCount) {
                return;
            }

            foreach (var fixedAxisRotator in FixedAxisRotators) {
                fixedAxisRotator.DrawArc(PointsCount);
            }

            _prevPointsCount = PointsCount;
        }

#endif
    }
}
