using System.Collections.Generic;
using ParticleEngine.Core;
using Xunit;

namespace ParticleEngine.Tests
{
    public class TrailBufferTests
    {
        private static List<double> Read(TrailBuffer t, int i)
        {
            var r = new List<double>();
            for (int k = 0; k < t.Length(i); k++)
            {
                t.Get(i, k, out double x, out double y, out double z);
                r.Add(x);
            }
            return r;
        }

        [Fact]
        public void PushesReadBackOldestToNewest()
        {
            var t = new TrailBuffer();
            t.Reset(1, 4);
            for (int k = 1; k <= 3; k++) t.Push(0, k, 0, 0);
            Assert.Equal(new List<double> { 1, 2, 3 }, Read(t, 0));
        }

        [Fact]
        public void WrapsOverwritingOldestAtCapacity()
        {
            var t = new TrailBuffer();
            t.Reset(1, 3);
            for (int k = 1; k <= 5; k++) t.Push(0, k, 0, 0); // 1..5 into capacity 3
            Assert.Equal(3, t.Length(0));
            Assert.Equal(new List<double> { 3, 4, 5 }, Read(t, 0)); // oldest two dropped
        }

        [Fact]
        public void CapacityIsAtLeastTwo()
        {
            var t = new TrailBuffer();
            t.Reset(1, 1);
            Assert.Equal(2, t.Capacity);
        }

        [Fact]
        public void ClearEmptiesOneTrail()
        {
            var t = new TrailBuffer();
            t.Reset(2, 4);
            t.Push(0, 1, 0, 0); t.Push(0, 2, 0, 0);
            t.Push(1, 9, 0, 0);
            t.Clear(0);
            Assert.Equal(0, t.Length(0));
            Assert.Equal(1, t.Length(1)); // other trail untouched
        }
    }
}
