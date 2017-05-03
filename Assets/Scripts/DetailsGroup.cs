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
		}

		public Detail[] Details { get { return _details.ToArray(); } }

		private readonly HashSet<Detail> _details = new HashSet<Detail>(); 

        public int DetailsCount
        {
            get { return _details.Count; }
        }

        public void Add (Detail detail)
        {
			if (detail.Group == this) return;

			if (detail.Group != null) {
				detail.Group.Remove(detail);
			}

			_details.Add(detail);
			detail.transform.SetParent(transform);
        }

        public static DetailsGroup CreateNewGroup(Vector3? position = null)
        {
            var newObj = new GameObject("DetailsGroup");
            newObj.transform.position = position ?? Vector3.zero;

            return newObj.AddComponent<DetailsGroup>();
        }

        private List<HashSet<Detail>> InitLastAdded(LinksBase lostLinks, List<HashSet<Detail>> subgroups)
        {
			var directlyAdded = new List<HashSet<Detail>>();

            // Подгруппы, порожденные прямыми связями
            foreach (var lostConnection in lostLinks.Connections)
            {
                directlyAdded.Add(new HashSet<Detail> { lostConnection });
                subgroups.Add(new HashSet<Detail>());
            }

//            DebugPrint(subgroups, directlyAdded);

            return MergeAllSubgroups(subgroups, directlyAdded);
        }

        public void UpdateContinuity(LinksBase lostLinks)
        {
            var subgroups = new List<HashSet<Detail>>();

            var lastAdded = InitLastAdded(lostLinks, subgroups);
            var detailsAdded = lastAdded.Sum(newDetails => newDetails.Count);

//            Debug.Log("/////////////////");

            while (detailsAdded > 0 && subgroups.Count > 1)
            {
                var directlyAdded = new List<HashSet<Detail>>();

                // добавляем в подгруппы следующий уровень соединений
                for (var i = 0; i < lastAdded.Count; i++) {

                    directlyAdded.Add(new HashSet<Detail>());

                    // пробегаем по всем последним добавленным в подгруппу деталям
                    foreach (var detail in lastAdded[i]) {

                        // добавляем детали по прямым связям
                        directlyAdded[i].UnionWith(detail.Connections);
                    }
                }

                lastAdded = MergeAllSubgroups(subgroups, directlyAdded); // TODO можно оптимизировать, подгруппы, в которые ничего не добавляется, можно отделять сразу, хотя, возможно, они могут присоединится неявными связями
                detailsAdded = lastAdded.Sum(newDetails => newDetails.Count);
            }

            SplitAndUpdate(subgroups);
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

        private void SplitAndUpdate(List<HashSet<Detail>> subgroups)
        {
            for (var i = 1; i < subgroups.Count; i++) {
                var newGroup = subgroups[i].Count > 1 ? CreateNewGroup(Vector3.zero) : null;

                foreach (var detail in subgroups[i]) {
	                if (newGroup != null) {
		                newGroup.Add(detail);
	                } else {
		                Remove(detail);
	                }
                }
            }
        }

        private List<HashSet<Detail>> MergeAllSubgroups(List<HashSet<Detail>> subgroups, List<HashSet<Detail>> directlyAdded)
        {
            var lastAdded = new List<HashSet<Detail>>();

            // Отбираем в lastAdded новые детали, которых еще нет ни в одной подгруппе.
            for (var i = 0; i < subgroups.Count; i++)
            {
                // Для новых деталей, добавленных по прямым связям, проверяем соответствующую подгруппу
                lastAdded.Add(new HashSet<Detail>(directlyAdded[i]));
                lastAdded[i].ExceptWith(subgroups[i]);
                subgroups[i].UnionWith(lastAdded[i]);
            }

//            var sub = new List<HashSet<Detail>>(subgroups);
//            var last = new List<HashSet<Detail>>(lastAdded);
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

            return lastAdded;
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

        public static void Merge(LinksBase newLinks)
        {
			var groups = new HashSet<DetailsGroup>();
			var details = new HashSet<Detail>();

			foreach (var detail in newLinks.Connections) {
				if (detail.Group == null) {
					details.Add(detail);
				} else {
					groups.Add(detail.Group);
				}
			}
			
			if (newLinks is DetailLinks) {
				details.Add(((DetailLinks) newLinks).Holder);
			} else {
				var groupLinks = (DetailsGroupLinks) newLinks;
				var group = groupLinks.DetailsLinks.First().Holder.Group;

				groups.Add(group);
			}

            DetailsGroup targetGroup = null;

            foreach (var detailsGroup in groups) {
                if (targetGroup == null || targetGroup.DetailsCount < detailsGroup.DetailsCount) {
                    targetGroup = detailsGroup;
                }
            }

            if (targetGroup == null) {
                if (details.Count > 1) {
                    targetGroup = CreateNewGroup(Vector3.zero);
                    groups.Add(targetGroup);
                } else {
                    return;
                }
            }

            foreach (var detailsGroup in groups)
            {
                if (detailsGroup == targetGroup) continue;

				foreach (var detail in detailsGroup.Details) {
					targetGroup.Add(detail);
				}
                Destroy(detailsGroup.gameObject);
            }

            foreach (var detail in details) {
				targetGroup.Add(detail);
            }
        }

        public void Remove(Detail detail)
        {
	        _details.Remove(detail);
            detail.transform.SetParent(null);

            if (DetailsCount == 1) {
                var lastChild = transform.GetChild(0);

                lastChild.SetParent(null);
                Destroy(gameObject);
            }

        }

        protected override void UpdateConnections(LinksBase newLinks)
        {
	        var groupLinks = newLinks as DetailsGroupLinks;

	        if (groupLinks == null) {
		        Debug.LogError("Invalid links type: expected DetailsGroupLinks!");
				return;
	        }

            foreach (var detailLinks in groupLinks.DetailsLinks) {
	            var detail = detailLinks.Holder;

				detail.UpdateLinks(detailLinks.LinksMode, detailLinks);
            }
        }

        public override Bounds Bounds
        {
            get
            {
                var combinedBounds = new Bounds();
                combinedBounds.center = _details.First().transform.position;
                foreach (Transform child in transform)
                {
                    combinedBounds.Encapsulate(child.GetComponent<DetailBase>().Bounds);
                }
                return combinedBounds;
            }
        }


        public override void Rotate(Vector3 axis, bool clockwise = true)
        {
	        var boundingBox = Bounds;

            transform.RotateAround(boundingBox.center, axis, clockwise ? 90 : -90);

            var bottomDetail = GetBottomDetail();
            var newPos = bottomDetail.transform.position;

            bottomDetail.AlignPosition(ref newPos);

            var offset = newPos - bottomDetail.transform.position;
            transform.Translate(offset, Space.World);
        }

        public DetailBase Detach(HashSet<Detail> detailsToDetach)
        {
			// если выделена вся группа целиком, то ничего отсоединять не надо
	        if (_details.Count == detailsToDetach.Count) {
		        return this;
	        }

	        if (_details.Count < detailsToDetach.Count)
	        {
				Debug.LogError("Multiple groups selection unsupported!");
				return null;
	        }

	        DetailBase targetDetail;
	        LinksBase emptyLinks;

		    if (detailsToDetach.Count > 1)
		    {
				var newGroup = CreateNewGroup();
				var emptyGroupLinks = new DetailsGroupLinks(LinksMode.ExceptSelected);

				targetDetail = newGroup;

				foreach (var detail in detailsToDetach) {
					if (detail.Group != this) {
						Debug.LogError("Multiple groups selection unsupported!");
						continue;
					}
					newGroup.Add(detail);
					emptyGroupLinks += new DetailLinks(detail, emptyGroupLinks.LinksMode);
				}

			    emptyLinks = emptyGroupLinks;
		    } else {
			    var first = detailsToDetach.First();

				targetDetail = first;
			    Remove(first);
				emptyLinks = new DetailLinks(first);
		    }
			
	        var lostLinks = targetDetail.GetLinks();

			targetDetail.UpdateLinks(LinksMode.ExceptSelected, emptyLinks);
			UpdateContinuity(lostLinks);

	        return targetDetail;
        }

        public override LinksBase GetLinks(LinksMode linksMode = LinksMode.ExceptSelected, Vector3? offset = null)
        {
	        return AppController.Instance.SelectedDetails.GetLinks(linksMode, offset);
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

                if (detail.transform.root != transform) continue;

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
