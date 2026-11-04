using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

#pragma warning disable CS0618

/// <summary>
/// Nivel 2: arena del minijefe Rey Cerdo. Un campo de batalla llano de 36 casillas con plataformas a dos alturas
/// para saltar las ondas del rey y moverse por más sitios. Menú: Spirit Dragon / Construir Nivel 2 (Rey Cerdo).
/// La montaña que antes era el Nivel 2 pasa a ser el Nivel 3 (Nivel3.unity).
/// </summary>
public static partial class ConstructorNiveles
{
    const string RutaN3 = "Assets/Scenes/Nivel3.unity";
    const string CarpetaCerdos = "Assets/Imagen/KingPigs/";

    static GameObject reyPrefab, soldadoPrefab, bombarderoPrefab, bombaPrefab;

    [MenuItem("Spirit Dragon/Construir Nivel 2 (Rey Cerdo)")]
    public static void ConstruirNivelRey()
    {
        EditorSceneManager.SaveOpenScenes();

        // La montaña existente pasa a ser el Nivel 3 (se conserva el GUID: el build sigue apuntando a ella)
        if (!File.Exists(RutaN3) && File.Exists(RutaN2))
        {
            string error = AssetDatabase.MoveAsset(RutaN2, RutaN3);
            if (!string.IsNullOrEmpty(error)) { Debug.LogError("No se pudo mover Nivel2 a Nivel3: " + error); return; }
        }

        CargarTiles();
        CargarPrefabsDeNivel();
        PrepararCerdos();

        if (!File.Exists(RutaN2)) AssetDatabase.CopyAsset(RutaN1, RutaN2);
        AssetDatabase.Refresh();

        EditorSceneManager.OpenScene(RutaN2, OpenSceneMode.Single);
        BorrarGeneradosNivel2();
        foreach (string n in new[] { "ReyCerdo", "DespertarRey", "Arena" })
        {
            GameObject viejo;
            while ((viejo = GameObject.Find(n)) != null) UnityEngine.Object.DestroyImmediate(viejo);
        }
        Iniciar();

        Tilemap suelo = GameObject.Find("Ground").GetComponent<Tilemap>();
        suelo.ClearAllTiles();

        // ---- Terreno: suelo llano largo con paredes en los extremos
        Dictionary<int, int> altura = new Dictionary<int, int>();
        Tramo(altura, -13, -8, 40);
        Tramo(altura, -8, 54, -4);
        Tramo(altura, 54, 60, 40);
        PintarSuelo(suelo, altura);

        // Plataformas (piedras de 1 de grosor). Fila superior = -4 + altura sobre el suelo.
        // A 4 casillas se sube con doble salto; las de 7 se alcanzan desde las de 4.
        // Con 3 casillas libres debajo el rey cabe y no se atasca.
        foreach (int[] p in new[] { new[] { 5, 10, 4 }, new[] { 17, 24, 4 }, new[] { 31, 36, 4 }, new[] { 11, 16, 7 }, new[] { 25, 30, 7 } })
            PintarPlataforma(suelo, p[0], p[1], -4 + p[2], 1);

        ReconstruirColision(suelo);

        // ---- Ambiente: pie de la montaña con nieve
        if (Directory.Exists(CarpetaFondoMontana)) CrearParallaxMontana();
        Decorar(false, 5);
        CrearAmbiente(true);
        CrearAbismo(-8f, 54f);

        // ---- Cofres en plataformas y en el suelo
        Transform objetos = Padre("Objetos");
        foreach (int x in new[] { 7, 13, 20, 27, 33 })
            Poner(cofre, x + 0.5f, objetos);

        // ---- Meta (aparece al derrotar al rey)
        GameObject banderaN2 = CrearBandera(46, "Nivel3");

        // ---- El rey y la entrada
        Transform arena = Padre("Arena");
        const float sueloY = -3f;

        GameObject rey = (GameObject)PrefabUtility.InstantiatePrefab(reyPrefab);
        rey.name = "ReyCerdo";
        rey.transform.SetParent(arena, false);
        rey.transform.position = new Vector3(31f, sueloY, 0f);

        ReyCerdo rc = rey.GetComponent<ReyCerdo>();
        rc.limiteIzq = 4f;
        rc.limiteDer = 40f;
        rc.materialPared = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(RutaMaterialSinFriccion);
        rc.soldadoPrefab = soldadoPrefab;
        rc.bombarderoPrefab = bombarderoPrefab;
        rc.recompensaPrefab = cofre;
        rc.activarAlMorir = banderaN2 != null ? new[] { banderaN2 } : new GameObject[0];

        GameObject disparador = new GameObject("DespertarRey");
        disparador.transform.SetParent(arena, false);
        disparador.transform.position = new Vector3(7.5f, 2f, 0f);
        BoxCollider2D bc = disparador.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(3f, 12f);
        disparador.AddComponent<DespertarRey>().rey = rc;

        FinNivel(new Vector2(1.5f, -1.62f));

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), RutaN2);
        RegistrarEnBuildSettings();
        Debug.Log("ConstructorNiveles: Nivel 2 (Rey Cerdo) construido. La montaña es ahora Nivel 3.");
    }

    // ------------------------------------------------------------------ sprites y prefabs de los cerdos
    static void PrepararCerdos()
    {
        Sprite[] reyIdle = CortarHoja("02-King Pig/Idle (38x28).png", "Rey_Idle", 38, 28, 10f, false);
        Sprite[] reyCorrer = CortarHoja("02-King Pig/Run (38x28).png", "Rey_Run", 38, 28, 10f, false);
        Sprite[] reyAtacar = CortarHoja("02-King Pig/Attack (38x28).png", "Rey_Attack", 38, 28, 10f, false);
        Sprite[] reyGolpe = CortarHoja("02-King Pig/Hit (38x28).png", "Rey_Hit", 38, 28, 10f, false);
        Sprite[] reyMuerte = CortarHoja("02-King Pig/Dead (38x28).png", "Rey_Dead", 38, 28, 10f, false);

        Sprite[] cIdle = CortarHoja("03-Pig/Idle (34x28).png", "Cerdo_Idle", 34, 28, 16f, false);
        Sprite[] cCorrer = CortarHoja("03-Pig/Run (34x28).png", "Cerdo_Run", 34, 28, 16f, false);
        Sprite[] cAtacar = CortarHoja("03-Pig/Attack (34x28).png", "Cerdo_Attack", 34, 28, 16f, false);
        Sprite[] cGolpe = CortarHoja("03-Pig/Hit (34x28).png", "Cerdo_Hit", 34, 28, 16f, false);
        Sprite[] cMuerte = CortarHoja("03-Pig/Dead (34x28).png", "Cerdo_Dead", 34, 28, 16f, false);

        Sprite[] bIdle = CortarHoja("05-Pig Thowing a Bomb/Idle (26x26).png", "Bomb_Idle", 26, 26, 16f, false);
        Sprite[] bCorrer = CortarHoja("05-Pig Thowing a Bomb/Run (26x26).png", "Bomb_Run", 26, 26, 16f, false);
        Sprite[] bLanzar = CortarHoja("05-Pig Thowing a Bomb/Throwing Boom (26x26).png", "Bomb_Throw", 26, 26, 16f, false);
        Sprite[] bGolpe = cGolpe;

        Sprite[] bombaOn = CortarHoja("09-Bomb/Bomb On (52x56).png", "Bomba_On", 52, 56, 24f, true);
        Sprite[] bombaBoom = CortarHoja("09-Bomb/Boooooom (52x56).png", "Bomba_Boom", 52, 56, 24f, true);

        PhysicsMaterial2D mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(RutaMaterialSinFriccion);

        // ---- Bomba
        bombaPrefab = GuardarPrefab("BombaCerdo", () =>
        {
            GameObject g = new GameObject("BombaCerdo");
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = bombaOn.Length > 0 ? bombaOn[0] : null;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 5;

            Rigidbody2D rb = g.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2.5f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D c = g.AddComponent<CircleCollider2D>();
            c.radius = 0.28f;

            AnimadorCuadros an = g.AddComponent<AnimadorCuadros>();
            an.sr = sr;

            BombaCerdo b = g.AddComponent<BombaCerdo>();
            b.mecha = bombaOn;
            b.explosion = bombaBoom;
            b.radio = 1.5f;
            b.danio = 18f;
            return g;
        });

        // ---- Cerdo soldado
        soldadoPrefab = GuardarPrefab("CerdoSoldado", () =>
        {
            GameObject g = CuerpoCerdo("CerdoSoldado", cIdle, 24f, 150, new Vector2(0.9f, 1.0f), mat);
            CerdoSoldado s = g.AddComponent<CerdoSoldado>();
            s.idle = cIdle; s.correr = cCorrer; s.atacar = cAtacar; s.golpe = cGolpe; s.muerte = cMuerte;
            s.velocidad = 3.4f;
            s.danio = 12f;
            return g;
        });

        // ---- Cerdo bombardero
        bombarderoPrefab = GuardarPrefab("CerdoBombardero", () =>
        {
            GameObject g = CuerpoCerdo("CerdoBombardero", bIdle, 24f, 200, new Vector2(0.8f, 0.95f), mat);
            CerdoBombardero s = g.AddComponent<CerdoBombardero>();
            s.idle = bIdle; s.correr = bCorrer; s.atacar = bLanzar; s.golpe = bGolpe; s.muerte = cMuerte;
            s.velocidad = 2.8f;
            s.bombaPrefab = bombaPrefab;
            return g;
        });

        // ---- Rey Cerdo
        reyPrefab = GuardarPrefab("ReyCerdo", () =>
        {
            GameObject g = CuerpoCerdo("ReyCerdo", reyIdle, 320f, 800, new Vector2(1.8f, 2.3f), mat);
            g.GetComponent<Rigidbody2D>().mass = 60f;

            ReyCerdo r = g.AddComponent<ReyCerdo>();
            r.idle = reyIdle; r.correr = reyCorrer; r.atacar = reyAtacar; r.golpe = reyGolpe; r.muerte = reyMuerte;
            r.salto = reyIdle;
            r.soldadoPrefab = soldadoPrefab;
            r.bombarderoPrefab = bombarderoPrefab;
            return g;
        });

        AssetDatabase.SaveAssets();
    }

    /// <summary>GameObject base de un cerdo: sprite, cuerpo físico, collider, vida y animador.</summary>
    static GameObject CuerpoCerdo(string nombre, Sprite[] idle, float vida, int puntos, Vector2 tam, PhysicsMaterial2D mat)
    {
        GameObject g = new GameObject(nombre);

        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = idle.Length > 0 ? idle[0] : null;
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 3;

        Rigidbody2D rb = g.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.size = tam;
        bc.offset = new Vector2(0f, tam.y * 0.5f);
        if (mat != null) bc.sharedMaterial = mat;

        Salud s = g.AddComponent<Salud>();
        s.equipo = Equipo.Enemigo;
        s.vidaMaxima = vida;
        s.destruirAlMorir = false;
        s.puntosAlMorir = puntos;

        AnimadorCuadros an = g.AddComponent<AnimadorCuadros>();
        an.sr = sr;
        return g;
    }

    /// <summary>Corta una hoja horizontal de fotogramas (nombres con ceros para que se ordenen bien).</summary>
    static Sprite[] CortarHoja(string relativa, string nombre, int ancho, int alto, float ppu, bool centro)
    {
        string ruta = CarpetaCerdos + relativa;
        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) { Debug.LogError("Falta " + ruta); return new Sprite[0]; }

        byte[] cab = new byte[24];
        using (FileStream fs = File.OpenRead(ruta)) fs.Read(cab, 0, 24);
        int w = (cab[16] << 24) | (cab[17] << 16) | (cab[18] << 8) | cab[19];
        int cantidad = Mathf.Max(1, w / ancho);

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;

        SpriteMetaData[] metas = new SpriteMetaData[cantidad];
        for (int i = 0; i < cantidad; i++)
        {
            metas[i] = new SpriteMetaData
            {
                name = nombre + "_" + i.ToString("00"),
                rect = new Rect(i * ancho, 0f, ancho, alto),
                alignment = (int)(centro ? SpriteAlignment.Center : SpriteAlignment.BottomCenter),
                pivot = centro ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f)
            };
        }

        imp.spritesheet = metas;
        imp.SaveAndReimport();

        return AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>()
            .OrderBy(s => s.name, System.StringComparer.Ordinal).ToArray();
    }
}
