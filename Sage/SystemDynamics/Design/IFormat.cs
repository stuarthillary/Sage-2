namespace Highpoint.Sage.SystemDynamics.Design
{
    public interface IFormat
    {
        double Precision
        {
            get;
        }
        double ScaleBy
        {
            get;
        }
        bool Delimit000s
        {
            get;
        }
        DisplayAs DisplayAs
        {
            get;
        }
    }
}
