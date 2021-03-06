using System;
using System.Threading;

namespace HotChocolate.Execution.Utilities
{
    internal class TaskStatistics : ITaskStatistics
    {
        private readonly object _sync = new object();
        private readonly EventArgs _empty = EventArgs.Empty;

        private int _allTasks;
        private int _newTasks;
        private int _completedTasks;

        public event EventHandler<EventArgs>? StateChanged;

        public event EventHandler<EventArgs>? AllTasksCompleted;

        public int AllTasks => _allTasks;

        public int NewTasks => _newTasks;

        public int RunningTasks => _allTasks - _completedTasks - _newTasks;

        public int CompletedTasks => _completedTasks;

        public bool IsCompleted { get; private set; }

        public void TaskCreated()
        {
            Interlocked.Increment(ref _allTasks);
            Interlocked.Increment(ref _newTasks);
            StateChanged?.Invoke(this, _empty);
        }

        public void TaskStarted()
        {
            Interlocked.Decrement(ref _newTasks);
            StateChanged?.Invoke(this, _empty);
        }

        public void TaskCompleted()
        {
            int allTasks = _allTasks;
            int completedTasks = Interlocked.Increment(ref _completedTasks);
            if (allTasks == completedTasks)
            {
                IsCompleted = true;
                AllTasksCompleted?.Invoke(this, _empty);
            }
        }

        public void Reset()
        {
            _newTasks = 0;
            _allTasks = 0;
            _completedTasks = 0;
            IsCompleted = false;
        }
    }
}
