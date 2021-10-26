using System;

namespace Tofunaut.TofuECS.Unity
{
    public interface IECSAsset
    {
        Type DataType { get; }
        int AssetId { get; set; }
        string AssetName { get; }
        object GetECSData();
    }
}