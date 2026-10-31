using ParticleEngine.Core;
using Xunit;

namespace ParticleEngine.Tests
{
    public class ParticleStateTests
    {
        [Fact]
        public void Reset_SetsCountAndMarksOriginals()
        {
            var s = new ParticleState();
            s.Reset(10);

            Assert.Equal(10, s.Count);
            Assert.True(s.Capacity >= 10);
            for (int i = 0; i < s.Count; i++)
            {
                Assert.Equal(-1, s.Parent[i]);
                Assert.Equal(0, s.ChildCount[i]);
            }
        }

        [Fact]
        public void EnsureCapacity_GrowsByDoublingAndPreservesData()
        {
            var s = new ParticleState();
            s.Reset(4);
            for (int i = 0; i < 4; i++) { s.Px[i] = i; s.Vy[i] = -i; }

            int capBefore = s.Capacity;
            s.EnsureCapacity(capBefore + 1);

            Assert.True(s.Capacity >= capBefore + 1);
            // Doubling growth (powers of two from the initial 16).
            Assert.Equal(capBefore * 2, s.Capacity);
            // Existing data survived the grow.
            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(i, s.Px[i]);
                Assert.Equal(-i, s.Vy[i]);
            }
        }

        [Fact]
        public void EnsureCapacity_NoOpWhenAlreadyLargeEnough()
        {
            var s = new ParticleState();
            s.Reset(8);
            var pxRef = s.Px;
            s.EnsureCapacity(s.Capacity);
            Assert.Same(pxRef, s.Px); // no reallocation
        }

        [Fact]
        public void Add_AppendsAgentGrowingWhenFull()
        {
            var s = new ParticleState();
            s.Reset(0);
            int idx = s.Add(1, 2, 3, 4, 5, 6, parent: 7);

            Assert.Equal(0, idx);
            Assert.Equal(1, s.Count);
            Assert.Equal(1.0, s.Px[0]);
            Assert.Equal(6.0, s.Vz[0]);
            Assert.Equal(7, s.Parent[0]);
        }

        [Fact]
        public void SwapBuffers_ExchangesFrontAndBack()
        {
            var s = new ParticleState();
            s.Reset(3);
            // Write a recognisable value into the back buffer, then swap it to the front.
            var backRef = s.Qx;
            s.Qx[0] = 42.0;

            s.SwapBuffers();

            Assert.Same(backRef, s.Px); // back is now front
            Assert.Equal(42.0, s.Px[0]);
        }
    }
}
