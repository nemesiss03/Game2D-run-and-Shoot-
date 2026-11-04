using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de fuego del HUD (abajo a la derecha, bajo el contador): se gasta con cada bola de fuego,
/// deja una estela clara que se vacía despacio y, al enfriar, se rellena mostrando el progreso.
/// Se divide en una marca por carga: si cambias PlayerController.cargasMaximas se adapta sola.
/// La crea HUDJuego; no hace falta ponerla en la escena.
/// </summary>
public class BarraFuego : MonoBehaviour
{
    HUDJuego hud;
    RectTransform interior, fantasma, relleno;
    Image rellenoImg, marco;
    readonly List<GameObject> marcas = new List<GameObject>();
    int marcasPara = -1;
    float fraccionFantasma = 1f;

    static readonly Color Lleno = new Color(1f, 0.78f, 0.25f);
    static readonly Color Bajo = new Color(1f, 0.18f, 0.12f);
    static readonly Color Enfriando = new Color(0.45f, 0.85f, 1f);

    public static void Crear(HUDJuego hud)
    {
        if (hud.textoMunicion == null) return;

        GameObject g = new GameObject("BarraFuego", typeof(RectTransform));
        g.transform.SetParent(hud.textoMunicion.transform.parent, false);
        BarraFuego b = g.AddComponent<BarraFuego>();
        b.hud = hud;
        b.Construir();
    }

    void Construir()
    {
        RectTransform rt = (RectTransform)transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-32f, 34f);
        rt.sizeDelta = new Vector2(360f, 26f);

        marco = Panel(rt, "Marco", new Color(0f, 0f, 0f, 0.75f));
        Estirar(marco.rectTransform, 0f);

        interior = Hijo(rt, "Interior");
        Estirar(interior, 3f);
        Image fondo = interior.gameObject.AddComponent<Image>();
        fondo.color = new Color(0.12f, 0.06f, 0.05f, 0.9f);
        fondo.raycastTarget = false;

        fantasma = Panel(interior, "Estela", new Color(1f, 0.95f, 0.8f, 0.55f)).rectTransform;
        Estirar(fantasma, 0f);

        rellenoImg = Panel(interior, "Relleno", Lleno);
        relleno = rellenoImg.rectTransform;
        Estirar(relleno, 0f);
    }

    void Update()
    {
        PlayerController j = hud != null ? hud.jugador : null;
        if (j == null) return;

        int max = Mathf.Max(1, j.CapacidadCargador);
        if (max != marcasPara) ReconstruirMarcas(max);

        bool enfriando = j.Recargando;
        float f = enfriando ? j.ProgresoRecarga : j.FraccionCargas;

        // La estela clara baja despacio detrás de la barra (y sube con ella al rellenar)
        if (f >= fraccionFantasma) fraccionFantasma = f;
        else fraccionFantasma = Mathf.MoveTowards(fraccionFantasma, f, Time.deltaTime * 0.9f);

        SetFraccion(relleno, f);
        SetFraccion(fantasma, enfriando ? 0f : fraccionFantasma);

        Color c;
        if (enfriando)
            c = Color.Lerp(new Color(1f, 0.45f, 0.2f), Enfriando, f);
        else if (j.FraccionCargas <= j.umbralAvisoFuego)
            c = Color.Lerp(Bajo, Lleno, j.FraccionCargas / Mathf.Max(0.01f, j.umbralAvisoFuego) * 0.5f);
        else
            c = Color.Lerp(Bajo, Lleno, Mathf.InverseLerp(j.umbralAvisoFuego, 0.7f, j.FraccionCargas));

        rellenoImg.color = c;

        // El marco late en rojo cuando casi no hay fuego o se está enfriando
        bool alerta = enfriando || j.FraccionCargas <= j.umbralAvisoFuego;
        marco.color = alerta
            ? Color.Lerp(new Color(0f, 0f, 0f, 0.75f), new Color(0.9f, 0.1f, 0.1f, 0.95f), 0.5f + 0.5f * Mathf.Sin(Time.time * 9f))
            : new Color(0f, 0f, 0f, 0.75f);
    }

    static void SetFraccion(RectTransform rt, float f)
    {
        rt.anchorMax = new Vector2(Mathf.Clamp01(f), 1f);
    }

    void ReconstruirMarcas(int max)
    {
        marcasPara = max;
        foreach (GameObject g in marcas) if (g != null) Destroy(g);
        marcas.Clear();

        for (int i = 1; i < max; i++)
        {
            Image m = Panel(interior, "Marca" + i, new Color(0f, 0f, 0f, 0.55f));
            RectTransform rt = m.rectTransform;
            float x = (float)i / max;
            rt.anchorMin = new Vector2(x, 0f);
            rt.anchorMax = new Vector2(x, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(2f, 0f);
            rt.anchoredPosition = Vector2.zero;
            marcas.Add(m.gameObject);
        }
    }

    // ------------------------------------------------------------ utilidades UI
    static RectTransform Hijo(Transform padre, string nombre)
    {
        GameObject g = new GameObject(nombre, typeof(RectTransform));
        g.transform.SetParent(padre, false);
        return (RectTransform)g.transform;
    }

    static Image Panel(Transform padre, string nombre, Color color)
    {
        RectTransform rt = Hijo(padre, nombre);
        Image im = rt.gameObject.AddComponent<Image>();
        im.color = color;
        im.raycastTarget = false;
        return im;
    }

    static void Estirar(RectTransform rt, float margen)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
    }
}
