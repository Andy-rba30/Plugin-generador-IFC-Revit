using ArbaModeloPrueba.Core;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Zonas
{
    /// <summary>Columnas de Nivel 1 a Nivel 2 con todas las secciones rectilíneas y los casos que Acero-columnas rechaza.</summary>
    internal sealed class ZonaColumnas : Zona
    {
        public override string Id => "COL";
        public override string Titulo => "Columnas";
        public override string Plugins => "Acero-columnas (ColumnRebar) · Uniones · Metrados · Borrar armado";
        public override string Guia =>
            "De Nivel 1 a Nivel 2 (3.00 m). C1-C4 rectangulares (25x50, 40x55, 45x45, 83x45) · C5 L · C6 T · C7 U · C8 cruz · C9 Z\n" +
            "C10 rectangular girada 30° · C11 L girada 45° · C12-C14 iguales (armar juntas)\n" +
            "Rechazos: C15 circular · C16 hueca · C17 con capitel · C18 de acero · C19 dos sólidos";
        public override double X0 => 0;
        public override double Y0 => -65;

        private const double H = 3.00;

        protected override void Generar()
        {
            Poner("C1 rectangular 25x50", "C1", "25x50", Rect(0, 0, 0.25, 0.50), 0, 0);
            Poner("C2 rectangular 40x55", "C2", "40x55", Rect(0, 0, 0.40, 0.55), 3, 0);
            Poner("C3 cuadrada 45x45", "C3", "45x45", Rect(0, 0, 0.45, 0.45), 6, 0);
            Poner("C4 rectangular 83x45", "C4", "83x45", Rect(0, 0, 0.83, 0.45), 9, 0);

            Poner("C5 en L 60x60 e=25", "C5", "L 60x60 e25",
                new[] { (-0.3, -0.3), (0.3, -0.3), (0.3, -0.05), (-0.05, -0.05), (-0.05, 0.3), (-0.3, 0.3) }, 0, -4);
            Poner("C6 en T 80x80 e=25", "C6", "T 80x80 e25",
                new[] { (-0.4, 0.4), (-0.4, 0.15), (-0.125, 0.15), (-0.125, -0.4), (0.125, -0.4), (0.125, 0.15), (0.4, 0.15), (0.4, 0.4) }, 3, -4);
            Poner("C7 en U 80x60 e=25", "C7", "U 80x60 e25",
                new[] { (-0.4, -0.3), (0.4, -0.3), (0.4, 0.3), (0.15, 0.3), (0.15, -0.05), (-0.15, -0.05), (-0.15, 0.3), (-0.4, 0.3) }, 6, -4);
            Poner("C8 en cruz 80x80 e=25", "C8", "cruz 80x80 e25",
                new[] { (-0.125, -0.4), (0.125, -0.4), (0.125, -0.125), (0.4, -0.125), (0.4, 0.125), (0.125, 0.125),
                        (0.125, 0.4), (-0.125, 0.4), (-0.125, 0.125), (-0.4, 0.125), (-0.4, -0.125), (-0.125, -0.125) }, 9, -4);
            Poner("C9 en Z", "C9", "Z 80x60 e25",
                new[] { (-0.4, 0.05), (-0.125, 0.05), (-0.125, -0.3), (0.4, -0.3), (0.4, -0.05), (0.125, -0.05), (0.125, 0.3), (-0.4, 0.3) }, 12, -4);

            Poner("C10 rectangular 30x60 girada 30°", "C10", "30x60", Rect(0, 0, 0.30, 0.60), 0, -8, 30);
            Poner("C11 en L girada 45°", "C11", "L 60x60 e25",
                new[] { (-0.3, -0.3), (0.3, -0.3), (0.3, -0.05), (-0.05, -0.05), (-0.05, 0.3), (-0.3, 0.3) }, 3, -8, 45);
            for (int i = 0; i < 3; i++)
                Poner("C" + (12 + i) + " igual a las otras dos (40x40)", "C" + (12 + i), "40x40", Rect(0, 0, 0.40, 0.40), 6 + 2 * i, -8);

            // ---- rechazos
            C.Paso("C15 circular D=50 (rechazo)", () =>
            {
                var f = new FamiliaSpec
                {
                    Nombre = Contexto.Prefijo + "Columna circular D50 h=" + F(H),
                    Categoria = BuiltInCategory.OST_StructuralColumns,
                    Material = Materiales.Concreto,
                    Geometria = () => Solidos.Cilindro(0, 0, 0.25, 0, H)
                };
                C.Columna(C.Fam.Obtener(f), X(0), Y(-12), C.N1, C.N2, "C15");
            });

            C.Paso("C16 hueca 60x60 (rechazo)", () =>
            {
                var f = new FamiliaSpec
                {
                    Nombre = Contexto.Prefijo + "Columna hueca 60x60 h=" + F(H),
                    Categoria = BuiltInCategory.OST_StructuralColumns,
                    Material = Materiales.Concreto,
                    Geometria = () => Solidos.Resta(Solidos.Caja(-0.3, -0.3, 0, 0.3, 0.3, H), Solidos.Caja(-0.15, -0.15, -1, 0.15, 0.15, H + 1))
                };
                C.Columna(C.Fam.Obtener(f), X(3), Y(-12), C.N1, C.N2, "C16");
            });

            C.Paso("C17 con capitel (rechazo: no prismática)", () =>
            {
                var f = new FamiliaSpec
                {
                    Nombre = Contexto.Prefijo + "Columna con capitel h=" + F(H),
                    Categoria = BuiltInCategory.OST_StructuralColumns,
                    Material = Materiales.Concreto,
                    Geometria = () => Solidos.Union(Solidos.Caja(-0.2, -0.2, 0, 0.2, 0.2, H - 0.5), Solidos.Caja(-0.4, -0.4, H - 0.5, 0.4, 0.4, H))
                };
                C.Columna(C.Fam.Obtener(f), X(6), Y(-12), C.N1, C.N2, "C17");
            });

            C.Paso("C18 de acero (rechazo: material)", () =>
                PonerColumna(FamColumnaRect(0.40, 0.40, H, Materiales.Acero), Rect(0, 0, 0.40, 0.40), X(9), Y(-12), C.N1, C.N2, "C18"));

            C.Paso("C19 dos sólidos en la familia (rechazo)", () =>
            {
                var f = new FamiliaSpec
                {
                    Nombre = Contexto.Prefijo + "Columna dos sólidos h=" + F(H),
                    Categoria = BuiltInCategory.OST_StructuralColumns,
                    Material = Materiales.Concreto,
                    Varios = () => new[] { Solidos.Caja(-0.3, -0.2, 0, -0.02, 0.2, H), Solidos.Caja(0.02, -0.2, 0, 0.3, 0.2, H) }
                };
                C.Columna(C.Fam.Obtener(f), X(12), Y(-12), C.N1, C.N2, "C19");
            });
        }

        private void Poner(string que, string marca, string nombreSeccion, (double x, double y)[] seccion, double dx, double dy, double giro = 0)
        {
            C.Paso(que, () => PonerColumna(FamColumna(nombreSeccion, seccion, H), seccion, X(dx), Y(dy), C.N1, C.N2, marca, giro));
        }
    }
}
