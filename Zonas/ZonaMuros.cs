using ArbaModeloPrueba.Core;
using Autodesk.Revit.DB;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Zonas
{
    /// <summary>
    /// Muros de contención como familias de cimentación de un único sólido (zapata + alzado), que es lo que arma
    /// Acero-automatico. Zapata de z = -0.60 a 0 (cara superior en el nivel Cimentación), alzado hasta +3.00.
    /// </summary>
    internal sealed class ZonaMuros : Zona
    {
        public override string Id => "MC";
        public override string Titulo => "Muros de contención";
        public override string Plugins => "Acero-automatico (RetainingWallRebar) · Encofrado-automatico · Metrados";
        public override string Guia =>
            "M1 T simétrico · M2 L (un trasdós) · M3 T con puntera 0.60 y talón 1.50 · M4 esquinero en L de una pieza\n" +
            "M5 vano de puerta (apoyado en la zapata) · M6 vano de ventana · M7+M8 cruzados (unidos) · M9+M10 unidos en esquina\n" +
            "M11 alzado de 5 m (horizontales en 1-3 tramos)\n" +
            "Rechazos: M12 contrafuertes · M13 alzado escalonado · M14 zapata y alzado en dos sólidos · M15 muro de sistema sin zapata";
        public override double X0 => 125;
        public override double Y0 => 0;

        public const double Hz = 0.60, Ha = 3.00;

        /// <summary>Sección (y, z) de un muro en T con puntera, talón y alzado de espesor e (el eje del alzado en y = 0).</summary>
        public static (double, double)[] SeccionT(double puntera, double talon, double e, double hz = Hz, double ha = Ha) => new[]
        {
            (-e / 2 - puntera, -hz), (e / 2 + talon, -hz), (e / 2 + talon, 0.0), (e / 2, 0.0),
            (e / 2, ha), (-e / 2, ha), (-e / 2, 0.0), (-e / 2 - puntera, 0.0)
        };

        public static FamiliaSpec FamMuroT(string nombre, double puntera, double talon, double e, double largo, double ha = Ha) =>
            FamCimentacion("Muro contención " + nombre + " L=" + F(largo), () => Solidos.PrismaX(SeccionT(puntera, talon, e, Hz, ha), 0, largo));

        protected override void Generar()
        {
            Level cim = C.Cim;

            Recto("M1 T simétrico", "M1", FamMuroT("T 260", 1.15, 1.15, 0.30, 6), 0, 0, 1.30, 1.30);
            Recto("M2 L (un solo trasdós)", "M2", FamMuroT("L 230", 0, 2.00, 0.30, 6), 0, -6, 0.15, 2.15);
            Recto("M3 T puntera 0.60 talón 1.50", "M3", FamMuroT("T 240 asimétrico", 0.60, 1.50, 0.30, 6), 0, -12, 0.75, 1.65);

            C.Paso("M4 esquinero en L de una sola pieza", () =>
            {
                var f = FamCimentacion("Muro contención esquinero L 6+6", () => Solidos.Union(
                    Solidos.Caja(-1.3, -1.3, -Hz, 6, 1.3, 0),
                    Solidos.Caja(-1.3, -1.3, -Hz, 1.3, 6, 0),
                    Solidos.Caja(-0.15, -0.15, 0, 6, 0.15, Ha),
                    Solidos.Caja(-0.15, -0.15, 0, 0.15, 6, Ha)));
                PonerZapata(f, X(0), Y(-26), cim, "M4", -1.3, -1.3, 6, 6, -Hz, Ha);
            });

            C.Paso("M5 vano de puerta apoyado en la zapata", () =>
            {
                var f = FamCimentacion("Muro contención T con puerta L=8", () => Solidos.Resta(
                    Solidos.PrismaX(SeccionT(1.15, 1.15, 0.30), 0, 8),
                    Solidos.Caja(3.0, -0.5, 0, 4.2, 0.5, 2.1)));
                PonerZapata(f, X(12), Y(0), cim, "M5", 0, -1.30, 8, 1.30, -Hz, Ha);
            });

            C.Paso("M6 vano de ventana", () =>
            {
                var f = FamCimentacion("Muro contención T con ventana L=8", () => Solidos.Resta(
                    Solidos.PrismaX(SeccionT(1.15, 1.15, 0.30), 0, 8),
                    Solidos.Caja(3.0, -0.5, 1.0, 4.5, 0.5, 2.0)));
                PonerZapata(f, X(12), Y(-6), cim, "M6", 0, -1.30, 8, 1.30, -Hz, Ha);
            });

            C.Paso("M7 + M8 muros cruzados y unidos", () =>
            {
                FamilySymbol t = C.Fam.Obtener(FamMuroT("T 260", 1.15, 1.15, 0.30, 8));
                FamilyInstance a = C.Zapata(t, X(12), Y(-14), cim, "M7");
                FamilyInstance b = C.Zapata(t, X(16), Y(-18), cim, "M8", 90);
                C.Doc.Regenerate();
                C.Unir(a, b);
            });

            C.Paso("M9 + M10 muros unidos en esquina", () =>
            {
                FamilySymbol t = C.Fam.Obtener(FamMuroT("T 260", 1.15, 1.15, 0.30, 6));
                FamilyInstance a = C.Zapata(t, X(12), Y(-26), cim, "M9");
                FamilyInstance b = C.Zapata(t, X(12), Y(-26), cim, "M10", 90);
                C.Doc.Regenerate();
                C.Unir(a, b);
            });

            Recto("M11 alzado de 5 m", "M11", FamMuroT("T 320 h5", 1.40, 1.40, 0.40, 6, 5.0), 24, 0, 1.60, 1.60, 5.0);

            // ---- rechazos
            C.Paso("M12 con contrafuertes (rechazo)", () =>
            {
                var f = FamCimentacion("Muro contención con contrafuertes L=6", () =>
                {
                    Solid s = Solidos.PrismaX(SeccionT(1.15, 1.15, 0.30), 0, 6);
                    var cartela = new[] { (0.15, 0.0), (1.30, 0.0), (0.15, 2.5) };
                    foreach (double x in new[] { 1.0, 4.75 })
                        s = Solidos.Union(s, Solidos.PrismaX(cartela, x, x + 0.25));
                    return s;
                });
                PonerZapata(f, X(24), Y(-8), cim, "M12", 0, -1.30, 6, 1.30, -Hz, Ha);
            });

            C.Paso("M13 alzado escalonado (rechazo)", () =>
            {
                var f = FamCimentacion("Muro contención alzado escalonado L=6", () => Solidos.Union(
                    Solidos.PrismaX(SeccionT(1.10, 1.10, 0.40, Hz, 1.5), 0, 6),
                    Solidos.Caja(0, -0.125, 1.5, 6, 0.125, Ha)));
                PonerZapata(f, X(24), Y(-14), cim, "M13", 0, -1.30, 6, 1.30, -Hz, Ha);
            });

            C.Paso("M14 zapata y alzado en dos sólidos (rechazo)", () =>
            {
                var f = new FamiliaSpec
                {
                    Nombre = Contexto.Prefijo + "Muro contención dos sólidos L=6",
                    Categoria = BuiltInCategory.OST_StructuralFoundation,
                    Material = Materiales.Concreto,
                    Varios = () => new[]
                    {
                        Solidos.Caja(0, -1.3, -Hz, 6, 1.3, 0),
                        Solidos.Caja(0, -0.15, 0, 6, 0.15, Ha)
                    }
                };
                PonerZapata(f, X(24), Y(-20), cim, "M14", 0, -1.30, 6, 1.30, -Hz, Ha);
            });

            C.Paso("M15 muro de sistema sin zapata", () =>
            {
                double z = Cota(cim);
                C.Muro(P(X(24), Y(-26), z), P(X(30), Y(-26), z), C.TipoMuro(0.30), cim, Ha, "M15");
            });
        }

        private void Recto(string que, string marca, FamiliaSpec f, double dx, double dy, double yMenos, double yMas, double ha = Ha)
        {
            C.Paso(que, () => PonerZapata(f, X(dx), Y(dy), C.Cim, marca, 0, -yMenos, f.Nombre.Contains("L=8") ? 8 : 6, yMas, -Hz, ha));
        }
    }
}
