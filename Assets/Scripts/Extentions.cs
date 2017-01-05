using System;
using UnityEngine;
using Object = UnityEngine.Object;

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

        public static DetailBase RootParent(this Transform transform)
        {
            return Object.FindObjectOfType<ApplicationController>().SelectedDetail.GetComponent<DetailBase>();
                //transform.root.GetComponent<DetailBase>();
        }

        public static void SetRootParent(this Transform transform, Transform value) {
                if (transform.parent != null) {
                    transform.parent.GetComponent<DetailsGroup>().ChildDetached();
                }
                transform.SetParent(value != null ? value.transform : null);
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
                    : roundValue + Math.Sign(value);

            return roundValue;
        }

        public static bool IsOdd(this int value) {
            return (value & 1) == 1;
        }
    }
}
