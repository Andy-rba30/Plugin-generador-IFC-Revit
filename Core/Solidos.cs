using System.Collections.Generic;
using Autodesk.Revit.DB;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Core
{
    /// <summary>
    /// Sólidos para las familias de prueba, descritos en metros. Cada forma es un único sólido (uniones y restas
    /// booleanas), que es lo que exigen los add-ins de armado ("varios sólidos" se rechaza).
    /// </summary>
    internal static class Solidos
    {
        /// <summary>Prisma vertical: polígono en planta (x, y) entre z0 y z1.</summary>
        public static Solid PrismaZ((double x, double y)[] planta, double z0, double z1)
        {
            CurveLoop lazo = Lazo(planta, v => P(v.Item1, v.Item2, z0), XYZ.BasisZ);
            return GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { lazo }, XYZ.BasisZ, M(z1 - z0));
        }

        /// <summary>Prisma horizontal según X: perfil en el plano (y, z) entre x0 y x1 (vigas, muros de contención).</summary>
        public static Solid PrismaX((double y, double z)[] perfil, double x0, double x1)
        {
            CurveLoop lazo = Lazo(perfil, v => P(x0, v.Item1, v.Item2), XYZ.BasisX);
            return GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { lazo }, XYZ.BasisX, M(x1 - x0));
        }

        /// <summary>Prisma horizontal según Y: perfil en el plano (x, z) entre y0 e y1 (vigas con cartelas o escalones).</summary>
        public static Solid PrismaY((double x, double z)[] perfil, double y0, double y1)
        {
            CurveLoop lazo = Lazo(perfil, v => P(v.Item1, y0, v.Item2), XYZ.BasisY);
            return GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { lazo }, XYZ.BasisY, M(y1 - y0));
        }

        /// <summary>Caja por esquinas (metros).</summary>
        public static Solid Caja(double x0, double y0, double z0, double x1, double y1, double z1)
            => PrismaZ(U.Caja(x0, y0, x1, y1), z0, z1);

        /// <summary>Cilindro vertical (columnas circulares, pilotes).</summary>
        public static Solid Cilindro(double cx, double cy, double radio, double z0, double z1)
        {
            XYZ c = P(cx, cy, z0);
            double r = M(radio);
            var lazo = new CurveLoop();
            lazo.Append(Arc.Create(c, r, 0, System.Math.PI, XYZ.BasisX, XYZ.BasisY));
            lazo.Append(Arc.Create(c, r, System.Math.PI, 2 * System.Math.PI, XYZ.BasisX, XYZ.BasisY));
            return GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { lazo }, XYZ.BasisZ, M(z1 - z0));
        }

        /// <summary>Tronco de pirámide rectangular centrado en (cx, cy): base a×b en z0 y cara superior c×d en z1.</summary>
        public static Solid Tronco(double cx, double cy, double a, double b, double z0, double c, double d, double z1)
        {
            CurveLoop abajo = Lazo(U.Rect(cx, cy, a, b), v => P(v.Item1, v.Item2, z0), XYZ.BasisZ);
            CurveLoop arriba = Lazo(U.Rect(cx, cy, c, d), v => P(v.Item1, v.Item2, z1), XYZ.BasisZ);
            return GeometryCreationUtilities.CreateBlendGeometry(abajo, arriba, null);
        }

        public static Solid Union(Solid a, params Solid[] otros)
        {
            Solid r = a;
            foreach (Solid s in otros)
                r = BooleanOperationsUtils.ExecuteBooleanOperation(r, s, BooleanOperationsType.Union);
            return r;
        }

        public static Solid Resta(Solid a, params Solid[] otros)
        {
            Solid r = a;
            foreach (Solid s in otros)
                r = BooleanOperationsUtils.ExecuteBooleanOperation(r, s, BooleanOperationsType.Difference);
            return r;
        }

        private static CurveLoop Lazo<T>(T[] v, System.Func<T, XYZ> punto, XYZ normal)
        {
            var lazo = new CurveLoop();
            for (int i = 0; i < v.Length; i++)
                lazo.Append(Line.CreateBound(punto(v[i]), punto(v[(i + 1) % v.Length])));
            if (!lazo.IsCounterclockwise(normal)) lazo.Flip();
            return lazo;
        }
    }
}
