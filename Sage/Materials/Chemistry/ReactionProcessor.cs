/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Persistence;
using Highpoint.Sage.SimCore;
using System;
using System.Collections;
using System.Linq;
using _Debug = System.Diagnostics.Debug;


namespace Highpoint.Sage.Materials.Chemistry
{


    /// <summary>
    /// Delegate implemented by a method that wants to be called when a reaction is added to
    /// or removed from, a ReactionProcessor.
    /// </summary>
    public delegate void ReactionProcessorEvent(ReactionProcessor rxnProcessor, Reaction reaction);

    /// <summary>
    /// A reaction processor knows of a set of chemical reactions, and watches a set of mixtures.
    /// Whenever a material is added to, or removed from, a mixture, the reaction processor examines
    /// that mixture to see if any of the reactions it knows of are capable of occurring. If any are,
    /// then it proceeds to execute that reaction, eliminating the appropriate quantity of reactants,
    /// generating the appropriate quantity of products (or vice versa) and changing the mixture's
    /// thermal characteristics.
    /// </summary>
    public class ReactionProcessor : IHasIdentity, IXmlPersistable
    {

        public event ReactionProcessorEvent ReactionAddedEvent;
        public event ReactionProcessorEvent ReactionRemovedEvent;

        private readonly ArrayList _reactions = new ArrayList();
        private readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("ReactionProcessor");

        public ReactionProcessor()
        {
        }

        public void AddReaction(Reaction reaction)
        {
            if (!reaction.IsValid)
                throw new ReactionDefinitionException(reaction);
            if (!_reactions.Contains(reaction))
            {
                _reactions.Add(reaction);
                ReactionAddedEvent?.Invoke(this, reaction);
            }
        }

        public void RemoveReaction(Reaction reaction)
        {
            if (_reactions.Contains(reaction))
            {
                _reactions.Remove(reaction);
                ReactionRemovedEvent?.Invoke(this, reaction);
            }
        }

        public ArrayList Reactions => ArrayList.ReadOnly(_reactions);

        public Reaction GetReaction(Guid rxnGuid)
        {
            return _reactions.Cast<Reaction>().FirstOrDefault(rxn => rxn.Guid.Equals(rxnGuid));
        }

        public bool CombineMaterials(IMaterial[] materialsToCombine)
        {
            IMaterial result;
            ArrayList observedReactions;
            ArrayList observedReactionInstances;
            return CombineMaterials(materialsToCombine, out result, out observedReactions, out observedReactionInstances);
        }

        public bool CombineMaterials(IMaterial[] materialsToCombine, out IMaterial result)
        {
            ArrayList observedReactions;
            ArrayList observedReactionInstances;
            return CombineMaterials(materialsToCombine, out result, out observedReactions, out observedReactionInstances);
        }

        public bool CombineMaterials(IMaterial[] materialsToCombine, out ArrayList observedReactions)
        {
            IMaterial result;
            ArrayList observedReactionInstances;
            return CombineMaterials(materialsToCombine, out result, out observedReactions, out observedReactionInstances);
        }

        public bool CombineMaterials(IMaterial[] materialsToCombine, out IMaterial result, out ArrayList observedReactions, out ArrayList observedReactionInstances)
        {
            Mixture scratch = new Mixture(null, "scratch mixture");
            Watch(scratch);
            ReactionCollector rc = new ReactionCollector(scratch);
            foreach (IMaterial material in materialsToCombine)
            {
                scratch.AddMaterial(material);
            }
            observedReactions = rc.Reactions;
            observedReactionInstances = rc.ReactionInstances;
            rc.Disconnect();
            result = scratch;
            return (observedReactions != null && observedReactions.Count > 0);
        }

        public void Watch(IMaterial material)
        {
            material.MaterialChanged += OnMaterialChanged;
        }

        public void Ignore(IMaterial material)
        {
            material.MaterialChanged -= OnMaterialChanged;
        }

        public IList GetReactionsByParticipant(MaterialType targetMt)
        {
            return GetReactionsByFilter(targetMt, Reaction.MaterialRole.Either);
        }
        public IList GetReactionsByReactant(MaterialType targetMt)
        {
            return GetReactionsByFilter(targetMt, Reaction.MaterialRole.Reactant);
        }
        public IList GetReactionsByProduct(MaterialType targetMt)
        {
            return GetReactionsByFilter(targetMt, Reaction.MaterialRole.Product);
        }

        private IList GetReactionsByFilter(MaterialType targetMt, Reaction.MaterialRole filter)
        {
            ArrayList reactions = new ArrayList();
            foreach (Reaction reaction in Reactions)
            {
                if (filter == Reaction.MaterialRole.Either || filter == Reaction.MaterialRole.Reactant)
                {
                    foreach (Reaction.ReactionParticipant rp in reaction.Reactants)
                    {
                        if (rp.MaterialType.Equals(targetMt))
                            reactions.Add(reaction);
                    }
                }
                if (filter == Reaction.MaterialRole.Either || filter == Reaction.MaterialRole.Product)
                {
                    foreach (Reaction.ReactionParticipant rp in reaction.Products)
                    {
                        if (rp.MaterialType.Equals(targetMt))
                            reactions.Add(reaction);
                    }
                }
            }
            return reactions;
        }


        public void OnMaterialChanged(IMaterial material, MaterialChangeType mct)
        {
            if (_diagnostics)
                _Debug.WriteLine("ReactionProcessor notified of change type " + mct + " to material " + material);
            if (mct == MaterialChangeType.Contents)
            {
                Mixture tmpMixture = material as Mixture;
                if (tmpMixture != null)
                {
                    Mixture mixture = tmpMixture;
                    ReactionInstance ri = null;
                    if (_diagnostics)
                        _Debug.WriteLine("Processing change type " + mct + " to mixture " + mixture.Name);

                    // If multiple reactions could occur? Only the first happens, but then the next change allows the next reaction, etc.
                    foreach (Reaction reaction in _reactions)
                    {
                        if (ri != null)
                            continue;
                        if (_diagnostics)
                            _Debug.WriteLine("Examining mixture for presence of reaction " + reaction.Name);
                        ri = reaction.React(mixture);
                    }
                }
            }
        }

        public object Tag
        {
            get; set;
        }

        #region >>> Implementation of IHasIdentity <<<
        private readonly string _name = "Reaction Processor";
        /// <summary>
        /// The name of this reaction processor.
        /// </summary>
        public string Name => _name;

        private readonly string _description = null;
        /// <summary>
        /// A description of this Reaction Processor.
        /// </summary>
        public string Description => _description ?? _name;

        /// <summary>
        /// The Guid by which this reaction processor will be known.
        /// </summary>
        public Guid Guid { get; } = Guid.Empty;

        #endregion


        private class ReactionCollector
        {
            private readonly ArrayList _reactions;
            private readonly ArrayList _reactionInstances;
            private readonly Mixture _mixture;
            private readonly ReactionHappenedEvent _reactionHandler;
            public ReactionCollector(Mixture mixture)
            {
                _mixture = mixture;
                _reactions = new ArrayList();
                _reactionInstances = new ArrayList();
                _reactionHandler = OnReactionHappened;
                _mixture.OnReactionHappened += _reactionHandler;
            }
            public void Disconnect()
            {
                _mixture.OnReactionHappened -= _reactionHandler;
            }

            private void OnReactionHappened(ReactionInstance ri)
            {
                _reactions.Add(ri.Reaction);
                _reactionInstances.Add(ri);
            }
            public ArrayList Reactions => _reactions;
            public ArrayList ReactionInstances => _reactionInstances;
        }

        #region IXmlPersistable Members

        /// <summary>
        /// Serializes this object to the specified XmlSerializatonContext.
        /// </summary>
        /// <param name="xmlsc">The XmlSerializatonContext into which this object is to be stored.</param>
        public void SerializeTo(XmlSerializationContext xmlsc)
        {
            xmlsc.StoreObject("Reactions", _reactions);
        }

        /// <summary>
        /// Deserializes this object from the specified XmlSerializatonContext.
        /// </summary>
        /// <param name="xmlsc">The XmlSerializatonContext from which this object is to be reconstituted.</param>
        public void DeserializeFrom(XmlSerializationContext xmlsc)
        {

        }

        #endregion
    }
}
