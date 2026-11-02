using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Brillo de pantalla: una capa negra translúcida por encima de todo que sobrevive
/// a los cambios de escena. Su opacidad sale de ConfiguracionPartida.Brillo.
/// </summary>
public class BrilloGlobal : MonoBehaviour
{
    const float OscuridadMaxima = 0.8f;

    static BrilloGlobal instancia;
    Image capa;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        if (instancia != null) return;

        ConfiguracionPartida.Cargar();   // llama a Actualizar() y crea la capa
    }

    public static void Actualizar()
    {
        if (!Application.isPlaying) return;

        if (instancia == null) instancia = Crear();
        instancia.Aplicar();
    }

    static BrilloGlobal Crear()
    {
        GameObject go = new GameObject("BrilloGlobal");
        DontDestroyOnLoad(go);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        GameObject capaGO = new GameObject("Capa", typeof(RectTransform));
        capaGO.transform.SetParent(go.transform, false);

        RectTransform rt = capaGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        BrilloGlobal b = go.AddComponent<BrilloGlobal>();
        b.capa = capaGO.AddComponent<Image>();
        b.capa.color = Color.clear;
        b.capa.raycastTarget = false;   // no debe bloquear los clics del menú

        return b;
    }

    void Aplicar()
    {
        float oscuridad = Mathf.InverseLerp(1f, ConfiguracionPartida.BrilloMinimo, ConfiguracionPartida.Brillo);
        capa.color = new Color(0f, 0f, 0f, oscuridad * OscuridadMaxima);
    }
}
