using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tofunaut.TofuECS.Unity
{
    
    [CreateAssetMenu(fileName = "new ECSDatabase", menuName = "TofuECS/ECSDatabase")]
    public class ECSDatabase : ScriptableObject
    {
        [SerializeField] private AssetReference[] _eCSAssets = Array.Empty<AssetReference>();
        [SerializeField] private AssetReference[] _entityViews = Array.Empty<AssetReference>();
        [SerializeField, HideInInspector] private int _prevECSAssetsLength;
        [SerializeField, HideInInspector] private int _prevEntityViewsLength;

        public bool IsBuilt { get; private set; }

        private Dictionary<int, AssetReference> _ecsAssetsAssetRefsDict;
        private Dictionary<int, AssetReference> _entityViewsAssetRefsDict;
        private Dictionary<int, ECSAsset> _idToPreloadedEcsAsset;
        private Dictionary<int, GameObject> _idToPreloadedEntityView;

        private bool _isInitialized;

        private void OnEnable()
        {
            _prevECSAssetsLength = _eCSAssets.Length;
            _prevEntityViewsLength = _entityViews.Length;
        }

        public async Task Preload(IEnumerable<int> assetIds, IEnumerable<int> prefabIds)
        {
            if(!_isInitialized)
                Initialize();
            
            if(assetIds != null)
                await PreloadAssets(assetIds);
            
            if(prefabIds != null)
                await PreloadEntityViews(prefabIds);
        }

        private void Initialize()
        {
            _ecsAssetsAssetRefsDict ??= new Dictionary<int, AssetReference>();
            _ecsAssetsAssetRefsDict.Clear();

            foreach (var assetReference in _eCSAssets)
                _ecsAssetsAssetRefsDict.Add(((ECSAsset)assetReference.Asset).AssetId, assetReference);

            _entityViewsAssetRefsDict ??= new Dictionary<int, AssetReference>();
            _entityViewsAssetRefsDict.Clear();

            foreach (var assetReference in _entityViews)
                _entityViewsAssetRefsDict.Add(((EntityView)assetReference.Asset).PrefabId, assetReference);

            _isInitialized = true;
        }

        private async Task PreloadAssets(IEnumerable<int> assetIds)
        {
            _idToPreloadedEcsAsset ??= new Dictionary<int, ECSAsset>();

            foreach (var assetId in assetIds)
            {
                if (!_ecsAssetsAssetRefsDict.TryGetValue(assetId, out var assetReference))
                    throw new ECSAssetNotRegisteredException(assetId);

                // I'm not sure if using the plural version of this API here (LoadAssetsAsync) while maintaining an
                // association with our custom key (the integer assetId) is currently possible.
                var asset = await Addressables.LoadAssetAsync<ECSAsset>(assetReference).Task;
                _idToPreloadedEcsAsset.Add(assetId, asset);
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

        /// <summary>
        /// Returns the asset with the given id. Fails if the asset has not been not been loaded before.
        /// </summary>
        public TAsset GetAsset<TAsset>(int assetId) where TAsset : ECSAsset
        {
            if (!_idToPreloadedEcsAsset.TryGetValue(assetId, out var asset))
                throw new ECSAssetNotLoadedException<TAsset>(assetId);

            return (TAsset)asset;
        }

        /// <summary>
        /// Returns the asset with the given id. Awaits Addressable.LoadAssetAsync if the asset has not been loaded before.
        /// </summary>
        public async Task<TAsset> GetAssetAsync<TAsset>(int assetId) where TAsset : ECSAsset
        {
            if (_idToPreloadedEcsAsset.TryGetValue(assetId, out var asset)) 
                return (TAsset)asset;
            
            asset = await Addressables.LoadAssetAsync<TAsset>(_ecsAssetsAssetRefsDict[assetId]).Task;
            _idToPreloadedEcsAsset[assetId] = asset;

            return (TAsset)asset;
        } 
        
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
                IsBuilt = false;
        }

        public void Build()
        {
            ValidateECSAssets();
            ValidateEntityViews();
            
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            
            IsBuilt = true;
        }

        private bool ValidateECSAssets()
        {
            var nonNullECSAssets = _eCSAssets.Where(x => x.editorAsset != null).ToArray();
            
            if (nonNullECSAssets.Length == _prevECSAssetsLength)
                return false;

            _prevECSAssetsLength = nonNullECSAssets.Length;
            
            if (nonNullECSAssets.Length <= 0) 
                return false;

            var unassignedList = nonNullECSAssets.Where(x => x != null && x.editorAsset is ECSAsset ecsAsset && ecsAsset.AssetId == 0).ToList();
            if (unassignedList.Count <= 0) 
                return false;

            var nextId = nonNullECSAssets.Max(x => ((ECSAsset)x.editorAsset).AssetId) + 1;
            foreach (var assetRef in unassignedList)
            {
                ((ECSAsset)assetRef.Asset).AssetId = nextId++;
                EditorUtility.SetDirty(assetRef.Asset);
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
                    Debug.LogError($"detected EntityView {entityView.name} with duplicate prefab id, resetting it");
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

    public class ECSAssetNotRegisteredException<TAsset> : Exception where TAsset : ECSAsset
    {
        private readonly int _id;

        public override string Message => $"The asset of type {nameof(TAsset)} with id {_id} has not been registered.";

        public ECSAssetNotRegisteredException(int id)
        {
            _id = id;
        }
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
    
    public class ECSAssetNotLoadedException<TAsset>: Exception where TAsset : ECSAsset
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