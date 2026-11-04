using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Onda que recorre el suelo tras un mazazo de la fase 2. Hay que saltarla o subirse a una plataforma.
/// Hace mucho daño; el dash la atraviesa.
/// </summary>
public class OndaChoque : MonoBehaviour
{
    const float Ancho = 1.1f;
    const float Alto = 1.5f;

    static Sprite spriteOnda;

    float velocidad, danio, limiteX, direccion;
    readonly HashSet<Salud> golpeados = new HashSet<Salud>();

    /// <summary>Crea una onda en el suelo (suelo = altura de la superficie) que viaja en la dirección dada.</summary>
    public static void Crear(float x, float suelo, int direccion, float velocidad, float danio, float limiteX)
    {
        GameObject g = new GameObject("OndaChoque");
        g.transform.position = new Vector3(x, suelo + Alto * 0.5f, 0f);

        if (spriteOnda == null) spriteOnda = CrearSprite();

        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = spriteOnda;
        sr.color = new Color(1f, 0.62f, 0.25f, 0.95f);
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 12;
        g.transform.localScale = new Vector3(direccion, 1f, 1f);

        OndaChoque o = g.AddComponent<OndaChoque>();
        o.velocidad = velocidad;
        o.danio = danio;
        o.limiteX = limiteX;
        o.direccion = direccion;
    }

    void Update()
    {
        transform.position += new Vector3(direccion * velocidad * Time.deltaTime, 0f, 0f);

        // Late un poco mientras avanza
        float k = 1f + 0.12f * Mathf.Sin(Time.time * 30f);
        transform.localScale = new Vector3(direccion * k, 1f + 0.08f * Mathf.Sin(Time.time * 22f), 1f);

        Rect r = new Rect(transform.position.x - Ancho * 0.5f, transform.position.y - Alto * 0.5f, Ancho, Alto);

        for (int i = 0; i < Jugadores.Todos.Count; i++)
        {
            PlayerController j = Jugadores.Todos[i];
            if (j == null || j.Muerto) continue;

            Salud s = j.GetComponent<Salud>();
            if (s == null || golpeados.Contains(s)) continue;

            Collider2D c = j.GetComponent<Collider2D>();
            if (c == null) continue;

            Bounds b = c.bounds;
            bool toca = b.min.x < r.xMax && b.max.x > r.xMin && b.min.y < r.yMax && b.max.y > r.yMin;
            if (!toca) continue;

            golpeados.Add(s);
            s.RecibirDanio(danio);
        }

        if ((direccion > 0 && transform.position.x > limiteX) || (direccion < 0 && transform.position.x < limiteX))
            Destroy(gameObject);
    }

    /// <summary>Cresta de energía: ancha abajo, acabada en punta arriba.</summary>
    static Sprite CrearSprite()
    {
        const int w = 48, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;   // 0 abajo, 1 arriba
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w * 2f - 1f;   // -1..1
                // El frente (derecha) es la punta; la cola se afina hacia atrás
                float limite = Mathf.Lerp(1f, 0.12f, v);
                float cola = Mathf.Clamp01(1f - (-u) * 0.9f);
                float a = Mathf.Clamp01((limite - Mathf.Abs(u)) * 5f) * cola * Mathf.Clamp01((1f - v) * 3f + 0.2f);
                float brillo = Mathf.Lerp(0.6f, 1f, v);
                tex.SetPixel(x, y, new Color(brillo, brillo, brillo, a));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 43f);
    }
}
