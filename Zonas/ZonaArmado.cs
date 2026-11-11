using System.Collections.Generic;
using System.Linq;
using ArbaModeloPrueba.Core;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Zonas
{
    /// <summary>
    /// Armado de muestra: barras sueltas con particiones antiguas (sin origen ARBA) para probar la migración, y
    /// refuerzo por área, por camino y malla para Borrar armado y Metrados. Necesita tipos de barra en el proyecto.
    /// </summary>
    internal sealed class ZonaArmado : Zona
    {
        public override string Id => "ARM";
        public override string Titulo => "Armado de muestra";
        public override string Plugins => "Borrar armado · Migración del contrato ARBA (Zapatas, Vigas) · Metrados (refuerzo) · Uniones (barras existentes)";
        public override string Guia =>
            "Necesita tipos de barra: ejecuta antes «Tipos de barra Perú». A1 viga con barras «VIG-VA1» (partición antigua: migrar)\n" +
            "A2 zapata con barra «ZAP-ZA2» (migrar) · A3 columna con barra sin partición · A4 losa con refuerzo por área\n" +
            "A5 muro con refuerzo por camino · A6 muro con malla (si hay tipos de malla) · Borrar armado: probar selección de anfitrión y de barra";
        public override double X0 => 110;
        public override double Y0 => -115;

        private RebarBarType _barra;

        protected override void Generar()
        {
            _barra = new FilteredElementCollector(C.Doc).OfClass(typeof(RebarBarType)).Cast<RebarBarType>()
                .OrderByDescending(b => b.Name.Contains("1/2"))
                .FirstOrDefault();
            if (_barra == null)
            {
                C.Informe.Error(C.Zona, "El proyecto no tiene tipos de barra. Ejecuta «Tipos de barra Perú» y vuelve a generar solo esta zona.");
                return;
            }
            double z2 = Cota(C.N2), z1 = Cota(C.N1), zc = Cota(C.Cim);

            C.Paso("A1 viga 30x60 con 3 barras inferiores «VIG-VA1»", () =>
            {
                FamilyInstance v = PonerViga(FamVigaRect(0.30, 0.60, 6), P(X(0), Y(0), z2), P(X(6), Y(0), z2), C.N2, "VA1", 0.30, 0.60);
                C.Doc.Regenerate();
                foreach (double y in new[] { -0.09, 0.0, 0.09 })
                    Barra(v, P(X(0.05), Y(y), z2 - 0.54), P(X(5.95), Y(y), z2 - 0.54), XYZ.BasisY, "VIG-VA1");
            });

            C.Paso("A2 zapata 2.00x2.00x0.60 con barra «ZAP-ZA2»", () =>
            {
                var f = FamCimentacion("Zapata 200x200x60", () => Solidos.Caja(-1.0, -1.0, -0.60, 1.0, 1.0, 0));
                FamilyInstance z = PonerZapata(f, X(10), Y(0), C.Cim, "ZA2", -1.0, -1.0, 1.0, 1.0, -0.60);
                C.Doc.Regenerate();
                Barra(z, P(X(9.1), Y(0), zc - 0.50), P(X(10.9), Y(0), zc - 0.50), XYZ.BasisY, "ZAP-ZA2");
            });

            C.Paso("A3 columna 40x40 con barra vertical sin partición", () =>
            {
                FamilyInstance c = PonerColumna(FamColumnaRect(0.40, 0.40, 3.00), Rect(0, 0, 0.4, 0.4), X(14), Y(0), C.N1, C.N2, "CA3");
                C.Doc.Regenerate();
                Barra(c, P(X(14 - 0.13), Y(-0.13), z1 + 0.05), P(X(14 - 0.13), Y(-0.13), z2 - 0.05), XYZ.BasisX, null);
            });

            C.Paso("A4 losa 6x5 con refuerzo por área", () =>
            {
                Floor l = C.Losa(Lista(Lazo(z2, Caja(X(0), Y(-9), X(6), Y(-4)))), C.TipoLosa(0.20), C.N2, "LA4");
                C.Doc.Regenerate();
                ElementId tipo = new FilteredElementCollector(C.Doc).OfClass(typeof(AreaReinforcementType)).FirstElementId();
                if (tipo == ElementId.InvalidElementId) tipo = AreaReinforcementType.CreateDefaultAreaReinforcementType(C.Doc);
                IList<Curve> borde = Lazo(z2, Caja(X(0.5), Y(-8.5), X(5.5), Y(-4.5))).ToList();
                AreaReinforcement ar = AreaReinforcement.Create(C.Doc, l, borde, XYZ.BasisX, tipo, _barra.Id, ElementId.InvalidElementId);
                C.Marca(ar, "AR-LA4");
            });

            C.Paso("A5 muro 6x3 con refuerzo por camino", () =>
            {
                Wall w = C.Muro(P(X(8), Y(-6), z1), P(X(14), Y(-6), z1), C.TipoMuro(0.25), C.N1, 3.0, "MA5");
                C.Doc.Regenerate();
                ElementId tipo = new FilteredElementCollector(C.Doc).OfClass(typeof(PathReinforcementType)).FirstElementId();
                if (tipo == ElementId.InvalidElementId) tipo = PathReinforcementType.CreateDefaultPathReinforcementType(C.Doc);
                var camino = new List<Curve> { Line.CreateBound(P(X(8.5), Y(-6), z1 + 2.5), P(X(13.5), Y(-6), z1 + 2.5)) };
                PathReinforcement pr = PathReinforcement.Create(C.Doc, w, camino, false, tipo, _barra.Id, ElementId.InvalidElementId, ElementId.InvalidElementId);
                C.Marca(pr, "PR-MA5");
            });

            C.Paso("A6 muro 6x3 con malla", () =>
            {
                ElementId tipoMalla = new FilteredElementCollector(C.Doc).OfClass(typeof(FabricSheetType)).FirstElementId();
                if (tipoMalla == ElementId.InvalidElementId)
                    throw new System.InvalidOperationException("No hay tipos de malla (FabricSheetType) en el proyecto: se omite la malla.");
                Wall w = C.Muro(P(X(8), Y(-10), z1), P(X(14), Y(-10), z1), C.TipoMuro(0.25), C.N1, 3.0, "MA6");
                C.Doc.Regenerate();
                FabricSheet.Create(C.Doc, w, tipoMalla);
            });
        }

        private void Barra(Element anfitrion, XYZ a, XYZ b, XYZ normal, string particion)
        {
            // Revit 2026+: los ganchos y terminaciones van en BarTerminationsData (aquí, barra recta sin ganchos).
            Rebar r = Rebar.CreateFromCurves(C.Doc, RebarStyle.Standard, _barra, anfitrion, normal,
                new List<Curve> { Line.CreateBound(a, b) }, new BarTerminationsData(C.Doc), true, true);
            if (!string.IsNullOrEmpty(particion)) Contexto.Fijar(r, BuiltInParameter.NUMBER_PARTITION_PARAM, particion);
        }
    }
}
