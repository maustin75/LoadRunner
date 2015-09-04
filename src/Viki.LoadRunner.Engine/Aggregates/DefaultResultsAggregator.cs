﻿using System.Collections.Generic;
using System.Linq;
using Viki.LoadRunner.Engine.Aggregates.Aggregates;
using Viki.LoadRunner.Engine.Aggregates.Results;
using Viki.LoadRunner.Engine.Aggregates.Utils;
using Viki.LoadRunner.Engine.Executor.Context;

namespace Viki.LoadRunner.Engine.Aggregates
{
    public class DefaultResultsAggregator : IResultsAggregator
    {
        private readonly CheckpointOrderLearner _orderLearner = new CheckpointOrderLearner();
        private readonly DefaultTestContextResultAggregate _statsAggregator = new DefaultTestContextResultAggregate();


        public void TestContextResultReceived(TestContextResult result)
        {
            _orderLearner.Learn(result);
            _statsAggregator.AggregateResult(result);
        }
        
        public ResultsContainer GetResults()
        {
            ResultsMapper mapper = new ResultsMapper(_orderLearner);
            IEnumerable<ResultItemRow> resultRows = mapper.Map(_statsAggregator);
            return new ResultsContainer(resultRows.ToList(), new ResultItemTotals(_statsAggregator));
        }

        public void Begin()
        {
            _statsAggregator.Reset();
            _orderLearner.Reset();
        }

        public void End()
        {

        }
    }
}