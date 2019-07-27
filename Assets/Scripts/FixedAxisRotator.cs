using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts 
{
	public class FixedAxisRotator : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
	{
		public LineRenderer LineRenderer;
		public Vector3 Axis;

		public void DrawArc(int halfArcPoints)
		{
			const float angle = 45f;
			var pointsCount = halfArcPoints;
			var totalPointsCount = pointsCount * 2 + 1;
			var points = new Vector3[totalPointsCount];
			var rotationStep = Quaternion.AngleAxis(angle / pointsCount, Axis);

			points[0] = Quaternion.AngleAxis(-angle, Axis) * transform.localPosition;
			
			for (var i = 1; i < totalPointsCount; i++)
			{
				points[i] = rotationStep * points[i - 1];
			}
			
			LineRenderer.positionCount = totalPointsCount;
			LineRenderer.SetPositions(points);
		}

		private Vector3 _rootPoint;
		private Vector3 _beginDragPoint;
		private Quaternion _sourceRotation;
		private float _axisSourceRotation;
		private Vector3 _rotationPlaneNormal;

		public void OnBeginDrag(PointerEventData eventData)
		{
			var detached = AppController.Instance.SelectedDetails.Detach();

			_rootPoint = detached.Bounds.center;
			
			var pointerRay = Camera.main.ScreenPointToRay(Input.mousePosition);

			_rotationPlaneNormal = detached.transform.TransformVector(Axis);

			FixedDirectionMover.LinePlaneIntersection(out _beginDragPoint, pointerRay.origin, pointerRay.direction, _rotationPlaneNormal, _rootPoint);

			_axisSourceRotation = Vector3.Dot(detached.transform.eulerAngles, Axis);

			_sourceRotation = AppController.Instance.SelectedDetails.Rotation;

//			var mousePos = Input.mousePosition;
////			mousePos.z = 0;
//
//			var pointerRay = Camera.main.ScreenPointToRay(mousePos);
//			var layerMask = LayerMask.NameToLayer("UI");
//			RaycastHit raycastHit;
//
//			Debug.Log(mousePos);
//
//			if (Physics.Raycast(pointerRay, out raycastHit, float.PositiveInfinity, layerMask))
//			{
//				_beginDragPoint = raycastHit.point;
//			}
//			else
//			{
//				Debug.LogError("No hit on raycast!");
//			}
//			
//			Debug.Log(_beginDragPoint);
		}

		public void OnDrag(PointerEventData eventData)
		{
			var pointerRay = Camera.main.ScreenPointToRay(Input.mousePosition);
			Vector3 dragPoint;

			FixedDirectionMover.LinePlaneIntersection(out dragPoint, pointerRay.origin, pointerRay.direction, _rotationPlaneNormal, _rootPoint);

			var sourceDirection = _beginDragPoint - _rootPoint;
			var currentDirection = dragPoint - _rootPoint;
			var axisRotationDelta = Vector3.SignedAngle(sourceDirection, currentDirection, _rotationPlaneNormal);

			var qRot = Quaternion.AngleAxis(axisRotationDelta, _rotationPlaneNormal);

			var targetRotation = qRot * _sourceRotation;

			AppController.Instance.SelectedDetails.Rotation = targetRotation;
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			var selected = AppController.Instance.SelectedDetails.Detach();

//			AppController.Instance.ActionsLog.RegisterAction(new RotateAction(selected.Bounds.center - _sourcePos));
			selected.UpdateLinks();
		}
	}
}
