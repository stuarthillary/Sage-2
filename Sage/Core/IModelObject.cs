/* This source code licensed under the GNU Affero General Public License */

using System;
#pragma warning disable 1587

/// <summary>
/// There are several key areas of interest in SimCore. They are the Executive, the Model, and the State Machine.
/// <para/>
/// The base engine behind Sage® consists of two orthogonal pieces, the executive and the model. The executive 
/// is a C# class with several supporting classes, that performs event (callback) registration and sequencing. In 
/// essence, through the executive, a model entity may request to receive a callback at a specific time, with a specific 
/// priority, on a specified method, and with a specified object provided to that method on the callback. The entity 
/// may rescind that request at any point before the call, and the method need not be located on the entity requesting 
/// the callback. Further, the entity requesting the callback may select how the callback is to be handled, currently 
/// among three choices:<para/>
/// 1.	<b>Synchronous</b> – the callback is called on the dispatch thread, and upon completion, the next callback is
/// selected based upon scheduled time and priority. This is similar to the “event queue” implementations in Garrido
/// (1993) and Law and Kelton (2000).<para/>
/// 2.	<b>Detachable</b> – the callback is called on a thread from the .Net thread pool, and the dispatch thread then
/// suspends awaiting the completion or suspension of that thread. If the event thread is sus-pended, an event controller
/// is made available to other entities which can be used to resume or abort that thread. This is useful for modeling
/// “intelligent entities” and situations where the developer wants to easily represent a delay or interruption of a process.<para/>
/// 3.	<b>Asynchronous</b> – the callback is called on a thread from the thread pool that is, in essence, fire-and-forget.
/// This is useful when the thread has a long task to perform, such as I/O, or external system interfacing (i.e. data export)
/// and the results of that activity cannot affect the simulation.
/// <code>// public member of Executive class.
/// public long RequestEvent(
/// ExecEventReceiver eer, // user callback
/// DateTime when, 
/// double priority, 
/// object userData, 
/// ExecEventType execEventType){ … }
/// </code>
/// <para/>
/// The Model class provided with Sage® performs containment and coordination between the executive, the model state 
/// machine and model entities such as queues, customers, manufacturing stages, transport hubs, etc.
/// <para/>
/// The model’s state machine is used to control and indicate the state of the model – for example, a model that has
/// states such as design, initialization, warmup, run, cooldown, data analysis, and perhaps pause, would represent
/// each of those states in the state machine. Additionally, the application designer may attach a handler to any specified
/// transition into or out of any given state, or between two specific states. Handlers may be given a sequence number to
/// describe the order in which they are to be executed. Each transition is performed through a two-phase-commit protocol,
/// with a prepare phase permitting registrants to indicate approval or denial of the transition, and a commit or rollback
/// phase completing or canceling the attempted transition.
/// The following code describes the interface that is implemented by a transition handler. User code may implement any of 
/// the three delegates (API signatures) at the top of the listing, and add the callback to the handlers for transition out
/// of, into, or between specified stages.
/// <code>
/// public delegate ITransitionFailureReason PrepareTransitionEvent(Model model);
/// public delegate void CommitTransitionEvent(Model model);
/// public delegate void RollbackTransitionEvent(Model model, IList reasons);
/// 
/// public interface ITransitionHandler {
/// 	event PrepareTransitionEvent Prepare;
/// 	event CommitTransitionEvent Commit;
/// 	event RollbackTransitionEvent Rollback;
/// 	bool IsValidTransition { get; }
/// 
/// 	void AddPrepareEvent(PrepareTransitionEvent pte,double sequence);
/// 	void RemovePrepareEvent(PrepareTransitionEvent pte);
/// 	void AddCommitEvent(CommitTransitionEvent cte,double sequence);
/// 	void RemoveCommitEvent(CommitTransitionEvent cte);
/// 	void AddRollbackEvent(RollbackTransitionEvent rte,double sequence);
/// 	void RemoveRollbackEvent(RollbackTransitionEvent rte);
/// }
/// </code>
/// </summary>
namespace Highpoint.Sage.SimCore
{
    /// <summary>
    /// Implemented by an object that 'belongs' to a model, or that needs to know its
    /// model in order to function properly.
    /// </summary>
    public interface IModelObject : IHasIdentity
    {
        /// <summary>
        /// The model that owns this object, or from which this object gets time, etc. data.
        /// </summary>
        IModel Model
        {
            get;
        }

        /// <summary>
        /// Initializes the fields that feed the properties of this IModelObject identity.
        /// </summary>
        /// <param name="model">The IModelObject's new model value.</param>
        /// <param name="name">The IModelObject's new name value.</param>
        /// <param name="description">The IModelObject's new description value.</param>
        /// <param name="guid">The GUID of this object. Typically registered as this object's ModelObject key, and thus, required to be unique in a pan-model context.</param>
        void InitializeIdentity(IModel model, string name, string description, Guid guid);

        //bool Intrinsic { set; get; }


    }
}
