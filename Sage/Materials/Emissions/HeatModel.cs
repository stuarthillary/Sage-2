/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Materials.Chemistry.VaporPressure;
using System;
using System.Collections;
using PN = Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.ParamNames;
using VPC = Highpoint.Sage.Materials.Chemistry.VaporPressure.VaporPressureCalculator;

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
    /// <summary>
    /// This model is used to calculate the emissions associated with the heating
    /// of a vessel or other piece of equipment containing a VOC and a non-condensable
    /// gas (nitrogen or air).  The model assumes that the non-condensable gas,
    /// saturated with the VOC mixture, is emitted from the vessel because of (1) the
    /// expansion of the gas upon heating and (2) an increase in the VOC vapor pressure.
    /// The emitted gas is saturated with the VOC mixture at the exit temperature, the
    /// condenser or receiver temperature.
    /// </summary>
    public class HeatModel : EmissionModel
    {

        /// <summary>
        /// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
        /// an emission model that it has never seen before.
        /// <p></p>In order to successfully call the Heat model on this API, the parameters hashtable
        /// must include the following entries (see the Heat(...) method for details):<p></p>
        /// &quot;ControlTemperature&quot;, &quot;InitialTemperature&quot;, &quot;FinalTemperature&quot;&quot;SystemPressure&quot; and &quot;FreeSpace&quot;. If there
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

            double controlTemperature = double.NaN;
            TryToRead(ref controlTemperature, PN.ControlTemperature_K, parameters);
            double initialTemperature = double.NaN;
            TryToRead(ref initialTemperature, PN.InitialTemperature_K, parameters);
            double finalTemperature = double.NaN;
            TryToRead(ref finalTemperature, PN.FinalTemperature_K, parameters);
            double systemPressure = GetSystemPressure(parameters);
            double vesselVolume = double.NaN;
            TryToRead(ref vesselVolume, PN.VesselVolume_M3, parameters);

            EvaluateSuccessOfParameterReads();

            Heat(initial, out final, out emission, modifyInPlace, controlTemperature, initialTemperature, finalTemperature, systemPressure, vesselVolume);

            ReportProcessCall(this, initial, final, emission, parameters);
        }


        #region >>> Usability Support <<<
        private static readonly string description = "This model is used to calculate the emissions associated with the heating of a vessel or other piece of equipment containing a VOC and a non-condensable gas (nitrogen or air).  The model assumes that the non-condensable gas, saturated with the VOC mixture, is emitted from the vessel because of (1) the expansion of the gas upon heating and (2) an increase in the VOC vapor pressure. The emitted gas is saturated with the VOC mixture at the exit temperature, the condenser or receiver temperature.";
        private static readonly EmissionParam[] parameters =
            {
                new EmissionParam(PN.ControlTemperature_K,"The control or condenser temperature, in degrees Kelvin."),
                new EmissionParam(PN.InitialTemperature_K,"The initial temerature of the mixture in degrees Kelvin."),
                new EmissionParam(PN.FinalTemperature_K,"The final temperature of the mixture in degrees Kelvin."),
                new EmissionParam(PN.SystemPressure_P,"The pressure of the system during the emission operation, in Pascals. This parameter can also be called \"Final Pressure\"."),
                new EmissionParam(PN.VesselVolume_M3,"The volume of the vessel, in cubic meters.")
            };
        private static readonly string[] keys = { "Heat" };
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
        /// This model is used to calculate the emissions associated with the heating
        /// of a vessel or other piece of equipment containing a VOC and a non-condensable
        /// gas (nitrogen or air).  The model assumes that the non-condensable gas,
        /// saturated with the VOC mixture, is emitted from the vessel because of (1) the
        /// expansion of the gas upon heating and (2) an increase in the VOC vapor pressure.
        /// The emitted gas is saturated with the VOC mixture at the exit temperature, the
        /// condenser or receiver temperature.
        /// </summary>
        /// <param name="initial">The mixture as it exists before the emission.</param>
        /// <param name="final">The resultant mixture after the emission.</param>
        /// <param name="emission">The mixture emitted as a result of this model.</param>
        /// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
        /// <param name="controlTemperature">The control or condenser temperature, in degrees Kelvin.</param>
        /// <param name="initialTemperature">The initial temerature of the mixture in degrees Kelvin.</param>
        /// <param name="finalTemperature">The final temperature of the mixture in degrees Kelvin.</param>
        /// <param name="systemPressure">The pressure of the system (vessel) in Pascals.</param>
        /// <param name="vesselVolume">The volume of the vessel, in cubic meters.</param>
        public void Heat(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            double controlTemperature,
            double initialTemperature,
            double finalTemperature, /* in degreesK */
            double systemPressure, /* in Pascals. */
            double vesselVolume /* in cubic meters. */
            )
        {

            double freeSpace = vesselVolume - (initial.Volume * 0.001 /*convert liters to cubic meters*/);

            Mixture mixture = modifyInPlace ? initial : (Mixture)initial.Clone();
            emission = new Mixture(initial.Name + " Heat emissions");

            double dsppi = systemPressure - VPC.SumOfPartialPressures(mixture, initialTemperature);
            double dsppf = systemPressure - VPC.SumOfPartialPressures(mixture, finalTemperature);
            double dsppc = systemPressure - VPC.SumOfPartialPressures(mixture, controlTemperature);

            double factor = (freeSpace / dsppc) * ((dsppi / initialTemperature) - (dsppf / finalTemperature));

            foreach (Substance substance in mixture.Constituents)
            {
                MaterialType mt = substance.MaterialType;
                double molWt = mt.MolecularWeight;
                double molFrac = mixture.GetMoleFraction(mt, MaterialType.FilterAcceptLiquidOnly);
                double vaporPressure = VaporPressureCalculator.ComputeVaporPressure(mt, controlTemperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
                double massOfSubstance = molWt * molFrac * vaporPressure * factor / Chemistry.Constants.MolarGasConstant;
                // At this point, massOfSubstance is in grams (since molWt is in grams per mole)
                massOfSubstance /= 1000;
                // now, massOfSubstance is in kg.

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
