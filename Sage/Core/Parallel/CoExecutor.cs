﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Highpoint.Sage.Utility;

namespace Highpoint.Sage.SimCore.Parallel
{
    /// <summary>
    /// Class CoExecutor is responsible for starting, and maintaining running, a set of implementers of
    /// IParallelExec until all have finished running their events.
    /// </summary>
    public class CoExecutor
    {
        // ReSharper disable once InconsistentNaming
        private static readonly bool m_diagnostics = Diagnostics.DiagnosticAids.Diagnostics("CoExecutor");
        private int m_nExecsAtEndTime;
        private readonly IParallelExec[] m_execs;
        private readonly DateTime m_terminateAt;

        private CoExecutor(IParallelExec[] execs, DateTime terminateAt)
        {
            for (int i = 0; i < execs.Length; i++)
            {
                if (execs[i] == null) execs[i] = (IParallelExec)ExecFactory.Instance.CreateExecutive(ExecType.ParallelSimulation);
            }
            m_execs = execs;
            m_terminateAt = terminateAt;
            foreach (IParallelExec executive in m_execs)
            {
                executive.Coexecutor = this;
            }
        }

        private void StartAll()
        {
            DateTime start = DateTime.Now;
            Thread[] threads = new Thread[m_execs.Length];
            Console.WriteLine("Creating all threads.");
            foreach (IParallelExec executive in m_execs)
            {
                Monitor.Enter(executive);
            }
            for (int i = 0; i < m_execs.Length; i++)
            {
                int ndx = i;
                if (m_diagnostics) Console.WriteLine("Creating thread {0}.", ndx);
                threads[ndx] = new Thread(() =>
                {
                    // ReSharper disable once EmptyEmbeddedStatement
                    try
                    {
                        if (m_diagnostics) Console.WriteLine("Starting executive {0}.", ndx);
                        m_execs[ndx].RequestEvent(CoTerminate, m_terminateAt);
                        m_execs[ndx].Start();
                    }

                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }) {Name = m_execs[ndx].Name, Priority = ThreadPriority.Normal};

            }

            foreach (Thread t in threads) t.Start();

            if (m_diagnostics) Console.WriteLine("Starting all tasks.");
            foreach (IParallelExec executive in m_execs)
            {
                Monitor.Exit(executive);
            }

            // TODO: What if one of the execs finishes between the last line and this one?
            // TODO: INVESTIGATE, can this line go after 'foreach (Thread t in threads) t.Start();'?
            foreach (Thread t in threads) t.Join();

            
            DateTime finish = DateTime.Now;
            if (m_diagnostics) Console.WriteLine("All threads complete in {0} seconds.", (finish - start).TotalSeconds);
        }

        public static void CoStart(IParallelExec[] execs, DateTime terminateAt)
        {
            new CoExecutor(execs, terminateAt).StartAll();
        }

        public DateTime GetEarliestExecDateTime()
        {
            DateTime dt = DateTime.MaxValue;
            foreach (IParallelExec executive in m_execs)
            {
                dt = DateTimeOperations.Min(dt, executive.Now);
            }
            return dt;
        }

        List<IParallelExec> m_activeRollbackInitiators = new List<IParallelExec>(); 
        /// <summary>
        /// All of the execs must stop during a rollback, for now. // TODO: Maybe figure out how not to halt everyone.
        /// CoExecutor's m_rollbackLock ensures that only one rollback happens at a time.
        /// 
        /// 1.) Pause all executives somewhere (if it's a pending read block, and the exec needs to rollback, the
        ///     Pending Read Block will be aborted.)
        /// 2.) 
        /// </summary>
        /// <param name="toWhen">The time to which a rollback is desired.</param>
        public void RollBack(DateTime toWhen, IParallelExec onBehalfOf)
        {
            IParallelExec localOnBehalfOf = onBehalfOf;
            if (m_activeRollbackInitiators.Contains(localOnBehalfOf)) return;
            m_activeRollbackInitiators.Add(localOnBehalfOf);
            // "Close the door" for all executives so that they stop at the RollbackBlock.
            foreach (IParallelExec executive in m_execs) executive.RollbackBlock.Reset();

            // By executing this in another thread, we allow this one, an executive thread, to proceed to its rollback lock.
            ThreadPool.QueueUserWorkItem(state =>
            {
                // Wait until all execs have stopped somewhere. Could be pending read block, or could be rollback block.
                while (m_execs.Any(n => !(n.IsBlockedAtRollbackBlock || n.IsBlockedInPendingReadCall))) {/* NOOP */}

                // Create the list of execs that need to roll back.
                List<IParallelExec> targets = m_execs.Where(parallelExec => parallelExec.Now > toWhen).ToList();

                if (m_diagnostics) Console.WriteLine("Rolling back {0} to {1}.", StringOperations.ToCommasAndAndedList(targets, n => n.Name), toWhen);

                foreach (IParallelExec target in targets)
                {
                    // Any target executive that's not at the Rollback Block is stuck at the Pending Read Block. 
                    // The pending read block must be aborted so that it can advance to the Rollback Block.
                    while (!target.IsBlockedAtRollbackBlock) target.PendingReadBlock.Set();
                }

                // Now execute rollbacks on targets.
                System.Threading.Tasks.Parallel.ForEach(targets, n => n.PerformRollback(toWhen));

                // All tasks have completed rollback. Resume running.
                foreach (IParallelExec executive in m_execs) executive.RollbackBlock.Set();

                m_activeRollbackInitiators.Remove(localOnBehalfOf);

            }); // Allows this thread to proceed to its RollbackBlock.
            
        }

        private void CoTerminate(IExecutive executive, object userData)
        {
            IParallelExec exec = (IParallelExec)executive;
            if (m_diagnostics) Console.WriteLine("{0} asking to terminate at {1}", exec.Name, exec.Now);
            Interlocked.Increment(ref m_nExecsAtEndTime);
            if (m_nExecsAtEndTime == m_execs.Length) foreach (IParallelExec exec2 in m_execs) exec2.Stop();
            else Thread.Sleep(500);
            Interlocked.Decrement(ref m_nExecsAtEndTime);
            exec.RequestEvent(CoTerminate, m_terminateAt);
        }
    }
}
