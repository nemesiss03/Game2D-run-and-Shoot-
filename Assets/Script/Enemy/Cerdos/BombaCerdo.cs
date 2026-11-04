using UnityEngine;

/// <summary>
/// Bomba lanzada en arco por un cerdo. Explota al acabar la mecha o al tocar al jugador y daña en un radio.
/// El dash (invulnerabilidad) la esquiva.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(AnimadorCuadros))]
public class BombaCerdo : MonoBehaviour
{
    public Sprite[] mecha;       // Bomb On
    public Sprite[] explosion;   // Boooooom
    public float tiempoMecha = 1.7f;
    public float radio = 1.7f;
    public float danio = 18f;

    Rigidbody2D rb;
    AnimadorCuadros anim;
    float inicio;
    bool explotando;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<AnimadorCuadros>();
    }

    /// <summary>Calcula la velocidad para llegar al destino en "tiempo" segundos describiendo un arco.</summary>
    public void Lanzar(Vector3 destino, float tiempo)
    {
        inicio = Time.time;
        tiempo = Mathf.Max(0.4f, tiempo);

        Vector2 d = destino - transform.position;
        float g = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        float vx = d.x / tiempo;
        float vy = (d.y + 0.5f * g * tiempo * tiempo) / tiempo;

        rb.linearVelocity = new Vector2(Mathf.Clamp(vx, -14f, 14f), Mathf.Clamp(vy, -4f, 18f));
        rb.angularVelocity = -Mathf.Sign(vx) * 240f;

        // No choca con los jugadores: los daña al explotar
        Collider2D mio = GetComponent<Collider2D>();
        for (int i = 0; i < Jugadores.Todos.Count; i++)
        {
            Collider2D c = Jugadores.Todos[i] != null ? Jugadores.Todos[i].GetComponent<Collider2D>() : null;
            if (c != null && mio != null) Physics2D.IgnoreCollision(mio, c);
        }

        foreach (CerdoMenor p in CerdoMenor.Todos)
        {
            Collider2D c = p.GetComponent<Collider2D>();
            if (c != null && mio != null) Physics2D.IgnoreCollision(mio, c);
        }

        if (ReyCerdo.Actual != null)
        {
            Collider2D c = ReyCerdo.Actual.GetComponent<Collider2D>();
            if (c != null && mio != null) Physics2D.IgnoreCollision(mio, c);
        }
    }

    void Start()
    {
        if (inicio == 0f) inicio = Time.time;
        anim.Reproducir(mecha, 10f);
    }

    void Update()
    {
        if (explotando)
        {
            if (anim.Terminada) Destroy(gameObject);
            return;
        }

        // Parpadea cada vez más rápido antes de explotar
        float p = (Time.time - inicio) / tiempoMecha;
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = Mathf.Sin(Time.time * Mathf.Lerp(8f, 40f, p)) > 0.4f ? new Color(1f, 0.6f, 0.6f) : Color.white;

        if (p >= 1f) { Explotar(); return; }

        for (int i = 0; i < Jugadores.Todos.Count; i++)
        {
            PlayerController j = Jugadores.Todos[i];
            if (j == null || j.Muerto) continue;
            if (Vector2.Distance(CentroDe(j), transform.position) < 0.9f) { Explotar(); return; }
        }
    }

    void Explotar()
    {
        explotando = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        transform.rotation = Quaternion.identity;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;

        anim.Reproducir(explosion, 14f, false, true);
        AudioJuego.SonidoExplosionEsbirro();

        for (int i = 0; i < Jugadores.Todos.Count; i++)
        {
            PlayerController j = Jugadores.Todos[i];
            if (j == null || j.Muerto) continue;

            if (Vector2.Distance(CentroDe(j), transform.position) <= radio)
            {
                Salud s = j.GetComponent<Salud>();
                if (s != null) s.RecibirDanio(danio);
            }
        }
    }

    static Vector2 CentroDe(PlayerController j)
    {
        Collider2D c = j.GetComponent<Collider2D>();
        return c != null ? (Vector2)c.bounds.center : (Vector2)j.transform.position;
    }
}
