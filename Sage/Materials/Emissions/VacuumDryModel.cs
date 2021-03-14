/* This source code licensed under the GNU Affero General Public License */
using System.Collections;
using PN = Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.ParamNames;

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
    /// <summary>
    /// This model is used to calculate the emissions associated with
    /// drying solid product in a vacuum dryer.  The calculation of the
    /// emission from the operation is identical to that for the Vacuum
    /// Distill model, except that the total calculated VOC emission cannot
    /// exceed the amount of VOC in the wet cake.
    /// </summary>
    public class VacuumDryModel : EmissionModel
    {

        /// <summary>
        /// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
        /// an emission model that it has never seen before.
        /// <p></p>In order to successfully call the Vacuum Dry model on this API, the parameters hashtable
        /// must include the following entries (see the VacuumDry(...) method for details):<p></p>
        /// &quot;AirLeakRate&quot;, &quot;AirLeakDuration&quot;, &quot;SystemPressure&quot;  &quot;MaterialGuidToVolumeFraction&quot;, &quot;MassOfDriedProductCake&quot; and &quot;ControlTemperature&quot;. If there
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
            //			Hashtable materialGuidToVolumeFraction = (Hashtable)parameters[PN.MaterialGuidToVolumeFraction];
            //			double massofDriedProductCake = (double)parameters[PN.MassOfDriedProductCake_Kg];

            double airLeakRate = double.NaN;
            TryToRead(ref airLeakRate, PN.AirLeakRate_KgPerMin, parameters);
            double airLeakDuration = double.NaN;
            TryToRead(ref airLeakDuration, PN.AirLeakDuration_Min, parameters);
            double controlTemperature = double.NaN;
            TryToRead(ref controlTemperature, PN.ControlTemperature_K, parameters);
            double systemPressure = GetSystemPressure(parameters);

            EvaluateSuccessOfParameterReads(); // Preceding values are required.

            // These two are optional. ////////////////////////////////////////////////////////////
            Hashtable materialGuidToVolumeFraction = null;
            if (parameters.Contains(PN.MaterialGuidToVolumeFraction))
            {
                materialGuidToVolumeFraction = (Hashtable)parameters[PN.MaterialGuidToVolumeFraction];
            }

            double massofDriedProductCake = double.NaN;
            if (parameters.Contains(PN.MassOfDriedProductCake_Kg))
            {
                massofDriedProductCake = (double)parameters[PN.MassOfDriedProductCake_Kg];
            }
            // ////////////////////////////////////////////////////////////////////////////////////

            VacuumDry(initial, out final, out emission, modifyInPlace, controlTemperature, systemPressure, airLeakRate, airLeakDuration, materialGuidToVolumeFraction, massofDriedProductCake);

            ReportProcessCall(this, initial, final, emission, parameters);

        }

        #region >>> Usability Support <<<
        private static readonly string description = "This model is used to calculate the emissions associated with drying solid product in a vacuum dryer.  The calculation of the emission from the operation is identical to that for the Vacuum Distill model, except that the total calculated VOC emission cannot exceed the amount of VOC in the wet cake.";
        private static readonly EmissionParam[] parameters
            = {
                                     new EmissionParam(PN.AirLeakRate_KgPerMin,"Air leak rate into the system, in kilograms per time unit."),
                                     new EmissionParam(PN.AirLeakDuration_Min,"Air leak rate into the system, in the AirLeakRate's time units."),
                                     new EmissionParam(PN.ControlTemperature_K,"The control or condenser temperature, in degrees Kelvin."),
                                     new EmissionParam(PN.SystemPressure_P,"The pressure of the system during the emission operation, in Pascals. This parameter can also be called \"Final Pressure\"."),
                                     new EmissionParam(PN.MaterialGuidToVolumeFraction,"A hashtable with the guids of materialTypes as keys, and the volumeFraction for that material type as values. VolumeFraction is the percent [0.0 to 1.0] of that material type in the offgas."),
                                     new EmissionParam(PN.MassOfDriedProductCake_Kg,"Kilogram mass of the post-drying product cake.")
                                 };
        private static readonly string[] keys = { "Vacuum Dry" };
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
        /// This model is used to calculate the emissions associated with
        /// drying solid product in a vacuum dryer.  The calculation of the
        /// emission from the operation is identical to that for the Vacuum
        /// Distill model, except that the total calculated VOC emission cannot
        /// exceed the amount of VOC in the wet cake.
        /// </summary>
        /// <param name="initial">The mixture as it exists before the emission.</param>
        /// <param name="final">The resultant mixture after the emission.</param>
        /// <param name="emission">The mixture emitted as a result of this model.</param>
        /// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
        /// <param name="controlTemperature">In degrees kelvin.</param>
        /// <param name="systemPressure">In Pascals.</param>
        /// <param name="airLeakRate">In kilograms per time unit.</param>
        /// <param name="airLeakDuration">In matching time units.</param>
        /// <param name="materialGuidToVolumeFraction">Hashtable with Material Type Guids as keys, and a double, [0..1] representing the fraction of that material present lost during the drying of the product cake.</param>
        /// <param name="massOfDriedProductCake">The final mass of the dried product cake.</param>
        public void VacuumDry(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            double controlTemperature,
            double systemPressure, /* in Pascals. */
            double airLeakRate, /* in kilograms per time unit. */
            double airLeakDuration, /* in matching time unit. */
            Hashtable materialGuidToVolumeFraction,
            double massOfDriedProductCake
            )
        {

            Mixture finalVd, emissionVd;
            new VacuumDistillationModel().VacuumDistillation(initial, out finalVd, out emissionVd, false, controlTemperature, systemPressure, airLeakRate, airLeakDuration);

            // If we have data for the AirDry equation, we will calculate it, and use the larger mass of the two (VD vs. AD).
            Mixture emissionAd;
            if (materialGuidToVolumeFraction != null)
            {
                Mixture finalAd;
                new AirDryModel().AirDry(initial, out finalAd, out emissionAd, false, massOfDriedProductCake, controlTemperature, materialGuidToVolumeFraction);
            }
            else
            {
                emissionAd = new Mixture(); // Nothing emitted.
            }

            if (materialGuidToVolumeFraction == null)
            {
                emission = emissionVd; // If we couldn't calculate an air dry emission, we must use the vacuum dry value.
            }
            else
            {
                // Otherwise, we use the lesser of the Airdry and VacuumDry calculations.
                emission = emissionVd.Mass > emissionAd.Mass ? emissionAd : emissionVd;
            }

            Mixture mixture = modifyInPlace ? initial : (Mixture)initial.Clone();
            foreach (Substance s in emission.Constituents)
            {
                double massOfSubstance = s.Mass;
                // No need to worry about overEmission - it was accounted for in the VacDist or AirDry models.
                mixture.RemoveMaterial(s.MaterialType, massOfSubstance);
            }
            final = mixture;

        }
    }
}
