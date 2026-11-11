using System;
using ArbaModeloPrueba.Core;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Zonas
{
    /// <summary>
    /// Nudos viga-columna para Uniones: cada viga empieza en el centro de su columna (un extremo dentro del contorno)
    /// y termina libre. Columnas de Nivel 1 a Nivel 3 salvo el nudo de cubierta.
    /// </summary>
    internal sealed class ZonaNudos : Zona
    {
        public override string Id => "NUD";
        public override string Titulo => "Nudos viga-columna";
        public override string Plugins => "Uniones (UnionesAcero) · Acero-columnas · Acero-vigas";
        public override string Guia =>
            "Vigas con la cara superior en Nivel 2, partidas en cada columna. J1 interior con 4 vigas · J2 dos colineales de distinto canto\n" +
            "J3 esquina · J4 columna en T: viga en la punta del ala y en la cara · J5 vigas a distinta cota · J6 nudo de cubierta (la columna acaba en la viga)\n" +
            "Avisos: J7 viga oblicua 30° · J8 viga con hueco de 10 cm · J9 viga más ancha que la columna · J10 columna girada 20° lejos del origen · J11 viga curva";
        public override double X0 => 120;
        public override double Y0 => -65;

        protected override void Generar()
        {
            double z2 = Cota(C.N2);
            var col6 = FamColumnaRect(0.40, 0.40, 6.00);
            var sec = Rect(0, 0, 0.40, 0.40);
            var v4 = FamVigaRect(0.25, 0.50, 4);

            Nudo("J1 interior con 4 vigas", "J1", col6, sec, 0, 0, C.N3, (v4, 0, 0.25, 0.50, 0.0), (v4, 90, 0.25, 0.50, 0.0), (v4, 180, 0.25, 0.50, 0.0), (v4, 270, 0.25, 0.50, 0.0));
            Nudo("J2 dos vigas colineales de distinto canto", "J2", col6, sec, 10, 0, C.N3, (v4, 0, 0.25, 0.50, 0.0), (FamVigaRect(0.25, 0.60, 4), 180, 0.25, 0.60, 0.0));
            Nudo("J3 esquina", "J3", col6, sec, 20, 0, C.N3, (v4, 0, 0.25, 0.50, 0.0), (v4, 90, 0.25, 0.50, 0.0));

            C.Paso("J4 columna en T: viga en la punta del ala y en la cara del ala", () =>
            {
                var t = new[] { (-0.4, 0.4), (-0.4, 0.15), (-0.125, 0.15), (-0.125, -0.4), (0.125, -0.4), (0.125, 0.15), (0.4, 0.15), (0.4, 0.4) };
                double x = X(0), y = Y(-10);
                FamilyInstance c = PonerColumna(FamColumna("T 80x80 e25", t, 6.0), t, x, y, C.N1, C.N3, "J4");
                FamilySymbol tv = C.Fam.Obtener(v4);
                Unir(c, C.Viga(tv, P(x + 0.275, y + 0.275, z2), P(x + 4.275, y + 0.275, z2), C.N2, "J4-punta ala"));
                Unir(c, C.Viga(tv, P(x - 0.275, y + 0.275, z2), P(x - 0.275, y + 4.275, z2), C.N2, "J4-cara ala"));
            });

            C.Paso("J5 vigas a distinta cota", () =>
            {
                double x = X(10), y = Y(-10);
                FamilyInstance c = PonerColumna(col6, sec, x, y, C.N1, C.N3, "J5");
                Unir(c, C.Viga(C.Fam.Obtener(v4), P(x, y, z2), P(x + 4, y, z2), C.N2, "J5-arriba"));
                Unir(c, C.Viga(C.Fam.Obtener(v4), P(x, y, z2 - 0.20), P(x, y + 4, z2 - 0.20), C.N2, "J5-20cm abajo"));
            });

            Nudo("J6 nudo de cubierta (la columna acaba en la viga)", "J6", FamColumnaRect(0.40, 0.40, 3.00), sec, 20, -10, C.N2,
                 (v4, 0, 0.25, 0.50, 0.0), (v4, 90, 0.25, 0.50, 0.0));

            Nudo("J7 viga oblicua a 30° (aviso)", "J7", col6, sec, 0, -20, C.N3, (v4, 30, 0.25, 0.50, 0.0));

            C.Paso("J8 viga con hueco de 10 cm a la cara (aviso)", () =>
            {
                double x = X(10), y = Y(-20);
                FamilyInstance c = PonerColumna(col6, sec, x, y, C.N1, C.N3, "J8");
                C.Viga(C.Fam.Obtener(FamVigaRect(0.25, 0.50, 3.70)), P(x + 0.30, y, z2), P(x + 4.0, y, z2), C.N2, "J8-viga");
            });

            Nudo("J9 viga más ancha que la columna (aviso)", "J9", FamColumnaRect(0.30, 0.30, 6.00), Rect(0, 0, 0.30, 0.30), 20, -20, C.N3,
                 (FamVigaRect(0.50, 0.50, 4), 0, 0.50, 0.50, 0.0));

            C.Paso("J10 columna girada 20° con vigas en sus ejes", () =>
            {
                double x = X(5), y = Y(-30);
                FamilyInstance c = C.Columna(C.Fam.Obtener(col6), x, y, C.N1, C.N3, "J10", 20);
                FamilySymbol tv = C.Fam.Obtener(v4);
                foreach (double g in new[] { 20.0, 110.0 })
                {
                    double r = g * Math.PI / 180;
                    Unir(c, C.Viga(tv, P(x, y, z2), P(x + 4 * Math.Cos(r), y + 4 * Math.Sin(r), z2), C.N2, "J10-" + g + "°"));
                }
            });

            C.Paso("J11 viga curva (se omite)", () =>
            {
                double x = X(15), y = Y(-30);
                FamilyInstance c = PonerColumna(col6, sec, x, y, C.N1, C.N3, "J11");
                Arc arco = Arc.Create(P(x, y, z2), P(x + 4, y, z2), P(x + 2, y + 1, z2));
                C.Doc.Create.NewFamilyInstance(arco, C.Fam.Obtener(v4), C.N2, StructuralType.Beam);
            });
        }

        /// <summary>Columna con vigas que salen de su centro en las direcciones indicadas (grados).</summary>
        private void Nudo(string que, string marca, FamiliaSpec col, (double x, double y)[] sec, double dx, double dy, Level tope,
                          params (FamiliaSpec viga, double grados, double ancho, double canto, double bajada)[] vigas)
        {
            C.Paso(que, () =>
            {
                double x = X(dx), y = Y(dy), z = Cota(C.N2);
                FamilyInstance c = PonerColumna(col, sec, x, y, C.N1, tope, marca);
                int i = 0;
                foreach (var v in vigas)
                {
                    double r = v.grados * Math.PI / 180;
                    XYZ a = P(x, y, z - v.bajada), b = P(x + 4 * Math.Cos(r), y + 4 * Math.Sin(r), z - v.bajada);
                    Unir(c, PonerViga(v.viga, a, b, C.N2, marca + "-V" + (++i), v.ancho, v.canto));
                }
            });
        }

        private void Unir(FamilyInstance columna, FamilyInstance viga)
        {
            C.Doc.Regenerate();
            try { C.Unir(columna, viga); }
            catch (Exception ex) { C.Informe.Aviso(C.Zona, "No se pudo unir " + viga.Name + " a la columna: " + ex.Message); }
        }
    }
}
