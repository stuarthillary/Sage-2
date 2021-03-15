/* This source code licensed under the GNU Affero General Public License */

using System.Collections;
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Highpoint.Sage.Resources
{
    /// <summary>
    /// MultiRequestProcessor provides ways to manipulate multiple resource requests at the same time.
    /// All requests must have a default resource manager specified, unless otherwise indicated in the
    /// specific API.
    /// </summary>
    public class MultiRequestProcessor
    {

        /// <summary>
        /// Replicates the specified requests.
        /// </summary>
        /// <param name="requests">The requests.</param>
        /// <returns></returns>
		public static IResourceRequest[] Replicate(ref IResourceRequest[] requests)
        {
            IResourceRequest[] replicates = new IResourceRequest[requests.Length];
            for (int i = 0; i < requests.Length; i++)
            {
                replicates[i] = requests[i].Replicate();
            }
            return replicates;
        }

        /// <summary>
        /// Acquires all of the resources referred to in the array of requests,
        /// or if it cannot, it acquires none of them. If the blocking parameter is
        /// true, it keeps trying until it is successful. Otherwise, it tries once,
        /// and returns immediately, indicating success or failure.
        /// </summary>
        /// <param name="requests">The resource requests on which this processor is to operate.</param>
        /// <param name="blockAwaitingAcquisition">If true, this call blocks until the resource is available.</param>
        /// <returns>true if the acquisition was successful, false otherwise.</returns>
        public static bool AcquireAll(ref IResourceRequest[] requests, bool blockAwaitingAcquisition)
        {
            if (blockAwaitingAcquisition)
                return AcquireAllWithWait(ref requests);

            bool successful = true;
            int i = 0;
            for (; i < requests.GetLength(0); i++)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (requests[i].QuantityObtained == requests[i].QuantityDesired)
                    continue; // Already reserved.
                if (!requests[i].Reserve(null, false))
                {
                    successful = false;
                    break;
                }
            }

            if (successful)
                i--; // walked off the end - get back to last live index.
            for (; i >= 0; --i)
            {
                lock (requests[i].ResourceObtained)
                {
                    requests[i].Unreserve();
                    if (successful)
                    {
                        requests[i].Acquire(null, false);
                    }
                }
            }
            return successful;
        }

        /// <summary>
        /// Reserves all of the resources referred to in the array of requests,
        /// or if it cannot, it acquires none of them. If the blocking parameter is
        /// true, it keeps trying until it is successful. Otherwise, it tries once,
        /// and returns immediately, indicating success or failure.
        /// </summary>
        /// <param name="requests">The resource requests on which this processor is to operate.</param>
        /// <param name="blockAwaitingAcquisition">If true, this call blocks until the resource is available.</param>
        /// <returns>true if the reservation was successful, false otherwise.</returns>
        public static bool ReserveAll(ref IResourceRequest[] requests, bool blockAwaitingAcquisition)
        {
            if (blockAwaitingAcquisition)
                return ReserveAllWithWait(ref requests);

            bool successful = true;
            ArrayList successes = new ArrayList(requests.Length);
            int i = 0;
            for (; i < requests.GetLength(0); i++)
            {
                if (!requests[i].Reserve(null, false))
                {
                    successful = false;
                    break;
                }
                successes.Add(requests[i]);
            }
            if (!successful)
            {
                foreach (IResourceRequest irr in successes)
                    irr.Unreserve();
            }
            return successful;
        }

        /// <summary>
        /// Releases all of the resources in the provided ResourceRequests.
        /// </summary>
        public static void ReleaseAll(ref IResourceRequest[] requests)
        {
            foreach (IResourceRequest irr in requests)
                irr.Release();
        }

        /// <summary>
        /// Unreserves all of the resources in the provided ResourceRequests.
        /// </summary>
        public static void UnreserveAll(ref IResourceRequest[] requests)
        {
            foreach (IResourceRequest irr in requests)
            {
                irr.Unreserve();
            }
        }


        private static bool ReserveAllWithWait(ref IResourceRequest[] requests)
        {

            #region >>> Acquire all resources without deadlock. <<<
            // We will maintain a queue of resource requirements. The first one in the
            // queue is reserved with a wait-lock, and subsequent RP's are reserved
            // without a wait lock. If a reservation succeeds, then the RP is requeued
            // at the end of the queue. If it fails, then all RP's in the queue are
            // unreserved, and the next attempt begins at the beginning of the queue.
            // -
            // Note that in this next attempt, the one for whom reservation has most
            // recently failed is still at the head of the queue, and is the one that
            // is reserved with a wait-lock.

            Hashtable successes = new Hashtable();
            Queue rscQueue = new Queue();

            #region >>> Load the queue with the resource requests. <<< 
            foreach (IResourceRequest irr in requests)
                rscQueue.Enqueue(irr);

            #endregion

            bool nextIsMaster = true;
            while (rscQueue.Count > 0)
            {
                IResourceRequest rp = (IResourceRequest)rscQueue.Peek();
                if (successes.Contains(rp))
                    break; // We've acquired all of them.
                bool rpSucceeded = rp.Reserve(null, nextIsMaster);
                nextIsMaster = !rpSucceeded; // If failed, the next time through, the head of the q will be master.
                if (!rpSucceeded)
                {
                    foreach (IResourceRequest reset in rscQueue)
                    {
                        if (successes.Contains(rp))
                            reset.Unreserve();
                    }
                    successes.Clear();
                }
                else
                {
                    rscQueue.Enqueue(rscQueue.Dequeue()); // Send the successful request to the back of the queue.
                    successes.Add(rp, rp);
                }
            }
            //			if ( rscQueue.Count == 0 ) {
            //				_Debug.WriteLine("No resources were requested.");
            //			}
            #endregion

            return true;
        }

        private static bool AcquireAllWithWait(ref IResourceRequest[] requests)
        {

            if (ReserveAllWithWait(ref requests))
            {
                foreach (IResourceRequest rrq in requests)
                {
                    //rrq.Unreserve();
                    rrq.Acquire(null, true);
                }
                return true;
            }

            return false;
        }
    }
}
