using UnityEngine;


namespace Assets.Scripts {

    public abstract class DetailBase : MonoBehaviour
    {
		public abstract bool IsSelected { get; }

        public abstract Bounds Bounds { get; }

		public abstract LinksBase GetLinks(LinksMode linksMode = LinksMode.ExceptSelected, Vector3? offset = null);

	    protected abstract void UpdateConnections(LinksBase newLinks);

	    public virtual void UpdateLinks(LinksMode linksMode = LinksMode.ExceptSelected, LinksBase newLinks = null)
	    {
		    if (newLinks == null) {
			    newLinks = GetLinks(linksMode);
			} else if (newLinks.LinksMode != linksMode) {
				Debug.LogError("Inconsistent links mode in parameters!");
				newLinks = GetLinks(linksMode);
			}

		    if (!newLinks.IsValid) {
			    return;
		    }

			UpdateConnections(newLinks);

			if (!newLinks.HasConnections) {
				return;
			}

		    var asDetail = this as Detail;

		    if (asDetail != null && asDetail.Group != null) {
			    return;
		    }

			DetailsGroup.Merge(newLinks);
	    }

        public abstract void Rotate(Vector3 axis, bool clockwise = true);
    }
}
