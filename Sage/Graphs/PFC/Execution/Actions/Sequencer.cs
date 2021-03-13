/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.SimCore;
using Highpoint.Sage.Utility;
using System;

namespace Highpoint.Sage.Graphs.PFC.Execution.Actions
{

    /// <summary>
    /// When a sequencer's Precondition is set as the precondition of a pfcStep, it will watch the PFCExecutionContext
    /// at it's level minus the rootHeight, and will not grant the step permission to start running until another
    /// the Sequencer with the same sequencer key, and one-less index has already granted its step permission to run.
    /// After the predecessor's permission is granted, the sequencer puts a key tailored for its successor into an
    /// exchange on which the successor is already waiting. Only when the successor receives that key will it grant
    /// itself permission to run.
    /// </summary>
    public class Sequencer
    {

        private Guid _sequenceKey;
        private Guid _myKey;
        private readonly int _myIndex;
        private readonly int _rootHeight;
        private IDetachableEventController m_idec = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Sequencer"/> class.
        /// </summary>
        /// <param name="sequencerKey">The sequencer key.</param>
        /// <param name="myIndex">The creator's index.</param>
        /// <param name="rootHeight">Height of the root.</param>
        public Sequencer(Guid sequencerKey, int myIndex, int rootHeight)
        {
            _sequenceKey = sequencerKey;
            _myIndex = myIndex;
            _myKey = GuidOps.Add(sequencerKey, myIndex);
            _rootHeight = rootHeight;
        }

        public Sequencer(Guid sequencerKey, int myIndex)
            : this(sequencerKey, myIndex, 0) { }

        public PfcAction Precondition
        {
            get
            {
                return new PfcAction(GetPermissionToStart);
            }
        }

        protected void GetPermissionToStart(PfcExecutionContext myPfcec, StepStateMachine ssm)
        {

            PfcExecutionContext root = myPfcec;
            int ascents = _rootHeight;
            while (ascents > 0)
            {
                root = (PfcExecutionContext)myPfcec.Parent.Payload;
                ascents--;
            }

            Exchange exchange = null;
            if (_myIndex == 0)
            {
                //Console.WriteLine(myPfcec.Name + " is creating an exchange and injecting it into pfcec " + root.Name + " under key " + m_sequenceKey);
                exchange = new Exchange(myPfcec.Model.Executive);
                root.Add(_sequenceKey, exchange);
            }
            else
            {
                //Console.WriteLine(myPfcec.Name + " is looking for an exchange in pfcec " + root.Name + " under key " + m_sequenceKey);
                DictionaryChange dc = new DictionaryChange(myPfcec_EntryAdded);
                while (true)
                {
                    exchange = (Exchange)root[_sequenceKey];
                    if (exchange == null)
                    {
                        root.EntryAdded += dc;
                        m_idec = myPfcec.Model.Executive.CurrentEventController;
                        m_idec.Suspend();
                    }
                    else
                    {
                        root.EntryAdded -= dc;
                        break;
                    }
                }
                exchange.Take(_myKey, true); // Only indices 1,2, ... take (and wait?). Index 0 only posts.
                //Console.WriteLine(myPfcec.Name + " got the key I was looking for!");
            }
            Guid nextGuysKey = GuidOps.Increment(_myKey);
            exchange.Post(nextGuysKey, nextGuysKey, false);
            //Console.WriteLine(myPfcec.Name + " posted the key the next guy is looking for!");
        }

        void myPfcec_EntryAdded(object key, object value)
        {
            if (key.Equals(_sequenceKey))
            {
                m_idec.Resume();
            }
        }
    }
}
