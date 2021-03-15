/* This source code licensed under the GNU Affero General Public License */

using System.Collections;
#pragma warning disable 1587

namespace Highpoint.Sage.Mathematics.Scaling
{
    /// <summary>
    /// An engine that is capable of performing groupwise rescaling of a set of <see cref="Highpoint.Sage.Mathematics.Scaling.IScalable"/>s.
    /// </summary>
    public class ScalingEngine : IScalingEngine
    {
        #region Private fields.
        private readonly IEnumerable _subjectEnumerator;
        private double _aggregate;
        #endregion

        /// <summary>
        /// Creates a new instance of the <see cref="T:ScalingEngine" /> class.
        /// </summary>
        /// <param name="scalables">The scalables.</param>
        public ScalingEngine(IEnumerable scalables){
            _subjectEnumerator = scalables;
            _aggregate = 1;
        }

        /// <summary>
        /// The scale to be applied to the target object. Cannot scale by a factor of zero.
        /// </summary>
        public void Rescale(double byFactor){
            // TODO: Figure out a scaling rollback strategy if somebody pukes on scaling.
            _aggregate*=byFactor;
            foreach ( IScalable scalable in _subjectEnumerator ) {
                scalable.Rescale(_aggregate);
            }
        }

        /// <summary>
        /// The combined, aggregate scale of all of the subjects of this scaling engine
        /// compared to their original scale.
        /// </summary>
        public double AggregateScale { 
            get{
                return _aggregate;
            }
            set{
                Rescale(1.0/_aggregate); // TODO: Think about this. In non-linear cases this is probably necessary.
                Rescale(value);
            }
        }    
    }
}