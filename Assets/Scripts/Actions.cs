

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts {

	public enum ActionType
	{
		Selection,
		Movement,
		Rotation,
		Creation,
		Deleting,
		Coloring
	}

	public abstract class ActionBase
	{
		public abstract ActionType Type { get; }

		public abstract void Do();
		public abstract void Undo();
	}

	//TODO make InitialAction base class for SelectAction and CreateAction

	public class SelectAction : ActionBase {
		public override ActionType Type {
			get { return ActionType.Selection; }
		}

		public DetailData SourceState { get; private set; }
		public List<Detail> SelectedDetails { get { return _nextSelection.ToList(); } } 

		private readonly HashSet<Detail> _prevSelection;
		private readonly HashSet<Detail> _nextSelection;

		public SelectAction(HashSet<Detail> prevSelection, HashSet<Detail> nextSelection) {
			_prevSelection = prevSelection;
			_nextSelection = nextSelection;
			SourceState = nextSelection.Count == 1 ? nextSelection.First().Data : null;
		}

		public override void Do() {
			var selected = AppController.Instance.SelectedDetails;

			selected.Clear();
			selected.Add(_nextSelection);
		}

		public override void Undo() {
			var selected = AppController.Instance.SelectedDetails;

			selected.Clear();
			selected.Add(_prevSelection);
		}
	}

	public class MoveAction : ActionBase {
		public override ActionType Type {
			get { return ActionType.Movement; }
		}

		public Vector3 Offset { get; private set; }

		public MoveAction(Vector3 offset) {
			Offset = offset;
		}

		public override void Do() {
			var selected = AppController.Instance.SelectedDetails;
			var targetDetail = selected.Detach();

			selected.Move(Offset);
			targetDetail.UpdateLinks();
		}

		public override void Undo() {
			var selected = AppController.Instance.SelectedDetails;
			var targetDetail = selected.Detach();

			selected.Move(-Offset);
			targetDetail.UpdateLinks();
		}
	}

	public class RotateAction : ActionBase {
		public override ActionType Type {
			get { return ActionType.Rotation; }
		}

		public Vector3 Axis { get; private set; }
		public Vector3 Pivot { get; private set; }
		public Vector3 Alignment { get; private set; }

		public RotateAction(Vector3 axis, Vector3 pivot, Vector3 alignment)
		{
			Axis = axis;
			Pivot = pivot;
			Alignment = alignment;
		}

		public override void Do()
		{
			var selected = AppController.Instance.SelectedDetails;
			var targetDetail = selected.Detach();

			selected.Rotate(Axis, true, Pivot, Alignment);
			targetDetail.UpdateLinks();
		}

		public override void Undo()
		{
			var selected = AppController.Instance.SelectedDetails;
			var targetDetail = selected.Detach();

			selected.Rotate(Axis, false, Pivot, Alignment);
			targetDetail.UpdateLinks();
		}
	}

	public class CreateAction : ActionBase {
		public override ActionType Type {
			get { return ActionType.Creation; }
		}

		public Detail Detail { get { return (Detail) _detail; } }
		public DetailData SourceState { get; private set; }

		private readonly DetailBase _detail;
		private readonly HashSet<Detail> _prevSelected;

		public CreateAction(DetailBase detail, HashSet<Detail> prevSelected) {
			_detail = detail;
			_prevSelected = prevSelected;
			SourceState = ((Detail) detail).Data;
		}

		public override void Do() {
			var selected = AppController.Instance.SelectedDetails;

			selected.Clear();
			selected.Add(_detail);
			selected.Show();
		}

		public override void Undo() {
			var selected = AppController.Instance.SelectedDetails;

			selected.Hide();
			selected.Clear();
			selected.Add(_prevSelected);
		}
	}

	public class DeleteAction : ActionBase {
		public override ActionType Type {
			get { return ActionType.Deleting; }
		}

		private readonly DetailBase _detail;

		public DeleteAction(DetailBase detail) {
			_detail = detail;
		}

		public override void Do() {
			var selected = AppController.Instance.SelectedDetails;

			selected.Hide();
			selected.Clear();
		}

		public override void Undo() {
			var selected = AppController.Instance.SelectedDetails;

			selected.Clear();
			selected.Add(_detail);
			selected.Show();
		}
	}

	public class SetColorAction : ActionBase {
		public override ActionType Type {
			get { return ActionType.Coloring; }
		}

		private readonly Dictionary<Detail, DetailColor> _prevColors = new Dictionary<Detail, DetailColor>();
		private readonly DetailColor _newColor;

		public SetColorAction(HashSet<Detail> targetDetails, DetailColor newColor)
		{
			_newColor = newColor;
			foreach (var detail in targetDetails) {
				_prevColors.Add(detail, detail.Color);
			}
		}

		public override void Do() {
			foreach (var detail in _prevColors.Keys) {
				detail.Color = _newColor;
			}
		}

		public override void Undo() {
			foreach (var prevColor in _prevColors) {
				prevColor.Key.Color = prevColor.Value;
			}
		}
	}

}
