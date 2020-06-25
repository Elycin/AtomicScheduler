using AtomicScheduler.Drivers;
using AtomicScheduler.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AtomicScheduler
{
    class ScheduledTask : IScheduledTask
    {
        /// <summary>
        /// Task state struct.
        /// </summary>
        struct TaskState
        {
            public string hostnameRunning;
            public bool running;
        }

        /// <summary>
        /// Determine weither the task should look if it is already being executed on another server.
        /// This is otherwise known as atomically locking.
        /// </summary>
        private bool oneMachineOnly = false;

        /// <summary>
        /// Determine if we should prevent the cron from overlapping if the previous execution has not exited.
        /// This will utilize a lock.
        /// </summary>
        private bool allowOverlapping = false;

        /// <summary>
        /// Generic C# lock handling to prevent re-running of exceptions.
        /// We'll only use this if allowOverlapping is true.
        /// </summary>
        private object executionLock;

        /// <summary>
        /// Determine if we should use UTC or the local timezone.
        /// </summary>
        private bool useUniversalTime = false;

        /// <summary>
        /// The interval that the process should run.
        /// </summary>
        private string cron;

        /// <summary>
        /// The thread object that will be reinstantiated and ran from.
        /// </summary>
        private Thread lastThread;

        /**
         Intervals list.
        */
        private List<int> minutes;
        private List<int> hours;
        private List<int> daysOfMonth;
        private List<int> months;
        private List<int> daysOfWeek;

        /// <summary>
        /// Reference to the scheduler that is running it so we can get drivers.
        /// </summary>
        private Scheduler hookedScheduler;

        /**
         Below are regexes that define different ways that cron can be expressed.
         */
        readonly static Regex everySoOftenExpression = new Regex(@"(\*/\d+)");
        readonly static Regex rangeExpression = new Regex(@"(\d+\-\d+)\/?(\d+)?");
        readonly static Regex wildcardExpression = new Regex(@"(\*)");
        readonly static Regex listExpression = new Regex(@"(((\d+,)*\d+)+)");
        readonly static Regex validationRegex = new Regex(everySoOftenExpression + "|" + rangeExpression + "|" + wildcardExpression + "|" + listExpression);

        /// <summary>
        /// The threaded task that should be executed.
        /// </summary>
        private ThreadStart task { get; set; }

        /// <summary>
        /// Way to uniquely identify the task.
        /// </summary>
        private string identifier;

        /// <summary>
        /// Construct a new scheduled task object.
        /// </summary>
        /// <param name="cron">String interval, eg: * * * * *</param>
        /// <param name="threadStart">ThreadStart Object</param>
        /// <param name="allowOverlapping">Boolean representing if the process should run again if it is laready running.</param>
        /// <param name="oneMachineOnly">Boolean representing if the process should run on a single machine for each execution.</param>
        /// <param name="universalTime">When set to true, this will operate on UTC.</param>
        public ScheduledTask(string cron, ThreadStart threadStart, bool allowOverlapping = false, bool oneMachineOnly = false, bool universalTime = false, string identifier = null)
        {
            this.cron = cron;
            this.task = threadStart;
            this.allowOverlapping = allowOverlapping;
            this.oneMachineOnly = oneMachineOnly;
            this.identifier = identifier;
        }


        /// <summary>
        /// Set the atomic state.
        /// Setting this to true will ensure that the task is already not running on another machine.
        /// </summary>
        /// <param name="state">If set to true, the process will only be ran on a single machine.</param>
        public void SetAtomicState(bool state)
        {
            this.oneMachineOnly = state;
        }

        public void SetOverlappingState(bool state)
        {
            this.allowOverlapping = state;
        }

        public bool IsDue()
        {
            // Determine the current time based on settings.
            var currentTime = useUniversalTime ? DateTime.UtcNow : DateTime.Now;

            // Check if expression matches.
            return minutes.Contains(currentTime.Minute) &&
                   hours.Contains(currentTime.Hour) &&
                   daysOfMonth.Contains(currentTime.Day) &&
                   months.Contains(currentTime.Month) &&
                   daysOfWeek.Contains((int)currentTime.DayOfWeek);
        }

        /// <summary>
        /// Execute the thread.
        /// </summary>
        public void Execute()
        {
            if (!allowOverlapping)
            {
                // Overlapping is disabled, we should make use of locks.
                lock(executionLock)
                {
                    if (oneMachineOnly)
                    {
                        if (!IsAtomicallyLocked())
                        {
                            MarkAtomicallyLocked();
                            ExecuteInternal();
                        }
                    } else
                    {
                        ExecuteInternal();
                    }
                }
            } else
            {
                if (oneMachineOnly)
                {
                    if (!IsAtomicallyLocked())
                    {
                        MarkAtomicallyLocked();
                        ExecuteInternal();
                    }
                }
                else
                {
                    ExecuteInternal();
                }
            }
        }
        
        /// <summary>
        /// The method that will actually run the thread so we don't repeat ourselves.
        /// </summary>
         private void ExecuteInternal()
        {
            lastThread = new Thread(task);
            lastThread.Start();
        }

        /// <summary>
        /// Passthrough method to determine weither a thread is running.
        /// Will throw an ThreadNotinstantiatedException if the task has not ran yet.
        /// </summary>
        public bool IsRunning()
        {
            if (lastThread == null) throw new ThreadNotInstantiatedException("The task has yet to run, a thread has not been created yet.");
            return lastThread.IsAlive;
        }

        /// <summary>
        /// Abort the last running thread.
        /// </summary>
        public void Abort()
        {
            if (lastThread == null) throw new ThreadNotInstantiatedException("The task has yet to run, a thread has not been created yet.");
            if (!lastThread.IsAlive) throw new ThreadNotRunningException("The thread has already completed execution, thus cannot be aborted.");
        }

        // Validate that the cron we have is valid.
        public static bool ValidCronExpression(string cronExpression)
        {
            MatchCollection matches = validationRegex.Matches(cronExpression);
            return matches.Count > 0;
        }

        public void SetIdentifier(string indentifier)
        {

        }
        public void MarkAtomicallyLocked()
        {
            GetAtomicDriver().Set(identifier);
        }

        public bool IsAtomicallyLocked()
        {
            return GetAtomicDriver().Has(identifier);
        }

        private IAtomicDriver GetAtomicDriver()
        {
            var driver = hookedScheduler;
            if (driver == null) throw new AtomicLockMissingConnection("The atomic driver connection was undeclared. Atomic locks cannot be used if there is no medium to keep track of locks.");
            return hookedScheduler.GetAtomicDriver();
        }

        /// <summary>
        /// Reference the scheduler that will run this method, we'll grab drivers through it.
        /// This should be called when the scheduler adds the task.
        /// </summary>
        /// <param name="scheduler"></param>
        public void HookScheduler(Scheduler scheduler)
        {
            this.hookedScheduler = scheduler;
        }

    }
}
