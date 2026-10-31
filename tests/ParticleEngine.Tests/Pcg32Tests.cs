using ParticleEngine.Core;
using Xunit;

namespace ParticleEngine.Tests
{
    public class Pcg32Tests
    {
        // Pinned from bench --emit-goldens on net8.0. Because PCG32 is pure unsigned-integer
        // arithmetic, these are bit-exact on every runtime; the net48 run of this test is the
        // cross-runtime determinism check (spec 11.2, R8).
        private static readonly uint[] First8 =
        {
            2707161783, 2068313097, 3122475824, 2211639955,
            3215226955, 3421331566, 3217466285, 2167406445,
        };

        private const uint XorAfter1000 = 1454914926;

        [Fact]
        public void GoldenStream_ForSeed42Seq54()
        {
            var rng = new Pcg32(42, 54);
            for (int i = 0; i < First8.Length; i++)
                Assert.Equal(First8[i], rng.NextUInt());

            uint checksum = 0;
            for (int i = 0; i < 1000; i++) checksum ^= rng.NextUInt();
            Assert.Equal(XorAfter1000, checksum);
        }

        [Fact]
        public void SameSeedAndStream_ProduceIdenticalSequences()
        {
            var a = new Pcg32(12345, 1);
            var b = new Pcg32(12345, 1);
            for (int i = 0; i < 1000; i++)
                Assert.Equal(a.NextUInt(), b.NextUInt());
        }

        [Fact]
        public void DifferentStreams_Diverge()
        {
            var a = new Pcg32(12345, 1);
            var b = new Pcg32(12345, 2);
            bool anyDifferent = false;
            for (int i = 0; i < 100; i++)
                if (a.NextUInt() != b.NextUInt()) { anyDifferent = true; break; }
            Assert.True(anyDifferent);
        }

        [Fact]
        public void NextDouble_StaysInUnitInterval()
        {
            var rng = new Pcg32(7, 7);
            for (int i = 0; i < 100000; i++)
            {
                double d = rng.NextDouble();
                Assert.True(d >= 0.0 && d < 1.0, $"out of range: {d}");
            }
        }

        [Fact]
        public void NextDoubleRange_StaysWithinBounds()
        {
            var rng = new Pcg32(7, 7);
            for (int i = 0; i < 100000; i++)
            {
                double d = rng.NextDouble(-3.5, 10.0);
                Assert.True(d >= -3.5 && d < 10.0, $"out of range: {d}");
            }
        }

        [Fact]
        public void NextUIntBounded_StaysBelowBoundAndCoversRange()
        {
            var rng = new Pcg32(99, 3);
            const uint bound = 6;
            var seen = new bool[bound];
            for (int i = 0; i < 100000; i++)
            {
                uint r = rng.NextUInt(bound);
                Assert.True(r < bound);
                seen[r] = true;
            }
            for (uint k = 0; k < bound; k++) Assert.True(seen[k], $"value {k} never drawn");
        }
    }
}
