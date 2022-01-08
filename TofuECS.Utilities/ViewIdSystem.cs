namespace Tofunaut.TofuECS.Utilities
{
    public unsafe class ViewIdSystem : ISystem
    {
        public void Initialize(ECS ecs) { }

        public void Process(ECS ecs)
        {
            var viewIdIterator = ecs.GetIterator<ViewId>();
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
                
                ecs.QueueExternalEvent(eventData);
            }
        }
    }

    public struct OnViewIdChangedEvent
    {
        public int ViewId;
        public int PrevId;
        public int EntityId;
    }
}