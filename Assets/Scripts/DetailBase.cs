using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


namespace Assets.Scripts {

    public class DetailLinks
    {
        public bool HasConnections { get { return Connections.Count > 0 || ImplicitConnections.Count > 0; } }

        public readonly HashSet<Detail> Connections;
        public readonly HashSet<Detail> Touches;
        public readonly HashSet<HashSet<Detail>> ImplicitConnections;

        public DetailLinks()
        {
            Connections = new HashSet<Detail>();
            Touches = new HashSet<Detail>();
            ImplicitConnections = new HashSet<HashSet<Detail>>();
        }

        public bool HasWeakConnectionsWith(Detail detail)
        {
            foreach (var implicitConnection in ImplicitConnections)
            {
                if (implicitConnection.Contains(detail))
                    return true;
            }
            return false;
        }
    }

    public abstract class DetailBase : MonoBehaviour
    {
        public virtual bool IsSelected {
            get { return _isSelected; }
            set {
                if (!value && transform.parent == null) {
                    UpdateLinks();
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

        public abstract void Detach();
        public abstract DetailLinks GetLinks(Vector3? pos = null);
        public abstract void UpdateLinks(DetailLinks newLinks = null);

        public abstract void SetRaycastOrigins(Vector3 offset);
        public abstract RaycastHit? CastDetailAndGetClosestHit(Vector3 direction, out Detail raycaster, out int rayOriginIndex);

        public abstract void Rotate(Vector3 axis);
    }
}
