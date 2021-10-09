
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
        [SerializeField] private Text _tickNumberLabel;
        [SerializeField] private InputField _seedInputField;
        [SerializeField] private Button _resetButton;

        private float _tickTimer;
        private bool _isPaused;

        private void Start()
        {
            _tickButton.onClick.RemoveAllListeners();
            _tickButton.onClick.AddListener(() => 
            {
                _simulationRunner.DoTick();
                _tickNumberLabel.text = $"Tick: {_simulationRunner.FrameNumber}";
            });

            _pauseButton.onClick.RemoveAllListeners();
            _pauseButton.onClick.AddListener(() => 
            {
                _isPaused = !_isPaused;
                _pauseButtonLabel.text = _isPaused ? "Unpause" : "Pause";
            });

            _tickIntervalSlider.SetValueWithoutNotify(0f);

            _seedInputField.text = _simulationRunner.Seed.ToString();

            _resetButton.onClick.RemoveAllListeners();
            _resetButton.onClick.AddListener(() =>
            {
                _simulationRunner.Reset(_seedInputField.text.GetHashCode());
            });
        }

        private void Update()
        {
            if (_isPaused)
                return;

            _tickIntervalSliderText.text = $"Frame Interval: {_tickIntervalSlider.value.ToString("F2")}";

            _tickTimer -= Time.deltaTime;
            if(_tickTimer < 0)
            {
                _tickTimer += _tickIntervalSlider.value;
                _simulationRunner.DoTick();
                _tickNumberLabel.text = $"Frame: {_simulationRunner.FrameNumber}";
            }
        }
    }
}
