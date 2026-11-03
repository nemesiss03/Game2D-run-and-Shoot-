using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

#pragma warning disable CS0618

/// <summary>
/// Nivel 2 como una montaña que se sube hacia arriba y a la derecha, con roca nevada
/// (tileset rock_ice_alt_tileset_3) y un fondo de montañas generado por código.
/// Menú: Sky Wars / Reconstruir Nivel 2 (montaña). Rehace solo el terreno y los objetos
/// generados; lo que hayas añadido a mano en la escena (por ejemplo tus pinchos) se conserva.
/// </summary>
public static partial class ConstructorNiveles
{
    const string PngRoca = "Assets/Imagen/Tittle/TilesMoreItch/rock_ice_alt_tileset_3.png";
    const string CarpetaTilesRoca = "Assets/Tittle(Piso)/Roca";
    const string CarpetaFondoMontana = "Assets/Imagen/Montana";

    static Tile[] rocaNieve, rocaRelleno, rocaFina;

    // ------------------------------------------------------------------ entrada
    [MenuItem("Sky Wars/Reconstruir Nivel 2 (montaña)")]
    public static void ReconstruirNivel2Montana()
    {
        EditorSceneManager.SaveOpenScenes();

        PrepararRoca();
        GenerarFondoMontana();
        CargarPrefabsDeNivel();

        EditorSceneManager.OpenScene(RutaN2, OpenSceneMode.Single);
        BorrarGeneradosNivel2();
        Iniciar();

        Tilemap suelo = GameObject.Find("Ground").GetComponent<Tilemap>();
        suelo.ClearAllTiles();

        // ---- Perfil de la montaña: (x inicial, x final sin incluir, altura de la fila superior)
        // Cada tramo sube 1 o 2 casillas respecto al anterior (3 o más piden doble salto).
        Dictionary<int, int> altura = new Dictionary<int, int>();
        Tramo(altura, -13, -8, 60);        // pared izquierda
        Tramo(altura, -8, 16, -4);         // valle de salida
        Tramo(altura, 16, 26, -2);
        Tramo(altura, 26, 29, -4);         // grieta (se sale saltando hacia la izquierda)
        Tramo(altura, 29, 38, 0);
        Tramo(altura, 38, 42, 2);
        Tramo(altura, 42, 54, 4);
        Tramo(altura, 54, 57, 2);          // grieta
        Tramo(altura, 57, 64, 6);
        Tramo(altura, 64, 88, 8);          // terraza ancha: primera zona de pelea
        Tramo(altura, 88, 96, 10);
        Tramo(altura, 96, 110, 8);         // fondo del barranco bajo las rocas flotantes
        Tramo(altura, 110, 118, 16);
        Tramo(altura, 118, 124, 18);
        Tramo(altura, 124, 128, 20);
        Tramo(altura, 128, 134, 22);
        Tramo(altura, 134, 140, 24);
        Tramo(altura, 140, 146, 26);
        Tramo(altura, 146, 149, 24);       // grieta
        Tramo(altura, 149, 156, 28);
        Tramo(altura, 156, 162, 30);
        Tramo(altura, 162, 192, 32);       // cima ancha: pelea final y bandera
        Tramo(altura, 192, 198, 80);       // pared derecha

        PintarMontana(suelo, altura);

        // Rocas flotantes que unen el hueco entre las terrazas 10 y 16 (se sube saltando de una a otra)
        PintarRocaFlotante(suelo, 97, 101, 11);
        PintarRocaFlotante(suelo, 102, 106, 13);
        PintarRocaFlotante(suelo, 106, 110, 15);

        // Piedras de paso sueltas, más pequeñas, junto a los tramos más altos
        PintarRocaFina(suelo, 46, 50, 8);
        PintarRocaFina(suelo, 68, 72, 13);
        PintarRocaFina(suelo, 112, 116, 21);

        ReconstruirColision(suelo);

        CrearParallaxMontana();

        // ---- Cofres
        Transform objetos = Padre("Objetos");
        foreach (int x in new[] { 8, 20, 33, 47, 60, 70, 80, 92, 99, 104, 108, 113, 121, 131, 143, 152, 159, 168, 187 })
            Poner(cofre, x + 0.5f, objetos);

        // ---- Voladores desde portales, repartidos por toda la subida
        List<Vector2> anclas = new List<Vector2>();
        foreach (int x in new[] { 22, 34, 48, 76, 92, 112, 126, 140, 154, 170 })
        {
            float s;
            if (superficie.TryGetValue(x, out s)) anclas.Add(new Vector2(x + 0.5f, s + 3.5f));
        }
        GameObject esbirro = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabEsbirro);
        CrearPortalesPuntos(esbirro, anclas, 8f, 13f, 2);

        CrearAmbiente(true);
        CrearAbismo(-8f, 192f);

        GameObject banderaN2 = CrearBandera(184, "Boss");

        CrearZona("ZonaMontana", 76f, superficie[76], 3, null);
        CrearZona("ZonaCima", 177f, superficie[177], 3, banderaN2);

        FinNivel(new Vector2(0f, -1.62f));

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), RutaN2);
        Debug.Log("ConstructorNiveles: Nivel 2 (montaña) reconstruido.");
    }

    static void CargarPrefabsDeNivel()
    {
        string c = CarpetaPrefabs + "/";
        moneda = AssetDatabase.LoadAssetAtPath<GameObject>(c + "Moneda.prefab");
        gema = AssetDatabase.LoadAssetAtPath<GameObject>(c + "Gema.prefab");
        cofre = AssetDatabase.LoadAssetAtPath<GameObject>(c + "Cofre.prefab");
        bandera = AssetDatabase.LoadAssetAtPath<GameObject>(c + "Bandera.prefab");
        voladorDirecto = AssetDatabase.LoadAssetAtPath<GameObject>(c + "VoladorDirecto.prefab");
        voladorAzar = AssetDatabase.LoadAssetAtPath<GameObject>(c + "VoladorAzar.prefab");

        if (moneda == null || gema == null || cofre == null || bandera == null || voladorDirecto == null || voladorAzar == null)
            CrearPrefabs();   // falta alguno: se crean de nuevo
    }

    static void BorrarGeneradosNivel2()
    {
        foreach (string n in BorrarPorNombre.Concat(new[] { "Abismo" }))
        {
            GameObject g;
            while ((g = GameObject.Find(n)) != null) UnityEngine.Object.DestroyImmediate(g);
        }
    }

    // ------------------------------------------------------------------ roca
    static void PrepararRoca()
    {
        // Cortar la hoja en celdas de 16 x 16 (7 columnas, 6 filas)
        TextureImporter imp = AssetImporter.GetAtPath(PngRoca) as TextureImporter;
        if (imp == null) { Debug.LogError("No encuentro " + PngRoca); return; }

        const int columnas = 7, filas = 6, celda = 16;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = celda;
        imp.filterMode = FilterMode.Point;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;

        List<SpriteMetaData> metas = new List<SpriteMetaData>();
        for (int f = 0; f < filas; f++)
        {
            for (int c = 0; c < columnas; c++)
            {
                metas.Add(new SpriteMetaData
                {
                    name = "roca_" + c + "_" + f,
                    rect = new Rect(c * celda, (filas - 1 - f) * celda, celda, celda),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                });
            }
        }

        imp.spritesheet = metas.ToArray();
        imp.SaveAndReimport();

        Directory.CreateDirectory(CarpetaTilesRoca);

        Sprite[] sprites = CargarSprites(PngRoca);

        rocaNieve = Enumerable.Range(0, 4).Select(c => CrearTileRoca(sprites, c, 0)).ToArray();
        rocaFina = Enumerable.Range(0, 4).Select(c => CrearTileRoca(sprites, c, 3)).ToArray();

        List<Tile> relleno = new List<Tile>();
        for (int f = 1; f <= 2; f++)
            for (int c = 0; c < 4; c++)
                relleno.Add(CrearTileRoca(sprites, c, f));
        rocaRelleno = relleno.ToArray();

        AssetDatabase.SaveAssets();
    }

    static Tile CrearTileRoca(Sprite[] sprites, int c, int f)
    {
        string nombre = "roca_" + c + "_" + f;
        string ruta = CarpetaTilesRoca + "/Roca_" + c + "_" + f + ".asset";

        Sprite sp = sprites.FirstOrDefault(s => s.name == nombre);

        Tile t = AssetDatabase.LoadAssetAtPath<Tile>(ruta);
        if (t == null)
        {
            t = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(t, ruta);
        }

        t.sprite = sp;
        t.colliderType = Tile.ColliderType.Grid;   // colisión cuadrada y lisa: la roca irregular no se engancha
        EditorUtility.SetDirty(t);
        return t;
    }

    /// <summary>Montaña sólida columna a columna: nieve arriba, roca debajo.</summary>
    static void PintarMontana(Tilemap tm, Dictionary<int, int> altura)
    {
        foreach (KeyValuePair<int, int> kv in altura)
        {
            int x = kv.Key, techo = kv.Value;

            for (int y = FondoSuelo; y <= techo; y++)
            {
                Tile t = y == techo
                    ? rocaNieve[Hash(x, y) % rocaNieve.Length]
                    : rocaRelleno[Hash(x, y) % rocaRelleno.Length];
                tm.SetTile(new Vector3Int(x, y, 0), t);
            }

            if (techo < 40) superficie[x] = techo + 1f;
        }
    }

    /// <summary>Roca flotante con forma de cuña: ancha arriba y cada fila más estrecha hacia abajo.</summary>
    static void PintarRocaFlotante(Tilemap tm, int x0, int x1, int filaSuperior)
    {
        int ancho = x1 - x0;

        for (int r = 0; ; r++)
        {
            int a = x0 + r, b = x1 - r;
            if (b - a < 1 || r > 3) break;

            for (int x = a; x < b; x++)
            {
                Tile t = r == 0
                    ? rocaNieve[Hash(x, filaSuperior) % rocaNieve.Length]
                    : rocaRelleno[Hash(x, filaSuperior - r) % rocaRelleno.Length];
                tm.SetTile(new Vector3Int(x, filaSuperior - r, 0), t);
            }
        }

        for (int x = x0; x < x1; x++) superficie[x] = filaSuperior + 1f;
    }

    /// <summary>Losa de roca de una sola fila (piedra de paso).</summary>
    static void PintarRocaFina(Tilemap tm, int x0, int x1, int fila)
    {
        for (int x = x0; x < x1; x++)
        {
            tm.SetTile(new Vector3Int(x, fila, 0), rocaFina[Hash(x, fila) % rocaFina.Length]);
            superficie[x] = fila + 1f;
        }
    }

    // ------------------------------------------------------------------ fondo
    static void CrearParallaxMontana(string nombre = "FondoParallax", int ordenBase = 0, float alfa = 1f)
    {
        GameObject go = new GameObject(nombre);
        FondoParallax p = go.AddComponent<FondoParallax>();
        p.ordenBase = ordenBase;
        p.alfaInicial = alfa;

        // nombre, seguir cámara en X (0 = pegada a la cámara), en Y, desfase vertical
        string[] capas = { "cielo", "nubes", "lejanas", "cercanas", "bosque", "pinos" };
        float[] fx = { 0f, 0.02f, 0.06f, 0.12f, 0.26f, 0.48f };
        float[] fy = { 1f, 1f, 0.95f, 0.90f, 0.80f, 0.70f };
        float[] dy = { 0f, 6.25f, 6.25f, 6.25f, 6.25f, 6.25f };

        p.capas = new CapaParalax[capas.Length];
        for (int i = 0; i < capas.Length; i++)
        {
            p.capas[i] = new CapaParalax
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaFondoMontana + "/fondo_" + capas[i] + ".png"),
                factorX = fx[i],
                factorY = fy[i],
                desfaseY = dy[i],
                orden = i,
                escala = 1f
            };
        }
    }

    /// <summary>
    /// Capas de 640 x 360 px (40 x 22,5 unidades) que se repiten sin costura en horizontal.
    /// La parte útil del dibujo está en los 160 px de abajo, que es lo que cabe en la cámara.
    /// </summary>
    static void GenerarFondoMontana()
    {
        Directory.CreateDirectory(CarpetaFondoMontana);

        const int W = 640, H = 360;

        EscribirCapa("cielo", CapaCielo(W, H));
        EscribirCapa("nubes", CapaNubes(W, H));
        EscribirCapa("lejanas", CapaMontanas(W, H, 77, 112, 0.92f, new Color(0.62f, 0.68f, 0.86f), new Color(0.46f, 0.53f, 0.76f), new Color(0.84f, 0.88f, 0.97f), 5));
        EscribirCapa("cercanas", CapaMontanas(W, H, 31, 86, 1.05f, new Color(0.42f, 0.50f, 0.72f), new Color(0.30f, 0.38f, 0.60f), new Color(0.66f, 0.73f, 0.90f), 4));
        EscribirCapa("bosque", CapaBosque(W, H));
        EscribirCapa("pinos", CapaPinos(W, H));

        AssetDatabase.Refresh();

        foreach (string n in new[] { "cielo", "nubes", "lejanas", "cercanas", "bosque", "pinos" })
        {
            string ruta = CarpetaFondoMontana + "/fondo_" + n + ".png";
            TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (imp == null) continue;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 16f;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
    }

    static void EscribirCapa(string nombre, Color32[] px)
    {
        const int W = 640, H = 360;
        Texture2D t = new Texture2D(W, H, TextureFormat.RGBA32, false);
        t.SetPixels32(px);
        t.Apply();
        File.WriteAllBytes(CarpetaFondoMontana + "/fondo_" + nombre + ".png", t.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(t);
    }

    static Color32 C32(Color c, float a = 1f) { return new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), (byte)(a * 255f)); }

    static readonly float[,] Bayer = { { 0f, 0.5f }, { 0.75f, 0.25f } };

    /// <summary>Degradado de cielo con bandas, como en el pixel art clásico.</summary>
    static Color32[] CapaCielo(int W, int H)
    {
        Color arriba = new Color(0.36f, 0.64f, 0.90f), medio = new Color(0.60f, 0.80f, 0.95f), abajo = new Color(0.84f, 0.93f, 0.98f);
        Color32[] px = new Color32[W * H];

        for (int y = 0; y < H; y++)
        {
            float t = y / (float)(H - 1);                      // 0 = abajo, 1 = arriba
            // el cielo es más claro cerca del horizonte, que queda a unos 160 px de altura
            float u = Mathf.Clamp01(t * 1.0f);
            Color c = u < 0.5f ? Color.Lerp(abajo, medio, u * 2f) : Color.Lerp(medio, arriba, (u - 0.5f) * 2f);

            for (int x = 0; x < W; x++)
            {
                // Cuantizar en 14 bandas con tramado para que no se vea liso
                float v = (u * 14f) + Bayer[y & 1, x & 1] * 0.9f;
                float q = Mathf.Floor(v) / 14f;
                Color cc = q <= 0.5f ? Color.Lerp(abajo, medio, q * 2f) : Color.Lerp(medio, arriba, (q - 0.5f) * 2f);
                px[y * W + x] = C32(Color.Lerp(c, cc, 0.85f));
            }
        }

        return px;
    }

    static float DistCircular(float a, float b, float W)
    {
        float d = Mathf.Abs(a - b);
        return Mathf.Min(d, W - d);
    }

    static Color32[] CapaNubes(int W, int H)
    {
        Color32[] px = new Color32[W * H];
        System.Random rnd = new System.Random(5);

        Color blanco = new Color(0.97f, 0.99f, 1f), sombra = new Color(0.78f, 0.88f, 0.96f), media = new Color(0.88f, 0.94f, 0.99f);

        for (int n = 0; n < 16; n++)
        {
            float cx = (float)rnd.NextDouble() * W;
            float cy = 100f + (float)rnd.NextDouble() * 200f;     // en la mitad de arriba del dibujo
            float ancho = 70f + (float)rnd.NextDouble() * 120f;
            float alto = 10f + (float)rnd.NextDouble() * 12f;

            // Una nube es la unión de varias elipses aplastadas
            int bultos = 3 + rnd.Next(3);
            for (int b = 0; b < bultos; b++)
            {
                float bx = cx + (b - bultos / 2f) * ancho / bultos * 1.3f;
                float by = cy + (float)(rnd.NextDouble() - 0.5) * alto * 0.7f;
                float rx = ancho / bultos * 1.2f, ry = alto * (0.8f + (float)rnd.NextDouble() * 0.5f);

                for (int y = (int)(by - ry); y <= (int)(by + ry); y++)
                {
                    if (y < 0 || y >= H) continue;
                    for (int dx = (int)-rx; dx <= (int)rx; dx++)
                    {
                        float ex = dx / rx, ey = (y - by) / ry;
                        if (ex * ex + ey * ey > 1f) continue;

                        int x = ((int)bx + dx) % W; if (x < 0) x += W;
                        Color c = ey > 0.35f ? blanco : (ey < -0.35f ? sombra : media);
                        px[y * W + x] = C32(c, 0.92f);
                    }
                }
            }
        }

        return px;
    }

    /// <summary>Cordillera con picos nevados; se repite sin costura porque los picos "dan la vuelta" a los lados.</summary>
    static Color32[] CapaMontanas(int W, int H, int semilla, float alturaMax, float pendiente, Color luz, Color sombra, Color nieve, int cantidad)
    {
        Color32[] px = new Color32[W * H];
        System.Random rnd = new System.Random(semilla);

        float[] cx = new float[cantidad], hh = new float[cantidad];
        for (int i = 0; i < cantidad; i++)
        {
            cx[i] = (i + 0.5f + ((float)rnd.NextDouble() - 0.5f) * 0.5f) * W / cantidad;
            hh[i] = alturaMax * (0.62f + 0.38f * (float)rnd.NextDouble());
        }

        for (int x = 0; x < W; x++)
        {
            float mejor = 0f; int pico = 0; float dxPico = 0f;
            for (int i = 0; i < cantidad; i++)
            {
                float dx = DistCircular(x, cx[i], W);
                float v = hh[i] - dx * pendiente + ((Hash(x / 2, i) % 3) - 1) * 0.8f;   // pequeñas irregularidades
                if (v > mejor) { mejor = v; pico = i; dxPico = x - cx[i]; if (dxPico > W / 2f) dxPico -= W; if (dxPico < -W / 2f) dxPico += W; }
            }

            int cima = Mathf.Clamp(Mathf.RoundToInt(mejor), 0, H - 1);

            for (int y = 0; y <= cima; y++)
            {
                float profundidad = (cima - y);
                Color c = dxPico > 0f ? sombra : luz;                          // la ladera derecha queda en sombra

                // Nieve en la punta con borde en dientes de sierra
                float limiteNieve = hh[pico] * 0.16f + ((Hash(x, 7) % 4) * 1f);
                if (profundidad < limiteNieve && mejor > hh[pico] * 0.55f) c = Color.Lerp(nieve, c, profundidad / (limiteNieve * 1.6f));

                // Bruma hacia la base
                c = Color.Lerp(c, Color.Lerp(luz, new Color(0.80f, 0.88f, 0.96f), 0.55f), Mathf.Clamp01(1f - y / (alturaMax * 0.5f)) * 0.55f);

                px[y * W + x] = C32(c);
            }
        }

        return px;
    }

    static Color32[] CapaBosque(int W, int H)
    {
        Color32[] px = new Color32[W * H];

        Color suelo = new Color(0.22f, 0.50f, 0.44f), copas = new Color(0.17f, 0.42f, 0.38f), claro = new Color(0.30f, 0.60f, 0.50f);

        // Colinas: suma de senos con ciclos enteros para que den la vuelta sin costura
        for (int x = 0; x < W; x++)
        {
            float t = x / (float)W * Mathf.PI * 2f;
            float colina = 38f + 12f * Mathf.Sin(t * 2f + 0.7f) + 7f * Mathf.Sin(t * 5f + 1.9f) + 3f * Mathf.Sin(t * 11f);
            int cima = Mathf.RoundToInt(colina);

            for (int y = 0; y <= cima; y++)
                px[y * W + x] = C32(y > cima - 3 ? claro : suelo);
        }

        // Pinos pequeños sobre las colinas
        System.Random rnd = new System.Random(11);
        for (int x = 4; x < W; x += 5 + rnd.Next(5))
        {
            int baseY = 0;
            for (int y = H - 1; y >= 0; y--) if (px[y * W + x].a > 0) { baseY = y; break; }
            DibujarPino(px, W, H, x, baseY - 1, 14 + rnd.Next(14), copas);
        }

        return px;
    }

    static Color32[] CapaPinos(int W, int H)
    {
        Color32[] px = new Color32[W * H];
        System.Random rnd = new System.Random(23);
        Color oscuro = new Color(0.09f, 0.14f, 0.26f);

        // Pinos grandes y oscuros pegados al borde inferior; los de los extremos se repiten al otro lado
        int cantidad = 17;
        for (int i = 0; i < cantidad; i++)
        {
            int cx = (int)((i + 0.3f + 0.4f * (float)rnd.NextDouble()) * W / cantidad);
            int alto = 52 + rnd.Next(56);
            DibujarPino(px, W, H, cx, 0, alto, oscuro);
        }

        return px;
    }

    /// <summary>Pino de píxeles: copa en pisos escalonados y un tronco corto. Da la vuelta por los lados.</summary>
    static void DibujarPino(Color32[] px, int W, int H, int cx, int baseY, int alto, Color color)
    {
        int tronco = Mathf.Max(3, alto / 9);
        float mitadMax = alto * 0.22f + 2f;
        int pisos = Mathf.Clamp(alto / 12, 3, 7);

        for (int y = 0; y < alto; y++)
        {
            int yy = baseY + y;
            if (yy < 0 || yy >= H) continue;

            float t = y / (float)alto;
            float mitad;

            if (y < tronco) mitad = 1.5f;
            else
            {
                float u = ((t - tronco / (float)alto) * pisos) % 1f;            // posición dentro del piso actual
                mitad = mitadMax * (1f - t) * (0.55f + 0.45f * (1f - u));
            }

            int m = Mathf.CeilToInt(mitad);
            for (int dx = -m; dx <= m; dx++)
            {
                int x = ((cx + dx) % W + W) % W;
                px[yy * W + x] = C32(color);
            }
        }
    }
}
