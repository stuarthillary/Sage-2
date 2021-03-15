namespace Highpoint.Sage.SystemDynamics.Design
{
    public interface IScale
    {
        double Min
        {
            get;
        }
        double Max
        {
            get;
        }
        bool Auto
        {
            get;
        }
        string Group
        {
            get;
        }
    }
}
