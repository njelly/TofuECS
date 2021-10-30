using System;
using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS
{
    public class Frame
    {
        public int Number { get; private set; }
        public bool IsVerified { get; private set; }
        public ISimulationConfig Config => _sim.Config;
        public int NumInputs => _inputs.Length;
        public Fix64 DeltaTime { get; internal set; }

        public XorShiftRandom RNG { get; }

        private readonly Simulation _sim;
        private IComponentBuffer[] _componentBuffers;
        private IEntityComponentIterator[] _iterators;
        private readonly Input[] _inputs;
        private readonly EntityBuffer _entityBuffer;

        public Frame(Simulation sim, int numInputs)
        {
            _sim = sim;
            Number = 0;
            RNG = new XorShiftRandom(_sim.Config.Seed);
            _componentBuffers = Array.Empty<IComponentBuffer>();
            _iterators = Array.Empty<IEntityComponentIterator>();
            _inputs = new Input[numInputs];
            _entityBuffer = new EntityBuffer();
        }

        public int CreateEntity() => _entityBuffer.Request();

        public void DestroyEntity(int entityId)
        {
            var entity = _entityBuffer.Get(entityId);
            entity.Destroy(Number);
            _entityBuffer.Release(entity);
            RaiseEvent(new OnEntityDestroyedEvent
            {
                EntityId = entityId,
            });
        }

        /// <summary>
        /// Returns true if the entity with the id has been created and has NOT been destroyed.
        /// </summary>
        public bool IsValid(int entityId) =>
            _entityBuffer.TryGet(entityId, out var entity) && !entity.IsDestroyed(this);

        public void AddComponent<TComponent>(int entityId) where TComponent : unmanaged
        {
            var entity = _entityBuffer.Get(entityId);
            var typeIndex = _sim.GetIndexForType(typeof(TComponent));
            entity.AssignComponent(typeof(TComponent), Number, IsVerified, _componentBuffers[typeIndex].Request());
            _iterators[typeIndex].AddEntity(entityId);
        }

        public void RemoveComponent<TComponent>(int entityId) where TComponent : unmanaged =>
            RemoveComponent(typeof(TComponent), entityId);

        private void RemoveComponent(Type type, int entityId)
        {
            var entity = _entityBuffer.Get(entityId);
            
            if (!entity.TryGetComponentIndex(type, out var index))
                return;

            var typeIndex = _sim.GetIndexForType(type);
            _componentBuffers[typeIndex].Release(index);
            _iterators[typeIndex].RemoveEntity(entityId);
        }

        public TComponent GetComponent<TComponent>(int entityId) where TComponent : unmanaged
        {
            var entity = _entityBuffer.Get(entityId);
            if (!entity.TryGetComponentIndex(typeof(TComponent), out var index))
                throw new EntityDoesNotContainComponentException<TComponent>(entityId);
            
            var typeIndex = _sim.GetIndexForType(typeof(TComponent));
            return ((ComponentBuffer<TComponent>)_componentBuffers[typeIndex]).Get(index);
        }

        public unsafe TComponent* GetComponentUnsafe<TComponent>(int entityId) where TComponent : unmanaged
        {
            var entity = _entityBuffer.Get(entityId);
            if (!entity.TryGetComponentIndex(typeof(TComponent), out var index))
                throw new EntityDoesNotContainComponentException<TComponent>(entityId);

            var typeIndex = _sim.GetIndexForType(typeof(TComponent));
            return ((ComponentBuffer<TComponent>)_componentBuffers[typeIndex]).GetUnsafe(index);
        }

        public bool TryGetComponent<TComponent>(int entityId, out TComponent component) where TComponent : unmanaged
        {
            var entity = _entityBuffer.Get(entityId);
            if (!entity.TryGetComponentIndex(typeof(TComponent), out var index))
            {
                component = default;
                return false;
            }

            var typeIndex = _sim.GetIndexForType(typeof(TComponent));
            component = ((ComponentBuffer<TComponent>)_componentBuffers[typeIndex]).Get(index);
            return true;
        }

        public unsafe bool TryGetComponentUnsafe<TComponent>(int entityId, out TComponent* component) where TComponent : unmanaged
        {
            var entity = _entityBuffer.Get(entityId);
            if (!entity.TryGetComponentIndex(typeof(TComponent), out var index))
            {
                component = null;
                return false;
            }

            var typeIndex = _sim.GetIndexForType(typeof(TComponent));
            component = ((ComponentBuffer<TComponent>)_componentBuffers[typeIndex]).GetUnsafe(index);
            return true;
        }

        public EntityComponentIterator<TComponent> GetIterator<TComponent>() where TComponent : unmanaged
        {
            var typeIndex = _sim.GetIndexForType(typeof(TComponent));
            var iterator = (EntityComponentIterator<TComponent>)_iterators[typeIndex];
            iterator.Reset(this);
            iterator.buffer = (ComponentBuffer<TComponent>)_componentBuffers[typeIndex];
            return iterator;
        }

        public Input GetInput(int index) => _inputs[index];
        public TInput GetInput<TInput>(int index) where TInput : Input => GetInput(index) as TInput;
        
        public void RaiseEvent<TEventData>(TEventData data) where TEventData : unmanaged, IDisposable =>
            _sim.EventDispatcher.Invoke(this, data);

        public void LogInfo(string s) => _sim.Log.Info(s);
        public void LogWarn(string s) => _sim.Log.Warn(s);
        public void LogError(string s) => _sim.Log.Error(s);
        public void LogException(Exception e) => _sim.Log.Exception(e);

        internal Entity GetEntity(int entityId) => _entityBuffer.Get(entityId);

        internal void Recycle(Frame prevFrame)
        {
            Number = prevFrame.Number + 1;
            IsVerified = false;

            for (var i = 0; i < prevFrame._componentBuffers.Length; i++)
            {
                _componentBuffers[i].Recycle(prevFrame._componentBuffers[i]);
                _iterators[i].Recycle(prevFrame._iterators[i]);
            }
            
            _entityBuffer.Recycle(prevFrame._entityBuffer);
            RNG.CopyState(prevFrame.RNG);

            Array.Copy(prevFrame._inputs, _inputs, _inputs.Length);
        }

        internal void Verify()
        {
            IsVerified = true;
        }

        internal void RegisterComponent<TComponent>() where TComponent : unmanaged
        {
            var newComponentBufferArray = new IComponentBuffer[_componentBuffers.Length + 1];
            Array.Copy(_componentBuffers, newComponentBufferArray, _componentBuffers.Length);
            _componentBuffers = newComponentBufferArray;
            var newComponentBuffer = new ComponentBuffer<TComponent>();
            _componentBuffers[_componentBuffers.Length - 1] = newComponentBuffer;

            var newIteratorArray = new IEntityComponentIterator[_iterators.Length + 1];
            Array.Copy(_iterators, newIteratorArray, _iterators.Length);
            _iterators = newIteratorArray;
            _iterators[_iterators.Length - 1] = new EntityComponentIterator<TComponent>();
        }

        internal void CopyInputs(Input[] inputs) => Array.Copy(inputs, _inputs, _inputs.Length);
    }

    public class EntityDoesNotContainComponentException<TComponent> : Exception where TComponent : unmanaged
    {
        private readonly int _entityId;
        public EntityDoesNotContainComponentException(int entityId)
        {
            _entityId = entityId;
        }
        public override string Message => $"the entity {_entityId} does not contain the component {nameof(TComponent)}";
    }

    public struct OnEntityDestroyedEvent : IDisposable
    {
        public int EntityId;
        public void Dispose() { }
    }
}
