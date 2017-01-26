using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;


namespace Assets.Scripts {

    public class DetailLinks
    {
        public bool HasConnections { get { return Connections.Count > 0 || ImplicitConnections.Count > 0; } }

        public readonly HashSet<Detail> Connections;
        public readonly HashSet<Detail> Touches;
        public readonly HashSet<HashSet<Detail>> ImplicitConnections;

        public DetailLinksData Data
        {
            get
            {
                var data = new DetailLinksData
                {
                    Connections = Connections.Select(connection => connection.GetInstanceID()).ToList(),
                    Touches = Touches.Select(touch =>touch.GetInstanceID()).ToList(),
                    ImplicitConnections = new List<List<int>>()
                };

                foreach (var implicitConnection in ImplicitConnections) {
                    data.ImplicitConnections.Add(implicitConnection.Select(connection => connection.GetInstanceID()).ToList());
                }

                return data;
            }
        }

        public DetailLinks()
        {
            Connections = new HashSet<Detail>();
            Touches = new HashSet<Detail>();
            ImplicitConnections = new HashSet<HashSet<Detail>>();
        }

        public bool HasWeakConnectionsWith(Detail detail, bool remove = false)
        {
            foreach (var implicitConnection in ImplicitConnections)
            {
                if (implicitConnection.Contains(detail))
                    ImplicitConnections.Remove(implicitConnection);
                    return true;
            }
            return false;
        }
    }

    [Serializable]
    public class DetailLinksData
    {
        public List<int> Connections;
        public List<int> Touches;
        public List<List<int>> ImplicitConnections;

        public override string ToString()
        {
            var str = new StringBuilder();
            var needComma = false;

            str.Append("Direct: ");
            foreach (var connection in Connections)
            {
                ApplicationController.AddComma(str, ref needComma);
                str.Append(connection);
            }

            str.Append(" Implicit: ");

            foreach (var @implicit in ImplicitConnections) {
                str.Append("   [ ");
                needComma = false;
                foreach (var detail in @implicit)
                {
                    ApplicationController.AddComma(str, ref needComma);
                    str.Append(detail);
                }
                str.Append(" ]");
            }

            str.Append(" Touches: ");
            needComma = false;

            foreach (var touch in Touches) {
                ApplicationController.AddComma(str, ref needComma);
                str.Append(touch);
            }

            return str.ToString();
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
        public abstract DetailLinks GetLinks(Vector3? pos = null, bool respectSelected = false);
        public abstract void UpdateLinks(DetailLinks newLinks = null, bool respectSelected = false);

        public abstract void SetRaycastOrigins(Vector3 offset);
        public abstract RaycastHit? CastDetailAndGetClosestHit(Vector3 direction, out Detail raycaster, out int rayOriginIndex);

        public abstract void Rotate(Vector3 axis);
    }
}
