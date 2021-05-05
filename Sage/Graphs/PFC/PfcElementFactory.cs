/* COPYRIGHT_NOTICE */
using Highpoint.Sage.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// This is the default implementation of IPfcElementFactory, provided to the
    /// ProcedureFunctionChart.ElementFactory as a default. It creates a Step,
    /// Transition or Link element on demand from this library's default types.
    /// </summary>
    public class PfcElementFactory : IPfcElementFactory
    {

        public delegate bool CanonicalityTest(string elementName);

        #region Private Members

        private readonly CanonicalityTest _linkCanonicality = new CanonicalityTest(delegate (string s) { int x; return (s.StartsWith("L_", StringComparison.Ordinal) && int.TryParse(s.Substring(2), out x)); });
        private static string _linkTemplate = "{0:D3}";
        private static string _linkPrefix = "L_";

        private readonly CanonicalityTest _stepCanonicality = new CanonicalityTest(delegate (string s) { int x; return (s.StartsWith("S_", StringComparison.Ordinal) && int.TryParse(s.Substring(2), out x)); });
        private static string _stepTemplate = "{0:D3}";
        private static string _stepPrefix = "S_";

        private readonly CanonicalityTest _transCanonicality = new CanonicalityTest(delegate (string s) { int x; return ((s.StartsWith("T_", StringComparison.Ordinal) && int.TryParse(s.Substring(2), out x)) || (s.StartsWith("T", StringComparison.Ordinal) && int.TryParse(s.Substring(1), out x))); });
        private static string _transitionTemplate = "{0:D3}";
        private static readonly string _transitionPrefix = "T_";

        private int _nextLinkNumber = 0;
        private int _nextStepNumber = 0;
        private int _nextTransitionNumber = 0;
        private IProcedureFunctionChart _hostPfc = null;
        private Guid _seedGuid = Guid.Empty;
        private Guid _maskGuid = Guid.Empty;
        private bool _repeatable = false;
        private GuidGenerator _guidGenerator = null;
        #endregion Private Members

        #region Private Helper Methods

        private string NextLinkName()
        {
            if (_nextLinkNumber == 1000)
            {
                _linkTemplate = "{0:D4}";
            }

            if (_nextLinkNumber == 10000)
            {
                _linkTemplate = "{0:D5}";
            }

            if (_nextLinkNumber == 100000)
            {
                _linkTemplate = "{0:D6}";
            }

            return _linkPrefix + string.Format(_linkTemplate, _nextLinkNumber++);
        }

        private string NextStepName()
        {
            if (_nextStepNumber == 1000)
            {
                _stepTemplate = "{0:D4}";
            }

            if (_nextStepNumber == 10000)
            {
                _stepTemplate = "{0:D5}";
            }

            if (_nextStepNumber == 100000)
            {
                _stepTemplate = "{0:D6}";
            }

            return _stepPrefix + string.Format(_stepTemplate, _nextStepNumber++);
        }

        private string NextTransitionName()
        {
            if (_nextTransitionNumber == 1000)
            {
                _transitionTemplate = "{0:D4}";
            }

            if (_nextTransitionNumber == 10000)
            {
                _transitionTemplate = "{0:D5}";
            }

            if (_nextTransitionNumber == 100000)
            {
                _transitionTemplate = "{0:D6}";
            }

            return _transitionPrefix + string.Format(_transitionTemplate, _nextTransitionNumber++);
        }

        private Guid NextGuid()
        {
            if (_repeatable)
            {
                _seedGuid = GuidOps.Increment(_seedGuid);
                _seedGuid = GuidOps.Rotate(_seedGuid, -1); // right-shift with wrap.
                _seedGuid = GuidOps.XOR(_seedGuid, _maskGuid);
                return _seedGuid;
            }
            else
            {
                return Guid.NewGuid();
            }
        }

        #endregion Private Helper Methods

        /// <summary>
        /// Creates a new instance of the <see cref="T:PfcElementFactory"/> class.
        /// </summary>
        public PfcElementFactory()
        {
            _guidGenerator = new GuidGenerator(Guid.Empty, Guid.Empty, 0);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="T:PfcElementFactory"/> class.
        /// </summary>
        public PfcElementFactory(GuidGenerator guidGenerator)
        {
            _guidGenerator = guidGenerator;
        }

        ///// <summary>
        ///// Creates a new instance of the <see cref="T:PfcElementFactory"/> class with a specified
        ///// Guid generator.
        ///// </summary>
        //public PfcElementFactory(Guid seed, Guid mask, int rotate) {
        //    m_guidGenerator = new GuidGenerator(seed, mask, rotate);
        //}

        /// <summary>
        /// Creates a new instance of the <see cref="T:PfcElementFactory"/> class.
        /// </summary>
        public PfcElementFactory(IProcedureFunctionChart hostPfc)
            : this()
        {
            HostPfc = hostPfc;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="T:PfcElementFactory"/> class.
        /// </summary>
        public PfcElementFactory(IProcedureFunctionChart hostPfc, Guid seed, Guid mask, int rotate)
            : this(new GuidGenerator(seed, mask, rotate))
        {
            HostPfc = hostPfc;
        }

        /// <summary>
        /// If this is called, then a repeatable Pfc will be created - Guid generation starts with {00000000-0000-0000-0000-000000000001}
        /// and increments each time. This is only useful for testing - otherwise, it is dangerous.
        /// </summary>
        /// <param name="maskGuid">If a non-Guid.Empty mask guid is used, the seed guid is incremented, the Guid is rotated right one
        /// bit, and then the resulting Guid is XOR'ed against this mask guid before being returned.</param>
        public void SetRepeatable(Guid maskGuid)
        {
            _repeatable = true;
            _maskGuid = maskGuid;
            _guidGenerator = new GuidGenerator(Guid.Empty, maskGuid, 1);
            _guidGenerator.Passthrough = true;
        }

        #region IPfcElementFactory Members

        /// <summary>
        /// Gets the Procedure Function Chart for which this factory is creating elements.
        /// </summary>
        /// <value>The host PFC.</value>
        public IProcedureFunctionChart HostPfc
        {
            get
            {
                return _hostPfc;
            }
            set
            {
                _hostPfc = value;
            }
        }

        /// <summary>
        /// Gets the GUID generator in use by this element factory.
        /// </summary>
        /// <value>The GUID generator.</value>
        public GuidGenerator GuidGenerator
        {
            get
            {
                return _guidGenerator;
            }
        }

        /// <summary>
        /// Creates a step node with the provided characteristics. Calls NewStepNode(...) to instantiate the new node.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="guid">The GUID.</param>
        /// <param name="description">The description.</param>
        /// <returns>The new IPfcStepNode.</returns>
        public virtual IPfcStepNode CreateStepNode(string name, Guid guid, string description)
        {
            if (name == null || name.Length == 0)
            {
                name = NextStepName();
            }
            if (description == null)
            {
                description = "";
            }
            if (guid.Equals(Guid.Empty))
            {
                guid = NextGuid();
            }

            IPfcStepNode node = NewStepNode(_hostPfc, name, guid, description);

            return node;
        }

        public virtual IPfcStepNode NewStepNode(IProcedureFunctionChart parent, string name, Guid guid, string description)
        {
            Debug.Assert(parent.Equals(_hostPfc));
            return new PfcStep(parent, name, description, guid);
        }

        /// <summary>
        /// Creates a transition node with the provided characteristics.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="guid">The GUID.</param>
        /// <param name="description">The description.</param>
        /// <returns>The new IPfcTransitionNode.</returns>
        public virtual IPfcTransitionNode CreateTransitionNode(string name, Guid guid, string description)
        {
            if (name == null || name.Length == 0)
            {
                name = NextTransitionName();
            }
            if (description == null)
            {
                description = "";
            }
            if (guid.Equals(Guid.Empty))
            {
                guid = NextGuid();
            }

            IPfcTransitionNode node = NewTransitionNode(_hostPfc, name, guid, description);

            return node;
        }

        public virtual IPfcTransitionNode NewTransitionNode(IProcedureFunctionChart parent, string name, Guid guid, string description)
        {
            Debug.Assert(parent.Equals(_hostPfc));
            return new PfcTransition(parent, name, description, guid);
        }

        /// <summary>
        /// Creates a link element with the provided characteristics.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="guid">The GUID.</param>
        /// <param name="description">The description.</param>
        /// <returns>The new IPfcLinkElement.</returns>
        public virtual IPfcLinkElement CreateLinkElement(string name, Guid guid, string description)
        {
            if (name == null || name.Length == 0)
            {
                name = NextLinkName();
            }
            if (description == null)
            {
                description = "";
            }
            if (guid.Equals(Guid.Empty))
            {
                guid = NextGuid();
            }
            return NewLinkElement(_hostPfc, name, guid, description);
        }

        public virtual IPfcLinkElement NewLinkElement(IProcedureFunctionChart parent, string name, Guid guid, string description)
        {
            Debug.Assert(parent.Equals(_hostPfc));
            return new PfcLink(parent, name, description, guid);
        }

        public virtual void Initialize(IPfcStepNode stepNode)
        {
        }

        public virtual void Initialize(IPfcTransitionNode transitionNode)
        {
        }

        public virtual void Initialize(IPfcLinkElement linkElement)
        {
        }

        /// <summary>
        /// Called when the loading of a new PFC has been completed.
        /// </summary>
        /// <param name="newPfc">The new PFC.</param>
        public void OnPfcLoadCompleted(IProcedureFunctionChart newPfc)
        {
            foreach (IPfcLinkElement link in newPfc.Links)
            {
                if (link.Name.StartsWith(_linkPrefix, StringComparison.Ordinal))
                {
                    int linkNum;
                    if (int.TryParse(link.Name.Substring(_linkPrefix.Length), out linkNum))
                    {
                        if (linkNum > _nextLinkNumber)
                        {
                            _nextLinkNumber = linkNum + 1;
                        }
                    }
                }
            }

            foreach (IPfcStepNode step in newPfc.Steps)
            {
                if (step.Name.StartsWith(_stepPrefix, StringComparison.Ordinal))
                {
                    int stepNum;
                    if (int.TryParse(step.Name.Substring(_stepPrefix.Length), out stepNum))
                    {
                        if (stepNum > _nextStepNumber)
                        {
                            _nextStepNumber = stepNum + 1;
                        }
                    }
                }
            }

            foreach (IPfcTransitionNode transition in newPfc.Transitions)
            {
                if (transition.Name.StartsWith(_transitionPrefix, StringComparison.Ordinal))
                {
                    int transitionNum;
                    if (int.TryParse(transition.Name.Substring(_transitionPrefix.Length), out transitionNum))
                    {
                        if (transitionNum > _nextTransitionNumber)
                        {
                            _nextTransitionNumber = transitionNum + 1;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the name of this element conforms to the naming rules that this factory imposes.
        /// </summary>
        /// <param name="element">The element whose name is to be assessed.</param>
        /// <returns><c>true</c> if the name of this element conforms to the naming rules that this factory imposes; otherwise, <c>false</c>.</returns>
        public bool IsCanonicallyNamed(IPfcElement element)
        {

            switch (element.ElementType)
            {
                case PfcElementType.Link:
                    return _linkCanonicality(element.Name);
                case PfcElementType.Transition:
                    return _transCanonicality(element.Name);
                case PfcElementType.Step:
                    return _stepCanonicality(element.Name);
                default:
                    throw new ApplicationException("Element of type " + element.ElementType + " encountered but unknown.");
            }
        }

        /// <summary>
        /// Causes Step, Transition and Link naming cursors to retract to the sequentially-earliest
        /// name that is not currently assigned in the PFC. That is, if the next transition name to
        /// be assigned was T_044, and the otherwise-highest assigned name was T_025, the transition
        /// naming cursor would retract to T_026. The Step and Link cursors would likewise retract
        /// as a result of this call.
        /// </summary>
        public void Retract()
        {

            #region Retract Link Name
            List<string> linkNames = new List<string>();
            _hostPfc.Links.ForEach(delegate (IPfcLinkElement le)
            {
                linkNames.Add(le.Name);
            });
            while (_nextLinkNumber > 0 && !linkNames.Contains(_linkPrefix + string.Format(_linkTemplate, _nextLinkNumber - 1)))
            {
                _nextLinkNumber -= 1;
                switch (_nextLinkNumber)
                {
                    case 999:
                        _linkTemplate = "{0:D3}";
                        break;
                    case 9999:
                        _linkTemplate = "{0:D4}";
                        break;
                    case 99999:
                        _linkTemplate = "{0:D5}";
                        break;
                    default:
                        break;
                }
            }
            #endregion

            #region Retract Step Name
            List<string> stepNames = new List<string>();
            _hostPfc.Steps.ForEach(delegate (IPfcStepNode sn)
            {
                stepNames.Add(sn.Name);
            });
            while (_nextStepNumber > 0 && !stepNames.Contains(_stepPrefix + string.Format(_stepTemplate, _nextStepNumber - 1)))
            {
                _nextStepNumber -= 1;
                switch (_nextStepNumber)
                {
                    case 999:
                        _stepTemplate = "{0:D3}";
                        break;
                    case 9999:
                        _stepTemplate = "{0:D4}";
                        break;
                    case 99999:
                        _stepTemplate = "{0:D5}";
                        break;
                    default:
                        break;
                }
            }
            #endregion

            #region Retract Transition Name
            List<string> transitionNames = new List<string>();
            _hostPfc.Transitions.ForEach(delegate (IPfcTransitionNode tn)
            {
                transitionNames.Add(tn.Name);
            });
            while (_nextTransitionNumber > 0 && !transitionNames.Contains(_transitionPrefix + string.Format(_transitionTemplate, _nextTransitionNumber - 1)))
            {
                _nextTransitionNumber -= 1;
                switch (_nextTransitionNumber)
                {
                    case 999:
                        _transitionTemplate = "{0:D3}";
                        break;
                    case 9999:
                        _transitionTemplate = "{0:D4}";
                        break;
                    case 99999:
                        _transitionTemplate = "{0:D5}";
                        break;
                    default:
                        break;
                }
            }
            #endregion

        }

        #endregion
    }
}
