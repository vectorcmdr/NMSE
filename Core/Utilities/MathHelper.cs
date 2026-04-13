using NMSE.Models;

namespace NMSE.Core.Utilities;

/// <summary>
/// Mathematical helper types for 3D coordinate transformations.
/// Used primarily by base relocation algorithms.
/// </summary>
public static class MathHelper
{
    /// <summary>
    /// A minimal 3D vector used for coordinate system transformations during base relocation.
    /// </summary>
    public readonly struct Vec3(double x, double y, double z)
    {
        public readonly double X = x, Y = y, Z = z;

        /// <summary>Computes the Euclidean length of this vector.</summary>
        public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);

        /// <summary>Returns a unit vector in the same direction, or zero vector if degenerate.</summary>
        public Vec3 Normalized()
        {
            double len = Length();
            if (len < 1e-15) return new Vec3(0, 0, 0);
            return new Vec3(X / len, Y / len, Z / len);
        }

        /// <summary>Reads a Vec3 from a JSON array (first three elements).</summary>
        public static Vec3 FromArray(JsonArray arr) =>
            new(arr.GetDouble(0), arr.GetDouble(1), arr.GetDouble(2));

        /// <summary>Writes this Vec3 into the first three elements of an existing JSON array.</summary>
        public void WriteToArray(JsonArray arr)
        {
            arr.Set(0, X);
            arr.Set(1, Y);
            arr.Set(2, Z);
        }

        public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(double s, Vec3 a) => new(a.X * s, a.Y * s, a.Z * s);
        public static Vec3 operator *(Vec3 a, double s) => new(a.X * s, a.Y * s, a.Z * s);
        public static Vec3 operator /(Vec3 a, double s) => new(a.X / s, a.Y / s, a.Z / s);

        /// <summary>Computes the dot product of two vectors.</summary>
        public static double Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        /// <summary>Computes the cross product of two vectors.</summary>
        public static Vec3 Cross(Vec3 a, Vec3 b) => new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);

        /// <summary>
        /// Gram-Schmidt orthogonalisation: returns the component of <paramref name="v"/>
        /// perpendicular to <paramref name="onto"/> (normalised result).
        /// </summary>
        public static Vec3 GramSchmidt(Vec3 v, Vec3 onto)
        {
            Vec3 ontoN = onto.Normalized();
            return (v - Dot(v, ontoN) * ontoN).Normalized();
        }
    }

    /// <summary>
    /// An orthonormal local coordinate system in 3D space. Provides forward (local-to-world)
    /// and inverse (world-to-local) transforms. Used during Move Base Computer to convert
    /// between the old and new base reference frames.
    /// Based on community knowledge of the NMS base coordinate model.
    /// </summary>
    public readonly struct CoordSystem
    {
        public readonly Vec3 Origin, AxisX, AxisY, AxisZ;

        public CoordSystem(Vec3 origin, Vec3 axisX, Vec3 axisY, Vec3 axisZ)
        {
            Origin = origin;
            AxisX = axisX.Normalized();
            AxisY = axisY.Normalized();
            AxisZ = axisZ.Normalized();
        }

        /// <summary>Transforms a local-space vector to world-space.</summary>
        public Vec3 Apply(Vec3 local) =>
            Origin + local.X * AxisX + local.Y * AxisY + local.Z * AxisZ;

        /// <summary>
        /// Transforms a world-space vector to local-space by solving the 3x3 linear system
        /// using Cramer's rule (scalar triple products).
        /// </summary>
        public Vec3 Solve(Vec3 world)
        {
            Vec3 d = world - Origin;
            // det([AxisX | AxisY | AxisZ]) = AxisX . (AxisY x AxisZ)
            double det = Vec3.Dot(AxisX, Vec3.Cross(AxisY, AxisZ));
            if (Math.Abs(det) < 1e-15)
                return new Vec3(0, 0, 0); // Degenerate -- axes are coplanar

            double x = Vec3.Dot(d, Vec3.Cross(AxisY, AxisZ)) / det;
            double y = Vec3.Dot(AxisX, Vec3.Cross(d, AxisZ)) / det;
            double z = Vec3.Dot(AxisX, Vec3.Cross(AxisY, d)) / det;
            return new Vec3(x, y, z);
        }
    }
}
