using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Constructor del dragón con cabeza articulada.
/// 1) Separa cada fotograma en cuerpo (sin cabeza) y cabeza suelta.
/// 2) Reescribe las animaciones para que muevan el cuerpo.
/// 3) Monta el rig CabezaPivot → Cabeza → Boca sobre el Player.
/// 4) Crea el prefab de la bola de fuego.
/// Menú: Spirit Dragon / Reconstruir dragón
/// </summary>
public static class ConstructorPersonaje
{
    const string CarpetaEntrada = "Assets/Dragon Warrior Files/Dragon Warrior PNG";
    const string CarpetaPartes = "Assets/Generated_Images/DragonPartes";
    const string RutaPrefabBola = "Assets/Prefab/BolaDeFuego.prefab";
    const string RutaSpriteBola = "Assets/Generated_Images/bola_fuego.png";

    static readonly string[] Clips =
    {
        "Assets/Animations/DragonIdle.anim",
        "Assets/Animations/DragonCaminar.anim",
        "Assets/Animations/DragonDash.anim",
        "Assets/Animations/DragonMuerte.anim"
    };

    // Silueta de la cabeza medida sobre idle_01.png (coordenadas con Y hacia abajo)
    static readonly Vector2[] PoligonoCabeza =
    {
        new Vector2(241f, 111f), new Vector2(250f, 114f), new Vector2(274f, 118f),
        new Vector2(283f, 122f), new Vector2(287f, 127f), new Vector2(300f, 127f),
        new Vector2(310f, 129f), new Vector2(310f, 140f), new Vector2(309f, 143f),
        new Vector2(308f, 148f), new Vector2(305f, 151f), new Vector2(302f, 154f),
        new Vector2(300f, 157f), new Vector2(292f, 159f), new Vector2(291f, 162f),
        new Vector2(291f, 165f), new Vector2(293f, 168f), new Vector2(297f, 171f),
        new Vector2(302f, 174f), new Vector2(303f, 178f), new Vector2(301f, 183f),
        new Vector2(296f, 189f), new Vector2(290f, 195f), new Vector2(270f, 197f),
        new Vector2(250f, 192f), new Vector2(234f, 184f), new Vector2(216f, 177f),
        new Vector2(204f, 173f), new Vector2(196f, 168f), new Vector2(196f, 162f),
        new Vector2(202f, 156f), new Vector2(206f, 150f), new Vector2(206f, 144f),
        new Vector2(209f, 139f), new Vector2(217f, 133f), new Vector2(223f, 129f),
        new Vector2(228f, 124f), new Vector2(233f, 118f)
    };

    // Pivote del cuello, en unidades locales del sprite (centro = 0,0)
    static readonly Vector3 PivoteCuello = new Vector3(0f, -0.60f, 0f);
    static readonly Vector3 OffsetCabeza = new Vector3(0f, 0.60f, 0f);
    static readonly Vector3 PosicionBoca = new Vector3(0.445f, -0.475f, 0f);

    [MenuItem("Spirit Dragon/Reconstruir dragón")]
    public static void ConstruirTodo()
    {
        Dictionary<Sprite, Sprite> mapaCuerpo = SepararFotogramas();
        ReescribirClips(mapaCuerpo);
        Sprite[] cabezas = CargarCabezas();
        MontarRig(cabezas);
        CrearBolaDeFuego();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("ConstructorPersonaje: dragón articulado listo (" + mapaCuerpo.Count + " fotogramas).");
    }

    // ------------------------------------------------------------ 1 · imágenes
    static Dictionary<Sprite, Sprite> SepararFotogramas()
    {
        Directory.CreateDirectory(CarpetaPartes);

        // Borra las partes anteriores. Sin esto, una ejecución que lea el origen
        // desde los clips vuelve a partir lo ya partido (_cuerpo_cuerpo_…) y cada
        // pasada recorta otra vez la zona de la cabeza.
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { CarpetaPartes }))
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

        // El origen es SIEMPRE la carpeta original del paquete de arte, nunca los clips.
        string[] origenes = Directory.GetFiles(CarpetaEntrada, "*.png");
        System.Array.Sort(origenes);

        // La silueta del dragón ocupa un sitio distinto del lienzo en cada fotograma
        // (en walk se desplaza más de 15 px). El polígono está medido sobre idle_01,
        // así que se recoloca con la caja de cada fotograma antes de recortar:
        // se ancla en la esquina superior derecha (la cabeza) y se escala con el
        // ancho, que es la medida más estable (el alto cambia al recoger las patas).
        Color[] refPx;
        int refBx0, refBy0, refBx1, refBy1;

        {
            Texture2D texRef = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texRef.LoadImage(File.ReadAllBytes(CarpetaEntrada + "/idle_01.png"));
            refPx = texRef.GetPixels();
            Silueta(refPx, texRef.width, texRef.height, out refBx0, out refBy0, out refBx1, out refBy1);
            Object.DestroyImmediate(texRef);
        }

        float refAncho = Mathf.Max(1f, refBx1 - refBx0);

        var mapa = new Dictionary<Sprite, Sprite>();

        foreach (string fichero in origenes)
        {
            string ruta = fichero.Replace('\\', '/');

            TextureImporter fuente = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (fuente == null) continue;

            Sprite original = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
            if (original == null) continue;

            string nombre = Path.GetFileNameWithoutExtension(ruta);
            string rutaCuerpo = CarpetaPartes + "/" + nombre + "_cuerpo.png";
            string rutaCabeza = CarpetaPartes + "/" + nombre + "_cabeza.png";

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(ruta));

            Color[] px = tex.GetPixels();
            int w = tex.width;
            int h = tex.height;

            bool separar = SeSepara(nombre);

            int bx0, by0, bx1, by1;
            Silueta(px, w, h, out bx0, out by0, out bx1, out by1);

            float sx = (bx1 - bx0) / refAncho;

            if (sx < 0.05f) separar = false;

            Color[] cuerpo = new Color[px.Length];
            Color[] cabeza = new Color[px.Length];

            for (int i = 0; i < px.Length; i++) { cuerpo[i] = px[i]; cabeza[i] = new Color(0f, 0f, 0f, 0f); }

            for (int yAbajo = 0; yAbajo < h; yAbajo++)
            {
                int yArriba = h - 1 - yAbajo;   // coordenada con Y hacia abajo

                for (int x = 0; x < w; x++)
                {
                    int i = yAbajo * w + x;
                    Color p = px[i];

                    if (p.a < 0.1f) { cuerpo[i] = new Color(0f, 0f, 0f, 0f); continue; }

                    // Se devuelve el punto a las coordenadas de idle_01 para
                    // comprobar el polígono.
                    float ix = refBx1 - (bx1 - x) / sx;
                    float iy = refBy0 + (yArriba - by0) / sx;

                    bool enCabeza = separar && !EsPurpura(p) && EnPoligono(ix, iy);

                    if (enCabeza)
                    {
                        cabeza[i] = p;
                        cuerpo[i] = new Color(0f, 0f, 0f, 0f);
                    }
                }
            }

            GuardarPng(rutaCuerpo, cuerpo, w, h);
            GuardarPng(rutaCabeza, cabeza, w, h);

            mapa[original] = null;   // se rellena tras el refresco
        }

        AssetDatabase.Refresh();

        var resultado = new Dictionary<Sprite, Sprite>();
        var clavesOriginales = new List<Sprite>(mapa.Keys);

        foreach (Sprite original in clavesOriginales)
        {
            string ruta = AssetDatabase.GetAssetPath(original);
            string nombre = Path.GetFileNameWithoutExtension(ruta);
            string rutaCuerpo = CarpetaPartes + "/" + nombre + "_cuerpo.png";
            string rutaCabeza = CarpetaPartes + "/" + nombre + "_cabeza.png";

            TextureImporter fuente = AssetImporter.GetAtPath(ruta) as TextureImporter;
            ImportarSprite(rutaCuerpo, fuente);

            // La cabeza también se importa como sprite único con el pivote del
            // lienzo original. Sin esto queda en modo Multiple y Unity le pone un
            // pivote propio en el centro de la silueta: la cabeza se dibuja
            // desplazada, deja el cuello al aire y el cuerpo parece incompleto.
            ImportarSprite(rutaCabeza, fuente);

            Sprite cuerpo = AssetDatabase.LoadAssetAtPath<Sprite>(rutaCuerpo);
            if (cuerpo != null) resultado[original] = cuerpo;
        }

        AssetDatabase.Refresh();
        return resultado;
    }

    static void GuardarPng(string ruta, Color[] pixeles, int w, int h)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.SetPixels(pixeles);
        t.Apply();
        File.WriteAllBytes(ruta, t.EncodeToPNG());
        Object.DestroyImmediate(t);
    }

    static void ImportarSprite(string ruta, TextureImporter fuente)
    {
        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) return;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.filterMode = FilterMode.Point;
        imp.wrapMode = TextureWrapMode.Clamp;

        if (fuente != null)
        {
            imp.spritePixelsPerUnit = fuente.spritePixelsPerUnit;
            imp.spritePivot = fuente.spritePivot;
            imp.filterMode = fuente.filterMode;
        }
        else
        {
            imp.spritePixelsPerUnit = 100f;
        }

        // El pivote tiene que quedar en el centro del lienzo de 512x256, no en el
        // centro de la silueta, para que cuerpo y cabeza encajen pixel a pixel.
        TextureImporterSettings ajustes = new TextureImporterSettings();
        imp.ReadTextureSettings(ajustes);
        ajustes.spriteAlignment = (int)SpriteAlignment.Center;
        ajustes.spritePivot = new Vector2(0.5f, 0.5f);
        imp.SetTextureSettings(ajustes);

        imp.SaveAndReimport();
    }

    // Caja de la silueta del dragón, sin la capa púrpura.
    static void Silueta(Color[] px, int w, int h, out int x0, out int y0, out int x1, out int y1)
    {
        x0 = int.MaxValue; y0 = int.MaxValue; x1 = int.MinValue; y1 = int.MinValue;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            Color p = px[(h - 1 - y) * w + x];
            if (p.a < 0.1f || EsPurpura(p)) continue;

            if (x < x0) x0 = x;
            if (x > x1) x1 = x;
            if (y < y0) y0 = y;
            if (y > y1) y1 = y;
        }

        if (x1 < x0) { x0 = 0; y0 = 0; x1 = 0; y1 = 0; }
    }

    // Solo se articulan los fotogramas en los que el dragón está de pie o
    // desplazándose. En la muerte el personaje se tinta de rojo, se convierte en un
    // aura y acaba siendo una bola: ahí no hay cabeza que separar y el fotograma se
    // deja entero en el cuerpo (la cabeza queda vacía, así que el conjunto sigue
    // siendo idéntico al original).
    static bool SeSepara(string nombre)
    {
        return nombre.StartsWith("idle") || nombre.StartsWith("walk") || nombre.StartsWith("flyKick");
    }

    static bool EnPoligono(float x, float y)
    {
        bool dentro = false;
        int n = PoligonoCabeza.Length;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if ((PoligonoCabeza[i].y > y) != (PoligonoCabeza[j].y > y) &&
                x < (PoligonoCabeza[j].x - PoligonoCabeza[i].x) * (y - PoligonoCabeza[i].y) /
                    (PoligonoCabeza[j].y - PoligonoCabeza[i].y) + PoligonoCabeza[i].x)
            {
                dentro = !dentro;
            }
        }

        return dentro;
    }

    // La capa púrpura de la capa no forma parte de la cabeza
    static bool EsPurpura(Color c)
    {
        return c.b > c.r + 0.02f && c.r > c.g + 0.02f;
    }

    // ------------------------------------------------------------ 2 · clips
    static void ReescribirClips(Dictionary<Sprite, Sprite> mapa)
    {
        for (int c = 0; c < Clips.Length; c++)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Clips[c]);
            if (clip == null) continue;

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                if (binding.propertyName != "m_Sprite") continue;

                var claves = AnimationUtility.GetObjectReferenceCurve(clip, binding);

                for (int k = 0; k < claves.Length; k++)
                {
                    Sprite original = claves[k].value as Sprite;
                    if (original == null) continue;

                    Sprite cuerpo = BuscarCuerpo(mapa, original);
                    if (cuerpo != null) claves[k].value = cuerpo;
                }

                AnimationUtility.SetObjectReferenceCurve(clip, binding, claves);
            }

            EditorUtility.SetDirty(clip);
        }

        AssetDatabase.SaveAssets();
    }

    // Empareja por nombre base y no por referencia: los clips pueden seguir
    // apuntando a fotogramas ya partidos (_cuerpo, _cuerpo_cuerpo, …).
    static Sprite BuscarCuerpo(Dictionary<Sprite, Sprite> mapa, Sprite original)
    {
        Sprite cuerpo;
        if (mapa.TryGetValue(original, out cuerpo) && cuerpo != null) return cuerpo;

        string buscado = Base(original.name);

        foreach (var par in mapa)
            if (par.Value != null && Base(par.Key.name) == buscado) return par.Value;

        return null;
    }

    static string Base(string nombre)
    {
        string n = nombre;

        while (true)
        {
            if (n.EndsWith("_cuerpo")) n = n.Substring(0, n.Length - 7);
            else if (n.EndsWith("_cabeza")) n = n.Substring(0, n.Length - 7);
            else return n;
        }
    }

    static Sprite[] CargarCabezas()
    {
        List<Sprite> lista = new List<Sprite>();

        string[] ficheros = Directory.GetFiles(CarpetaPartes, "*_cabeza.png");
        for (int i = 0; i < ficheros.Length; i++)
        {
            string ruta = ficheros[i].Replace('\\', '/');
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
            if (s != null) lista.Add(s);
        }

        return lista.ToArray();
    }

    // ------------------------------------------------------------ 3 · rig
    static void MontarRig(Sprite[] cabezas)
    {
        GameObject player = GameObject.Find("Arena/Player");
        if (player == null)
        {
            Debug.LogError("ConstructorPersonaje: no se encuentra Arena/Player en la escena abierta.");
            return;
        }

        // La pistola se monta aparte con "Spirit Dragon/Reconstruir arma"
        Transform viejo = player.transform.Find("CabezaPivot");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);

        SpriteRenderer cuerpo = player.GetComponent<SpriteRenderer>();

        // Cuerpo sin cabeza
        Sprite cuerpoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            CarpetaPartes + "/" + (cuerpo.sprite != null ? Base(cuerpo.sprite.name) : "idle_01") + "_cuerpo.png");

        if (cuerpoSprite == null)
            cuerpoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaPartes + "/idle_01_cuerpo.png");

        if (cuerpoSprite != null) cuerpo.sprite = cuerpoSprite;

        // Pivote del cuello
        GameObject pivote = new GameObject("CabezaPivot");
        pivote.transform.SetParent(player.transform, false);
        pivote.transform.localPosition = PivoteCuello;
        pivote.transform.localRotation = Quaternion.identity;
        pivote.transform.localScale = Vector3.one;

        // Cabeza
        GameObject goCabeza = new GameObject("Cabeza");
        goCabeza.transform.SetParent(pivote.transform, false);
        goCabeza.transform.localPosition = OffsetCabeza;

        SpriteRenderer srCabeza = goCabeza.AddComponent<SpriteRenderer>();
        srCabeza.sortingOrder = 6;
        srCabeza.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaPartes + "/idle_01_cabeza.png");

        // Boca por la que sale el fuego
        GameObject boca = new GameObject("Boca");
        boca.transform.SetParent(goCabeza.transform, false);
        boca.transform.localPosition = PosicionBoca;

        CabezaDragon script = player.GetComponent<CabezaDragon>();
        if (script == null) script = player.AddComponent<CabezaDragon>();

        script.pivoteCabeza = pivote.transform;
        script.cuerpo = cuerpo;
        script.cabeza = srCabeza;
        script.boca = boca.transform;
        script.cabezas = cabezas;

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.cabeza = script;
            pc.spriteCuerpo = cuerpo;
        }

        EditorUtility.SetDirty(player);
        Debug.Log("ConstructorPersonaje: rig de cabeza montado con " + cabezas.Length + " cabezas.");
    }

    // ------------------------------------------------------------ 4 · bola de fuego
    static void CrearBolaDeFuego()
    {
        TextureImporter imp = AssetImporter.GetAtPath(RutaSpriteBola) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 1024f;   // 1 unidad de diámetro
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.filterMode = FilterMode.Bilinear;
            imp.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSpriteBola);
        if (sprite == null) { Debug.LogError("ConstructorPersonaje: falta " + RutaSpriteBola); return; }

        GameObject go = new GameObject("BolaDeFuego");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 7;
        go.transform.localScale = Vector3.one * 0.5f;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.42f;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Bala bala = go.AddComponent<Bala>();
        bala.equipo = Equipo.Jugador;
        bala.velocidad = 17f;
        bala.vida = 3f;
        bala.danio = 12f;

        Directory.CreateDirectory("Assets/Prefab");

        PrefabUtility.SaveAsPrefabAsset(go, RutaPrefabBola);
        Object.DestroyImmediate(go);

        Debug.Log("ConstructorPersonaje: prefab de bola de fuego en " + RutaPrefabBola);
    }
}
