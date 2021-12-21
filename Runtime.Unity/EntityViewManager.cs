using System.Collections.Generic;
using UnityEngine;

namespace Tofunaut.TofuECS.Unity
{
    public class EntityViewManager
    {
        private readonly ECSDatabaseAsset _databaseAsset;
        private Dictionary<int, (EntityView entityView, Transform transform)> _entityToView;
        private Dictionary<int, Queue<EntityView>> _prefabIdToPool;

        public EntityViewManager(ECSDatabaseAsset databaseAsset)
        {
            _databaseAsset = databaseAsset;
            _entityToView = new Dictionary<int, (EntityView, Transform)>();
            _prefabIdToPool = new Dictionary<int, Queue<EntityView>>();
        }

        public EntityView GetEntityView(int entityId) =>
            !_entityToView.TryGetValue(entityId, out var tuple) ? null : tuple.Item1;

        public void RequestView(int entityId, int prefabId)
        {
            if (!_prefabIdToPool.TryGetValue(prefabId, out var pool))
            {
                pool = new Queue<EntityView>();
                _prefabIdToPool.Add(prefabId, pool);
            }

            // TODO: pre-instantiated pools?
            var entityView = pool.Count <= 0
                ? Object.Instantiate(_databaseAsset.GetEntityViewPrefab(prefabId)).GetComponent<EntityView>()
                : pool.Dequeue();

            _entityToView[entityId] = (entityView, entityView.transform);
            entityView.Initialize(entityId);
        }

        public void ReleaseView(int entityId)
        {
            if (!_entityToView.TryGetValue(entityId, out var tuple))
                return;
            
            tuple.entityView.CleanUp();
            _entityToView.Remove(entityId);
            tuple.entityView.gameObject.SetActive(false);
            _prefabIdToPool[tuple.Item1.PrefabId].Enqueue(tuple.Item1);
        }
    }
}