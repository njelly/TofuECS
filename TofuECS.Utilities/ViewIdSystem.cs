namespace Tofunaut.TofuECS.Utilities
{
    public class ViewIdSystem : ISystem
    {
        public void Initialize(ECS ecs) { }

        public void Process(ECS ecs)
        {
            var viewIdIterator = ecs.Buffer<ViewId>().GetIterator();
            while (viewIdIterator.Next())
            {
                if (viewIdIterator.Current.Id == viewIdIterator.Current.PrevId)
                    return;

                viewIdIterator.ModifyCurrent((ref ViewId viewId) => { viewId.PrevId = viewId.Id; });
                
                var eventData = new OnViewIdChangedEvent
                {
                    ViewId = viewIdIterator.Current.Id,
                    PrevId = viewIdIterator.Current.PrevId,
                    EntityId = viewIdIterator.CurrentEntity,
                };
                
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