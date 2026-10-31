using System;

namespace ParticleEngine.Core
{
    /// <summary>
    /// Small, allocation-free 3D vector helpers on raw doubles (spec 4.3, 9.3, 9.4).
    /// Everything here is pure IEEE-double arithmetic with no trig, so it is safe in the
    /// per-frame hot path and reproducible (within tolerance) across runtimes.
    /// Operating on components rather than a struct keeps callers free to use the
    /// struct-of-arrays layout without boxing or copies.
    /// </summary>
    public static class VectorMath
    {
        public static double LengthSquared(double x, double y, double z) => x * x + y * y + z * z;

        public static double Length(double x, double y, double z) => Math.Sqrt(x * x + y * y + z * z);

        public static double DistanceSquared(double ax, double ay, double az, double bx, double by, double bz)
        {
            double dx = ax - bx, dy = ay - by, dz = az - bz;
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>
        /// Scale (x,y,z) so its length is at most <paramref name="maxLength"/>. Shorter vectors
        /// and the zero vector are returned unchanged. Uses a squared compare first so the
        /// square root is taken only when a clamp is actually needed (spec 9.4).
        /// </summary>
        public static void ClampLength(ref double x, ref double y, ref double z, double maxLength)
        {
            if (maxLength <= 0.0)
            {
                x = 0.0; y = 0.0; z = 0.0;
                return;
            }

            double lenSq = x * x + y * y + z * z;
            if (lenSq <= maxLength * maxLength) return;

            double scale = maxLength / Math.Sqrt(lenSq);
            x *= scale; y *= scale; z *= scale;
        }

        /// <summary>
        /// Rescale (x,y,z) to exactly <paramref name="length"/>. The zero vector stays zero
        /// (no direction to preserve).
        /// </summary>
        public static void SetLength(ref double x, ref double y, ref double z, double length)
        {
            double lenSq = x * x + y * y + z * z;
            if (lenSq <= 0.0)
            {
                x = 0.0; y = 0.0; z = 0.0;
                return;
            }

            double scale = length / Math.Sqrt(lenSq);
            x *= scale; y *= scale; z *= scale;
        }

        /// <summary>
        /// Normalise to unit length in place. Returns false and leaves the vector zeroed if it
        /// had no length, so callers can skip direction-dependent work.
        /// </summary>
        public static bool TryNormalize(ref double x, ref double y, ref double z)
        {
            double lenSq = x * x + y * y + z * z;
            if (lenSq <= 0.0)
            {
                x = 0.0; y = 0.0; z = 0.0;
                return false;
            }

            double invLen = 1.0 / Math.Sqrt(lenSq);
            x *= invLen; y *= invLen; z *= invLen;
            return true;
        }

        public static double Dot(double ax, double ay, double az, double bx, double by, double bz)
            => ax * bx + ay * by + az * bz;
    }
}
