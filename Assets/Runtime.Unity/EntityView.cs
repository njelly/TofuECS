using UnityEngine;
using UnityEngine.Events;

namespace Tofunaut.TofuECS.Unity
{
    public sealed class EntityView : MonoBehaviour
    {
        public int EntityId { get; private set; }
        public int PrefabId
        {
            get => _prefabId;
            internal set => _prefabId = value;
        }
        
        public UnityEvent OnInitialize;
        
        [SerializeField, HideInInspector] private int _prefabId;
        
        internal void Initialize(int entityId)
        {
            EntityId = entityId;
            OnInitialize?.Invoke();
        }
    }
}