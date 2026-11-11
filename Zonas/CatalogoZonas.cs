using System.Collections.Generic;

namespace ArbaModeloPrueba.Zonas
{
    /// <summary>Zonas de prueba en el orden en que se generan. Filas: cimentaciones (y = 0), estructura (y = -65), resto (y = -115).</summary>
    internal static class CatalogoZonas
    {
        public static List<Zona> Todas() => new List<Zona>
        {
            new ZonaZapatas(),
            new ZonaCimientos(),
            new ZonaBloques(),
            new ZonaMuros(),
            new ZonaEncofrado(),
            new ZonaColumnas(),
            new ZonaVigas(),
            new ZonaLosas(),
            new ZonaNudos(),
            new ZonaEditorLosas(),
            new ZonaExcavaciones(),
            new ZonaMetrados(),
            new ZonaArmado(),
            new ZonaRotarNorte(),
        };
    }
}
