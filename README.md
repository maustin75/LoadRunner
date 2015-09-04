### Load/Stress test library for executing tests written in c# ###
* NuGet: `Install-Package Viki.LoadRunner`

## *Quick Intro*

### *ILoadTestScenario*
Implement `ILoadTestScenario` interface by defining test scenario for single thread instance.
Each worker-thread will create its own `ILoadTestScenario` instance and will keep it persistent until the test is over.
```cs
public class TestScenario : ILoadTestScenario
{
    private static readonly Random Random = new Random(42);

    public void ScenarioSetup(ITestContext testContext)
    {
        Debug.WriteLine("ScenarioSetup Executes on thread creation");
        Debug.WriteLine("Exceptions here are not handled!");
    }

    public void IterationSetup(ITestContext testContext)
    {
        Debug.WriteLine("IterationSetup is executed before each ExecuteScenario call");

        if (Random.Next(100) % 50 == 0)
            throw new Exception("2% error chance for testing");
    }

    public void ExecuteScenario(ITestContext testContext)
    {
        Debug.WriteLine(
            "ExecuteScenario defines single iteration for load test scenario, " +
            "It is called after each successful IterationSetup call. " +
            "Execution time is measured only for this function" +
            "You can use testContext.Checkpoint() function to mark points, "+
            "where time should be measured"
        );
        Thread.Sleep(Random.Next(5000));

        testContext.Checkpoint("First Checkpoint");

        if (Random.Next(100) % 10 == 0)
            throw new Exception("10% error chance for testing");

        testContext.Checkpoint("Last Checkpoint");
        Thread.Sleep(Random.Next(1000));

        if (Random.Next(100) % 100 == 0)
            throw new Exception("1% error chance for testing");
    }

    public void IterationTearDown(ITestContext testContext)
    {
        Debug.WriteLine("IterationTearDown is executed each time after ExecuteScenario iteration is finished");
        Debug.WriteLine("unless thread is terminated by finishTimeoutMilliseconds abort");

        if (Random.Next(100) % 25 == 0)
            throw new Exception("4% error chance for testing");
    }

    public void ScenarioTearDown(ITestContext testContext)
    {
        Debug.WriteLine("ScenarioTearDown Executes once LoadTest execution is over");
        Debug.WriteLine("(unless thread is terminated by finishTimeoutMilliseconds abort");

        Debug.WriteLine("Exceptions here are not handled!");
    }
}
```
### *Configure Load-test runner parameters*
```cs
ExecutionParameters executionParameters = new ExecutionParameters(

    // Maximum LoadTest duration (after it load test stops)
    maxDuration: TimeSpan.FromSeconds(15),

    // Maximum iteratations count (after it load test stops)
    maxIterationsCount: 2000,

    // Minimum amount of worker-threads to precreate
    minThreads: 20,

    // Maximum amount of worker-threads that can be created 
    // New threads only be created if maxRequestsPerSecond speed is not achieved
    // with current amount of threads          
    maxThreads: 200,

    // Maximum count of requests that will be created per second
    // (Unless there are no available worker-threads left)
    maxRequestsPerSecond: Double.MaxValue,

    // Once LoadTest execution finishes because of maxDuration or maxIterationsCount limit
    // Coordinating thread will wait finishTimeoutMilliseconds amount of time before 
    // terminating them with Thread.Abort()
    finishTimeoutMilliseconds: 10000
);
```

### *Choose results aggregator*
```cs
  // This aggregation is similar to SoapUI (Like Min, Max, Avg, ...)
  DefaultResultsAggregator resultsAggregator = new DefaultResultsAggregator();
  // This one aggregates same results as DefaultResultsAggregator, but splits into time-based histogram
  HistogramResultsAggregator histogramResultsAggregator = new HistogramResultsAggregator(aggregationStepSeconds: 3);
```

Or `IResultsAggregator` interface could be implemented, thus giving access to raw measurements
```cs
    public interface IResultsAggregator
    {
        /// <summary>
        /// Results from all running threads will be poured into this one.
        /// </summary>
        void TestContextResultReceived(TestContextResult result);
    }
```

### *Put it all together*

```cs
    ExecutionParameters executionParameters = new ExecutionParameters(
        maxDuration: TimeSpan.FromSeconds(15),
        maxIterationsCount: 2000,
        minThreads: 20,
        maxThreads: 200,
        maxRequestsPerSecond: Double.MaxValue,
        finishTimeoutMilliseconds: 10000
    );

    DefaultResultsAggregator resultsAggregator = new DefaultResultsAggregator();

    // Initializing LoadTest Client
    LoadRunnerEngine loadRunner = LoadRunnerEngine.Create<TestScenario>(executionParameters, resultsAggregator);

    // Run test (blocking call)
    loadRunner.Run();

    // ResultItem will have all logged exceptions within LoadTest execution
    ResultsContainer defaultResults = resultsAggregator.GetResults();
    Console.WriteLine(JsonConvert.SerializeObject(defaultResults, Formatting.Indented));

    Console.ReadKey();
```
## *Enjoy the result*

```js
{
  "Results": [
    {
      "Name": "First Checkpoint",
      "MomentMin": "00:00:00.0020296",
      "MomentMax": "00:00:04.9959708",
      "MomentAverage": "00:00:02.4800000",
      "SummedMin": "00:00:00.0020296",
      "SummedMax": "00:00:04.9959708",
      "SummedAverage": "00:00:02.4800000",
      "SuccessIterationsPerSec": 56.574550654270006,
      "Count": 1138,
      "ErrorRatio": 0.0,
      "ErrorCount": 0
    },
    {
      "Name": "Last Checkpoint",
      "MomentMin": "00:00:00.0000003",
      "MomentMax": "00:00:00.0000804",
      "MomentAverage": "00:00:00",
      "SummedMin": "00:00:00.0020317",
      "SummedMax": "00:00:04.9959717",
      "SummedAverage": "00:00:02.4880000",
      "SuccessIterationsPerSec": 51.056294834741038,
      "Count": 1027,
      "ErrorRatio": 0.097539543057996475,
      "ErrorCount": 111
    },
    {
      "Name": "SYS_ITERATION_END",
      "MomentMin": "00:00:00.0000181",
      "MomentMax": "00:00:00.9997012",
      "MomentAverage": "00:00:00.4820000",
      "SummedMin": "00:00:00.0799777",
      "SummedMax": "00:00:05.8661616",
      "SummedAverage": "00:00:02.9660000",
      "SuccessIterationsPerSec": 50.310584588858745,
      "Count": 1012,
      "ErrorRatio": 0.014605647517039922,
      "ErrorCount": 15
    }
  ],
  "Totals": {
    "TotalIterationsStartedCount": 1167,
    "TotalSuccessfulIterationCount": 959,
    "TotalFailedIterationCount": 208,
    "TotalDuration": "00:00:20.1150515",
    "IterationErrorsRatio": 0.17823479005998288,
    "SuccessIterationsPerSec": 47.67574172007464,
    "IterationSetupErrorCount": 29,
    "IterationTearDownErrorCount": 53
  }
}
```
