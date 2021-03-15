using System.Collections.Generic;

namespace Highpoint.Sage.SystemDynamics.Design
{
    public interface IMacro
    {
        string Namespace
        {
            get;
        }
        string Name
        {
            get;
        }
        MacroFilter Filter
        {
            get;
        }
        MacroApplyTo ApplyTo
        {
            get;
        }
        ISimSpecs SimSpecs
        {
            get;
        }
        IVariables Variables
        {
            get;
        }
        IViews Views
        {
            get;
        }
        string Doc
        {
            get;
        }
        string Equation
        {
            get;
        }
        IFormat Format
        {
            get;
        }
        List<string> Parameters
        {
            get;
        }
    }
}
