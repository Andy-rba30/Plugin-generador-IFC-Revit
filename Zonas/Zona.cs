using System;
using System.Linq;
using ArbaModeloPrueba.Core;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Zonas
{
    /// <summary>
    /// Zona de prueba: un grupo de elementos para uno o varios add-ins, en su propio rectángulo del modelo y
    /// rotulado en la planta "PRUEBA ARBA - Planta Nivel 1".
    /// </summary>
    internal abstract class Zona
    {
        public abstract string Id { get; }
        public abstract string Titulo { get; }
        /// <summary>Add-ins que se prueban en la zona.</summary>
        public abstract string Plugins { get; }
        /// <summary>Qué probar con estos elementos (va al rótulo y al README).</summary>
        public abstract string Guia { get; }
        public abstract double X0 { get; }
        public abstract double Y0 { get; }

        protected Contexto C;

        public void Ejecutar(Contexto c)
        {
            C = c;
            c.Zona = Id + " " + Titulo;
            Generar();
            c.Paso("Rótulo", () => c.Rotulo(X0 - 3, Y0 + 9, Id + " - " + Titulo + "\n" + Plugins + "\n" + Guia));
        }

        protected abstract void Generar();

        protected double X(double dx) => X0 + dx;
        protected double Y(double dy) => Y0 + dy;
        protected double Cota(Level n) => AM(n.Elevation);

        // ------------------------------------------------------------------ familias habituales

        protected static FamiliaSpec FamColumna(string nombre, (double x, double y)[] seccion, double alto,
            string material = Materiales.Concreto, StructuralMaterialType tipo = StructuralMaterialType.Concrete) => new FamiliaSpec
        {
            Nombre = Contexto.Prefijo + "Columna " + nombre + " h=" + F(alto),
            Categoria = BuiltInCategory.OST_StructuralColumns,
            Geometria = () => Solidos.PrismaZ(seccion, 0, alto),
            Material = material,
            TipoMaterial = tipo
        };

        protected static FamiliaSpec FamColumnaRect(double b, double h, double alto, string material = Materiales.Concreto) =>
            FamColumna(Cm(b) + "x" + Cm(h) + (material == Materiales.Concreto ? "" : " " + material.Replace(Contexto.Prefijo, "")),
                       Rect(0, 0, b, h), alto, material,
                       material == Materiales.Concreto ? StructuralMaterialType.Concrete : StructuralMaterialType.Steel);

        /// <summary>Viga: perfil (y, z) con la cara superior en z = 0, longitud L según X.</summary>
        protected static FamiliaSpec FamViga(string nombre, (double y, double z)[] perfil, double largo,
            string material = Materiales.Concreto, StructuralMaterialType tipo = StructuralMaterialType.Concrete) => new FamiliaSpec
        {
            Nombre = Contexto.Prefijo + "Viga " + nombre + " L=" + F(largo),
            Categoria = BuiltInCategory.OST_StructuralFraming,
            Geometria = () => Solidos.PrismaX(perfil, 0, largo),
            Material = material,
            TipoMaterial = tipo
        };

        protected static FamiliaSpec FamVigaRect(double b, double h, double largo) =>
            FamViga(Cm(b) + "x" + Cm(h), PerfilRect(b, h), largo);

        protected static (double, double)[] PerfilRect(double b, double h) => new[] { (-b / 2, -h), (b / 2, -h), (b / 2, 0.0), (-b / 2, 0.0) };

        /// <summary>Cimentación: sólido libre (cara superior en z = 0 salvo que la zona diga otra cosa).</summary>
        protected static FamiliaSpec FamCimentacion(string nombre, Func<Solid> geometria,
            string material = Materiales.Concreto, StructuralMaterialType tipo = StructuralMaterialType.Concrete) => new FamiliaSpec
        {
            Nombre = Contexto.Prefijo + nombre,
            Categoria = BuiltInCategory.OST_StructuralFoundation,
            Geometria = geometria,
            Material = material,
            TipoMaterial = tipo
        };

        protected static string Cm(double m) => Math.Round(m * 100).ToString("0");

        // ------------------------------------------------------------------ colocación con comprobación

        /// <summary>Columna recta (sin giro) con comprobación de su caja.</summary>
        protected FamilyInstance PonerColumna(FamiliaSpec spec, (double x, double y)[] seccion, double x, double y,
            Level baseN, Level topeN, string marca, double giro = 0)
        {
            FamilyInstance c = C.Columna(C.Fam.Obtener(spec), x, y, baseN, topeN, marca, giro);
            if (Math.Abs(giro) < 1e-9)
            {
                double x0 = seccion.Min(v => v.x), x1 = seccion.Max(v => v.x), y0 = seccion.Min(v => v.y), y1 = seccion.Max(v => v.y);
                C.Encajar(c, x + x0, y + y0, Cota(baseN), x + x1, y + y1, Cota(topeN), marca);
            }
            return c;
        }

        /// <summary>Viga según X (de xa a xb) o según Y, con la cara superior a la cota z.</summary>
        protected FamilyInstance PonerViga(FamiliaSpec spec, XYZ a, XYZ b, Level nivel, string marca, double ancho, double canto)
        {
            FamilyInstance v = C.Viga(C.Fam.Obtener(spec), a, b, nivel, marca);
            double ax = AM(a.X), ay = AM(a.Y), bx = AM(b.X), by = AM(b.Y), z = AM(Math.Max(a.Z, b.Z));
            bool segunX = Math.Abs(ay - by) < 1e-6 && Math.Abs(a.Z - b.Z) < 1e-9;
            bool segunY = Math.Abs(ax - bx) < 1e-6 && Math.Abs(a.Z - b.Z) < 1e-9;
            if (segunX)
                C.Encajar(v, Math.Min(ax, bx), ay - ancho / 2, z - canto, Math.Max(ax, bx), ay + ancho / 2, z, marca, mover: false);
            else if (segunY)
                C.Encajar(v, ax - ancho / 2, Math.Min(ay, by), z - canto, ax + ancho / 2, Math.Max(ay, by), z, marca, mover: false);
            return v;
        }

        /// <summary>Cimentación aislada con comprobación de su caja (planta y canto conocidos, cara superior en el nivel).</summary>
        protected FamilyInstance PonerZapata(FamiliaSpec spec, double x, double y, Level nivel, string marca,
            double xMin, double yMin, double xMax, double yMax, double zMin, double zMax = 0)
        {
            FamilyInstance z = C.Zapata(C.Fam.Obtener(spec), x, y, nivel, marca);
            double c = Cota(nivel);
            C.Encajar(z, x + xMin, y + yMin, c + zMin, x + xMax, y + yMax, c + zMax, marca);
            return z;
        }
    }
}
