/* This source code licensed under the GNU Affero General Public License */

using System.Collections;

namespace Highpoint.Sage.SimCore
{
    internal class MergedTransitionHandler : TransitionHandler
    {
        public MergedTransitionHandler(TransitionHandler inbound,
                                       TransitionHandler across,
                                       TransitionHandler outbound,
                                       TransitionHandler universal)
        {

            MergeHandlers(ref prepareHandlers, inbound.PrepareHandlers, outbound.PrepareHandlers, across.PrepareHandlers, universal.PrepareHandlers);
            MergeHandlers(ref commitHandlers, inbound.CommitHandlers, outbound.CommitHandlers, across.CommitHandlers, universal.CommitHandlers);
            MergeHandlers(ref rollbackHandlers, inbound.RollbackHandlers, outbound.RollbackHandlers, across.RollbackHandlers, universal.RollbackHandlers);

        }

        private void MergeHandlers(ref SortedList target, SortedList src1, SortedList src2, SortedList src3, SortedList src4)
        {
            int nextKey = 0;
            ArrayList enumerators = new ArrayList();
            enumerators.Add(src1.GetEnumerator());
            enumerators.Add(src2.GetEnumerator());
            enumerators.Add(src3.GetEnumerator());
            enumerators.Add(src4.GetEnumerator());

            ArrayList removees = new ArrayList();
            foreach (IEnumerator enumerator in enumerators)
            {
                if (!enumerator.MoveNext())
                    removees.Add(enumerator);
            }
            foreach (IEnumerator removee in removees)
                enumerators.Remove(removee);

            while (enumerators.Count != 0)
            {
                IEnumerator hostEnum = (IEnumerator)enumerators[0];
                double lowest = (double)((DictionaryEntry)hostEnum.Current).Key;
                foreach (IEnumerator enumerator in enumerators)
                {
                    double thisKey = (double)((DictionaryEntry)enumerator.Current).Key;
                    if (thisKey < lowest)
                    {
                        hostEnum = enumerator;
                        lowest = thisKey;
                    }
                }

                target.Add(nextKey++, ((DictionaryEntry)hostEnum.Current).Value);
                if (!hostEnum.MoveNext())
                    enumerators.Remove(hostEnum);
            }
        }
    }

}
