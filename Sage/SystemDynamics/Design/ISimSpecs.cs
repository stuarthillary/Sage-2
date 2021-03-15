namespace Highpoint.Sage.SystemDynamics.Design
{
    public interface ISimSpecs
    {
        double Start
        {
            get;
        }
        double Stop
        {
            get;
        }
        string TimeUnits
        {
            get;
        }
        string Method
        {
            get;
        }
        double Pause
        {
            get;
        }
        double DeltaTime
        {
            get;
        }
        RunSpecs Run
        {
            get;
        }
    }
}
