using System;

namespace Tofunaut.TofuECS.Utilities
{
    public unsafe class ViewIdSystem : ISystem
    {
        public void Initialize(Frame f) { }

        public void Process(Frame f)
        {
            var viewIdIterator = f.GetIterator<ViewId>();
            while (viewIdIterator.NextUnsafe(out var entityId, out var viewId))
            {
                if (viewId->Id == viewId->PrevId)
                    continue;

                viewId->Id = viewId->PrevId;
                
                f.RaiseEvent(new OnViewIdChanged
                {
                    NewId = viewId->Id,
                    EntityId = entityId,
                });
            }
        }

        public void Dispose(Frame f) { }
    }

    public struct OnViewIdChanged : IDisposable
    {
        public int NewId;
        public int EntityId;

        public void Dispose() { }
    }
}