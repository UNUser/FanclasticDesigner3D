using UnityEngine;
using UnityEngine.EventSystems;


namespace Assets.Scripts {

    public class DetailButton : MonoBehaviour, IPointerUpHandler, IDragHandler
    {
        private GameObject _detailPrefab;
        private Detail _newDetail;
        private bool _isDrag;

        public void SetDetail(GameObject detail)
        {
            _detailPrefab = detail;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDrag) return;

//            _newDetail.OnPointerUp(null);
            _isDrag = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDrag)
            {
                _newDetail = Instantiate(_detailPrefab).GetComponent<Detail>();
                //TODO тут покрасивше как-то переделать
                _newDetail.GetComponent<MeshRenderer>().material = FindObjectOfType<AddDetailPanel>().ActiveColor;
                _newDetail.transform.position = Vector3.down * 5; //TODO (если мы отпустили кнопку на фоне "неба", то созданная деталь останется под плоскостью)
                _newDetail.OnPointerClick(null);
                _newDetail.OnBeginDrag(null);
                _isDrag = true;
            }

            _newDetail.OnDrag(null);
        }
    }
}
