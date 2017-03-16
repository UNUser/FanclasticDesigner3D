using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Assets.Scripts {

    public abstract class DetailBase : MonoBehaviour
    {
		public abstract bool IsSelected { get; set; }

        public abstract Bounds Bounds { get; }


        public abstract bool GetLinks(out LinksBase links, Vector3? offset = null, LinksMode linksMode = LinksMode.ExceptSelected);

	    public virtual void UpdateLinks(LinksBase newLinks = null, LinksMode linksMode = LinksMode.ExceptSelected)
	    {
			if (newLinks == null) {
				transform.RootParentOld().GetLinks(out newLinks, null, linksMode);
			}

			foreach (var detailLinks in newLinks) {
				detailLinks.Holder.UpdateConnections(detailLinks.Connections, linksMode);
				detailLinks.Holder.UpdateWeakNeighbours(detailLinks.Touches, linksMode);
				detailLinks.Holder.UpdateImplicitConnections(detailLinks.ImplicitConnections, linksMode);
			}


			var connectionsGroups = new HashSet<DetailsGroup>();
			var freeDetails = new HashSet<Detail>();

			foreach (var detailLinks in newLinks) {
				foreach (var detail in detailLinks.Connections) {
					if (detail.Group == null) {
						freeDetails.Add(detail);
					} else {
						connectionsGroups.Add(detail.Group);
					}
				}

				foreach (var connection in detailLinks.ImplicitConnections) {
					connectionsGroups.Add(connection.First().Group);
				}
			}

		    var asDetail = this as Detail;

		    if (asDetail != null)
		    {

			    //TODO посмотреть не слишком ли много раз тут будут вызываться UpdateLinks. 
			    //TODO Там после присоединения свободных деталей идет как бы обратная волна апдейтов на детали группы.
				if (connectionsGroups.Count < 2 && freeDetails.Count == 0 && asDetail.Group != null)
			    {
					asDetail.Group = connectionsGroups.Count == 0 ? null : connectionsGroups.Single();
				    return;
			    }


				if (!connectionsGroups.Contains(asDetail.Group))
			    {
					asDetail.Group = null;
			    }

				if (asDetail.Group == null)
			    {
				    freeDetails.Add(asDetail);
			    }
		    }
		    else
		    {
			    if (connectionsGroups.Count == 0 && freeDetails.Count == 0) {
				    return;
			    }

			    connectionsGroups.Add(this as DetailsGroup);
		    }


		    //            Debug.Log("UpdateLinks Merge: " + gameObject.name + " groups: " + connectionsGroups.Count + ", free: " + freeDetails.Count + ", touches: " + Touches.Count);
			DetailsGroup.Merge(connectionsGroups, freeDetails);
	    }

        public abstract void SetRaycastOrigins(Vector3 offset);
        public abstract RaycastHit? CastDetailAndGetClosestHit(Vector3 direction, out Detail raycaster, out int rayOriginIndex);

        public abstract void Rotate(Vector3 axis);
    }
}
