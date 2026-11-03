using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Herramientas visuales de las cinemáticas: barras negras de cine arriba y abajo, fundidos a negro
/// y destellos. Vive en su propio Canvas y pasa de una escena a otra, así un fundido a negro puede
/// empezar en un nivel y terminar en el siguiente.
/// Se usa con "yield return EfectosCine.Instancia.Fundido(...)" dentro de una corrutina.
/// </summary>
public class EfectosCine : MonoBehaviour
{
    static EfectosCine instancia;

    public static EfectosCine Instancia
    {
        get
        {
            if (instancia == null)
            {
                GameObject go = new GameObject("EfectosCine");
                DontDestroyOnLoad(go);
                instancia = go.AddComponent<EfectosCine>();
                instancia.Construir();
            }

            return instancia;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpiar() { instancia = null; }

    RectTransform barraArriba, barraAbajo;
    Image velo;
    float alturaBarras;          // 0 = ocultas, 1 = visibles
    const float AlturaPantalla = 0.12f;

    void Construir()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;

        barraArriba = CrearBarra("BarraArriba", true);
        barraAbajo = CrearBarra("BarraAbajo", false);

        GameObject v = new GameObject("Velo", typeof(RectTransform));
        v.transform.SetParent(transform, false);
        velo = v.AddComponent<Image>();
        velo.color = new Color(0f, 0f, 0f, 0f);
        velo.raycastTarget = false;

        RectTransform rt = velo.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        AplicarBarras();
    }

    RectTransform CrearBarra(string nombre, bool arriba)
    {
        GameObject g = new GameObject(nombre, typeof(RectTransform));
        g.transform.SetParent(transform, false);

        Image img = g.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(0f, arriba ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, arriba ? 1f : 0f);
        rt.pivot = new Vector2(0.5f, arriba ? 1f : 0f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    void AplicarBarras()
    {
        float alto = Screen.height * AlturaPantalla * alturaBarras;
        barraArriba.sizeDelta = new Vector2(0f, alto);
        barraAbajo.sizeDelta = new Vector2(0f, alto);
    }

    // ------------------------------------------------------------ API
    public IEnumerator Barras(bool visibles, float duracion = 0.6f)
    {
        float desde = alturaBarras, hasta = visibles ? 1f : 0f, t = 0f;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            alturaBarras = Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duracion)));
            AplicarBarras();
            yield return null;
        }

        alturaBarras = hasta;
        AplicarBarras();
    }

    public void BarrasInmediato(bool visibles)
    {
        alturaBarras = visibles ? 1f : 0f;
        AplicarBarras();
    }

    public IEnumerator Fundido(float alfaFinal, float duracion, Color color)
    {
        Color desde = velo.color;
        Color hasta = new Color(color.r, color.g, color.b, alfaFinal);
        float t = 0f;

        // Si el velo era de otro color, se cambia el color sin salto y se anima solo el alfa
        Color inicio = new Color(color.r, color.g, color.b, desde.a);
        velo.color = inicio;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            velo.color = Color.Lerp(inicio, hasta, Mathf.Clamp01(t / duracion));
            yield return null;
        }

        velo.color = hasta;
    }

    public void FundidoInmediato(float alfa)
    {
        Color c = velo.color;
        c.a = alfa;
        velo.color = c;
    }

    /// <summary>Destello blanco corto (explosiones grandes).</summary>
    public IEnumerator Destello(float duracion = 0.35f)
    {
        velo.color = new Color(1f, 1f, 1f, 0.85f);

        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            velo.color = new Color(1f, 1f, 1f, 0.85f * (1f - Mathf.Clamp01(t / duracion)));
            yield return null;
        }

        velo.color = new Color(1f, 1f, 1f, 0f);
    }
}
