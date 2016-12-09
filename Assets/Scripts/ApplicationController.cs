using UnityEngine;

namespace Assets.Scripts {
    public class ApplicationController : MonoBehaviour
    {
        public Material[] DetailColors;

        public Detail SelectedDetail
        {
            get { return _selectedDetail; }
            set
            {
                if (_selectedDetail != null)
                {
                    _selectedDetail.IsSelected = false;
                }
                _selectedDetail = value;
                _selectedDetail.IsSelected = true;
            }
        }

        private Detail _selectedDetail = null;

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
