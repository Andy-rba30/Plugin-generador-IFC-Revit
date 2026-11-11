using ArbaModeloPrueba.Core;
using Autodesk.Revit.DB;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Zonas
{
    /// <summary>Encofrado de muros de contención: el mismo muro en T con distintos vecinos que descuentan o no contacto.</summary>
    internal sealed class ZonaEncofrado : Zona
    {
        public override string Id => "ENC";
        public override string Titulo => "Encofrado de muros de contención";
        public override string Plugins => "Encofrado-automatico (RetainingWallFormwork) · Metrados > Encofrado";
        public override string Guia =>
            "Muro en T de 6 m (zapata en Cimentación, alzado hasta +2.00) con vecinos sobre el talón:\n" +
            "E1 losa unida · E2 losa sin unir · E3 muro perpendicular unido · E4 sin unir · E5 columna adosada\n" +
            "E6 muro de albañilería (no descuenta) · E7 modelo genérico a 15 mm (tolerancia) · E8 muro de sistema sin zapata\n" +
            "E9 muro curvo · E10 con contrafuertes (debe metrar con aviso)";
        public override double X0 => 160;
        public override double Y0 => 0;

        protected override void Generar()
        {
            Level cim = C.Cim;
            var muro = ZonaMuros.FamMuroT("T 260", 1.15, 1.15, 0.30, 6);
            double zc = Cota(cim), zTop = zc + ZonaMuros.Ha;

            FamilyInstance Base(string marca, double dy) => PonerZapata(muro, X(0), Y(dy), cim, marca, 0, -1.30, 6, 1.30, -ZonaMuros.Hz, ZonaMuros.Ha);

            C.Paso("E1 losa apoyada y unida", () =>
            {
                FamilyInstance m = Base("E1", 0);
                Floor l = C.Losa(Lista(Lazo(zTop, Caja(X(0), Y(0.15), X(6), Y(4)))), C.TipoLosa(0.20), C.N1, "E1-losa");
                Contexto.Fijar(l, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM, M(zTop - Cota(C.N1)));
                C.Doc.Regenerate();
                C.Unir(m, l);
            });

            C.Paso("E2 losa apoyada sin unir", () =>
            {
                Base("E2", -8);
                Floor l = C.Losa(Lista(Lazo(zTop, Caja(X(0), Y(-7.85), X(6), Y(-4)))), C.TipoLosa(0.20), C.N1, "E2-losa");
                Contexto.Fijar(l, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM, M(zTop - Cota(C.N1)));
            });

            C.Paso("E3 muro perpendicular unido", () =>
            {
                FamilyInstance m = Base("E3", -16);
                Wall w = C.Muro(P(X(3), Y(-15.85), zc), P(X(3), Y(-12.5), zc), C.TipoMuro(0.25), cim, ZonaMuros.Ha, "E3-muro");
                C.Doc.Regenerate();
                C.Unir(m, w);
            });

            C.Paso("E4 muro perpendicular sin unir", () =>
            {
                Base("E4", -24);
                C.Muro(P(X(3), Y(-23.85), zc), P(X(3), Y(-20.5), zc), C.TipoMuro(0.25), cim, ZonaMuros.Ha, "E4-muro");
            });

            C.Paso("E5 columna adosada al alzado", () =>
            {
                Base("E5", -32);
                PonerColumna(FamColumnaRect(0.40, 0.40, 4.00), Rect(0, 0, 0.40, 0.40), X(3), Y(-32 + 0.35), cim, C.N2, "E5-col");
            });

            C.Paso("E6 muro de albañilería adosado (no descuenta)", () =>
            {
                Base("E6", -40);
                C.Muro(P(X(3), Y(-39.85), zc), P(X(3), Y(-36.5), zc), C.TipoMuro(0.15, Materiales.Albanileria), cim, ZonaMuros.Ha, "E6-muro", estructural: false);
            });

            C.Paso("E7 modelo genérico a 15 mm del alzado", () =>
            {
                Base("E7", -48);
                var g = new FamiliaSpec
                {
                    Nombre = Contexto.Prefijo + "Bloque genérico 50x50x100",
                    Categoria = BuiltInCategory.OST_GenericModel,
                    Material = Materiales.Concreto,
                    Geometria = () => Solidos.Caja(-0.25, 0, 0, 0.25, 0.5, 1.0)
                };
                C.Pieza(C.Fam.Obtener(g), X(3), Y(-48 + 0.15 + 0.015), cim, "E7-gen");
            });

            C.Paso("E8 muro de sistema sin zapata", () =>
                C.Muro(P(X(10), Y(0), zc), P(X(16), Y(0), zc), C.TipoMuro(0.30), cim, ZonaMuros.Ha, "E8"));

            C.Paso("E9 muro curvo de sistema", () =>
            {
                Arc arco = Arc.Create(P(X(10), Y(-8), zc), P(X(16), Y(-8), zc), P(X(13), Y(-5), zc));
                C.MuroCurvo(arco, C.TipoMuro(0.30), cim, ZonaMuros.Ha, "E9");
            });

            C.Paso("E10 muro con contrafuertes", () =>
            {
                var f = FamCimentacion("Muro contención con contrafuertes L=6", () =>
                {
                    Solid s = Solidos.PrismaX(ZonaMuros.SeccionT(1.15, 1.15, 0.30), 0, 6);
                    var cartela = new[] { (0.15, 0.0), (1.30, 0.0), (0.15, 2.5) };
                    foreach (double x in new[] { 1.0, 4.75 })
                        s = Solidos.Union(s, Solidos.PrismaX(cartela, x, x + 0.25));
                    return s;
                });
                PonerZapata(f, X(10), Y(-16), cim, "E10", 0, -1.30, 6, 1.30, -ZonaMuros.Hz, ZonaMuros.Ha);
            });
        }
    }
}
