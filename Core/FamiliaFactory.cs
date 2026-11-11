using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace ArbaModeloPrueba.Core
{
    /// <summary>
    /// Familia de prueba de geometría fija: un único sólido (FreeFormElement) en coordenadas de la familia, en pies.
    /// Columnas: base en z = 0 (nivel inferior de referencia), eje en el origen.
    /// Vigas: eje X de 0 a L, cara superior en z = 0, centrada en y = 0.
    /// Cimentaciones: cara superior en z = 0 y el sólido hacia abajo, salvo que la zona diga otra cosa.
    /// </summary>
    internal sealed class FamiliaSpec
    {
        public string Nombre;
        public BuiltInCategory Categoria;
        public Func<Solid> Geometria;
        /// <summary>Alternativa a Geometria: varios sólidos separados (casos de prueba "varios sólidos").</summary>
        public Func<Solid[]> Varios;
        /// <summary>Nombre del material del proyecto que se asigna (debe existir en el proyecto).</summary>
        public string Material;
        /// <summary>Tipo de material para el comportamiento del modelo (columnas y vigas).</summary>
        public StructuralMaterialType TipoMaterial = StructuralMaterialType.Concrete;
    }

    /// <summary>
    /// Crea familias desde las plantillas .rft instaladas con Revit, con un sólido FreeFormElement, las guarda en
    /// %TEMP%\ARBA-ModeloPrueba y las carga en el proyecto. Las familias que ya existen en el proyecto se reutilizan.
    /// </summary>
    internal sealed class FamiliaFactory
    {
        private readonly Document _doc;
        private readonly Application _app;
        private readonly Dictionary<BuiltInCategory, string> _plantillas = new Dictionary<BuiltInCategory, string>();
        private readonly string _carpeta;
        private List<string> _rft;

        public FamiliaFactory(Document doc)
        {
            _doc = doc;
            _app = doc.Application;
            _carpeta = Path.Combine(Path.GetTempPath(), "ARBA-ModeloPrueba", "familias");
            Directory.CreateDirectory(_carpeta);
        }

        /// <summary>
        /// Devuelve el tipo (activado) de la familia; si no existe la crea y la carga. Se llama dentro de la
        /// transacción del paso que la usa (si no hay ninguna abierta, abre la suya).
        /// </summary>
        public FamilySymbol Obtener(FamiliaSpec spec)
        {
            // El nombre de la familia en el proyecto es el del archivo .rfa (sin caracteres no válidos como «/»).
            string nombre = Limpio(spec.Nombre);
            Family existente = new FilteredElementCollector(_doc).OfClass(typeof(Family)).Cast<Family>()
                .FirstOrDefault(f => f.Name == nombre);
            string ruta = existente == null ? Crear(spec) : null;

            Transaction propia = null;
            if (!_doc.IsModifiable)
            {
                propia = new Transaction(_doc, "Cargar familia " + spec.Nombre);
                propia.Start();
            }
            try
            {
                if (existente == null && (!_doc.LoadFamily(ruta, new OpcionesCarga(), out existente) || existente == null))
                    throw new InvalidOperationException("Revit no pudo cargar la familia " + spec.Nombre + ".");

                FamilySymbol tipo = existente.GetFamilySymbolIds().Select(id => _doc.GetElement(id)).OfType<FamilySymbol>().First();
                if (!tipo.IsActive) tipo.Activate();
                Material mat = Materiales.Buscar(_doc, spec.Material);
                if (mat != null)
                {
                    Parameter p = tipo.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM);
                    if (p != null && !p.IsReadOnly) p.Set(mat.Id);
                }
                _doc.Regenerate();
                propia?.Commit();
                return tipo;
            }
            finally
            {
                propia?.Dispose();
            }
        }

        private string Crear(FamiliaSpec spec)
        {
            bool cambiarCategoria;
            string plantilla = Plantilla(spec.Categoria, out cambiarCategoria);
            Document fam = _app.NewFamilyDocument(plantilla);
            try
            {
                using (var t = new Transaction(fam, "Geometría de prueba"))
                {
                    t.Start();

                    // Algunas plantillas (vigas) traen geometría: se borra para dejar solo el sólido de prueba.
                    var previas = new FilteredElementCollector(fam).OfClass(typeof(GenericForm)).ToElementIds();
                    if (previas.Count > 0) fam.Delete(previas);

                    if (cambiarCategoria)
                        fam.OwnerFamily.FamilyCategory = Category.GetCategory(fam, spec.Categoria);

                    Solid[] solidos = spec.Varios != null ? spec.Varios() : new[] { spec.Geometria() };
                    ElementId matId = ElementId.InvalidElementId;
                    if (!string.IsNullOrEmpty(spec.Material))
                    {
                        Material mat = new FilteredElementCollector(fam).OfClass(typeof(Material)).Cast<Material>()
                            .FirstOrDefault(m => m.Name == spec.Material);
                        matId = mat?.Id ?? Material.Create(fam, spec.Material);
                    }
                    foreach (Solid solido in solidos)
                    {
                        if (solido == null || solido.Volume <= 1e-9)
                            throw new InvalidOperationException("La geometría de " + spec.Nombre + " está vacía.");
                        FreeFormElement forma = FreeFormElement.Create(fam, solido);
                        if (matId != ElementId.InvalidElementId)
                            forma.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM)?.Set(matId);
                    }

                    FijarEstructural(fam, spec.TipoMaterial);
                    t.Commit();
                }

                string ruta = Path.Combine(_carpeta, Limpio(spec.Nombre) + ".rfa");
                fam.SaveAs(ruta, new SaveAsOptions { OverwriteExistingFile = true });
                return ruta;
            }
            finally
            {
                fam.Close(false);
            }
        }

        /// <summary>Material para el comportamiento del modelo y "puede alojar armadura" cuando la plantilla los tiene.</summary>
        private static void FijarEstructural(Document fam, StructuralMaterialType tipo)
        {
            Family f = fam.OwnerFamily;
            try
            {
                Parameter p = f.get_Parameter(BuiltInParameter.FAMILY_STRUCT_MATERIAL_TYPE);
                if (p != null && !p.IsReadOnly) p.Set((int)tipo);
            }
            catch { /* la plantilla no lo admite */ }
            try
            {
                Parameter p = f.get_Parameter(BuiltInParameter.FAMILY_CAN_HOST_REBAR);
                if (p != null && !p.IsReadOnly) p.Set(1);
            }
            catch { }
        }

        // ------------------------------------------------------------------ plantillas

        private static readonly Dictionary<BuiltInCategory, string[]> Claves = new Dictionary<BuiltInCategory, string[]>
        {
            [BuiltInCategory.OST_StructuralColumns] = new[] { "structural column", "pilar estructural", "columna estructural", "poteau porteur" },
            [BuiltInCategory.OST_StructuralFraming] = new[] { "beams and braces", "structural framing", "armazon estructural", "vigas y tirantes", "ossature" },
            [BuiltInCategory.OST_StructuralFoundation] = new[] { "structural foundation", "cimentacion estructural", "fondation" },
            [BuiltInCategory.OST_GenericModel] = new[] { "generic model", "modelo generico", "modele generique" },
        };

        private static readonly string[] Excluir =
        {
            "based", "basad", "adaptive", "adaptativ", "pattern", "patron", "trus", "celosia", "tag", "etiqueta",
            "profile", "perfil", "detail", "detalle", "slab edge", "borde de losa", "two level", "dos niveles",
            "line", "linea", "face", "cara", "wall", "muro", "ceiling", "techo", "roof", "cubierta", "annotation",
            "anotacion", "rebar", "armadura", "title", "cajetin", "pipe", "duct", "hosted", "nested", "anidad"
        };

        private string Plantilla(BuiltInCategory cat, out bool cambiarCategoria)
        {
            cambiarCategoria = false;
            if (_plantillas.TryGetValue(cat, out string conocida)) return conocida;

            foreach (string ruta in Candidatas(cat))
            {
                Document prueba = null;
                try
                {
                    prueba = _app.NewFamilyDocument(ruta);
                    Category c = prueba.OwnerFamily.FamilyCategory;
                    if (c != null && c.Id.Value == (long)cat)
                    {
                        _plantillas[cat] = ruta;
                        return ruta;
                    }
                }
                catch { /* plantilla dañada o de otro tipo */ }
                finally { prueba?.Close(false); }
            }

            if (cat != BuiltInCategory.OST_GenericModel)
            {
                // Último recurso: modelo genérico y cambio de categoría.
                bool _;
                string generica = Plantilla(BuiltInCategory.OST_GenericModel, out _);
                cambiarCategoria = true;
                return generica;
            }

            throw new FileNotFoundException(
                "No se encontró ninguna plantilla de familia (.rft) de Revit. Se buscó en: " + string.Join("; ", Raices()) +
                ". Instala las plantillas de familia de Revit (contenido de Revit) y vuelve a intentarlo.");
        }

        private IEnumerable<string> Candidatas(BuiltInCategory cat)
        {
            if (_rft == null)
            {
                _rft = new List<string>();
                foreach (string raiz in Raices())
                {
                    try { _rft.AddRange(Directory.EnumerateFiles(raiz, "*.rft", SearchOption.AllDirectories)); }
                    catch { }
                }
                _rft = _rft.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }

            string[] claves = Claves[cat];
            return _rft
                .Select(r => new { Ruta = r, Nombre = SinTildes(Path.GetFileNameWithoutExtension(r)).ToLowerInvariant() })
                .Where(x => claves.Any(k => x.Nombre.Contains(k)) && !Excluir.Any(e => x.Nombre.Contains(e)))
                .OrderByDescending(x => x.Nombre.Contains("metric") || x.Nombre.Contains("metrico") || x.Nombre.StartsWith("m_"))
                .ThenBy(x => x.Nombre.Length)
                .Select(x => x.Ruta)
                .Take(8)
                .ToList();
        }

        private IEnumerable<string> Raices()
        {
            var r = new List<string>();
            string propia = _app.FamilyTemplatePath;
            if (!string.IsNullOrEmpty(propia) && Directory.Exists(propia))
            {
                r.Add(propia);
                string padre = Path.GetDirectoryName(propia.TrimEnd('\\', '/'));
                if (!string.IsNullOrEmpty(padre) && Directory.Exists(padre)) r.Add(padre);
            }
            string comun = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                        "Autodesk", "RVT " + _app.VersionNumber, "Family Templates");
            if (Directory.Exists(comun)) r.Add(comun);
            return r.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string SinTildes(string s)
        {
            var sb = new StringBuilder();
            foreach (char ch in s.Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string Limpio(string nombre)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) nombre = nombre.Replace(c, '_');
            return nombre;
        }

        private sealed class OpcionesCarga : IFamilyLoadOptions
        {
            public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
            {
                overwriteParameterValues = true;
                return true;
            }

            public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
            {
                source = FamilySource.Family;
                overwriteParameterValues = true;
                return true;
            }
        }
    }
}
