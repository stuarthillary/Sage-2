/* This source code licensed under the GNU Affero General Public License */

using System;

namespace Highpoint.Sage.Materials.Chemistry
{
    /// <summary>
    /// A ReactionInstance is created every time a reaction takes place. It records
    /// what reaction took place, how much of the reaction took place (i.e. percent
    /// completion) in both the forward and reverse directions, and the percent of
    /// completion the reaction had been told to attempt to accomplish, in case the
    /// value were to be changed in the reaction, later (such as as a result of a
    /// change in temperature.)
    /// </summary>
    public class ReactionInstance
    {

        private readonly Reaction _reaction;
        private readonly double _fwdScale;
        private readonly double _revScale;

        public ReactionInstance(Reaction reaction, double fwdScale, double revScale, Guid rxnInstanceGuid)
        {
            _reaction = reaction;
            _fwdScale = fwdScale;
            _revScale = revScale;
            Guid = rxnInstanceGuid;
        }

        //		public ReactionInstance(Reaction reaction, double fwdScale, double revScale)
        //			:this(reaction,fwdScale,revScale,Guid.NewGuid()){}

        public Reaction Reaction => _reaction;

        private Reaction _isReaction;
        public Reaction InstanceSpecificReaction
        {
            get
            {
                if (_isReaction == null)
                {
                    double scale = (_revScale - _fwdScale);
                    _isReaction = new Reaction(_reaction.Model, "Instance Specific Reaction", Guid.NewGuid());
                    foreach (Reaction.ReactionParticipant rp in _reaction.Reactants)
                    {
                        _isReaction.AddReactant(rp.MaterialType, rp.Mass * (scale));
                    }
                    foreach (Reaction.ReactionParticipant rp in _reaction.Products)
                    {
                        _isReaction.AddProduct(rp.MaterialType, rp.Mass * (-scale));
                    }
                }
                return _isReaction;
            }
        }

        public double ForwardScale => _fwdScale;

        public double ReverseScale => _revScale;

        public double PercentCompletion => _reaction.PercentCompletion;

        public override string ToString()
        {
            return "Reaction " + _reaction.Name + " occurred with a forward scale of " + _fwdScale + " and a reverse scale of " + _revScale + ". Reaction ran to " + (_reaction.PercentCompletion * 100d) + "% completion.";
        }

        public string InstanceSpecificReactionString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < _reaction.Reactants.Count; i++)
            {
                sb.Append(((Reaction.ReactionParticipant)_reaction.Reactants[i]).ToString(_fwdScale - _revScale));
                if (i < _reaction.Reactants.Count - 1)
                    sb.Append(" + ");
            }
            sb.Append(" <==> ");
            for (int i = 0; i < _reaction.Products.Count; i++)
            {
                sb.Append(((Reaction.ReactionParticipant)_reaction.Products[i]).ToString(_fwdScale - _revScale));
                if (i < _reaction.Products.Count - 1)
                    sb.Append(" + ");
            }
            return sb.ToString();
        }

        public Guid Guid
        {
            get;
        }
    }
}
