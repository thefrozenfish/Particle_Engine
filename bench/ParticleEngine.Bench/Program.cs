using System;
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

            Console.WriteLine("ParticleEngine.Bench stub. No scenarios yet (added from M2 on).");
            Console.WriteLine("Run with --emit-goldens to print the PRNG golden block for the tests.");
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

            // XOR checksum of the first 1000 outputs (continuing the same stream).
            uint checksum = 0;
            for (int i = 0; i < 1000; i++) checksum ^= rng.NextUInt();
            Console.WriteLine($"xor_after_1000={checksum}");
        }
    }
}
