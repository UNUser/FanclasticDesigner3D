using UnityEngine;


namespace Assets.Scripts {

    public abstract class DetailBase : MonoBehaviour
    {
        public abstract bool IsSelected { get; }

        public abstract Bounds Bounds { get; }
        public abstract Transform Lattice { get; }

        public abstract LinksBase GetLinks(Vector3 offset, Quaternion rotation, LinksMode linksMode = LinksMode.ExceptSelected);

        public virtual LinksBase GetLinks(LinksMode linksMode = LinksMode.ExceptSelected)
        {
            return GetLinks(Vector3.zero, Quaternion.identity, linksMode);
        }

        protected abstract void UpdateConnections(LinksBase newLinks);

        public virtual void UpdateLinks(LinksMode linksMode = LinksMode.ExceptSelected, LinksBase newLinks = null)
        {
            if (newLinks == null) {
                newLinks = GetLinks(linksMode);
            } else if (newLinks.LinksMode != linksMode) {
                Debug.LogError("Inconsistent links mode in parameters!");
                newLinks = GetLinks(linksMode);
            }

            AppController.Instance.SelectedDetails.IsValid = newLinks.IsValid;

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

    }
}
