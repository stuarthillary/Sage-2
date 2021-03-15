namespace Highpoint.Sage.SystemDynamics.Design
{
    public interface IAux
    {
        string Name
        {
            get;
        }
        string Dimension
        {
            get;
        }
        string Documentation
        {
            get;
        }
        string Element
        {
            get;
        }
        string Equation
        {
            get;
        }
        IEventPoster EventPoster
        {
            get;
        }
        IFormat Format
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
        AccessType Access
        {
            get;
        }
    }
}
