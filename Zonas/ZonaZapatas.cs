using ArbaModeloPrueba.Core;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Zonas
{
    /// <summary>Zapatas: todas las formas que reconoce Acero-Zapatas, con columnas encima y casos que debe rechazar.</summary>
    internal sealed class ZonaZapatas : Zona
    {
        public override string Id => "ZAP";
        public override string Titulo => "Zapatas";
        public override string Plugins => "Acero-Zapatas · Excavaciones · Metrados · Borrar armado";
        public override string Guia =>
            "Z1 aislada · Z2 combinada (2 columnas, parrilla superior) · Z3 en L · Z4 con pase · Z5 escalonada · Z6 piramidal\n" +
            "Z7 fondo a dos cotas (nota) · Z8 invertida (nota) · Z9 losa de cimentación con huecos · Z10 corrida bajo muro\n" +
            "Rechazos: Z11 base inclinada · Z12 canto 8 cm · Z13 de acero · Z14 columna fuera (no debe contar)";
        public override double X0 => 0;
        public override double Y0 => 0;

        protected override void Generar()
        {
            Level cim = C.Cim;
            var col = FamColumnaRect(0.40, 0.40, 1.00);
            var secCol = Rect(0, 0, 0.40, 0.40);

            C.Paso("Z1 aislada 1.80x1.50x0.60 + columna", () =>
            {
                var f = FamCimentacion("Zapata 180x150x60", () => Solidos.Caja(-0.90, -0.75, -0.60, 0.90, 0.75, 0));
                PonerZapata(f, X(0), Y(0), cim, "Z1", -0.90, -0.75, 0.90, 0.75, -0.60);
                PonerColumna(col, secCol, X(0), Y(0), cim, C.N1, "C-Z1");
            });

            C.Paso("Z2 combinada 4.20x1.60x0.70 + 2 columnas", () =>
            {
                var f = FamCimentacion("Zapata combinada 420x160x70", () => Solidos.Caja(-2.10, -0.80, -0.70, 2.10, 0.80, 0));
                PonerZapata(f, X(7), Y(0), cim, "Z2", -2.10, -0.80, 2.10, 0.80, -0.70);
                PonerColumna(col, secCol, X(7 - 1.30), Y(0), cim, C.N1, "C-Z2a");
                PonerColumna(col, secCol, X(7 + 1.30), Y(0), cim, C.N1, "C-Z2b");
            });

            C.Paso("Z3 en L", () =>
            {
                var planta = new[] { (-1.5, -1.5), (1.5, -1.5), (1.5, -0.3), (-0.3, -0.3), (-0.3, 1.5), (-1.5, 1.5) };
                var f = FamCimentacion("Zapata en L 300x300 brazo 120", () => Solidos.PrismaZ(planta, -0.60, 0));
                PonerZapata(f, X(14), Y(0), cim, "Z3", -1.5, -1.5, 1.5, 1.5, -0.60);
                PonerColumna(col, secCol, X(14 - 0.9), Y(-0.9), cim, C.N1, "C-Z3");
            });

            C.Paso("Z4 con pase 0.50x0.50", () =>
            {
                var f = FamCimentacion("Zapata 260x260 con pase", () => Solidos.Resta(
                    Solidos.Caja(-1.30, -1.30, -0.60, 1.30, 1.30, 0),
                    Solidos.Caja(0.45, 0.45, -1.0, 0.95, 0.95, 0.5)));
                PonerZapata(f, X(21), Y(0), cim, "Z4", -1.30, -1.30, 1.30, 1.30, -0.60);
                PonerColumna(col, secCol, X(21), Y(0), cim, C.N1, "C-Z4");
            });

            C.Paso("Z5 escalonada", () =>
            {
                var f = FamCimentacion("Zapata escalonada 220-120", () => Solidos.Union(
                    Solidos.Caja(-1.10, -1.10, -0.70, 1.10, 1.10, -0.30),
                    Solidos.Caja(-0.60, -0.60, -0.30, 0.60, 0.60, 0)));
                PonerZapata(f, X(0), Y(-7), cim, "Z5", -1.10, -1.10, 1.10, 1.10, -0.70);
                PonerColumna(col, secCol, X(0), Y(-7), cim, C.N1, "C-Z5");
            });

            C.Paso("Z6 piramidal", () =>
            {
                var f = FamCimentacion("Zapata piramidal 200-60", () => Solidos.Union(
                    Solidos.Caja(-1.0, -1.0, -0.60, 1.0, 1.0, -0.30),
                    Solidos.Tronco(0, 0, 2.0, 2.0, -0.30, 0.60, 0.60, 0)));
                PonerZapata(f, X(7), Y(-7), cim, "Z6", -1.0, -1.0, 1.0, 1.0, -0.60);
                PonerColumna(col, secCol, X(7), Y(-7), cim, C.N1, "C-Z6");
            });

            C.Paso("Z7 fondo a dos cotas", () =>
            {
                var f = FamCimentacion("Zapata fondo a dos cotas", () => Solidos.Union(
                    Solidos.Caja(-1.5, -0.75, -0.60, 0, 0.75, 0),
                    Solidos.Caja(0, -0.75, -0.90, 1.5, 0.75, 0)));
                PonerZapata(f, X(14), Y(-7), cim, "Z7", -1.5, -0.75, 1.5, 0.75, -0.90);
            });

            C.Paso("Z8 invertida (cara superior mayor)", () =>
            {
                var f = FamCimentacion("Zapata invertida", () => Solidos.Union(
                    Solidos.Caja(-1.0, -1.0, -0.30, 1.0, 1.0, 0),
                    Solidos.Caja(-0.6, -0.6, -0.80, 0.6, 0.6, -0.30)));
                PonerZapata(f, X(21), Y(-7), cim, "Z8", -1.0, -1.0, 1.0, 1.0, -0.80);
            });

            C.Paso("Z9 losa de cimentación 8x6 con 2 huecos + 2 columnas", () =>
            {
                double x = X(0), y = Y(-16);
                var lazos = Lista(
                    Lazo(Cota(cim), Caja(x - 4, y - 3, x + 4, y + 3)),
                    Lazo(Cota(cim), Caja(x - 3, y + 0.8, x - 2, y + 1.8)),
                    Lazo(Cota(cim), Caja(x + 2, y - 1.8, x + 3, y - 0.8)));
                C.Losa(lazos, C.TipoLosaCimentacion(0.50), cim, "Z9");
                PonerColumna(col, secCol, x - 1.5, y, cim, C.N1, "C-Z9a");
                PonerColumna(col, secCol, x + 1.5, y, cim, C.N1, "C-Z9b");
            });

            C.Paso("Z10 corrida bajo muro (cimentación de muro)", () =>
            {
                double y = Y(-16);
                Wall w = C.Muro(P(X(9), y, Cota(cim)), P(X(17), y, Cota(cim)), C.TipoMuro(0.25), cim, 1.0, "SC-Z10");
                WallFoundation wf = WallFoundation.Create(C.Doc, C.TipoCimientoCorrido(0.60, 0.50).Id, w.Id);
                C.Marca(wf, "Z10");
            });

            // ---- casos que Acero-Zapatas debe rechazar o avisar
            C.Paso("Z11 base inclinada (rechazo)", () =>
            {
                var perfil = new[] { (-1.0, 0.0), (1.0, 0.0), (1.0, -0.80), (-1.0, -0.50) };
                var f = FamCimentacion("Zapata base inclinada", () => Solidos.PrismaY(perfil, -0.75, 0.75));
                PonerZapata(f, X(0), Y(-24), cim, "Z11", -1.0, -0.75, 1.0, 0.75, -0.80);
            });

            C.Paso("Z12 canto 8 cm (rechazo)", () =>
            {
                var f = FamCimentacion("Zapata canto 8 cm", () => Solidos.Caja(-0.75, -0.75, -0.08, 0.75, 0.75, 0));
                PonerZapata(f, X(7), Y(-24), cim, "Z12", -0.75, -0.75, 0.75, 0.75, -0.08);
            });

            C.Paso("Z13 de acero (no es anfitrión válido)", () =>
            {
                var f = FamCimentacion("Zapata de acero", () => Solidos.Caja(-0.75, -0.75, -0.50, 0.75, 0.75, 0),
                                       Materiales.Acero, StructuralMaterialType.Steel);
                PonerZapata(f, X(14), Y(-24), cim, "Z13", -0.75, -0.75, 0.75, 0.75, -0.50);
            });

            C.Paso("Z14 columna fuera de la zapata", () =>
            {
                var f = FamCimentacion("Zapata 150x150x50", () => Solidos.Caja(-0.75, -0.75, -0.50, 0.75, 0.75, 0));
                PonerZapata(f, X(21), Y(-24), cim, "Z14", -0.75, -0.75, 0.75, 0.75, -0.50);
                PonerColumna(col, secCol, X(21 + 1.6), Y(-24), cim, C.N1, "C-Z14 fuera");
            });
        }
    }
}
