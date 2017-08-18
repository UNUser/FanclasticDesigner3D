﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


namespace Assets.Scripts {

    public class DetailButton : MonoBehaviour, IPointerUpHandler, IDragHandler
    {
        private GameObject _detailPrefab;
        private Detail _newDetail;
        private bool _isDrag;

	    private HashSet<Detail> _prevSelected;

        public void SetDetail(GameObject detail)
        {
            _detailPrefab = detail;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDrag) return;

            _isDrag = false;
            if (_newDetail.transform.position.y < 0) {
				AppController.Instance.RemoveSelected();
				return;
            }

			AppController.Instance.ActionsLog.RegisterAction(new CreateAction(_newDetail, _prevSelected));
        }

        public void OnDrag(PointerEventData eventData)
        {
	        if (!AppController.Instance.SelectedDetails.IsValid) {
		        return;
	        }

            if (!_isDrag)
            {
                _newDetail = Instantiate(_detailPrefab).GetComponent<Detail>();

                //TODO тут покрасивше как-то переделать
	            _prevSelected = AppController.Instance.SelectedDetails.Selected;

                _newDetail.Color = FindObjectOfType<AddDetailPanel>().ActiveColor;
                _newDetail.transform.position = Vector3.down * 5;
                _newDetail.OnPointerDown(null);
                _newDetail.OnPointerUp(null);
                _newDetail.OnBeginDrag(null);
                _isDrag = true;
            }

            _newDetail.OnDrag(null);
        }
    }
}
