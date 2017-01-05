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

        public int DetailsCount
        {
            get { return _connections.Count; }
        }

        private readonly Dictionary<Detail, List<Detail>> _connections = new Dictionary<Detail, List<Detail>>();

        public void Add (Detail detail, List<Detail> neighbors)
        {
//            List<Detail> value;

//            if (_connections.TryGetValue(detail, out value))
//            {
//                Debug.LogError("Detail already belong the group!");
//            }
            neighbors = neighbors ?? new List<Detail>();

            _connections.Add(detail, neighbors);
            foreach (var neighbour in neighbors)
            {
                _connections[neighbour].Add(detail);
            }
            detail.transform.SetParent(transform);
        }

        public void Consume(DetailsGroup group, Dictionary<Detail, List<Detail>> connections)
        {
            connections = connections ?? new Dictionary<Detail, List<Detail>>();

            foreach (var connection in group._connections)
            {
                List<Detail> newNeighbours;

                if (connections.TryGetValue(connection.Key, out newNeighbours))
                {
                    connection.Value.AddRange(newNeighbours);
                    foreach (var newNeighbour in newNeighbours)
                    {
                        _connections[newNeighbour].Add(connection.Key);
                    }
                }
                _connections.Add(connection.Key, connection.Value);
                connection.Key.transform.SetParent(transform);
            }
            Destroy(group.gameObject);
        }

        public void Remove(Detail detail)
        {
            var neighbours = _connections[detail];

            foreach (var neighbour in neighbours)
            {
                _connections[neighbour].Remove(detail);
            }
            _connections.Remove(detail);
            detail.transform.SetParent(null);

            if (DetailsCount == 1) {
                var lastChild = transform.GetChild(0);

                lastChild.SetParent(null);
                Destroy(gameObject);
            }

        }

        public void UpdateConnections(Detail detail, List<Detail> newNeighbours)
        {
            foreach (var oldNeighbour in _connections[detail])
            {
                if (!newNeighbours.Contains(oldNeighbour))
                {
                    _connections[oldNeighbour].Remove(detail);
                }
            }

            _connections[detail] = newNeighbours;
        }

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

        public override List<Detail> GetConnections(Vector3? pos = null) 
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

//        public void CombineChildren() {
//            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
//            CombineInstance[] combine = new CombineInstance[meshFilters.Length];
//            int i = 0;
//            while (i < meshFilters.Length) {
//                combine[i].mesh = meshFilters[i].sharedMesh;
//                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
////                meshFilters[i].gameObject.SetActive(false);
//                i++;
//            }
//            var meshFilter = transform.GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
//
//            if (meshFilter == null)
//                meshFilter = gameObject.AddComponent<MeshFilter>();
//
//            Debug.Log(meshFilter);
//             
//            meshFilter.mesh = new Mesh();
//            meshFilter.mesh.CombineMeshes(combine);
//            transform.gameObject.SetActive(true);
//        }
    }
}
