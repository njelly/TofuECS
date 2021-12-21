using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tofunaut.TofuECS.Unity
{
    public class ECSDatabase : IECSDatabase
    {
        private readonly Dictionary<int, object> _data;

        public ECSDatabase(Dictionary<int, object> data)
        {
            _data = data;
        }

        public TData Get<TData>(int id) where TData : unmanaged => (TData) _data[id];
    }
    
    [CreateAssetMenu(fileName = "new ECSDatabase", menuName = "TofuECS/ECSDatabase")]
    public class ECSDatabaseAsset : ScriptableObject
    {
        [SerializeField] private AssetReference[] _eCSAssets = Array.Empty<AssetReference>();
        [SerializeField] private AssetReference[] _entityViews = Array.Empty<AssetReference>();
        [SerializeField, HideInInspector] private int[] _eCSAssetIds;
        [SerializeField, HideInInspector] private int[] _entityViewIds;
        [SerializeField, HideInInspector] private int _prevECSAssetsLength;
        [SerializeField, HideInInspector] private int _prevEntityViewsLength;

        private Dictionary<int, AssetReference> _ecsAssetsAssetRefsDict;
        private Dictionary<int, AssetReference> _entityViewsAssetRefsDict;
        private Dictionary<int, IECSAsset> _idToPreloadedEcsAsset;
        private Dictionary<int, GameObject> _idToPreloadedEntityView;

        private bool _isInitialized;
        private ECSDatabase _ecsDatabase;

        private void OnEnable()
        {
            _isInitialized = false;
            _prevECSAssetsLength = _eCSAssets.Length;
            _prevEntityViewsLength = _entityViews.Length;
        }

        public async Task PreloadAll()
        {
            if(!_isInitialized)
                Initialize();
            
            var assetIds = _ecsAssetsAssetRefsDict.Keys;
            var prefabIds = _entityViewsAssetRefsDict.Keys;
            await Preload(assetIds, prefabIds);
        }

        public async Task Preload(IEnumerable<int> assetIds, IEnumerable<int> prefabIds)
        {
            
            if(assetIds != null)
                await PreloadAssets(assetIds);
            
            if(prefabIds != null)
                await PreloadEntityViews(prefabIds);
        }

        private void Initialize()
        {
            _ecsAssetsAssetRefsDict ??= new Dictionary<int, AssetReference>();
            _ecsAssetsAssetRefsDict.Clear();

            for (var i = 0; i < _eCSAssetIds.Length; i++)
                _ecsAssetsAssetRefsDict.Add(_eCSAssetIds[i], _eCSAssets[i]);

            _entityViewsAssetRefsDict ??= new Dictionary<int, AssetReference>();
            _entityViewsAssetRefsDict.Clear();

            for (var i = 0; i < _entityViewIds.Length; i++)
                _entityViewsAssetRefsDict.Add(_entityViewIds[i], _entityViews[i]);

            _isInitialized = true;
        }

        private async Task PreloadAssets(IEnumerable<int> assetIds)
        {
            _idToPreloadedEcsAsset ??= new Dictionary<int, IECSAsset>();

            foreach (var assetId in assetIds)
            {
                if (!_ecsAssetsAssetRefsDict.TryGetValue(assetId, out var assetReference))
                    throw new ECSAssetNotRegisteredException(assetId);

                // I'm not sure if using the plural version of this API here (LoadAssetsAsync) while maintaining an
                // association with our custom key (the integer assetId) is currently possible.
                var asset = await Addressables.LoadAssetAsync<Object>(assetReference).Task;
                _idToPreloadedEcsAsset.Add(assetId, (IECSAsset)asset);
            }
        }
        
        private async Task PreloadEntityViews(IEnumerable<int> prefabIds)
        {
            _idToPreloadedEntityView ??= new Dictionary<int, GameObject>();

            foreach (var prefabId in prefabIds)
            {
                if (!_entityViewsAssetRefsDict.TryGetValue(prefabId, out var assetReference))
                    throw new EntityViewNotRegisteredException(prefabId);

                // I'm not sure if using the plural version of this API here (LoadAssetsAsync) while maintaining an
                // association with our custom key (the integer assetId) is currently possible.
                var asset = await Addressables.LoadAssetAsync<GameObject>(assetReference).Task;
                _idToPreloadedEntityView.Add(prefabId, asset);
            }
        }
        
        public TData GetECSData<TData>(int id) where TData : unmanaged
        {
            if (!_idToPreloadedEcsAsset.TryGetValue(id, out var asset))
                throw new ECSAssetNotLoadedException(id);

            if (asset.DataType == typeof(TData)) 
                return (TData)asset.GetECSData();
            
            Debug.LogError(
                $"The asset with id {id} contains data of type {asset.DataType}, not the requested {typeof(TData)}.");
            
            return default;
        }

        public ECSDatabase BuildECSDatabase() =>
            new ECSDatabase(_idToPreloadedEcsAsset.ToDictionary(x => x.Key, x => x.Value.GetECSData()));
        
        /// <summary>
        /// Returns the EntityView prefab with the given id. Fails if the asset has not been not been loaded before.
        /// </summary>
        public GameObject GetEntityViewPrefab(int prefabId)
        {
            if (!_idToPreloadedEntityView.TryGetValue(prefabId, out var asset))
                throw new EntityViewNotLoadedException(prefabId);

            return asset;
        }

        /// <summary>
        /// Returns the EntityView prefab with the given id. Awaits Addressable.LoadAssetAsync if the asset has not been loaded before.
        /// </summary>
        public async Task<GameObject> GetEntityViewPrefabAsync(int prefabId)
        {
            if (_idToPreloadedEntityView.TryGetValue(prefabId, out var asset)) 
                return asset;
            
            asset = await Addressables.LoadAssetAsync<GameObject>(_ecsAssetsAssetRefsDict[prefabId]).Task;
            _idToPreloadedEntityView[prefabId] = asset;

            return asset;
        }
        
#if UNITY_EDITOR
        
        private void OnValidate()
        {
            var didSetDirty = false;
            didSetDirty |= ValidateECSAssets();
            didSetDirty |= ValidateEntityViews();

            if (didSetDirty)
                EditorUtility.SetDirty(this);
        }

        private bool ValidateECSAssets()
        {
            var toRemove = new List<AssetReference>();
            var validECSAssets = new List<IECSAsset>();
            foreach (var assetReference in _eCSAssets)
            {
                if (assetReference.editorAsset == null)
                    continue;
                
                IECSAsset ecsAsset;
                try
                {
                    ecsAsset = (IECSAsset)assetReference.editorAsset;
                }
                catch (InvalidCastException e)
                {
                    Debug.LogError("ECSDatabase ECSAssets must be be a scriptable object derived from ECSAsset");
                    Debug.LogException(e);
                    toRemove.Add(assetReference);
                    continue;
                }
                
                if (ecsAsset == null)
                    continue;

                if (validECSAssets.Contains(ecsAsset))
                {
                    toRemove.Add(assetReference);
                    continue;
                }

                if (validECSAssets.Any(x => x.AssetId == ecsAsset.AssetId))
                {
                    ecsAsset.AssetId = 0;
                    Debug.LogWarning($"detected ECSAsset {ecsAsset.AssetName} with duplicate asset id, resetting it");
                }
                
                validECSAssets.Add(ecsAsset);
            }

            if (toRemove.Count > 0)
            {
                var validList = _eCSAssets.ToList();
                var numInvalid = validList.RemoveAll(x => toRemove.Contains(x));
                var invalidList = new AssetReference[numInvalid];
                _eCSAssets = validList.Concat(invalidList).ToArray();
            }

            _eCSAssetIds = validECSAssets.Select(x => x.AssetId).ToArray();
            
            if (validECSAssets.Count == _prevECSAssetsLength)
                return false;

            _prevECSAssetsLength = validECSAssets.Count;
            
            if (validECSAssets.Count <= 0) 
                return false;

            var unassignedArray = validECSAssets.Where(x => x != null && x.AssetId == 0).ToArray();
            if (unassignedArray.Length <= 0) 
                return false;

            var nextId = 1;
            if(validECSAssets.Count > 0)
                nextId = validECSAssets.Max(x => x.AssetId) + 1;
            
            foreach (var ecsAsset in unassignedArray)
            {
                ecsAsset.AssetId = nextId++;
                Debug.Log($"registered ECSDatabase EntityView {ecsAsset.AssetName}: {ecsAsset.AssetId}");
                EditorUtility.SetDirty((Object)ecsAsset);
            }

            return true;
        }

        private bool ValidateEntityViews()
        {
            var toRemove = new List<AssetReference>();
            var validEntityViews = new List<EntityView>();
            foreach (var assetReference in _entityViews)
            {
                GameObject entityViewObj;
                try
                {
                    entityViewObj = (GameObject)assetReference.editorAsset;
                }
                catch (InvalidCastException e)
                {
                    Debug.LogError("ECSDatabase EntityViews must be a prefab");
                    Debug.LogException(e);
                    toRemove.Add(assetReference);
                    continue;
                }
                
                if (entityViewObj == null)
                    continue;

                var entityView = entityViewObj.GetComponent<EntityView>();
                if (entityView == null)
                {
                    toRemove.Add(assetReference);
                    continue;
                }

                if (validEntityViews.Contains(entityView))
                {
                    toRemove.Add(assetReference);
                    continue;
                }

                if (validEntityViews.Any(x => x.PrefabId == entityView.PrefabId))
                {
                    entityView.PrefabId = 0;
                    Debug.LogWarning($"detected EntityView {entityView.name} with duplicate prefab id, resetting it");
                }
                
                validEntityViews.Add(entityView);
            }

            if (toRemove.Count > 0)
            {
                var validList = _entityViews.ToList();
                var numInvalid = validList.RemoveAll(x => toRemove.Contains(x));
                var invalidList = new AssetReference[numInvalid];
                _entityViews = validList.Concat(invalidList).ToArray();
            }
            
            _entityViewIds = validEntityViews.Select(x => x.PrefabId).ToArray();
            
            if (validEntityViews.Count == _prevEntityViewsLength)
                return false;

            _prevEntityViewsLength = validEntityViews.Count;
            
            if (validEntityViews.Count <= 0) 
                return false;

            var unassignedArray = validEntityViews.Where(x => x != null && x.PrefabId == 0).ToArray();
            if (unassignedArray.Length <= 0) 
                return false;

            var nextId = 1;
            if(validEntityViews.Count > 0)
                nextId = validEntityViews.Max(x => x.PrefabId) + 1;
            
            foreach (var entityView in unassignedArray)
            {
                entityView.PrefabId = nextId++;
                Debug.Log($"registered ECSDatabase EntityView {entityView.name}: {entityView.PrefabId}");
                EditorUtility.SetDirty(entityView.gameObject);
            }

            return true;
        }
#endif
    }

    public class ECSDatabaseNotInitializedException : Exception
    {
        public override string Message => "The ECS Database has not been initialized.";
    }  
    
    public class ECSAssetNotRegisteredException : Exception
    {
        private readonly int _id;

        public override string Message => $"The asset with id {_id} has not been registered.";

        public ECSAssetNotRegisteredException(int id)
        {
            _id = id;
        }
    }    
    
    public class ECSAssetNotLoadedException : Exception
    {
        private readonly int _id;

        public override string Message => $"The asset with id {_id} has not been loaded.";

        public ECSAssetNotLoadedException(int id)
        {
            _id = id;
        }
    }  
    
    public class EntityViewNotRegisteredException : Exception
    {
        private readonly int _id;

        public override string Message => $"The entity view  with id {_id} has not been registered.";

        public EntityViewNotRegisteredException(int id)
        {
            _id = id;
        }
    }    
    
    public class EntityViewNotLoadedException : Exception
    {
        private readonly int _id;

        public override string Message => $"The entity view with id {_id} has not been loaded.";

        public EntityViewNotLoadedException(int id)
        {
            _id = id;
        }
    }
}