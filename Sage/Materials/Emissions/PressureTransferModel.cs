/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Materials.Chemistry.VaporPressure;
using System;
using System.Collections;
using PN = Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.ParamNames;

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
    /// <summary>
    /// This model is used when any material (solid or liquid) is added to a vessel
    /// already containing a liquid or vapor VOC, and the vapor from that vessel is
    /// thereby emitted by displacement.  The model assumes that the volume of vapor
    /// displaced from the vessel is equal to the amount of material added to the
    /// vessel.  In addition, the vapor displaced from the vessel is saturated with
    /// the VOC vapor at the exit temperature.
    /// </summary>
    public class PressureTransferModel : EmissionModel
    {

        /// <summary>
        /// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
        /// an emission model that it has never seen before.
        /// <p></p>In order to successfully call the Pressure Transfer model on this API, the parameters hashtable
        /// must include the following entries (see the PressureTransfer(...) method for details):<p></p>
        /// &quot;MaterialToAdd&quot; and &quot;ControlTemperature&quot;. 
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

            Mixture materialToAdd = null;
            TryToRead(ref materialToAdd, PN.MaterialToAdd, parameters);
            double controlTemperature = double.NaN;
            TryToRead(ref controlTemperature, PN.ControlTemperature_K, parameters);

            EvaluateSuccessOfParameterReads();

            PressureTransfer(initial, out final, out emission, modifyInPlace, materialToAdd, controlTemperature);

            ReportProcessCall(this, initial, final, emission, parameters);

        }

        #region >>> Usability Support <<<
        private static readonly string description = "This model is used when any material (solid or liquid) is added to a vessel already containing a liquid or vapor VOC, and the vapor from that vessel is thereby emitted by displacement.  The model assumes that the volume of vapor displaced from the vessel is equal to the amount of material added to the vessel.  In addition, the vapor displaced from the vessel is saturated with the VOC vapor at the exit temperature.";
        private static readonly EmissionParam[] parameters
            = {
                                     new EmissionParam(PN.MaterialToAdd,"The material to be added in the fill operation.The volume property of the material will be used to determine volume."),
                                     new EmissionParam(PN.ControlTemperature_K,"The control, or condenser, temperature in degrees Kelvin.")
                                 };
        private static readonly string[] keys = { "Pressure Transfer" };
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
        /// This model is used when any material (solid or liquid) is added to a vessel
        /// already containing a liquid or vapor VOC, and the vapor from that vessel is
        /// thereby emitted by displacement.  The model assumes that the volume of vapor
        /// displaced from the vessel is equal to the amount of material added to the
        /// vessel.  In addition, the vapor displaced from the vessel is saturated with
        /// the VOC vapor at the exit temperature.
        /// </summary>
        /// <param name="initial">The mixture as it exists before the emission.</param>
        /// <param name="final">The resultant mixture after the emission.</param>
        /// <param name="emission">The mixture emitted as a result of this model.</param>
        /// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
        /// <param name="materialToAdd">The material to be added in the fill operation.The volume property of the material will be used to determine volume.</param>
        /// <param name="controlTemperature">The control, or condenser, temperature in degrees Kelvin.</param>
        public void PressureTransfer(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            Mixture materialToAdd,
            double controlTemperature
            )
        {
            Mixture mixture = modifyInPlace ? initial : (Mixture)initial.Clone();
            emission = new Mixture(initial.Name + " Fill emissions");

            double volumeOfMaterialAdded = materialToAdd.Volume /*, which is in liters*/ * .001 /*, to convert it to m^3.*/;

            ArrayList substances = new ArrayList(mixture.Constituents);
            foreach (Substance substance in substances)
            {
                MaterialType mt = substance.MaterialType;
                double molWt = mt.MolecularWeight;
                double molFrac = mixture.GetMoleFraction(mt, MaterialType.FilterAcceptLiquidOnly);
                double vaporPressure = VaporPressureCalculator.ComputeVaporPressure(mt, controlTemperature, TemperatureUnits.Kelvin, PressureUnits.Pascals);
                double massOfSubstance = volumeOfMaterialAdded * molWt * molFrac * vaporPressure / (Chemistry.Constants.MolarGasConstant * controlTemperature); // grams, since molWt = grams per mole.
                massOfSubstance *= .001; // kilograms per gram.

                if (!PermitOverEmission)
                    massOfSubstance = Math.Min(massOfSubstance, substance.Mass);
                if (!PermitUnderEmission)
                    massOfSubstance = Math.Max(0, massOfSubstance);


                Substance emitted = (Substance)mt.CreateMass(massOfSubstance, substance.Temperature);
                Substance.ApplyMaterialSpecs(emitted, substance);
                emission.AddMaterial(emitted);
            }

            foreach (Substance s in materialToAdd.Constituents)
            {
                mixture.AddMaterial(s);
            }

            foreach (Substance s in emission.Constituents)
            {
                mixture.RemoveMaterial(s.MaterialType, s.Mass);
            }

            final = mixture;
        }
    }
}
