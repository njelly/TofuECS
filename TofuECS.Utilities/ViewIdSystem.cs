namespace Tofunaut.TofuECS.Utilities
{
    public class ViewIdSystem : ISystem
    {
        public void Initialize(Simulation s) { }

        public void Process(Simulation s)
        {
            var viewIdIterator = s.Buffer<ViewId>().GetIterator();
            while (viewIdIterator.Next())
            {
                if (viewIdIterator.Current.Id == viewIdIterator.Current.PrevId)
                    return;
                
                var eventData = new OnViewIdChangedEvent
                {
                    ViewId = viewIdIterator.Current.Id,
                    PrevId = viewIdIterator.Current.PrevId,
                    EntityId = viewIdIterator.CurrentEntity,
                };

                viewIdIterator.ModifyCurrent((ref ViewId viewId) => { viewId.PrevId = viewId.Id; });
                
                s.QueueExternalEvent(eventData);
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