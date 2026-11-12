using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Pulido visual de los niveles: bloom suave, viñeta, algo de color y un resplandor cálido
/// alrededor del dragón. Se crea solo al cargar un nivel (no toca las escenas) y los valores
/// salen de la tabla de abajo. Para desactivarlo: Activo = false.
/// </summary>
public static class MejoraVisual
{
    public static bool Activo = true;

    struct Ajustes
    {
        public float bloom, umbral, vineta, saturacion, contraste;
        public Color tinte;
    }

    static Ajustes Para(string escena)
    {
        switch (escena)
        {
            case "Nivel2": // montaña nevada: fría y limpia
                return new Ajustes { bloom = 0.30f, umbral = 0.95f, vineta = 0.20f, saturacion = 4f, contraste = 6f, tinte = new Color(0.93f, 0.97f, 1f) };
            case "Nivel3":
                return new Ajustes { bloom = 0.40f, umbral = 0.90f, vineta = 0.26f, saturacion = 8f, contraste = 8f, tinte = new Color(1f, 0.95f, 0.98f) };
            case "Boss":
                return new Ajustes { bloom = 0.55f, umbral = 0.85f, vineta = 0.32f, saturacion = 10f, contraste = 12f, tinte = new Color(1f, 0.92f, 0.92f) };
            default:       // Nivel1: bosque cálido
                return new Ajustes { bloom = 0.35f, umbral = 0.92f, vineta = 0.22f, saturacion = 8f, contraste = 6f, tinte = new Color(1f, 0.98f, 0.93f) };
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Arrancar()
    {
        // Con "Reload Domain" desactivado las estáticas sobreviven entre Plays: nunca suscribir dos veces.
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        Aplicar(SceneManager.GetActiveScene());
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        Aplicar(escena);
    }

    static void Aplicar(Scene escena)
    {
        if (!Activo) return;
        string n = escena.name;
        if (n != "Nivel1" && n != "Nivel2" && n != "Nivel3" && n != "Boss") return;

        Camera cam = Camera.main;
        if (cam == null) return;

        UniversalAdditionalCameraData datos = cam.GetUniversalAdditionalCameraData();
        if (datos != null) datos.renderPostProcessing = true;

        CrearVolumen(Para(n));
        CrearLuces();
    }

    static void CrearVolumen(Ajustes a)
    {
        if (GameObject.Find("MejoraVisual_Volumen") != null) return;

        GameObject go = new GameObject("MejoraVisual_Volumen");
        Volume vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10f;
        VolumeProfile perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        vol.sharedProfile = perfil;

        Bloom bloom = perfil.Add<Bloom>(true);
        bloom.threshold.Override(a.umbral);
        bloom.intensity.Override(a.bloom);
        bloom.scatter.Override(0.6f);

        Vignette vineta = perfil.Add<Vignette>(true);
        vineta.intensity.Override(a.vineta);
        vineta.smoothness.Override(0.45f);

        ColorAdjustments color = perfil.Add<ColorAdjustments>(true);
        color.saturation.Override(a.saturacion);
        color.contrast.Override(a.contraste);
        color.colorFilter.Override(a.tinte);
    }

    static void CrearLuces()
    {
        // Si la escena ya trae luces 2D (el jefe las usa) no se toca nada.
        if (Object.FindFirstObjectByType<Light2D>() != null) return;

        // Luz global a intensidad normal: mantiene el brillo actual de los sprites.
        GameObject global = new GameObject("MejoraVisual_LuzGlobal");
        Light2D g = global.AddComponent<Light2D>();
        g.lightType = Light2D.LightType.Global;
        g.intensity = 1f;

        PlayerController jugador = Object.FindFirstObjectByType<PlayerController>();
        if (jugador == null) return;

        GameObject brillo = new GameObject("MejoraVisual_Resplandor");
        brillo.transform.SetParent(jugador.transform, false);
        Light2D l = brillo.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = new Color(1f, 0.62f, 0.25f);
        l.intensity = 0.45f;
        l.pointLightOuterRadius = 4.5f;
        l.pointLightInnerRadius = 0.4f;
        l.falloffIntensity = 0.6f;
    }
}
