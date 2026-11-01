using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using ParticleEngine.Core.Settings;
using ParticleEngine.Grasshopper.Goo;
using Rhino.Geometry;

namespace ParticleEngine.Grasshopper
{
    /// <summary>
    /// Reading helpers for the M2 components: coerce Grasshopper's generic ports to the scalar/geometry
    /// types the Core settings need, and unwrap our <see cref="ParticleGoo{T}"/> back to the Core object.
    /// </summary>
    internal static class GhInput
    {
        internal static bool GetInt(IGH_DataAccess da, int index, ref int value)
        {
            IGH_Goo? goo = null;
            if (!da.GetData(index, ref goo) || goo == null) return false;
            if (GH_Convert.ToInt32(goo, out int r, GH_Conversion.Both)) { value = r; return true; }
            return false;
        }

        internal static bool GetDouble(IGH_DataAccess da, int index, ref double value)
        {
            IGH_Goo? goo = null;
            if (!da.GetData(index, ref goo) || goo == null) return false;
            if (GH_Convert.ToDouble(goo, out double r, GH_Conversion.Both)) { value = r; return true; }
            return false;
        }

        internal static bool GetBool(IGH_DataAccess da, int index, ref bool value)
        {
            IGH_Goo? goo = null;
            if (!da.GetData(index, ref goo) || goo == null) return false;
            if (GH_Convert.ToBoolean(goo, out bool r, GH_Conversion.Both)) { value = r; return true; }
            return false;
        }

        /// <summary>Initial Speed||Vector: a vector if one was supplied, otherwise a scalar speed.</summary>
        internal static bool GetVectorOrNumber(IGH_DataAccess da, int index,
            out bool isVector, out Vector3d vector, out double number)
        {
            isVector = false; vector = Vector3d.Zero; number = 0.0;
            IGH_Goo? goo = null;
            if (!da.GetData(index, ref goo) || goo == null) return false;

            if (goo is GH_Vector gv) { isVector = true; vector = gv.Value; return true; }
            if (GH_Convert.ToVector3d(goo, ref vector, GH_Conversion.Primary)) { isVector = true; return true; }
            if (GH_Convert.ToDouble(goo, out number, GH_Conversion.Both)) { isVector = false; return true; }
            return false;
        }

        /// <summary>Unwrap one of our settings objects from an ITEM-access generic input.</summary>
        internal static T? GetSettings<T>(IGH_DataAccess da, int index) where T : class, IParticleSettings
        {
            IGH_Goo? goo = null;
            if (!da.GetData(index, ref goo) || goo == null) return null;
            return Unwrap<T>(goo);
        }

        /// <summary>Unwrap the first settings object from a LIST-access generic input (e.g. Init Settings'
        /// Spawn Settings, which Culebra declares as list access).</summary>
        internal static T? GetSettingsFromList<T>(IGH_DataAccess da, int index) where T : class, IParticleSettings
        {
            var goos = new List<IGH_Goo>();
            if (!da.GetDataList(index, goos)) return null;
            foreach (IGH_Goo goo in goos)
            {
                if (goo == null) continue;
                T? value = Unwrap<T>(goo);
                if (value != null) return value;
            }
            return null;
        }

        private static T? Unwrap<T>(IGH_Goo goo) where T : class, IParticleSettings
        {
            if (goo is ParticleGoo<T> pg) return pg.Value;
            if (goo is GH_ObjectWrapper w && w.Value is T wrapped) return wrapped;
            return goo.ScriptVariable() as T;
        }

        /// <summary>Flatten a point list into x,y,z triples for Core.</summary>
        internal static double[] PointsToFlat(List<Point3d> points)
        {
            var flat = new double[points.Count * 3];
            for (int i = 0; i < points.Count; i++)
            {
                flat[i * 3] = points[i].X;
                flat[i * 3 + 1] = points[i].Y;
                flat[i * 3 + 2] = points[i].Z;
            }
            return flat;
        }

        /// <summary>Threshold for target index with longest-list behaviour: one value applies to all.</summary>
        internal static double ThresholdFor(IReadOnlyList<double> thresholds, int index)
        {
            if (thresholds == null || thresholds.Count == 0) return 10.0; // spec fallback
            if (thresholds.Count == 1) return thresholds[0];
            return thresholds[index < thresholds.Count ? index : thresholds.Count - 1];
        }

        /// <summary>World axis-aligned min/max of a Box (Core works on AABB bounds).</summary>
        internal static void BoxBounds(Box box, out double[] min, out double[] max)
        {
            BoundingBox bb = box.BoundingBox;
            min = new[] { bb.Min.X, bb.Min.Y, bb.Min.Z };
            max = new[] { bb.Max.X, bb.Max.Y, bb.Max.Z };
        }
    }
}
