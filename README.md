# ARBA Modelo de prueba (Plugin-generador-IFC-Revit)

Add-in de **Revit 2027** que genera, en el proyecto abierto, un modelo de prueba con **elementos nativos de Revit**
(columnas, vigas, zapatas, muros, losas, terreno, vistas y anotaciones) para probar todos los add-ins de la suite ARBA
en todas sus variantes, incluidos los casos que deben rechazar.

## Por qué no es un archivo IFC

Al abrir un IFC, Revit convierte los elementos en formas importadas (`DirectShape`), no en columnas, vigas, zapatas,
muros o losas. Los add-ins ARBA exigen anfitriones de armadura válidos (`RebarHostData.IsValidHost`), instancias de
familia con `LocationCurve` o suelos con boceto, así que sobre un IFC casi ninguno funcionaría. Este add-in crea los
elementos directamente con la API de Revit.

## Uso

1. Crea un proyecto nuevo con la **plantilla estructural** de Revit 2027 (trae tipos de losa de cimentación,
   cimentación de muro y Toposolid, que algunas zonas necesitan).
2. Ejecuta **Tipos de barra Perú** (pestaña *Peru*): crea los tipos de barra que usan los add-ins de armado y la zona
   «Armado de muestra».
3. Pestaña **ARBA** > panel **Pruebas** > **Modelo de prueba**. Elige las zonas (todas por defecto) y pulsa
   **Generar**.
4. Al terminar se abre el informe: lo creado, los avisos y los errores por zona (también se guarda en `%TEMP%`).
   Se abre la vista **PRUEBA ARBA - Planta Nivel 1**; cada zona lleva un rótulo con lo que hay que probar.

Todo se hace en un único grupo de transacciones: **Ctrl+Z** lo deshace. Cada elemento va en su propio paso, así que
si uno falla se anota en el informe y el resto se crea igual. Se puede volver a ejecutar para una zona sola: reutiliza
niveles, tipos y familias ya creados.

Niveles: **PRUEBA ARBA - Cimentación** (-1.00), **Nivel 1** (0.00), **Nivel 2** (+3.00), **Nivel 3** (+6.00).
Tipos, familias y materiales empiezan por `ARBA-P`.

## Zonas

| Zona | Add-ins | Qué contiene |
|---|---|---|
| **ZAP** Zapatas (x=0, y=0) | Acero-Zapatas | Aislada con columna; combinada con 2 columnas; en L; con pase; escalonada; piramidal; fondo a dos cotas; invertida; losa de cimentación con huecos; corrida bajo muro. Rechazos: base inclinada, canto 8 cm, de acero, columna fuera |
| **CC** Cimientos corridos (x=45) | Acero-cimientos-corridos | Sobrecimientos (muro) + cimentación de muro en recto, L, U, anillo, T, cruce, escalón de fondo, muro partido en T; suelos en franja en L, T y cruz; viga de cimentación; T invertida; escalonada; muro con columna unida. Rechazos: muro curvo, suelo no estructural, suelo con arco |
| **BLQ** Bloques con foso (x=90) | Fosa_transformadores | Foso perimetral con murete; canaleta en un lado; foso central; dos fosos de distinta profundidad. Rechazos: sin foso, voladizo |
| **MC** Muros de contención (x=125) | Acero-automatico | T simétrico, L, T asimétrico, esquinero en L de una pieza, vano de puerta, vano de ventana, cruzados, unidos en esquina, alzado de 5 m. Rechazos: contrafuertes, alzado escalonado, dos sólidos, muro de sistema |
| **ENC** Encofrado (x=160) | Encofrado-automatico | Muro en T con losa unida y sin unir, muro perpendicular unido y sin unir, columna adosada, albañilería, modelo genérico a 15 mm, muro de sistema, muro curvo, contrafuertes |
| **COL** Columnas (y=-65) | Acero-columnas | Rectangulares, L, T, U, cruz, Z, giradas, tres iguales. Rechazos: circular, hueca, con capitel, de acero, dos sólidos |
| **VIG** Vigas (x=35) | Acero-vigas | Pórtico de 3 tramos + voladizo unidos a columnas, losa unida (modo *cut*), T, L, T invertida, I, cartelas, escalón, 15 m (empalmes), inclinada. Rechazos: en arco, ancho variable, de acero, dos sólidos |
| **LOSA** Losas (x=80) | Acero-losas | Maciza y aligerada con apoyo interior, en L con hueco, dos paños, sin vigas, borde en arco, vigas por debajo, viga oblicua. Rechazos: inclinada, dos capas, 3 cm, no estructural |
| **NUD** Nudos (x=120) | Uniones | Nudo interior con 4 vigas, colineales de distinto canto, esquina, columna en T, distinta cota, cubierta. Avisos: oblicua, hueco, viga más ancha, columna girada, viga curva |
| **ELO** Dividir/unir losas (x=165) | Editor de losas | Losas con líneas de corte (recta, diagonal, rejilla, por vértice), en L, U, con hueco, hueco circular; pares para unir: adyacentes, 0.5 mm, 5 mm, solapadas, anillo. Rechazos: tipos distintos, inclinada |
| **EXC** Excavaciones (y=-115) | Excavaciones | Toposolid en pendiente; zapatas, contiguas solapadas, viga y losa de cimentación, zapata más honda, pilote; plataformas por líneas, región rellena y suelo. Rechazos: zapata sobre el terreno, líneas abiertas |
| **MET** Metrados (x=60) | Exportación de metrados | Concreto (circular, f'c 280, material sin la palabra, losa con acabado, cubierta), acero (W12X26, HSS, sin material, columna W, cubierta), conexiones (PL, perno, espárrago, rigidizador, placa base), genéricos, madera, 2 tablas |
| **ARM** Armado de muestra (x=110) | Borrar armado, migración ARBA | Barras con particiones antiguas `VIG-VA1` y `ZAP-ZA2`, barra sin partición, refuerzo por área, por camino y malla |
| **ROT** Rotar Norte (x=150) | Rotar Norte | Rejillas (una anclada), plano de referencia, columna anclada, habitación, muro cortina, hueco, grupos de modelo y de detalle, cota, etiqueta, texto, región; sección, llamada, alzado, recorte en L + dependiente, planta a Norte verdadero, 3D con caja, bloqueada y perspectiva, plano con ventanas gráficas; Norte verdadero del proyecto a 15° |

## Cómo se crean las familias

Las columnas, vigas, zapatas, bloques y muros de contención son familias creadas por código: se abre la plantilla
`.rft` de su categoría (se busca en la carpeta de plantillas de familia de Revit, en cualquier idioma), se añade un
único sólido (`FreeFormElement`) con la forma exacta, se marca como hormigón (o acero/madera) y se carga en el
proyecto. La geometría es fija: cada largo de viga o alto de columna es una familia distinta (`ARBA-P Viga 25x50
L=6.00`). Si Revit coloca una familia desplazada respecto a lo previsto, el informe lo indica.

## Limitaciones conocidas

- No se ha probado todavía dentro de Revit: compila contra la API de Revit 2027, pero el primer uso puede dar errores
  en algún paso. El informe dice cuál y por qué: pásalo para corregirlo.
- Si no se encuentran plantillas de familia (`.rft`), las zonas con familias fallan. Instala el contenido de Revit.
- La familia `L-Angle` que usan los ángulos de Bloques con foso no se crea: cárgala de la biblioteca de Autodesk.
- La zona ROT cambia el ángulo a Norte verdadero del proyecto a 15° (solo si estaba en 0°).
- No crea vínculos RVT/DWG ni modelos con subproyectos (worksharing).

## Compilar

```powershell
dotnet build -c Release
```

En Debug y en Windows copia el add-in a `%AppData%\Autodesk\Revit\Addins\2027\`. El código común ARBA viene del
submódulo `external/ARBA-comun` (clona con `--recurse-submodules`). También se instala con el instalador de
[ARBA-Revit](https://github.com/Andy-rba30/ARBA-Revit) (componente *Modelo de prueba*).
