using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Arba.Comun;
using Autodesk.Revit.UI;

namespace ArbaModeloPrueba
{
    /// <summary>Añade el botón "Modelo de prueba" en el panel "Pruebas" de la pestaña ARBA.</summary>
    public class App : IExternalApplication
    {
        public const string PanelPruebas = "Pruebas";

        public Result OnStartup(UIControlledApplication app)
        {
            try
            {
                // Primero los paneles comunes en su orden; "Pruebas" queda detrás de ellos.
                ArbaRibbon.Ensure(app);
                string ensamblado = Assembly.GetExecutingAssembly().Location;
                var datos = new PushButtonData("ARBA_Pruebas_ModeloPrueba", "Modelo\nde prueba", ensamblado,
                                               typeof(GenerarModeloCommand).FullName)
                {
                    ToolTip = "Genera un modelo con elementos nativos para probar todos los add-ins ARBA",
                    LongDescription = "Crea en el proyecto abierto niveles, vistas y una zona rotulada por add-in (zapatas, " +
                                      "cimientos corridos, bloques con foso, muros de contención, encofrado, columnas, vigas, " +
                                      "losas, nudos, edición de losas, excavaciones, metrados, armado de muestra y Rotar Norte), " +
                                      "con las variantes que cada uno debe armar y los casos que debe rechazar. " +
                                      "Usa un proyecto nuevo con la plantilla estructural.",
                    LargeImage = Icono(32),
                    Image = Icono(16)
                };
                ArbaRibbon.AddButton(app, PanelPruebas, datos);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("ARBA", "No se pudo añadir el botón Modelo de prueba a la cinta: " + ex.Message +
                                "\nEl comando sigue disponible en Complementos > Herramientas externas.");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;

        /// <summary>Icono: pórtico (dos columnas y una viga) sobre una zapata, con una marca de verificación.</summary>
        private static BitmapSource Icono(int size)
        {
            double s = size / 32.0;
            var visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                var concreto = new SolidColorBrush(Color.FromRgb(0xD9, 0xD9, 0xD9));
                var borde = new Pen(new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)), 1.1 * s);
                var ok = new Pen(new SolidColorBrush(Color.FromRgb(0x2E, 0x9E, 0x4F)), 2.4 * s)
                { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };

                dc.DrawRectangle(concreto, borde, new Rect(3 * s, 5 * s, 20 * s, 4 * s));    // viga
                dc.DrawRectangle(concreto, borde, new Rect(4 * s, 9 * s, 4 * s, 13 * s));    // columna izquierda
                dc.DrawRectangle(concreto, borde, new Rect(18 * s, 9 * s, 4 * s, 13 * s));   // columna derecha
                dc.DrawRectangle(concreto, borde, new Rect(1 * s, 22 * s, 24 * s, 5 * s));   // zapata corrida
                var marca = new StreamGeometry();
                using (StreamGeometryContext g = marca.Open())
                {
                    g.BeginFigure(new Point(19 * s, 21 * s), false, false);
                    g.LineTo(new Point(23.5 * s, 26 * s), true, false);
                    g.LineTo(new Point(31 * s, 15 * s), true, false);
                }
                dc.DrawGeometry(null, ok, marca);
            }
            var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(visual);
            bmp.Freeze();
            return bmp;
        }
    }
}
