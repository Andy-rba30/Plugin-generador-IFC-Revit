using ArbaModeloPrueba.Core;
using Autodesk.Revit.DB;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Zonas
{
    /// <summary>Losas para dividir y unir (Editor-de-losas), con líneas de corte de modelo y una rejilla.</summary>
    internal sealed class ZonaEditorLosas : Zona
    {
        public override string Id => "ELO";
        public override string Titulo => "Dividir y unir losas";
        public override string Plugins => "Editor de losas (ARBA.Losas) · Acero-losas";
        public override string Guia =>
            "Losas e=0.20 en Nivel 1 con líneas de modelo para cortar. D1 rectángulo (corte recto, diagonal y rejilla) · D2 L (corte por el vértice)\n" +
            "D3 U · D4 con hueco (corte por el vértice del hueco) · D5 hueco circular y borde en arco · D1 tiene una viga unida debajo\n" +
            "Unir: U1 adyacentes · U2 separadas 0.5 mm · U3 separadas 5 mm · U4 solapadas · U5 anillo de 4\n" +
            "Rechazos: U6 tipos distintos · D6 inclinada";
        public override double X0 => 165;
        public override double Y0 => -65;

        private FloorType _t20;

        protected override void Generar()
        {
            C.Paso("Tipo de losa", () => _t20 = C.TipoLosa(0.20));
            if (_t20 == null) return;
            double z = Cota(C.N1);

            C.Paso("D1 rectángulo 8x6 con cortes recto, diagonal y por rejilla + viga unida", () =>
            {
                Floor f = Losa("D1", Caja(X(0), Y(-6), X(8), Y(0)));
                C.LineaModelo(Line.CreateBound(P(X(4), Y(-7), z), P(X(4), Y(1), z)), C.N1);
                C.LineaModelo(Line.CreateBound(P(X(-1), Y(-7), z), P(X(9), Y(1), z)), C.N1);
                Grid g = Grid.Create(C.Doc, Line.CreateBound(P(X(-1), Y(-2), z), P(X(9), Y(-2), z)));
                try { g.Name = "D1-corte"; } catch { }
                FamilyInstance v = PonerViga(FamVigaRect(0.25, 0.50, 8), P(X(0), Y(-4), z), P(X(8), Y(-4), z), C.N1, "D1-viga", 0.25, 0.50);
                C.Doc.Regenerate();
                C.Unir(f, v);
            });

            C.Paso("D2 en L con corte por el vértice interior", () =>
            {
                Losa("D2", Mover(new[] { (0.0, 0.0), (6.0, 0.0), (6.0, 2.5), (2.5, 2.5), (2.5, 6.0), (0.0, 6.0) }, X(11), Y(-6)));
                C.LineaModelo(Line.CreateBound(P(X(13.5), Y(-7), z), P(X(13.5), Y(1), z)), C.N1);
            });

            C.Paso("D3 en U", () =>
            {
                Losa("D3", Mover(new[] { (0.0, 0.0), (7.0, 0.0), (7.0, 6.0), (5.0, 6.0), (5.0, 2.0), (2.0, 2.0), (2.0, 6.0), (0.0, 6.0) }, X(20), Y(-6)));
                C.LineaModelo(Line.CreateBound(P(X(19), Y(-5), z), P(X(28), Y(-5), z)), C.N1);
            });

            C.Paso("D4 con hueco y corte por su vértice", () =>
            {
                C.Losa(Lista(Lazo(z, Caja(X(0), Y(-16), X(8), Y(-10))), Lazo(z, Caja(X(3), Y(-14), X(5), Y(-12)))), _t20, C.N1, "D4");
                C.LineaModelo(Line.CreateBound(P(X(3), Y(-17), z), P(X(3), Y(-9), z)), C.N1);
            });

            C.Paso("D5 hueco circular y borde en arco", () =>
            {
                var exterior = new CurveLoop();
                exterior.Append(Line.CreateBound(P(X(11), Y(-16), z), P(X(16), Y(-16), z)));
                exterior.Append(Arc.Create(P(X(16), Y(-16), z), P(X(16), Y(-10), z), P(X(19), Y(-13), z)));
                exterior.Append(Line.CreateBound(P(X(16), Y(-10), z), P(X(11), Y(-10), z)));
                exterior.Append(Line.CreateBound(P(X(11), Y(-10), z), P(X(11), Y(-16), z)));
                var hueco = new CurveLoop();
                XYZ c = P(X(13.5), Y(-13), z);
                hueco.Append(Arc.Create(c, M(0.6), 0, System.Math.PI, XYZ.BasisX, XYZ.BasisY));
                hueco.Append(Arc.Create(c, M(0.6), System.Math.PI, 2 * System.Math.PI, XYZ.BasisX, XYZ.BasisY));
                C.Losa(Lista(exterior, hueco), _t20, C.N1, "D5");
                C.LineaModelo(Line.CreateBound(P(X(10), Y(-12.4), z), P(X(20), Y(-12.4), z)), C.N1);
            });

            C.Paso("U1 dos losas adyacentes", () => { Losa("U1a", Caja(X(0), Y(-24), X(4), Y(-20))); Losa("U1b", Caja(X(4), Y(-24), X(8), Y(-20))); });
            C.Paso("U2 separadas 0.5 mm", () => { Losa("U2a", Caja(X(11), Y(-24), X(15), Y(-20))); Losa("U2b", Caja(X(15.0005), Y(-24), X(19), Y(-20))); });
            C.Paso("U3 separadas 5 mm", () => { Losa("U3a", Caja(X(22), Y(-24), X(26), Y(-20))); Losa("U3b", Caja(X(26.005), Y(-24), X(30), Y(-20))); });
            C.Paso("U4 solapadas", () => { Losa("U4a", Caja(X(0), Y(-32), X(4.5), Y(-28))); Losa("U4b", Caja(X(3.5), Y(-32), X(8), Y(-28))); });
            C.Paso("U5 anillo de cuatro losas", () =>
            {
                Losa("U5a", Caja(X(11), Y(-34), X(19), Y(-32)));
                Losa("U5b", Caja(X(11), Y(-28), X(19), Y(-26)));
                Losa("U5c", Caja(X(11), Y(-32), X(13), Y(-28)));
                Losa("U5d", Caja(X(17), Y(-32), X(19), Y(-28)));
            });

            C.Paso("U6 adyacentes de tipos distintos (rechazo)", () =>
            {
                Losa("U6a", Caja(X(22), Y(-32), X(26), Y(-28)));
                C.Losa(Lista(Lazo(z, Caja(X(26), Y(-32), X(30), Y(-28)))), C.TipoLosa(0.25), C.N1, "U6b");
            });

            C.Paso("D6 inclinada (rechazo)", () =>
            {
                Line flecha = Line.CreateBound(P(X(22), Y(-16), z), P(X(22), Y(-10), z));
                Floor f = Floor.Create(C.Doc, Lista(Lazo(z, Caja(X(22), Y(-16), X(28), Y(-10)))), _t20.Id, C.N1.Id, true, flecha, 0.10);
                C.Marca(f, "D6");
            });
        }

        private Floor Losa(string marca, (double, double)[] contorno) =>
            C.Losa(Lista(Lazo(Cota(C.N1), contorno)), _t20, C.N1, marca);
    }
}
