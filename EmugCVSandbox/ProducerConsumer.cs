using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace EmugCVSandbox
{
    class ProducerConsumer<T> : IDisposable where T : class
    {
        object locker = new object();
        Thread[] workers;
        Queue<T> taskQ = new Queue<T>();
        Action<T> _consumeAction;

        public ProducerConsumer(int workerCount, Action<T> consumeAction)
        {
            workers = new Thread[workerCount];
            _consumeAction = consumeAction;
            // Create and start a separate thread for each worker
            for (int i = 0; i < workerCount; i++)
                (workers[i] = new Thread(Consume)).Start();
        }

        public void Dispose()
        {
            // Enqueue one null task per worker to make each exit.
            foreach (Thread worker in workers) EnqueueTask(null);
            foreach (Thread worker in workers) worker.Join();
        }

        public void EnqueueTask(T task)
        {
            lock (locker)
            {
                taskQ.Enqueue(task);
                Monitor.PulseAll(locker);
            }
        }

        void Consume()
        {
            while (true)
            {
                T task;
                lock (locker)
                {
                    while (taskQ.Count == 0) Monitor.Wait(locker);
                    task = taskQ.Dequeue();
                }
                if (task == null) return;         // This signals our exit
                if (_consumeAction != null)
                {
                    _consumeAction.Invoke(task);
                }
            }
        }
    }
}

