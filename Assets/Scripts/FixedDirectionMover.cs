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

		public void BindToTransform(Transform target)
		{
			_target = target;

		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			var pointerPos = Input.mousePosition;
			var centerPos = Camera.main.WorldToScreenPoint(transform.position);

			_offset = pointerPos - centerPos;

			Debug.Log("Begin: " + _offset);
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
