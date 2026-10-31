using System;

namespace ParticleEngine.Core
{
    /// <summary>
    /// Fixed-capacity ring buffer of recent positions per creeper (spec 7.8, 9.3). Flat
    /// <c>double[N*cap*3]</c> storage — no per-particle lists, no <c>RemoveAt(0)</c>, no reallocation
    /// after <see cref="Reset"/>. Pushing past capacity overwrites the oldest point. Points are read
    /// back oldest → newest for output.
    /// </summary>
    public sealed class TrailBuffer
    {
        private double[] _xyz = Array.Empty<double>();
        private int[] _head = Array.Empty<int>(); // index of the next write slot per creeper
        private int[] _len = Array.Empty<int>();   // stored count per creeper (<= Capacity)

        public int Count { get; private set; }
        public int Capacity { get; private set; }

        /// <summary>Allocate for <paramref name="count"/> creepers, capacity <paramref name="capacity"/> each
        /// (clamped to >= 2), and clear all trails. Grows arrays by need; never shrinks.</summary>
        public void Reset(int count, int capacity)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            Count = count;
            Capacity = Math.Max(2, capacity);

            int need = count * Capacity * 3;
            if (_xyz.Length < need) _xyz = new double[need];
            if (_head.Length < count) { _head = new int[count]; _len = new int[count]; }

            for (int i = 0; i < count; i++) { _head[i] = 0; _len[i] = 0; }
        }

        /// <summary>Clear one creeper's trail (e.g. on respawn/teleport, so no line is drawn across the domain).</summary>
        public void Clear(int i)
        {
            _head[i] = 0;
            _len[i] = 0;
        }

        /// <summary>Append a point for creeper <paramref name="i"/>, overwriting the oldest once full.</summary>
        public void Push(int i, double x, double y, double z)
        {
            int slot = (i * Capacity + _head[i]) * 3;
            _xyz[slot] = x;
            _xyz[slot + 1] = y;
            _xyz[slot + 2] = z;

            _head[i] = (_head[i] + 1) % Capacity;
            if (_len[i] < Capacity) _len[i]++;
        }

        /// <summary>Number of stored points for creeper <paramref name="i"/>.</summary>
        public int Length(int i) => _len[i];

        /// <summary>Read the k-th stored point (k = 0 is the oldest) for creeper <paramref name="i"/>.</summary>
        public void Get(int i, int k, out double x, out double y, out double z)
        {
            int len = _len[i];
            if (k < 0 || k >= len) throw new ArgumentOutOfRangeException(nameof(k));
            // Oldest stored point sits at head - len; walk forward by k, wrapping.
            int slotIndex = Mod(_head[i] - len + k, Capacity);
            int slot = (i * Capacity + slotIndex) * 3;
            x = _xyz[slot];
            y = _xyz[slot + 1];
            z = _xyz[slot + 2];
        }

        private static int Mod(int a, int m) => ((a % m) + m) % m;
    }
}
