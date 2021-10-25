using UnityEngine;

namespace Tofunaut.TofuECS.Unity
{
    public abstract class ECSAsset : ScriptableObject
    {
        public int AssetId
        {
            get => _assetId;
            internal set => _assetId = value;
        }
        
        [SerializeField, HideInInspector] private int _assetId;
    }
}