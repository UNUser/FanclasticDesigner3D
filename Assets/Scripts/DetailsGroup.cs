using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


namespace Assets.Scripts {
    public class DetailsGroup : DetailBase {

        public override bool IsSelected
        {
            get
            {
                return base.IsSelected;
            }
            set
            {
                base.IsSelected = value;
                foreach (Transform child in transform)
                {
                    child.GetComponent<DetailBase>().IsSelected = value;
                }
            }
        }

        //        private readonly List<IMovable> _childs = new List<IMovable>();

//        private bool _isSelected;
        public override Bounds Bounds
        {
            get
            {
                var combinedBounds = new Bounds();
                combinedBounds.center = transform.position;
                foreach (Transform child in transform)
                {
                    combinedBounds.Encapsulate(child.GetComponent<DetailBase>().Bounds);
                }
                return combinedBounds;
            }
        }


        public void ChildDetached()
        {
            if (transform.childCount == 1) {
                var lastChild = transform.GetChild(0).GetComponent<IMovable>();

                lastChild.Parent = transform.parent.GetComponent<DetailsGroup>();
                Destroy(gameObject);
            }
        }

        public override void UpdateConnections()
        {
            
        }

        public override void SetRaycastOrigins(Vector3 offset) {

            foreach (Transform child in transform) {
                child.GetComponent<DetailBase>().SetRaycastOrigins(offset);
            }
        }

        public override RaycastHit? CastDetailAndGetClosestHit(Vector3 direction, out Detail raycaster, out int rayOriginIndex)
        {
            RaycastHit? minRayHitInfo = null;

            rayOriginIndex = -1;
            raycaster = null;

            foreach (Transform child in transform)
            {
                Detail childRaycaster;
                int childRayOriginIndex;

                var childHitInfo = child.GetComponent<DetailBase>()
                    .CastDetailAndGetClosestHit(direction, out childRaycaster, out childRayOriginIndex);

                if (childHitInfo == null) continue;
                if (minRayHitInfo != null && minRayHitInfo.Value.distance <= childHitInfo.Value.distance) continue;

                minRayHitInfo = childHitInfo;
                raycaster = childRaycaster;
                rayOriginIndex = childRayOriginIndex;
            }

            return minRayHitInfo;
        }

        public override void Rotate(Vector3 axis)
        {
            transform.Rotate(axis, 90, Space.World);

            var bottomDetail = GetBottomDetail();
            var newPos = bottomDetail.transform.position;

            bottomDetail.AlignPosition(ref newPos);

            var offset = newPos - bottomDetail.transform.position;
            transform.Translate(offset, Space.World);
        }

        public override List<Transform> GetConnections(Vector3? pos = null) 
        {
            throw new System.NotImplementedException();
        }


        private Detail GetBottomDetail()
        {
            var bounds = Bounds;

            bounds.center += Vector3.down * (bounds.extents.y);
            bounds.extents += Vector3.down * (bounds.extents.y - 0.25f);

            var details = Physics.OverlapBox(bounds.center, bounds.extents);

            if (details == null)
            {
                Debug.LogError("Wrong bounds in details group!");
                return null;
            }

            foreach (var detail in details)
            {
                if (detail.transform.RootParent() != this) continue;

                return detail.GetComponent<Detail>();
            }

            Debug.LogError("Wrong bounds in details group!");
            return null; 
        }
    }
}
