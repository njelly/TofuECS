
using UnityEngine;
using UnityEngine.UI;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife
{
    public unsafe class Ticker : MonoBehaviour
    {
        [SerializeField] private SimulationRunner _simulationRunner;
        [SerializeField] private Button _tickButton;
        [SerializeField, Range(-1, 2)] private float _tickInterval;

        private float _tickTimer;

        private void Awake()
        {
            _tickButton.onClick.RemoveAllListeners();
            _tickButton.onClick.AddListener(() => _simulationRunner.DoTick());
        }

        private void Update()
        {
            if (_tickInterval <= 0)
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
