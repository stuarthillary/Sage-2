/* This source code licensed under the GNU Affero General Public License */
using Highpoint.Sage.Scheduling;
using Highpoint.Sage.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Highpoint.Sage.Graphs.PFC.Execution
{

    /// <summary>
    /// Class PfcExecutionContext holds all of the information necessary to track one execution through a PFC. The PFC governs structure,
    /// the PfcExecutionContext governs process-instance-specific data.
    /// </summary>
    /// <seealso cref="Highpoint.Sage.Utility.ExecutionContext" />
    /// <seealso cref="Highpoint.Sage.Scheduling.ISupportsCorrelation" />
    public class PfcExecutionContext : ExecutionContext, ISupportsCorrelation
    {

        #region Private Fields
        private int _instanceCount = 0;
        private readonly IProcedureFunctionChart _pfc;
        private readonly IPfcStepNode _step;
        private readonly ITimePeriod _timePeriod;
        private static readonly Guid _time_Period_Mask = new Guid("8aeaf15a-f138-4739-b815-6db516107103");
        private static readonly bool _diagnostics = Diagnostics.DiagnosticAids.Diagnostics("PfcExecutionContext");
        #endregion Private Fields

        #region Constructors
        public PfcExecutionContext(IProcedureFunctionChart pfc, string name, string description, Guid guid, PfcExecutionContext parent)
            : base(pfc.Model, name, description, guid, parent)
        {

            if (_diagnostics)
            {
                string parentName = (parent == null ? "<null>" : parent.Name);
                Console.WriteLine("Creating PFCEC \"" + name + "\" under PFCEC \"" + parentName + "\" For parent " + pfc.Name + " and numbered " + guid);
            }

            _pfc = pfc;
            _step = null;
            _timePeriod = new TimePeriodEnvelope(name, GuidOps.XOR(guid, _time_Period_Mask));
            _timePeriod.Subject = this;
            if (parent != null)
            {
                ((TimePeriodEnvelope)parent.TimePeriod).AddTimePeriod(_timePeriod);
            }
            _timePeriod.ChangeEvent += new ObservableChangeHandler(timePeriod_ChangeEvent);
        }

        public PfcExecutionContext(IPfcStepNode stepNode, string name, string description, Guid guid, PfcExecutionContext parent)
            : base(stepNode.Parent.Model, name, description, guid, parent)
        {

            if (_diagnostics)
            {
                Console.WriteLine("Creating PfcEC \"" + name + "\" under PfcEC \"" + parent.Name + "\" For parent " + stepNode.Name + " and numbered " + guid);
            }

            _pfc = stepNode.Parent;
            _step = stepNode;
            if (stepNode.Actions.Count == 0)
            {
                _timePeriod = new TimePeriod(name, GuidOps.XOR(guid, _time_Period_Mask), TimeAdjustmentMode.InferDuration);
                _timePeriod.Subject = this;
                ((TimePeriodEnvelope)parent.TimePeriod).AddTimePeriod(_timePeriod);
            }
            else
            {
                _timePeriod = new TimePeriodEnvelope(name, GuidOps.XOR(guid, _time_Period_Mask));
                _timePeriod.Subject = this;
                ((TimePeriodEnvelope)parent.TimePeriod).AddTimePeriod(_timePeriod);
            }
            _timePeriod.ChangeEvent += new ObservableChangeHandler(timePeriod_ChangeEvent);
        }
        #endregion Constructors

        private void timePeriod_ChangeEvent(object whoChanged, object whatChanged, object howChanged)
        {
            if (TimePeriodChange != null)
                TimePeriodChange((ITimePeriod)whoChanged, (TimePeriod.ChangeType)whatChanged);
        }

        public event TimePeriodChange TimePeriodChange;

        public IProcedureFunctionChart PFC
        {
            [DebuggerStepThrough]
            get
            {
                return _pfc;
            }
        }
        public IPfcStepNode Step
        {
            [DebuggerStepThrough]
            get
            {
                return _step;
            }
        }
        public bool IsStepCentric
        {
            [DebuggerStepThrough]
            get
            {
                return _step != null;
            }
        }
        public ITimePeriod TimePeriod
        {
            [DebuggerStepThrough]
            get
            {
                return _timePeriod;
            }
        }

        public IEnumerable<IPfcStepNode> ChildSteps
        {
            get
            {
                foreach (object obj in Values)
                {
                    StepStateMachine ssm = obj as StepStateMachine;
                    if (ssm != null)
                    {
                        yield return ssm.MyStep;
                    }
                }
            }
        }

        public IEnumerable<StepStateMachine> ChildStepStateMachines
        {
            get
            {
                foreach (object obj in Keys)
                {
                    StepStateMachine ssm = obj as StepStateMachine;
                    if (ssm != null)
                    {
                        yield return ssm;
                    }
                }
            }
        }

        #region To Various String Representations
        public string ToXmlString(bool deep)
        {
            StringBuilder sb = new StringBuilder();
            _toXmlString(sb, deep);
            return sb.ToString();
        }

        public override string ToString()
        {
            return (IsStepCentric ? "Step" : "PFC") + " Exec Ctx : " + Name;
        }

        private void _toXmlString(StringBuilder sb, bool deep)
        {
            sb.Append(@"<PfcExecutionContext pfcName=""" + _pfc.Name + @""">");
            foreach (DictionaryEntry de in this)
            {
                sb.Append("<Entry><Key>" + de.Key + "</Key><Value>" + de.Value + "</Value></Entry>");
            }
            if (deep)
            {
                sb.Append("<Children>");
                foreach (PfcExecutionContext child in Children)
                {
                    child._toXmlString(sb, deep);
                }
                sb.Append("</Children>");
            }
            sb.Append("</PfcExecutionContext>");
        }
        #endregion To Various String Representations

        #region ISupportsCorrelation Members

        /// <summary>
        /// Gets the parent of this tree node for the purpose of correlation. This will be its nominal parent, too.
        /// </summary>
        /// <value>The parent.</value>
        public Guid ParentGuid
        {
            get
            {
                return Parent.Payload.Guid;
            }
        }

        public int InstanceCount
        {
            get
            {
                return _instanceCount;
            }
            set
            {
                _instanceCount = value;
            }
        }
        #endregion

    }
}
