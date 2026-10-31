using System;

namespace ParticleEngine.Core
{
    /// <summary>
    /// Deterministic PRNG for the simulation (spec 7.10, R8). PCG32 (O'Neill 2014),
    /// the minimal single-stream variant. All state and output are produced with
    /// unsigned-integer arithmetic only, so the stream is bit-identical on .NET
    /// Framework, Mono and modern .NET — unlike <see cref="System.Random"/>, whose
    /// sequence is not guaranteed stable across runtimes.
    ///
    /// Seed and stream select a reproducible sequence; derive a per-particle stream
    /// from (seed, particleIndex) so parallel execution does not change results.
    /// This type is a mutable struct: copy it to fork a stream.
    /// </summary>
    public struct Pcg32
    {
        private const ulong Multiplier = 6364136223846793005UL;
        private const ulong DefaultIncrement = 1442695040888963407UL;

        private ulong _state;
        private ulong _inc; // controls the stream; always forced odd.

        /// <summary>Create a generator seeded by <paramref name="seed"/> on stream <paramref name="sequence"/>.</summary>
        public Pcg32(ulong seed, ulong sequence = 0)
        {
            _state = 0UL;
            _inc = (sequence << 1) | 1UL;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        /// <summary>Next 32-bit value, uniform over the whole uint range.</summary>
        public uint NextUInt()
        {
            ulong oldState = _state;
            _state = unchecked(oldState * Multiplier + _inc);
            uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            int rot = (int)(oldState >> 59);
            // Rotate right by rot (branchless; (-rot) & 31 is the complementary shift).
            return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
        }

        /// <summary>Uniform integer in [0, exclusiveBound) with no modulo bias (rejection sampling).</summary>
        public uint NextUInt(uint exclusiveBound)
        {
            if (exclusiveBound == 0) throw new ArgumentOutOfRangeException(nameof(exclusiveBound));
            // Reject the low region so the remaining range divides evenly.
            uint threshold = (uint)((0x100000000UL - exclusiveBound) % exclusiveBound);
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold) return r % exclusiveBound;
            }
        }

        /// <summary>Uniform double in [0, 1). 53-bit mantissa resolution.</summary>
        public double NextDouble()
        {
            // Take 53 bits from two draws; divide by 2^53. Integer-exact, so the mantissa
            // pattern is identical across runtimes; only the final divide is IEEE double.
            ulong hi = NextUInt();
            ulong lo = NextUInt();
            ulong bits = ((hi << 21) ^ (lo >> 11)) & ((1UL << 53) - 1UL);
            return bits * (1.0 / 9007199254740992.0); // 2^53
        }

        /// <summary>Uniform double in [min, max).</summary>
        public double NextDouble(double min, double max)
        {
            return min + (max - min) * NextDouble();
        }
    }
}
