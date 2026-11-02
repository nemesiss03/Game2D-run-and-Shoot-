using UnityEngine;

public enum TipoRecolectable { Moneda, Gema }

/// <summary>
/// Moneda o gema. Cae con un pequeño salto cuando sale de un cofre, se queda flotando
/// y el jugador la recoge al acercarse (el imán la atrae desde un poco más lejos).
/// </summary>
public class Recolectable : MonoBehaviour
{
    public TipoRecolectable tipo = TipoRecolectable.Moneda;
    public int puntos = Puntuacion.PuntosMoneda;
    public Color colorTexto = new Color(1f, 0.86f, 0.25f, 1f);

    [Header("Aspecto")]
    public Sprite[] frames;
    public float fps = 8f;

    [Header("Recogida")]
    public float radioRecogida = 0.55f;
    public float radioIman = 2.6f;
    public float velocidadIman = 12f;
    [Tooltip("Segundos tras salir del cofre antes de poder recogerla.")]
    public float retrasoRecogida = 0.45f;

    SpriteRenderer sr;
    Vector2 velocidad;
    bool enSuelo;
    float baseY;
    float nace;
    float tiempoAnim;
    int frameActual = -1;
    float fase;

    const float Gravedad = 20f;
    const float AlturaFlotando = 0.3f;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        nace = Time.time;
        fase = Random.value * 6.28f;
    }

    /// <summary>Impulso inicial al salir del cofre.</summary>
    public void Lanzar(Vector2 impulso)
    {
        velocidad = impulso;
        enSuelo = false;
    }

    void Update()
    {
        Animar();

        if (!enSuelo) Caer();
        else Flotar();

        if (Time.time - nace < retrasoRecogida) return;

        PlayerController jugador = JugadorCercano(radioIman);
        if (jugador == null) return;

        Vector2 destino = jugador.transform.position;
        Collider2D c = jugador.GetComponent<Collider2D>();
        if (c != null) destino = c.bounds.center;

        Vector2 pos = transform.position;

        if (Mathf.Sqrt(DistanciaCuadrada(jugador, pos)) <= radioRecogida)
        {
            Recoger();
            return;
        }

        // Imán: se acerca más rápido cuanto más cerca está
        float t = 1f - Mathf.Clamp01(Vector2.Distance(pos, destino) / radioIman);
        transform.position = Vector2.MoveTowards(pos, destino, velocidadIman * (0.4f + t) * Time.deltaTime);
        enSuelo = true;
        baseY = transform.position.y;
    }

    void Caer()
    {
        velocidad.y -= Gravedad * Time.deltaTime;

        Vector2 pos = transform.position;
        Vector2 paso = velocidad * Time.deltaTime;

        if (velocidad.y < 0f)
        {
            float alcance = Mathf.Abs(paso.y) + AlturaFlotando;
            RaycastHit2D[] hits = Physics2D.RaycastAll(pos, Vector2.down, alcance);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D c = hits[i].collider;
                if (c == null || c.isTrigger) continue;
                if (c.GetComponentInParent<Salud>() != null) continue;

                transform.position = new Vector3(pos.x + paso.x, hits[i].point.y + AlturaFlotando, 0f);
                baseY = transform.position.y;
                enSuelo = true;
                return;
            }
        }

        transform.position = pos + paso;

        // Si nunca encuentra suelo (cae a un abismo) desaparece
        if (transform.position.y < -60f) Destroy(gameObject);
    }

    void Flotar()
    {
        Vector3 p = transform.position;
        p.y = baseY + Mathf.Sin(Time.time * 3f + fase) * 0.06f;
        transform.position = p;
    }

    PlayerController JugadorCercano(float radio)
    {
        Vector2 pos = transform.position;
        PlayerController mejor = null;
        float mejorD = radio * radio;

        for (int i = 0; i < Jugadores.Todos.Count; i++)
        {
            PlayerController j = Jugadores.Todos[i];
            if (j == null || j.Muerto) continue;

            float d = DistanciaCuadrada(j, pos);
            if (d < mejorD) { mejorD = d; mejor = j; }
        }

        return mejor;
    }

    static float DistanciaCuadrada(PlayerController j, Vector2 punto)
    {
        Collider2D c = j.GetComponent<Collider2D>();
        if (c == null) return ((Vector2)j.transform.position - punto).sqrMagnitude;
        return c.bounds.SqrDistance(punto);
    }

    void Recoger()
    {
        Puntuacion.Sumar(puntos, transform.position, colorTexto);
        AudioJuego.SonidoCuracion();
        Destroy(gameObject);
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
