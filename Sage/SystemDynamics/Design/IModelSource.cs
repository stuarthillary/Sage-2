using System.Collections.Generic;

namespace Highpoint.Sage.SystemDynamics.Design
{
    public interface IModelSource
    {
        ICodeGenSpecs CodeGenSpecs
        {
            get;
        }
        IBehaviors Behaviors
        {
            get;
        }
        IData Data
        {
            get;
        }
        IDimensions Dimensions
        {
            get;
        }
        IEnumerable<IMacro> Macros
        {
            get;
        }
        IModel Model
        {
            get;
        }
        IModelUnits ModelUnits
        {
            get;
        }
        ISimSpecs SimSpecs
        {
            get;
        }
        IModelStyle Style
        {
            get;
        }
    }
}
