using UnityEngine;

/// <summary>
/// Esbirro invocado por los portales del jefe. Persigue al jugador y le hace daño por contacto.
/// </summary>
[RequireComponent(typeof(Salud))]
public class Esbirro : MonoBehaviour
{
    [Header("Persecución")]
    public Transform objetivo;
    public float velocidadHorizontal = 3.6f;
    public float velocidadVertical = 6f;
    public float distanciaDanio = 1.05f;
    public float danio = 10f;
    public float enfriamientoDanio = 2f;

    [Header("Orbe de curación")]
    [Tooltip("Probabilidad, al nacer, de que este esbirro lance orbes que curan al jugador.")]
    [Range(0f, 1f)] public float probabilidadOrbe = 0.35f;
    public GameObject orbeCuracionPrefab;
    public float intervaloOrbe = 3.2f;
    public float primeraEsperaOrbe = 1.4f;
    public float velocidadOrbe = 7f;
    public float vidaOrbe = 3.5f;
    public float curacionOrbe = 18f;

    [Header("Animación")]
    public Sprite[] frames;
    public float fps = 10f;

    [Header("Limpieza")]
    public float distanciaMaxima = 40f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    float proximoDanio;
    float tiempoAnim;
    int frameActual = -1;
    float proximoRetarget;
    bool lanzaOrbes;
    float proximoOrbe;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        if (objetivo == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) objetivo = p.transform;
        }

        lanzaOrbes = Random.value < probabilidadOrbe;
        proximoOrbe = Time.time + primeraEsperaOrbe;
    }

    void FixedUpdate()
    {
        // Con varios jugadores persigue al más cercano
        if (Jugadores.Todos.Count > 1 && Time.time >= proximoRetarget)
        {
            proximoRetarget = Time.time + 0.4f;
            Transform cercano = Jugadores.MasCercano(transform.position);
            if (cercano != null) objetivo = cercano;
        }

        if (objetivo == null || rb == null) return;

        Vector2 pos = rb.position;
        Vector2 destino = objetivo.position;

        float x = Mathf.MoveTowards(pos.x, destino.x, velocidadHorizontal * Time.fixedDeltaTime);
        float y = Mathf.MoveTowards(pos.y, destino.y, velocidadVertical * Time.fixedDeltaTime);

        rb.MovePosition(new Vector2(x, y));
    }

    void Update()
    {
        Animar();

        if (objetivo == null) return;

        Vector2 pos = transform.position;
        Vector2 destino = objetivo.position;

        if (sr != null) sr.flipX = destino.x < pos.x;

        if (Vector2.Distance(pos, destino) <= distanciaDanio && Time.time >= proximoDanio)
        {
            proximoDanio = Time.time + enfriamientoDanio;

            Salud salud = objetivo.GetComponent<Salud>();
            if (salud != null) salud.RecibirDanio(danio);
        }

        if (lanzaOrbes && Time.time >= proximoOrbe) LanzarOrbe();

        if (Vector2.Distance(pos, destino) > distanciaMaxima) Destroy(gameObject);
    }

    /// <summary>Lanza un orbe que cura al jugador al tocarlo.</summary>
    void LanzarOrbe()
    {
        if (orbeCuracionPrefab == null || objetivo == null)
        {
            lanzaOrbes = false;
            return;
        }

        proximoOrbe = Time.time + intervaloOrbe;

        Vector2 origen = transform.position;
        Vector2 destino = objetivo.position;
        Vector2 dir = (destino - origen).normalized;

        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;

        GameObject g = Instantiate(orbeCuracionPrefab, origen, Quaternion.identity);

        Bala orbe = g.GetComponent<Bala>();
        if (orbe != null)
        {
            orbe.cura = true;
            orbe.curacion = curacionOrbe;
            orbe.velocidad = velocidadOrbe;
            orbe.vida = vidaOrbe;
            orbe.objetivo = objetivo;
            orbe.Lanzar(dir);
        }
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
