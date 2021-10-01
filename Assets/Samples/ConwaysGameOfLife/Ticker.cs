
using UnityEngine;
using UnityEngine.UI;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife
{
    public unsafe class Ticker : MonoBehaviour
    {
        [SerializeField] private SimulationRunner _simulationRunner;
        [SerializeField] private Button _tickButton;
        [SerializeField] private Slider _tickIntervalSlider;
        [SerializeField] private Text _tickIntervalSliderText;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Text _pauseButtonLabel;

        private float _tickTimer;
        private float _tickInterval = 0.017f;
        private bool _isPaused;

        private void Awake()
        {
            _tickButton.onClick.RemoveAllListeners();
            _tickButton.onClick.AddListener(() => 
            {
                _simulationRunner.DoTick();
            });

            void UpdateIntervalSliderText(float interval)
            {
                _tickIntervalSliderText.text = $"Tick Interval: {interval.ToString("F2")}";
            }

            _tickIntervalSlider.onValueChanged.RemoveAllListeners();
            _tickIntervalSlider.onValueChanged.AddListener(x =>
            {
                _tickInterval = x;
                UpdateIntervalSliderText(x);
            });

            _tickIntervalSlider.SetValueWithoutNotify(_tickInterval);
            UpdateIntervalSliderText(_tickInterval);

            _pauseButton.onClick.RemoveAllListeners();
            _pauseButton.onClick.AddListener(() => 
            {
                _isPaused = !_isPaused;
                _pauseButtonLabel.text = _isPaused ? "Unpause" : "Pause";
            });
        }

        private void Update()
        {
            if (_isPaused)
                return;

            _tickTimer -= Time.deltaTime;
            if(_tickTimer < 0)
            {
                _tickTimer += _tickInterval;
                _simulationRunner.DoTick();
            }
        }
    }
}
