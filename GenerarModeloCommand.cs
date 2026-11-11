using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;
using ArbaModeloPrueba.Core;
using ArbaModeloPrueba.UI;
using ArbaModeloPrueba.Zonas;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ArbaModeloPrueba
{
    /// <summary>Pide las zonas, genera el modelo de prueba en un grupo de transacciones y muestra el informe.</summary>
    [Transaction(TransactionMode.Manual)]
    public class GenerarModeloCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc?.Document;
            if (doc == null || doc.IsFamilyDocument)
            {
                TaskDialog.Show("ARBA - Modelo de prueba", "Abre un proyecto (no una familia). Lo mejor es un proyecto nuevo con la plantilla estructural.");
                return Result.Cancelled;
            }

            List<Zona> todas = CatalogoZonas.Todas();
            var ventana = new VentanaZonas(todas);
            new WindowInteropHelper(ventana).Owner = commandData.Application.MainWindowHandle;
            if (ventana.ShowDialog() != true) return Result.Cancelled;
            List<Zona> elegidas = ventana.Elegidas;
            if (elegidas.Count == 0) return Result.Cancelled;

            var ctx = new Contexto(doc);
            using (var grupo = new TransactionGroup(doc, "ARBA - Modelo de prueba"))
            {
                grupo.Start();
                if (!ctx.Preparar())
                {
                    grupo.RollBack();
                    MostrarInforme(commandData, ctx, "No se pudieron crear los niveles o materiales de prueba.");
                    return Result.Failed;
                }
                foreach (Zona z in elegidas)
                {
                    try { z.Ejecutar(ctx); }
                    catch (Exception ex) { ctx.Informe.Error(z.Id + " " + z.Titulo, "Zona interrumpida: " + ex.Message); }
                }
                grupo.Assimilate();
            }

            try
            {
                if (ctx.PlantaN1 != null && ctx.PlantaN1.IsValidObject) uidoc.ActiveView = ctx.PlantaN1;
            }
            catch { /* la vista activa no se puede cambiar en este momento */ }

            MostrarInforme(commandData, ctx, null);
            return Result.Succeeded;
        }

        private static void MostrarInforme(ExternalCommandData data, Contexto ctx, string cabecera)
        {
            string ruta = ctx.Informe.Guardar();
            var v = new VentanaInforme(ctx.Informe, ruta, cabecera);
            new WindowInteropHelper(v).Owner = data.Application.MainWindowHandle;
            v.ShowDialog();
        }
    }
}
