using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts {

	public class ActionsLog : MonoBehaviour
	{

		public GameObject UndoButton;
		public GameObject RedoButton;


		private readonly List<ActionBase> _history = new List<ActionBase>();
		private int _actionIndex;

		public void Start()
		{
			UndoButton.SetActive(false);
			RedoButton.SetActive(false);
		}

		public void RegisterAction(ActionBase action)
		{
			var tailLength = _history.Count - _actionIndex;

			if (tailLength > 0) {
				_history.RemoveRange(_actionIndex, tailLength);
			}

			_history.Add(action);
			_actionIndex++;

			RedoButton.SetActive(false);
			UndoButton.SetActive(true);

			Debug.Log("Registered: " + action.GetType() + " " + _actionIndex + "/" + _history.Count);
		}

		public void OnUndoButtonClicked()
		{
			_actionIndex--;
			_history[_actionIndex].Undo();

			UndoButton.SetActive(_actionIndex > 0);
			RedoButton.SetActive(true);

			Debug.Log("Undo: " + _actionIndex + "/" + _history.Count);
		}

		public void OnRedoButtonClicked()
		{
			_history[_actionIndex].Do();
			_actionIndex++;

			RedoButton.SetActive(_actionIndex < _history.Count);
			UndoButton.SetActive(true);

			Debug.Log("Redo: " + _actionIndex + "/" + _history.Count);
		}

	}
}
