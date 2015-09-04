﻿using System;
using System.Collections.Generic;
using System.Linq;
using Viki.LoadRunner.Engine.Aggregates.Aggregates;
using Viki.LoadRunner.Engine.Executor.Context;

namespace Viki.LoadRunner.Engine.Aggregates.Results
{
    public class ResultItemTotals
    {
        private readonly List<Exception> _iterationSetupErrors;
        private readonly List<Exception> _iterationTearDownErrors;


        public ResultItemTotals(DefaultTestContextResultAggregate results)
        {
            DefaultCheckpointAggregate setupRow = results.CheckpointAggregates[Checkpoint.IterationSetupCheckpointName];
            DefaultCheckpointAggregate tearDownRow = results.CheckpointAggregates[Checkpoint.IterationTearDownCheckpointName];

            TotalDuration = results.IterationEndTime - results.IterationBeginTime;

            TotalIterationsStartedCount = setupRow.Count;
            TotalFailedIterationCount = results.CheckpointAggregates.Sum(resultItemRow => resultItemRow.Value.Errors.Count);
            TotalSuccessfulIterationCount = TotalIterationsStartedCount - TotalFailedIterationCount;


            _iterationSetupErrors = setupRow.Errors;
            _iterationTearDownErrors = tearDownRow.Errors;
        }

        public readonly int TotalIterationsStartedCount;
        public readonly int TotalSuccessfulIterationCount;
        public readonly int TotalFailedIterationCount;

        public double IterationErrorsRatio => (1.0 / TotalIterationsStartedCount) * TotalFailedIterationCount;

        public double SuccessIterationsPerSec => TotalSuccessfulIterationCount / (TotalDuration.TotalMilliseconds / 1000.0);

        public readonly TimeSpan TotalDuration;

        public IReadOnlyList<Exception> GetIterationSetupErrors() => _iterationSetupErrors;
        public IReadOnlyList<Exception> GetIterationTearDownErrors() => _iterationTearDownErrors;

        public int IterationSetupErrorCount => _iterationSetupErrors.Count;
        public int IterationTearDownErrorCount => _iterationTearDownErrors.Count;
    }
}