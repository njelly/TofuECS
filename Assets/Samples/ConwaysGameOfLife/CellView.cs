using UnityEngine;
using UnityEngine.EventSystems;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife
{
    public class CellView : MonoBehaviour
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Color _offColor;
        [SerializeField] private Color _onColor;

        public void SetState(bool state)
        {
            _renderer.material.color = state ? _onColor : _offColor;
        }
    }
}
