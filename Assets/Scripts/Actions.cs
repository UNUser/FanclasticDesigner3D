

using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts {


	public abstract class ActionBase
	{
		public abstract void Do();
		public abstract void Undo();
	}



	public class SelectAction : ActionBase {

		private readonly HashSet<Detail> _prevSelection;
		private readonly HashSet<Detail> _nextSelection;

		public SelectAction(HashSet<Detail> prevSelection, HashSet<Detail> nextSelection) {
			_prevSelection = prevSelection;
			_nextSelection = nextSelection;
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

		private readonly Vector3 _offset;

		public MoveAction(Vector3 offset) {
			_offset = offset;
		}

		public override void Do() {
			var selected = AppController.Instance.SelectedDetails;
			var targetDetail = selected.Detach();

			selected.Move(_offset);
			targetDetail.UpdateLinks();
		}

		public override void Undo() {
			var selected = AppController.Instance.SelectedDetails;
			var targetDetail = selected.Detach();

			selected.Move(-_offset);
			targetDetail.UpdateLinks();
		}
	}

	public class RotateAction : ActionBase
	{
		private readonly Vector3 _axis;

		public RotateAction(Vector3 axis)
		{
			_axis = axis;
		}

		public override void Do()
		{
			var selected = AppController.Instance.SelectedDetails;
			var targetDetail = selected.Detach();

			selected.Rotate(_axis);
			targetDetail.UpdateLinks();
		}

		public override void Undo()
		{
			var selected = AppController.Instance.SelectedDetails;
			var targetDetail = selected.Detach();

			selected.Rotate(_axis, false);
			targetDetail.UpdateLinks();
		}
	}

	public class CreateAction : ActionBase {

		private readonly DetailBase _detail;
		private readonly HashSet<Detail> _prevSelected;

		public CreateAction(DetailBase detail, HashSet<Detail> prevSelected) {
			_detail = detail;
			_prevSelected = prevSelected;
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

	public class SetColorAction : ActionBase
	{

		private readonly Dictionary<Detail, Material> _oldMaterials = new Dictionary<Detail, Material>();
		private readonly Material _newMaterial;

		public SetColorAction(HashSet<Detail> targetDetails, Material newMaterial)
		{
			_newMaterial = newMaterial;
			foreach (var detail in targetDetails) {
				_oldMaterials.Add(detail, detail.Material);
			}
		}

		public override void Do() {
			foreach (var detail in _oldMaterials.Keys) {
				detail.Material = _newMaterial;
			}
		}

		public override void Undo() {
			foreach (var oldMaterial in _oldMaterials) {
				oldMaterial.Key.Material = oldMaterial.Value;
			}
		}
	}

}
