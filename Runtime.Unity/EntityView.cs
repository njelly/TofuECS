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
        
        /// <summary>
        /// Invoked when this EntityView is initialized by the EntityViewManager.
        /// </summary>
        public UnityEvent OnInitialize;
        
        /// <summary>
        /// Invoked when this EntityView is released by the EntityViewManager, before it is disabled.
        /// </summary>
        public UnityEvent OnCleanedUp;
        
        [SerializeField, HideInInspector] private int _prefabId;

        internal void Initialize(int entityId)
        {
            EntityId = entityId;
            OnInitialize?.Invoke();
        }

        internal void CleanUp()
        {
            OnCleanedUp?.Invoke();
        }
    }
}