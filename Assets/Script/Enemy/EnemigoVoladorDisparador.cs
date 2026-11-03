using System.Collections;
using UnityEngine;

/// <summary>
/// Enemigo volador que se mantiene a distancia del jugador y le dispara.
/// Antes de cada disparo parpadea en rojo para avisar. Puede apuntar directo al jugador,
/// disparar al azar (dispersado alrededor del jugador) o mezclar los dos.
/// </summary>
[RequireComponent(typeof(Salud))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemigoVoladorDisparador : MonoBehaviour
{
    public enum ModoDisparo { Directo, Azar, Mixto }

    [Header("Movimiento")]
    public float velocidad = 3.4f;
    [Tooltip("Distancia horizontal a la que intenta mantenerse del jugador.")]
    public float distanciaPreferida = 5.5f;
    public float alturaSobreJugador = 3.2f;
    public float balanceo = 0.45f;

    [Header("Disparo")]
    public ModoDisparo modo = ModoDisparo.Mixto;
    public GameObject balaPrefab;
    public float cadencia = 2.3f;
    [Tooltip("Segundos de aviso (parpadeo rojo) antes de disparar.")]
    public float aviso = 0.45f;
    public int proyectilesPorDisparo = 1;
    public float velocidadBala = 7.5f;
    public float vidaBala = 4f;
    public float danioBala = 5f;
    [Tooltip("En modo Azar, el disparo se desvía hasta estos grados respecto al jugador.")]
    public float dispersionAzar = 50f;
    [Tooltip("En modo Azar, probabilidad de disparar en una dirección totalmente aleatoria.")]
    [Range(0f, 1f)] public float probabilidadLoca = 0.2f;

    [Header("Aspecto")]
    public Sprite[] frames;
    public float fps = 8f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Transform objetivo;
    float proximoDisparo;
    float proximoRetarget;
    float lado;
    float fase;
    float tiempoAnim;
    int frameActual = -1;
    bool avisando;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        sr = GetComponentInChildren<SpriteRenderer>();
        lado = Random.value < 0.5f ? -1f : 1f;
        fase = Random.value * 6.28f;
    }

    void Start()
    {
        proximoDisparo = Time.time + Random.Range(1.0f, cadencia);
    }

    void FixedUpdate()
    {
        if (Time.time >= proximoRetarget)
        {
            proximoRetarget = Time.time + 0.4f;
            objetivo = Jugadores.MasCercano(transform.position);
        }

        if (objetivo == null) return;

        Vector2 pos = rb.position;
        Vector2 jugador = objetivo.position;

        // Se coloca a un lado y por encima del jugador; si se acerca demasiado, cambia de lado
        if (Mathf.Abs(pos.x - jugador.x) < distanciaPreferida * 0.4f && Random.value < 0.02f) lado = -lado;

        Vector2 destino = new Vector2(
            jugador.x + lado * distanciaPreferida,
            jugador.y + alturaSobreJugador + Mathf.Sin(Time.time * 1.6f + fase) * balanceo);

        destino = LimitarALaZona(destino);

        rb.MovePosition(Vector2.MoveTowards(pos, destino, velocidad * Time.fixedDeltaTime));
    }

    void Update()
    {
        Animar();

        if (objetivo == null) return;

        if (sr != null && !avisando) sr.flipX = objetivo.position.x < transform.position.x;

        if (!avisando && Time.time >= proximoDisparo)
        {
            proximoDisparo = Time.time + cadencia * Random.Range(0.85f, 1.25f);
            StartCoroutine(Disparar());
        }
    }

    /// <summary>Dentro de una zona de pelea no puede salirse de sus paredes.</summary>
    static Vector2 LimitarALaZona(Vector2 p)
    {
        ZonaCombate z = ZonaCombate.Actual;
        if (z == null) return p;

        Rect r = z.Limites;
        p.x = Mathf.Clamp(p.x, r.xMin + 1f, r.xMax - 1f);
        p.y = Mathf.Clamp(p.y, r.yMin + 1f, r.yMax - 0.5f);
        return p;
    }

    IEnumerator Disparar()
    {
        avisando = true;
        Color original = sr != null ? sr.color : Color.white;

        // Aviso: parpadeo rojo cada vez más rápido
        float t = 0f;
        while (t < aviso)
        {
            t += Time.deltaTime;
            if (sr != null)
            {
                float parpadeo = Mathf.PingPong(t * (8f + 20f * (t / aviso)), 1f);
                sr.color = Color.Lerp(original, new Color(1f, 0.25f, 0.25f, 1f), parpadeo);
            }
            yield return null;
        }

        if (sr != null) sr.color = original;
        avisando = false;

        if (balaPrefab == null || objetivo == null) yield break;

        for (int i = 0; i < Mathf.Max(1, proyectilesPorDisparo); i++)
        {
            Vector2 dir = ElegirDireccion();
            LanzarBala(dir);
            if (i < proyectilesPorDisparo - 1) yield return new WaitForSeconds(0.12f);
        }
    }

    Vector2 ElegirDireccion()
    {
        Vector2 origen = transform.position;
        Vector2 directo = ((Vector2)objetivo.position - origen).normalized;

        bool azar = modo == ModoDisparo.Azar || (modo == ModoDisparo.Mixto && Random.value < 0.5f);
        if (!azar) return directo;

        if (Random.value < probabilidadLoca)
            return Random.insideUnitCircle.normalized;

        float desvio = Random.Range(-dispersionAzar, dispersionAzar);
        float rad = desvio * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        return new Vector2(directo.x * cos - directo.y * sin, directo.x * sin + directo.y * cos);
    }

    void LanzarBala(Vector2 dir)
    {
        Vector3 origen = transform.position;
        GameObject g = Instantiate(balaPrefab, origen, Quaternion.identity);

        Bala b = g.GetComponent<Bala>();
        if (b != null)
        {
            b.equipo = Equipo.Enemigo;
            b.velocidad = velocidadBala;
            b.vida = vidaBala;
            b.danio = danioBala;
            b.Lanzar(dir);
        }

        EfectosJuego.Fogonazo(origen, dir, 0.6f);
        AudioJuego.SonidoDisparoJefe();
    }

    void Animar()
    {
        if (sr == null || frames == null || frames.Length == 0) return;

        tiempoAnim += Time.deltaTime;
        int indice = Mathf.FloorToInt(tiempoAnim * fps) % frames.Length;

        if (indice != frameActual)
        {
            frameActual = indice;
            sr.sprite = frames[indice];
        }
    }
}
