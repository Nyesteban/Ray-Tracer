using System;


namespace rt
{
    public class Ellipsoid : Geometry
    {
        public Vector Center { get; }
        private Vector SemiAxesLength { get; }
        private double Radius { get; }
        
        
        public Ellipsoid(Vector center, Vector semiAxesLength, double radius, Material material, Color color) : base(material, color)
        {
            Center = center;
            SemiAxesLength = semiAxesLength;
            Radius = radius;
        }

        public Ellipsoid(Vector center, Vector semiAxesLength, double radius, Color color) : base(color)
        {
            Center = center;
            SemiAxesLength = semiAxesLength;
            Radius = radius;
        }

        public override Intersection GetIntersection(Line line, double minDist, double maxDist)
        {
            // TODO: ADD CODE HERE
            double a, b, c, d, e, f, dx, dy, dz, cA, cB, cC, delta, t1, t2, t, A, B, C, vx, vy, vz;

            A = SemiAxesLength.X * SemiAxesLength.X;
            B = SemiAxesLength.Y * SemiAxesLength.Y;
            C = SemiAxesLength.Z * SemiAxesLength.Z;

            a = line.Dx.X;
            c = line.Dx.Y;
            e = line.Dx.Z;

            b = line.X0.X;
            d = line.X0.Y;
            f = line.X0.Z;

            dx = b - Center.X;
            dy = d - Center.Y;
            dz = f - Center.Z;

            cA = a * a / A + c * c / B + e * e / C;
            cB = 2 * (a * dx / A + c * dy / B + e * dz / C);
            cC = dx * dx / A + dy * dy / B + dz * dz / C - Radius * Radius;

            delta = cB * cB - 4 * cA * cC;
            if (delta < 0)
            {
                return new Intersection();
            }
            t1 = (-cB - Math.Sqrt(delta)) / (2 * cA);
            t2 = (-cB + Math.Sqrt(delta)) / (2 * cA);
            if (t1 > minDist && t1 < maxDist)
            {
                t = t1;
            }
            else if (t2 > minDist && t2 < maxDist)
            {
                t = t2;
            }
            else
            {
                return new Intersection();
            }

            var vector = line.CoordinateToPosition(t);
            vx = vector.X - Center.X;
            vy = vector.Y - Center.Y;
            vz = vector.Z - Center.Z;
            return new Intersection(true, true, this, line, t, new Vector(2 * vx / A, 2 * vy / B, 2 * vz / C).Normalize(), Material, Color);
        }
    }
}
