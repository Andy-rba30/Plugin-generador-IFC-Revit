using System.Collections.Generic;
using System.Linq;
using ArbaModeloPrueba.Core;
using Autodesk.Revit.DB;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Zonas
{
    /// <summary>Terreno (Toposolid) en pendiente con cimentaciones bajo él y contornos de plataforma de varios tipos.</summary>
    internal sealed class ZonaExcavaciones : Zona
    {
        public override string Id => "EXC";
        public override string Titulo => "Excavaciones";
        public override string Plugins => "Excavaciones (cimentación, plataforma, metrado)";
        public override string Guia =>
            "Toposolid de 50x35 m en pendiente (+0.20 a +2.70 sobre Nivel 1). Cimentaciones con la cara superior en Cimentación (-1.00):\n" +
            "X1-X3 zapatas aisladas · X4+X5 contiguas solapadas (probar Unir sí/no) · X6 viga de cimentación · X7 losa de cimentación\n" +
            "X8 zapata 0.80 m más honda · X9 pilote circular · X10 zapata por encima del terreno (rechazo)\n" +
            "Plataformas: P1 líneas de modelo cerradas · P2 región rellena (planta Nivel 1) · P3 suelo · P4 líneas abiertas (rechazo)";
        public override double X0 => 0;
        public override double Y0 => -115;

        protected override void Generar()
        {
            double z1 = Cota(C.N1);

            C.Paso("Terreno Toposolid en pendiente", () =>
            {
                ToposolidType tipo = new FilteredElementCollector(C.Doc).OfClass(typeof(ToposolidType)).Cast<ToposolidType>().FirstOrDefault()
                    ?? throw new System.InvalidOperationException("El proyecto no tiene ningún tipo de Toposolid.");
                var puntos = new List<XYZ>();
                for (double x = -5; x <= 45.01; x += 5)
                    for (double y = -30; y <= 5.01; y += 5)
                        puntos.Add(P(X(x), Y(y), z1 + 0.20 + 0.05 * (x + 5)));
                Toposolid t = Toposolid.Create(C.Doc, puntos, tipo.Id, C.N1.Id);
                C.Comentario(t, "Terreno de prueba ARBA");
            });

            Level cim = C.Cim;
            var z150 = FamCimentacion("Zapata 150x150x50", () => Solidos.Caja(-0.75, -0.75, -0.50, 0.75, 0.75, 0));
            C.Paso("X1-X3 zapatas aisladas", () =>
            {
                for (int i = 0; i < 3; i++)
                    PonerZapata(z150, X(4 * i), Y(0), cim, "X" + (i + 1), -0.75, -0.75, 0.75, 0.75, -0.50);
            });
            C.Paso("X4 + X5 zapatas contiguas solapadas", () =>
            {
                PonerZapata(z150, X(14), Y(0), cim, "X4", -0.75, -0.75, 0.75, 0.75, -0.50);
                PonerZapata(z150, X(15.2), Y(0), cim, "X5", -0.75, -0.75, 0.75, 0.75, -0.50);
            });
            C.Paso("X6 viga de cimentación", () =>
            {
                double zc = Cota(cim);
                PonerViga(FamVigaRect(0.40, 0.70, 8), P(X(0), Y(-6), zc), P(X(8), Y(-6), zc), cim, "X6", 0.40, 0.70);
            });
            C.Paso("X7 losa de cimentación 6x4", () =>
                C.Losa(Lista(Lazo(Cota(cim), Caja(X(12), Y(-8), X(18), Y(-4)))), C.TipoLosaCimentacion(0.40), cim, "X7"));
            C.Paso("X8 zapata 0.80 m más honda", () =>
            {
                FamilyInstance z = PonerZapata(z150, X(24), Y(0), cim, "X8", -0.75, -0.75, 0.75, 0.75, -0.50);
                ElementTransformUtils.MoveElement(C.Doc, z.Id, P(0, 0, -0.80));
            });
            C.Paso("X9 pilote circular D=0.60 L=6", () =>
            {
                var f = FamCimentacion("Pilote D60 L=6.00", () => Solidos.Cilindro(0, 0, 0.30, -6.0, 0));
                PonerZapata(f, X(30), Y(0), cim, "X9", -0.30, -0.30, 0.30, 0.30, -6.0);
            });
            C.Paso("X10 zapata por encima del terreno (rechazo)", () =>
                PonerZapata(z150, X(36), Y(0), C.N3, "X10", -0.75, -0.75, 0.75, 0.75, -0.50));

            // ---- contornos de plataforma
            C.Paso("P1 plataforma por líneas de modelo cerradas", () =>
            {
                foreach (Curve c in Lazo(z1, Caja(X(20), Y(-22), X(30), Y(-14))))
                    C.LineaModelo(c, C.N1);
            });
            C.Paso("P2 plataforma por región rellena (planta Nivel 1)", () =>
            {
                ElementId tipo = new FilteredElementCollector(C.Doc).OfClass(typeof(FilledRegionType)).FirstElementId();
                if (tipo == ElementId.InvalidElementId) throw new System.InvalidOperationException("No hay tipos de región rellena.");
                FilledRegion.Create(C.Doc, tipo, C.PlantaN1.Id, Lista(Lazo(z1, Caja(X(32), Y(-22), X(42), Y(-14)))));
            });
            C.Paso("P3 plataforma por suelo", () =>
                C.Losa(Lista(Lazo(z1, Caja(X(0), Y(-22), X(8), Y(-14)))), C.TipoLosa(0.15, Contexto.Prefijo + "Plataforma e=0.15"), C.N1, "P3", estructural: false));
            C.Paso("P4 líneas abiertas (rechazo)", () =>
            {
                C.LineaModelo(Line.CreateBound(P(X(10), Y(-22), z1), P(X(18), Y(-22), z1)), C.N1);
                C.LineaModelo(Line.CreateBound(P(X(18), Y(-22), z1), P(X(18), Y(-14), z1)), C.N1);
                C.LineaModelo(Line.CreateBound(P(X(18), Y(-14), z1), P(X(10), Y(-14), z1)), C.N1);
            });
        }
    }
}
