using ArbaModeloPrueba.Core;
using Autodesk.Revit.DB;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Zonas
{
    /// <summary>Losas macizas y aligeradas (un Floor de una capa; las aligeradas por el nombre del tipo) con vigas de apoyo.</summary>
    internal sealed class ZonaLosas : Zona
    {
        public override string Id => "LOSA";
        public override string Titulo => "Losas";
        public override string Plugins => "Acero-losas (SlabRebar) · Editor de losas · Metrados · Borrar armado";
        public override string Guia =>
            "Cara superior en Nivel 2; vigas 25x50 enrasadas arriba. L1 maciza 6x8.5 con apoyo interior (dirección y)\n" +
            "L2 aligerada 6x8.5 (tipo «Losa aligerada»), mismas vigas · L3 maciza en L con hueco · L4 dos paños en un mismo suelo\n" +
            "L5 sin vigas (bordes como apoyos) · L6 borde en arco · L7 vigas por debajo · L8 viga oblicua (se ignora con aviso)\n" +
            "Rechazos: L9 inclinada · L10 dos capas (concreto + acabado) · L11 espesor 3 cm · L12 no estructural";
        public override double X0 => 80;
        public override double Y0 => -65;

        private FloorType _maciza, _aligerada;

        protected override void Generar()
        {
            C.Paso("Tipos de losa", () =>
            {
                _maciza = C.TipoLosa(0.20);
                _aligerada = C.TipoLosa(0.20, Contexto.Prefijo + "Losa aligerada e=0.20");
            });
            if (_maciza == null) return;

            ConVigas("L1 maciza 6x8.5 con apoyo interior", "L1", _maciza, 0, 0);
            ConVigas("L2 aligerada 6x8.5 con apoyo interior", "L2", _aligerada ?? _maciza, 9, 0);

            double z2 = Cota(C.N2);
            C.Paso("L3 maciza en L con hueco", () =>
            {
                var lazos = Lista(
                    Lazo(z2, Mover(new[] { (0.0, 0.0), (7.0, 0.0), (7.0, 3.0), (3.0, 3.0), (3.0, 6.0), (0.0, 6.0) }, X(18), Y(-6))),
                    Lazo(z2, Caja(X(18 + 1), Y(-6 + 1), X(18 + 2), Y(-6 + 2))));
                C.Losa(lazos, _maciza, C.N2, "L3");
            });

            C.Paso("L4 dos paños en un mismo suelo", () =>
                C.Losa(Lista(Lazo(z2, Caja(X(0), Y(-14), X(4), Y(-10))), Lazo(z2, Caja(X(5), Y(-14), X(9), Y(-10)))), _maciza, C.N2, "L4"));

            C.Paso("L5 sin vigas (bordes como apoyos)", () =>
                C.Losa(Lista(Lazo(z2, Caja(X(11), Y(-14), X(15), Y(-10)))), C.TipoLosa(0.15), C.N2, "L5"));

            C.Paso("L6 borde en arco", () =>
            {
                var lazo = new CurveLoop();
                lazo.Append(Line.CreateBound(P(X(18), Y(-14), z2), P(X(23), Y(-14), z2)));
                lazo.Append(Arc.Create(P(X(23), Y(-14), z2), P(X(23), Y(-10), z2), P(X(25), Y(-12), z2)));
                lazo.Append(Line.CreateBound(P(X(23), Y(-10), z2), P(X(18), Y(-10), z2)));
                lazo.Append(Line.CreateBound(P(X(18), Y(-10), z2), P(X(18), Y(-14), z2)));
                C.Losa(Lista(lazo), _maciza, C.N2, "L6");
            });

            C.Paso("L7 vigas por debajo de la losa", () =>
            {
                C.Losa(Lista(Lazo(z2, Caja(X(0), Y(-22), X(5), Y(-18)))), _maciza, C.N2, "L7");
                var v = FamVigaRect(0.25, 0.40, 5);
                PonerViga(v, P(X(0), Y(-22), z2 - 0.20), P(X(5), Y(-22), z2 - 0.20), C.N2, "VL7a", 0.25, 0.40);
                PonerViga(v, P(X(0), Y(-18), z2 - 0.20), P(X(5), Y(-18), z2 - 0.20), C.N2, "VL7b", 0.25, 0.40);
            });

            C.Paso("L8 viga oblicua bajo la losa (debe ignorarse con aviso)", () =>
            {
                C.Losa(Lista(Lazo(z2, Caja(X(7), Y(-22), X(12), Y(-18)))), _maciza, C.N2, "L8");
                var v = FamVigaRect(0.25, 0.50, 5);
                PonerViga(v, P(X(7), Y(-22), z2), P(X(12), Y(-22), z2), C.N2, "VL8a", 0.25, 0.50);
                PonerViga(v, P(X(7), Y(-18), z2), P(X(12), Y(-18), z2), C.N2, "VL8b", 0.25, 0.50);
                double d = System.Math.Sqrt(2) * 3;
                C.Viga(C.Fam.Obtener(FamVigaRect(0.25, 0.50, d)), P(X(8), Y(-21.5), z2), P(X(11), Y(-18.5), z2), C.N2, "VL8-oblicua");
            });

            // ---- rechazos
            C.Paso("L9 inclinada (rechazo)", () =>
            {
                Line flecha = Line.CreateBound(P(X(14), Y(-22), z2), P(X(14), Y(-18), z2));
                Floor f = Floor.Create(C.Doc, Lista(Lazo(z2, Caja(X(14), Y(-22), X(18), Y(-18)))), _maciza.Id, C.N2.Id, true, flecha, 0.10);
                C.Marca(f, "L9");
            });
            C.Paso("L10 dos capas: concreto + acabado (rechazo)", () =>
                C.Losa(Lista(Lazo(z2, Caja(X(20), Y(-22), X(24), Y(-18)))), C.TipoLosaConAcabado(0.20, 0.05), C.N2, "L10"));
            C.Paso("L11 espesor 3 cm (rechazo)", () =>
                C.Losa(Lista(Lazo(z2, Caja(X(0), Y(-28), X(4), Y(-25)))), C.TipoLosa(0.03), C.N2, "L11"));
            C.Paso("L12 no estructural (rechazo)", () =>
                C.Losa(Lista(Lazo(z2, Caja(X(6), Y(-28), X(10), Y(-25)))), _maciza, C.N2, "L12", estructural: false));
        }

        /// <summary>Losa 6 (x) x 8.5 (y) con vigas según X en y = 0, 4.25 y 8.5 (apoyos de barras según y) y bordes según Y.</summary>
        private void ConVigas(string que, string marca, FloorType tipo, double dx, double dy)
        {
            double z2 = Cota(C.N2);
            C.Paso(que, () =>
            {
                double x0 = X(dx), y0 = Y(dy - 8.5);
                C.Losa(Lista(Lazo(z2, Caja(x0, y0, x0 + 6, y0 + 8.5))), tipo, C.N2, marca);
                var porX = FamVigaRect(0.25, 0.50, 6);
                var porY = FamVigaRect(0.25, 0.50, 8.5);
                string[] n = { "a", "b", "c" };
                for (int i = 0; i < 3; i++)
                    PonerViga(porX, P(x0, y0 + 4.25 * i, z2), P(x0 + 6, y0 + 4.25 * i, z2), C.N2, "V" + marca + n[i], 0.25, 0.50);
                PonerViga(porY, P(x0, y0, z2), P(x0, y0 + 8.5, z2), C.N2, "V" + marca + "d", 0.25, 0.50);
                PonerViga(porY, P(x0 + 6, y0, z2), P(x0 + 6, y0 + 8.5, z2), C.N2, "V" + marca + "e", 0.25, 0.50);
            });
        }
    }
}
