

using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public abstract class GizmoBase : MonoBehaviour
    {
        protected abstract GameObject RootObj { get; }
        protected abstract Vector3 TargetPos { get; }
        protected abstract Quaternion TargetRotation { get; }

        protected SelectedDetails Selected { get { return _selected ?? (_selected = AppController.Instance.SelectedDetails); } }
        private SelectedDetails _selected;
        public readonly Vector3 ScreenOriginOffset = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);

        protected void OnDisable()
        {
            RootObj.SetActive(false);
        }


        public void Update()
        {
            if (!Selected.Selected.Any()) {
                RootObj.SetActive(false);
                return;
            }

            var centerToScreenPoint = Camera.main.WorldToScreenPoint(TargetPos);

            if (centerToScreenPoint.z < 0) {
                RootObj.SetActive(false);
                return;
            }

            var rootLocalPos = centerToScreenPoint - ScreenOriginOffset;

            rootLocalPos.z = 0;

            RootObj.transform.localPosition = rootLocalPos;
            RootObj.transform.rotation = TargetRotation;

            RootObj.SetActive(true);
        }
    }
}
