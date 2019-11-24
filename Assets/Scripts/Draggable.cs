
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts 
{
	public class Draggable : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
	{

		private HingeJoint _hingeJoint;
		private Rigidbody _rigidbody;
		private Vector3 _holdingLocalPoint;

		private void Awake()
		{
			_rigidbody = GetComponent<Rigidbody>();
			_hingeJoint = GetComponent<HingeJoint>();
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			var pointerRay = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hitInfo;

			// 1. Бросаем луч в то место, на которое нажал пользователь
			if (!Physics.Raycast(pointerRay, out hitInfo)) {
				Debug.LogError("OnBeginDrag without raycast hit!");
				return;
			}

			// 2. Переводим точку попадания луча в нашу деталь в локальные координаты детали
			//    и ищем перебором ближайший к этой точке коннектор
			var localHitPoint = transform.InverseTransformPoint(hitInfo.point);

			_holdingLocalPoint = localHitPoint;


		}

		public void OnEndDrag(PointerEventData eventData)
		{
			throw new System.NotImplementedException();
		}

		public void OnDrag(PointerEventData eventData)
		{
			var pointerRay = Camera.main.ScreenPointToRay(Input.mousePosition);
			var holdingGlobalPoint = transform.TransformPoint(_holdingLocalPoint);
			Vector3 targetPoint;
			
			FixedDirectionMover.LinePlaneIntersection(out targetPoint, pointerRay.origin, pointerRay.direction, 
				transform.TransformVector(_hingeJoint.axis), 
				transform.TransformPoint(_hingeJoint.anchor));

			var force = targetPoint - holdingGlobalPoint;

			_rigidbody.AddForceAtPosition(force, holdingGlobalPoint);

			Debug.DrawLine(holdingGlobalPoint, targetPoint, Color.red, 10f);
		}
	}
}
