using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using static ArbaModeloPrueba.Core.U;

namespace ArbaModeloPrueba.Core
{
    /// <summary>
    /// Estado compartido por las zonas: documento, niveles, vistas, materiales, tipos y la fábrica de familias.
    /// Cada paso de una zona corre en su propia transacción (<see cref="Paso"/>): si falla, se deshace solo ese paso,
    /// se anota en el informe y la generación sigue.
    /// </summary>
    internal sealed class Contexto
    {
        public const string Prefijo = "ARBA-P ";

        public Document Doc { get; }
        public Informe Informe { get; }
        public FamiliaFactory Fam { get; }

        public Level Cim, N1, N2, N3;
        public ViewPlan PlantaCim, PlantaN1, PlantaN2, PlantaN3;
        public View3D Vista3D;

        /// <summary>Zona en curso (para el informe).</summary>
        public string Zona = "Preparación";

        public Contexto(Document doc)
        {
            Doc = doc;
            Informe = new Informe();
            Fam = new FamiliaFactory(doc);
        }

        // ------------------------------------------------------------------ ejecución

        /// <summary>Ejecuta una acción en su propia transacción y anota el resultado.</summary>
        public bool Paso(string que, Action accion)
        {
            try
            {
                using (var t = new Transaction(Doc, "ARBA-P " + que))
                {
                    FailureHandlingOptions o = t.GetFailureHandlingOptions();
                    o.SetFailuresPreprocessor(new Silenciador(Informe, Zona, que));
                    o.SetClearAfterRollback(true);
                    t.SetFailureHandlingOptions(o);
                    t.Start();
                    accion();
                    if (t.Commit() != TransactionStatus.Committed)
                    {
                        Informe.Error(Zona, que + ": Revit no aceptó los cambios (ver avisos anteriores).");
                        return false;
                    }
                }
                Informe.Ok(Zona, que);
                return true;
            }
            catch (Exception ex)
            {
                Informe.Error(Zona, que + ": " + ex.Message);
                return false;
            }
        }

        private sealed class Silenciador : IFailuresPreprocessor
        {
            private readonly Informe _informe;
            private readonly string _zona, _que;
            public Silenciador(Informe informe, string zona, string que) { _informe = informe; _zona = zona; _que = que; }

            public FailureProcessingResult PreprocessFailures(FailuresAccessor fa)
            {
                bool error = false;
                foreach (FailureMessageAccessor f in fa.GetFailureMessages())
                {
                    string texto = f.GetDescriptionText();
                    if (f.GetSeverity() == FailureSeverity.Warning)
                    {
                        _informe.Aviso(_zona, _que + " (aviso de Revit): " + texto);
                        fa.DeleteWarning(f);
                    }
                    else
                    {
                        _informe.Error(_zona, _que + " (error de Revit): " + texto);
                        error = true;
                    }
                }
                return error ? FailureProcessingResult.ProceedWithRollBack : FailureProcessingResult.Continue;
            }
        }

        // ------------------------------------------------------------------ preparación

        /// <summary>Niveles, vistas y materiales. Devuelve false si no se pudo preparar lo imprescindible.</summary>
        public bool Preparar()
        {
            Zona = "Preparación";
            bool ok = Paso("Materiales de prueba", () => Materiales.Asegurar(Doc, Informe));
            ok &= Paso("Niveles de prueba", () =>
            {
                Cim = Nivel("PRUEBA ARBA - Cimentación", -1.00);
                N1 = Nivel("PRUEBA ARBA - Nivel 1", 0.00);
                N2 = Nivel("PRUEBA ARBA - Nivel 2", 3.00);
                N3 = Nivel("PRUEBA ARBA - Nivel 3", 6.00);
            });
            if (!ok) return false;

            Paso("Vistas de planta y 3D", () =>
            {
                PlantaCim = Planta(Cim);
                PlantaN1 = Planta(N1);
                PlantaN2 = Planta(N2);
                PlantaN3 = Planta(N3);
                Vista3D = Vista3DPrueba();
            });
            return true;
        }

        private Level Nivel(string nombre, double cota)
        {
            Level n = new FilteredElementCollector(Doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault(l => l.Name == nombre);
            if (n != null) return n;
            n = Level.Create(Doc, M(cota));
            n.Name = nombre;
            return n;
        }

        private ViewPlan Planta(Level nivel)
        {
            string nombre = "PRUEBA ARBA - Planta " + nivel.Name.Replace("PRUEBA ARBA - ", "");
            ViewPlan v = new FilteredElementCollector(Doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>()
                .FirstOrDefault(p => !p.IsTemplate && p.Name == nombre);
            if (v != null) return v;
            ViewFamilyType tipo = TipoVista(ViewFamily.StructuralPlan) ?? TipoVista(ViewFamily.FloorPlan);
            v = ViewPlan.Create(Doc, tipo.Id, nivel.Id);
            v.Name = nombre;
            try { v.DetailLevel = ViewDetailLevel.Fine; } catch { }
            return v;
        }

        private View3D Vista3DPrueba()
        {
            const string nombre = "PRUEBA ARBA - 3D";
            View3D v = new FilteredElementCollector(Doc).OfClass(typeof(View3D)).Cast<View3D>()
                .FirstOrDefault(p => !p.IsTemplate && p.Name == nombre);
            if (v != null) return v;
            v = View3D.CreateIsometric(Doc, TipoVista(ViewFamily.ThreeDimensional).Id);
            v.Name = nombre;
            try { v.DetailLevel = ViewDetailLevel.Fine; } catch { }
            return v;
        }

        public ViewFamilyType TipoVista(ViewFamily familia) =>
            new FilteredElementCollector(Doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                .FirstOrDefault(t => t.ViewFamily == familia);

        // ------------------------------------------------------------------ materiales y tipos

        public ElementId Mat(string nombre) => Materiales.Buscar(Doc, nombre)?.Id ?? ElementId.InvalidElementId;

        public WallType TipoMuro(double espesor, string material = Materiales.Concreto, string nombre = null)
        {
            nombre = nombre ?? Prefijo + "Muro e=" + F(espesor) + (material == Materiales.Concreto ? "" : " " + material.Replace(Prefijo, ""));
            WallType t = new FilteredElementCollector(Doc).OfClass(typeof(WallType)).Cast<WallType>().FirstOrDefault(w => w.Name == nombre);
            if (t != null) return t;
            WallType basico = new FilteredElementCollector(Doc).OfClass(typeof(WallType)).Cast<WallType>()
                .FirstOrDefault(w => w.Kind == WallKind.Basic)
                ?? throw new InvalidOperationException("El proyecto no tiene ningún tipo de muro básico para duplicar.");
            t = (WallType)basico.Duplicate(nombre);
            t.SetCompoundStructure(CompoundStructure.CreateSingleLayerCompoundStructure(
                MaterialFunctionAssignment.Structure, M(espesor), Mat(material)));
            return t;
        }

        public FloorType TipoLosa(double espesor, string nombre = null, string material = Materiales.Concreto)
        {
            nombre = nombre ?? Prefijo + "Losa maciza e=" + F(espesor);
            FloorType t = BuscarTipoSuelo(nombre);
            if (t != null) return t;
            FloorType basico = new FilteredElementCollector(Doc).OfClass(typeof(FloorType)).Cast<FloorType>()
                .FirstOrDefault(f => !f.IsFoundationSlab)
                ?? throw new InvalidOperationException("El proyecto no tiene ningún tipo de suelo para duplicar.");
            t = (FloorType)basico.Duplicate(nombre);
            t.SetCompoundStructure(CompoundStructure.CreateSingleLayerCompoundStructure(
                MaterialFunctionAssignment.Structure, M(espesor), Mat(material)));
            return t;
        }

        /// <summary>Tipo de suelo de dos capas: concreto + acabado (Losas lo rechaza; Metrados debe contar solo el concreto).</summary>
        public FloorType TipoLosaConAcabado(double espesor, double acabado)
        {
            string nombre = Prefijo + "Losa e=" + F(espesor) + " + acabado " + F(acabado);
            FloorType t = BuscarTipoSuelo(nombre);
            if (t != null) return t;
            t = (FloorType)TipoLosa(espesor).Duplicate(nombre);
            var capas = new List<CompoundStructureLayer>
            {
                new CompoundStructureLayer(M(acabado), MaterialFunctionAssignment.Finish1, Mat(Materiales.Acabado)),
                new CompoundStructureLayer(M(espesor), MaterialFunctionAssignment.Structure, Mat(Materiales.Concreto)),
            };
            CompoundStructure cs = CompoundStructure.CreateSimpleCompoundStructure(capas);
            cs.StructuralMaterialIndex = 1;
            t.SetCompoundStructure(cs);
            return t;
        }

        /// <summary>Tipo de losa de cimentación (se duplica uno existente: la API no puede convertir un suelo normal).</summary>
        public FloorType TipoLosaCimentacion(double espesor)
        {
            string nombre = Prefijo + "Losa de cimentación e=" + F(espesor);
            FloorType t = BuscarTipoSuelo(nombre);
            if (t != null) return t;
            FloorType basico = new FilteredElementCollector(Doc).OfClass(typeof(FloorType)).Cast<FloorType>()
                .FirstOrDefault(f => f.IsFoundationSlab)
                ?? throw new InvalidOperationException(
                    "El proyecto no tiene ningún tipo de losa de cimentación. Usa la plantilla estructural de Revit " +
                    "o crea uno (Estructura > Cimentación > Losa) y vuelve a generar.");
            t = (FloorType)basico.Duplicate(nombre);
            t.SetCompoundStructure(CompoundStructure.CreateSingleLayerCompoundStructure(
                MaterialFunctionAssignment.Structure, M(espesor), Mat(Materiales.Concreto)));
            return t;
        }

        private FloorType BuscarTipoSuelo(string nombre) =>
            new FilteredElementCollector(Doc).OfClass(typeof(FloorType)).Cast<FloorType>().FirstOrDefault(f => f.Name == nombre);

        public WallFoundationType TipoCimientoCorrido(double ancho, double canto)
        {
            string nombre = Prefijo + "Cimiento corrido " + F(ancho) + "x" + F(canto);
            WallFoundationType t = new FilteredElementCollector(Doc).OfClass(typeof(WallFoundationType)).Cast<WallFoundationType>()
                .FirstOrDefault(w => w.Name == nombre);
            if (t != null) return t;
            WallFoundationType basico = new FilteredElementCollector(Doc).OfClass(typeof(WallFoundationType)).Cast<WallFoundationType>().FirstOrDefault()
                ?? throw new InvalidOperationException(
                    "El proyecto no tiene ningún tipo de cimentación de muro. Usa la plantilla estructural de Revit.");
            t = (WallFoundationType)basico.Duplicate(nombre);
            Fijar(t, BuiltInParameter.STRUCTURAL_FOUNDATION_WIDTH, M(ancho));
            Fijar(t, BuiltInParameter.STRUCTURAL_FOUNDATION_THICKNESS, M(canto));
            Fijar(t, BuiltInParameter.STRUCTURAL_MATERIAL_PARAM, Mat(Materiales.Concreto));
            return t;
        }

        // ------------------------------------------------------------------ colocación

        public FamilyInstance Columna(FamilySymbol tipo, double x, double y, Level baseN, Level topeN, string marca, double giroGrados = 0)
        {
            FamilyInstance c = Doc.Create.NewFamilyInstance(P(x, y, AM(baseN.Elevation)), tipo, baseN, StructuralType.Column);
            Fijar(c, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM, topeN.Id);
            Fijar(c, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM, 0.0);
            Fijar(c, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM, 0.0);
            Girar(c, x, y, giroGrados);
            Marca(c, marca);
            return c;
        }

        /// <summary>Viga entre dos puntos (metros, cota absoluta de la cara superior).</summary>
        public FamilyInstance Viga(FamilySymbol tipo, XYZ a, XYZ b, Level nivel, string marca)
        {
            FamilyInstance v = Doc.Create.NewFamilyInstance(Line.CreateBound(a, b), tipo, nivel, StructuralType.Beam);
            Marca(v, marca);
            return v;
        }

        /// <summary>Cimentación aislada (familia) con la cara superior en el nivel.</summary>
        public FamilyInstance Zapata(FamilySymbol tipo, double x, double y, Level nivel, string marca, double giroGrados = 0)
        {
            FamilyInstance z = Doc.Create.NewFamilyInstance(P(x, y, AM(nivel.Elevation)), tipo, nivel, StructuralType.Footing);
            Girar(z, x, y, giroGrados);
            Marca(z, marca);
            return z;
        }

        /// <summary>Familia de modelo genérico u otra no estructural colocada en un punto.</summary>
        public FamilyInstance Pieza(FamilySymbol tipo, double x, double y, Level nivel, string marca, double giroGrados = 0)
        {
            FamilyInstance p = Doc.Create.NewFamilyInstance(P(x, y, AM(nivel.Elevation)), tipo, nivel, StructuralType.NonStructural);
            Girar(p, x, y, giroGrados);
            Marca(p, marca);
            return p;
        }

        public Wall Muro(XYZ a, XYZ b, WallType tipo, Level nivel, double altura, string marca, double desfase = 0, bool estructural = true)
        {
            Wall w = Wall.Create(Doc, Line.CreateBound(a, b), tipo.Id, nivel.Id, M(altura), M(desfase), false, estructural);
            Marca(w, marca);
            return w;
        }

        public Wall MuroCurvo(Arc arco, WallType tipo, Level nivel, double altura, string marca)
        {
            Wall w = Wall.Create(Doc, arco, tipo.Id, nivel.Id, M(altura), 0, false, true);
            Marca(w, marca);
            return w;
        }

        public Floor Losa(IList<CurveLoop> lazos, FloorType tipo, Level nivel, string marca, bool estructural = true)
        {
            Floor f = Floor.Create(Doc, lazos, tipo.Id, nivel.Id, estructural, null, 0);
            Marca(f, marca);
            return f;
        }

        public void Marca(Element e, string marca)
        {
            if (string.IsNullOrEmpty(marca)) return;
            Fijar(e, BuiltInParameter.ALL_MODEL_MARK, marca);
        }

        public void Comentario(Element e, string texto) => Fijar(e, BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS, texto);

        /// <summary>Une la geometría; el primero corta al segundo.</summary>
        public void Unir(Element corta, Element cortado)
        {
            if (!JoinGeometryUtils.AreElementsJoined(Doc, corta, cortado))
                JoinGeometryUtils.JoinGeometry(Doc, corta, cortado);
            if (!JoinGeometryUtils.IsCuttingElementInJoin(Doc, corta, cortado))
                JoinGeometryUtils.SwitchJoinOrder(Doc, corta, cortado);
        }

        public void Girar(Element e, double x, double y, double grados)
        {
            if (Math.Abs(grados) < 1e-9) return;
            Line eje = Line.CreateBound(P(x, y, 0), P(x, y, 1));
            ElementTransformUtils.RotateElement(Doc, e.Id, eje, grados * Math.PI / 180.0);
        }

        /// <summary>Línea de modelo en el plano horizontal de un nivel.</summary>
        public ModelCurve LineaModelo(Curve c, Level nivel)
        {
            SketchPlane sp = SketchPlane.Create(Doc, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, nivel.Elevation)));
            return Doc.Create.NewModelCurve(c, sp) as ModelCurve;
        }

        /// <summary>Texto en la planta de un nivel (por defecto Nivel 1) para rotular las zonas.</summary>
        public void Rotulo(double x, double y, string texto, ViewPlan vista = null)
        {
            vista = vista ?? PlantaN1;
            if (vista == null) return;
            ElementId tipo = Doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
            TextNote.Create(Doc, vista.Id, P(x, y, 0), texto, tipo);
        }

        // ------------------------------------------------------------------ comprobaciones

        /// <summary>
        /// Compara la caja del elemento con la esperada (metros). Si solo está desplazada, mueve el elemento; si la
        /// forma no coincide lo anota como aviso. Sirve para detectar familias que Revit coloca de otra manera.
        /// </summary>
        public void Encajar(Element e, double x0, double y0, double z0, double x1, double y1, double z1, string que, bool mover = true)
        {
            Doc.Regenerate();
            BoundingBoxXYZ bb = e.get_BoundingBox(null);
            if (bb == null) { Informe.Aviso(Zona, que + ": sin geometría visible."); return; }
            XYZ min = bb.Min, max = bb.Max;
            double tol = M(0.01);
            XYZ esperadoMin = P(x0, y0, z0), esperadoMax = P(x1, y1, z1);
            XYZ tam = max - min, tamEsp = esperadoMax - esperadoMin;
            bool mismoTam = Math.Abs(tam.X - tamEsp.X) < tol && Math.Abs(tam.Y - tamEsp.Y) < tol && Math.Abs(tam.Z - tamEsp.Z) < tol;
            XYZ delta = esperadoMin - min;
            if (mismoTam && delta.GetLength() < tol) return;
            if (mismoTam && mover)
            {
                ElementTransformUtils.MoveElement(Doc, e.Id, delta);
                Informe.Aviso(Zona, que + ": la familia quedó desplazada " + FX(delta) + " y se corrigió.");
                return;
            }
            Informe.Aviso(Zona, que + ": la caja no coincide con la esperada. Esperada " + FX(tamEsp) + ", obtenida " + FX(tam) +
                                ", desplazamiento " + FX(delta) + ".");
        }

        private static string FX(XYZ v) => "(" + F(AM(v.X)) + ", " + F(AM(v.Y)) + ", " + F(AM(v.Z)) + ") m";

        // ------------------------------------------------------------------ parámetros

        public static void Fijar(Element e, BuiltInParameter bip, string valor)
        {
            Parameter p = e?.get_Parameter(bip);
            if (p != null && !p.IsReadOnly) p.Set(valor);
        }

        public static void Fijar(Element e, BuiltInParameter bip, double valor)
        {
            Parameter p = e?.get_Parameter(bip);
            if (p != null && !p.IsReadOnly) p.Set(valor);
        }

        public static void Fijar(Element e, BuiltInParameter bip, ElementId valor)
        {
            Parameter p = e?.get_Parameter(bip);
            if (p != null && !p.IsReadOnly && valor != null && valor != ElementId.InvalidElementId) p.Set(valor);
        }

        public static void Fijar(Element e, BuiltInParameter bip, int valor)
        {
            Parameter p = e?.get_Parameter(bip);
            if (p != null && !p.IsReadOnly) p.Set(valor);
        }
    }
}
