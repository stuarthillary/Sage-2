/* This source code licensed under the GNU Affero General Public License */
using System.Collections;
using PN = Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.ParamNames;

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
    /// <summary>
    /// This model is used to calculate the emissions associated with vacuum 
    /// distillation.  The calculation of the emission from the operation is 
    /// identical to that for the Vacuum Distill model.  This model incorporates 
    /// the effect of the vacuum jet scrubbers into the emission calculation.  
    /// The vacuum jet scrubbers are used to condense the steam exiting from the 
    /// vacuum jet but they also condense solvent vapors through direct contact 
    /// heat exchange.
    /// <p>The assumptions of the new model are similar to that of the existing 
    /// vacuum distill model.  Air leaks into the system under vacuum and becomes 
    /// saturated with solvent vapors.  With the vacuum distill model it is 
    /// assumed that condensation of some fraction of these vapors occurs in the
    /// primary condenser and any uncondensed vapor is exhausted to the atmosphere
    /// (via control devices, if any).  With this model, the vacuum jet scrubber
    /// acts as the final control device, assuming that a vacuum jet is being used
    /// to evacuate the system.  The vacuum jet scrubber condenses vapors, which
    /// remain uncondensed by the primary condenser, through direct contact heat
    /// exchange with the scrubber water.</p>
    /// </summary>
    public class VacuumDistillationWScrubberModel : EmissionModel
    {

        /// <summary>
        /// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
        /// an emission model that it has never seen before.
        /// <p></p>In order to successfully call the Vacuum Distillation With Scrubber model on this API, the parameters hashtable
        /// must include the following entries (see the VacuumDistillationWScrubber(...) method for details):<p></p>
        /// &quot;AirLeakRate&quot;, &quot;AirLeakDuration&quot;, &quot;SystemPressure&quot; and &quot;ControlTemperature&quot;. If there
        /// is no entry under &quot;SystemPressure&quot;, then this method looks for entries under &quot;InitialPressure&quot; 
        /// and &quot;FinalPressure&quot; and uses their average.
        /// </summary>
        /// <param name="initial">The initial mixture on which the emission model is to run.</param>
        /// <param name="final">The final mixture that is delivered after the emission model has run.</param>
        /// <param name="emission">The mixture that is evolved in the process of the emission.</param>
        /// <param name="modifyInPlace">True if the initial mixture is to be modified by the service.</param>
        /// <param name="parameters">A hashtable of name/value pairs containing additional parameters.</param>
        public override void Process(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            Hashtable parameters)
        {

            PrepareToReadLateBoundParameters();

            //			double airLeakRate = (double)parameters[PN.AirLeakRate_KgPerMin];
            //			double airLeakDuration = (double)parameters[PN.AirLeakDuration_Min];
            //			double controlTemperature = (double)parameters[PN.ControlTemperature_K];
            //			double systemPressure = GetSystemPressure(parameters);

            double airLeakRate = double.NaN;
            TryToRead(ref airLeakRate, PN.AirLeakRate_KgPerMin, parameters);
            double airLeakDuration = double.NaN;
            TryToRead(ref airLeakDuration, PN.AirLeakDuration_Min, parameters);
            double controlTemperature = double.NaN;
            TryToRead(ref controlTemperature, PN.ControlTemperature_K, parameters);
            double systemPressure = GetSystemPressure(parameters);

            EvaluateSuccessOfParameterReads();

            VacuumDistillationWScrubber(initial, out final, out emission, modifyInPlace, controlTemperature, systemPressure, airLeakRate, airLeakDuration);

            ReportProcessCall(this, initial, final, emission, parameters);

        }

        #region >>> Usability Support <<<
        private static readonly string description = "This model is used to calculate the emissions associated with vacuum distillation.  The calculation of the emission from the operation is identical to that for the Vacuum Distill model.  This model incorporates  the effect of the vacuum jet scrubbers into the emission calculation.  The vacuum jet scrubbers are used to condense the steam exiting from the  vacuum jet but they also condense solvent vapors through direct contact heat exchange.\r\nThe assumptions of the new model are similar to that of the existing vacuum distill model.  Air leaks into the system under vacuum and becomes saturated with solvent vapors.  With the vacuum distill model it is assumed that condensation of some fraction of these vapors occurs in the primary condenser and any uncondensed vapor is exhausted to the atmosphere (via control devices, if any).  With this model, the vacuum jet scrubber acts as the final control device, assuming that a vacuum jet is being used to evacuate the system.  The vacuum jet scrubber condenses vapors, which remain uncondensed by the primary condenser, through direct contact heat exchange with the scrubber water.";
        private static readonly EmissionParam[] parameters
            = {
                                     new EmissionParam(PN.AirLeakRate_KgPerMin,"Air leak rate into the system, in kilograms per time unit."),
                                     new EmissionParam(PN.AirLeakDuration_Min,"Air leak rate into the system, in the AirLeakRate's time units."),
                                     new EmissionParam(PN.ControlTemperature_K,"The control or condenser temperature, in degrees Kelvin."),
                                     new EmissionParam(PN.SystemPressure_P,"The pressure of the system during the emission operation, in Pascals. This parameter can also be called \"Final Pressure\".")
                                 };
        private static readonly string[] keys = { "Vacuum Distillation With Scrubber", "Vacuum Distillation w/ Scrubber" };
        /// <summary>
        /// This is a description of what emissions mode this model computes (such as Air Dry, Gas Sweep, etc.)
        /// </summary>
        public override string ModelDescription => description;

        /// <summary>
        /// This is the list of parameters this model uses, and therefore expects as input.
        /// </summary>
        public override EmissionParam[] Parameters => parameters;

        /// <summary>
        /// The keys which, when fed to the Emissions Service's ProcessEmission method, determines
        /// that this model is to be called.
        /// </summary>
        public override string[] Keys => keys;

        #endregion

        /// <summary>
        /// This model is used to calculate the emissions associated with vacuum 
        /// distillation.  The calculation of the emission from the operation is 
        /// identical to that for the Vacuum Distill model.  This model incorporates 
        /// the effect of the vacuum jet scrubbers into the emission calculation.  
        /// The vacuum jet scrubbers are used to condense the steam exiting from the 
        /// vacuum jet but they also condense solvent vapors through direct contact 
        /// heat exchange.
        /// <p>The assumptions of the new model are similar to that of the existing 
        /// vacuum distill model.  Air leaks into the system under vacuum and becomes 
        /// saturated with solvent vapors.  With the vacuum distill model it is 
        /// assumed that condensation of some fraction of these vapors occurs in the
        /// primary condenser and any uncondensed vapor is exhausted to the atmosphere
        /// (via control devices, if any).  With this model, the vacuum jet scrubber
        /// acts as the final control device, assuming that a vacuum jet is being used
        /// to evacuate the system.  The vacuum jet scrubber condenses vapors, which
        /// remain uncondensed by the primary condenser, through direct contact heat
        /// exchange with the scrubber water.</p>
        /// </summary>
        /// <param name="initial">The mixture as it exists before the emission.</param>
        /// <param name="final">The resultant mixture after the emission.</param>
        /// <param name="emission">The mixture emitted as a result of this model.</param>
        /// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
        /// <param name="controlTemperature">In degrees Kelvin.</param>
        /// <param name="systemPressure">In Pascals.</param>
        /// <param name="airLeakRate">In Kilograms per time unit.</param>
        /// <param name="airLeakDuration">In matching time units.</param>
        public void VacuumDistillationWScrubber(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            double controlTemperature,
            double systemPressure, /* in Pascals. */
            double airLeakRate, /* in kilograms per time unit. */
            double airLeakDuration /* in matching time unit. */
            )
        {
            new VacuumDistillationModel().VacuumDistillation(initial, out final, out emission, modifyInPlace, controlTemperature, systemPressure, airLeakRate, airLeakDuration);
        }

    }
}
