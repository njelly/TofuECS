using System;
using UnityEngine;

namespace Tofunaut.TofuECS.Unity
{
    public abstract class ECSAsset<TData> : ScriptableObject, IECSAsset where TData : unmanaged
    {
        public Type DataType => typeof(TData);

        public int AssetId
        {
            get => _assetId;
            set => _assetId = value;
        }

        public string AssetName => name;

        public object GetECSData()
        {
            return BuildECSData();
        }

        [SerializeField, HideInInspector] private int _assetId;

        protected abstract TData BuildECSData();
    }
}