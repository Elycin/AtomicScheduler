using AtomicScheduler.Drivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AtomicScheduler
{
    class Scheduler
    {

        private IAtomicDriver driver;
        private Thread _schedulerThread;
        private List<ScheduledTask> tasks;

        public Scheduler()
        {

        }

        public void AddTask(ScheduledTask task)
        {
            task.HookScheduler(this);
            this.tasks.Add(task);
        }

        public void AddTask(List<ScheduledTask> tasks)
        {
            this.tasks.AddRange(tasks);
        }

        public void Start()
        {
            _schedulerThread = new Thread(Worker);
            _schedulerThread.Start();
        }

        public void Stop()
        {
            _schedulerThread.Abort();
        }

        public void Worker()
        {
            // Ensure we're starting on a proper second.
            while (DateTime.Now.Second != 0) Thread.Sleep(100);
            while(true)
            {
                foreach(var task in tasks)
                {
                    if (task.IsDue()) task.Execute();
                }
                Thread.Sleep(60 * 1000);
            }
        }

        public void SetAtomicDriver(IAtomicDriver driver)
        {
            this.driver = driver;
        }

        public IAtomicDriver GetAtomicDriver() => driver;
    }
}
