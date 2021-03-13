/* This source code licensed under the GNU Affero General Public License */
using System;
using System.Collections.Generic;
using _Debug = System.Diagnostics.Debug;

namespace Highpoint.Sage.Graphs.PFC.Expressions
{
    /// <summary>
    /// A participant dictionary is a dictionary of Expression Elements that is mapped by name and by
    /// Guid. Expressions are an array of references to these elements, and formatting of these expressions
    /// is achieved by taking some format of each expression element in sequence. There are three
    /// representations of these expressions - Rote Strings, Dual Mode Strings and Macros.
    /// It is in concatenating the particular (Hostile, Friendly, Expanded) formats of those elements
    /// that an expression expresses itself.<para></para>Note: the reason that guids are needed is to support
    /// serialization through the User-Hostile mapping, and to permit renaming of steps such as when a step
    /// is flattened up into its parent, and its name goes from, e.g. &quot;Prepare-Step&quot; to 
    /// &quot;B : Xfr_Liquid2.Prepare-Step&quot;.
    /// </summary>
    public class ParticipantDirectory : IEnumerable<ExpressionElement>
    {

        #region Private Fields
        private readonly Dictionary<string, ExpressionElement> _nameMap = new Dictionary<string, ExpressionElement>();
        private readonly Dictionary<Guid, ExpressionElement> _guidMap = new Dictionary<Guid, ExpressionElement>();
        private static readonly Dictionary<Type, Macro> _knownMacros = new Dictionary<Type, Macro>();
        private ParticipantDirectory _parent = null;

        #endregion

        /// <summary>
        /// Registers a macro of the specified type, which must be an extender of the abstract class Macro.
        /// </summary>
        /// <param name="macroType">The type of the macro.</param>
        public void RegisterMacro(Type macroType)
        {
            if (!typeof(Macro).IsAssignableFrom(macroType))
            {
                throw new ApplicationException(macroType.FullName + " is being registered as a macro, but is not one.");
            }
            else
            {

                Macro macro = null;
                if (!_knownMacros.TryGetValue(macroType, out macro))
                {
                    macro = (Macro)macroType.GetConstructor(new Type[] { }).Invoke(new object[] { });
                    _knownMacros.Add(macroType, macro);
                }

                if (!_guidMap.ContainsKey(macro.Guid))
                {
                    _nameMap.Add(macro.Name.Trim('\''), macro);
                    _guidMap.Add(macro.Guid, macro);
                }
            }
        }

        /// <summary>
        /// Registers a mapping of this string as a DualMode string, creating a Guid under which it will be mapped.
        /// This will be a string that represents a step or transition, and which is backed by a Guid so that the
        /// string can be changed (such as when a step is flattened up into its parent, and its name goes from, e.g.
        /// &quot;Prepare-Step&quot; to &quot;B : Xfr_Liquid2.Prepare-Step&quot;.
        /// </summary>
        /// <param name="name">The name that is to become a DualMode string.</param>
        /// <returns>The newly-created (or preexisting, if it was there already) dual mode string element.</returns>
        public ExpressionElement RegisterMapping(string name)
        {
            string nameKey = name.Trim('\'');
            if (!_nameMap.ContainsKey(nameKey))
            {
                return RegisterMapping(name, Guid.NewGuid());
            }
            else
            {
                if (!_nameMap.ContainsKey(name))
                {
                    throw new ApplicationException(Msg_NameMapDoesntContainKey(name));
                }
                return _nameMap[name];
            }
        }

        /// <summary>
        /// Registers a mapping of this string as a DualMode string, mapped under the provided Guid.
        /// This will be a string that represents a step or transition, and which is backed by a Guid so that the
        /// string can be changed (such as when a step is flattened up into its parent, and its name goes from, e.g.
        /// "Prepare-Step" to "B : Xfr_Liquid2.Prepare-Step".
        /// </summary>
        /// <param name="name">The name that is to become a DualMode string.</param>
        /// <param name="guid">The guid by which this element is to be known.</param>
        /// <returns>
        /// The newly-created (or preexisting, if it was there already) dual mode string element.
        /// </returns>
        public ExpressionElement RegisterMapping(string name, Guid guid)
        {
            bool guidIsAlreadyKnown = _guidMap.ContainsKey(guid);
            bool nameIsAlreadyKnown = _nameMap.ContainsKey(name);
            if (!nameIsAlreadyKnown && !guidIsAlreadyKnown)
            {
                #region Create a Dual Mode String and add it into the two maps.
                DualModeString dms = new DualModeString(guid, name);
                _nameMap.Add(name, dms);
                _guidMap.Add(guid, dms);
                #endregion
            }
            else if (nameIsAlreadyKnown && !guidIsAlreadyKnown)
            {
                #region There's already one with this name, but it's got a different guid.
                int i = 1;
                while (_nameMap.ContainsKey(name + "_" + i))
                {
                    i++;
                }
                name = name + "_" + i;
                DualModeString dms = new DualModeString(guid, name);
                _nameMap.Add(name, dms);
                _guidMap.Add(guid, dms);
                #endregion
            }
            else if (!nameIsAlreadyKnown && guidIsAlreadyKnown)
            {
                #region There's already one with this Guid, but it has a different name.
                string msg = "Attempting to register a mapping between " + name + " and " + guid + " in a ParticipantDirectory, but the guid is already correlated to a different string, " + _guidMap[guid].Name + ".";
                throw new ApplicationException(msg);
                #endregion
            }
            else
            {
                // Both name and guid are known - object is already added in.
            }

            return _guidMap[guid];

        }

        /// <summary>
        /// Deletes the name GUID pair.
        /// </summary>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        public void DeleteNameGuidPair(string name, Guid guid)
        {
            ExpressionElement ee1 = _nameMap[name];
            ExpressionElement ee2 = _guidMap[guid];

            if (ee1 != null && ee1.Equals(ee2))
            {
                _nameMap.Remove(name);
                _guidMap.Remove(guid);
            }
        }

        /// <summary>
        /// Changes the GUID of an expression element from one value to another. This is needed when an Operation 
        /// or OpStep remaps its child steps.
        /// </summary>
        /// <param name="from">The guid of the expression element that the caller wants to remap.</param>
        /// <param name="to">The guid to which the caller wants to remap the expression element.</param>
        public void ChangeGuid(Guid from, Guid to)
        {
            ExpressionElement expressionElement = _guidMap[from];
            expressionElement.Guid = to;
            _guidMap.Remove(from);
            _guidMap.Add(to, expressionElement);
        }

        /// <summary>
        /// Changes the GUID of an expression element from one value to another. This is needed when an Operation 
        /// or OpStep remaps its child steps.
        /// </summary>
        /// <param name="fromElementsName">The name of the expression element that the caller wants to remap.</param>
        /// <param name="to">The guid to which the caller wants to remap the expression element.</param>
        public void ChangeGuid(string fromElementsName, Guid to)
        {
            ExpressionElement expressionElement = _nameMap[fromElementsName];
            _guidMap.Remove(expressionElement.Guid);
            expressionElement.Guid = to;
            _guidMap.Add(expressionElement.Guid, expressionElement);
        }

        /// <summary>
        /// Changes the name of an expression element from one value to another.
        /// </summary>
        /// <param name="from">The name of the expression element that the caller wants to remap.</param>
        /// <param name="to">The name to which the caller wants to remap the expression element.</param>
        public void ChangeName(string from, string to)
        {
            if (!_nameMap.ContainsKey(from))
            {
                throw new ApplicationException(Msg_NameMapDoesntContainKey(from));
            }
            ExpressionElement expressionElement = _nameMap[from];
            if (expressionElement != null)
            {
                DualModeString dms = expressionElement as DualModeString;
                if (dms != null)
                {
                    dms.Name = to;
                    _nameMap.Remove(from);
                    _nameMap.Add(to, expressionElement);
                }
            }
        }

        private string Msg_NameMapDoesntContainKey(string from)
        {
            string msg = "A caller requested an expression element from the ParticipantDirectory associated with the name, \"" + from +
                "\", but it was not there. Some possible alternatives (closely-named entries) are : ";

            List<string> closeMatchers = new List<string>();
            foreach (string potentialName in _nameMap.Keys)
            {
                if (from.Contains(potentialName) || potentialName.Contains(from))
                {
                    closeMatchers.Add(potentialName);
                }
            }

            msg += Utility.StringOperations.ToCommasAndAndedList(closeMatchers.ToArray());

            closeMatchers = new List<string>(_nameMap.Keys);

            msg += ". A list of all names in the list is " + Utility.StringOperations.ToCommasAndAndedList(closeMatchers.ToArray());

            return msg;

        }

        /// <summary>
        /// Gets the <see cref="T:ExpressionElement"/> with the specified name.
        /// </summary>
        /// <value></value>
        public ExpressionElement this[string name]
        {
            get
            {
                if (_nameMap.ContainsKey(name))
                {
                    return _nameMap[name];
                }
                else
                {
                    if (Parent != null && Parent.Contains(name))
                    {
                        return Parent[name];
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the parent participantDirectory to this one..
        /// </summary>
        /// <value>The parent.</value>
        public ParticipantDirectory Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                _parent = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="T:ExpressionElement"/> with the specified GUID.
        /// </summary>
        /// <value></value>
        public ExpressionElement this[Guid guid]
        {
            get
            {
                if (_guidMap.ContainsKey(guid))
                {
                    return _guidMap[guid];
                }
                else
                {
                    if (Parent != null && Parent.Contains(guid))
                    {
                        return Parent[guid];
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether this ParticipantDirectory contains the specified name.
        /// </summary>
        /// <param name="name">The user-friendly name of this object. Typically not required to be unique in a pan-model context.</param>
        /// <returns>
        /// 	<c>true</c> if this ParticipantDirectory contains the specified name; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string name)
        {
            return _nameMap.ContainsKey(name) || _nameMap.ContainsKey(name.Trim('\'')) || (Parent != null && Parent.Contains(name));
        }

        /// <summary>
        /// Determines whether this ParticipantDirectory contains the specified guid.
        /// </summary>
        /// <param name="guid">The guid of the object-of-interest.</param>
        /// <returns>
        /// 	<c>true</c> if this ParticipantDirectory contains the specified guid; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(Guid guid)
        {
            return _guidMap.ContainsKey(guid) || (Parent != null && Parent.Contains(guid));
        }

        #region IEnumerable<ExpressionElement> Members

        /// <summary>
        /// Returns an enumerator that iterates through the ExpressionElements in the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<ExpressionElement> GetEnumerator()
        {
            return _nameMap.Values.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the ExpressionElements in the collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        /// </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _nameMap.Values.GetEnumerator();
        }

        #endregion


        /// <summary>
        /// Refreshes the Participant Directory to contain only the ExpressionElements in the specified PFC.
        /// Performed via a Mark-and-Sweep algorithm.
        /// </summary>
        /// <param name="pfc">The PFC.</param>
        internal void Refresh(ProcedureFunctionChart pfc)
        {

            foreach (ExpressionElement ee in _nameMap.Values)
            {
                ee.Marked = false;
                _Debug.Assert(_guidMap.ContainsValue(ee));
            }

            foreach (ExpressionElement ee in _guidMap.Values)
            {
                _Debug.Assert(ee.Marked == false);
                _Debug.Assert(_nameMap.ContainsValue(ee));
            }

            foreach (IPfcNode node in pfc.Steps)
            {
                if (_nameMap.ContainsKey(node.Name))
                {
                    _nameMap[node.Name].Marked = true;
                }
            }

            // PCB20080725: Was this. Changed to the following, to key on name.
            //foreach (IPfcTransitionNode trans in pfc.Transitions) {
            //    foreach (ExpressionElement ee in trans.Expression.Elements) {
            //        ee.Marked = true;
            //    }
            //}

            foreach (IPfcTransitionNode trans in pfc.Transitions)
            {
                foreach (ExpressionElement ee in trans.Expression.Elements)
                {
                    if (ee is Macro)
                    {
                        _nameMap[ee.Name.Trim('\'')].Marked = true;
                    }
                    else if (ee.Name != string.Empty)
                    {
                        _nameMap[ee.Name].Marked = true;
                    }
                }
            }

            List<ExpressionElement> rejects = new List<ExpressionElement>();
            foreach (ExpressionElement ee in _nameMap.Values)
            {
                if (!ee.Marked)
                {
                    rejects.Add(ee);
                }
                ee.Marked = false;
            }

            foreach (ExpressionElement ee in rejects)
            {
                _nameMap.Remove(ee.Name);
                _guidMap.Remove(ee.Guid);
            }
        }
    }
}