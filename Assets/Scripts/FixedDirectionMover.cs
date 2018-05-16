using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts
{
	public class FixedDirectionMover : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
	{
		public Vector3 Direction;

		private Transform Root { get { return transform.parent; } }
		private Vector3 _offset;
		private Transform _target;

		private Vector3 _lineBegining;
		private Vector3 _lineEnd;

		private Vector3 _projToScreenBegining;
		private Vector3 _projToScreenEnd;

		private const float WorkspaceExtents = 500;

		public void BindToTransform(Transform target)
		{
			_target = target;

		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			var pointerPos = Input.mousePosition;
			var centerPos = Camera.main.WorldToScreenPoint(transform.position);
			var targetPos = _target.position;

			_offset = pointerPos - centerPos;


			_lineBegining = new Vector3(Direction.x > 0 ? - WorkspaceExtents : targetPos.x,
										Direction.y > 0 ? 0 : targetPos.y,
										Direction.z > 0 ? - WorkspaceExtents : targetPos.z);
			_lineEnd = new Vector3(Direction.x > 0 ? WorkspaceExtents : targetPos.x,
								   Direction.y > 0 ? 2 * WorkspaceExtents : targetPos.y,
								   Direction.z > 0 ? WorkspaceExtents : targetPos.z);

			_projToScreenBegining = Camera.main.WorldToScreenPoint(_lineBegining);
			_projToScreenEnd = Camera.main.WorldToScreenPoint(_lineEnd);

			Debug.DrawLine(_lineBegining, _lineEnd, Color.red, 5f);

			Debug.Log("Begin: " + (Vector2) _projToScreenBegining + " " + (Vector2) _projToScreenEnd);
		}

		public void OnDrag(PointerEventData eventData)
		{
			
//			Vector3.Cross()
			
			Debug.Log("Drag!");
		}

		public void OnEndDrag(PointerEventData eventData) 
		{
			Debug.Log("End!");
		}

	}
}
