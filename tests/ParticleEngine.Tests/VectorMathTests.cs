using ParticleEngine.Core;
using Xunit;

namespace ParticleEngine.Tests
{
    public class VectorMathTests
    {
        private const double Tol = 1e-12;

        [Fact]
        public void LengthAndLengthSquared_MatchKnownTriple()
        {
            // 3-4-12 -> 13.
            Assert.Equal(169.0, VectorMath.LengthSquared(3, 4, 12), Tol);
            Assert.Equal(13.0, VectorMath.Length(3, 4, 12), Tol);
        }

        [Fact]
        public void DistanceSquared_IsSymmetricAndCorrect()
        {
            double d1 = VectorMath.DistanceSquared(1, 2, 3, 4, 6, 15);
            double d2 = VectorMath.DistanceSquared(4, 6, 15, 1, 2, 3);
            Assert.Equal(9.0 + 16.0 + 144.0, d1, Tol);
            Assert.Equal(d1, d2, Tol);
        }

        [Fact]
        public void ClampLength_LeavesShortVectorsUnchanged()
        {
            double x = 1, y = 2, z = 2; // length 3
            VectorMath.ClampLength(ref x, ref y, ref z, 5.0);
            Assert.Equal(1.0, x, Tol);
            Assert.Equal(2.0, y, Tol);
            Assert.Equal(2.0, z, Tol);
        }

        [Fact]
        public void ClampLength_ScalesLongVectorsToMax()
        {
            double x = 3, y = 0, z = 4; // length 5
            VectorMath.ClampLength(ref x, ref y, ref z, 2.5);
            Assert.Equal(2.5, VectorMath.Length(x, y, z), 1e-9);
            // Direction preserved: still along (3,0,4)/5.
            Assert.Equal(1.5, x, 1e-9);
            Assert.Equal(0.0, y, Tol);
            Assert.Equal(2.0, z, 1e-9);
        }

        [Fact]
        public void ClampLength_NonPositiveMaxZeroesVector()
        {
            double x = 3, y = 4, z = 0;
            VectorMath.ClampLength(ref x, ref y, ref z, 0.0);
            Assert.Equal(0.0, x, Tol);
            Assert.Equal(0.0, y, Tol);
            Assert.Equal(0.0, z, Tol);
        }

        [Fact]
        public void SetLength_RescalesToTarget()
        {
            double x = 0, y = 0, z = 7;
            VectorMath.SetLength(ref x, ref y, ref z, 3.0);
            Assert.Equal(0.0, x, Tol);
            Assert.Equal(0.0, y, Tol);
            Assert.Equal(3.0, z, 1e-9);
        }

        [Fact]
        public void SetLength_ZeroVectorStaysZero()
        {
            double x = 0, y = 0, z = 0;
            VectorMath.SetLength(ref x, ref y, ref z, 10.0);
            Assert.Equal(0.0, x, Tol);
            Assert.Equal(0.0, y, Tol);
            Assert.Equal(0.0, z, Tol);
        }

        [Fact]
        public void TryNormalize_ReturnsUnitVectorAndTrue()
        {
            double x = 0, y = 3, z = 4;
            bool ok = VectorMath.TryNormalize(ref x, ref y, ref z);
            Assert.True(ok);
            Assert.Equal(1.0, VectorMath.Length(x, y, z), 1e-9);
        }

        [Fact]
        public void TryNormalize_ZeroVectorReturnsFalse()
        {
            double x = 0, y = 0, z = 0;
            bool ok = VectorMath.TryNormalize(ref x, ref y, ref z);
            Assert.False(ok);
        }

        [Fact]
        public void Dot_IsCorrect()
        {
            Assert.Equal(1.0 * 4 + 2 * 5 + 3 * 6, VectorMath.Dot(1, 2, 3, 4, 5, 6), Tol);
        }
    }
}
