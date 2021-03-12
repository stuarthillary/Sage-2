/* COPYRIGHT_NOTICE */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Highpoint.Sage.Utility;
using System.Linq;

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

        private CanonicalityTest m_linkCanonicality = new CanonicalityTest(delegate (string s) { int x; return (s.StartsWith("L_") && int.TryParse(s.Substring(2), out x)); });
        private static string _linkTemplate = "{0:D3}";
        private static string _linkPrefix = "L_";

        private CanonicalityTest m_stepCanonicality = new CanonicalityTest(delegate (string s) { int x; return (s.StartsWith("S_") && int.TryParse(s.Substring(2), out x)); });
        private static string _stepTemplate = "{0:D3}";
        private static string _stepPrefix = "S_";

        private CanonicalityTest m_transCanonicality = new CanonicalityTest(delegate (string s) { int x; return ((s.StartsWith("T_") && int.TryParse(s.Substring(2), out x)) || (s.StartsWith("T") && int.TryParse(s.Substring(1), out x))); });
        private static string _transitionTemplate = "{0:D3}";
        private static string _transitionPrefix = "T_";

        private int m_nextLinkNumber = 0;
        private int m_nextStepNumber = 0;
        private int m_nextTransitionNumber = 0;
        private IProcedureFunctionChart m_hostPfc = null;
        private Guid m_seedGuid = Guid.Empty;
        private Guid m_maskGuid = Guid.Empty;
        private bool m_repeatable = false;
        private GuidGenerator m_guidGenerator = null;
        #endregion Private Members

        #region Private Helper Methods

        private string NextLinkName()
        {
            if (m_nextLinkNumber == 1000)
            {
                _linkTemplate = "{0:D4}";
            }

            if (m_nextLinkNumber == 10000)
            {
                _linkTemplate = "{0:D5}";
            }

            if (m_nextLinkNumber == 100000)
            {
                _linkTemplate = "{0:D6}";
            }

            return _linkPrefix + string.Format(_linkTemplate, m_nextLinkNumber++);
        }

        private string NextStepName()
        {
            if (m_nextStepNumber == 1000)
            {
                _stepTemplate = "{0:D4}";
            }

            if (m_nextStepNumber == 10000)
            {
                _stepTemplate = "{0:D5}";
            }

            if (m_nextStepNumber == 100000)
            {
                _stepTemplate = "{0:D6}";
            }

            return _stepPrefix + string.Format(_stepTemplate, m_nextStepNumber++);
        }

        private string NextTransitionName()
        {
            if (m_nextTransitionNumber == 1000)
            {
                _transitionTemplate = "{0:D4}";
            }

            if (m_nextTransitionNumber == 10000)
            {
                _transitionTemplate = "{0:D5}";
            }

            if (m_nextTransitionNumber == 100000)
            {
                _transitionTemplate = "{0:D6}";
            }

            return _transitionPrefix + string.Format(_transitionTemplate, m_nextTransitionNumber++);
        }

        private Guid NextGuid()
        {
            if (m_repeatable)
            {
                m_seedGuid = GuidOps.Increment(m_seedGuid);
                m_seedGuid = GuidOps.Rotate(m_seedGuid, -1); // right-shift with wrap.
                m_seedGuid = GuidOps.XOR(m_seedGuid, m_maskGuid);
                return m_seedGuid;
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
            m_guidGenerator = new GuidGenerator(Guid.Empty, Guid.Empty, 0);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="T:PfcElementFactory"/> class.
        /// </summary>
        public PfcElementFactory(GuidGenerator guidGenerator)
        {
            m_guidGenerator = guidGenerator;
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
            m_repeatable = true;
            m_maskGuid = maskGuid;
            m_guidGenerator = new GuidGenerator(Guid.Empty, maskGuid, 1);
            m_guidGenerator.Passthrough = true;
        }

        #region IPfcElementFactory Members

        /// <summary>
        /// Gets the Procedure Function Chart for which this factory is creating elements.
        /// </summary>
        /// <value>The host PFC.</value>
        public IProcedureFunctionChart HostPfc { get { return m_hostPfc; } set { m_hostPfc = value; } }

        /// <summary>
        /// Gets the GUID generator in use by this element factory.
        /// </summary>
        /// <value>The GUID generator.</value>
        public GuidGenerator GuidGenerator { get { return m_guidGenerator; } }

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

            IPfcStepNode node = NewStepNode(m_hostPfc, name, guid, description);

            return node;
        }

        public virtual IPfcStepNode NewStepNode(IProcedureFunctionChart parent, string name, Guid guid, string description)
        {
            Debug.Assert(parent.Equals(m_hostPfc));
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

            IPfcTransitionNode node = NewTransitionNode(m_hostPfc, name, guid, description);

            return node;
        }

        public virtual IPfcTransitionNode NewTransitionNode(IProcedureFunctionChart parent, string name, Guid guid, string description)
        {
            Debug.Assert(parent.Equals(m_hostPfc));
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
            return NewLinkElement(m_hostPfc, name, guid, description);
        }

        public virtual IPfcLinkElement NewLinkElement(IProcedureFunctionChart parent, string name, Guid guid, string description)
        {
            Debug.Assert(parent.Equals(m_hostPfc));
            return new PfcLink(parent, name, description, guid);
        }

        public virtual void Initialize(IPfcStepNode stepNode) { }

        public virtual void Initialize(IPfcTransitionNode transitionNode) { }

        public virtual void Initialize(IPfcLinkElement linkElement) { }

        /// <summary>
        /// Called when the loading of a new PFC has been completed.
        /// </summary>
        /// <param name="newPfc">The new PFC.</param>
        public void OnPfcLoadCompleted(IProcedureFunctionChart newPfc)
        {
            foreach (IPfcLinkElement link in newPfc.Links)
            {
                if (link.Name.StartsWith(_linkPrefix))
                {
                    int linkNum;
                    if (int.TryParse(link.Name.Substring(_linkPrefix.Length), out linkNum))
                    {
                        if (linkNum > m_nextLinkNumber)
                        {
                            m_nextLinkNumber = linkNum + 1;
                        }
                    }
                }
            }

            foreach (IPfcStepNode step in newPfc.Steps)
            {
                if (step.Name.StartsWith(_stepPrefix))
                {
                    int stepNum;
                    if (int.TryParse(step.Name.Substring(_stepPrefix.Length), out stepNum))
                    {
                        if (stepNum > m_nextStepNumber)
                        {
                            m_nextStepNumber = stepNum + 1;
                        }
                    }
                }
            }

            foreach (IPfcTransitionNode transition in newPfc.Transitions)
            {
                if (transition.Name.StartsWith(_transitionPrefix))
                {
                    int transitionNum;
                    if (int.TryParse(transition.Name.Substring(_transitionPrefix.Length), out transitionNum))
                    {
                        if (transitionNum > m_nextTransitionNumber)
                        {
                            m_nextTransitionNumber = transitionNum + 1;
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
                    return m_linkCanonicality(element.Name);
                case PfcElementType.Transition:
                    return m_transCanonicality(element.Name);
                case PfcElementType.Step:
                    return m_stepCanonicality(element.Name);
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
            m_hostPfc.Links.ForEach(delegate (IPfcLinkElement le) { linkNames.Add(le.Name); });
            while (m_nextLinkNumber > 0 && !linkNames.Contains(_linkPrefix + string.Format(_linkTemplate, m_nextLinkNumber - 1)))
            {
                m_nextLinkNumber -= 1;
                switch (m_nextLinkNumber)
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
            m_hostPfc.Steps.ForEach(delegate (IPfcStepNode sn) { stepNames.Add(sn.Name); });
            while (m_nextStepNumber > 0 && !stepNames.Contains(_stepPrefix + string.Format(_stepTemplate, m_nextStepNumber - 1)))
            {
                m_nextStepNumber -= 1;
                switch (m_nextStepNumber)
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
            m_hostPfc.Transitions.ForEach(delegate (IPfcTransitionNode tn) { transitionNames.Add(tn.Name); });
            while (m_nextTransitionNumber > 0 && !transitionNames.Contains(_transitionPrefix + string.Format(_transitionTemplate, m_nextTransitionNumber - 1)))
            {
                m_nextTransitionNumber -= 1;
                switch (m_nextTransitionNumber)
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
