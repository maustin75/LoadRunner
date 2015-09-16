﻿using System;
using Viki.LoadRunner.Engine.Strategies;
using Viki.LoadRunner.Engine.Strategies.Speed;
using Viki.LoadRunner.Engine.Strategies.Threading;

namespace Viki.LoadRunner.Engine.Parameters
{
    public class LoadRunnerParameters
    {
        public ExecutionLimits Limits = new ExecutionLimits();
        public IThreadingStrategy ThreadingStrategy = new SemiAutoThreading(10, 10);
        public ISpeedStrategy SpeedStrategy = new FixedSpeed(Double.MaxValue);
    }
}