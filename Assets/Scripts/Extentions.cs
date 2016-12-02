using System;
using UnityEngine;

namespace Assets.Scripts {
    public static class Extentions {

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
