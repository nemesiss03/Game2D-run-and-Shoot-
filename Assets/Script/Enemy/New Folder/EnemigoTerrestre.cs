using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Salud))]
public class EnemigoTerrestre : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadPatrulla = 2f;
    public float velocidadPersecucion = 3.6f;
    public float rangoDeteccion = 7f;
    [Tooltip("Capa del suelo y las paredes. Si se deja vacía usa la capa Ground.")]
    public LayerMask capaSuelo;

    [Header("Sensores")]
    public float distanciaFrente = 0.5f;

    [Header("Daño por contacto")]
    public float danio = 4f;
    public float enfriamientoDanio = 1f;

    [Header("Sprite")]
    [Tooltip("Marca esto si el dibujo original mira hacia la derecha.")]
    public bool spriteMiraDerecha = true;

    Rigidbody2D rb;
    Collider2D col;
    SpriteRenderer sr;
    int direccion = 1;
    float proximoDanio;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        rb.freezeRotation = true;

        if (capaSuelo.value == 0) capaSuelo = LayerMask.GetMask("Ground");
        if (capaSuelo.value == 0)
            Debug.LogWarning("EnemigoTerrestre: no hay capa de suelo asignada.", this);

        direccion = Random.value < 0.5f ? -1 : 1;
    }

    void FixedUpdate()
    {
        Transform objetivo = Jugadores.MasCercano(transform.position);
        bool persiguiendo = false;

        if (objetivo != null)
        {
            Vector2 a = objetivo.position - transform.position;
            if (Mathf.Abs(a.x) <= rangoDeteccion && Mathf.Abs(a.y) <= 3f)
            {
                persiguiendo = true;
                if (Mathf.Abs(a.x) > 0.2f) direccion = a.x > 0f ? 1 : -1;
            }
        }

        if (!HayPisoAdelante() || HayParedAdelante())
        {
            if (persiguiendo)
            {
                // Se detiene en el borde en vez de caerse
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                ActualizarSprite();
                return;
            }
            direccion = -direccion;
        }

        float v = persiguiendo ? velocidadPersecucion : velocidadPatrulla;
        rb.linearVelocity = new Vector2(direccion * v, rb.linearVelocity.y);
        ActualizarSprite();
    }

    void ActualizarSprite()
    {
        if (sr != null) sr.flipX = spriteMiraDerecha ? direccion < 0 : direccion > 0;
    }

    bool HayPisoAdelante()
    {
        Bounds b = col.bounds;
        float x = (direccion > 0 ? b.max.x : b.min.x) + direccion * 0.15f;
        return Golpea(new Vector2(x, b.min.y + 0.1f), Vector2.down, 0.5f);
    }

    bool HayParedAdelante()
    {
        Bounds b = col.bounds;
        float x = direccion > 0 ? b.max.x : b.min.x;
        return Golpea(new Vector2(x, b.center.y), new Vector2(direccion, 0f), distanciaFrente);
    }

    bool Golpea(Vector2 origen, Vector2 dir, float distancia)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origen, dir, distancia, capaSuelo);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i].collider;
            if (c == null || c.isTrigger) continue;
            if (c.GetComponentInParent<Salud>() != null) continue;   // ignora jugadores y enemigos
            return true;
        }
        return false;
    }

    void OnCollisionStay2D(Collision2D c)
    {
        if (Time.time < proximoDanio) return;

        Salud s = c.collider.GetComponentInParent<Salud>();
        if (s == null || s.equipo != Equipo.Jugador) return;

        s.RecibirDanio(danio);
        proximoDanio = Time.time + enfriamientoDanio;
    }
}