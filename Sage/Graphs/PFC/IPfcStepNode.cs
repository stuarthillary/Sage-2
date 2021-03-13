/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.Graphs.PFC.Execution;
using System;
using System.Collections.Generic;

namespace Highpoint.Sage.Graphs.PFC
{
    /// <summary>
    /// Implemented by an object that is an SFC SfcStep.
    /// </summary>
    public interface IPfcStepNode : IPfcNode
    {

        /// <summary>
        /// Finds the child node, if any, at the specified path relative to this node.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        IPfcElement Find(string path);

        /// <summary>
        /// Gets all of the elements that are contained in or under this Pfc, to a depth
        /// specified by the 'depth' parameter, and that pass the 'filter' criteria.
        /// </summary>
        /// <param name="depth">The depth to which retrieval is to be done.</param>
        /// <param name="filter">The filter predicate that dictates which elements are acceptable.</param>
        /// <param name="children">The children, treated as a return value.</param>
        /// <returns></returns>
        void GetChildren(int depth, Predicate<IPfcElement> filter, ref List<IPfcElement> children);

        /// <summary>
        /// Gets the actions associated with this PFC Step. They are keyed by ActionName, and are themselves, PFCs.
        /// </summary>
        /// <value>The actions.</value>
        Dictionary<string, IProcedureFunctionChart> Actions
        {
            get;
        }

        /// <summary>
        /// Adds a child Pfc into the actions list under this step.
        /// </summary>
        /// <param name="actionName">The name of this action.</param>
        /// <param name="pfc">The Pfc that contains procedural details of this action.</param>
        void AddAction(string actionName, IProcedureFunctionChart pfc);

        /// <summary>
        /// The executable action that will be performed if there are no PFCs under this step. By default, it will
        /// run the child Action PFCs in parallel, if there are any, and will return immediately if there are not.
        /// </summary>
        PfcAction LeafLevelAction
        {
            get; set;
        }

        /// <summary>
        /// Sets the Actor that will determine the behavior behind this step. The actor provides the leaf level
        /// action, as well as preconditiond for running.
        /// </summary>
        /// <param name="actor">The actor that will provide the behaviors.</param>
        void SetActor(PfcActor actor);

        /// <summary>
        /// Gets the step state machine associated with this PFC step.
        /// </summary>
        /// <value>My step state machine.</value>
        StepStateMachine MyStepStateMachine
        {
            get;
        }

        /// <summary>
        /// Returns the actions under this Step as a procedure function chart.
        /// </summary>
        /// <returns></returns>
        ProcedureFunctionChart ToProcedureFunctionChart();

        /// <summary>
        /// Gets key data on the unit with which this step is associated. Note that a step, such as a
        /// recipe start step, or one added without such data, may not hold any unit data at all. In this case,
        /// the UnitInfo property will be null.
        /// </summary>
        /// <value>The unit info.</value>
        IPfcUnitInfo UnitInfo
        {
            get; set;
        }

        /// <summary>
        /// Returns the Guid of the element in the source recipe that is represented by this PfcStep.
        /// </summary>
        Guid RecipeSourceGuid
        {
            get;
        }

        /// <summary>
        /// Gets or sets the earliest time that this element can start.
        /// </summary>
        /// <value>The earliest start.</value>
        DateTime? EarliestStart
        {
            get; set;
        }

        /// <summary>
        /// Gets permission from the step to start.
        /// </summary>
        /// <param name="myPfcec">My pfcec.</param>
        /// <param name="ssm">The StepStateMachine that will govern this run.</param>
        void GetPermissionToStart(PfcExecutionContext myPfcec, StepStateMachine ssm);

        /// <summary>
        /// Gets or sets the precondition under which this step is permitted to start. If null, permission is assumed.
        /// </summary>
        /// <value>The precondition.</value>
        PfcAction Precondition
        {
            get; set;
        }

    }
}
