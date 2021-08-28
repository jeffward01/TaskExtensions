﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JPenny.TaskExtensions.Tasks
{
    public abstract class PipelineTask
    {
        public bool Cancelled { get; private set; }

        public bool Completed { get; private set; }

        public bool Failed { get; private set; }

        public bool Started { get; private set; }

        public bool Succeeded { get; private set; }

        public ITaskResolver MainTaskResolver { get; set; }

        public ITaskResolver CancelledTaskResolver { get; set; }

        public ITaskResolver SuccessTaskResolver { get; set; }

        public ITaskResolver CompletedTaskResolver { get; set; }

        public IDictionary<Type, Action<Exception>> ExceptionHandlers { get; internal set; } = new Dictionary<Type, Action<Exception>>();

        protected async Task ExecuteAsync(Task task)
        {
            Started = true;
            try
            {
                await Pipeline.ExecuteAsync(task);
                Succeeded = true;

                await Pipeline.ExecuteAsync(SuccessTaskResolver);
            }
            catch (TaskCanceledException)
            {
                Cancelled = true;
                await Pipeline.ExecuteAsync(CancelledTaskResolver);
            }
            catch (AggregateException aggEx)
            {
                Failed = true;
                aggEx.Handle(HandleException);
            }
            catch (Exception ex)
            {
                Failed = true;
                HandleException(ex);
            }
            finally
            {
                Completed = true;
                await Pipeline.ExecuteAsync(CompletedTaskResolver);
            }
        }

        private bool HandleException(Exception ex)
        {
            var exType = ex.GetType();
            if (ExceptionHandlers.ContainsKey(exType))
            {
                var handler = ExceptionHandlers[exType];
                handler(ex);
                return true;
            }
            return false;
        }
    }
}
