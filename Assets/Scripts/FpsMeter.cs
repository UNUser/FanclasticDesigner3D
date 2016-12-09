using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts {
    public class FpsMeter : MonoBehaviour
    {
        public Text Text;
        public float UpdatesPerSecond = 10f;

        private int _frameCount;
        private float _dt;
        private float _fps;

        private void Update() {
            _frameCount++;
            _dt += Time.deltaTime;

            if (_dt > 1.0 / UpdatesPerSecond) {
                _fps = _frameCount / _dt;
                _frameCount = 0;
                _dt -= 1f / UpdatesPerSecond;

                Text.text = _fps.ToString("F");
            }
        }
    }
}
