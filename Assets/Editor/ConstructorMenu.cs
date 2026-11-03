using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Construye la escena del menú principal de "Spirit Dragon" con todos sus paneles
/// (Jugar → Solo PC / Multijugador, Configuración y Salir).
/// Menú: Spirit Dragon / Construir menú
/// </summary>
public static class ConstructorMenu
{
    const string RutaEscena = "Assets/Scenes/MenuPrincipal.unity";
    const string RutaFondo = "Assets/Generated_Images/skywars_fondo.png";
    const string RutaNube = "Assets/Generated_Images/nube_menu.png";
    const string CarpetaUI = "Assets/UI";

    static readonly Color Dorado = new Color(1f, 0.83f, 0.35f, 1f);
    static readonly Color Crema = new Color(1f, 0.96f, 0.86f, 1f);
    static readonly Color AzulOscuro = new Color(0.10f, 0.15f, 0.32f, 1f);

    static Font fuente;

    [MenuItem("Spirit Dragon/Construir menú")]
    public static void Construir()
    {
        GenerarSpritesUI();
        ImportarSprite(RutaFondo, false);
        ImportarSprite(RutaNube, false);

        fuente = Fuente();

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Cámara (fondo por si acaso) ---
        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.08f, 0.18f, 1f);
        cam.orthographic = true;
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0f, 0f, -10f);

        // --- Canvas ---
        GameObject canvasGO = new GameObject("CanvasMenu");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        MenuPrincipal menu = canvasGO.AddComponent<MenuPrincipal>();

        // --- EventSystem con el Input System nuevo ---
        GameObject esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        // --- Fondo y decoración ---
        Image fondo = CrearImagen(canvasGO.transform, "Fondo", AssetDatabase.LoadAssetAtPath<Sprite>(RutaFondo), Color.white);
        Estirar(fondo.rectTransform);
        fondo.raycastTarget = false;

        Image velo = CrearImagen(canvasGO.transform, "Velo", null, new Color(0.02f, 0.03f, 0.10f, 0.35f));
        Estirar(velo.rectTransform);
        velo.raycastTarget = false;

        Sprite nube = AssetDatabase.LoadAssetAtPath<Sprite>(RutaNube);
        RectTransform[] nubes = new RectTransform[5];
        for (int i = 0; i < nubes.Length; i++)
        {
            Image n = CrearImagen(canvasGO.transform, "Nube" + (i + 1), nube, new Color(1f, 1f, 1f, 0.55f - i * 0.06f));
            n.raycastTarget = false;

            float x = -700f + i * 340f;
            float y = 260f - (i % 3) * 230f;
            float esc = 0.9f + (i % 2) * 0.5f;
            n.rectTransform.sizeDelta = new Vector2(420f, 220f) * esc;
            n.rectTransform.anchoredPosition = new Vector2(x, y);
        }

        // --- Paneles ---
        GameObject panelPrincipal = CrearPanel(canvasGO.transform, "PanelPrincipal", false);
        GameObject panelJugar = CrearPanel(canvasGO.transform, "PanelJugar", true);
        GameObject panelLocal = CrearPanel(canvasGO.transform, "PanelLocal", true);
        GameObject panelDispositivos = CrearPanel(canvasGO.transform, "PanelDispositivos", true);
        GameObject panelOnline = CrearPanel(canvasGO.transform, "PanelOnline", true);
        GameObject panelConfig = CrearPanel(canvasGO.transform, "PanelConfig", true);

        // ==================== PRINCIPAL ====================
        RectTransform titulo = CrearTitulo(panelPrincipal.transform, "SPIRIT DRAGON", 130, new Vector2(0f, 265f));

        Text sub = CrearTexto(panelPrincipal.transform, "Subtitulo", "ABYSS HUNTER", 44,
            Crema, new Vector2(0f, 150f), new Vector2(1000f, 60f), TextAnchor.MiddleCenter);
        sub.fontStyle = FontStyle.BoldAndItalic;

        Button bJugar = CrearBoton(panelPrincipal.transform, "BotonJugar", "EMPEZAR", new Vector2(0f, 20f), new Vector2(520f, 112f), Dorado);
        Button bConfig = CrearBoton(panelPrincipal.transform, "BotonConfig", "OPCIONES", new Vector2(0f, -105f), new Vector2(520f, 112f), Crema);
        Button bSalir = CrearBoton(panelPrincipal.transform, "BotonSalir", "SALIR", new Vector2(0f, -230f), new Vector2(520f, 112f), new Color(1f, 0.55f, 0.5f, 1f));

        Text version = CrearTexto(panelPrincipal.transform, "Version", "SPIRIT DRAGON: ABYSS HUNTER · v1.0", 26,
            new Color(1f, 1f, 1f, 0.65f), Vector2.zero, new Vector2(420f, 40f), TextAnchor.MiddleRight);
        Anclar(version.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-230f, 40f));

        // ==================== JUGAR ====================
        RectTransform marcoJugar = CrearMarco(panelJugar.transform, "JUGAR");
        CrearBoton(marcoJugar, "BotonSoloPC", "SOLO PC", new Vector2(0f, 90f), new Vector2(560f, 110f), Dorado);
        CrearBoton(marcoJugar, "BotonMulti", "MULTIJUGADOR", new Vector2(0f, -40f), new Vector2(560f, 110f), Crema);
        CrearBoton(marcoJugar, "BotonVolver", "VOLVER", new Vector2(0f, -185f), new Vector2(340f, 84f), new Color(0.85f, 0.85f, 0.9f, 1f));

        // ==================== SOLO PC ====================
        RectTransform marcoLocal = CrearMarco(panelLocal.transform, "JUGAR");
        Button bUnJugador = CrearBoton(marcoLocal, "Boton1Jugador", "1 JUGADOR", new Vector2(0f, 90f), new Vector2(560f, 110f), Dorado);
        Button bDosJugadores = CrearBoton(marcoLocal, "Boton2Jugadores", "2 JUGADORES", new Vector2(0f, -40f), new Vector2(560f, 110f), Crema);
        Button bVolverLocal = CrearBoton(marcoLocal, "BotonVolver", "VOLVER", new Vector2(0f, -185f), new Vector2(340f, 84f), new Color(0.85f, 0.85f, 0.9f, 1f));

        // ==================== DISPOSITIVOS ====================
        RectTransform marcoDisp = CrearMarco(panelDispositivos.transform, "DOS JUGADORES");

        Text tJ1 = CrearTexto(marcoDisp, "TextoJ1", "JUGADOR 1: TECLADO + RATÓN", 34, Dorado,
            new Vector2(0f, 170f), new Vector2(800f, 44f), TextAnchor.MiddleCenter);

        CrearBoton(marcoDisp, "J1Teclado", "TECLADO + RATÓN", new Vector2(-220f, 105f), new Vector2(400f, 78f), Crema);
        CrearBoton(marcoDisp, "J1Mando", "MANDO", new Vector2(220f, 105f), new Vector2(400f, 78f), Crema);

        Text tJ2 = CrearTexto(marcoDisp, "TextoJ2", "JUGADOR 2: TECLADO", 34, Dorado,
            new Vector2(0f, 25f), new Vector2(800f, 44f), TextAnchor.MiddleCenter);

        CrearBoton(marcoDisp, "J2Teclado", "TECLADO", new Vector2(-220f, -40f), new Vector2(400f, 78f), Crema);
        CrearBoton(marcoDisp, "J2Mando", "MANDO", new Vector2(220f, -40f), new Vector2(400f, 78f), Crema);

        Button bComenzar = CrearBoton(marcoDisp, "BotonComenzar", "¡COMENZAR!", new Vector2(0f, -150f), new Vector2(560f, 100f), Dorado);
        Button bVolverDisp = CrearBoton(marcoDisp, "BotonVolver", "VOLVER", new Vector2(0f, -265f), new Vector2(340f, 78f), new Color(0.85f, 0.85f, 0.9f, 1f));

        // ==================== ONLINE ====================
        RectTransform marcoOnline = CrearMarco(panelOnline.transform, "MULTIJUGADOR");

        Button bHost = CrearBoton(marcoOnline, "BotonHost", "ABRIR SERVIDOR", new Vector2(0f, 140f), new Vector2(560f, 100f), Dorado);
        Button bUnirse = CrearBoton(marcoOnline, "BotonUnirse", "UNIRSE A UNA PARTIDA", new Vector2(0f, -60f), new Vector2(560f, 100f), Crema);

        InputField campoIp = CrearCampoTexto(marcoOnline, "CampoIP", "IP del host (127.0.0.1)", new Vector2(0f, 20f), new Vector2(560f, 74f));

        Text tOnline = CrearTexto(marcoOnline, "TextoOnline", "", 28, Crema,
            new Vector2(0f, -170f), new Vector2(880f, 90f), TextAnchor.MiddleCenter);
        tOnline.horizontalOverflow = HorizontalWrapMode.Wrap;

        Button bVolverOnline = CrearBoton(marcoOnline, "BotonVolver", "VOLVER", new Vector2(0f, -265f), new Vector2(340f, 78f), new Color(0.85f, 0.85f, 0.9f, 1f));

        // ==================== CONFIGURACIÓN ====================
        RectTransform marcoConfig = CrearMarco(panelConfig.transform, "OPCIONES");

        CrearTexto(marcoConfig, "LabelVolumen", "SONIDO GENERAL", 36, Crema,
            new Vector2(0f, 175f), new Vector2(700f, 48f), TextAnchor.MiddleCenter);

        Slider slider = CrearSlider(marcoConfig, "SliderVolumen", new Vector2(0f, 120f), new Vector2(620f, 40f));

        Text tVol = CrearTexto(marcoConfig, "TextoVolumen", "100%", 32, Dorado,
            new Vector2(0f, 70f), new Vector2(400f, 44f), TextAnchor.MiddleCenter);

        CrearTexto(marcoConfig, "LabelBrillo", "BRILLO", 36, Crema,
            new Vector2(0f, -20f), new Vector2(700f, 48f), TextAnchor.MiddleCenter);

        Slider sliderBrillo = CrearSlider(marcoConfig, "SliderBrillo", new Vector2(0f, -75f), new Vector2(620f, 40f));

        Text tBrillo = CrearTexto(marcoConfig, "TextoBrillo", "100%", 32, Dorado,
            new Vector2(0f, -125f), new Vector2(400f, 44f), TextAnchor.MiddleCenter);

        Button bVolverConfig = CrearBoton(marcoConfig, "BotonVolver", "VOLVER", new Vector2(0f, -240f), new Vector2(340f, 84f), new Color(0.85f, 0.85f, 0.9f, 1f));

        // ==================== ANIMADOR ====================
        MenuAnimador anim = canvasGO.AddComponent<MenuAnimador>();
        anim.titulo = titulo;
        anim.nubes = nubes;
        anim.destellos = new Graphic[] { sub };

        // ==================== CABLEADO ====================
        menu.panelPrincipal = panelPrincipal;
        menu.panelJugar = panelJugar;
        menu.panelLocal = panelLocal;
        menu.panelDispositivos = panelDispositivos;
        menu.panelOnline = panelOnline;
        menu.panelConfig = panelConfig;
        menu.textoJ1 = tJ1;
        menu.textoJ2 = tJ2;
        menu.textoOnline = tOnline;
        menu.textoVolumen = tVol;
        menu.sliderVolumen = slider;
        menu.sliderBrillo = sliderBrillo;
        menu.textoBrillo = tBrillo;
        menu.campoIp = campoIp;

        UnityEventTools.AddPersistentListener(bJugar.onClick, menu.IrAJugar);
        UnityEventTools.AddPersistentListener(bConfig.onClick, menu.IrAConfig);
        UnityEventTools.AddPersistentListener(bSalir.onClick, menu.Salir);

        Transform marcoJugarT = panelJugar.transform.Find("Marco");
        UnityEventTools.AddPersistentListener(marcoJugarT.Find("BotonSoloPC").GetComponent<Button>().onClick, menu.IrALocal);
        UnityEventTools.AddPersistentListener(marcoJugarT.Find("BotonMulti").GetComponent<Button>().onClick, menu.IrAOnline);
        UnityEventTools.AddPersistentListener(marcoJugarT.Find("BotonVolver").GetComponent<Button>().onClick, menu.IrAInicio);

        Transform marcoLocalT = panelLocal.transform.Find("Marco");
        UnityEventTools.AddPersistentListener(marcoLocalT.Find("Boton1Jugador").GetComponent<Button>().onClick, menu.JugarUnJugador);
        UnityEventTools.AddPersistentListener(marcoLocalT.Find("Boton2Jugadores").GetComponent<Button>().onClick, menu.IrADosJugadores);
        UnityEventTools.AddPersistentListener(marcoLocalT.Find("BotonVolver").GetComponent<Button>().onClick, menu.IrAInicio);

        Transform marcoDispT = panelDispositivos.transform.Find("Marco");
        UnityEventTools.AddPersistentListener(marcoDispT.Find("J1Teclado").GetComponent<Button>().onClick, menu.J1Teclado);
        UnityEventTools.AddPersistentListener(marcoDispT.Find("J1Mando").GetComponent<Button>().onClick, menu.J1Mando);
        UnityEventTools.AddPersistentListener(marcoDispT.Find("J2Teclado").GetComponent<Button>().onClick, menu.J2Teclado);
        UnityEventTools.AddPersistentListener(marcoDispT.Find("J2Mando").GetComponent<Button>().onClick, menu.J2Mando);
        UnityEventTools.AddPersistentListener(bComenzar.onClick, menu.ComenzarDosJugadores);
        UnityEventTools.AddPersistentListener(bVolverDisp.onClick, menu.IrALocal);

        Transform marcoOnlineT = panelOnline.transform.Find("Marco");
        UnityEventTools.AddPersistentListener(bHost.onClick, menu.AbrirServidor);
        UnityEventTools.AddPersistentListener(bUnirse.onClick, menu.Unirse);
        UnityEventTools.AddPersistentListener(bVolverOnline.onClick, menu.IrAJugar);

        UnityEventTools.AddPersistentListener(bVolverConfig.onClick, menu.IrAInicio);

        // --- Guardado ---
        panelJugar.SetActive(false);
        panelLocal.SetActive(false);
        panelDispositivos.SetActive(false);
        panelOnline.SetActive(false);
        panelConfig.SetActive(false);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(escena, RutaEscena);

        RegistrarEnBuildSettings();

        Debug.Log("ConstructorMenu: menú de Spirit Dragon creado en " + RutaEscena);
    }

    // ------------------------------------------------------------ build settings
    static void RegistrarEnBuildSettings()
    {
        var lista = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        lista.RemoveAll(s => s.path == RutaEscena || s.path == "Assets/Scenes/Boss.unity" ||
                             s.path == "Assets/Scenes/Nivel1.unity" || s.path == "Assets/Scenes/SampleScene.unity");

        lista.Insert(0, new EditorBuildSettingsScene(RutaEscena, true));
        lista.Insert(1, new EditorBuildSettingsScene("Assets/Scenes/Boss.unity", true));
        lista.Insert(2, new EditorBuildSettingsScene("Assets/Scenes/Nivel1.unity", true));

        EditorBuildSettings.scenes = lista.ToArray();
    }

    // ------------------------------------------------------------ fuentes / sprites
    static Font Fuente()
    {
        Font f = null;

        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }

        return f;
    }

    static void ImportarSprite(string ruta, bool punto)
    {
        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) return;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100f;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.filterMode = punto ? FilterMode.Point : FilterMode.Bilinear;
        imp.SaveAndReimport();
    }

    static void GenerarSpritesUI()
    {
        Directory.CreateDirectory(CarpetaUI);

        GenerarRedondeado(CarpetaUI + "/boton_ui.png",
            new Color(0.16f, 0.24f, 0.52f, 1f), new Color(0.08f, 0.12f, 0.30f, 1f),
            new Color(1f, 0.82f, 0.36f, 1f), 22, 4);

        GenerarRedondeado(CarpetaUI + "/panel_ui.png",
            new Color(0.05f, 0.07f, 0.16f, 0.94f), new Color(0.02f, 0.03f, 0.09f, 0.94f),
            new Color(1f, 0.82f, 0.36f, 0.95f), 22, 3);

        GenerarRedondeado(CarpetaUI + "/campo_ui.png",
            new Color(0.92f, 0.95f, 1f, 1f), new Color(0.80f, 0.85f, 0.96f, 1f),
            new Color(1f, 0.82f, 0.36f, 1f), 16, 3);

        AssetDatabase.Refresh();

        AjustarBorde(CarpetaUI + "/boton_ui.png", 28);
        AjustarBorde(CarpetaUI + "/panel_ui.png", 28);
        AjustarBorde(CarpetaUI + "/campo_ui.png", 20);

        AssetDatabase.Refresh();
    }

    static void GenerarRedondeado(string ruta, Color arriba, Color abajo, Color borde, int radio, int grosor)
    {
        const int T = 96;

        Texture2D tex = new Texture2D(T, T, TextureFormat.RGBA32, false);
        Color[] px = new Color[T * T];

        for (int y = 0; y < T; y++)
        {
            for (int x = 0; x < T; x++)
            {
                float dx = Mathf.Max(radio - x, x - (T - 1 - radio), 0f);
                float dy = Mathf.Max(radio - y, y - (T - 1 - radio), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                Color c;

                if (dist > radio) c = new Color(0f, 0f, 0f, 0f);
                else if (dist > radio - grosor) c = borde;
                else
                {
                    float t = (float)y / (T - 1);
                    c = Color.Lerp(abajo, arriba, t);

                    // brillo superior tipo cristal
                    if (y > T - 22 && dist <= radio - grosor) c = Color.Lerp(c, Color.white, 0.18f);
                }

                px[y * T + x] = c;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static void AjustarBorde(string ruta, int borde)
    {
        TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) return;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100f;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.filterMode = FilterMode.Bilinear;

        var settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        settings.spriteBorder = new Vector4(borde, borde, borde, borde);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(settings);

        imp.SaveAndReimport();
    }

    // ------------------------------------------------------------ helpers UI
    static RectTransform Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    static void Anclar(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
    }

    static Image CrearImagen(Transform padre, string nombre, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        if (sprite != null) img.type = Image.Type.Sliced;

        return img;
    }

    static GameObject CrearPanel(Transform padre, string nombre, bool conFondo)
    {
        GameObject panel = new GameObject(nombre, typeof(RectTransform));
        panel.transform.SetParent(padre, false);
        Estirar(panel.GetComponent<RectTransform>());

        if (conFondo)
        {
            Image velo = CrearImagen(panel.transform, "Velo", null, new Color(0.01f, 0.02f, 0.06f, 0.72f));
            Estirar(velo.rectTransform);
            velo.raycastTarget = true;
        }

        return panel;
    }

    static RectTransform CrearMarco(Transform panel, string titulo)
    {
        Image marco = CrearImagen(panel, "Marco", AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaUI + "/panel_ui.png"), Color.white);
        marco.rectTransform.sizeDelta = new Vector2(1000f, 720f);
        marco.rectTransform.anchoredPosition = Vector2.zero;

        CrearTitulo(marco.transform, titulo, 92, new Vector2(0f, 260f));

        return marco.rectTransform;
    }

    static RectTransform CrearTitulo(Transform padre, string texto, int tam, Vector2 pos)
    {
        Text t = CrearTexto(padre, "Titulo", texto, tam, Dorado, pos, new Vector2(1300f, tam * 1.3f), TextAnchor.MiddleCenter);
        t.fontStyle = FontStyle.Bold;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        Outline o = t.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0.35f, 0.13f, 0f, 0.95f);
        o.effectDistance = new Vector2(5f, -5f);

        Shadow s = t.gameObject.AddComponent<Shadow>();
        s.effectColor = new Color(0f, 0f, 0f, 0.65f);
        s.effectDistance = new Vector2(8f, -8f);

        return t.rectTransform;
    }

    static Text CrearTexto(Transform padre, string nombre, string contenido, int tam, Color color,
                           Vector2 pos, Vector2 tamano, TextAnchor anclaje)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        Text t = go.AddComponent<Text>();
        t.font = fuente;
        t.text = contenido;
        t.fontSize = tam;
        t.color = color;
        t.alignment = anclaje;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        Outline o = go.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.9f);
        o.effectDistance = new Vector2(3f, -3f);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = tamano;
        rt.anchoredPosition = pos;

        return t;
    }

    static Button CrearBoton(Transform padre, string nombre, string etiqueta, Vector2 pos, Vector2 tam, Color colorTexto)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = tam;
        rt.anchoredPosition = pos;

        Image img = go.AddComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaUI + "/boton_ui.png");
        img.type = Image.Type.Sliced;

        Button b = go.AddComponent<Button>();
        b.targetGraphic = img;

        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.93f, 0.62f, 1f);
        cb.pressedColor = new Color(0.72f, 0.72f, 0.78f, 1f);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.08f;
        cb.colorMultiplier = 1.4f;
        b.colors = cb;

        Text t = CrearTexto(go.transform, "Etiqueta", etiqueta, Mathf.RoundToInt(tam.y * 0.46f), colorTexto,
            Vector2.zero, tam, TextAnchor.MiddleCenter);
        t.fontStyle = FontStyle.Bold;
        Estirar(t.rectTransform);

        return b;
    }

    static InputField CrearCampoTexto(Transform padre, string nombre, string marcador, Vector2 pos, Vector2 tam)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = tam;
        rt.anchoredPosition = pos;

        Image img = go.AddComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaUI + "/campo_ui.png");
        img.type = Image.Type.Sliced;

        InputField campo = go.AddComponent<InputField>();

        Text texto = CrearTexto(go.transform, "Texto", "", Mathf.RoundToInt(tam.y * 0.42f),
            AzulOscuro, Vector2.zero, tam, TextAnchor.MiddleLeft);
        texto.rectTransform.offsetMin = new Vector2(18f, 0f);
        texto.rectTransform.offsetMax = new Vector2(-18f, 0f);
        Estirar(texto.rectTransform);
        texto.rectTransform.offsetMin = new Vector2(18f, 0f);
        texto.rectTransform.offsetMax = new Vector2(-18f, 0f);
        texto.supportRichText = false;
        texto.horizontalOverflow = HorizontalWrapMode.Overflow;

        Text marca = CrearTexto(go.transform, "Marcador", marcador, Mathf.RoundToInt(tam.y * 0.38f),
            new Color(0.35f, 0.4f, 0.55f, 0.85f), Vector2.zero, tam, TextAnchor.MiddleLeft);
        Estirar(marca.rectTransform);
        marca.rectTransform.offsetMin = new Vector2(18f, 0f);
        marca.rectTransform.offsetMax = new Vector2(-18f, 0f);
        marca.fontStyle = FontStyle.Italic;

        campo.textComponent = texto;
        campo.placeholder = marca;
        campo.targetGraphic = img;
        campo.text = "127.0.0.1";

        return campo;
    }

    static Slider CrearSlider(Transform padre, string nombre, Vector2 pos, Vector2 tam)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = tam;
        rt.anchoredPosition = pos;

        Slider slider = go.AddComponent<Slider>();

        Image fondoBarra = CrearImagen(go.transform, "Fondo", AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaUI + "/panel_ui.png"),
            new Color(1f, 1f, 1f, 0.85f));
        Estirar(fondoBarra.rectTransform);

        GameObject area = new GameObject("RellenoArea", typeof(RectTransform));
        area.transform.SetParent(go.transform, false);
        Estirar(area.GetComponent<RectTransform>());

        GameObject relleno = new GameObject("Relleno", typeof(RectTransform));
        relleno.transform.SetParent(area.transform, false);
        Image imgRelleno = relleno.AddComponent<Image>();
        imgRelleno.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaUI + "/boton_ui.png");
        imgRelleno.type = Image.Type.Sliced;
        Estirar(relleno.GetComponent<RectTransform>());

        GameObject tirador = new GameObject("Tirador", typeof(RectTransform));
        tirador.transform.SetParent(go.transform, false);
        Image imgTirador = tirador.AddComponent<Image>();
        imgTirador.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaUI + "/boton_ui.png");
        imgTirador.type = Image.Type.Sliced;
        tirador.GetComponent<RectTransform>().sizeDelta = new Vector2(26f, 46f);

        slider.fillRect = relleno.GetComponent<RectTransform>();
        slider.handleRect = tirador.GetComponent<RectTransform>();
        slider.targetGraphic = imgTirador;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        return slider;
    }

    static Toggle CrearToggle(Transform padre, string nombre, Vector2 pos)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(56f, 56f);
        rt.anchoredPosition = pos;

        Image img = go.AddComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaUI + "/panel_ui.png");
        img.type = Image.Type.Sliced;

        Toggle toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = img;

        GameObject marca = new GameObject("Marca", typeof(RectTransform));
        marca.transform.SetParent(go.transform, false);
        Estirar(marca.GetComponent<RectTransform>());
        marca.GetComponent<RectTransform>().offsetMin = new Vector2(8f, 8f);
        marca.GetComponent<RectTransform>().offsetMax = new Vector2(-8f, -8f);

        Image imgMarca = marca.AddComponent<Image>();
        imgMarca.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaUI + "/boton_ui.png");
        imgMarca.type = Image.Type.Sliced;
        imgMarca.color = new Color(1f, 0.85f, 0.4f, 1f);

        toggle.graphic = imgMarca;
        toggle.isOn = true;

        return toggle;
    }
}
