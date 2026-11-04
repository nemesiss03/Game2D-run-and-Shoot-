using UnityEngine;

/// <summary>
/// Ilusión de caída infinita. El dragón se queda siempre a la misma altura (ver
/// PlayerController) y lo que se mueve son estas nubes de aire, que suben a toda
/// velocidad y se reciclan. Así el jefe y los esbirros nunca se quedan atrás.
/// </summary>
public class CaidaInfinita : MonoBehaviour
{
    public static CaidaInfinita Instancia { get; private set; }

    [Header("Aspecto")]
    public int cantidadNubes = 30;
    public float ancho = 26f;
    public float alto = 18f;
    public float velocidad = 18f;
    public Color colorNube = new Color(0.60f, 0.73f, 0.93f, 0.42f);
    public int ordenDibujo = -20;

    [Header("Balanceo")]
    public float balanceo = 0.35f;

    /// <summary>Plano en el que viven las nubes: detrás del dragón (z=0) y delante del cielo (z=5).</summary>
    const float PROFUNDIDAD = 3f;

    bool activa;
    Transform raiz;
    Transform[] nubes;
    float[] velocidades;
    float[] fases;
    Sprite spriteNube;

    public static bool EstaActiva
    {
        get { return Instancia != null && Instancia.activa; }
    }

    /// <summary>Arranca la caída (lo llama el jugador cuando la plataforma se rompe).</summary>
    public static void ActivarGlobal()
    {
        CaidaInfinita c = Instancia;
        if (c == null)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            c = cam.GetComponent<CaidaInfinita>();
            if (c == null) c = cam.gameObject.AddComponent<CaidaInfinita>();
        }

        c.Activar();
    }

    public static void DetenerGlobal()
    {
        if (Instancia != null) Instancia.Detener();
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(this);
            return;
        }

        Instancia = this;
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    public void Activar()
    {
        if (activa) return;

        CrearNubes();
        activa = true;
        PresionCaida.Crear(gameObject);
    }

    public void Detener()
    {
        activa = false;

        if (raiz != null) raiz.gameObject.SetActive(false);
    }

    void CrearNubes()
    {
        if (raiz != null)
        {
            raiz.gameObject.SetActive(true);
            return;
        }

        if (spriteNube == null) spriteNube = CrearSpriteNube();

        GameObject goRaiz = new GameObject("CaidaInfinita");
        raiz = goRaiz.transform;
        raiz.SetParent(transform, false);
        raiz.localPosition = Vector3.zero;

        int total = Mathf.Max(1, cantidadNubes);
        nubes = new Transform[total];
        velocidades = new float[total];
        fases = new float[total];

        for (int i = 0; i < total; i++)
        {
            GameObject g = new GameObject("Nube" + i);
            g.transform.SetParent(raiz, false);
            g.transform.localPosition = Vector3.zero;

            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = spriteNube;
            sr.color = colorNube;
            sr.sortingOrder = ordenDibujo;

            nubes[i] = g.transform;
            velocidades[i] = Random.Range(0.55f, 1.5f);
            fases[i] = Random.Range(0f, 6.283f);
        }

        ReposicionarTodas();
    }

    void ReposicionarTodas()
    {
        Transform cam = Camera.main != null ? Camera.main.transform : transform;
        Vector3 centro = cam.position;

        for (int i = 0; i < nubes.Length; i++)
        {
            if (nubes[i] == null) continue;

            nubes[i].position = new Vector3(
                centro.x + Random.Range(-ancho * 0.5f, ancho * 0.5f),
                centro.y + Random.Range(-alto * 0.5f, alto * 0.5f),
                PROFUNDIDAD);

            float tam = Random.Range(0.55f, 1.6f);
            nubes[i].localScale = new Vector3(tam * Random.Range(1.8f, 3.4f), tam * 0.8f, 1f);
        }
    }

    void Update()
    {
        if (!activa) return;
        if (nubes == null || nubes.Length == 0) return;

        Transform cam = Camera.main != null ? Camera.main.transform : transform;
        Vector3 centro = cam.position;

        float limiteArriba = centro.y + alto * 0.5f;
        float limiteAbajo = centro.y - alto * 0.5f;

        for (int i = 0; i < nubes.Length; i++)
        {
            Transform n = nubes[i];
            if (n == null) continue;

            Vector3 p = n.position;
            p.y += velocidad * velocidades[i] * Time.deltaTime;

            float desvio = Mathf.Sin(Time.time * 1.7f + fases[i]) * balanceo * Time.deltaTime;
            p.x += desvio;

            if (p.y > limiteArriba)
            {
                p.y = limiteAbajo - Random.Range(0f, 2f);
                p.x = centro.x + Random.Range(-ancho * 0.5f, ancho * 0.5f);

                float tam = Random.Range(0.55f, 1.6f);
                n.localScale = new Vector3(tam * Random.Range(1.8f, 3.4f), tam * 0.8f, 1f);
            }

            p.z = PROFUNDIDAD;
            n.position = p;
        }
    }

    /// <summary>Nube suave generada por código: no hace falta ningún asset.</summary>
    static Sprite CrearSpriteNube()
    {
        int w = 96;
        int h = 48;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.name = "NubeCaida";

        Color[] px = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = ((x + 0.5f) / w) * 2f - 1f;
                float ny = ((y + 0.5f) / h) * 2f - 1f;

                float d = Mathf.Sqrt(nx * nx + ny * ny * 2.4f);
                float a = Mathf.Clamp01(1f - d);
                a = a * a * a;

                px[y * w + x] = new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 32f);
    }
}
