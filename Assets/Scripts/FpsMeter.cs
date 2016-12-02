using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts {
    class FpsMeter : MonoBehaviour
    {
        public Text Text;

        private int frameCount = 0;
        private float dt = 0f;
        private float fps = 0f;
        private float updateRate = 10f;  // 4 updates per sec.

        private void Update() {
            frameCount++;
            dt += Time.deltaTime;

            if (dt > 1.0 / updateRate) {
                fps = frameCount / dt;
                frameCount = 0;
                dt -= 1f / updateRate;

                Text.text = fps.ToString("F");
            }
        }

    }
}
