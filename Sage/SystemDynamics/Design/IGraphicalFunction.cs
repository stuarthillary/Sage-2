namespace Highpoint.Sage.SystemDynamics.Design
{
    public interface IGraphicalFunction
    {
        GraphicalFunctionType InterpolationType
        {
            get;
        }
        double[] XPoints
        {
            get;
        }
        double[] YPoints
        {
            get;
        }
        string Name
        {
            get;
        }
    }
}
