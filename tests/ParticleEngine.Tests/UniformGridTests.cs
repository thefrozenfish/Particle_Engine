using System.Collections.Generic;
using ParticleEngine.Core;
using Xunit;

namespace ParticleEngine.Tests
{
    public class UniformGridTests
    {
        private static void Cloud(int n, double size, bool is2D, ulong seed,
            out double[] px, out double[] py, out double[] pz)
        {
            var rng = new Pcg32(seed);
            px = new double[n]; py = new double[n]; pz = new double[n];
            for (int i = 0; i < n; i++)
            {
                px[i] = rng.NextDouble(0, size);
                py[i] = rng.NextDouble(0, size);
                pz[i] = is2D ? 0.0 : rng.NextDouble(0, size);
            }
        }

        [Theory]
        [InlineData(true, 0.5)]
        [InlineData(true, 3.0)]
        [InlineData(true, 7.0)]
        [InlineData(false, 0.5)]
        [InlineData(false, 3.0)]
        [InlineData(false, 7.0)]
        public void CandidatesContainEveryBruteForceNeighbor(bool is2D, double radius)
        {
            const int n = 500;
            Cloud(n, 20.0, is2D, 777, out var px, out var py, out var pz);

            var grid = new UniformGrid();
            grid.Build(px, py, pz, n, radius, is2D);

            double r2 = radius * radius;
            var cand = new List<int>();
            for (int i = 0; i < n; i++)
            {
                grid.QueryNeighborCells(px[i], py[i], pz[i], cand);
                var set = new HashSet<int>(cand);
                for (int j = 0; j < n; j++)
                {
                    if (j == i) continue;
                    double d2 = VectorMath.DistanceSquared(px[i], py[i], pz[i], px[j], py[j], pz[j]);
                    if (d2 < r2)
                        Assert.Contains(j, set); // every true neighbour must be a grid candidate
                }
            }
        }

        [Fact]
        public void HandlesEmptyAndSingle()
        {
            var grid = new UniformGrid();
            grid.Build(System.Array.Empty<double>(), System.Array.Empty<double>(), System.Array.Empty<double>(), 0, 5, false);
            var cand = new List<int>();
            grid.QueryNeighborCells(0, 0, 0, cand);
            Assert.Empty(cand);

            grid.Build(new[] { 1.0 }, new[] { 2.0 }, new[] { 3.0 }, 1, 5, false);
            grid.QueryNeighborCells(1, 2, 3, cand);
            Assert.Single(cand);
            Assert.Equal(0, cand[0]);
        }

        [Fact]
        public void CoincidentPointsAllLandInOneCellAndAreFound()
        {
            int n = 50;
            var px = new double[n]; var py = new double[n]; var pz = new double[n];
            for (int i = 0; i < n; i++) { px[i] = 5; py[i] = 5; pz[i] = 5; }

            var grid = new UniformGrid();
            grid.Build(px, py, pz, n, 2.0, false);
            var cand = new List<int>();
            grid.QueryNeighborCells(5, 5, 5, cand);
            Assert.Equal(n, cand.Count); // all coincident points are candidates
        }
    }
}
