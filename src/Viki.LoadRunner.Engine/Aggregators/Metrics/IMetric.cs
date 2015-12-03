﻿using System.Security.Cryptography.X509Certificates;
using Viki.LoadRunner.Engine.Executor.Context;

namespace Viki.LoadRunner.Engine.Aggregators.Metrics
{
    public interface IMetric
    {
        /// <summary>
        /// Creates new IMetric instance based on current instance settings
        /// </summary>
        /// <returns></returns>
        IMetric CreateNew();

        /// <summary>
        /// New iteration result received
        /// </summary>
        /// <param name="result">Iteration result</param>
        void Add(TestContextResult result);

        /// <summary>
        /// Names of columns produced by this metric (order must match [Values] order)
        /// </summary>
        string[] ColumnNames { get; }
        /// <summary>
        /// Values produced by this metric (order must match [ColumnNames] order)
        /// </summary>
        object[] Values { get; }
    }
}