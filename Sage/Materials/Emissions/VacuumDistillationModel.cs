/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Materials.Chemistry.VaporPressure;
using System;
using System.Collections;
using K = Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.Constants;
using PN = Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.ParamNames;
using VPC = Highpoint.Sage.Materials.Chemistry.VaporPressure.VaporPressureCalculator;

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
    /// <summary>
    /// This model is used to calculate the emissions associated with vacuum
    /// operations.  The model assumes that air leaks into the system under
    /// vacuum, is exposed to the VOC, becomes saturated with the VOC vapor at
    /// the exit temperature, and leaves the system via the vacuum source.
    /// <p>The most important input parameter to this model is the leak rate of
    /// the air into the system.  If the leak rate for a particular piece of equipment
    /// has been measured, then this leak rate can be used.  On the other hand, if no
    /// leak rate information is available, EmitNJ will estimate the leak rate using
    /// the system volume entered by the user and industry standard leak rates for 
    /// 'commercially tight' systems.</p>
    /// </summary>
    public class VacuumDistillationModel : EmissionModel
    {

        /// <summary>
        /// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
        /// an emission model that it has never seen before.
        /// <p></p>In order to successfully call the Vacuum Distillation model on this API, the parameters hashtable
        /// must include the following entries (see the VacuumDistillation(...) method for details):<p></p>
        /// &quot;AirLeakRate&quot;, &quot;AirLeakDuration&quot;, &quot;SystemPressure&quot; and &quot;ControlTemperature&quot;. If there
        /// is no entry under &quot;VacuumSystemPressure&quot;, then this method looks for entries under &quot;InitialPressure&quot; 
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
            //			double vacuumSystemPressure = (double)parameters[PN.VacuumSystemPressure_P];

            double airLeakRate = double.NaN;
            TryToRead(ref airLeakRate, PN.AirLeakRate_KgPerMin, parameters);
            double airLeakDuration = double.NaN;
            TryToRead(ref airLeakDuration, PN.AirLeakDuration_Min, parameters);
            double controlTemperature = double.NaN;
            TryToRead(ref controlTemperature, PN.ControlTemperature_K, parameters);
            double vacuumSystemPressure = double.NaN;
            TryToRead(ref vacuumSystemPressure, PN.VacuumSystemPressure_P, parameters);

            EvaluateSuccessOfParameterReads();

            VacuumDistillation(initial, out final, out emission, modifyInPlace, controlTemperature, vacuumSystemPressure, airLeakRate, airLeakDuration);

            ReportProcessCall(this, initial, final, emission, parameters);

        }

        #region >>> Usability Support <<<
        private static readonly string description = "This model is used to calculate the emissions associated with vacuum operations.  The model assumes that air leaks into the system under vacuum, is exposed to the VOC, becomes saturated with the VOC vapor at the exit temperature, and leaves the system via the vacuum source. \r\nThe most important input parameter to this model is the leak rate of the air into the system.  If the leak rate for a particular piece of equipment has been measured, then this leak rate can be used.  On the other hand, if no leak rate information is available, EmitNJ will estimate the leak rate using the system volume entered by the user and industry standard leak rates for  'commercially tight' systems.";
        private static readonly EmissionParam[] parameters
            = {
                                     new EmissionParam(PN.AirLeakRate_KgPerMin,"Air leak rate into the system, in kilograms per time unit."),
                                     new EmissionParam(PN.AirLeakDuration_Min,"Air leak rate into the system, in the AirLeakRate's time units."),
                                     new EmissionParam(PN.ControlTemperature_K,"The control or condenser temperature, in degrees Kelvin."),
                                     new EmissionParam(PN.VacuumSystemPressure_P,"The pressure to which the vacuum system drives the vessel, in Pascals.")
                                 };
        private static readonly string[] keys = { "Vacuum Distillation", "Vacuum Distill" };
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
        /// operations.  The model assumes that air leaks into the system under
        /// vacuum, is exposed to the VOC, becomes saturated with the VOC vapor at
        /// the exit temperature, and leaves the system via the vacuum source.
        /// <p>The most important input parameter to this model is the leak rate of
        /// the air into the system.  If the leak rate for a particular piece of equipment
        /// has been measured, then this leak rate can be used.  On the other hand, if no
        /// leak rate information is available, EmitNJ will estimate the leak rate using
        /// the system volume entered by the user and industry standard leak rates for 
        /// 'commercially tight' systems.</p>
        /// </summary>
        /// <param name="initial">The mixture as it exists before the emission.</param>
        /// <param name="final">The resultant mixture after the emission.</param>
        /// <param name="emission">The mixture emitted as a result of this model.</param>
        /// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
        /// <param name="controlTemperature">In degrees kelvin.</param>
        /// <param name="vacuumSystemPressure">In Pascals.</param>
        /// <param name="airLeakRate">In kilograms per time unit.</param>
        /// <param name="airLeakDuration">In matching time units.</param>
        public void VacuumDistillation(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            double controlTemperature,
            double vacuumSystemPressure, /* in Pascals. */
            double airLeakRate, /* in kilograms per time unit. */
            double airLeakDuration /* in matching time unit. */
            )
        {
            Mixture mixture = modifyInPlace ? initial : (Mixture)initial.Clone();
            emission = new Mixture(initial.Name + " Vacuum Distillation emissions");

            double kilogramsOfAir = airLeakDuration * airLeakRate;

            double dsppc = vacuumSystemPressure - VPC.SumOfPartialPressures(mixture, controlTemperature);

            double factor = (kilogramsOfAir / K.MolecularWeightOfAir) * (1.0 / dsppc);

            foreach (Substance substance in mixture.Constituents)
            {
                MaterialType mt = substance.MaterialType;
                double molWt = mt.MolecularWeight;
                double molFrac = mixture.GetMoleFraction(mt, MaterialType.FilterAcceptLiquidOnly);
                double vaporPressure = VaporPressureCalculator.ComputeVaporPressure(mt, controlTemperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
                double massOfSubstance = molWt * molFrac * vaporPressure * factor;

                if (!PermitOverEmission)
                    massOfSubstance = Math.Min(substance.Mass, massOfSubstance);
                if (!PermitUnderEmission)
                    massOfSubstance = Math.Max(0, massOfSubstance);

                Substance emitted = (Substance)mt.CreateMass(massOfSubstance, controlTemperature + Chemistry.Constants.KELVIN_TO_CELSIUS);
                Substance.ApplyMaterialSpecs(emitted, substance);
                emission.AddMaterial(emitted);
            }

            foreach (Substance s in emission.Constituents)
            {
                mixture.RemoveMaterial(s.MaterialType, s.Mass);
            }
            final = mixture;
        }
    }
}
