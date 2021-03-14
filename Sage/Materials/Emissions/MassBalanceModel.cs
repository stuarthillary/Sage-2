/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections;
using PN = Highpoint.Sage.Materials.Chemistry.Emissions.EmissionModel.ParamNames;

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
    /// <summary>
    /// This model is used whenever an emission of a known mixture occurs
    /// during a particular operation.  The user must specify the mixture
    /// containing emission.  As an example, the butane emission from
    /// the reaction of n-butyllithium could be specified by using this model.
    /// However, the VOC emission caused by the evolution and emission of the
    /// butane would have to be calculated by the Gas Evolve model.
    /// </summary>
    public class MassBalanceModel : EmissionModel
    {

        /// <summary>
        /// One-size-fits-all API for all Emission Models, so that the Emissions Service can run
        /// an emission model that it has never seen before.
        /// <p></p>In order to successfully call the Mass Balance model on this API, the parameters hashtable
        /// must include the following entry (see the MassBalance(...) method for details):<p></p>
        /// &quot;DesiredEmission&quot;.
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

            Mixture desiredEmission = (Mixture)parameters[PN.DesiredEmission];
            PrepareToReadLateBoundParameters();
            if (desiredEmission == null)
            {
                if (parameters.ContainsKey(PN.MaterialTypeGuidToEmit))
                {
                    Guid materialTypeGuid = (Guid)parameters[PN.MaterialTypeGuidToEmit];
                    Guid materialSpecGuid = Guid.Empty;
                    if (parameters.ContainsKey(PN.MaterialSpecGuidToEmit))
                    {
                        materialSpecGuid = (Guid)parameters[PN.MaterialSpecGuidToEmit];
                    }

                    // Emission by fraction.
                    if (parameters.ContainsKey(PN.MaterialFractionToEmit))
                    {
                        double fraction = (double)parameters[PN.MaterialFractionToEmit];
                        desiredEmission = new Mixture();
                        foreach (IMaterial material in initial.Constituents)
                        {
                            if (MaterialMatchesTypeAndSpec((Substance)material, materialTypeGuid, materialSpecGuid))
                            {
                                Substance s = (Substance)material.Clone();
                                s = s.Remove(s.Mass * fraction);
                                desiredEmission.AddMaterial(s);
                            }
                        }
                        // Emission by mass.
                    }
                    else if (parameters.ContainsKey(PN.MaterialMassToEmit))
                    {
                        double massOut = (double)parameters[PN.MaterialMassToEmit];
                        desiredEmission = new Mixture();
                        foreach (IMaterial material in initial.Constituents)
                        {
                            if (MaterialMatchesTypeAndSpec((Substance)material, materialTypeGuid, materialSpecGuid))
                            {
                                Substance s = (Substance)material.Clone();
                                s = s.Remove(massOut);
                                desiredEmission.AddMaterial(s);
                            }
                        }
                    }
                    else
                    {
                        ErrorMessages.Add("Attempt to read missing parameters, \"" + PN.MaterialFractionToEmit + "\" (a double, [0.0->1.0]) or \"" + PN.MaterialMassToEmit + "\"  (a double representing kilograms) from supplied parameters. The parameter \"" + PN.DesiredEmission + "\" was also missing, although this is okay if the others are provided.\r\n");
                    }
                }
                else
                {
                    ErrorMessages.Add("Attempt to read missing parameter \"" + PN.MaterialTypeGuidToEmit + "\" (a double, [0.0->1.0]) or \"MaterialTypeGuidToEmit\" (a Guid representing a material type) from supplied parameters. The parameter \"" + PN.DesiredEmission + "\" was also missing, although this is okay if the others are provided. The parameter \"" + PN.DesiredEmission + "\" was also missing, although this is okay if the others are provided.\r\n");
                }
            }
            EvaluateSuccessOfParameterReads();

            MassBalance(initial, out final, out emission, modifyInPlace, desiredEmission);

            ReportProcessCall(this, initial, final, emission, parameters);
        }

        private bool MaterialMatchesTypeAndSpec(Substance s, Guid typeGuid, Guid specGuid)
        {
            if (!s.MaterialType.Guid.Equals(typeGuid))
                return false;
            if (specGuid.Equals(Guid.Empty) || s.GetMaterialSpec(specGuid) > 0.0)
                return true;
            return false;
        }

        #region >>> Usability Support <<<
        private static readonly string description = "This model is used whenever an emission of a known mixture occurs during a particular operation.  The user must specify the materials emitted from the initial mixture in the form of a mixture.  As an example, the butane emission from the reaction of n-butyllithium could be specified by using this model. However, the VOC emission caused by the evolution and emission of the butane would have to be calculated by the Gas Evolve model.";
        private static readonly EmissionParam[] parameters =
            { new EmissionParam(PN.DesiredEmission,"The mixture that is to be removed from the initial mixture as emission.") };
        private static readonly string[] keys = { "Mass Balance" };
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
        /// This model is used whenever an emission of a known mixture occurs
        /// during a particular operation.  The user must specify the mixture
        /// containing emission.  As an example, the butane emission from
        /// the reaction of n-butyllithium could be specified by using this model.
        /// However, the VOC emission caused by the evolution and emission of the
        /// butane would have to be calculated by the Gas Evolve model.
        /// </summary>
        /// <param name="initial">The mixture as it exists before the emission.</param>
        /// <param name="final">The resultant mixture after the emission.</param>
        /// <param name="emission">The mixture emitted as a result of this model.</param>
        /// <param name="modifyInPlace">If true, then the initial mixture is returned in its final state after emission, otherwise, it is left as-is.</param>
        /// <param name="desiredEmission">The mixture that is to be removed from the initial mixture as emission.</param>
        public void MassBalance(
            Mixture initial,
            out Mixture final,
            out Mixture emission,
            bool modifyInPlace,
            Mixture desiredEmission
            )
        {
            Mixture mixture = modifyInPlace ? initial : (Mixture)initial.Clone();

            emission = (Mixture)desiredEmission.Clone();

            foreach (Substance s in emission.Constituents)
            {

                if (!PermitUnderEmission && emission.ContainedMassOf(s.MaterialType) < 0)
                    emission.RemoveMaterial(s.MaterialType);
                if (!PermitOverEmission)
                {
                    // The emission cannot contain more than the quantity in the mixture.
                    double overMass = Math.Min(0, emission.ContainedMassOf(s.MaterialType) - mixture.ContainedMassOf(s.MaterialType));
                    if (overMass > 0)
                        emission.RemoveMaterial(s.MaterialType, overMass);
                }


                mixture.RemoveMaterial(s.MaterialType, emission.ContainedMassOf(s.MaterialType));
            }
            final = mixture;
        }
    }
}
