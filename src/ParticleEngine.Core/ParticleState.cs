using System;

namespace ParticleEngine.Core
{
    /// <summary>
    /// Struct-of-arrays simulation state (spec 4.3, 9.3). Double precision throughout:
    /// CAD coordinates can be large and float positions lose precision far from the origin.
    ///
    /// Two sets of position/velocity buffers are kept. A frame reads the <c>front</c> buffers
    /// (previous frame) and writes the <c>back</c> buffers, then <see cref="SwapBuffers"/> makes
    /// the new state current. This makes the per-particle update order-independent and safe to
    /// parallelise (spec 7.1).
    ///
    /// Arrays are grown by doubling (never per-particle objects, never shrunk mid-run), so the
    /// steady-state hot path allocates nothing. Growth happens only when the agent count rises
    /// (child spawning), at a frame boundary.
    /// </summary>
    public sealed class ParticleState
    {
        // Front buffers: the current, readable frame.
        public double[] Px = Array.Empty<double>();
        public double[] Py = Array.Empty<double>();
        public double[] Pz = Array.Empty<double>();
        public double[] Vx = Array.Empty<double>();
        public double[] Vy = Array.Empty<double>();
        public double[] Vz = Array.Empty<double>();

        // Back buffers: written during a step, swapped in afterwards.
        public double[] Qx = Array.Empty<double>();
        public double[] Qy = Array.Empty<double>();
        public double[] Qz = Array.Empty<double>();
        public double[] Ux = Array.Empty<double>();
        public double[] Uy = Array.Empty<double>();
        public double[] Uz = Array.Empty<double>();

        public int[] Parent = Array.Empty<int>();     // -1 for originals (spec 4.3)
        public int[] ChildCount = Array.Empty<int>();

        /// <summary>Number of live agents.</summary>
        public int Count { get; private set; }

        /// <summary>Allocated slots; always &gt;= <see cref="Count"/>.</summary>
        public int Capacity => Px.Length;

        /// <summary>
        /// Reset to <paramref name="count"/> agents, allocating if needed. Parents default to -1
        /// (originals). Existing array contents are not cleared beyond what is reused; callers seed
        /// positions/velocities after this. Used on initialise and on Reset.
        /// </summary>
        public void Reset(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            EnsureCapacity(count);
            Count = count;
            for (int i = 0; i < count; i++)
            {
                Parent[i] = -1;
                ChildCount[i] = 0;
            }
        }

        /// <summary>
        /// Grow all arrays so at least <paramref name="required"/> slots exist, doubling to amortise
        /// repeated growth. Existing data in [0, Count) is preserved. No-op when capacity already fits.
        /// </summary>
        public void EnsureCapacity(int required)
        {
            if (required <= Capacity) return;

            int newCap = Capacity == 0 ? 16 : Capacity;
            while (newCap < required) newCap *= 2;

            GrowDouble(ref Px, newCap);
            GrowDouble(ref Py, newCap);
            GrowDouble(ref Pz, newCap);
            GrowDouble(ref Vx, newCap);
            GrowDouble(ref Vy, newCap);
            GrowDouble(ref Vz, newCap);

            GrowDouble(ref Qx, newCap);
            GrowDouble(ref Qy, newCap);
            GrowDouble(ref Qz, newCap);
            GrowDouble(ref Ux, newCap);
            GrowDouble(ref Uy, newCap);
            GrowDouble(ref Uz, newCap);

            GrowInt(ref Parent, newCap);
            GrowInt(ref ChildCount, newCap);
        }

        /// <summary>
        /// Append one agent (a spawned child, spec 7.7) at the given position and velocity and return
        /// its index. Grows the arrays if full.
        /// </summary>
        public int Add(double px, double py, double pz, double vx, double vy, double vz, int parent)
        {
            EnsureCapacity(Count + 1);
            int i = Count;
            Px[i] = px; Py[i] = py; Pz[i] = pz;
            Vx[i] = vx; Vy[i] = vy; Vz[i] = vz;
            Parent[i] = parent;
            ChildCount[i] = 0;
            Count = i + 1;
            return i;
        }

        /// <summary>
        /// Make the freshly written back buffers current. Called once per frame after every agent has
        /// been written (spec 7.1 step 6). Reference swap only; no copying.
        /// </summary>
        public void SwapBuffers()
        {
            Swap(ref Px, ref Qx);
            Swap(ref Py, ref Qy);
            Swap(ref Pz, ref Qz);
            Swap(ref Vx, ref Ux);
            Swap(ref Vy, ref Uy);
            Swap(ref Vz, ref Uz);
        }

        private static void GrowDouble(ref double[] array, int newCap)
        {
            var next = new double[newCap];
            Array.Copy(array, next, array.Length);
            array = next;
        }

        private static void GrowInt(ref int[] array, int newCap)
        {
            var next = new int[newCap];
            Array.Copy(array, next, array.Length);
            array = next;
        }

        private static void Swap(ref double[] a, ref double[] b)
        {
            var t = a; a = b; b = t;
        }
    }
}
