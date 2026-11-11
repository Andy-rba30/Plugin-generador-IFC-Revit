using System.Linq;
using ArbaModeloPrueba.Core;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Zonas
{
    /// <summary>
    /// Metrados: una pieza de cada grupo y material (concreto, acero por material y por nombre, madera, conexiones,
    /// otros, cubiertas, losa con acabado) y dos tablas de planificación para exportar.
    /// </summary>
    internal sealed class ZonaMetrados : Zona
    {
        public override string Id => "MET";
        public override string Titulo => "Metrados";
        public override string Plugins => "Exportación de metrados a Excel (exportar, metrado automático, asignar partición, encofrado)";
        public override string Guia =>
            "Concreto: K1 columna circular · K2 f'c 280 · K3 material con activo Concrete sin la palabra · K4 losa con acabado (solo cuenta el concreto) · K5 cubierta de concreto\n" +
            "Acero: S1 W12X26 (A36) · S2 HSS6X6X1/4 (S355JR) · S3 «W12X26» sin material · S4 columna W · S5 cubierta metálica\n" +
            "Conexiones: Q1 «PL 12mm» · Q2 «Perno M20» · Q3 «ESPARRAGO 5-8» · Q4 rigidizador · Q5 conexión estructural\n" +
            "Otros: G1 pedestal genérico de concreto · G2 genérico metálico · W1 viga de madera · Tablas de columnas y vigas";
        public override double X0 => 60;
        public override double Y0 => -115;

        protected override void Generar()
        {
            double z2 = Cota(C.N2);

            C.Paso("K1 columna circular D=50", () =>
            {
                var f = new FamiliaSpec
                {
                    Nombre = Contexto.Prefijo + "Columna circular D50 h=3.00",
                    Categoria = BuiltInCategory.OST_StructuralColumns,
                    Material = Materiales.Concreto,
                    Geometria = () => Solidos.Cilindro(0, 0, 0.25, 0, 3.0)
                };
                C.Columna(C.Fam.Obtener(f), X(0), Y(0), C.N1, C.N2, "K1");
            });
            C.Paso("K2 columna f'c 280", () =>
                C.Columna(C.Fam.Obtener(FamColumna("40x40 fc280", Rect(0, 0, 0.4, 0.4), 3.0, Materiales.Concreto280)), X(3), Y(0), C.N1, C.N2, "K2"));
            C.Paso("K3 columna con material «Mezcla estructural» (activo Concrete)", () =>
                C.Columna(C.Fam.Obtener(FamColumna("40x40 mezcla", Rect(0, 0, 0.4, 0.4), 3.0, Materiales.ConcretoSinNombre)), X(6), Y(0), C.N1, C.N2, "K3"));
            C.Paso("K4 losa con capa de acabado", () =>
                C.Losa(Lista(Lazo(Cota(C.N1), Caja(X(9), Y(-2), X(14), Y(2)))), C.TipoLosaConAcabado(0.20, 0.05), C.N1, "K4"));
            C.Paso("K5 cubierta de concreto", () => Cubierta("K5", Materiales.Concreto, 0.15, X(16), Y(-2)));

            // ---- acero
            var perfilW = new[] { (-0.083, 0.0), (0.083, 0.0), (0.083, -0.010), (0.003, -0.010), (0.003, -0.300), (0.083, -0.300), (0.083, -0.310),
                                  (-0.083, -0.310), (-0.083, -0.300), (-0.003, -0.300), (-0.003, -0.010), (-0.083, -0.010) };
            C.Paso("S1 viga W12X26 de acero A36", () =>
                C.Viga(C.Fam.Obtener(Perfil("W12X26", perfilW, Materiales.Acero)), P(X(0), Y(-6), z2), P(X(6), Y(-6), z2), C.N2, "S1"));
            C.Paso("S2 viga HSS6X6X1/4 de acero S355JR", () =>
            {
                var f = new FamiliaSpec
                {
                    Nombre = "HSS6X6X1/4",
                    Categoria = BuiltInCategory.OST_StructuralFraming,
                    Material = Materiales.AceroS355,
                    TipoMaterial = StructuralMaterialType.Steel,
                    Geometria = () => Solidos.Resta(Solidos.Caja(0, -0.076, -0.152, 6, 0.076, 0), Solidos.Caja(-1, -0.070, -0.146, 7, 0.070, -0.006))
                };
                C.Viga(C.Fam.Obtener(f), P(X(0), Y(-8), z2), P(X(6), Y(-8), z2), C.N2, "S2");
            });
            C.Paso("S3 viga «W12X26» sin material", () =>
                C.Viga(C.Fam.Obtener(Perfil("W12X26 sin material", perfilW, null, StructuralMaterialType.Other)), P(X(0), Y(-10), z2), P(X(6), Y(-10), z2), C.N2, "S3"));
            C.Paso("S4 columna W de acero", () =>
            {
                var planta = new[] { (-0.083, -0.155), (0.083, -0.155), (0.083, -0.145), (0.003, -0.145), (0.003, 0.145), (0.083, 0.145), (0.083, 0.155),
                                     (-0.083, 0.155), (-0.083, 0.145), (-0.003, 0.145), (-0.003, -0.145), (-0.083, -0.145) };
                var f = new FamiliaSpec
                {
                    Nombre = "W12X26 columna",
                    Categoria = BuiltInCategory.OST_StructuralColumns,
                    Material = Materiales.Acero,
                    TipoMaterial = StructuralMaterialType.Steel,
                    Geometria = () => Solidos.PrismaZ(planta, 0, 3.0)
                };
                C.Columna(C.Fam.Obtener(f), X(9), Y(-8), C.N1, C.N2, "S4");
            });
            C.Paso("S5 cubierta metálica", () => Cubierta("S5", Materiales.Acero, 0.01, X(16), Y(-8)));

            // ---- conexiones (por nombre y por categoría)
            C.Paso("Q1 viga «PL 12mm»", () =>
                C.Viga(C.Fam.Obtener(Perfil("PL 12mm", new[] { (-0.10, 0.0), (0.10, 0.0), (0.10, -0.012), (-0.10, -0.012) }, Materiales.Acero)),
                       P(X(0), Y(-11.5), z2), P(X(6), Y(-11.5), z2), C.N2, "Q1"));
            C.Paso("Q2 columna «Perno M20»", () =>
            {
                var f = new FamiliaSpec
                {
                    Nombre = "Perno M20", Categoria = BuiltInCategory.OST_StructuralColumns, Material = Materiales.Acero,
                    TipoMaterial = StructuralMaterialType.Steel, Geometria = () => Solidos.Cilindro(0, 0, 0.01, 0, 0.30)
                };
                C.Columna(C.Fam.Obtener(f), X(2), Y(-13), C.N1, C.N2, "Q2");
            });
            C.Paso("Q3 columna «ESPARRAGO 5-8»", () =>
            {
                var f = new FamiliaSpec
                {
                    Nombre = "ESPARRAGO 5-8", Categoria = BuiltInCategory.OST_StructuralColumns, Material = Materiales.Acero,
                    TipoMaterial = StructuralMaterialType.Steel, Geometria = () => Solidos.Cilindro(0, 0, 0.008, 0, 0.40)
                };
                C.Columna(C.Fam.Obtener(f), X(4), Y(-13), C.N1, C.N2, "Q3");
            });
            C.Paso("Q4 rigidizador (categoría rigidizadores estructurales)", () =>
                C.Pieza(C.Fam.Obtener(Pieza("Rigidizador 10mm", BuiltInCategory.OST_StructuralStiffener, Materiales.Acero,
                    () => Solidos.Caja(-0.1, -0.005, 0, 0.1, 0.005, 0.3))), X(6), Y(-13), C.N1, "Q4"));
            C.Paso("Q5 conexión estructural (categoría conexiones)", () =>
                C.Pieza(C.Fam.Obtener(Pieza("Placa base 30x30", BuiltInCategory.OST_StructConnections, Materiales.Acero,
                    () => Solidos.Caja(-0.15, -0.15, 0, 0.15, 0.15, 0.02))), X(8), Y(-13), C.N1, "Q5"));

            // ---- otros
            C.Paso("G1 pedestal (modelo genérico de concreto)", () =>
                C.Pieza(C.Fam.Obtener(Pieza("Pedestal genérico 60x60x80", BuiltInCategory.OST_GenericModel, Materiales.Concreto,
                    () => Solidos.Caja(-0.3, -0.3, 0, 0.3, 0.3, 0.8))), X(0), Y(-17), C.N1, "G1"));
            C.Paso("G2 modelo genérico metálico", () =>
                C.Pieza(C.Fam.Obtener(Pieza("Soporte genérico metálico", BuiltInCategory.OST_GenericModel, Materiales.Acero,
                    () => Solidos.Caja(-0.1, -0.1, 0, 0.1, 0.1, 1.0))), X(3), Y(-17), C.N1, "G2"));
            C.Paso("W1 viga de madera 15x30", () =>
                C.Viga(C.Fam.Obtener(FamViga("15x30 madera", PerfilRect(0.15, 0.30), 5, Materiales.Madera, StructuralMaterialType.Wood)),
                       P(X(6), Y(-17), z2), P(X(11), Y(-17), z2), C.N2, "W1"));

            // ---- tablas para exportar
            C.Paso("Tablas de planificación de columnas y vigas", () =>
            {
                Tabla("PRUEBA ARBA - Tabla de columnas", BuiltInCategory.OST_StructuralColumns);
                Tabla("PRUEBA ARBA - Tabla de vigas", BuiltInCategory.OST_StructuralFraming);
            });
        }

        private static FamiliaSpec Perfil(string nombre, (double y, double z)[] perfil, string material,
                                          StructuralMaterialType tipo = StructuralMaterialType.Steel) => new FamiliaSpec
        {
            Nombre = nombre,
            Categoria = BuiltInCategory.OST_StructuralFraming,
            Material = material,
            TipoMaterial = tipo,
            Geometria = () => Solidos.PrismaX(perfil, 0, 6)
        };

        private static FamiliaSpec Pieza(string nombre, BuiltInCategory cat, string material, System.Func<Solid> g) => new FamiliaSpec
        {
            Nombre = Contexto.Prefijo + nombre,
            Categoria = cat,
            Material = material,
            TipoMaterial = material == Materiales.Concreto ? StructuralMaterialType.Concrete : StructuralMaterialType.Steel,
            Geometria = g
        };

        private void Cubierta(string marca, string material, double espesor, double x, double y)
        {
            RoofType basico = new FilteredElementCollector(C.Doc).OfClass(typeof(RoofType)).Cast<RoofType>()
                .FirstOrDefault(r => r.GetCompoundStructure() != null)
                ?? throw new System.InvalidOperationException("El proyecto no tiene ningún tipo de cubierta básica.");
            string nombre = Contexto.Prefijo + "Cubierta " + material.Replace(Contexto.Prefijo, "") + " e=" + F(espesor);
            RoofType tipo = new FilteredElementCollector(C.Doc).OfClass(typeof(RoofType)).Cast<RoofType>().FirstOrDefault(r => r.Name == nombre);
            if (tipo == null)
            {
                tipo = (RoofType)basico.Duplicate(nombre);
                tipo.SetCompoundStructure(CompoundStructure.CreateSingleLayerCompoundStructure(MaterialFunctionAssignment.Structure, M(espesor), C.Mat(material)));
            }
            var contorno = new CurveArray();
            foreach (Curve c in Lazo(Cota(C.N3), Caja(x, y, x + 5, y + 4)))
                contorno.Append(c);
            FootPrintRoof r = C.Doc.Create.NewFootPrintRoof(contorno, C.N3, tipo, out ModelCurveArray _);
            C.Marca(r, marca);
        }

        private void Tabla(string nombre, BuiltInCategory cat)
        {
            if (new FilteredElementCollector(C.Doc).OfClass(typeof(ViewSchedule)).Any(v => v.Name == nombre)) return;
            ViewSchedule t = ViewSchedule.CreateSchedule(C.Doc, new ElementId(cat));
            t.Name = nombre;
            ScheduleDefinition def = t.Definition;
            foreach (SchedulableField sf in def.GetSchedulableFields())
            {
                long id = sf.ParameterId.Value;
                if (id == (long)BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM || id == (long)BuiltInParameter.ALL_MODEL_MARK ||
                    id == (long)BuiltInParameter.STRUCTURAL_MATERIAL_PARAM || id == (long)BuiltInParameter.HOST_VOLUME_COMPUTED)
                    def.AddField(sf);
            }
        }
    }
}
