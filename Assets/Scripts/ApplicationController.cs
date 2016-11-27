using UnityEngine;

namespace Assets.Scripts {
    class ApplicationController : MonoBehaviour
    {
        public Material[] DetailColors;

        public Detail SelectedDetail { get; set; }
        //{ return _selecteDetail; } set { _selecteDetail = value; } }
//        private Detail _selecteDetail = null;

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
