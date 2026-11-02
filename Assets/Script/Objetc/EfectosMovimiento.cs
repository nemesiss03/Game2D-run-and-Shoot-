using UnityEngine;

/// <summary>
/// Efectos del movimiento del dragón: anillo del doble salto, estela y polvo del dash.
/// Todo se dibuja por código (sin assets) y se destruye solo.
/// </summary>
public static class EfectosMovimiento
{
    static Sprite spriteAnillo;
    static Sprite spriteBola;

    static readonly Color ColorSalto = new Color(0.65f, 0.95f, 1f, 0.9f);
    static readonly Color ColorDash = new Color(0.55f, 0.85f, 1f, 0.55f);

    /// <summary>Onda que se expande bajo los pies al hacer el doble salto.</summary>
    public static void AnilloSalto(Vector3 posicion)
    {
        Temporal.Crear(posicion, Anillo(), ColorSalto, 0.25f, 1.9f, 0.35f, Vector2.zero);

        // Chispitas que salen hacia los lados y abajo
        for (int i = 0; i < 6; i++)
        {
            Vector2 dir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, -0.1f)).normalized;
            Temporal.Crear(posicion, Bola(), ColorSalto, 0.16f, 0.04f, 0.35f, dir * Random.Range(2f, 4.5f));
        }
    }

    /// <summary>Polvo al arrancar el dash, hacia atrás.</summary>
    public static void PolvoDash(Vector3 posicion, float direccion)
    {
        for (int i = 0; i < 7; i++)
        {
            Vector2 v = new Vector2(-direccion * Random.Range(2f, 6f), Random.Range(-0.5f, 1.8f));
            float tam = Random.Range(0.25f, 0.5f);
            Temporal.Crear(posicion + new Vector3(0f, Random.Range(-0.5f, 0.3f), 0f), Bola(),
                new Color(0.9f, 0.95f, 1f, 0.6f), tam, tam * 1.8f, 0.4f, v);
        }
    }

    /// <summary>Copia desvanecida del dragón (todas sus piezas) para dejar estela mientras hace el dash.</summary>
    public static void Fantasma(GameObject jugador)
    {
        SpriteRenderer[] piezas = jugador.GetComponentsInChildren<SpriteRenderer>();

        for (int i = 0; i < piezas.Length; i++)
        {
            SpriteRenderer origen = piezas[i];
            if (origen == null || origen.sprite == null || !origen.enabled) continue;

            GameObject g = new GameObject("FantasmaDash");
            g.transform.position = origen.transform.position;
            g.transform.rotation = origen.transform.rotation;
            g.transform.localScale = origen.transform.lossyScale;

            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = origen.sprite;
            sr.flipX = origen.flipX;
            sr.flipY = origen.flipY;
            sr.sortingLayerID = origen.sortingLayerID;
            sr.sortingOrder = origen.sortingOrder - 1;

            Temporal t = g.AddComponent<Temporal>();
            t.Configurar(sr, ColorDash, 0f, 0f, 0.28f, Vector2.zero);
        }
    }

    static Sprite Anillo()
    {
        if (spriteAnillo != null) return spriteAnillo;
        spriteAnillo = Generar(64, 0.82f, 1f);
        return spriteAnillo;
    }

    static Sprite Bola()
    {
        if (spriteBola != null) return spriteBola;
        spriteBola = Generar(32, 0f, 1f);
        return spriteBola;
    }

    /// <summary>Círculo (o aro si interior &gt; 0) con borde suave, de 1 unidad de diámetro.</summary>
    static Sprite Generar(int tam, float interior, float exterior)
    {
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float r = tam * 0.5f;

        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01((exterior - d) * 8f);
                if (interior > 0f) a *= Mathf.Clamp01((d - interior) * 8f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, tam, tam), new Vector2(0.5f, 0.5f), tam);
    }

    /// <summary>Sprite temporal que crece o se encoge, se mueve y se desvanece.</summary>
    public class Temporal : MonoBehaviour
    {
        SpriteRenderer sr;
        Color color;
        float escalaInicial;
        float escalaFinal;
        float duracion;
        Vector2 velocidad;
        float edad;

        public static void Crear(Vector3 pos, Sprite sprite, Color color, float escalaIni, float escalaFin, float duracion, Vector2 velocidad)
        {
            GameObject g = new GameObject("EfectoMovimiento");
            g.transform.position = pos;

            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 8;

            Temporal t = g.AddComponent<Temporal>();
            t.Configurar(sr, color, escalaIni, escalaFin, duracion, velocidad);
        }

        public void Configurar(SpriteRenderer renderer, Color c, float escalaIni, float escalaFin, float dur, Vector2 vel)
        {
            sr = renderer;
            color = c;
            escalaInicial = escalaIni;
            escalaFinal = escalaFin;
            duracion = Mathf.Max(0.01f, dur);
            velocidad = vel;
            sr.color = c;
        }

        void Update()
        {
            edad += Time.deltaTime;
            float t = Mathf.Clamp01(edad / duracion);

            if (escalaInicial > 0f || escalaFinal > 0f)
                transform.localScale = Vector3.one * Mathf.Lerp(escalaInicial, escalaFinal, t);

            transform.position += (Vector3)(velocidad * Time.deltaTime);
            velocidad *= 1f - 3f * Time.deltaTime;

            Color c = color;
            c.a = color.a * (1f - t);
            sr.color = c;

            if (t >= 1f) Destroy(gameObject);
        }
    }
}
