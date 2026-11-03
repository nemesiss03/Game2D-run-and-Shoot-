using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

#pragma warning disable CS0618   // TextureImporter.spritesheet (sigue funcionando para cortar sprites por código)

/// <summary>
/// Construye Nivel1 (bosque, terreno casi recto) y Nivel2 (subida por la montaña) con los tiles del
/// proyecto, y crea los prefabs de cofre, moneda, gema y bandera.
/// Menú: Sky Wars / Construir niveles.
/// OJO: reconstruye el terreno y los objetos de los dos niveles desde cero.
/// </summary>
public static class ConstructorNiveles
{
    const string RutaN1 = "Assets/Scenes/Nivel1.unity";
    const string RutaN2 = "Assets/Scenes/Nivel2.unity";
    const string RutaBoss = "Assets/Scenes/Boss.unity";
    const string RutaMenu = "Assets/Scenes/MenuPrincipal.unity";
    const string CarpetaTiles = "Assets/Tittle(Piso)";
    const string CarpetaPrefabs = "Assets/Prefab/Nivel";

    const string PngMoneda = "Assets/Imagen/Items/Coin.png";
    const string PngCofre = "Assets/Imagen/Items/Chest.png";
    const string PngBandera = "Assets/Imagen/Items/Flag.png";
    const string PngGema = "Assets/Imagen/Items/Gema.png";

    const string PrefabEnemigo = "Assets/Prefab/Enemy03_Idle_RP1 (32x36)_0.prefab";
    const string PrefabEsbirro = "Assets/Prefab/Esbirro.prefab";
    const string PrefabPortal = "Assets/Prefab/Portal.prefab";
    const string PrefabJugador = "Assets/Prefab/Player.prefab";
    const string PrefabBalaEnemiga = "Assets/Prefab/Triangle.prefab";
    const string PngVolador = "Assets/Imagen/Enemy/Enemy01_RP1 (32x16).png";
    const string RutaMaterialSinFriccion = "Assets/Material/SinFriccion.physicsMaterial2D";

    const int FondoSuelo = -10;   // fila más baja del terreno

    static Dictionary<Vector2Int, Tile> tiles;
    static Dictionary<int, float> superficie;      // x -> altura de la superficie donde se puede pisar
    static List<Vector2> puntosDecor;
    static List<Vector2> puntosPortal;

    // ------------------------------------------------------------------ entrada
    [MenuItem("Sky Wars/Construir niveles")]
    public static void Construir()
    {
        EditorSceneManager.SaveOpenScenes();

        // Copia de seguridad del Nivel1 anterior (fuera de Assets para que Unity no la cargue)
        Directory.CreateDirectory("Backups");
        if (File.Exists(RutaN1)) File.Copy(RutaN1, "Backups/Nivel1_antes_de_construir_niveles.unity", true);

        CargarTiles();
        PrepararSprites();
        CrearPrefabs();

        LimpiarNivel1();
        AssetDatabase.CopyAsset(RutaN1, RutaN2);
        AssetDatabase.Refresh();

        ConstruirNivel1();
        ConstruirNivel2();
        RegistrarEnBuildSettings();

        Debug.Log("ConstructorNiveles: Nivel1 y Nivel2 construidos.");
    }

    // ------------------------------------------------------------------ tiles
    static void CargarTiles()
    {
        tiles = new Dictionary<Vector2Int, Tile>();

        foreach (string guid in AssetDatabase.FindAssets("t:Tile", new[] { CarpetaTiles }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            Tile t = AssetDatabase.LoadAssetAtPath<Tile>(ruta);
            if (t == null || t.sprite == null) continue;

            // La hoja original mide 320x192 con celdas de 16 px; la fila 0 es la de arriba
            Rect r = t.sprite.rect;
            Vector2Int clave = new Vector2Int(Mathf.RoundToInt(r.x / 16f), Mathf.RoundToInt((176f - r.y) / 16f));

            if (!tiles.ContainsKey(clave) || !ruta.EndsWith(" 1.asset")) tiles[clave] = t;
        }
    }

    static Tile T(int col, int fila)
    {
        Tile t;
        return tiles.TryGetValue(new Vector2Int(col, fila), out t) ? t : null;
    }

    static int Hash(int x, int y) { return ((x * 73856093) ^ (y * 19349663)) & 0x7fffffff; }

    // ------------------------------------------------------------------ sprites
    static void PrepararSprites()
    {
        GenerarGema();

        Cortar(PngMoneda, "Moneda", 4, 10, 10, 18f, SpriteAlignment.Center);
        Cortar(PngCofre, "Cofre", 4, 32, 32, 22f, SpriteAlignment.BottomCenter);
        Cortar(PngBandera, "Bandera", 4, 48, 48, 20f, SpriteAlignment.BottomCenter);
        Cortar(PngGema, "Gema", 4, 12, 12, 18f, SpriteAlignment.Center);
    }

    static void Cortar(string ruta, string nombre, int cantidad, int ancho, int alto, float ppu, SpriteAlignment alineacion)
    {
        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) { Debug.LogWarning("No encuentro " + ruta); return; }

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;

        SpriteMetaData[] metas = new SpriteMetaData[cantidad];
        for (int i = 0; i < cantidad; i++)
        {
            metas[i] = new SpriteMetaData
            {
                name = nombre + "_" + i,
                rect = new Rect(i * ancho, 0f, ancho, alto),
                alignment = (int)alineacion,
                pivot = alineacion == SpriteAlignment.BottomCenter ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f)
            };
        }

        imp.spritesheet = metas;
        imp.SaveAndReimport();
    }

    static Sprite[] CargarSprites(string ruta)
    {
        return AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>()
            .OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Gema de píxel art de 4 fotogramas (el brillo recorre la piedra).</summary>
    static void GenerarGema()
    {
        const int W = 12, H = 12, F = 4;

        Texture2D tex = new Texture2D(W * F, H, TextureFormat.RGBA32, false);
        Color[] vacio = Enumerable.Repeat(Color.clear, W * F * H).ToArray();
        tex.SetPixels(vacio);

        Color borde = new Color(0.05f, 0.18f, 0.40f, 1f);
        Color claro = new Color(0.65f, 0.97f, 1f, 1f);
        Color medio = new Color(0.20f, 0.75f, 0.95f, 1f);
        Color oscuro = new Color(0.10f, 0.45f, 0.80f, 1f);
        Color brillo = Color.white;
        Vector2Int[] destellos = { new Vector2Int(3, 8), new Vector2Int(5, 9), new Vector2Int(7, 8), new Vector2Int(4, 7) };

        for (int f = 0; f < F; f++)
        {
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float dx = Mathf.Abs(x - 5.5f), dy = Mathf.Abs(y - 5.5f);
                    float d = (dx + dy) / 6f;
                    if (d > 1f) continue;

                    Color c;
                    if (d > 0.82f) c = borde;
                    else if (y > 5.5f) c = x < 5.5f ? claro : medio;
                    else c = x < 5.5f ? medio : oscuro;

                    if (x == destellos[f].x && y == destellos[f].y) c = brillo;

                    tex.SetPixel(f * W + x, y, c);
                }
            }
        }

        tex.Apply();
        File.WriteAllBytes(PngGema, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(PngGema, ImportAssetOptions.ForceUpdate);
    }

    // ------------------------------------------------------------------ prefabs
    static GameObject moneda, gema, cofre, bandera, voladorDirecto, voladorAzar;
    static Vector3 escalaVolador = Vector3.one;

    static void CrearPrefabs()
    {
        Directory.CreateDirectory(CarpetaPrefabs);

        Sprite[] fMoneda = CargarSprites(PngMoneda);
        Sprite[] fGema = CargarSprites(PngGema);
        Sprite[] fCofre = CargarSprites(PngCofre);
        Sprite[] fBandera = CargarSprites(PngBandera);

        // --- Moneda y gema ---
        moneda = GuardarPrefab("Moneda", () =>
        {
            GameObject g = new GameObject("Moneda");
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = fMoneda.Length > 0 ? fMoneda[0] : null;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 4;

            Recolectable r = g.AddComponent<Recolectable>();
            r.tipo = TipoRecolectable.Moneda;
            r.puntos = Puntuacion.PuntosMoneda;
            r.colorTexto = new Color(1f, 0.86f, 0.25f, 1f);
            r.frames = fMoneda;
            r.fps = 9f;
            return g;
        });

        gema = GuardarPrefab("Gema", () =>
        {
            GameObject g = new GameObject("Gema");
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = fGema.Length > 0 ? fGema[0] : null;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 4;

            Recolectable r = g.AddComponent<Recolectable>();
            r.tipo = TipoRecolectable.Gema;
            r.puntos = Puntuacion.PuntosGema;
            r.colorTexto = new Color(0.5f, 0.9f, 1f, 1f);
            r.frames = fGema;
            r.fps = 5f;
            return g;
        });

        // --- Cofre ---
        cofre = GuardarPrefab("Cofre", () =>
        {
            GameObject g = new GameObject("Cofre");
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = fCofre.Length > 0 ? fCofre[0] : null;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 3;

            BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(1.5f, 1.2f);
            bc.offset = new Vector2(0f, 0.6f);

            Cofre c = g.AddComponent<Cofre>();
            c.frames = fCofre;
            c.monedaPrefab = moneda;
            c.gemaPrefab = gema;
            return g;
        });

        // --- Bandera ---
        bandera = GuardarPrefab("Bandera", () =>
        {
            GameObject g = new GameObject("Bandera");
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = fBandera.Length > 0 ? fBandera[0] : null;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 2;

            BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(1.6f, 2.6f);
            bc.offset = new Vector2(0f, 1.3f);

            Bandera b = g.AddComponent<Bandera>();
            b.frames = fBandera;
            return g;
        });

        // --- Voladores que disparan ---
        // Mismo dibujo que los esbirros, con tinte propio (naranja = apunta directo, morado = dispara al azar)
        GameObject esbirroBase = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabEsbirro);
        Sprite[] fVolador = esbirroBase.GetComponent<Esbirro>().frames;
        escalaVolador = esbirroBase.transform.localScale;
        GameObject bala = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabBalaEnemiga);

        voladorDirecto = GuardarPrefab("VoladorDirecto", () => CrearVolador("VoladorDirecto", fVolador, bala,
            EnemigoVoladorDisparador.ModoDisparo.Directo, 1, new Color(1f, 0.78f, 0.45f, 1f)));
        voladorAzar = GuardarPrefab("VoladorAzar", () => CrearVolador("VoladorAzar", fVolador, bala,
            EnemigoVoladorDisparador.ModoDisparo.Azar, 2, new Color(0.78f, 0.62f, 1f, 1f)));
    }

    static GameObject CrearVolador(string nombre, Sprite[] frames, GameObject bala,
        EnemigoVoladorDisparador.ModoDisparo modo, int proyectiles, Color tinte)
    {
        GameObject g = new GameObject(nombre);
        g.transform.localScale = escalaVolador;

        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = frames.Length > 0 ? frames[0] : null;
        sr.color = tinte;
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 3;

        Rigidbody2D rb = g.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(0.32f, 0.32f);   // como el esbirro (la escala del objeto es 2,2)

        Salud s = g.AddComponent<Salud>();
        s.equipo = Equipo.Enemigo;
        s.vidaMaxima = 20f;

        EnemigoVoladorDisparador e = g.AddComponent<EnemigoVoladorDisparador>();
        e.frames = frames;
        e.balaPrefab = bala;
        e.modo = modo;
        e.proyectilesPorDisparo = proyectiles;
        return g;
    }

    static GameObject GuardarPrefab(string nombre, Func<GameObject> crear)
    {
        GameObject g = crear();
        string ruta = CarpetaPrefabs + "/" + nombre + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(g, ruta);
        UnityEngine.Object.DestroyImmediate(g);
        return prefab;
    }

    // ------------------------------------------------------------------ escena base
    static readonly string[] BorrarPorNombre =
    {
        "1_0", "2_0", "Grass(Group)", "Enemy03_Idle_RP1 (32x36)_0", "GenerationEnemi", "Abismos", "lasto Pint",
        "FondoParallax", "Decoracion", "Objetos", "Enemigos", "Portales", "Meta", "ObjetivoCamara", "Arranque", "Zonas"
    };

    static void LimpiarNivel1()
    {
        EditorSceneManager.OpenScene(RutaN1, OpenSceneMode.Single);

        foreach (string n in BorrarPorNombre)
        {
            GameObject g;
            while ((g = GameObject.Find(n)) != null) UnityEngine.Object.DestroyImmediate(g);
        }

        Tilemap suelo = GameObject.Find("Ground").GetComponent<Tilemap>();
        suelo.ClearAllTiles();

        // En los niveles no hay jefe: fuera su barra de vida
        foreach (string n in new[] { "BarraJefe", "NombreJefe", "TextoVidaJefe" })
        {
            GameObject g = BuscarEnHijos("CanvasHUD", n);
            if (g != null) g.SetActive(false);
        }

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), RutaN1);
    }

    static GameObject BuscarEnHijos(string raiz, string nombre)
    {
        GameObject r = GameObject.Find(raiz);
        if (r == null) return null;

        foreach (Transform t in r.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t.gameObject;

        return null;
    }

    // ------------------------------------------------------------------ nivel 1: bosque
    static void ConstruirNivel1()
    {
        EditorSceneManager.OpenScene(RutaN1, OpenSceneMode.Single);
        Iniciar();

        Tilemap suelo = GameObject.Find("Ground").GetComponent<Tilemap>();

        // Casi todo recto. Alturas de la fila superior; las que faltan son abismos.
        Dictionary<int, int> altura = new Dictionary<int, int>();
        Tramo(altura, -13, -8, 40);      // pared izquierda: el nivel no se acaba en un precipicio
        Tramo(altura, -8, 30, -4);
        // abismo 30..32
        Tramo(altura, 33, 64, -4);
        Tramo(altura, 64, 86, -3);      // escalón de 1
        Tramo(altura, 86, 112, -4);
        // abismo 112..115
        Tramo(altura, 116, 148, -4);
        Tramo(altura, 148, 158, -2);    // colina de 2
        Tramo(altura, 158, 180, -4);

        PintarSuelo(suelo, altura);

        // Plataformas flotantes (x desde, x hasta sin incluir, fila superior).
        // Quedan 4 casillas sobre el suelo: se llega con doble salto y se puede pasar por debajo.
        PintarPlataforma(suelo, 17, 22, 0);
        PintarPlataforma(suelo, 52, 57, 0);
        PintarPlataforma(suelo, 74, 79, 1);
        PintarPlataforma(suelo, 96, 101, 0);
        PintarPlataforma(suelo, 113, 116, -2);
        PintarPlataforma(suelo, 128, 133, 0);
        PintarPlataforma(suelo, 140, 145, 0);

        ReconstruirColision(suelo);

        // Fondo con parallax y bosque
        CrearParallax(0.92f);
        Decorar(true, 1);

        // Cofres: unos en el suelo y otros en lo alto
        Transform objetos = Padre("Objetos");
        foreach (int x in new[] { 9, 19, 40, 54, 76, 90, 98, 130, 142, 152, 166 })
            Poner(cofre, x + 0.5f, objetos);

        // Enemigos terrestres
        GameObject enemigo = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabEnemigo);
        Transform enemigos = Padre("Enemigos");
        foreach (int x in new[] { 24, 38, 48, 70, 82, 104, 120, 135, 150, 165 })
            if (superficie.ContainsKey(x)) Poner(enemigo, x + 0.5f, enemigos, 0.8f);

        // Portales: solo en el suelo, sacan enemigos terrestres
        CrearPortales(enemigo, new[] { 20, 45, 60, 80, 100, 125, 140, 162 }, 1.3f, 8f, 13f, 1);

        CrearAbismo(-8f, 180f);
        GameObject banderaN1 = CrearBandera(172, "Nivel2");

        // Zonas de pelea: una a mitad del nivel y otra que guarda la bandera
        CrearZona("Zona1", 47f, superficie[47], 2, null);
        CrearZona("Zona2", 168f, superficie[168], 3, banderaN1);
        FinNivel(new Vector2(-5.08f, -1.62f));

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), RutaN1);
    }

    // ------------------------------------------------------------------ nivel 2: montaña
    static void ConstruirNivel2()
    {
        EditorSceneManager.OpenScene(RutaN2, OpenSceneMode.Single);
        Iniciar();

        Tilemap suelo = GameObject.Find("Ground").GetComponent<Tilemap>();

        Dictionary<int, int> altura = new Dictionary<int, int>();
        Tramo(altura, -5, 31, -4);          // suelo del valle
        Tramo(altura, -10, -5, 52);         // pared izquierda de la montaña
        Tramo(altura, 31, 36, 52);          // pared derecha

        PintarSuelo(suelo, altura);

        // Zigzag de repisas: cada una 2 casillas más alta que la anterior, alternando izquierda y derecha.
        // La repisa j tiene su superficie en y = -3 + 2j (la 0 es el suelo del valle).
        // Entre izquierda y derecha hay 3 casillas de hueco: se cruzan con salto doble.
        // En las transiciones impares hay una piedra de paso en medio para descansar.
        const int repisas = 13;
        puntosPortal.Clear();

        for (int j = 1; j <= repisas; j++)
        {
            int superficieJ = -3 + 2 * j;
            int fila = superficieJ - 1;
            bool izquierda = j % 2 == 0;

            if (j < repisas)
            {
                if (izquierda) PintarPlataforma(suelo, -5, 9, fila);
                else PintarPlataforma(suelo, 12, 31, fila);

                if (j % 2 == 1 && j >= 3) PintarPlataforma(suelo, 10, 12, superficieJ, 1);   // piedra de paso, 1 casilla sobre la repisa

                puntosPortal.Add(new Vector2(izquierda ? 2f : 22f, superficieJ + 2.3f));
            }
            else
            {
                PintarPlataforma(suelo, 8, 28, fila);    // cima
            }
        }

        ReconstruirColision(suelo);

        CrearParallax(0.96f);
        Decorar(false, 2);

        // Cofres en las repisas de arriba (la izquierda y la derecha se van alternando)
        Transform objetos = Padre("Objetos");
        for (int j = 2; j < repisas; j += 2)
        {
            float x = (j % 2 == 0) ? 3.5f : 24.5f;
            Poner(cofre, x, objetos, 0f, -3f + 2f * j);
        }
        Poner(cofre, 24.5f, objetos, 0f, -3f + 2f * 7);
        Poner(cofre, 11f, objetos, 0f, -3f + 2f * 9 + 1f);   // sobre la piedra de paso de la repisa 9

        // Enemigos voladores desde portales (cerca de las repisas)
        GameObject esbirro = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabEsbirro);
        CrearPortalesPuntos(esbirro, puntosPortal, 7f, 11f, 2);

        CrearAbismo(-5f, 31f);
        GameObject banderaN2 = CrearBandera(18, "Boss", -3f + 2f * repisas);

        // Pelea final en la cima: la bandera aparece al despejarla
        CrearZona("ZonaCima", 18f, -3f + 2f * repisas, 3, banderaN2);
        FinNivel(new Vector2(0f, -1.62f));

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), RutaN2);
    }

    // ------------------------------------------------------------------ terreno
    static void Iniciar()
    {
        superficie = new Dictionary<int, float>();
        puntosDecor = new List<Vector2>();
        puntosPortal = new List<Vector2>();
    }

    static void Tramo(Dictionary<int, int> altura, int x0, int x1, int fila)
    {
        for (int x = x0; x < x1; x++) altura[x] = fila;
    }

    /// <summary>Terreno sólido columna a columna: hierba arriba, bordes donde hay aire a un lado.</summary>
    static void PintarSuelo(Tilemap tm, Dictionary<int, int> altura)
    {
        foreach (KeyValuePair<int, int> kv in altura)
        {
            int x = kv.Key, techo = kv.Value;

            for (int y = FondoSuelo; y <= techo; y++)
            {
                bool izq = !altura.ContainsKey(x - 1) || altura[x - 1] < y;
                bool der = !altura.ContainsKey(x + 1) || altura[x + 1] < y;

                int col = izq ? 0 : (der ? 5 : 1 + Hash(x, y) % 4);
                int fila;

                if (y == techo) fila = 0;
                else if (y == FondoSuelo) fila = 5;
                else fila = 1 + ((techo - y - 1) % 4);

                tm.SetTile(new Vector3Int(x, y, 0), T(col, fila));
            }

            // Solo cuentan las zonas que no son pared alta (las paredes no se pisan)
            if (techo < 40) { superficie[x] = techo + 1f; if (techo == -4 || techo == -3 || techo == -2) puntosDecor.Add(new Vector2(x + 0.5f, techo + 1f)); }
        }
    }

    /// <summary>Plataforma flotante (hierba arriba): de 2 de grosor, o de 1 si es una piedra de paso.</summary>
    static void PintarPlataforma(Tilemap tm, int x0, int x1, int fila, int grosor = 2)
    {
        for (int x = x0; x < x1; x++)
        {
            int col = (x == x0) ? 2 : (x == x1 - 1 ? 7 : 3 + Hash(x, fila) % 4);
            tm.SetTile(new Vector3Int(x, fila, 0), T(col, 6));
            if (grosor > 1) tm.SetTile(new Vector3Int(x, fila - 1, 0), T(col, 7));

            superficie[x] = fila + 1f;
            puntosDecor.Add(new Vector2(x + 0.5f, fila + 1f));
        }
    }

    /// <summary>Sin esto el collider conserva la forma del nivel anterior y el jugador atraviesa el suelo.</summary>
    static void ReconstruirColision(Tilemap tm)
    {
        TilemapCollider2D tc = tm.GetComponent<TilemapCollider2D>();
        CompositeCollider2D cc = tm.GetComponent<CompositeCollider2D>();

        if (tc != null) tc.ProcessTilemapChanges();
        if (cc != null) cc.GenerateGeometry();
    }

    // ------------------------------------------------------------------ objetos
    static Transform Padre(string nombre)
    {
        GameObject g = GameObject.Find(nombre);
        if (g == null) g = new GameObject(nombre);
        return g.transform;
    }

    /// <summary>Instancia un prefab sobre la superficie en x (o a una altura fija si se indica).</summary>
    static GameObject Poner(GameObject prefab, float x, Transform padre, float extraY = 0f, float yFija = float.NaN)
    {
        float y;
        if (!float.IsNaN(yFija)) y = yFija;
        else
        {
            float s;
            if (!superficie.TryGetValue(Mathf.FloorToInt(x), out s)) return null;
            y = s;
        }

        GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        g.transform.SetParent(padre, false);
        g.transform.position = new Vector3(x, y + extraY, 0f);
        return g;
    }

    static void CrearParallax(float factorVertical)
    {
        GameObject go = new GameObject("FondoParallax");
        FondoParallax p = go.AddComponent<FondoParallax>();

        float[] factoresX = { 0.02f, 0.08f, 0.18f, 0.32f, 0.50f };
        p.capas = new CapaParalax[5];

        for (int i = 0; i < 5; i++)
        {
            p.capas[i] = new CapaParalax
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Imagen/2 Background/Layers/" + (i + 1) + ".png"),
                factorX = factoresX[i],
                factorY = Mathf.Lerp(factorVertical, factorVertical - 0.1f, i / 4f),
                orden = i,
                escala = 1f
            };
        }
    }

    /// <summary>Árboles al fondo, arbustos y hierba delante.</summary>
    static void Decorar(bool arboles, int semilla)
    {
        Transform padre = Padre("Decoracion");
        System.Random rnd = new System.Random(semilla);

        Sprite[] arbol = CargarCarpeta("Assets/Imagen/Objects/Trees");
        Sprite[] arbusto = CargarCarpeta("Assets/Imagen/Objects/Bushes");
        Sprite[] hierba = CargarCarpeta("Assets/Imagen/Objects/Grass");

        foreach (Vector2 p in puntosDecor)
        {
            double r = rnd.NextDouble();

            if (arboles && arbol.Length > 0 && r < 0.07)
                PonerSprite(padre, arbol[rnd.Next(arbol.Length)], p, 0.55f, "BackGround", 1, rnd);
            else if (r < 0.20 && arbusto.Length > 0)
                PonerSprite(padre, arbusto[rnd.Next(arbusto.Length)], p, 1f, "Player", 1, rnd);
            else if (r < 0.45 && hierba.Length > 0)
                PonerSprite(padre, hierba[rnd.Next(hierba.Length)], p, 1f, "Player", 1, rnd);
        }
    }

    static Sprite[] CargarCarpeta(string carpeta)
    {
        List<Sprite> lista = new List<Sprite>();

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { carpeta }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            lista.AddRange(AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>());
        }

        return lista.ToArray();
    }

    static void PonerSprite(Transform padre, Sprite s, Vector2 pos, float escala, string capa, int orden, System.Random rnd)
    {
        GameObject g = new GameObject(s.name);
        g.transform.SetParent(padre, false);

        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingLayerName = capa;
        sr.sortingOrder = orden;
        sr.flipX = rnd.Next(2) == 0;

        g.transform.localScale = Vector3.one * escala;

        // El pivote del sprite está en el centro: se sube la mitad de su alto para que "pise" la superficie
        float alto = s.bounds.size.y * escala;
        g.transform.position = new Vector3(pos.x + (float)(rnd.NextDouble() - 0.5) * 0.4f, pos.y + alto * 0.5f - 0.04f, 0f);
    }

    // ------------------------------------------------------------------ portales, abismo, bandera
    static void CrearPortales(GameObject enemigo, int[] xs, float alto, float esperaMin, float esperaMax, int maxAbiertos)
    {
        List<Vector2> puntos = new List<Vector2>();
        foreach (int x in xs)
        {
            float s;
            if (superficie.TryGetValue(x, out s)) puntos.Add(new Vector2(x + 0.5f, s + alto));
        }

        CrearPortalesPuntos(enemigo, puntos, esperaMin, esperaMax, maxAbiertos);
    }

    static void CrearPortalesPuntos(GameObject enemigo, List<Vector2> puntos, float esperaMin, float esperaMax, int maxAbiertos)
    {
        GameObject raiz = new GameObject("Portales");
        GeneradorPortales gen = raiz.AddComponent<GeneradorPortales>();
        gen.portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPortal);
        gen.enemigoPrefab = enemigo;
        gen.esperaMinima = esperaMin;
        gen.esperaMaxima = esperaMax;
        gen.maxPortalesAbiertos = maxAbiertos;
        gen.minEnemigos = 3;
        gen.maxEnemigos = 4;

        List<Transform> anclas = new List<Transform>();
        for (int i = 0; i < puntos.Count; i++)
        {
            GameObject a = new GameObject("Ancla" + i);
            a.transform.SetParent(raiz.transform, false);
            a.transform.position = new Vector3(puntos[i].x, puntos[i].y, 0f);
            anclas.Add(a.transform);
        }

        gen.anclas = anclas.ToArray();
    }

    /// <summary>Franja bajo el nivel: caer en ella quita 50 de vida y devuelve al último suelo.</summary>
    static void CrearAbismo(float x0, float x1)
    {
        GameObject g = new GameObject("Abismo");
        g.transform.position = new Vector3((x0 + x1) * 0.5f, FondoSuelo - 5f, 0f);

        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(x1 - x0 + 120f, 2f);

        ZonaDanio z = g.AddComponent<ZonaDanio>();
        z.tipo = ZonaDanio.Tipo.Abismo;
        z.AplicarValoresSugeridos();   // 50 de daño
    }

    static GameObject CrearBandera(int x, string siguiente, float yFija = float.NaN)
    {
        Transform meta = Padre("Meta");
        GameObject g = Poner(bandera, x + 0.5f, meta, 0f, yFija);
        if (g == null) return null;

        g.GetComponent<Bandera>().escenaSiguiente = siguiente;
        return g;
    }

    /// <summary>
    /// Zona de pelea de 16 x 10 (lo que muestra la cámara): paredes, cámara quieta y oleadas de voladores.
    /// La cámara queda centrada 3,7 sobre la superficie.
    /// </summary>
    static void CrearZona(string nombre, float centroX, float superficieY, int oleadas, GameObject activarAlTerminar)
    {
        Transform padre = Padre("Zonas");

        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        float centroY = superficieY + 3.7f;
        g.transform.position = new Vector3(centroX, centroY, 0f);

        ZonaCombate zona = g.AddComponent<ZonaCombate>();   // añade también el BoxCollider2D y el GeneradorEnemigos
        BoxCollider2D area = g.GetComponent<BoxCollider2D>();
        area.size = new Vector2(16f, 10f);

        zona.materialPared = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(RutaMaterialSinFriccion);
        zona.recompensaPrefab = cofre;
        zona.posicionRecompensa = new Vector2(centroX - 3f, superficieY);
        if (activarAlTerminar != null) zona.activarAlTerminar = new[] { activarAlTerminar };

        // Puntos por donde entran los enemigos: la franja alta de la pantalla
        GeneradorEnemigos gen = g.GetComponent<GeneradorEnemigos>();
        List<Transform> puntos = new List<Transform>();
        float[] xs = { -6.5f, -3.5f, 0f, 3.5f, 6.5f };
        for (int i = 0; i < xs.Length; i++)
        {
            GameObject a = new GameObject("Entrada" + i);
            a.transform.SetParent(g.transform, false);
            a.transform.localPosition = new Vector3(xs[i], 3.3f - (i % 2) * 1.2f, 0f);
            puntos.Add(a.transform);
        }

        gen.puntosAparicion = puntos.ToArray();
        gen.maxVivos = 4;

        GameObject esbirro = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabEsbirro);

        List<GeneradorEnemigos.Oleada> lista = new List<GeneradorEnemigos.Oleada>();
        lista.Add(new GeneradorEnemigos.Oleada { nombre = "Oleada 1", enemigos = new[] { voladorDirecto }, cantidad = 3, intervalo = 1.4f, pausaAlTerminar = 1.5f });
        if (oleadas >= 2)
            lista.Add(new GeneradorEnemigos.Oleada { nombre = "Oleada 2", enemigos = new[] { voladorDirecto, voladorAzar }, cantidad = 4, intervalo = 1.3f, pausaAlTerminar = 1.5f });
        if (oleadas >= 3)
            lista.Add(new GeneradorEnemigos.Oleada { nombre = "Oleada 3", enemigos = new[] { voladorAzar, voladorDirecto, esbirro }, cantidad = 6, intervalo = 1.1f, pausaAlTerminar = 1.5f });

        gen.oleadas = lista.ToArray();
    }

    /// <summary>Jugador, cámara y arranque de partida (2 jugadores) al final de cada nivel.</summary>
    static void FinNivel(Vector2 inicio)
    {
        GameObject jugador = GameObject.Find("Player");
        if (jugador != null) jugador.transform.position = new Vector3(inicio.x, inicio.y, 0f);

        // El punto medio de los jugadores es lo que sigue la cámara
        GameObject objetivo = new GameObject("ObjetivoCamara");
        objetivo.transform.position = new Vector3(inicio.x, inicio.y, 0f);
        objetivo.AddComponent<PuntoMedioJugadores>();

        GameObject camGO = GameObject.Find("CinemachineCamera(Plauert)");
        if (camGO != null)
        {
            CinemachineCamera cc = camGO.GetComponent<CinemachineCamera>();
            if (cc != null) cc.Follow = objetivo.transform;
            camGO.transform.position = new Vector3(inicio.x, inicio.y, -10f);
        }

        Camera main = Camera.main;
        if (main != null) main.transform.position = new Vector3(inicio.x, inicio.y, -10f);

        // Lee el menú: segundo jugador, dispositivos...
        GameObject arranque = new GameObject("Arranque");
        ArranquePartida ap = arranque.AddComponent<ArranquePartida>();
        ap.prefabJugador = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabJugador);
        ap.jugador1 = jugador != null ? jugador.GetComponent<PlayerController>() : null;
        ap.posicionJugador2 = new Vector3(inicio.x + 1.6f, inicio.y + 0.5f, 0f);

        GameObject hud = GameObject.Find("CanvasHUD");
        if (hud != null) ap.hud = hud.GetComponent<HUDJuego>();
    }

    // ------------------------------------------------------------------ build settings
    static void RegistrarEnBuildSettings()
    {
        string[] orden = { RutaMenu, RutaN1, RutaN2, RutaBoss };

        List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>();
        foreach (string ruta in orden)
            if (File.Exists(ruta)) lista.Add(new EditorBuildSettingsScene(ruta, true));

        EditorBuildSettings.scenes = lista.ToArray();
    }
}
