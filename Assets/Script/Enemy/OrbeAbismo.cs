using UnityEngine;

/// <summary>
/// Orbe morado del Señor del Abismo (el mismo de la cinemática inicial) usado como ataque de la fase 2:
/// crece junto al jefe, se lanza contra el jugador y le persigue un poco. Se puede destruir a disparos.
/// </summary>
[RequireComponent(typeof(Salud))]
public class OrbeAbismo : MonoBehaviour
{
    public Transform origen;
    public Vector3 desplazamiento = new Vector3(0f, -1.1f, 0f);
    public Transform objetivo;
    public float danio = 22f;
    public float velocidad = 7f;
    public float tiempoCarga = 1.1f;
    public float escalaFinal = 2f;
    public float vidaTotal = 7f;
    [Tooltip("Cuánto gira hacia el jugador mientras vuela (grados por segundo).")]
    public float giro = 55f;
    public float tiempoGiro = 1.6f;

    static Sprite spriteOrbe;

    Rigidbody2D rb;
    Collider2D col;
    float t;
    bool lanzado;
    Vector2 dir;
    float inicioVuelo;
    bool golpeo;

    /// <summary>Crea el orbe pegado al jefe; se lanza solo cuando termina de cargar.</summary>
    public static OrbeAbismo Crear(Transform origen, Transform objetivo, float danio, float velocidad)
    {
        GameObject g = new GameObject("OrbeDelAbismo");
        g.transform.position = origen.position + new Vector3(0f, -1.1f, 0f);
        g.transform.localScale = Vector3.one * 0.2f;

        if (spriteOrbe == null) spriteOrbe = CrearSprite();

        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = spriteOrbe;
        sr.color = new Color(0.45f, 0.12f, 0.75f, 1f);
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 13;

        GameObject halo = new GameObject("Halo");
        halo.transform.SetParent(g.transform, false);
        halo.transform.localScale = Vector3.one * 1.5f;
        SpriteRenderer sh = halo.AddComponent<SpriteRenderer>();
        sh.sprite = spriteOrbe;
        sh.color = new Color(0.75f, 0.35f, 1f, 0.35f);
        sh.sortingLayerName = "Player";
        sh.sortingOrder = 12;

        Salud s = g.AddComponent<Salud>();
        s.equipo = Equipo.Enemigo;
        s.vidaMaxima = 4f;
        s.puntosAlMorir = 25;

        Rigidbody2D rb = g.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        CircleCollider2D c = g.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = 0.42f;
        c.enabled = false;   // mientras carga no daña ni recibe disparos

        OrbeAbismo o = g.AddComponent<OrbeAbismo>();
        o.origen = origen;
        o.objetivo = objetivo;
        o.danio = danio;
        o.velocidad = velocidad;
        return o;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        t += Time.deltaTime;

        if (!lanzado)
        {
            float k = Mathf.Clamp01(t / tiempoCarga);
            transform.localScale = Vector3.one * Mathf.Lerp(0.2f, escalaFinal, k);

            if (origen != null)
                transform.position = origen.position + desplazamiento + (Vector3)(Random.insideUnitCircle * 0.04f * k);

            if (t >= tiempoCarga) Lanzar();
            return;
        }

        transform.Rotate(0f, 0f, 240f * Time.deltaTime);
        if (Time.time - inicioVuelo > vidaTotal) Destruir();
    }

    void FixedUpdate()
    {
        if (!lanzado) return;

        if (objetivo != null && Time.time - inicioVuelo < tiempoGiro)
        {
            Vector2 deseo = ((Vector2)objetivo.position - rb.position).normalized;
            float ang = Vector2.SignedAngle(dir, deseo);
            float paso = Mathf.Clamp(ang, -giro * Time.fixedDeltaTime, giro * Time.fixedDeltaTime);
            dir = Quaternion.Euler(0f, 0f, paso) * dir;
        }

        rb.linearVelocity = dir * velocidad;
    }

    void Lanzar()
    {
        lanzado = true;
        inicioVuelo = Time.time;
        col.enabled = true;

        Vector2 haciaJugador = objetivo != null ? (Vector2)objetivo.position - (Vector2)transform.position : Vector2.down;
        dir = haciaJugador.sqrMagnitude < 0.0001f ? Vector2.down : haciaJugador.normalized;
        AudioJuego.SonidoDisparoJefe();
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!lanzado || golpeo) return;

        Salud s = otro.GetComponentInParent<Salud>();
        if (s == null || s.equipo != Equipo.Jugador) return;

        PlayerController pc = otro.GetComponentInParent<PlayerController>();
        if (pc != null && pc.controladoPorRed) return;

        golpeo = true;
        s.RecibirDanio(danio);   // con el dash no hace nada: es esquivable
        Destruir();
    }

    void Destruir()
    {
        EfectosJuego.ExplosionJefe(transform.position, 1.6f);
        AudioJuego.SonidoExplosionEsbirro();
        Destroy(gameObject);
    }

    static Sprite CrearSprite()
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
                float a = Mathf.Clamp01((1f - d) * 3f);
                float brillo = Mathf.Lerp(1f, 0.25f, d);
                tex.SetPixel(x, y, new Color(brillo, brillo, brillo, a));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, tam, tam), new Vector2(0.5f, 0.5f), tam / 2f);
    }
}
