using System;
using System.Collections.Generic;

namespace ParticleEngine.Core
{
    /// <summary>
    /// Uniform spatial grid for neighbour queries (spec 9.2). Built each frame with a counting sort
    /// into flat CSR arrays (<see cref="CellStart"/> + <see cref="Items"/>) — one pass to count, one to
    /// scatter, no per-cell lists and no allocation after the buffers are sized. Cell size is the
    /// largest search radius, so neighbours lie in the 27 (3D) / 9 (2D) cells around a point.
    ///
    /// Hot-path use: <see cref="CellCoords"/> to find a point's cell, then iterate the neighbour cells
    /// with <see cref="CellIndexOf"/> and read item indices from <see cref="Items"/> in the range
    /// [CellStart[c], CellStart[c+1]). This avoids enumerators/closures in the step loop (spec 9.4).
    /// </summary>
    public sealed class UniformGrid
    {
        private const int MaxCells = 2_000_000;

        private int[] _cellStart = Array.Empty<int>();
        private int[] _items = Array.Empty<int>();
        private int[] _cursor = Array.Empty<int>();

        private double _cell = 1.0;
        private double _ox, _oy, _oz;
        private int _nx = 1, _ny = 1, _nz = 1;
        private bool _is2D;
        private int _count;

        public int Nx => _nx;
        public int Ny => _ny;
        public int Nz => _nz;
        public double CellSize => _cell;

        /// <summary>CSR start offsets, length NumCells+1. Items for cell c are Items[CellStart[c]..CellStart[c+1]).</summary>
        public int[] CellStart => _cellStart;

        /// <summary>Particle indices, grouped by cell.</summary>
        public int[] Items => _items;

        public int NumCells => _nx * _ny * _nz;

        /// <summary>Rebuild the grid from the live point cloud. Origin tracks the live bounding box so
        /// particles that have left the spawn box are still indexed (spec 9.2).</summary>
        public void Build(double[] px, double[] py, double[] pz, int count, double searchRadius, bool is2D)
        {
            _count = count;
            _is2D = is2D;

            if (count <= 0)
            {
                _nx = _ny = _nz = 1;
                EnsureCellStart(1);
                _cellStart[0] = 0; _cellStart[1] = 0;
                return;
            }

            double minX = px[0], minY = py[0], minZ = pz[0];
            double maxX = px[0], maxY = py[0], maxZ = pz[0];
            for (int i = 1; i < count; i++)
            {
                if (px[i] < minX) minX = px[i]; else if (px[i] > maxX) maxX = px[i];
                if (py[i] < minY) minY = py[i]; else if (py[i] > maxY) maxY = py[i];
                if (pz[i] < minZ) minZ = pz[i]; else if (pz[i] > maxZ) maxZ = pz[i];
            }

            _ox = minX; _oy = minY; _oz = minZ;
            _cell = searchRadius > 1e-9 ? searchRadius : 1.0;

            // Choose cell counts; grow the cell size until the total fits the cap.
            while (true)
            {
                _nx = Cells(maxX - minX, _cell);
                _ny = Cells(maxY - minY, _cell);
                _nz = is2D ? 1 : Cells(maxZ - minZ, _cell);
                long total = (long)_nx * _ny * _nz;
                if (total <= MaxCells) break;
                _cell *= 2.0;
            }

            int numCells = _nx * _ny * _nz;
            EnsureCellStart(numCells);
            if (_items.Length < count) _items = new int[count];

            // Count per cell (into CellStart[c+1]).
            for (int c = 0; c <= numCells; c++) _cellStart[c] = 0;
            for (int i = 0; i < count; i++)
            {
                int c = CellIndexFromPoint(px[i], py[i], pz[i]);
                _cellStart[c + 1]++;
            }
            // Prefix sum -> start offsets.
            for (int c = 0; c < numCells; c++) _cellStart[c + 1] += _cellStart[c];

            // Scatter items into their cell ranges.
            if (_cursor.Length < numCells) _cursor = new int[numCells];
            for (int c = 0; c < numCells; c++) _cursor[c] = _cellStart[c];
            for (int i = 0; i < count; i++)
            {
                int c = CellIndexFromPoint(px[i], py[i], pz[i]);
                _items[_cursor[c]++] = i;
            }
        }

        /// <summary>Cell coordinates for a world point (clamped into range).</summary>
        public void CellCoords(double x, double y, double z, out int cx, out int cy, out int cz)
        {
            cx = Clamp((int)Math.Floor((x - _ox) / _cell), _nx);
            cy = Clamp((int)Math.Floor((y - _oy) / _cell), _ny);
            cz = _is2D ? 0 : Clamp((int)Math.Floor((z - _oz) / _cell), _nz);
        }

        /// <summary>Flat cell index for cell coordinates, or -1 if outside the grid.</summary>
        public int CellIndexOf(int cx, int cy, int cz)
        {
            if (cx < 0 || cx >= _nx || cy < 0 || cy >= _ny || cz < 0 || cz >= _nz) return -1;
            return (cz * _ny + cy) * _nx + cx;
        }

        /// <summary>Test/helper: collect candidate indices in the neighbour cells of a point (allocates).</summary>
        public void QueryNeighborCells(double x, double y, double z, List<int> result)
        {
            result.Clear();
            CellCoords(x, y, z, out int cx, out int cy, out int cz);
            int zlo = _is2D ? cz : cz - 1;
            int zhi = _is2D ? cz : cz + 1;
            for (int gz = zlo; gz <= zhi; gz++)
                for (int gy = cy - 1; gy <= cy + 1; gy++)
                    for (int gx = cx - 1; gx <= cx + 1; gx++)
                    {
                        int c = CellIndexOf(gx, gy, gz);
                        if (c < 0) continue;
                        for (int k = _cellStart[c]; k < _cellStart[c + 1]; k++)
                            result.Add(_items[k]);
                    }
        }

        private int CellIndexFromPoint(double x, double y, double z)
        {
            CellCoords(x, y, z, out int cx, out int cy, out int cz);
            return (cz * _ny + cy) * _nx + cx;
        }

        private static int Cells(double extent, double cell)
        {
            int n = (int)(extent / cell) + 1;
            return n < 1 ? 1 : n;
        }

        private static int Clamp(int v, int n)
        {
            if (v < 0) return 0;
            if (v >= n) return n - 1;
            return v;
        }

        private void EnsureCellStart(int numCells)
        {
            if (_cellStart.Length < numCells + 1) _cellStart = new int[numCells + 1];
        }
    }
}
