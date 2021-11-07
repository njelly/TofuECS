using Tofunaut.TofuECS.Samples.ConwaysGameOfLife.ECS;
using Tofunaut.TofuECS.Unity;
using UnityEngine;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife
{
    //[CreateAssetMenu(menuName = "ConwaysGameOfLife/BoardConfigAsset")]
    public class BoardConfigAsset : ECSAsset<BoardConfig>
    {
        [SerializeField] private EntityView _boardView;
        [SerializeField] private int _width;
        [SerializeField] private int _height;
        [SerializeField] private float _startStaticThreshold;
        
        protected override BoardConfig BuildECSData()
        {
            return new BoardConfig
            {
                ViewId = _boardView.PrefabId,
                Width = _width,
                Height = _height,
                StartStaticThreshold = _startStaticThreshold,
            };
        }
    }
}