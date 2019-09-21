using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts
{
	public class FixedDirectionMover : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
	{
		public Vector3 Direction;

		private AxisMover AxisMover { get { return _axisMover ?? (_axisMover = transform.parent.parent.parent.GetComponent<AxisMover>()); } }
		private AxisMover _axisMover;
//		private Vector3 _offset;
//		private Transform _target;

		private Vector3 _lineBegining;
		private Vector3 _lineEnd;

		private Vector2 _projToScreenBegining;
		private Vector2 _projToScreenEnd;

		private const float WorkspaceExtents = 500;

//		public void BindToTransform(Transform target)
//		{
//			_target = target;
//
//		}

		private Vector3 _sourcePos;
		private Vector3 _prevCenterPos;
//		private Vector3 _correction;

		public void OnBeginDrag(PointerEventData eventData)
		{
//			var pointerPos = Input.mousePosition;
//			var centerPos = Camera.main.WorldToScreenPoint(transform.position);
			
			_sourcePos = AppController.Instance.SelectedDetails.Detach().Bounds.center;
			_prevCenterPos = _sourcePos;
//			_offset = pointerPos - centerPos;
//			_correction = Vector3.zero;


			// если крайняя точка оси попадает во вьюпорт, то движение курсора ограничивать не правильно, т.к. курсор лежит на цилиндре, за который объект перемещается, а сам объект 
			// находится на некотором расстоянии от этого цилиндра и когда курсор доходит до границы, то самому объекту еще есть куда двигаться
			_lineBegining = new Vector3(Direction.x > 0 ? -WorkspaceExtents : _prevCenterPos.x,
										Direction.y > 0 ? 0 : _prevCenterPos.y,
										Direction.z > 0 ? -WorkspaceExtents : _prevCenterPos.z);
			_lineEnd = new Vector3(Direction.x > 0 ? WorkspaceExtents : _prevCenterPos.x,
								   Direction.y > 0 ? 2 * WorkspaceExtents : _prevCenterPos.y,
								   Direction.z > 0 ? WorkspaceExtents : _prevCenterPos.z);

			var rawProjToScreenBegining = Camera.main.WorldToScreenPoint(_lineBegining);
			var rawProjToScreenEnd = Camera.main.WorldToScreenPoint(_lineEnd);

			if (rawProjToScreenBegining.z < 0 &&
			    rawProjToScreenEnd.z < 0) {
				return;
			}

			if (rawProjToScreenBegining.z < 0)
			{
				var normal = Camera.main.transform.forward;
				var planePoint = Camera.main.transform.position;
				var lineVector = _lineEnd - _lineBegining;
				var alpha = Vector3.Dot(planePoint - _lineBegining, normal) / Vector3.Dot(lineVector, normal);

				var intersectionPoint = _lineBegining + alpha * lineVector + lineVector.normalized * 0.01f;




//				Debug.Log(_lineBegining + " " + (_lineBegining - _lineEnd) + " " + Camera.main.transform.forward + " " + Camera.main.transform.position);

//				LinePlaneIntersection(out intersectionPoint, _lineBegining, _lineEnd - _lineBegining,
//					Camera.main.transform.forward, Camera.main.transform.position);

				_lineBegining = intersectionPoint;
				rawProjToScreenBegining = Camera.main.WorldToScreenPoint(_lineBegining);

				
			}


			if (rawProjToScreenEnd.z < 0) {
				var normal = Camera.main.transform.forward;
				var planePoint = Camera.main.transform.position;
				var lineVector = _lineEnd - _lineBegining;
				var alpha = Vector3.Dot(planePoint - _lineBegining, normal) / Vector3.Dot(lineVector, normal);

				var intersectionPoint = _lineBegining + alpha * lineVector - lineVector.normalized * 0.01f;



//				Debug.Log(_lineBegining + " " + (_lineBegining - _lineEnd) + " " + Camera.main.transform.forward + " " + Camera.main.transform.position);

//				LinePlaneIntersection(out intersectionPoint, _lineBegining, _lineEnd - _lineBegining,
//					Camera.main.transform.forward, Camera.main.transform.position);

				_lineEnd = intersectionPoint;
				rawProjToScreenEnd = Camera.main.WorldToScreenPoint(_lineEnd);
			}

			_projToScreenBegining = rawProjToScreenBegining;
			_projToScreenEnd = rawProjToScreenEnd;

			ClampSegmentToViewport(ref _projToScreenBegining, ref _projToScreenEnd);


//			Debug.DrawLine(Camera.main.ScreenToWorldPoint(new Vector3(_projToScreenBegining.x, _projToScreenBegining.y, 1f)),
//						   Camera.main.ScreenToWorldPoint(new Vector3(_projToScreenEnd.x, _projToScreenEnd.y, 1f)),
//						   Color.red, 100f);


//			Debug.DrawLine(_lineBegining, _lineEnd, Color.green, 100f);

//			Debug.Log("Begin: " + _lineBegining + " " + _lineEnd);
//			Debug.Log("Begin: " + rawProjToScreenBegining + " " + rawProjToScreenEnd);
//			Debug.Log("Begin: " + _projToScreenBegining + " " + _projToScreenEnd);


//			RaycastHit hitInfo;

//			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

//			Debug.DrawRay(ray.origin, ray.direction, Color.red, 100f);
			/*if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, 10f))
			{
				Debug.LogError("No hit!");
				return;
			}

			var hitScreenPoint = Camera.main.WorldToScreenPoint(hitInfo.point);*/

			_rootOffset = /*hitInfo.point*/transform.position - AxisMover.Root.transform.position;
			_cameraDistance = Camera.main.WorldToScreenPoint(transform.position).z/*hitScreenPoint.z*/;


//			Debug.Log(hitScreenPoint + " " + Input.mousePosition + " " + hitInfo.collider.gameObject.name);


		}

		private void ClampSegmentToViewport(ref Vector2 begin, ref Vector2 end)
		{
			var checklist = new List<Vector2>();

			Vector2? resultBegin = null;
			Vector2? resultEnd = null;
			
			checklist.Add(begin);
			checklist.Add(end);

			if (begin.x != end.x) {
				checklist.Add(new Vector2(0f,           begin.y -  begin.x                 * (end.y - begin.y) / (end.x - begin.x)));
				checklist.Add(new Vector2(Screen.width, begin.y - (begin.x - Screen.width) * (end.y - begin.y) / (end.x - begin.x)));
			}

			if (begin.y != end.y) {
				checklist.Add(new Vector2(begin.x -  begin.y                  * (end.x - begin.x) / (end.y - begin.y), 0f));
				checklist.Add(new Vector2(begin.x - (begin.y - Screen.height) * (end.x - begin.x) / (end.y - begin.y), Screen.height));
			}

			var min = new Vector2(Mathf.Min(begin.x, end.x), Mathf.Min(begin.y, end.y));
			var max = new Vector2(Mathf.Max(begin.x, end.x), Mathf.Max(begin.y, end.y));

			foreach (var point in checklist)
			{
//				CheckAndAssign(point, ref resultBegin, ref resultEnd);



				if (point.x >= 0 && point.x <= Screen.width &&
					point.y >= 0 && point.y <= Screen.height &&
					point.x >= min.x && point.x <= max.x &&
					point.y >= min.y && point.y <= max.y) 
				{
//					Debug.Log(point + " " + begin + " " + end + " " + min + " " + end);

					if (resultBegin == null) {
						resultBegin = point;
					} else {
						resultEnd = point;
					}
				}

				if (resultEnd != null) {
					break;
				}
			}

			begin = resultBegin.Value;
			end = resultEnd.Value;
		}

//		private void CheckAndAssign(Vector2 value, ref Vector2? resultBegin, ref Vector2? resultEnd) 
//		{
//			if (resultEnd != null) {
//				return;
//			}
//
//			if (value.x >= 0 && value.x <= Screen.width &&
//				value.y >= 0 && value.y <= Screen.height)
//			{
//				if (resultBegin == null) {
//					resultBegin = value;
//				} else {
//					resultEnd = value;
//				}
//			}
//		}

		private float _cameraDistance;
		private Vector3 _rootOffset;

		public void OnDrag(PointerEventData eventData)
		{
//			Debug.Log("Drag!");

			var selectedDetails = AppController.Instance.SelectedDetails;
			var pointerPos = ProjectPointOnLineSegment(_projToScreenBegining, _projToScreenEnd, Input.mousePosition);


//			Debug.DrawLine(Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1f)),
//						   Camera.main.ScreenToWorldPoint(new Vector3(pointerPos.x, pointerPos.y, 1f)),
//						   Color.red, 1f);


			var newHitScreenPoint = new Vector3(pointerPos.x, pointerPos.y, _cameraDistance);
			var newRootWorldPos = Camera.main.ScreenToWorldPoint(newHitScreenPoint) - _rootOffset;
			var newRootScreenPos = Camera.main.WorldToScreenPoint(newRootWorldPos);

			newRootScreenPos.z = 0;


			var newRootRay = Camera.main.ScreenPointToRay(newRootScreenPos);
			Vector3 selectedNewPos;
			Vector3 dummy;

			ClosestPointsOnTwoLines(out selectedNewPos, out dummy, _lineBegining, Direction, newRootRay.origin,
				newRootRay.direction);

			var selectedOffset = selectedNewPos - _prevCenterPos;

			selectedOffset.x = Mathf.Round(selectedOffset.x) * Direction.x;
			selectedOffset.y = Mathf.Round(selectedOffset.y) * Direction.y;
			selectedOffset.z = Mathf.Round(selectedOffset.z) * Direction.z;

			var bottomDetailSpaceBounds = selectedDetails.GetBottomDetail().SpaceBounds.bounds;
			var bottomDetailSpaceBoundsCorrected = new Bounds(bottomDetailSpaceBounds.center - AxisMover.Correction,
				bottomDetailSpaceBounds.size);
			var maxColliderOffsetY = - Mathf.RoundToInt(bottomDetailSpaceBoundsCorrected.min.y + 1);
			var maxPosOffsetY = - Mathf.RoundToInt(bottomDetailSpaceBoundsCorrected.center.y);
			var maxOffsetY = Math.Max(maxColliderOffsetY, maxPosOffsetY);

			selectedOffset.y = Math.Max(selectedOffset.y, maxOffsetY);

			if (selectedOffset == Vector3.zero) {
				return;
			}

			var offsetFromSource = _prevCenterPos + selectedOffset - _sourcePos;
			var offsetValue = Mathf.RoundToInt(offsetFromSource.x + offsetFromSource.y + offsetFromSource.z);
			var oddOffsetCorrection = Vector3.zero;

			if (!offsetValue.IsOdd())
			{
				var links = selectedDetails.GetLinks(LinksMode.ExceptSelected, selectedOffset - AxisMover.Correction);

				if (!links.IsValid) {
					return;
				}
			} else {

				var offsetCorrections = new [] {
					new Vector3(Direction.x == 0 ? 1 : 0, 
							    Direction.y == 0 ? 1 : 0, 
							    Direction.z == 0 ? 1 : 0), 
					new Vector3(Direction.x == 0 ? -1 : 0, 
							    Direction.y == 0 ? -1 : 0, 
							    Direction.z == 0 ? -1 : 0), 
					new Vector3(Direction.x == 0 ? 1 : 0, 
							    Direction.y == 0 ? (Direction.x == 0 ? -1 : 1) : 0, 
							    Direction.z == 0 ? -1 : 0), 
					new Vector3(Direction.x == 0 ? -1 : 0, 
							    Direction.y == 0 ? (Direction.x == 0 ? 1 : -1) : 0, 
							    Direction.z == 0 ? 1 : 0)
				};

				int correctionIndex;

				for (correctionIndex = 0; correctionIndex < offsetCorrections.Length; correctionIndex++)
				{
					var validOffset = offsetCorrections[correctionIndex] + selectedOffset;

					if (maxOffsetY == 0 && validOffset.y < 0) {
						continue;
					}

					var links = selectedDetails.GetLinks(LinksMode.ExceptSelected, validOffset - AxisMover.Correction);

					if (links.HasConnections) {
						break;
					}
				}

				if (correctionIndex == offsetCorrections.Length) {
					return;
				}

				oddOffsetCorrection = offsetCorrections[correctionIndex];
			}


			


			selectedDetails.Move(selectedOffset - AxisMover.Correction + oddOffsetCorrection);
			selectedDetails.IsValid = true;
			_prevCenterPos = _prevCenterPos + selectedOffset;

			AxisMover.Correction = oddOffsetCorrection;

			
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			var selected = AppController.Instance.SelectedDetails.Detach();

			AxisMover.Correction = Vector3.zero;
			AppController.Instance.ActionsLog.RegisterAction(new MoveAction(selected.Bounds.center - _sourcePos));
			selected.UpdateLinks();
		}










		public static Vector3 SetVectorLength(Vector3 vector, float size) {

			//normalize the vector
			Vector3 vectorNormalized = Vector3.Normalize(vector);

			//scale the vector
			return vectorNormalized *= size;
		}

		public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point) {

			//get vector from point on line to point in space
			Vector3 linePointToPoint = point - linePoint;

			float t = Vector3.Dot(linePointToPoint, lineVec);

			return linePoint + lineVec * t;
		}

		public static int PointOnWhichSideOfLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point) {

			Vector3 lineVec = linePoint2 - linePoint1;
			Vector3 pointVec = point - linePoint1;

			float dot = Vector3.Dot(pointVec, lineVec);

			//point is on side of linePoint2, compared to linePoint1
			if (dot > 0) {

				//point is on the line segment
				if (pointVec.magnitude <= lineVec.magnitude) {

					return 0;
				}

				//point is not on the line segment and it is on the side of linePoint2
				else {

					return 2;
				}
			}

			//Point is not on side of linePoint2, compared to linePoint1.
				//Point is not on the line segment and it is on the side of linePoint1.
			else {

				return 1;
			}
		}

		public static Vector3 ProjectPointOnLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point) {

			Vector3 vector = linePoint2 - linePoint1;

			Vector3 projectedPoint = ProjectPointOnLine(linePoint1, vector.normalized, point);

			int side = PointOnWhichSideOfLineSegment(linePoint1, linePoint2, projectedPoint);

			//The projected point is on the line segment
			if (side == 0) {

				return projectedPoint;
			}

			if (side == 1) {

				return linePoint1;
			}

			if (side == 2) {

				return linePoint2;
			}

			//output is invalid
			return Vector3.zero;
		}

		public static bool LinePlaneIntersection(out Vector3 intersection, Vector3 linePoint, Vector3 lineVec, Vector3 planeNormal, Vector3 planePoint) {

			float length;
			float dotNumerator;
			float dotDenominator;
			Vector3 vector;
			intersection = Vector3.zero;

			//calculate the distance between the linePoint and the line-plane intersection point
			dotNumerator = Vector3.Dot((planePoint - linePoint), planeNormal);
			dotDenominator = Vector3.Dot(lineVec, planeNormal);

			//line and plane are not parallel
			if (dotDenominator != 0.0f) {
				length = dotNumerator / dotDenominator;

				//create a vector from the linePoint to the intersection point
				vector = SetVectorLength(lineVec, length);

				//get the coordinates of the line-plane intersection point
				intersection = linePoint + vector;

				return true;
			}

			//output not valid
			else {
				return false;
			}
		}


		public static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2) {

			closestPointLine1 = Vector3.zero;
			closestPointLine2 = Vector3.zero;

			float a = Vector3.Dot(lineVec1, lineVec1);
			float b = Vector3.Dot(lineVec1, lineVec2);
			float e = Vector3.Dot(lineVec2, lineVec2);

			float d = a * e - b * b;

			//lines are not parallel
			if (d != 0.0f) {

				Vector3 r = linePoint1 - linePoint2;
				float c = Vector3.Dot(lineVec1, r);
				float f = Vector3.Dot(lineVec2, r);

				float s = (b * f - c * e) / d;
				float t = (a * f - c * b) / d;

				closestPointLine1 = linePoint1 + lineVec1 * s;
				closestPointLine2 = linePoint2 + lineVec2 * t;

				return true;
			} else {
				return false;
			}
		}

	}
}
