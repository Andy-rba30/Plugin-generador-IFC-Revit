using System.Collections.Generic;
using ArbaModeloPrueba.Core;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Zonas
{
    /// <summary>Cimientos corridos y sobrecimientos: cadenas de muros con cimentación de muro, suelos en franja y vigas de cimentación.</summary>
    internal sealed class ZonaCimientos : Zona
    {
        public override string Id => "CC";
        public override string Titulo => "Cimientos corridos y sobrecimientos";
        public override string Plugins => "Acero-cimientos-corridos · Acero-Zapatas (corrida) · Excavaciones · Metrados";
        public override string Guia =>
            "Sobrecimiento = muro 0.25 de Cimentación a Nivel 1; cimiento = cimentación de muro 0.60x0.50.\n" +
            "CC1 recto · CC2 L · CC3 U · CC4 anillo · CC5 T · CC6 cruce · CC7 escalón de fondo · CC8 muro partido en T\n" +
            "CC9-11 suelo en L, T y cruz · CC12 viga de cimentación · CC13 T invertida · CC14 escalonada · CC15 muro con columna unida\n" +
            "Rechazos: CC16 muro curvo · CC17 suelo no estructural · CC18 suelo con borde en arco";
        public override double X0 => 45;
        public override double Y0 => 0;

        private WallType _sc;
        private WallFoundationType _cc;

        protected override void Generar()
        {
            C.Paso("Tipos de sobrecimiento y cimiento", () =>
            {
                _sc = C.TipoMuro(0.25);
                _cc = C.TipoCimientoCorrido(0.60, 0.50);
            });
            if (_sc == null || _cc == null) return;

            Cadena("CC1 tramo recto", "CC1", false, (0, 0), (7, 0));
            Cadena("CC2 cadena en L", "CC2", false, (0, -4), (7, -4), (7, -9));
            Cadena("CC3 cadena en U", "CC3", false, (0, -12), (0, -17), (7, -17), (7, -12));
            Cadena("CC4 anillo cerrado", "CC4", true, (10, -12), (17, -12), (17, -17), (10, -17));

            C.Paso("CC5 llegada en T", () =>
            {
                Sobrecimiento((10, 0), (17, 0), "CC5a");
                Sobrecimiento((13.5, 0), (13.5, -5), "CC5b");
            });

            C.Paso("CC6 cruce (cuatro tramos desde el centro)", () =>
            {
                var c = (23.5, -2.5);
                Sobrecimiento(c, (20, -2.5), "CC6a");
                Sobrecimiento(c, (27, -2.5), "CC6b");
                Sobrecimiento(c, (23.5, 1), "CC6c");
                Sobrecimiento(c, (23.5, -6), "CC6d");
            });

            C.Paso("CC7 escalón de cota de fondo (bayoneta)", () =>
            {
                Sobrecimiento((20, -10), (23.5, -10), "CC7a");
                Sobrecimiento((23.5, -10), (27, -10), "CC7b", desfase: -0.40, altura: 1.40);
            });

            C.Paso("CC8 tramos alineados (muro partido en una T)", () =>
            {
                Sobrecimiento((20, -14), (23.5, -14), "CC8a");
                Sobrecimiento((23.5, -14), (27, -14), "CC8b");
                Sobrecimiento((23.5, -14), (23.5, -18), "CC8c");
            });

            FloorType franja = null;
            C.Paso("Tipo de suelo para cimiento en franja", () => franja = C.TipoLosa(0.50, Contexto.Prefijo + "Cimiento (suelo) e=0.50"));
            if (franja != null)
            {
                SueloFranja("CC9 suelo en L", "CC9", franja, true,
                    (0, -22), (7, -22), (7, -22.6), (0.6, -22.6), (0.6, -28), (0, -28));
                SueloFranja("CC10 suelo en T", "CC10", franja, true,
                    (10, -22), (17, -22), (17, -22.6), (13.8, -22.6), (13.8, -28), (13.2, -28), (13.2, -22.6), (10, -22.6));
                SueloFranja("CC11 suelo en cruz", "CC11", franja, true,
                    (20, -25.3), (23.2, -25.3), (23.2, -28.5), (23.8, -28.5), (23.8, -25.3), (27, -25.3),
                    (27, -24.7), (23.8, -24.7), (23.8, -21.5), (23.2, -21.5), (23.2, -24.7), (20, -24.7));
                SueloFranja("CC17 suelo NO estructural (rechazo)", "CC17", franja, false,
                    (30, -26), (37, -26), (37, -25.4), (30, -25.4));
                C.Paso("CC18 suelo con borde en arco (rechazo)", () =>
                {
                    double z = Cota(C.Cim);
                    var lazo = new CurveLoop();
                    lazo.Append(Line.CreateBound(P(X(30), Y(-29), z), P(X(36), Y(-29), z)));
                    lazo.Append(Arc.Create(P(X(36), Y(-29), z), P(X(36), Y(-28.4), z), P(X(36.3), Y(-28.7), z)));
                    lazo.Append(Line.CreateBound(P(X(36), Y(-28.4), z), P(X(30), Y(-28.4), z)));
                    lazo.Append(Line.CreateBound(P(X(30), Y(-28.4), z), P(X(30), Y(-29), z)));
                    C.Losa(Lista(lazo), franja, C.Cim, "CC18");
                });
            }

            double zc = Cota(C.Cim);
            C.Paso("CC12 viga de cimentación 0.40x0.70", () =>
                PonerViga(FamVigaRect(0.40, 0.70, 7), P(X(30), Y(0), zc), P(X(37), Y(0), zc), C.Cim, "CC12", 0.40, 0.70));

            C.Paso("CC13 sección en T invertida", () =>
            {
                var perfil = new[] { (-0.125, 0.0), (0.125, 0.0), (0.125, -0.70), (0.40, -0.70), (0.40, -1.0), (-0.40, -1.0), (-0.40, -0.70), (-0.125, -0.70) };
                PonerViga(FamViga("T invertida 80x100", perfil, 7), P(X(30), Y(-4), zc), P(X(37), Y(-4), zc), C.Cim, "CC13", 0.80, 1.0);
            });

            C.Paso("CC14 sección escalonada", () =>
            {
                var perfil = new[] { (-0.125, 0.0), (0.125, 0.0), (0.125, -0.50), (0.225, -0.50), (0.225, -0.75), (0.40, -0.75), (0.40, -1.0),
                                     (-0.40, -1.0), (-0.40, -0.75), (-0.225, -0.75), (-0.225, -0.50), (-0.125, -0.50) };
                PonerViga(FamViga("escalonada 80x100", perfil, 7), P(X(30), Y(-8), zc), P(X(37), Y(-8), zc), C.Cim, "CC14", 0.80, 1.0);
            });

            C.Paso("CC15 sobrecimiento unido a una columna con prioridad", () =>
            {
                Wall w = Sobrecimiento((30, -12), (37, -12), "CC15");
                var col = FamColumnaRect(0.40, 0.40, 1.00);
                FamilyInstance c = PonerColumna(col, Rect(0, 0, 0.40, 0.40), X(33.5), Y(-12), C.Cim, C.N1, "C-CC15");
                C.Unir(c, w);
            });

            C.Paso("CC16 muro curvo con cimiento (rechazo: eje en arco)", () =>
            {
                double z = Cota(C.Cim);
                Arc arco = Arc.Create(P(X(30), Y(-20), z), P(X(37), Y(-20), z), P(X(33.5), Y(-16.5), z));
                Wall w = C.MuroCurvo(arco, _sc, C.Cim, 1.0, "SC-CC16");
                C.Marca(WallFoundation.Create(C.Doc, _cc.Id, w.Id), "CC16");
            });
        }

        private Wall Sobrecimiento((double x, double y) a, (double x, double y) b, string marca, double desfase = 0, double altura = 1.0)
        {
            double z = Cota(C.Cim);
            Wall w = C.Muro(P(X(a.x), Y(a.y), z), P(X(b.x), Y(b.y), z), _sc, C.Cim, altura, "SC-" + marca, desfase);
            C.Marca(WallFoundation.Create(C.Doc, _cc.Id, w.Id), marca);
            return w;
        }

        private void Cadena(string que, string marca, bool cerrada, params (double x, double y)[] v)
        {
            C.Paso(que, () =>
            {
                int n = cerrada ? v.Length : v.Length - 1;
                for (int i = 0; i < n; i++)
                    Sobrecimiento(v[i], v[(i + 1) % v.Length], marca + (char)('a' + i));
            });
        }

        private void SueloFranja(string que, string marca, FloorType tipo, bool estructural, params (double x, double y)[] v)
        {
            C.Paso(que, () => C.Losa(Lista(Lazo(Cota(C.Cim), Mover(v, X0, Y0))), tipo, C.Cim, marca, estructural));
        }
    }
}
