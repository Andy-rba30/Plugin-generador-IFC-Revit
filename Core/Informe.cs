using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ArbaModeloPrueba.Core
{
    internal enum Nivel { Ok, Aviso, Error }

    internal sealed class Entrada
    {
        public Nivel Nivel;
        public string Zona;
        public string Texto;
    }

    /// <summary>Registro de lo creado, de los avisos y de los errores, por zona de prueba.</summary>
    internal sealed class Informe
    {
        private readonly List<Entrada> _entradas = new List<Entrada>();

        public IReadOnlyList<Entrada> Entradas => _entradas;
        public int Errores => _entradas.Count(e => e.Nivel == Nivel.Error);
        public int Avisos => _entradas.Count(e => e.Nivel == Nivel.Aviso);

        public void Ok(string zona, string texto) => _entradas.Add(new Entrada { Nivel = Nivel.Ok, Zona = zona, Texto = texto });
        public void Aviso(string zona, string texto) => _entradas.Add(new Entrada { Nivel = Nivel.Aviso, Zona = zona, Texto = texto });
        public void Error(string zona, string texto) => _entradas.Add(new Entrada { Nivel = Nivel.Error, Zona = zona, Texto = texto });

        public override string ToString()
        {
            var sb = new StringBuilder();
            string zona = null;
            foreach (var e in _entradas)
            {
                if (e.Zona != zona)
                {
                    zona = e.Zona;
                    sb.AppendLine();
                    sb.AppendLine("== " + zona);
                }
                string marca = e.Nivel == Nivel.Ok ? "  ok     " : e.Nivel == Nivel.Aviso ? "  AVISO  " : "  ERROR  ";
                sb.AppendLine(marca + e.Texto);
            }
            return sb.ToString().TrimStart();
        }

        /// <summary>Guarda el informe en %TEMP% y devuelve la ruta (null si no se pudo).</summary>
        public string Guardar()
        {
            try
            {
                string ruta = Path.Combine(Path.GetTempPath(), "ARBA-ModeloPrueba-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
                File.WriteAllText(ruta, ToString(), Encoding.UTF8);
                return ruta;
            }
            catch
            {
                return null;
            }
        }
    }
}
