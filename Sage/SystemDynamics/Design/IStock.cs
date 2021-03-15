using System.Collections.Generic;

namespace Highpoint.Sage.SystemDynamics.Design
{
    public interface IStock
    {
        string Name
        {
            get;
        }
        string VariableName
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
        string Dimension
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
        List<string> Inflows
        {
            get;
        }
        List<string> Outflows
        {
            get;
        }
        //string NonNegative { get; }
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

        string Queue
        {
            get;
        }
        string Conveyor
        {
            get;
        }
    }
}
