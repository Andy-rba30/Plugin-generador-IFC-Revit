using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Arba.Comun;
using ArbaModeloPrueba.Core;
using ArbaModeloPrueba.Zonas;

namespace ArbaModeloPrueba.UI
{
    /// <summary>Elección de las zonas que se generan.</summary>
    internal sealed class VentanaZonas : Window
    {
        private readonly List<(Zona zona, CheckBox casilla)> _filas = new List<(Zona, CheckBox)>();
        public List<Zona> Elegidas => _filas.Where(f => f.casilla.IsChecked == true).Select(f => f.zona).ToList();

        public VentanaZonas(IEnumerable<Zona> zonas)
        {
            Title = "ARBA - Modelo de prueba";
            Width = 720;
            Height = 640;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResizeWithGrip;

            var raiz = new DockPanel { Margin = new Thickness(12) };

            var cabecera = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10),
                Text = "Crea elementos nativos de Revit (columnas, vigas, zapatas, muros, losas, terreno, vistas) en una zona " +
                       "rotulada por add-in, sobre los niveles «PRUEBA ARBA». Usa un proyecto nuevo con la plantilla estructural. " +
                       "Para la zona «Armado de muestra» ejecuta antes «Tipos de barra Perú». Cada elemento va en su propio paso: " +
                       "si uno falla, se anota en el informe y el resto se crea igual. Ctrl+Z deshace todo."
            };
            DockPanel.SetDock(cabecera, Dock.Top);
            raiz.Children.Add(cabecera);

            var botones = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            var todas = new Button { Content = "Todas", Width = 90, Margin = new Thickness(0, 0, 8, 0) };
            var ninguna = new Button { Content = "Ninguna", Width = 90, Margin = new Thickness(0, 0, 24, 0) };
            var generar = new Button { Content = "Generar", Width = 110, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
            var cancelar = new Button { Content = "Cancelar", Width = 90, IsCancel = true };
            todas.Click += (s, e) => _filas.ForEach(f => f.casilla.IsChecked = true);
            ninguna.Click += (s, e) => _filas.ForEach(f => f.casilla.IsChecked = false);
            generar.Click += (s, e) => { DialogResult = true; };
            botones.Children.Add(todas);
            botones.Children.Add(ninguna);
            botones.Children.Add(generar);
            botones.Children.Add(cancelar);
            DockPanel.SetDock(botones, Dock.Bottom);
            raiz.Children.Add(botones);

            var lista = new StackPanel();
            foreach (Zona z in zonas)
            {
                var texto = new StackPanel();
                texto.Children.Add(new TextBlock { Text = z.Id + " · " + z.Titulo, FontWeight = FontWeights.SemiBold });
                texto.Children.Add(new TextBlock { Text = z.Plugins, Foreground = RevitTheme.Muted, TextWrapping = TextWrapping.Wrap });
                var casilla = new CheckBox { Content = texto, IsChecked = true, Margin = new Thickness(0, 4, 0, 4) };
                _filas.Add((z, casilla));
                lista.Children.Add(casilla);
            }
            raiz.Children.Add(new ScrollViewer { Content = lista, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });

            Content = raiz;
            RevitTheme.Apply(this);
        }
    }

    /// <summary>Informe final: lo creado, los avisos y los errores por zona.</summary>
    internal sealed class VentanaInforme : Window
    {
        public VentanaInforme(Informe informe, string ruta, string cabecera)
        {
            Title = "ARBA - Modelo de prueba: informe";
            Width = 900;
            Height = 680;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResizeWithGrip;

            var raiz = new DockPanel { Margin = new Thickness(12) };
            int ok = informe.Entradas.Count(e => e.Nivel == Nivel.Ok);
            var resumen = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8),
                Text = (cabecera != null ? cabecera + "\n" : "") +
                       ok + " pasos correctos, " + informe.Avisos + " avisos, " + informe.Errores + " errores." +
                       (ruta != null ? "\nInforme guardado en " + ruta : "") +
                       "\nAbre «PRUEBA ARBA - Planta Nivel 1» y «PRUEBA ARBA - 3D»: cada zona lleva un rótulo con lo que hay que probar.",
                Foreground = informe.Errores > 0 ? RevitTheme.Error : RevitTheme.Ok
            };
            DockPanel.SetDock(resumen, Dock.Top);
            raiz.Children.Add(resumen);

            var botones = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
            var copiar = new Button { Content = "Copiar", Width = 90, Margin = new Thickness(0, 0, 8, 0) };
            var abrir = new Button { Content = "Abrir archivo", Width = 110, Margin = new Thickness(0, 0, 8, 0), IsEnabled = ruta != null };
            var cerrar = new Button { Content = "Cerrar", Width = 90, IsCancel = true, IsDefault = true };
            string texto = informe.ToString();
            copiar.Click += (s, e) => { try { Clipboard.SetText(texto); } catch { } };
            abrir.Click += (s, e) => { try { Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true }); } catch { } };
            botones.Children.Add(copiar);
            botones.Children.Add(abrir);
            botones.Children.Add(cerrar);
            DockPanel.SetDock(botones, Dock.Bottom);
            raiz.Children.Add(botones);

            raiz.Children.Add(new TextBox
            {
                Text = texto,
                IsReadOnly = true,
                FontFamily = new FontFamily("Consolas"),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.NoWrap
            });

            Content = raiz;
            RevitTheme.Apply(this);
        }
    }
}
