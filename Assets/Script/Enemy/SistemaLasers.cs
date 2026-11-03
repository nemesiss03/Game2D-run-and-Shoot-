using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fase 2 del jefe: 5 emisores láser repartidos por el borde de la pantalla.
/// Cada uno apunta al jugador mostrando una línea de aviso que se va cargando, se fija un instante antes
/// de disparar (para poder esquivar con el dash o saltando) y entonces dispara un rayo grueso.
/// Se crea solo: JefeFlotante llama a SistemaLasers.Activar() al empezar la caída libre.
/// </summary>
public class SistemaLasers : MonoBehaviour
{
    static SistemaLasers instancia;

    [Header("Ciclo de cada láser")]
    public float tiempoCarga = 1.7f;
    [Tooltip("Parte de la carga (0 a 1) durante la que el láser sigue al jugador. Después queda fijo.")]
    [Range(0.2f, 0.95f)] public float seguirHasta = 0.65f;
    public float duracionRayo = 0.4f;
    public float descansoMinimo = 2.4f;
    public float descansoMaximo = 4.2f;
    [Tooltip("Cuántos láseres pueden estar cargando o disparando a la vez.")]
    public int maxSimultaneos = 3;

    [Header("Daño")]
    public float danio = 14f;
    public float radioImpacto = 0.55f;

    readonly List<GameObject> piezas = new List<GameObject>();
    int activos;
    bool encendido;
    Sprite circulo;
    Material materialLinea;

    const float LargoRayo = 60f;

    // ------------------------------------------------------------ API estática
    public static void Activar()
    {
        if (instancia != null) return;

        GameObject go = new GameObject("SistemaLasers");
        instancia = go.AddComponent<SistemaLasers>();
        instancia.Iniciar();
    }

    public static void Detener()
    {
        if (instancia == null) return;

        instancia.encendido = false;
        Destroy(instancia.gameObject);
        instancia = null;
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    // ------------------------------------------------------------ montaje
    void Iniciar()
    {
        encendido = true;

        Camera cam = Camera.main;
        Vector2 c = cam != null ? (Vector2)cam.transform.position : Vector2.zero;
        float mitadAlto = cam != null ? cam.orthographicSize : 5f;
        float mitadAncho = cam != null ? mitadAlto * cam.aspect : 8f;

        materialLinea = new Material(Shader.Find("Sprites/Default"));
        circulo = CrearCirculo();

        // 5 posiciones repartidas por el borde de la pantalla
        Vector2[] posiciones =
        {
            new Vector2(c.x - mitadAncho + 0.8f, c.y + mitadAlto * 0.55f),
            new Vector2(c.x - mitadAncho + 0.8f, c.y - mitadAlto * 0.15f),
            new Vector2(c.x + mitadAncho - 0.8f, c.y + mitadAlto * 0.55f),
            new Vector2(c.x + mitadAncho - 0.8f, c.y - mitadAlto * 0.15f),
            new Vector2(c.x, c.y - mitadAlto + 0.8f)
        };

        for (int i = 0; i < posiciones.Length; i++)
            StartCoroutine(CicloEmisor(i, posiciones[i]));

        if (HUDJuego.Instancia != null) HUDJuego.Instancia.MostrarAviso("¡LÁSERES!", 2.5f);
    }

    // ------------------------------------------------------------ un emisor
    IEnumerator CicloEmisor(int indice, Vector2 posicion)
    {
        // Base del emisor y anillo que se llena al cargar
        GameObject baseGO = Pieza("EmisorLaser" + indice, posicion, circulo, new Color(0.15f, 0.05f, 0.12f, 1f), 0.9f, 20);
        SpriteRenderer anillo = Pieza("CargaLaser" + indice, posicion, circulo, new Color(1f, 0.25f, 0.2f, 0f), 0.2f, 21).GetComponent<SpriteRenderer>();

        LineRenderer linea = CrearLinea("LineaLaser" + indice);

        // Escalonar los arranques para que no disparen todos a la vez
        yield return new WaitForSeconds(0.8f + indice * 0.9f + Random.Range(0f, 0.8f));

        while (encendido)
        {
            Transform objetivo = Jugadores.MasCercano(posicion);
            if (objetivo == null) { yield return new WaitForSeconds(0.5f); continue; }

            while (activos >= maxSimultaneos) yield return null;
            activos++;

            // ---- Carga: la línea sigue al jugador y luego queda fija
            AudioJuego.SonidoInvocacion();
            Vector2 dir = Vector2.right;
            float t = 0f;

            while (t < tiempoCarga && encendido)
            {
                float p = t / tiempoCarga;

                if (p < seguirHasta && objetivo != null)
                {
                    Vector2 destino = CentroDe(objetivo);
                    dir = (destino - posicion).normalized;
                }

                bool fijado = p >= seguirHasta;
                float parpadeo = fijado ? (Mathf.Sin(t * 50f) > 0f ? 1f : 0.35f) : 1f;

                float ancho = Mathf.Lerp(0.03f, fijado ? 0.2f : 0.1f, p);
                Color col = fijado ? new Color(1f, 0.2f, 0.15f, 0.95f * parpadeo) : new Color(1f, 0.5f, 0.3f, Mathf.Lerp(0.25f, 0.8f, p));

                PintarLinea(linea, posicion, dir, ancho, col);

                anillo.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.1f, p);
                anillo.color = new Color(1f, 0.3f, 0.2f, Mathf.Lerp(0.2f, 0.9f, p));

                t += Time.deltaTime;
                yield return null;
            }

            if (!encendido) break;

            // ---- Disparo: rayo grueso con la dirección fijada
            AudioJuego.SonidoDisparoJefe();
            EfectosJuego.Fogonazo(posicion, dir, 1.4f);

            bool golpeado = false;
            float d = 0f;

            while (d < duracionRayo && encendido)
            {
                PintarLinea(linea, posicion, dir, 0.55f, new Color(1f, 0.95f, 0.85f, 1f));
                anillo.color = new Color(1f, 1f, 1f, 1f);

                if (!golpeado) golpeado = ComprobarImpacto(posicion, dir);

                d += Time.deltaTime;
                yield return null;
            }

            linea.enabled = false;
            anillo.color = new Color(1f, 0.25f, 0.2f, 0f);
            activos = Mathf.Max(0, activos - 1);

            yield return new WaitForSeconds(Random.Range(descansoMinimo, descansoMaximo));
        }

        if (baseGO != null) Destroy(baseGO);
    }

    bool ComprobarImpacto(Vector2 origen, Vector2 dir)
    {
        bool alguno = false;

        for (int i = 0; i < Jugadores.Todos.Count; i++)
        {
            PlayerController j = Jugadores.Todos[i];
            if (j == null || j.Muerto) continue;

            Salud s = j.GetComponent<Salud>();
            if (s == null) continue;

            Vector2 p = CentroDe(j.transform);
            float distancia = DistanciaASegmento(p, origen, origen + dir * LargoRayo);

            if (distancia <= radioImpacto + 0.4f)
            {
                s.RecibirDanio(danio);   // el dash da invulnerabilidad: esquivable
                alguno = true;
            }
        }

        return alguno;
    }

    // ------------------------------------------------------------ utilidades
    static Vector2 CentroDe(Transform t)
    {
        Collider2D c = t.GetComponent<Collider2D>();
        return c != null ? (Vector2)c.bounds.center : (Vector2)t.position;
    }

    static float DistanciaASegmento(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    GameObject Pieza(string nombre, Vector2 pos, Sprite sprite, Color color, float escala, int orden)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(transform, false);
        g.transform.position = new Vector3(pos.x, pos.y, 0f);
        g.transform.localScale = Vector3.one * escala;

        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingLayerName = "Player";
        sr.sortingOrder = orden;

        piezas.Add(g);
        return g;
    }

    LineRenderer CrearLinea(string nombre)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(transform, false);

        LineRenderer lr = g.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.material = materialLinea;
        lr.numCapVertices = 4;
        lr.sortingLayerName = "Player";
        lr.sortingOrder = 22;
        lr.enabled = false;

        return lr;
    }

    static void PintarLinea(LineRenderer lr, Vector2 origen, Vector2 dir, float ancho, Color color)
    {
        lr.enabled = true;
        lr.SetPosition(0, new Vector3(origen.x, origen.y, 0f));
        lr.SetPosition(1, new Vector3(origen.x + dir.x * LargoRayo, origen.y + dir.y * LargoRayo, 0f));
        lr.startWidth = ancho;
        lr.endWidth = ancho;
        lr.startColor = color;
        lr.endColor = color;
    }

    static Sprite CrearCirculo()
    {
        const int tam = 64;
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float r = tam * 0.5f;
        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * 6f)));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, tam, tam), new Vector2(0.5f, 0.5f), tam);
    }
}
