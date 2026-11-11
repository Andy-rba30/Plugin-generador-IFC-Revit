using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace ArbaModeloPrueba.Core
{
    /// <summary>Unidades y geometría: el modelo se describe en metros; Revit trabaja en pies.</summary>
    internal static class U
    {
        public const double PiePorMetro = 1.0 / 0.3048;

        /// <summary>Metros a unidades internas (pies).</summary>
        public static double M(double metros) => metros * PiePorMetro;

        /// <summary>Unidades internas a metros.</summary>
        public static double AM(double pies) => pies * 0.3048;

        /// <summary>Punto en metros.</summary>
        public static XYZ P(double x, double y, double z = 0) => new XYZ(M(x), M(y), M(z));

        public static Line L(XYZ a, XYZ b) => Line.CreateBound(a, b);

        /// <summary>Contorno cerrado (en pies) a partir de vértices en metros sobre el plano z.</summary>
        public static CurveLoop Lazo(double z, params (double x, double y)[] v)
        {
            var lazo = new CurveLoop();
            for (int i = 0; i < v.Length; i++)
            {
                var a = v[i];
                var b = v[(i + 1) % v.Length];
                lazo.Append(Line.CreateBound(P(a.x, a.y, z), P(b.x, b.y, z)));
            }
            return lazo;
        }

        /// <summary>Rectángulo (en metros) centrado en (cx, cy) de lados a (X) y b (Y).</summary>
        public static (double, double)[] Rect(double cx, double cy, double a, double b) => new[]
        {
            (cx - a / 2, cy - b / 2), (cx + a / 2, cy - b / 2), (cx + a / 2, cy + b / 2), (cx - a / 2, cy + b / 2)
        };

        /// <summary>Rectángulo (en metros) por esquinas.</summary>
        public static (double, double)[] Caja(double x0, double y0, double x1, double y1) => new[]
        {
            (x0, y0), (x1, y0), (x1, y1), (x0, y1)
        };

        /// <summary>Desplaza una lista de vértices.</summary>
        public static (double, double)[] Mover((double x, double y)[] v, double dx, double dy)
        {
            var r = new (double, double)[v.Length];
            for (int i = 0; i < v.Length; i++) r[i] = (v[i].x + dx, v[i].y + dy);
            return r;
        }

        public static string F(double metros) => metros.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        public static IList<CurveLoop> Lista(params CurveLoop[] lazos) => new List<CurveLoop>(lazos);
    }
}
