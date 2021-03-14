/* This source code licensed under the GNU Affero General Public License */

using System;
using System.Collections;

namespace Highpoint.Sage.SimCore
{
    internal class TransitionHandler : ITransitionHandler
    {

        #region Prepare Event
        protected SortedList prepareHandlers = new SortedList();
        private double _nextPreparePriority = 0.0;
        public event PrepareTransitionEvent Prepare
        {
            add
            {
                AddPrepareEvent(value, (_nextPreparePriority += double.Epsilon));
            }
            remove
            {
                RemovePrepareEvent(value);
            }
        }
        public void AddPrepareEvent(PrepareTransitionEvent pte, double priority)
        {
            if (!prepareHandlers.ContainsValue(pte))
            {
                prepareHandlers.Add(priority, pte);
            }
        }
        public void RemovePrepareEvent(PrepareTransitionEvent pte)
        {
            if (prepareHandlers.ContainsValue(pte))
            {
                prepareHandlers.Remove(commitHandlers.GetKey(commitHandlers.IndexOfValue(pte)));
            }
        }
        internal SortedList PrepareHandlers
        {
            get
            {
                return prepareHandlers;
            }
        }
        #endregion Prepare Event

        #region Commit Event
        protected SortedList commitHandlers = new SortedList();
        private double _nextCommitPriority = 0.0;
        public event CommitTransitionEvent Commit
        {
            add
            {
                AddCommitEvent(value, (_nextCommitPriority += double.Epsilon));
            }
            remove
            {
                RemoveCommitEvent(value);
            }
        }
        public void AddCommitEvent(CommitTransitionEvent cte, double priority)
        {
            if (!commitHandlers.ContainsValue(cte))
            {
                commitHandlers.Add(priority, cte);
            }
        }
        public void RemoveCommitEvent(CommitTransitionEvent cte)
        {
            if (commitHandlers.ContainsValue(cte))
            {
                commitHandlers.Remove(commitHandlers.GetKey(commitHandlers.IndexOfValue(cte)));
            }

        }
        internal SortedList CommitHandlers
        {
            get
            {
                return commitHandlers;
            }
        }
        #endregion

        #region Rollback Event
        protected SortedList rollbackHandlers = new SortedList();
        private double _nextRollbackPriority = 0.0;
        public event RollbackTransitionEvent Rollback
        {
            add
            {
                AddRollbackEvent(value, (_nextRollbackPriority += double.Epsilon));
            }
            remove
            {
                RemoveRollbackEvent(value);
            }
        }
        public void AddRollbackEvent(RollbackTransitionEvent rte, double priority)
        {
            if (!rollbackHandlers.ContainsValue(rte))
            {
                rollbackHandlers.Add(priority, rte);
            }
        }
        public void RemoveRollbackEvent(RollbackTransitionEvent rte)
        {
            if (rollbackHandlers.ContainsValue(rte))
            {
                rollbackHandlers.Remove(commitHandlers.GetKey(commitHandlers.IndexOfValue(rte)));
            }
        }
        internal SortedList RollbackHandlers
        {
            get
            {
                return rollbackHandlers;
            }
        }
        #endregion

        public bool IsValidTransition
        {
            get
            {
                return true;
            }
        }

        public IList DoPrepare(IModel model, object userData)
        {
            ArrayList al = new ArrayList();
            for (int i = 0; i < prepareHandlers.Count; i++)
            {
                PrepareTransitionEvent pte = (PrepareTransitionEvent)prepareHandlers.GetByIndex(i);
                object result = pte(model, userData);
                if (result != null)
                    al.Add(result);
            }
            return al;
        }

        public void DoCommit(IModel model, object userData)
        {
            for (int i = 0; i < commitHandlers.Count; i++)
            {
                CommitTransitionEvent cte = (CommitTransitionEvent)commitHandlers.GetByIndex(i);
                cte(model, userData);
            }
        }

        public void DoRollback(IModel model, object userData, IList failureReasons)
        {
            for (int i = 0; i < rollbackHandlers.Count; i++)
            {
                RollbackTransitionEvent rte = (RollbackTransitionEvent)rollbackHandlers.GetByIndex(i);
                rte(model, userData, failureReasons);
            }
        }

        public string Dump()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Recipients for \"Prepare\" event:");
            sb.Append("\t");
            sb.Append(prepareHandlers.Count);
            sb.Append("\r\n");
            sb.Append(DumpHandlers(prepareHandlers));

            sb.Append("Recipients for \"Rollback\" event:");
            sb.Append("\t");
            sb.Append(rollbackHandlers.Count);
            sb.Append("\r\n");
            sb.Append(DumpHandlers(rollbackHandlers));

            sb.Append("Recipients for \"Commit\" event:");
            sb.Append("\t");
            sb.Append(commitHandlers.Count);
            sb.Append("\r\n");
            sb.Append(DumpHandlers(commitHandlers));
            return sb.ToString();
        }

        private string DumpHandlers(SortedList handlers)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int i = 0;
            foreach (DictionaryEntry de in handlers)
            {
                double pri = Convert.ToDouble(de.Key);
                Delegate del = (Delegate)de.Value;
                sb.Append("\t" + i + ".)\t[" + del.Target + "].[" + del.Method + "] @ pri = " + pri + "\r\n");
                i++;
            }
            return sb.ToString();
        }
    }

}
