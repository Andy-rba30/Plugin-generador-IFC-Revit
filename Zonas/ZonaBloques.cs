using ArbaModeloPrueba.Core;
using Autodesk.Revit.DB;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Zonas
{
    /// <summary>Bloques de cimentación con fosos o canaletas (Fosa_transformadores): un único sólido con los fosos ya restados.</summary>
    internal sealed class ZonaBloques : Zona
    {
        public override string Id => "BLQ";
        public override string Titulo => "Bloques con foso (transformadores)";
        public override string Plugins => "Fosa_transformadores (BlockRebar) · Metrados · Excavaciones";
        public override string Guia =>
            "Cara superior en Nivel 1. B1 foso perimetral con murete (4.80x3.80x1.30, foso 0.60x0.80) · B2 canaleta en un lado\n" +
            "B3 foso central sin murete · B4 dos fosos de distinta profundidad\n" +
            "Rechazos: B5 sin foso (remite a Zapatas) · B6 voladizo (cara inferior a cota intermedia)\n" +
            "Rejillas/ángulos: botón «Colocar rejillas y ángulos» (necesita la familia L-Angle de la biblioteca de Revit).";
        public override double X0 => 90;
        public override double Y0 => 0;

        protected override void Generar()
        {
            Level n1 = C.N1;

            C.Paso("B1 foso perimetral con murete", () =>
            {
                var f = FamCimentacion("Bloque foso perimetral 480x380x130", () =>
                {
                    Solid bloque = Solidos.Caja(-2.40, -1.90, -1.30, 2.40, 1.90, 0);
                    Solid anillo = Solidos.Resta(Solidos.Caja(-2.25, -1.75, -0.80, 2.25, 1.75, 0.5),
                                                 Solidos.Caja(-1.65, -1.15, -1.0, 1.65, 1.15, 0.6));
                    return Solidos.Resta(bloque, anillo);
                });
                PonerZapata(f, X(0), Y(0), n1, "B1", -2.40, -1.90, 2.40, 1.90, -1.30);
            });

            C.Paso("B2 canaleta en un lado, abierta en los extremos", () =>
            {
                var f = FamCimentacion("Bloque canaleta 400x300x120", () => Solidos.Resta(
                    Solidos.Caja(-2.0, -1.5, -1.20, 2.0, 1.5, 0),
                    Solidos.Caja(-2.5, 0.75, -0.50, 2.5, 1.35, 0.5)));
                PonerZapata(f, X(8), Y(0), n1, "B2", -2.0, -1.5, 2.0, 1.5, -1.20);
            });

            C.Paso("B3 foso central sin murete", () =>
            {
                var f = FamCimentacion("Bloque foso central 400x300x120", () => Solidos.Resta(
                    Solidos.Caja(-2.0, -1.5, -1.20, 2.0, 1.5, 0),
                    Solidos.Caja(-0.75, -0.50, -0.70, 0.75, 0.50, 0.5)));
                PonerZapata(f, X(16), Y(0), n1, "B3", -2.0, -1.5, 2.0, 1.5, -1.20);
            });

            C.Paso("B4 dos fosos de distinta profundidad", () =>
            {
                var f = FamCimentacion("Bloque dos fosos 500x300x120", () => Solidos.Resta(
                    Solidos.Caja(-2.5, -1.5, -1.20, 2.5, 1.5, 0),
                    Solidos.Caja(-1.7, -0.5, -0.60, -0.7, 0.5, 0.5),
                    Solidos.Caja(0.7, -0.5, -0.90, 1.7, 0.5, 0.5)));
                PonerZapata(f, X(0), Y(-8), n1, "B4", -2.5, -1.5, 2.5, 1.5, -1.20);
            });

            C.Paso("B5 bloque sin foso (rechazo)", () =>
            {
                var f = FamCimentacion("Bloque macizo 300x300x100", () => Solidos.Caja(-1.5, -1.5, -1.0, 1.5, 1.5, 0));
                PonerZapata(f, X(8), Y(-8), n1, "B5", -1.5, -1.5, 1.5, 1.5, -1.0);
            });

            C.Paso("B6 bloque con voladizo (rechazo)", () =>
            {
                var f = FamCimentacion("Bloque con voladizo", () => Solidos.Resta(
                    Solidos.Union(Solidos.Caja(-2.0, -1.5, -0.40, 2.0, 1.5, 0), Solidos.Caja(-1.0, -1.0, -1.20, 1.0, 1.0, -0.40)),
                    Solidos.Caja(-0.4, -0.4, -0.30, 0.4, 0.4, 0.5)));
                PonerZapata(f, X(16), Y(-8), n1, "B6", -2.0, -1.5, 2.0, 1.5, -1.20);
            });
        }
    }
}
