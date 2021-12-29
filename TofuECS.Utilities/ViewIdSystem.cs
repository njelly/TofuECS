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

                var eventData = new OnViewIdChangedEvent
                {
                    ViewId = viewId->Id,
                    PrevId = viewId->PrevId,
                    EntityId = entityId,
                };

                viewId->PrevId = viewId->Id;
                
                f.RaiseExternalEvent(eventData);
            }
        }

        public void Dispose(Frame f) { }
    }

    public struct OnViewIdChangedEvent
    {
        public int ViewId;
        public int PrevId;
        public int EntityId;
    }
}