using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts {
    public static class Extentions {

        public static Bounds Overlap(Bounds box1, Bounds box2)
        {
            var intersection = new Bounds();

            var min1 = box1.min;
            var min2 = box2.min;
            var max1 = box1.max;
            var max2 = box2.max;

            var intersectionMin = new Vector3(Mathf.Max(min1.x, min2.x), Mathf.Max(min1.y, min2.y),
                Mathf.Max(min1.z, min2.z));
            var intersectionMax = new Vector3(Mathf.Min(max1.x, max2.x), Mathf.Min(max1.y, max2.y),
                Mathf.Min(max1.z, max2.z));

            intersection.SetMinMax(intersectionMin, intersectionMax);

            return intersection;
        }

        public static float MaxSize(this Bounds bounds)
        {
            return Mathf.Max(Mathf.Max(bounds.size.x, bounds.size.y), bounds.size.z);
        }

        public static bool Valid(this Bounds bounds) {
            return bounds.size.x > 0 && bounds.size.y > 0 && bounds.size.z > 0;
        }

		public static void DrawGL(this Bounds bounds, Material axisMaterial) {

			var ext = bounds.extents;
			var center = bounds.center;

			axisMaterial.SetPass(0);

			GL.PushMatrix();
			GL.Begin(GL.LINES);
			GL.Color(Color.green);

			for (var i = 0; i < 4; i++) {
				// - + +, + + -, - - -, + - +
				var begin = new Vector3(ext.x * (i.IsOdd() ? 1 : -1), ext.y * (i / 2 < 1 ? 1 : -1), ext.z * (i % 3 == 0 ? 1 : -1));
				var beginGL = begin + center;
				for (var j = 0; j < 3; j++) {
					GL.Vertex3(beginGL.x, beginGL.y, beginGL.z);
					var end = begin;
					end[j] *= -1;
					var endGL = end + center;
					GL.Vertex3(endGL.x, endGL.y, endGL.z);
				}
			}
			GL.End();
			GL.PopMatrix();
		}

		public static void SafeInvoke <T> (this Action <T> action, T arg)
	    {
			if (action != null) {
				action (arg);
			}
	    }

        /// <summary>
        /// Выравнивание координат вектора по сетке крестовых коннекторов (все координаты вектора должны быть целые и либо все четные, либо все нечетные)
        /// Если ForceAlignment указан None, то выравнивание идет в сторону четности большинства координат вектора
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="forceAlignment"></param>
        /// <returns></returns>
        public static Vector3 AlignAsCross(this Vector3 vector)
        {
            var sum = (Mathf.RoundToInt(vector.x) & 1)
                       + (Mathf.RoundToInt(vector.y) & 1)
                       + (Mathf.RoundToInt(vector.z) & 1);
            var isFloor = Mathf.RoundToInt(vector.y) == 0;
            var isOddAlignment = !isFloor && sum > 1;

            var v = new Vector3(AlignValue(isOddAlignment, vector.x), 
                                AlignValue(isOddAlignment, vector.y), 
                                AlignValue(isOddAlignment, vector.z));

            /*Debug.Log("Old: " + vector + ", new: " + v + ", isOddAlignment: " + isOddAlignment + " " + (Mathf.RoundToInt(vector.x) & 1) + " "
                                + (Mathf.RoundToInt(vector.y) & 1) + " "
                                + (Mathf.RoundToInt(vector.z) & 1) + " " + sum);*/
            return v;
        }

		public static Vector3 AlignByCrossPoint(this Vector3 vector, Vector3 crossPoint) {
			var oddSum = (Mathf.RoundToInt(crossPoint.x) & 1)
					   + (Mathf.RoundToInt(crossPoint.y) & 1)
				       + (Mathf.RoundToInt(crossPoint.z) & 1);
			var isOddY = Mathf.RoundToInt(crossPoint.y).IsOdd();
			var isFloor = Mathf.RoundToInt(vector.y) == 0;
			var isOddAlignment = isFloor ? isOddY : (oddSum > 1);

			var newCrossPoint = new Vector3(AlignValue(isOddAlignment, crossPoint.x),
											AlignValue(isOddAlignment, crossPoint.y),
											AlignValue(isOddAlignment, crossPoint.z));

			var crossPointAlignment = newCrossPoint - crossPoint;
			/*Debug.Log("Old: " + vector + ", crossPointAlignment: " + crossPointAlignment + ", isOddAlignment: " + isOddAlignment + " " + (Mathf.RoundToInt(vector.x) & 1) + " "
								+ (Mathf.RoundToInt(vector.y) & 1) + " "
								+ (Mathf.RoundToInt(vector.z) & 1) + " " + oddSum
								 + ", isOddY: " + isOddY
								  + ", isFloor: " + isFloor
								   + ", newCrossPoint: " + newCrossPoint);*/
			return vector + crossPointAlignment;
		}

        /// <summary>
        /// Выравнивание координат вектора по сетке квадратных дырок-коннекторов (все координаты вектора должны быть целые и четность одной координаты должна отличаться)
        /// Если ForceAlignment указан None, то выравнивание идет в сторону четности большинства координат вектора
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="forceAlignment"></param>
        /// <returns></returns>
        public static Vector3 AlignAsSquare(this Vector3 vector)
        {
            var roundX = Mathf.RoundToInt(vector.x);
            var roundY = Mathf.RoundToInt(vector.y);
            var roundZ = Mathf.RoundToInt(vector.z);

            var isFloor = roundY == 0;
            var sum = (roundX & 1)
                       + (roundY & 1)
                       + (roundZ & 1);

            if (isFloor)
                return new Vector3(AlignValue(true, vector.x), roundY, AlignValue(true, vector.z));

            if (sum != 0 && sum != 3) 
                return new Vector3(roundX, roundY, roundZ);

            var deltaX = Mathf.Abs(vector.x - roundX);
            var deltaY = isFloor ? -1 : Mathf.Abs(vector.y - roundY);
            var deltaZ = Mathf.Abs(vector.z - roundZ);

            var maxDeltaIndex = deltaX > deltaY
                                    ? deltaX > deltaZ ? 0 : 2
                                    : deltaY > deltaZ ? 1 : 2;

            var newValue = AlignValue(!Mathf.RoundToInt(vector[maxDeltaIndex]).IsOdd(), vector[maxDeltaIndex]);
            
            var v = new Vector3(maxDeltaIndex == 0 ? newValue : roundX,
                                maxDeltaIndex == 1 ? newValue : roundY,
                                maxDeltaIndex == 2 ? newValue : roundZ);

            /*Debug.Log("Old: " + vector + ", new: " + v + ", isOddAlignment: " + " " + (Mathf.RoundToInt(vector.x) & 1) + " "
                                + (Mathf.RoundToInt(vector.y) & 1) + " "
                                + (Mathf.RoundToInt(vector.z) & 1) + " " + sum);*/
            return v;
        }

        private static int AlignValue(bool isOddAlignment, float value)
        {
            var roundValue = Mathf.RoundToInt(value);

            if (isOddAlignment != roundValue.IsOdd())
                return roundValue != (int) value
                    ? (int) value
                    : roundValue + (value != 0 ? Math.Sign(value) : 1);

            return roundValue;
        }

        public static bool IsOdd(this int value) {
            return (value & 1) == 1;
        }

        public static void TextRespectingRtl(this Text textComponent, string text)
        {
            var langIdx = UiLang.Lang;

            textComponent.text = text;

            if (text == "" || langIdx != 4 || Regex.IsMatch(text, "^[0-9a-zA-Z()\\s.,-]*$")) {
                return;
            }

            Canvas.ForceUpdateCanvases();

            var linesInfo = new List<UILineInfo>();
            var result = "";

            textComponent.cachedTextGenerator.GetLines(linesInfo);

            for (var i = 0; i < linesInfo.Count; i++) {
                var nextLineStartIdx = i + 1 < linesInfo.Count
                    ? linesInfo[i + 1].startCharIdx
                    : text.Length;
                var linesSeparator = char.IsWhiteSpace(text[nextLineStartIdx - 1])
                    ? text[nextLineStartIdx - 1].ToString()
                    : "";
                var line = text.Substring(linesInfo[i].startCharIdx, nextLineStartIdx - linesInfo[i].startCharIdx - (linesSeparator == "" ? 0 : 1));

                result += ReverseString(line) + linesSeparator 
                    + (linesSeparator != "\n" && nextLineStartIdx < text.Length ? "\n" : "");
            }
            textComponent.text = result;
        }

        private static string ReverseString(string text) 
        {
            var substingsWithoutReverse = Regex.Matches(text, "[0-9a-zA-Z()]+(\\s[0-9a-zA-Z()]+)*");
            var result = text.ToCharArray();

            Array.Reverse(result);

            foreach (Match substing in substingsWithoutReverse) {
                substing.Value.CopyTo(0, result, result.Length - substing.Index - substing.Length, substing.Length);
            }

            return new string(result);
        }

	    public static string Lang(this string key)
	    {
		    var language = UiLang.Lang;
		    string text;

		    if (UiLang.Dictionaries[language].TryGetValue(key, out text)) {
			    return text;
		    }

		    Debug.LogError("Key " + key + " not found in dictionary " + language);
		    return null;
	    }
    }

    [Serializable]
    public struct SerializableVector3Int {

        public int x;
        public int y;
        public int z;

        public SerializableVector3Int(float rX, float rY, float rZ) {
            x = Mathf.RoundToInt(rX);
            y = Mathf.RoundToInt(rY);
            z = Mathf.RoundToInt(rZ);
        }

        public override string ToString() {
            return string.Format("[{0}, {1}, {2}]", x, y, z);
        }

        public static implicit operator Vector3(SerializableVector3Int rValue) {
            return new Vector3(rValue.x, rValue.y, rValue.z);
        }

        public static implicit operator SerializableVector3Int(Vector3 rValue) {
            return new SerializableVector3Int(rValue.x, rValue.y, rValue.z);
        }
    }

	[Serializable]
	public struct SerializableVector3 {

		public float x;
		public float y;
		public float z;

		public SerializableVector3(float rX, float rY, float rZ)
		{
			// округляем дробную часть либо до ближайшего целого, либо до 0.5
			var rXDecimal = rX - (float) Math.Truncate(rX);
			var rYDecimal = rY - (float) Math.Truncate(rY);
			var rZDecimal = rZ - (float) Math.Truncate(rZ);

			var rXDecimalRounded = Mathf.Abs(rXDecimal - 0.5f * Mathf.Sign(rX)) < 0.3 ? 0.5f * Mathf.Sign(rX) : Mathf.RoundToInt(rXDecimal);
			var rYDecimalRounded = Mathf.Abs(rYDecimal - 0.5f * Mathf.Sign(rY)) < 0.3 ? 0.5f * Mathf.Sign(rY) : Mathf.RoundToInt(rYDecimal);
			var rZDecimalRounded = Mathf.Abs(rZDecimal - 0.5f * Mathf.Sign(rZ)) < 0.3 ? 0.5f * Mathf.Sign(rZ) : Mathf.RoundToInt(rZDecimal);

			x = (float) Math.Truncate(rX) + rXDecimalRounded;
			y = (float) Math.Truncate(rY) + rYDecimalRounded;
			z = (float) Math.Truncate(rZ) + rZDecimalRounded;
		}

		public override string ToString() {
			return string.Format("[{0}, {1}, {2}]", x, y, z);
		}

		public static implicit operator Vector3(SerializableVector3 rValue) {
			return new Vector3(rValue.x, rValue.y, rValue.z);
		}

		public static implicit operator SerializableVector3(Vector3 rValue) {
			return new SerializableVector3(rValue.x, rValue.y, rValue.z);
		}
	}
}
