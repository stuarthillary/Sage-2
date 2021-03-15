namespace Highpoint.Sage.SystemDynamics.Design
{
    public interface IFlow
    {
        string Dimension
        {
            get;
        }
        string Name
        {
            get;
        }
        string Equation
        {
            get;
        }
        string Documentation
        {
            get;
        }
        IEventPoster EventPoster
        {
            get;
        }
        IGraphicalFunction GraphicalFunction
        {
            get;
        }
        string MathML
        {
            get;
        }
        IRange Range
        {
            get;
        }
        IScale Scale
        {
            get;
        }
        string Units
        {
            get;
        }
        //string Leak { get; }
        //string LeakIntegers { get; }
        double Multiplier
        {
            get;
        }
        //string NonNegative { get; }ative = false;
        //string Overflow { get; }
        AccessType Access
        {
            get;
        }
    }
}
