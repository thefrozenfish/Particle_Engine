using System;
using System.Diagnostics;
using System.Globalization;
using ParticleEngine.Core;

namespace ParticleEngine.Bench
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--emit-goldens")
            {
                EmitPrngGoldens();
                return 0;
            }

            int timed = 120;   // measured steps for timing percentiles
            int allocSteps = 60; // measured steps for allocation-per-step

            Console.WriteLine("ParticleEngine.Bench — spec 9.1 scenarios");
            Console.WriteLine("Runtime: " + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
                              + " / " + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture);
            Console.WriteLine();
            Console.WriteLine("| Scenario | Target | median ms | p95 ms | alloc B/step |");
            Console.WriteLine("| --- | --- | --- | --- | --- |");

            foreach (Scenario sc in Scenarios.All())
            {
                CreeperSimulation sim = sc.Build();
                for (int i = 0; i < sc.Warm; i++) sim.Step();

                // Timing: one Step per sample, into a preallocated buffer (no harness allocation here).
                var ms = new double[timed];
                var sw = new Stopwatch();
                for (int i = 0; i < timed; i++)
                {
                    sw.Restart();
                    sim.Step();
                    sw.Stop();
                    ms[i] = sw.Elapsed.TotalMilliseconds;
                }
                Array.Sort(ms);
                double median = ms[timed / 2];
                double p95 = ms[(int)(timed * 0.95)];

                // Allocation per step: measured in a clean pass with nothing but Step() between reads.
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < allocSteps; i++) sim.Step();
                long after = GC.GetAllocatedBytesForCurrentThread();
                long perStep = (after - before) / allocSteps;

                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "| {0} | {1} | {2:0.000} | {3:0.000} | {4} |",
                    sc.Name, sc.Target, median, p95, perStep));
            }

            Console.WriteLine();
            Console.WriteLine("Acceptance (spec 9.5): steady-state Core allocation per step is zero (+/- 1 KB).");
            Console.WriteLine("Numbers are PROVISIONAL until the Culebra probes lock the behaviour (per owner).");
            return 0;
        }

        // Prints the first draws and a checksum for a fixed seed/stream, so the exact numbers
        // can be pinned in the determinism test (spec 11.2). Integer output, so identical on
        // every runtime.
        private static void EmitPrngGoldens()
        {
            const ulong seed = 42UL;
            const ulong seq = 54UL;
            var rng = new Pcg32(seed, seq);

            Console.WriteLine($"seed={seed} seq={seq}");
            Console.Write("first8=");
            for (int i = 0; i < 8; i++)
            {
                if (i > 0) Console.Write(", ");
                Console.Write(rng.NextUInt().ToString(CultureInfo.InvariantCulture));
            }
            Console.WriteLine();

            uint checksum = 0;
            for (int i = 0; i < 1000; i++) checksum ^= rng.NextUInt();
            Console.WriteLine($"xor_after_1000={checksum}");
        }
    }
}
