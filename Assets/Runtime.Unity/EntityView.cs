using UnityEngine;
using UnityEngine.Events;

namespace Tofunaut.TofuECS.Unity
{
    public class EntityView : MonoBehaviour
    {
        public int EntityId { get; private set; }
        public int PrefabId
        {
            get => _prefabId;
            internal set => _prefabId = value;
        }
        
        public UnityEvent OnInitialize;
        
        [ SerializeField ] private int _prefabId;
        
        internal void Initialize(int entityId)
        {
            EntityId = entityId;
            OnInitialize?.Invoke();
        }
    }
}