using UnityEngine;

/// <summary>
/// Ambiente que cae por toda la pantalla: hojas en el bosque y nieve en la montaña.
/// No usa assets ni físicas: son sprites pequeños dibujados por código que se reciclan alrededor de la cámara.
/// Una parte queda detrás del terreno (lejos) y otra por delante (cerca), para dar profundidad.
/// </summary>
public class AmbienteCayendo : MonoBehaviour
{
    public enum Tipo { Hojas, Nieve }

    public Tipo tipo = Tipo.Hojas;
    [Tooltip("Cuántas partículas hay a la vez en pantalla.")]
    public int cantidad = 50;
    public float velocidadMinima = 0.9f;
    public float velocidadMaxima = 2.2f;
    [Tooltip("Empuje horizontal constante (viento). Negativo = hacia la izquierda.")]
    public float viento = 0.7f;
    [Tooltip("Cuánto se mecen de lado a lado mientras caen.")]
    public float balanceo = 0.9f;
    [Range(0f, 1f)] public float proporcionDelante = 0.25f;

    class Particula
    {
        public Transform t;
        public SpriteRenderer sr;
        public float velocidad, fase, frecuencia, giro, escala;
        public bool delante;
    }

    Particula[] particulas;
    Sprite spriteHoja, spriteCopo;

    static readonly Color[] ColoresHoja =
    {
        new Color(0.45f, 0.75f, 0.30f), new Color(0.62f, 0.82f, 0.32f), new Color(0.82f, 0.78f, 0.28f),
        new Color(0.90f, 0.62f, 0.22f), new Color(0.80f, 0.40f, 0.20f), new Color(0.30f, 0.60f, 0.35f)
    };

    void Start()
    {
        spriteHoja = CrearHoja();
        spriteCopo = CrearCopo();

        particulas = new Particula[cantidad];
        for (int i = 0; i < cantidad; i++)
        {
            Particula p = new Particula();
            p.delante = Random.value < proporcionDelante;
            p.velocidad = Random.Range(velocidadMinima, velocidadMaxima) * (p.delante ? 1.25f : 1f);
            p.fase = Random.value * 6.28f;
            p.frecuencia = Random.Range(0.8f, 1.8f);
            p.giro = Random.Range(-90f, 90f);
            p.escala = (tipo == Tipo.Hojas ? Random.Range(0.8f, 1.35f) : Random.Range(0.7f, 1.5f)) * (p.delante ? 1.25f : 0.85f);

            GameObject g = new GameObject(tipo == Tipo.Hojas ? "Hoja" : "Copo");
            g.transform.SetParent(transform, false);
            g.transform.localScale = Vector3.one * p.escala;

            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = tipo == Tipo.Hojas ? spriteHoja : spriteCopo;
            sr.sortingLayerName = p.delante ? "Player" : "BackGround";
            sr.sortingOrder = p.delante ? 30 : 6;

            Color c = tipo == Tipo.Hojas ? ColoresHoja[Random.Range(0, ColoresHoja.Length)] : Color.white;
            c.a = p.delante ? 0.95f : 0.7f;
            sr.color = c;

            p.t = g.transform;
            p.sr = sr;
            particulas[i] = p;

            Colocar(p, true);
        }
    }

    void Update()
    {
        Camera cam = Camera.main;
        if (cam == null || particulas == null) return;

        float dt = Time.deltaTime;
        Vector3 c = cam.transform.position;
        float mitadAlto = cam.orthographicSize + 1.5f;
        float mitadAncho = cam.orthographicSize * cam.aspect + 2.5f;

        for (int i = 0; i < particulas.Length; i++)
        {
            Particula p = particulas[i];
            Vector3 pos = p.t.position;

            float vaiven = Mathf.Sin(Time.time * p.frecuencia + p.fase) * balanceo;
            pos.x += (viento + vaiven) * dt;
            pos.y -= p.velocidad * dt;
            p.t.position = pos;

            if (tipo == Tipo.Hojas)
                p.t.Rotate(0f, 0f, (p.giro + vaiven * 60f) * dt);

            // Fuera del recuadro de la cámara: vuelve a entrar por el lado contrario
            if (pos.y < c.y - mitadAlto) { pos.y = c.y + mitadAlto; pos.x = c.x + Random.Range(-mitadAncho, mitadAncho); p.t.position = pos; }
            else if (pos.x > c.x + mitadAncho) { pos.x -= mitadAncho * 2f; p.t.position = pos; }
            else if (pos.x < c.x - mitadAncho) { pos.x += mitadAncho * 2f; p.t.position = pos; }
            else if (pos.y > c.y + mitadAlto + 4f) { pos.y -= mitadAlto * 2f; p.t.position = pos; }   // la cámara subió de golpe
        }
    }

    void Colocar(Particula p, bool alAzarEnPantalla)
    {
        Camera cam = Camera.main;
        Vector3 c = cam != null ? cam.transform.position : Vector3.zero;
        float mitadAlto = (cam != null ? cam.orthographicSize : 5f) + 1.5f;
        float mitadAncho = (cam != null ? cam.orthographicSize * cam.aspect : 8f) + 2.5f;

        p.t.position = new Vector3(
            c.x + Random.Range(-mitadAncho, mitadAncho),
            alAzarEnPantalla ? c.y + Random.Range(-mitadAlto, mitadAlto) : c.y + mitadAlto,
            0f);

        p.t.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
    }

    // ------------------------------------------------------------ sprites dibujados por código
    static Sprite CrearHoja()
    {
        // 9 x 5 píxeles: óvalo con nervio central y punta
        const int w = 9, h = 5;
        Texture2D tex = Nueva(w, h);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (x - 4f) / 4.4f, ny = (y - 2f) / 2.2f;
                bool dentro = nx * nx + ny * ny <= 1f && !(x == 0 && y != 2);
                if (!dentro) continue;

                bool nervio = y == 2;
                tex.SetPixel(x, y, nervio ? new Color(0.78f, 0.78f, 0.78f, 1f) : Color.white);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 16f);
    }

    static Sprite CrearCopo()
    {
        // 4 x 4 píxeles: cuadrado con las esquinas quitadas
        const int n = 4;
        Texture2D tex = Nueva(n, n);

        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                bool esquina = (x == 0 || x == n - 1) && (y == 0 || y == n - 1);
                if (!esquina) tex.SetPixel(x, y, Color.white);
            }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 32f);
    }

    static Texture2D Nueva(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] vacio = new Color[w * h];
        tex.SetPixels(vacio);
        return tex;
    }
}
