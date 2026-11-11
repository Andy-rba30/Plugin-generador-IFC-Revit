using System.Linq;
using Autodesk.Revit.DB;

namespace ArbaModeloPrueba.Core
{
    /// <summary>Materiales de prueba con su activo estructural (los add-ins de armado exigen hormigón).</summary>
    internal static class Materiales
    {
        public const string Concreto = "ARBA-P Concreto f'c 210";
        public const string Concreto280 = "ARBA-P Concreto f'c 280";
        public const string ConcretoSinNombre = "ARBA-P Mezcla estructural";   // activo Concrete, sin la palabra en el nombre
        public const string Acero = "ARBA-P Acero A36";
        public const string AceroS355 = "ARBA-P Acero S355JR";
        public const string Madera = "ARBA-P Madera estructural";
        public const string Albanileria = "ARBA-P Albañilería";
        public const string Acabado = "ARBA-P Acabado de piso";

        public static Material Buscar(Document doc, string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return null;
            return new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>().FirstOrDefault(m => m.Name == nombre);
        }

        /// <summary>Crea (dentro de una transacción) todos los materiales de prueba que falten.</summary>
        public static void Asegurar(Document doc, Informe informe)
        {
            Crear(doc, informe, Concreto, "Concreto", StructuralAssetClass.Concrete, new Color(192, 192, 192));
            Crear(doc, informe, Concreto280, "Concreto", StructuralAssetClass.Concrete, new Color(170, 170, 170));
            Crear(doc, informe, ConcretoSinNombre, "Genérico", StructuralAssetClass.Concrete, new Color(150, 160, 170));
            Crear(doc, informe, Acero, "Metal", StructuralAssetClass.Metal, new Color(80, 110, 160));
            Crear(doc, informe, AceroS355, "Metal", StructuralAssetClass.Metal, new Color(70, 90, 140));
            Crear(doc, informe, Madera, "Madera", StructuralAssetClass.Wood, new Color(170, 120, 70));
            Crear(doc, informe, Albanileria, "Mampostería", StructuralAssetClass.Generic, new Color(180, 90, 70));
            Crear(doc, informe, Acabado, "Acabado", StructuralAssetClass.Generic, new Color(220, 210, 180));
        }

        private static void Crear(Document doc, Informe informe, string nombre, string clase, StructuralAssetClass activo, Color color)
        {
            Material m = Buscar(doc, nombre);
            if (m == null)
            {
                m = doc.GetElement(Material.Create(doc, nombre)) as Material;
                m.MaterialClass = clase;
                m.MaterialCategory = clase;
                m.Color = color;
                m.SurfaceForegroundPatternColor = color;
            }
            if (m.StructuralAssetId == ElementId.InvalidElementId)
            {
                try
                {
                    var asset = new StructuralAsset(nombre, activo);
                    PropertySetElement pse = PropertySetElement.Create(doc, asset);
                    m.SetMaterialAspectByPropertySet(MaterialAspect.Structural, pse.Id);
                }
                catch (System.Exception ex)
                {
                    informe.Aviso("Preparación", "No se pudo dar activo estructural a " + nombre + ": " + ex.Message);
                }
            }
        }
    }
}
