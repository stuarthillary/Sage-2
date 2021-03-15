namespace Highpoint.Sage.SystemDynamics.Design
{
    public interface IEventPosterThresholdEvent
    {
        object[] Items
        {
            get;
        }

        EventPosterThresholdEventSimAction SimAction
        {
            get;
        }
    }
}
