using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sensación de presión de la caída libre: rayas de viento que suben a toda velocidad, ráfagas de aire que
/// pegan al dragón y al jefe, viñeta azul que late en los bordes y ráfagas que sacuden la cámara.
/// Lo crea CaidaInfinita al empezar la caída; no hace falta ponerlo en la escena.
/// </summary>
public class PresionCaida : MonoBehaviour
{
    /// <summary>Multiplicador del temblor de cámara (1 = normal, sube en las ráfagas).</summary>
    public static float Intensidad = 1f;

    const int RayasPantalla = 34;
    const int RayasPorPersonaje = 14;

    class Raya
    {
        public Transform t;
        public SpriteRenderer sr;
        public float velocidad, vida, edad, alfa;
        public Transform dueno;
    }

    Raya[] pantalla;
    Raya[] cuerpo;
    Sprite spriteRaya;
    Image vineta;
    Canvas canvasVineta;   // se guarda aparte: Image.canvas devuelve null si el Canvas está desactivado
    float proximaRafaga, finRafaga, arranque;
    float proximoCuerpo;

    public static void Crear(GameObject sobre)
    {
        if (sobre.GetComponent<PresionCaida>() == null) sobre.AddComponent<PresionCaida>();
    }

    void Start()
    {
        arranque = Time.time;
        spriteRaya = CrearSpriteRaya();

        pantalla = new Raya[RayasPantalla];
        for (int i = 0; i < pantalla.Length; i++)
        {
            pantalla[i] = NuevaRaya("RayaViento", i % 3 != 0);
            ColocarEnPantalla(pantalla[i], true);
        }

        cuerpo = new Raya[RayasPorPersonaje * 2];
        for (int i = 0; i < cuerpo.Length; i++)
        {
            cuerpo[i] = NuevaRaya("RayaCuerpo", true);
            cuerpo[i].sr.enabled = false;
            cuerpo[i].edad = 99f;
        }

        CrearVineta();
        proximaRafaga = Time.time + 1.2f;
    }

    void OnDestroy()
    {
        Intensidad = 1f;
        if (canvasVineta != null) Destroy(canvasVineta.gameObject);
    }

    void OnDisable()
    {
        Intensidad = 1f;
    }

    void Update()
    {
        if (!CaidaInfinita.EstaActiva)
        {
            Intensidad = 1f;
            if (canvasVineta != null) canvasVineta.enabled = false;
            Ocultar(pantalla);
            Ocultar(cuerpo);
            return;
        }

        if (canvasVineta != null) canvasVineta.enabled = true;

        float subida = Mathf.Clamp01((Time.time - arranque) / 1.5f);

        // Ráfagas: cada pocos segundos el aire aprieta más durante un instante
        if (Time.time >= proximaRafaga)
        {
            finRafaga = Time.time + Random.Range(0.35f, 0.6f);
            proximaRafaga = Time.time + Random.Range(2.2f, 4.2f);
        }

        float rafaga = Time.time < finRafaga ? 1f : 0f;
        Intensidad = Mathf.Lerp(Intensidad, (1f + rafaga * 1.6f) * (1f + subida), 10f * Time.deltaTime);

        ActualizarPantalla(subida, rafaga);
        ActualizarCuerpos(rafaga);
        ActualizarVineta(subida, rafaga);
    }

    // ------------------------------------------------------------ rayas por toda la pantalla
    void ActualizarPantalla(float subida, float rafaga)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float mitadAlto = cam.orthographicSize + 2f;
        float mitadAncho = cam.orthographicSize * cam.aspect + 1f;
        Vector3 c = cam.transform.position;

        for (int i = 0; i < pantalla.Length; i++)
        {
            Raya r = pantalla[i];
            r.sr.enabled = true;

            Vector3 p = r.t.position;
            p.y += r.velocidad * (1f + rafaga * 0.5f) * subida * Time.deltaTime;
            r.t.position = p;

            if (p.y > c.y + mitadAlto) ColocarEnPantalla(r, false);
            else if (Mathf.Abs(p.x - c.x) > mitadAncho + 2f) ColocarEnPantalla(r, true);

            Color col = new Color(0.33f, 0.52f, 0.9f, Mathf.Clamp01(r.alfa * subida * (1f + rafaga * 0.5f)));
            r.sr.color = col;
        }
    }

    void ColocarEnPantalla(Raya r, bool alAzarEnY)
    {
        Camera cam = Camera.main;
        Vector3 c = cam != null ? cam.transform.position : Vector3.zero;
        float alto = (cam != null ? cam.orthographicSize : 5f) + 2f;
        float ancho = (cam != null ? cam.orthographicSize * cam.aspect : 9f) + 1f;

        r.t.position = new Vector3(
            c.x + Random.Range(-ancho, ancho),
            alJefe(alAzarEnY, c.y, alto),
            0f);

        float largo = Random.Range(1.6f, 4.2f);
        r.t.localScale = new Vector3(Random.Range(0.9f, 2.0f), largo, 1f);
        r.velocidad = Random.Range(26f, 46f);
        r.alfa = Random.Range(0.30f, 0.65f);
    }

    static float alJefe(bool alAzar, float cy, float alto)
    {
        return alAzar ? cy + Random.Range(-alto, alto) : cy - alto - Random.Range(0f, 3f);
    }

    // ------------------------------------------------------------ aire pegado a los personajes
    void ActualizarCuerpos(float rafaga)
    {
        if (Time.time >= proximoCuerpo)
        {
            proximoCuerpo = Time.time + 0.03f / (1f + rafaga);

            for (int k = 0; k < 2; k++)
            {
                Transform dueno = null;
                if (k == 0 && Jugadores.Todos.Count > 0) dueno = Jugadores.Todos[Random.Range(0, Jugadores.Todos.Count)].transform;
                if (k == 1 && JefeFlotante.Activo != null) dueno = JefeFlotante.Activo.transform;
                if (dueno == null) continue;

                Raya libre = null;
                for (int i = 0; i < cuerpo.Length; i++)
                    if (cuerpo[i].edad >= cuerpo[i].vida) { libre = cuerpo[i]; break; }
                if (libre == null) continue;

                float ancho = k == 1 ? 1.6f : 0.7f;
                libre.dueno = dueno;
                libre.edad = 0f;
                libre.vida = Random.Range(0.25f, 0.45f);
                libre.velocidad = Random.Range(16f, 26f);
                libre.t.position = dueno.position + new Vector3(Random.Range(-ancho, ancho), Random.Range(-0.6f, 0.4f), 0f);
                libre.t.localScale = new Vector3(0.7f, Random.Range(0.9f, 1.8f), 1f);
                libre.sr.enabled = true;
            }
        }

        for (int i = 0; i < cuerpo.Length; i++)
        {
            Raya r = cuerpo[i];
            if (r.edad >= r.vida) { r.sr.enabled = false; continue; }

            r.edad += Time.deltaTime;
            Vector3 p = r.t.position;
            p.y += r.velocidad * Time.deltaTime;
            r.t.position = p;

            Color col = new Color(0.3f, 0.48f, 0.88f, (1f - r.edad / r.vida) * 0.85f);
            r.sr.color = col;
        }
    }

    // ------------------------------------------------------------ viñeta que late
    void ActualizarVineta(float subida, float rafaga)
    {
        if (vineta == null) return;

        float latido = 0.5f + 0.5f * Mathf.Sin(Time.time * 7f);
        float a = Mathf.Lerp(0.45f, 0.7f, latido) + rafaga * 0.25f;
        vineta.color = new Color(0.12f, 0.27f, 0.7f, Mathf.Clamp01(a) * subida);
    }

    void CrearVineta()
    {
        GameObject go = new GameObject("VinetaPresion");
        Canvas cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = -5;   // por debajo del HUD
        canvasVineta = cv;

        GameObject im = new GameObject("Vineta");
        im.transform.SetParent(go.transform, false);
        vineta = im.AddComponent<Image>();
        vineta.raycastTarget = false;
        vineta.sprite = CrearSpriteVineta();

        RectTransform rt = vineta.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        vineta.color = new Color(0.25f, 0.45f, 0.85f, 0f);
    }

    // ------------------------------------------------------------ utilidades
    Raya NuevaRaya(string nombre, bool delante)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(transform, false);

        Raya r = new Raya();
        r.t = g.transform;
        r.sr = g.AddComponent<SpriteRenderer>();
        r.sr.sprite = spriteRaya;
        r.sr.color = new Color(0.35f, 0.55f, 0.92f, 0.3f);
        r.sr.sortingLayerName = "Player";
        r.sr.sortingOrder = delante ? 28 : -15;
        r.vida = 1f;
        return r;
    }

    static void Ocultar(Raya[] rs)
    {
        if (rs == null) return;
        for (int i = 0; i < rs.Length; i++) if (rs[i].sr != null) rs[i].sr.enabled = false;
    }

    static Sprite CrearSpriteRaya()
    {
        const int w = 4, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;
            float punta = Mathf.Sin(v * Mathf.PI);   // se desvanece por los dos extremos
            for (int x = 0; x < w; x++)
            {
                float u = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, punta * (1f - u * 0.6f)));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 64f);
    }

    static Sprite CrearSpriteVineta()
    {
        const int n = 128;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f;
                float dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.2f;
                float a = Mathf.Clamp01((d - 0.35f) / 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}
