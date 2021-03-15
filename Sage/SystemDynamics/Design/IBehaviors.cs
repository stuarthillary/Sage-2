namespace Highpoint.Sage.SystemDynamics.Design
{
    public interface IBehaviors
    {
        IStockBehavior Stock
        {
            get;
        }
        IFlowBehavior Flow
        {
            get;
        }
        bool NonNegative
        {
            get;
        }
    }
}
