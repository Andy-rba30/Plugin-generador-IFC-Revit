using System;
using System.Collections.Generic;
using System.Linq;
using ArbaModeloPrueba.Core;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Zonas
{
    /// <summary>Elementos y vistas que Rotar Norte debe girar, omitir o conservar: rejillas, anotaciones, habitaciones, vistas y planos.</summary>
    internal sealed class ZonaRotarNorte : Zona
    {
        public override string Id => "ROT";
        public override string Titulo => "Rotar Norte";
        public override string Plugins => "Rotar Norte (girar, simular, Norte verdadero, enderezar vista)";
        public override string Guia =>
            "R1 rejillas (RN-1 anclada) · R2 plano de referencia · R3 columna anclada · R4 habitación con 4 muros · R5 muro cortina\n" +
            "R6 hueco en muro · R7 grupo de modelo · R8 cota, etiqueta, texto, línea y región en Planta Nivel 1 · R9 grupo de detalle\n" +
            "Vistas: sección, llamada, alzado, planta con recorte en L + dependiente, planta a Norte verdadero, 3D con caja de sección,\n" +
            "3D bloqueada, perspectiva, plano con ventanas gráficas · Norte verdadero del proyecto a 15° si estaba en 0°";
        public override double X0 => 150;
        public override double Y0 => -115;

        protected override void Generar()
        {
            double z1 = Cota(C.N1);
            var grids = new List<Grid>();

            C.Paso("R1 rejillas (RN-1 anclada)", () =>
            {
                for (int i = 0; i < 3; i++)
                {
                    Grid g = Grid.Create(C.Doc, Line.CreateBound(P(X(6 * i), Y(3), z1), P(X(6 * i), Y(-20), z1)));
                    Nombrar(g, "RN-" + (i + 1));
                    grids.Add(g);
                }
                for (int j = 0; j < 2; j++)
                {
                    Grid g = Grid.Create(C.Doc, Line.CreateBound(P(X(-3), Y(-8 * j), z1), P(X(15), Y(-8 * j), z1)));
                    Nombrar(g, "RN-" + (char)('A' + j));
                }
                grids[0].Pinned = true;
            });

            C.Paso("R2 plano de referencia", () =>
            {
                ReferencePlane rp = C.Doc.Create.NewReferencePlane(P(X(-2), Y(-4), z1), P(X(14), Y(-4), z1), XYZ.BasisZ, C.PlantaN1);
                rp.Name = "RN-Plano";
            });

            FamilyInstance col1 = null, col2 = null;
            C.Paso("R3 columnas (una anclada)", () =>
            {
                var col = FamColumnaRect(0.40, 0.40, 3.00);
                col1 = PonerColumna(col, Rect(0, 0, 0.4, 0.4), X(0), Y(0), C.N1, C.N2, "RN-C1");
                col2 = PonerColumna(col, Rect(0, 0, 0.4, 0.4), X(6), Y(0), C.N1, C.N2, "RN-C2");
                col1.Pinned = true;
            });

            Wall muroHueco = null;
            C.Paso("R4 habitación cerrada por cuatro muros", () =>
            {
                WallType t = C.TipoMuro(0.15);
                var v = new[] { (0.0, -10.0), (6.0, -10.0), (6.0, -15.0), (0.0, -15.0) };
                for (int i = 0; i < 4; i++)
                {
                    var a = v[i]; var b = v[(i + 1) % 4];
                    Wall w = C.Muro(P(X(a.Item1), Y(a.Item2), z1), P(X(b.Item1), Y(b.Item2), z1), t, C.N1, 3.0, "RN-M" + (i + 1), estructural: false);
                    if (i == 0) muroHueco = w;
                }
                C.Doc.Regenerate();
                Room r = C.Doc.Create.NewRoom(C.N1, new UV(M(X(3)), M(Y(-12.5))));
                r.Name = "Sala de prueba";
            });

            C.Paso("R5 muro cortina", () =>
            {
                WallType cortina = new FilteredElementCollector(C.Doc).OfClass(typeof(WallType)).Cast<WallType>().FirstOrDefault(w => w.Kind == WallKind.Curtain)
                    ?? throw new InvalidOperationException("No hay tipos de muro cortina en el proyecto.");
                C.Muro(P(X(8), Y(-10), z1), P(X(14), Y(-10), z1), cortina, C.N1, 3.0, "RN-MC", estructural: false);
            });

            C.Paso("R6 hueco rectangular en muro", () =>
            {
                if (muroHueco == null) throw new InvalidOperationException("No se creó el muro de la habitación.");
                C.Doc.Create.NewOpening(muroHueco, P(X(2), Y(-10), z1 + 0.9), P(X(3.2), Y(-10), z1 + 2.1));
            });

            C.Paso("R7 grupo de modelo", () =>
            {
                var f = FamColumnaRect(0.30, 0.30, 3.00);
                FamilyInstance a = PonerColumna(f, Rect(0, 0, 0.3, 0.3), X(12), Y(0), C.N1, C.N2, "RN-G1");
                FamilyInstance b = PonerColumna(f, Rect(0, 0, 0.3, 0.3), X(13), Y(0), C.N1, C.N2, "RN-G2");
                Group g = C.Doc.Create.NewGroup(new List<ElementId> { a.Id, b.Id });
                Nombrar(g.GroupType, "ARBA-P Grupo de modelo");
            });

            View planta = C.PlantaN1;
            C.Paso("R8 anotaciones en Planta Nivel 1", () =>
            {
                C.Doc.Create.NewDetailCurve(planta, Line.CreateBound(P(X(-2), Y(-17), z1), P(X(10), Y(-17), z1)));
                C.Rotulo(X(-2), Y(-18), "Texto de prueba Rotar Norte");
                ElementId tipoRegion = new FilteredElementCollector(C.Doc).OfClass(typeof(FilledRegionType)).FirstElementId();
                FilledRegion.Create(C.Doc, tipoRegion, planta.Id, Lista(Lazo(z1, Caja(X(8), Y(-19), X(10), Y(-17.5)))));
                if (grids.Count >= 2)
                {
                    var refs = new ReferenceArray();
                    refs.Append(new Reference(grids[0]));
                    refs.Append(new Reference(grids[1]));
                    C.Doc.Create.NewDimension(planta, Line.CreateBound(P(X(0), Y(2), z1), P(X(6), Y(2), z1)), refs);
                }
            });

            C.Paso("R8b etiqueta de columna", () =>
            {
                if (col2 == null) throw new InvalidOperationException("No se creó la columna.");
                IndependentTag.Create(C.Doc, planta.Id, new Reference(col2), false, TagMode.TM_ADDBY_CATEGORY, TagOrientation.Horizontal, P(X(7), Y(1), z1));
            });

            C.Paso("R9 grupo de detalle", () =>
            {
                DetailCurve a = C.Doc.Create.NewDetailCurve(planta, Line.CreateBound(P(X(-2), Y(-20), z1), P(X(2), Y(-20), z1)));
                DetailCurve b = C.Doc.Create.NewDetailCurve(planta, Line.CreateBound(P(X(-2), Y(-21), z1), P(X(2), Y(-21), z1)));
                C.Doc.Create.NewGroup(new List<ElementId> { a.Id, b.Id });
            });

            ViewSection seccion = null;
            C.Paso("Vista de sección", () =>
            {
                ViewFamilyType t = C.TipoVista(ViewFamily.Section);
                var caja = new BoundingBoxXYZ();
                Transform tr = Transform.Identity;
                tr.Origin = P(X(6), Y(-5), z1);
                tr.BasisX = XYZ.BasisX;
                tr.BasisY = XYZ.BasisZ;
                tr.BasisZ = XYZ.BasisX.CrossProduct(XYZ.BasisZ);
                caja.Transform = tr;
                caja.Min = P(-9, -2, -1);
                caja.Max = P(9, 8, 6);
                seccion = ViewSection.CreateSection(C.Doc, t.Id, caja);
                Nombrar(seccion, "PRUEBA ARBA - Sección Rotar Norte");
            });

            C.Paso("Llamada en la planta", () =>
            {
                View llamada = ViewSection.CreateCallout(C.Doc, planta.Id, planta.GetTypeId(), P(X(-1), Y(-16), z1), P(X(7), Y(-9), z1));
                Nombrar(llamada, "PRUEBA ARBA - Llamada Rotar Norte");
            });

            C.Paso("Alzado", () =>
            {
                ViewFamilyType t = C.TipoVista(ViewFamily.Elevation);
                ElevationMarker m = ElevationMarker.CreateElevationMarker(C.Doc, t.Id, P(X(6), Y(-24), z1), 100);
                ViewSection alzado = m.CreateElevation(C.Doc, planta.Id, 1);
                Nombrar(alzado, "PRUEBA ARBA - Alzado Rotar Norte");
            });

            ViewPlan recorte = null;
            C.Paso("Planta con recorte en L y vista dependiente", () =>
            {
                ViewFamilyType t = C.TipoVista(ViewFamily.StructuralPlan) ?? C.TipoVista(ViewFamily.FloorPlan);
                recorte = ViewPlan.Create(C.Doc, t.Id, C.N1.Id);
                Nombrar(recorte, "PRUEBA ARBA - Rotar Norte (recorte en L)");
                recorte.CropBoxActive = true;
                recorte.CropBoxVisible = true;
                C.Doc.Regenerate();
                recorte.GetCropRegionShapeManager().SetCropShape(Lazo(z1, new[]
                    { (X(-4), Y(-22)), (X(16), Y(-22)), (X(16), Y(-12)), (X(6), Y(-12)), (X(6), Y(4)), (X(-4), Y(4)) }));
                ElementId dep = recorte.Duplicate(ViewDuplicateOption.AsDependent);
                Nombrar(C.Doc.GetElement(dep), "PRUEBA ARBA - Rotar Norte (dependiente)");
            });

            C.Paso("Planta a Norte verdadero", () =>
            {
                ViewFamilyType t = C.TipoVista(ViewFamily.StructuralPlan) ?? C.TipoVista(ViewFamily.FloorPlan);
                ViewPlan v = ViewPlan.Create(C.Doc, t.Id, C.N1.Id);
                Nombrar(v, "PRUEBA ARBA - Norte verdadero");
                Contexto.Fijar(v, BuiltInParameter.PLAN_VIEW_NORTH, 1);
            });

            C.Paso("Vistas 3D: caja de sección, bloqueada y perspectiva", () =>
            {
                ViewFamilyType t = C.TipoVista(ViewFamily.ThreeDimensional);
                View3D caja = View3D.CreateIsometric(C.Doc, t.Id);
                Nombrar(caja, "PRUEBA ARBA - 3D caja de sección");
                caja.SetSectionBox(new BoundingBoxXYZ { Min = P(X(-4), Y(-22), -2), Max = P(X(16), Y(4), 7) });

                View3D bloqueada = View3D.CreateIsometric(C.Doc, t.Id);
                Nombrar(bloqueada, "PRUEBA ARBA - 3D bloqueada");
                bloqueada.SaveOrientationAndLock();

                View3D perspectiva = View3D.CreatePerspective(C.Doc, t.Id);
                Nombrar(perspectiva, "PRUEBA ARBA - 3D perspectiva");
            });

            C.Paso("Plano con ventanas gráficas", () =>
            {
                ElementId cajetin = new FilteredElementCollector(C.Doc).OfCategory(BuiltInCategory.OST_TitleBlocks).OfClass(typeof(FamilySymbol)).FirstElementId();
                ViewSheet plano = ViewSheet.Create(C.Doc, cajetin);
                plano.SheetNumber = "ARBA-P01";
                plano.Name = "Prueba Rotar Norte";
                if (C.PlantaN2 != null && Viewport.CanAddViewToSheet(C.Doc, plano.Id, C.PlantaN2.Id))
                    Viewport.Create(C.Doc, plano.Id, C.PlantaN2.Id, new XYZ(1, 0.8, 0));
                if (seccion != null && Viewport.CanAddViewToSheet(C.Doc, plano.Id, seccion.Id))
                    Viewport.Create(C.Doc, plano.Id, seccion.Id, new XYZ(2, 0.8, 0));
            });

            C.Paso("Norte verdadero del proyecto a 15°", () =>
            {
                ProjectLocation loc = C.Doc.ActiveProjectLocation;
                ProjectPosition pp = loc.GetProjectPosition(XYZ.Zero);
                if (Math.Abs(pp.Angle) > 1e-9)
                {
                    C.Informe.Aviso(C.Zona, "El proyecto ya tenía un ángulo a Norte verdadero; no se cambia.");
                    return;
                }
                loc.SetProjectPosition(XYZ.Zero, new ProjectPosition(pp.EastWest, pp.NorthSouth, pp.Elevation, 15 * Math.PI / 180));
            });
        }

        private void Nombrar(Element e, string nombre)
        {
            try { e.Name = nombre; }
            catch { C.Informe.Aviso(C.Zona, "No se pudo nombrar «" + nombre + "» (ya existe)."); }
        }
    }
}
