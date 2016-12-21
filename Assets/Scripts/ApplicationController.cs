using UnityEngine;

namespace Assets.Scripts {
    public class ApplicationController : MonoBehaviour
    {
        public Material[] DetailColors;

        public DetailBase SelectedDetail
        {
            get { return _selectedDetail; }
            set
            {
                if (_selectedDetail != null)
                {
                    _selectedDetail.IsSelected = false;
                }
                _selectedDetail = value;
                if (value != null)
                {
                    _selectedDetail.IsSelected = true;
                }
            }
        }

        private DetailBase _selectedDetail = null;

        public void RotateSelectedByX() {
            if (SelectedDetail == null) return;
            
            SelectedDetail.Rotate(Vector3.right);
        }

        public void RotateSelectedByY() {
            if (SelectedDetail == null) return;

            SelectedDetail.Rotate(Vector3.up);
        }

        public void RotateSelectedByZ() {
            if (SelectedDetail == null) return;

            SelectedDetail.Rotate(Vector3.forward);
        }

        public void RemoveSelected() {
            if (SelectedDetail == null) return;

            Destroy(SelectedDetail.gameObject);
        }
    }
}
