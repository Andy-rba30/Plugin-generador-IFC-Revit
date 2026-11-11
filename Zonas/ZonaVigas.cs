using System;
using ArbaModeloPrueba.Core;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Zonas
{
    /// <summary>
    /// Vigas: pórtico de 3 tramos + voladizo (un elemento por tramo, unido a las columnas), losa unida para el modo
    /// "cut", todas las secciones del perfil (T, L, T invertida, I, cartelas, escalón), viga larga para empalmes,
    /// viga inclinada y rechazos.
    /// </summary>
    internal sealed class ZonaVigas : Zona
    {
        public override string Id => "VIG";
        public override string Titulo => "Vigas";
        public override string Plugins => "Acero-vigas (BeamRebar) · Uniones · Acero-losas (apoyos) · Metrados · Borrar armado";
        public override string Guia =>
            "Cara superior en Nivel 2. V1-V3 pórtico de 3 tramos de 6 m sobre columnas 40x40 (unidas; probar joinedGeometry auto/cut/full)\n" +
            "V4 voladizo 2 m · losa e=0.20 unida a V1-V3 (modo cut) · V5 T · V6 L · V7 T invertida · V8 I\n" +
            "V9 cartelas en los dos extremos · V10 escalón de canto · V11 de 15 m (empalmes) · V12 inclinada\n" +
            "Rechazos: V13 eje en arco · V14 ancho de alma variable · V15 de acero · V16 dos sólidos";
        public override double X0 => 35;
        public override double Y0 => -65;

        protected override void Generar()
        {
            double z2 = Cota(C.N2);
            var col = FamColumnaRect(0.40, 0.40, 3.00);
            var secCol = Rect(0, 0, 0.40, 0.40);
            var viga = FamVigaRect(0.25, 0.50, 6);

            FamilyInstance[] cols = new FamilyInstance[4];
            C.Paso("Columnas del pórtico", () =>
            {
                for (int i = 0; i < 4; i++)
                    cols[i] = PonerColumna(col, secCol, X(6 * i), Y(0), C.N1, C.N2, "C-V" + (i + 1));
            });

            FamilyInstance[] tramos = new FamilyInstance[3];
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                C.Paso("V" + (k + 1) + " tramo " + (k + 1) + " del pórtico (unida a sus columnas)", () =>
                {
                    tramos[k] = PonerViga(viga, P(X(6 * k), Y(0), z2), P(X(6 * k + 6), Y(0), z2), C.N2, "V" + (k + 1), 0.25, 0.50);
                    C.Doc.Regenerate();
                    if (cols[k] != null) C.Unir(cols[k], tramos[k]);
                    if (cols[k + 1] != null) C.Unir(cols[k + 1], tramos[k]);
                });
            }

            C.Paso("V4 voladizo de 2 m", () =>
            {
                FamilyInstance v = PonerViga(FamVigaRect(0.25, 0.50, 2), P(X(18), Y(0), z2), P(X(20), Y(0), z2), C.N2, "V4", 0.25, 0.50);
                C.Doc.Regenerate();
                if (cols[3] != null) C.Unir(cols[3], v);
            });

            C.Paso("Losa e=0.20 unida a V1-V3 (corta las vigas)", () =>
            {
                Floor l = C.Losa(Lista(Lazo(z2, Caja(X(0), Y(-2), X(18), Y(2)))), C.TipoLosa(0.20), C.N2, "L-V");
                C.Doc.Regenerate();
                foreach (FamilyInstance t in tramos)
                    if (t != null) C.Unir(l, t);
            });

            // ---- secciones del perfil (vigas sueltas de 6 m)
            Seccion("V5 sección en T", "V5", "T 80x60", new[] { (-0.4, 0.0), (0.4, 0.0), (0.4, -0.2), (0.125, -0.2), (0.125, -0.6), (-0.125, -0.6), (-0.125, -0.2), (-0.4, -0.2) }, -6, 0.80, 0.60);
            Seccion("V6 sección en L", "V6", "L 68x60", new[] { (-0.125, 0.0), (0.55, 0.0), (0.55, -0.2), (0.125, -0.2), (0.125, -0.6), (-0.125, -0.6) }, -9, 0.675, 0.60, centroY: 0.2125);
            Seccion("V7 sección en T invertida", "V7", "T invertida 80x60", new[] { (-0.125, 0.0), (0.125, 0.0), (0.125, -0.4), (0.4, -0.4), (0.4, -0.6), (-0.4, -0.6), (-0.4, -0.4), (-0.125, -0.4) }, -12, 0.80, 0.60);
            Seccion("V8 sección en I", "V8", "I 60x70", new[] { (-0.3, 0.0), (0.3, 0.0), (0.3, -0.15), (0.125, -0.15), (0.125, -0.55), (0.3, -0.55), (0.3, -0.7),
                                                               (-0.3, -0.7), (-0.3, -0.55), (-0.125, -0.55), (-0.125, -0.15), (-0.3, -0.15) }, -15, 0.60, 0.70);

            C.Paso("V9 cartelas rectas en los dos extremos", () =>
            {
                var lado = new[] { (0.0, 0.0), (7.0, 0.0), (7.0, -0.8), (5.8, -0.5), (1.2, -0.5), (0.0, -0.8) };
                var f = new FamiliaSpec
                {
                    Nombre = Contexto.Prefijo + "Viga cartelas 25x50-80 L=7.00",
                    Categoria = BuiltInCategory.OST_StructuralFraming,
                    Material = Materiales.Concreto,
                    Geometria = () => Solidos.PrismaY(lado, -0.125, 0.125)
                };
                PonerViga(f, P(X(0), Y(-18), z2), P(X(7), Y(-18), z2), C.N2, "V9", 0.25, 0.80);
            });

            C.Paso("V10 escalón de canto a media luz", () =>
            {
                var lado = new[] { (0.0, 0.0), (6.0, 0.0), (6.0, -0.7), (3.0, -0.7), (3.0, -0.5), (0.0, -0.5) };
                var f = new FamiliaSpec
                {
                    Nombre = Contexto.Prefijo + "Viga escalón 25x50-70 L=6.00",
                    Categoria = BuiltInCategory.OST_StructuralFraming,
                    Material = Materiales.Concreto,
                    Geometria = () => Solidos.PrismaY(lado, -0.125, 0.125)
                };
                PonerViga(f, P(X(0), Y(-21), z2), P(X(6), Y(-21), z2), C.N2, "V10", 0.25, 0.70);
            });

            C.Paso("V11 viga de 15 m (empalmes)", () =>
                PonerViga(FamVigaRect(0.30, 0.60, 15), P(X(0), Y(-24), z2), P(X(15), Y(-24), z2), C.N2, "V11", 0.30, 0.60));

            C.Paso("V12 viga inclinada (sube 1 m en 6 m)", () =>
            {
                double largo = Math.Sqrt(6 * 6 + 1 * 1);
                C.Viga(C.Fam.Obtener(FamVigaRect(0.25, 0.50, largo)), P(X(0), Y(-27), z2 - 1), P(X(6), Y(-27), z2), C.N2, "V12");
            });

            // ---- rechazos
            C.Paso("V13 eje en arco (rechazo)", () =>
            {
                Arc arco = Arc.Create(P(X(10), Y(-27), z2), P(X(16), Y(-27), z2), P(X(13), Y(-25.5), z2));
                C.Doc.Create.NewFamilyInstance(arco, C.Fam.Obtener(FamVigaRect(0.25, 0.50, 6)), C.N2, StructuralType.Beam);
            });

            C.Paso("V14 ancho de alma variable (rechazo)", () =>
            {
                var planta = new[] { (0.0, -0.10), (6.0, -0.20), (6.0, 0.20), (0.0, 0.10) };
                var f = new FamiliaSpec
                {
                    Nombre = Contexto.Prefijo + "Viga ancho variable L=6.00",
                    Categoria = BuiltInCategory.OST_StructuralFraming,
                    Material = Materiales.Concreto,
                    Geometria = () => Solidos.PrismaZ(planta, -0.5, 0)
                };
                C.Viga(C.Fam.Obtener(f), P(X(10), Y(-6), z2), P(X(16), Y(-6), z2), C.N2, "V14");
            });

            C.Paso("V15 de acero (rechazo: material)", () =>
                C.Viga(C.Fam.Obtener(FamViga("30x50 acero", PerfilRect(0.30, 0.50), 6, Materiales.Acero, StructuralMaterialType.Steel)),
                       P(X(10), Y(-9), z2), P(X(16), Y(-9), z2), C.N2, "V15"));

            C.Paso("V16 dos sólidos en la familia (rechazo)", () =>
            {
                var f = new FamiliaSpec
                {
                    Nombre = Contexto.Prefijo + "Viga dos sólidos L=6.00",
                    Categoria = BuiltInCategory.OST_StructuralFraming,
                    Material = Materiales.Concreto,
                    Varios = () => new[] { Solidos.Caja(0, -0.125, -0.5, 2.9, 0.125, 0), Solidos.Caja(3.1, -0.125, -0.5, 6, 0.125, 0) }
                };
                C.Viga(C.Fam.Obtener(f), P(X(10), Y(-12), z2), P(X(16), Y(-12), z2), C.N2, "V16");
            });
        }

        private void Seccion(string que, string marca, string nombre, (double y, double z)[] perfil, double dy, double ancho, double canto, double centroY = 0)
        {
            double z2 = Cota(C.N2);
            C.Paso(que, () =>
            {
                FamilyInstance v = C.Viga(C.Fam.Obtener(FamViga(nombre, perfil, 6)), P(X(0), Y(dy), z2), P(X(6), Y(dy), z2), C.N2, marca);
                C.Encajar(v, X(0), Y(dy) + centroY - ancho / 2, z2 - canto, X(6), Y(dy) + centroY + ancho / 2, z2, marca, mover: false);
            });
        }
    }
}
