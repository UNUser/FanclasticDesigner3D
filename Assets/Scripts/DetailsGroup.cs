using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace Assets.Scripts {
    public class DetailsGroup : DetailBase {

		public override bool IsSelected {
			get {
				return _details.All(detail => detail.IsSelected);
			}
			set {
				foreach (var detail in _details) {
					detail.IsSelected = value;
				}
			}
		}

		private readonly HashSet<Detail> _details = new HashSet<Detail>(); 

        public int DetailsCount
        {
            get { return transform.childCount; }
        }

        public void Add (Detail detail)
        {
	        _details.Add(detail);
            detail.transform.SetParent(transform);
        }

        public static DetailsGroup CreateNewGroup(Vector3? position = null)
        {
            var newObj = new GameObject("DetailsGroup");
            newObj.transform.position = position ?? Vector3.zero;

            return newObj.AddComponent<DetailsGroup>();
        }

        private List<HashSet<Detail>> InitLastAdded(Detail detachedDetail, List<HashSet<Detail>> subgroups, Dictionary<HashSet<Detail>, Detail> unconfirmed)
        {
            var lostLinks = detachedDetail.Links;

            var directlyAdded = new List<HashSet<Detail>>();
            var implicitlyAdded = new HashSet<Detail>();

            // Подгруппы, порожденные прямыми связями
            foreach (var lostConnection in lostLinks.Connections)
            {
                directlyAdded.Add(new HashSet<Detail> { lostConnection });
                subgroups.Add(new HashSet<Detail>());
            }

            // Подгруппы, порожденные деталями, которые имели неявную связь с удаляемой деталью
            foreach (var touch in lostLinks.Touches) {
                if (touch.Links.HasWeakConnectionsWith(detachedDetail, true)) {
                    directlyAdded.Add(new HashSet<Detail> { touch });
                    subgroups.Add(new HashSet<Detail>());
                }
            }

            // Подгруппы, порожденные возможными изолированными группами деталей, которые были
            // связаны с основной группой только через неявную связь с удаляемой деталью
            InitLastAddedWithImplicitConnections(lostLinks, subgroups, directlyAdded);

            detachedDetail.UpdateLinks(new List<LinksBase>{ new LinksBase(TODO, detachedDetail) });    // TODO тут этого быть не должно! переделать!

//            DebugPrint(subgroups, directlyAdded);

            return MergeAllSubgroups(subgroups, directlyAdded, implicitlyAdded, unconfirmed);
        }

        private void InitLastAddedWithImplicitConnections(LinksBase lostLinksBase, List<HashSet<Detail>> subgroups,
            List<HashSet<Detail>> directlyAdded)
        {
            if (lostLinksBase.ImplicitConnections.Count < 2) return;

            // Нужно разбить неявные соединения на непересекающиеся подгруппы
            var connections = lostLinksBase.ImplicitConnections.ToList();
            var result = new HashSet<HashSet<HashSet<Detail>>>();

            for (var i = 0; i < connections.Count; i++) {

                var targetSubgroup = result.FirstOrDefault(subgroup => subgroup.Contains(connections[i]));

                if (targetSubgroup == null) {
                    targetSubgroup = new HashSet<HashSet<Detail>> { connections[i] };
                    result.Add(targetSubgroup);
                }

                for (var j = i + 1; j < connections.Count; j++) {
                    if (connections[i].Overlaps(connections[j])) {
                        targetSubgroup.Add(connections[j]);
                    }
                }
            }

            // Добавить по одному соединению из каждой такой независимой подгруппы, если их больше одной
            if (result.Count < 2) return;

            foreach (var subgroup in result) {
                var connection = subgroup.First();

                foreach (var detail in connection) {
                    directlyAdded.Add(new HashSet<Detail> { detail });
                    subgroups.Add(new HashSet<Detail>());
                }
            }
        }

        public void UpdateContinuity(Detail detachedDetail)
        {
            var subgroups = new List<HashSet<Detail>>();
            var unconfirmedImplicitConnections = new Dictionary<HashSet<Detail>, Detail>();

            var lastAdded = InitLastAdded(detachedDetail, subgroups, unconfirmedImplicitConnections);
            var detailsAdded = lastAdded.Sum(newDetails => newDetails.Count);

//            Debug.Log("/////////////////");

            while (detailsAdded > 0 && subgroups.Count > 1)
            {
                var directlyAdded = new List<HashSet<Detail>>();
                var implicitlyAdded = new HashSet<Detail>();

                // добавляем в подгруппы следующий уровень соединений
                for (var i = 0; i < lastAdded.Count; i++) {

                    directlyAdded.Add(new HashSet<Detail>());

                    // пробегаем по всем последним добавленным в подгруппу деталям
                    foreach (var detail in lastAdded[i]) {

                        // добавляем детали по прямым связям
                        directlyAdded[i].UnionWith(detail.Connections);
                            
                        // добавляем детали по неявным связям
                        foreach (var touch in detail.Touches) {
                            if (touch.Links.HasWeakConnectionsWith(detail)) {
                                implicitlyAdded.Add(touch);
                            }
                        }
                    }
                }

                lastAdded = MergeAllSubgroups(subgroups, directlyAdded, implicitlyAdded, unconfirmedImplicitConnections); // TODO можно оптимизировать, подгруппы, в которые ничего не добавляется, можно отделять сразу, хотя, возможно, они могут присоединится неявными связями
                detailsAdded = lastAdded.Sum(newDetails => newDetails.Count);
            }

            SplitAndUpdate(subgroups, unconfirmedImplicitConnections);
        }

        private void DebugPrint(List<HashSet<Detail>> subgroups, List<HashSet<Detail>> lastAdded)
        {
            StringBuilder str = new StringBuilder();

            str.Append("subgroups: ");

            for (var i = 0; i < subgroups.Count; i++)
            {
                var subgroup = subgroups[i];
                var commaNeeded = false;

                str.Append("{ ");
                foreach (var detail in subgroup)
                {
                    if (lastAdded[i].Contains(detail)) continue;

                    if (commaNeeded)
                        str.Append(", ");
                    else
                        commaNeeded = true;
                    
                    str.Append(detail.gameObject.name);
                }

                commaNeeded = false;

                str.Append(" [ ");
                foreach (var detail in lastAdded[i]) {
                    if (commaNeeded)
                        str.Append(", ");
                    else
                        commaNeeded = true;

                    str.Append(detail.gameObject.name);
                }
                str.Append(" ] } ");
            }

            Debug.Log(str);
        }

        private void SplitAndUpdate(List<HashSet<Detail>> subgroups, Dictionary<HashSet<Detail>, Detail> unconfirmed)
        {
            for (var i = 1; i < subgroups.Count; i++) {
                var newGroup = subgroups[i].Count > 1 ? CreateNewGroup(Vector3.zero) : null;

                foreach (var detail in subgroups[i]) {
                    detail.Group = newGroup;
                }
            }

            foreach (var unconfirmedPair in unconfirmed) {
                unconfirmedPair.Value.Links.ImplicitConnections.Remove(unconfirmedPair.Key);
            }
        }

        private List<HashSet<Detail>> MergeAllSubgroups(List<HashSet<Detail>> subgroups, List<HashSet<Detail>> directlyAdded, HashSet<Detail> implicitlyAdded, Dictionary<HashSet<Detail>, Detail> unconfirmed)
        {
            var lastAdded = new List<HashSet<Detail>>();
            var newSubgroupsSpawners = new HashSet<Detail>(implicitlyAdded);

            // Отбираем в lastAdded новые детали, которых еще нет ни в одной подгруппе.
            for (var i = 0; i < subgroups.Count; i++)
            {
                // Для новых деталей, добавленных по прямым связям, проверяем соответствующую подгруппу
                lastAdded.Add(new HashSet<Detail>(directlyAdded[i]));
                lastAdded[i].ExceptWith(subgroups[i]);
                subgroups[i].UnionWith(lastAdded[i]);

                // Находим и запоминаем у деталей неявные соединения и детали, которые эти соединения образуют. Они, возможно, порождают новые подгруппы
                AddUnconfirmedAndSpawners(lastAdded[i], newSubgroupsSpawners, unconfirmed);
            }

            AddUnconfirmedAndSpawners(implicitlyAdded, newSubgroupsSpawners, unconfirmed);

            // Среди деталей, порождающих новые подгруппы, оставляем только те, которые не содержатся ни в одной из старых подгрупп
            var spawners = newSubgroupsSpawners.ToList();

            foreach (var subgroup in subgroups) {
                for (var spawnerIndex = 0; spawnerIndex < spawners.Count; spawnerIndex++) {
                    var detail = spawners[spawnerIndex];

                    if (subgroup.Contains(detail)) {
                        newSubgroupsSpawners.Remove(detail);
                        spawners.RemoveAt(spawnerIndex);
                        spawnerIndex--;
                    }
                }
            }

//            var sub = new List<HashSet<Detail>>(subgroups);
//            sub.AddRange(newSubgroupsSpawners.Select(detail => new HashSet<Detail> {detail}));
//            var last = new List<HashSet<Detail>>(lastAdded);
//            last.AddRange(newSubgroupsSpawners.Select(detail => new HashSet<Detail> { detail }));
//            DebugPrint(sub, last);

            // Объединяем подгруппы, которые пересеклись по прямым связям.
            // Поскольку на каждой итерации от каждой новой добавленной детали происходит добавление сразу всех деталей по всем возможным связям, 
            // то появление пересечений подгрупп (т.е. деталий, общих для несокольких подгрупп) возможно только среди новых добавленных деталей
            // и искать пересечения нужно только среди них, не затрагивая детали, добавленные в подгруппы до этого
            for (var i = 0; i < lastAdded.Count; i++) {
                for (var j = i + 1; j < subgroups.Count; j++) {

                    if (!subgroups[j].Overlaps(lastAdded[i])) continue;

                    MergeSubgroups(subgroups, i, j, lastAdded);
                    j--;
                }
            }
            
            var connections = unconfirmed.ToList();

            for (var subgroupIndex = 0; subgroupIndex < subgroups.Count; subgroupIndex++)
            {
                for (var connectionIndex = 0; connectionIndex < connections.Count; connectionIndex++)
                {
                    var keyValuePair = connections[connectionIndex];
                    var connection = keyValuePair.Key;
                    var detail = keyValuePair.Value;

                    // Подтверждаем неявное соединение
                    if (!subgroups[subgroupIndex].IsSupersetOf(connection)) continue;

                    // Убираем соединение из неподтвержденных
                    unconfirmed.Remove(connection);
                    connections.RemoveAt(connectionIndex);
                    connectionIndex--;

                    // Ищем подгруппу, в которую входит деталь с подтвержденным соединением

                    // Сначала ищем среди деталей, порождающих новые подгруппы
                    if (newSubgroupsSpawners.Contains(detail))
                    {
                        newSubgroupsSpawners.Remove(detail);
                        subgroups[subgroupIndex].Add(detail);
                        lastAdded[subgroupIndex].Add(detail);
                        continue;
                    }

                    // Если не нашли, то ищем по всем подгруппам
                    for (var detailIndex = 0; detailIndex < subgroups.Count; detailIndex++)
                    {
                        if (!subgroups[detailIndex].Contains(detail)) continue;
                        if (detailIndex == subgroupIndex) break;

                        // Объединяем подгруппы
                        MergeSubgroups(subgroups, subgroupIndex, detailIndex, lastAdded);

                        // Для объединенной подгруппы нужно проверить все соединения снова
                        connectionIndex = 0;

                        subgroupIndex--;
                    }
                }
            }

            // Добавляем новые подгруппы
            foreach (var detail in newSubgroupsSpawners) {
                lastAdded.Add(new HashSet<Detail>{detail});
                subgroups.Add(new HashSet<Detail>{detail});
            }

            return lastAdded;
        }

        private void AddUnconfirmedAndSpawners(HashSet<Detail> details, HashSet<Detail> spawners,
            Dictionary<HashSet<Detail>, Detail> unconfirmed)
        {
            // Все неявные связи новых деталей добавляем в группу неподтвержденных связей
            foreach (var detail in details) {
                foreach (var implicitConnection in detail.ImplicitConnections) {
                    if (unconfirmed.ContainsKey(implicitConnection))
                        break;
                    unconfirmed.Add(implicitConnection, detail);
                    // Все детали, образующие новые неявные соединения, добавляем в группу деталей, порождающих новые подгруппы
                    foreach (var connectionDetail in implicitConnection) {
                        spawners.Add(connectionDetail);
                    }
                }
            }
        }

        /// <summary>
        /// Объединяет две подгруппы с индексами firstIndex и secondIndex в множество, у которого число эелементов больше, и 
        /// располагает его в списке под индексом firstIndex. Из списка удаляется подгруппа с индексом secondIndex.
        /// </summary>
        private void MergeSubgroups(List<HashSet<Detail>> subgroups, int firstIndex, int secondIndex, List<HashSet<Detail>> lastAdded)
        {
            var targetSubgroupIndex = subgroups[firstIndex].Count > subgroups[secondIndex].Count ? firstIndex : secondIndex;
            var sourceSubgroupIndex = targetSubgroupIndex == secondIndex ? firstIndex : secondIndex;

            subgroups[targetSubgroupIndex].UnionWith(subgroups[sourceSubgroupIndex]);
            lastAdded[targetSubgroupIndex].UnionWith(lastAdded[sourceSubgroupIndex]);

            if (firstIndex != targetSubgroupIndex) {
                subgroups[firstIndex] = subgroups[secondIndex];
                lastAdded[firstIndex] = lastAdded[secondIndex];
            }

            subgroups.RemoveAt(secondIndex);
            lastAdded.RemoveAt(secondIndex);

//            Debug.Log("Merged: " + firstIndex + " " + secondIndex);
//            DebugPrint(subgroups, lastAdded);
        }

        public static void Merge(HashSet<DetailsGroup> groups, HashSet<Detail> freeDetails)
        {
            DetailsGroup targetGroup = null;
            var toUpdateLinks = new HashSet<Detail>();

            foreach (var detailsGroup in groups) {
                if (targetGroup == null || targetGroup.DetailsCount < detailsGroup.DetailsCount) {
                    targetGroup = detailsGroup;
                }
            }

            if (targetGroup == null) {
                if (freeDetails.Count > 1) {
                    targetGroup = CreateNewGroup(Vector3.zero);
                    groups.Add(targetGroup);
                } else {
                    return;
                }
            }

            foreach (var detailsGroup in groups)
            {
                if (detailsGroup == targetGroup) continue;

                // прямой цикл по потомкам изменяющегося трансформа не доходит до последней детали
                var children = detailsGroup.transform.Cast<Transform>().ToList();

                foreach (var child in children) {
                    var detail = child.GetComponent<Detail>();

                    AddExternalTouchesToUpdateLinks(detail, groups, freeDetails, toUpdateLinks);
                    child.SetParent(targetGroup.transform);
                }
                Destroy(detailsGroup.gameObject);
            }

            foreach (var detail in freeDetails) {
                AddExternalTouchesToUpdateLinks(detail, groups, freeDetails, toUpdateLinks);
                detail.transform.SetParent(targetGroup.transform);
            }

            foreach (var detail in toUpdateLinks) {
                detail.UpdateLinks(null, LinksMode.All);
            }

        }

        private static void AddExternalTouchesToUpdateLinks (Detail detail, HashSet<DetailsGroup> groups, HashSet<Detail> freeDetails, HashSet<Detail> toUpdateLinks)
        {
            foreach (var touch in detail.Touches) {

                var touchGroup = touch.Group;

                if (touchGroup != detail.Group) { 
                    toUpdateLinks.Add(detail);
                    toUpdateLinks.Add(touch);  //Debug.Log("Added both!");
                } else if (touchGroup == null && !freeDetails.Contains(touch)) {
                    toUpdateLinks.Add(touch); //Debug.Log("Added touch!");
                }
            }
        }

//        public void Consume(DetailsGroup group, Dictionary<Detail, List<Detail>> connections)
//        {
//            connections = connections ?? new Dictionary<Detail, List<Detail>>();
//
//            foreach (var connection in group._connections)
//            {
//                List<Detail> newNeighbours;
//
//                if (connections.TryGetValue(connection.Key, out newNeighbours))
//                {
//                    connection.Value.AddRange(newNeighbours);
//                    foreach (var newNeighbour in newNeighbours)
//                    {
//                        _connections[newNeighbour].Add(connection.Key);
//                    }
//                }
//                _connections.Add(connection.Key, connection.Value);
//                connection.Key.transform.SetParent(transform);
//            }
//            Destroy(group.gameObject);
//        }

        public void Remove(Detail detail)
        {
//            var neighbours = _connections[detail];
//
//            foreach (var neighbour in neighbours)
//            {
//                _connections[neighbour].Remove(detail);
//            }
//            _connections.Remove(detail);
	        _details.Remove(detail);
            detail.transform.SetParent(null);

            if (DetailsCount == 1) {
                var lastChild = transform.GetChild(0);

                lastChild.SetParent(null);
                Destroy(gameObject);
            }

        }

//        public void UpdateConnections(Detail detail, List<Detail> newNeighbours)
//        {
//            foreach (var oldNeighbour in _connections[detail])
//            {
//                if (!newNeighbours.Contains(oldNeighbour))
//                {
//                    _connections[oldNeighbour].Remove(detail);
//                }
//            }
//
//            _connections[detail] = newNeighbours;
//        }

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

//
//        public void ChildDetached()
//        {
//            if (transform.childCount == 1) {
//                var lastChild = transform.GetChild(0).GetComponent<IMovable>();
//
//                lastChild.Parent = transform.parent.GetComponent<DetailsGroup>();
//                Destroy(gameObject);
//            }
//        }

//        public override void UpdateLinks(List<DetailLinks> newLinks = null, LinksMode linksMode = LinksMode.ExceptSelected)
//        {
//            Debug.LogError("NotImplemented!");
//        }

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

        public void Detach(HashSet<Detail> detailsToDetach)
        {
			// если выделена вся группа целиком, то ничего отсоединять не надо
	        if (_details.Count == detailsToDetach.Count) {
		        return;
	        }

	        if (_details.Count > detailsToDetach.Count)
	        {
				Debug.LogError("Multiple groups selection unsupported!");
				return;
	        }

	        DetailBase targetDetail;
	        LinksBase emptyLinks;

		    if (detailsToDetach.Count > 1)
		    {
				var newGroup = CreateNewGroup();

				foreach (var detail in detailsToDetach) {
					if (detail.Group != this) {
						Debug.LogError("Multiple groups selection unsupported!");
						continue;
					}
					detail.Group = newGroup;
				}
				targetDetail = newGroup;
			    emptyLinks = new DetailsGroupLinks(LinksMode.ExceptSelected, newGroup);
		    } else {
			    var first = detailsToDetach.First();

				targetDetail = first;
			    first.Group = null;
				emptyLinks = new DetailLinks(LinksMode.ExceptSelected, first);
		    }

			targetDetail.UpdateLinks(emptyLinks);
			UpdateContinuity();
        }

        public override bool GetLinks(out LinksBase links, Vector3? offset = null, LinksMode linksMode = LinksMode.ExceptSelected) 
        {
	        var hasConnections = false;

			links = new List<LinksBase>();

	        foreach (Transform child in transform)
	        {
		        var detail = child.GetComponent<Detail>();
		        List<LinksBase> detailLinks;

		        if (detail.GetLinks(out detailLinks, offset, linksMode)) {
			        hasConnections = true;
		        }

				//TODO отсеивать пустые линки
				links.Add(detailLinks[0]);
	        }

	        return hasConnections;
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
