using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts {

	public class SelectionLayer : MonoBehaviour
	{

		private Action<ConnectedGroup> _onSelectedAction;
		private List<ConnectedGroup> _connectedGroups;
		private List<DetailsGroup> _detailsGroups;

		private int _selectedIndex;

		public void ShowSelection(List<ConnectedGroup> connectedGroups, Action<ConnectedGroup> onSelectedAction) {

			if (connectedGroups.Count == 1) {
				var id = connectedGroups[0].Details.First().Id;
				var detailsGroup = AppController.Instance.Session.GetDetail(id).Group;

				detailsGroup.gameObject.SetActive(true);
				onSelectedAction(connectedGroups.First());
				return;
			}

			_selectedIndex = 0;
			_connectedGroups = connectedGroups;
			_onSelectedAction = onSelectedAction;

			_detailsGroups = new List<DetailsGroup>();

			foreach (var connectedGroup in _connectedGroups) {

				var id = connectedGroup.Details.First().Id;
				var detailsGroup = AppController.Instance.Session.GetDetail(id).Group;

				_detailsGroups.Add(detailsGroup);
			}

			_detailsGroups[0].gameObject.SetActive(true);
			gameObject.SetActive(true);
			AppController.Instance.CameraController.Focus = _detailsGroups[0].Bounds.center;
		}

		public void OnNextButtonClicked()
		{
			_detailsGroups[_selectedIndex].gameObject.SetActive(false);
			_selectedIndex = (_selectedIndex + 1 + _detailsGroups.Count) % _detailsGroups.Count;
			_detailsGroups[_selectedIndex].gameObject.SetActive(true);
			AppController.Instance.CameraController.Focus = _detailsGroups[_selectedIndex].Bounds.center;
		}

		public void OnPrevButtonClicked()
		{
			_detailsGroups[_selectedIndex].gameObject.SetActive(false);
			_selectedIndex = (_selectedIndex - 1 + _detailsGroups.Count) % _detailsGroups.Count;
			_detailsGroups[_selectedIndex].gameObject.SetActive(true);
			AppController.Instance.CameraController.Focus = _detailsGroups[_selectedIndex].Bounds.center;
		}

		public void OnOkButtonClicked()
		{
			gameObject.SetActive(false);
			_onSelectedAction(_connectedGroups[_selectedIndex]);
		}

		public void OnBackButtonClicked()
		{
			gameObject.SetActive(false);
			_onSelectedAction(null);
		}
	}
}
