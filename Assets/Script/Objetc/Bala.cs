using UnityEngine;

/// <summary>
/// Proyectil genérico. El equipo decide a quién puede dañar.
/// Si <see cref="cura"/> está activo, en vez de dañar cura al jugador y atraviesa a los enemigos.
/// </summary>
public class Bala : MonoBehaviour
{
    public Equipo equipo = Equipo.Jugador;   // de quién es la bala
    public float velocidad = 18f;
    public float vida = 2.5f;
    public float danio = 1f;

    [Header("Orbe de curación")]
    [Tooltip("Si está activo, el proyectil cura al jugador en vez de dañar.")]
    public bool cura = false;
    public float curacion = 18f;
    [Tooltip("Objetivo al que persigue el orbe. Si está vacío, vuela recto.")]
    public Transform objetivo;
    public float giroPorSegundo = 260f;

    [Header("Efectos")]
    public bool efectoImpacto = true;
    public float escalaImpacto = 0.45f;

    public void Lanzar(Vector2 direccion)
    {
        if (direccion.sqrMagnitude < 0.0001f) direccion = Vector2.right;
        direccion.Normalize();

        Destroy(gameObject, vida);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        rb.linearVelocity = direccion * velocidad;
        float ang = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);
    }

    void FixedUpdate()
    {
        // Los orbes de curación persiguen al jugador para no fallar nunca
        if (objetivo == null) return;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) return;

        Vector2 actual = rb.linearVelocity;
        if (actual.sqrMagnitude < 0.0001f) return;

        Vector2 deseo = (Vector2)objetivo.position - rb.position;
        if (deseo.sqrMagnitude < 0.0001f) return;

        deseo = deseo.normalized * velocidad;

        float angulo = Vector2.SignedAngle(actual, deseo);
        float paso = Mathf.Clamp(angulo, -giroPorSegundo * Time.fixedDeltaTime, giroPorSegundo * Time.fixedDeltaTime);

        float rad = paso * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        Vector2 nuevo = new Vector2(actual.x * cos - actual.y * sin, actual.x * sin + actual.y * cos);

        rb.linearVelocity = nuevo;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(nuevo.y, nuevo.x) * Mathf.Rad2Deg);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro == null) return;
        if (otro.GetComponent<Bala>() != null) return;      // las balas no se chocan entre sí
        if (otro.GetComponent<Portal>() != null) return;    // los portales no bloquean

        Salud salud = otro.GetComponentInParent<Salud>();

        // Las zonas, cofres, banderas y demás triggers no son sólidos: la bala los atraviesa
        if (otro.isTrigger && salud == null) return;

        if (cura)
        {
            // El orbe de curación solo le importa el jugador: atraviesa a los enemigos
            if (salud == null || salud.equipo != Equipo.Jugador) return;

            salud.Curar(curacion);
            AudioJuego.SonidoCuracion();

            if (efectoImpacto) EfectosJuego.Impacto(transform.position, escalaImpacto * 1.4f);

            Destroy(gameObject);
            return;
        }

        if (salud != null)
        {
            if (salud.equipo == equipo) return;             // no daña a su propio equipo
            salud.RecibirDanio(danio);
        }

        if (efectoImpacto) EfectosJuego.Impacto(transform.position, escalaImpacto);

        Destroy(gameObject);
    }
}
