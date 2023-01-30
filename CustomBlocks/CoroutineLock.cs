using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DestinyCustomBlocks
{
    // Simple class designed to act like a thread lock for coroutines working on
    // the same thread. Mainly used by the image related stuff to make sure it's
    // all handled in the correct order.
    public class CoroutineLock : MonoBehaviour
    {
        private Queue<object> queue;

        void Start()
        {
            this.queue = new Queue<object>();
        }

        /*
         * Creates a new object, assining it to the lock variable, and then
         * waits for it to be next before calling the callback with it.
         */
        public IEnumerator Lock(Action<object> callback)
        {
            // Create a new object, add it to the queue, then wait for it to be
            // next in line before returning.
            var ticket = new object();
            this.queue.Enqueue(ticket);
            while (this.queue.Peek() != ticket)
            {
                yield return null;
            }
            callback(ticket);
        }

        /*
         * Removed the object from the front of the queue. Returns false if the
         * object provided is not at the front of the queue.
         */
        public bool Unlock(object _lock)
        {
            if (this.queue.Peek() == _lock)
            {
                this.queue.Dequeue();
                return true;
            }
            return false;
        }
    }
}
