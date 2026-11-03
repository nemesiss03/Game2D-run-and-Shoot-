using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Evita el hueco del cuello cuando la cabeza gira.
/// Cuerpo y cabeza son dos recortes del mismo dibujo: donde estaba la cabeza el cuerpo quedó vacío,
/// y al girar la cabeza alrededor del cuello se veía ese hueco (el fondo asomaba por detrás de la mandíbula).
/// Aquí se rellena, en cada fotograma de cuerpo, la parte del hueco pegada al cuerpo (cuello y hombros),
/// prolongando los colores del propio cuerpo. El resto del hueco sigue vacío: es aire.
/// Menús: Spirit Dragon / Rellenar cuello del dragón  ·  Spirit Dragon / Probar giro de cabeza.
/// </summary>
public static class GeneradorCuello
{
    const string Carpeta = "Assets/Generated_Images/DragonPartes";
    const int Ancho = 512, Alto = 256;

    /// <summary>Radio (en píxeles) de la zona del cuello que se rellena, alrededor del pivote.</summary>
    const float RadioCuello = 25f;

    /// <summary>Por encima de esta altura (origen abajo) no se rellena: ahí empieza la cabeza.</summary>
    const int AlturaMaxima = 96;

    /// <summary>Las motas sueltas del cuerpo se buscan por encima de esta altura (origen abajo).</summary>
    const float AlturaMotas = 60f;

    /// <summary>Lo que se añade queda más oscuro: es la sombra bajo la barbilla.</summary>
    const float Sombra = 0.72f;

    /// <summary>Restos sueltos del recorte (motas de contorno) por debajo de este tamaño se borran.</summary>
    const int TamanoMota = 90;

    // Pivote del cuello en el lienzo de 512 x 256 con el origen ABAJO a la izquierda: (256, 68)
    static readonly Vector2 PivoteCuello = new Vector2(256f, 68f);

    static readonly string[] Bases =
    {
        "idle_01", "idle_02", "idle_03", "idle_04", "idle_05", "idle_06",
        "walk_01", "walk_02", "walk_03", "walk_04", "walk_05", "walk_06",
        "flyKick_01", "flyKick_02", "flyKick_03", "flyKick_04", "flyKick_05", "flyKick_06"
    };

    // ------------------------------------------------------------------ relleno
    [MenuItem("Spirit Dragon/Rellenar cuello del dragón")]
    public static void Rellenar()
    {
        int hechos = 0;

        foreach (string b in Bases)
        {
            string rutaCuerpo = Carpeta + "/" + b + "_cuerpo.png";
            string rutaCabeza = Carpeta + "/" + b + "_cabeza.png";
            if (!File.Exists(rutaCuerpo) || !File.Exists(rutaCabeza)) continue;

            Color[] cuerpo = Leer(rutaCuerpo);
            Color[] cabeza = Leer(rutaCabeza);

            int anadidos = RellenarHueco(cuerpo, cabeza);

            // La cabeza también trae motas sueltas de contorno que se verían girar junto al hocico
            int motasCabeza = BorrarMotas(cabeza, 70, -1f);
            if (motasCabeza > 0) Escribir(rutaCabeza, cabeza);

            if (anadidos == 0 && motasCabeza == 0) continue;

            Escribir(rutaCuerpo, cuerpo);
            hechos++;
        }

        AssetDatabase.Refresh();
        Debug.Log("GeneradorCuello: cuello rellenado en " + hechos + " fotogramas.");
    }

    /// <summary>Devuelve cuántos píxeles cambió (rellenados más motas borradas).</summary>
    static int RellenarHueco(Color[] cuerpo, Color[] cabeza)
    {
        int cambios = BorrarMotas(cuerpo);

        // Color del cuello: lo que hay justo debajo del pivote (el torso), oscurecido
        Color suma = Color.clear;
        int n = 0;
        for (int y = (int)PivoteCuello.y - 22; y <= (int)PivoteCuello.y - 8; y++)
        {
            for (int x = (int)PivoteCuello.x - 10; x <= (int)PivoteCuello.x + 10; x++)
            {
                Color c = cuerpo[y * Ancho + x];
                if (c.a < 0.9f) continue;
                if (c.r < c.g * 1.8f || c.r < 0.35f) continue;   // solo el rojo del torso, sin dorado ni gris
                suma += c;
                n++;
            }
        }

        if (n == 0) return cambios;

        Color medio = suma / n;
        Color cuello = new Color(medio.r * Sombra, medio.g * Sombra, medio.b * Sombra, 1f);

        // Hueco (hay cabeza y el cuerpo está vacío) cerca del cuello
        for (int y = 0; y < Alto; y++)
        {
            for (int x = 0; x < Ancho; x++)
            {
                int i = y * Ancho + x;
                if (cabeza[i].a <= 0.1f || cuerpo[i].a >= 0.1f) continue;

                float d = Vector2.Distance(new Vector2(x, y), PivoteCuello);
                if (d > RadioCuello) continue;

                cuerpo[i] = cuello;
                cambios++;
            }
        }

        return cambios;
    }

    /// <summary>
    /// Borra grupos pequeños de píxeles sueltos por encima del cuello (restos del contorno de la cabeza):
    /// con la cabeza girada asomaban como motas flotando junto al hocico.
    /// </summary>
    static int BorrarMotas(Color[] cuerpo, int tamanoMaximo = TamanoMota, float alturaMinima = AlturaMotas)
    {
        int[] etiqueta = new int[cuerpo.Length];
        int borrados = 0, grupo = 0;
        Stack<int> pila = new Stack<int>();
        List<int> miembros = new List<int>();

        for (int y0 = 0; y0 < Alto; y0++)
        {
            for (int x0 = 0; x0 < Ancho; x0++)
            {
                int i0 = y0 * Ancho + x0;
                if (etiqueta[i0] != 0 || cuerpo[i0].a < 0.1f) continue;

                grupo++;
                miembros.Clear();
                pila.Push(i0);
                etiqueta[i0] = grupo;
                long sumaY = 0;

                while (pila.Count > 0)
                {
                    int i = pila.Pop();
                    miembros.Add(i);
                    sumaY += i / Ancho;

                    int x = i % Ancho, y = i / Ancho;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= Ancho || ny >= Alto) continue;

                            int j = ny * Ancho + nx;
                            if (etiqueta[j] != 0 || cuerpo[j].a < 0.1f) continue;

                            etiqueta[j] = grupo;
                            pila.Push(j);
                        }
                    }
                }

                float alturaMedia = sumaY / (float)miembros.Count;

                // Solo motas pequeñas y altas (a la altura de la cabeza), nunca el cuerpo ni las garras
                if (miembros.Count < tamanoMaximo && alturaMedia > alturaMinima)
                {
                    foreach (int i in miembros) cuerpo[i] = Color.clear;
                    borrados += miembros.Count;
                }
            }
        }

        return borrados;
    }

    // ------------------------------------------------------------------ prueba visual
    /// <summary>Dibuja la cabeza girada a varios ángulos sobre el cuerpo y guarda la imagen en la carpeta que se indica.</summary>
    [MenuItem("Spirit Dragon/Probar giro de cabeza")]
    public static void Probar()
    {
        ProbarEn(Path.Combine(Path.GetTempPath(), "giro_cabeza.png"), new[] { "idle_01", "walk_03", "volar_02" }, new float[] { -75f, -40f, 0f, 40f, 75f });
    }

    public static void ProbarEn(string destino, string[] fotogramas, float[] angulos)
    {
        // Recorte del lienzo (zona del dragón)
        int x0 = 150, x1 = 340, y0 = 10, y1 = 210;
        int cw = x1 - x0, ch = y1 - y0, escala = 2;

        int columnas = angulos.Length, filas = fotogramas.Length;
        Texture2D salida = new Texture2D((cw * escala + 6) * columnas, (ch * escala + 6) * filas, TextureFormat.RGBA32, false);
        Color fondo = new Color(0.62f, 0.80f, 0.94f, 1f);
        Color[] limpio = new Color[salida.width * salida.height];
        for (int i = 0; i < limpio.Length; i++) limpio[i] = fondo;
        salida.SetPixels(limpio);

        for (int f = 0; f < filas; f++)
        {
            string baseCuerpo = fotogramas[f];
            string baseCabeza = baseCuerpo.StartsWith("volar") ? "idle_01" : baseCuerpo;

            Color[] cuerpo = Leer(Carpeta + "/" + baseCuerpo + "_cuerpo.png");
            Color[] cabeza = Leer(Carpeta + "/" + baseCabeza + "_cabeza.png");

            for (int a = 0; a < columnas; a++)
            {
                float rad = angulos[a] * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);

                int ox = a * (cw * escala + 6);
                int oy = (filas - 1 - f) * (ch * escala + 6);

                for (int y = 0; y < ch * escala; y++)
                {
                    for (int x = 0; x < cw * escala; x++)
                    {
                        int px = x0 + x / escala, py = y0 + y / escala;
                        Color c = fondo;

                        Color pc = cuerpo[py * Ancho + px];
                        if (pc.a > 0.02f) c = Color.Lerp(c, new Color(pc.r, pc.g, pc.b), pc.a);

                        // La cabeza girada: transformación inversa alrededor del cuello
                        float dx = px - PivoteCuello.x, dy = py - PivoteCuello.y;
                        float sx = PivoteCuello.x + dx * cos + dy * sin;
                        float sy = PivoteCuello.y - dx * sin + dy * cos;

                        int ix = Mathf.RoundToInt(sx), iy = Mathf.RoundToInt(sy);
                        if (ix >= 0 && iy >= 0 && ix < Ancho && iy < Alto)
                        {
                            Color ph = cabeza[iy * Ancho + ix];
                            if (ph.a > 0.02f) c = Color.Lerp(c, new Color(ph.r, ph.g, ph.b), ph.a);
                        }

                        salida.SetPixel(ox + x, oy + y, c);
                    }
                }
            }
        }

        salida.Apply();
        File.WriteAllBytes(destino, salida.EncodeToPNG());
        Object.DestroyImmediate(salida);
    }

    // ------------------------------------------------------------------ imagen
    static Color[] Leer(string ruta)
    {
        Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        t.LoadImage(File.ReadAllBytes(ruta));
        Color[] c = t.GetPixels();
        Object.DestroyImmediate(t);
        return c;
    }

    static void Escribir(string ruta, Color[] px)
    {
        Texture2D t = new Texture2D(Ancho, Alto, TextureFormat.RGBA32, false);
        t.SetPixels(px);
        t.Apply();
        File.WriteAllBytes(ruta, t.EncodeToPNG());
        Object.DestroyImmediate(t);
    }
}
