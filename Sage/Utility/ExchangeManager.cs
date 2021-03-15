/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;

namespace Highpoint.Sage.Utility
{
    /// <summary>
    /// An ExchangeManager manages a set of ITupleSpace instances (Exchanges) that are used for
    /// coordination and synchronization between otherwise uncoupled elements of a simulation.
    /// </summary>
    public class ExchangeManager : IModelService
    {

        #region Private Memebers
        private Dictionary<Guid, ITupleSpace> _exchanges;
        private IExecutive _exec;
        #endregion

        /// <summary>
        /// Creates a new instance of the <see cref="ExchangeManager"/> class.
        /// </summary>
        public ExchangeManager()
        {
        }

        /// <summary>
        /// Gets the default exchange for this model.
        /// </summary>
        /// <returns>The default exchange for this model.</returns>
        public ITupleSpace GetExchange()
        {
            return GetExchange(Guid.Empty);
        }

        /// <summary>
        /// Gets the exchange associated with the provided identifier. If the identifier is Guid.Empty, then
        /// the default exchange is acquired.
        /// </summary>
        /// <param name="exchangeIdentifier">The exchange identifier.</param>
        /// <returns>The exchange associated with the provided identifier.</returns>
        public ITupleSpace GetExchange(Guid exchangeIdentifier)
        {
            if (_exchanges == null)
            {
                lock (this)
                {
                    if (_exchanges == null)
                    {
                        _exchanges = new Dictionary<Guid, ITupleSpace> { { Guid.Empty, new Exchange(_exec) } };
                    }
                }
            }

            if (!_exchanges.ContainsKey(exchangeIdentifier))
            {
                _exchanges.Add(exchangeIdentifier, new Exchange(_exec));
            }

            return _exchanges[exchangeIdentifier];
        }

        /// <summary>
        /// Initializes the service to run in the provided model. This is called by the model 
        /// immediately after the service is added.
        /// </summary>
        /// <param name="model">The model.</param>
        public void InitializeService(IModel model)
        {
            _exec = model.Executive;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has been initialized yet.
        /// </summary>
        /// <value><c>true</c> if this instance is initialized; otherwise, <c>false</c>.</value>
        public bool IsInitialized
        {
            get
            {
                return _exec != null;
            }
            set
            {
            }
        }
        /// <summary>
        /// Gets a value indicating whether the service is to be automatically initialized inline when
        /// the service is added to the model, or if the user (i.e. the custom model class) will do so later.
        /// </summary>
        /// <value><c>true</c> if initialization is to occur inline, otherwise, <c>false</c>.</value>
        public bool InlineInitialization => true;
    }
}