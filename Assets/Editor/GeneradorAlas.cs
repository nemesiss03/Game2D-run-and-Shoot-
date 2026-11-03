using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Crea la animación de vuelo del dragón: 3 fotogramas de aleteo (alas arriba, medio, abajo) generados
/// a partir del cuerpo en reposo, el clip DragonVolar y el estado "Volar" del Animator.
/// Menú: Sky Wars / Generar animación de alas.
/// Solo se mueven las alas: gira el ala alrededor del hombro y se agranda un poco para que parezca abierta.
/// </summary>
public static class GeneradorAlas
{
    const string Carpeta = "Assets/Generated_Images/DragonPartes";
    const string Origen = Carpeta + "/idle_01_cuerpo.png";
    const string RutaClip = "Assets/Animations/DragonVolar.anim";
    const string RutaControlador = "Assets/Animations/Dragon.controller";

    // Zona del ala en el dibujo de 512 x 256 (el origen de y está ABAJO)
    const int CorteX = 234;          // el ala está a la izquierda de esta columna; el cuerpo, a la derecha
    const int CorteY = 95;           // por encima quedan restos del recorte de la cabeza: no son ala
    static readonly Vector2 Hombro = new Vector2(232f, 62f);
    const float RadioRaiz = 11f;    // alrededor del hombro el ala original no se mueve

    // Cada fotograma: ángulo (negativo = la punta sube) y escala del ala
    static readonly float[] Angulos = { -42f, -8f, 34f };
    static readonly float[] Escalas = { 1.32f, 1.38f, 1.28f };

    [MenuItem("Sky Wars/Generar animación de alas")]
    public static void Generar()
    {
        Texture2D origen = AssetDatabase.LoadAssetAtPath<Texture2D>(Origen);
        Color[] px = Leer(origen);
        int W = origen.width, H = origen.height;

        for (int f = 0; f < 3; f++)
        {
            Color[] salida = new Color[W * H];

            // 0) La raíz del ala original se queda: así no queda un hueco junto al hombro al girarla
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < CorteX; x++)
                {
                    if (y >= CorteY) continue;
                    if (Vector2.Distance(new Vector2(x, y), Hombro) > RadioRaiz) continue;
                    Color c = px[y * W + x];
                    if (c.a > 0f) salida[y * W + x] = c;
                }
            }

            // 1) El ala girada va detrás del cuerpo
            float a = Angulos[f] * Mathf.Deg2Rad;
            float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
            float s = Escalas[f];

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    // Transformación inversa: de la posición final a la del dibujo original
                    float dx = x - Hombro.x, dy = y - Hombro.y;
                    float rx = (dx * cos + dy * sin) / s;
                    float ry = (-dx * sin + dy * cos) / s;
                    float sx = Hombro.x + rx, sy = Hombro.y + ry;

                    if (sx >= CorteX || sy >= CorteY || sx < 0f || sy < 0f) continue;   // fuera del ala original

                    Color c = Bilineal(px, W, H, sx, sy, CorteX, CorteY);
                    if (c.a > 0.01f) salida[y * W + x] = Sobre(c, salida[y * W + x]);
                }
            }

            // 2) El cuerpo original encima (todo lo que no es ala)
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    bool esAla = x < CorteX && y < CorteY;
                    if (esAla) continue;

                    Color c = px[y * W + x];
                    if (c.a <= 0f) continue;
                    salida[y * W + x] = Sobre(c, salida[y * W + x]);
                }
            }

            string ruta = Carpeta + "/volar_0" + (f + 1) + "_cuerpo.png";
            Escribir(ruta, salida, W, H);
        }

        AssetDatabase.Refresh();

        TextureImporter modelo = AssetImporter.GetAtPath(Origen) as TextureImporter;
        for (int f = 0; f < 3; f++)
        {
            string ruta = Carpeta + "/volar_0" + (f + 1) + "_cuerpo.png";
            TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (imp == null) continue;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            if (modelo != null)
            {
                imp.spritePixelsPerUnit = modelo.spritePixelsPerUnit;
                imp.filterMode = modelo.filterMode;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                TextureImporterSettings ajustes = new TextureImporterSettings();
                modelo.ReadTextureSettings(ajustes);
                imp.SetTextureSettings(ajustes);
            }
            imp.SaveAndReimport();
        }

        RellenarClipsVacios();
        CrearClipYEstado();
        Debug.Log("GeneradorAlas: animación de vuelo creada (3 fotogramas + clip DragonVolar + estado Volar).");
    }

    // ------------------------------------------------------------------ clips existentes sin sprites
    /// <summary>
    /// Los clips Idle, Caminar, Dash y Muerte del repositorio tienen todas sus claves vacías (fileID 0):
    /// el Animator los reproduce y deja al dragón sin sprite. Aquí se rellenan con los fotogramas de
    /// Generated_Images/DragonPartes. Solo se toca un clip si todas sus claves están vacías.
    /// </summary>
    static void RellenarClipsVacios()
    {
        RellenarClip("Assets/Animations/DragonIdle.anim", new[] { "idle_01", "idle_02", "idle_03", "idle_04", "idle_05", "idle_06", "idle_01" });
        RellenarClip("Assets/Animations/DragonCaminar.anim", new[] { "walk_01", "walk_02", "walk_03", "walk_04", "walk_05", "walk_06", "walk_01" });
        RellenarClip("Assets/Animations/DragonDash.anim", new[] { "flyKick_01", "flyKick_02", "flyKick_03", "flyKick_04", "flyKick_05", "flyKick_06", "flyKick_06" });
        RellenarClip("Assets/Animations/DragonMuerte.anim", new[] { "die_001", "die_002", "die_003", "die_004", "die_005", "die_006", "die_007", "die_008", "die_009", "die_010", "die_010" });
    }

    static void RellenarClip(string ruta, string[] fotogramas)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta);
        if (clip == null) { Debug.LogWarning("No encuentro " + ruta); return; }

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] actuales = AnimationUtility.GetObjectReferenceCurve(clip, binding);

        bool hayValidas = false;
        if (actuales != null)
            foreach (ObjectReferenceKeyframe k in actuales)
                if (k.value != null) hayValidas = true;

        if (hayValidas) return;   // ya tiene sprites: no se toca

        float paso = 1f / Mathf.Max(1f, clip.frameRate);
        ObjectReferenceKeyframe[] claves = new ObjectReferenceKeyframe[fotogramas.Length];

        for (int i = 0; i < fotogramas.Length; i++)
        {
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(Carpeta + "/" + fotogramas[i] + "_cuerpo.png");
            claves[i] = new ObjectReferenceKeyframe { time = i * paso, value = sp };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, claves);
        EditorUtility.SetDirty(clip);
        Debug.Log("GeneradorAlas: clip " + clip.name + " rellenado con " + fotogramas.Length + " fotogramas.");
    }

    // ------------------------------------------------------------------ clip y animator
    static void CrearClipYEstado()
    {
        Sprite[] frames = new Sprite[3];
        for (int i = 0; i < 3; i++)
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(Carpeta + "/volar_0" + (i + 1) + "_cuerpo.png");

        // Aleteo de ida y vuelta: arriba, medio, abajo, medio
        int[] orden = { 0, 1, 2, 1 };
        float paso = 0.08f;

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RutaClip);
        if (clip == null)
        {
            clip = new AnimationClip { name = "DragonVolar" };
            AssetDatabase.CreateAsset(clip, RutaClip);
        }

        clip.frameRate = 12f;
        ObjectReferenceKeyframe[] claves = new ObjectReferenceKeyframe[orden.Length];
        for (int i = 0; i < orden.Length; i++)
            claves[i] = new ObjectReferenceKeyframe { time = i * paso, value = frames[orden[i]] };

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, claves);

        AnimationClipSettings ajustes = AnimationUtility.GetAnimationClipSettings(clip);
        ajustes.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, ajustes);
        EditorUtility.SetDirty(clip);

        // Animator: parámetro EnAire y estado Volar
        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);
        if (ctrl == null) { Debug.LogError("No encuentro " + RutaControlador); return; }

        bool tieneParametro = false;
        foreach (AnimatorControllerParameter p in ctrl.parameters)
            if (p.name == "EnAire") tieneParametro = true;
        if (!tieneParametro) ctrl.AddParameter("EnAire", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        AnimatorState volar = null, idle = null, caminar = null;
        foreach (ChildAnimatorState cs in sm.states)
        {
            if (cs.state.name == "Volar") volar = cs.state;
            else if (cs.state.name == "Idle") idle = cs.state;
            else if (cs.state.name == "Caminar") caminar = cs.state;
        }

        if (volar == null)
        {
            volar = sm.AddState("Volar", new Vector3(500f, -120f, 0f));
            volar.motion = clip;

            if (idle != null) Transicion(idle, volar, true);
            if (caminar != null) Transicion(caminar, volar, true);
            if (idle != null) Transicion(volar, idle, false);
        }
        else
        {
            volar.motion = clip;
        }

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
    }

    static void Transicion(AnimatorState desde, AnimatorState hasta, bool enAire)
    {
        AnimatorStateTransition t = desde.AddTransition(hasta);
        t.hasExitTime = false;
        t.duration = 0f;
        t.AddCondition(enAire ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "EnAire");
    }

    // ------------------------------------------------------------------ imagen
    static Color[] Leer(Texture2D src)
    {
        RenderTexture rt = RenderTexture.GetTemporary(src.width, src.height, 0);
        Graphics.Blit(src, rt);
        RenderTexture previo = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D lectura = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        lectura.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        lectura.Apply();

        RenderTexture.active = previo;
        RenderTexture.ReleaseTemporary(rt);

        Color[] c = lectura.GetPixels();
        Object.DestroyImmediate(lectura);
        return c;
    }

    /// <summary>Interpolación bilineal con color premultiplicado, para que los bordes del ala no se ensucien.</summary>
    static Color Bilineal(Color[] px, int W, int H, float x, float y, int maxX, int maxY)
    {
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;

        Color a = Pre(px, W, H, x0, y0, maxX, maxY), b = Pre(px, W, H, x0 + 1, y0, maxX, maxY);
        Color c = Pre(px, W, H, x0, y0 + 1, maxX, maxY), d = Pre(px, W, H, x0 + 1, y0 + 1, maxX, maxY);

        Color r = Color.Lerp(Color.Lerp(a, b, fx), Color.Lerp(c, d, fx), fy);
        if (r.a <= 0.001f) return Color.clear;
        return new Color(r.r / r.a, r.g / r.a, r.b / r.a, r.a);
    }

    static Color Pre(Color[] px, int W, int H, int x, int y, int maxX, int maxY)
    {
        if (x < 0 || y < 0 || x >= maxX || y >= maxY || x >= W || y >= H) return Color.clear;
        Color c = px[y * W + x];
        return new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
    }

    /// <summary>Compone "arriba" sobre "abajo" (sin premultiplicar).</summary>
    static Color Sobre(Color arriba, Color abajo)
    {
        float a = arriba.a + abajo.a * (1f - arriba.a);
        if (a <= 0f) return Color.clear;

        Color c = (arriba * arriba.a + abajo * abajo.a * (1f - arriba.a)) / a;
        c.a = a;
        return c;
    }

    static void Escribir(string ruta, Color[] px, int W, int H)
    {
        Texture2D t = new Texture2D(W, H, TextureFormat.RGBA32, false);
        t.SetPixels(px);
        t.Apply();
        File.WriteAllBytes(ruta, t.EncodeToPNG());
        Object.DestroyImmediate(t);
    }
}
