﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Viki.LoadRunner.Engine.Executor.Result;
using Viki.LoadRunner.Engine.Executor.Timer;
#pragma warning disable 1591

namespace Viki.LoadRunner.Engine.Executor.Threads
{
    public class ThreadCoordinator : IDisposable
    {
        #region Fields

        private readonly Type _testScenarioType;
        private readonly ITimer _executionTimer;

        private readonly ConcurrentDictionary<int, TestExecutorThread> _allThreads;
        private readonly ConcurrentDictionary<int, TestExecutorThread> _initializedThreads;
        private readonly ConcurrentQueue<TestExecutorThread> _idleThreads;
        private readonly ConcurrentBag<Exception> _threadErrors;

        private bool _disposing;
        private int _nextIterationId;
        private int _nextThreadId;

        #endregion

        #region Properties

        public object InitialUserData = null;

        #endregion

        #region Ctor

        public ThreadCoordinator(Type testScenarioType, ITimer executionTimer)
        {
            if (testScenarioType == null)
                throw new ArgumentNullException(nameof(testScenarioType));
            if (executionTimer == null)
                throw new ArgumentNullException(nameof(executionTimer));

            _allThreads = new ConcurrentDictionary<int, TestExecutorThread>();
            _initializedThreads = new ConcurrentDictionary<int, TestExecutorThread>();
            _idleThreads = new ConcurrentQueue<TestExecutorThread>();
            _threadErrors = new ConcurrentBag<Exception>();

            _testScenarioType = testScenarioType;
            _executionTimer = executionTimer;

        }

        #endregion

        #region Methods

        public bool TryEnqueueSingleIteration()
        {
            bool result = false;
            TestExecutorThread thread = TryDequeueFreeThread();

            if (thread != null)
            {
                thread.QueueIteration(_nextIterationId++);
                result = true;
            }

            return result;
        }

        public void StopWorkersAsync(int threadCount)
        {
            for (int i = 0; i < threadCount; i++)
            {
                TryRemoveThread(_allThreads.Keys.First());
            }
        }

        private TestExecutorThread TryDequeueFreeThread()
        {
            TestExecutorThread result;
            _idleThreads.TryDequeue(out result);
            if (result?.QueuedToStop == true)
                return null;

            return result;
        }

        public void InitializeThreads(int threadCount)
        {
            InitializeThreadsAsync(threadCount);

            while (_allThreads.Any(t => t.Value.IsAlive && t.Value.ScenarioInitialized == false))
            {
                Thread.Sleep(100);
            }
        }

        public void InitializeThreadsAsync(int threadCount)
        {
            IEnumerable<TestExecutorThread> newThreads = CreateThreads(threadCount);

            foreach (TestExecutorThread newThread in newThreads)
            {
                newThread.ScenarioIterationFinished += ExecutorThreadScenarioIterationFinished;
                newThread.ThreadFailed += ExecutorThread_ThreadFailed;
                newThread.ScenarioSetupSucceeded += NewThread_ScenarioSetupSucceeded;

                newThread.StartThread();

                _allThreads.TryAdd(newThread.ThreadId, newThread);
            }
        }

        public WorkerThreadStats BuildWorkerThreadStats() => new WorkerThreadStats(_allThreads.Count, _initializedThreads.Count, _idleThreads.Count);

        private IEnumerable<TestExecutorThread> CreateThreads(int threadCount)
        {
            for (int i = 0; i < threadCount; i++)
            {
                var testScenarioInstance = (ILoadTestScenario)Activator.CreateInstance(_testScenarioType);
                yield return new TestExecutorThread(testScenarioInstance, _executionTimer, _nextThreadId++, InitialUserData);
            }
        }

        public void AssertThreadErrors()
        {
            if (_threadErrors.Count != 0)
            {
                Exception resultError;
                _threadErrors.TryTake(out resultError);

                if (resultError != null)
                    throw resultError;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _disposing = true;

            foreach (TestExecutorThread testExecutorThread in _allThreads.Values)
            {
                testExecutorThread.Dispose();
            }
        }

        public void StopAndDispose(int timeoutMilliseconds)
        {
            DateTime timeoutThreshold = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);

            foreach (TestExecutorThread testExecutorThread in _allThreads.Values)
            {
                testExecutorThread.QueueStopThreadAsync();
            }

            foreach (TestExecutorThread testExecutorThread in _allThreads.Values)
            {
                int timeleftTillTimeout = (int) (timeoutThreshold - DateTime.UtcNow).TotalMilliseconds;
                if (timeleftTillTimeout < 0)
                    timeleftTillTimeout = 0;

                testExecutorThread.StopThread(timeleftTillTimeout);
            }

            Dispose();
        }

        #endregion

        #region Events

        private void NewThread_ScenarioSetupSucceeded(TestExecutorThread sender)
        {
            if (!_disposing && !sender.QueuedToStop)
            {
                _idleThreads.Enqueue(sender);
                _initializedThreads.TryAdd(sender.ThreadId, sender);
            }
        }

        private void ExecutorThreadScenarioIterationFinished(TestExecutorThread sender, IterationResult result)
        {
            if (!_disposing)
            {
                result.CreatedThreads = _allThreads.Count;
                result.WorkingThreads = _initializedThreads.Count - _idleThreads.Count;

                OnScenarioExecutionFinished(result);

                if (!sender.QueuedToStop)
                    _idleThreads.Enqueue(sender);
            }
        }

        private void ExecutorThread_ThreadFailed(TestExecutorThread sender, IterationResult result, Exception ex)
        {
            if (!_disposing)
            {
                TryRemoveThread(sender.ThreadId);

                _threadErrors.Add(ex);
            }
        }

        public delegate void ScenarioExecutionFinishedEventHandler(IterationResult result);
        public event ScenarioExecutionFinishedEventHandler ScenarioIterationFinished;

        private void OnScenarioExecutionFinished(IterationResult result)
        {
            ScenarioIterationFinished?.Invoke(result);
        }

        private void TryRemoveThread(int threadId)
        {
            TestExecutorThread removedThread;

            _allThreads.TryRemove(threadId, out removedThread);

            removedThread.QueueStopThreadAsync();

            _initializedThreads.TryRemove(threadId, out removedThread);
        }

        #endregion
    }

    public struct WorkerThreadStats
    {
        private readonly int _createdThreadCount;
        private readonly int _initializedTheadCount;
        private readonly int _readyThreadCount;

        public int CreatedThreadCount => _createdThreadCount;
        public int WorkingThreadCount => _initializedTheadCount - _readyThreadCount;
        public int InitializedThreadCount => _initializedTheadCount;
        public int ReadyThreadCount => _readyThreadCount;

        public WorkerThreadStats(int createdThreadCount, int initializedTheadCount, int readyThreadCount)
        {
            _createdThreadCount = createdThreadCount;
            _initializedTheadCount = initializedTheadCount;
            _readyThreadCount = readyThreadCount;
        }
    }
}