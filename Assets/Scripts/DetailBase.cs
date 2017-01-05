using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


namespace Assets.Scripts {
    public abstract class DetailBase : MonoBehaviour
    {
        public virtual bool IsSelected {
            get { return _isSelected; }
            set {
                if (!value && transform.parent == null) {
                    UpdateConnections();
                }
                _isSelected = value;
                gameObject.layer = LayerMask.NameToLayer(_isSelected ? "SelectedItem" : "Default");
            }
        }
        private bool _isSelected;

        public abstract Bounds Bounds { get; }

//        public DetailsGroup RootParent {
//            get { return transform.root.GetComponent<DetailsGroup>(); }
//            set {
//                if (transform.parent != null) {
//                    transform.parent.GetComponent<DetailsGroup>().ChildDetached();
//                }
//                transform.SetParent(value != null ? value.transform : null);
//            }
//        }

        public abstract List<Detail> GetConnections(Vector3? pos = null);
        public abstract void UpdateConnections();

        public abstract void SetRaycastOrigins(Vector3 offset);
        public abstract RaycastHit? CastDetailAndGetClosestHit(Vector3 direction, out Detail raycaster, out int rayOriginIndex);

        public abstract void Rotate(Vector3 axis);
    }
}
