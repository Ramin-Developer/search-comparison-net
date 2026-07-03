// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------

namespace BenchmarkSuite1;

internal sealed class Program
{
    private static void Main(string[] args)
    {
        // Assembly-wide discovery via BenchmarkSwitcher so the run_benchmark tooling can pass
        // --filter args to select specific benchmarks; still discovers every [Benchmark] type.
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
