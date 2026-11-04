using System.Collections;
using UnityEngine;

/// <summary>
/// Jefe flotante con dos habilidades:
///   1) ráfagas de proyectiles teledirigidas hacia el jugador.
///   2) portales que invocan esbirros que persiguen al jugador.
/// Al llegar al 50 % de vida destruye la plataforma y la pelea pasa a caída libre.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class JefeFlotante : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform objetivo;            // el jugador
    public PlayerController jugador;
    public Salud saludPropia;

    [Header("Flote")]
    public float amplitud = 0.35f;
    public float frecuencia = 1.5f;
    public float velocidadSeguir = 3.5f;
    public float alturaSobreObjetivo = 3.4f;
    public float rangoHorizontal = 4.6f;

    [Header("Habilidad 1 · Proyectiles")]
    public GameObject balaPrefab;
    public Transform puntoDisparo;
    public float cadenciaDisparo = 2.4f;
    public int proyectilesPorRafaga = 2;
    public float dispersionGrados = 13f;
    public float esperaEntreProyectiles = 0.12f;
    public float velocidadProyectil = 8.5f;
    public float vidaProyectil = 5f;
    public float danioProyectil = 10f;

    [Header("Fase 2 · Orbe del abismo (sustituye a los proyectiles)")]
    public float cadenciaOrbe = 4.6f;
    public float danioOrbe = 22f;
    public float velocidadOrbe = 7f;

    [Header("Ataque en U")]
    public float cadenciaAtaqueU = 17f;
    public float primerAtaqueU = 11f;

    [Header("Habilidad 2 · Portales")]
    public GameObject portalPrefab;
    public float cadenciaPortales = 12f;
    public int portalesPorAtaque = 2;
    public float separacionPortales = 3.4f;
    public float alturaPortales = 1.2f;

    [Header("Fase 2 · Caída libre")]
    public PlataformaCelestial plataforma;
    public float umbralFase2 = 0.5f;
    public bool faseCaida { get; private set; }

    [Header("Animación")]
    public Animator animador;

    [Header("Muerte")]
    public float tiempoDesaparicion = 2.2f;

    static readonly int hashAtacar = Animator.StringToHash("Atacar");
    static readonly int hashMorir = Animator.StringToHash("Morir");

    /// <summary>Jefe actual de la escena (null si ya ha muerto).</summary>
    public static JefeFlotante Activo { get; private set; }

    Rigidbody2D rb;
    float baseY;
    float proximoDisparo;
    float proximoPortal;
    float proximoAtaqueU;
    bool derrotado;
    float proximoRetarget;

    void OnEnable() { Activo = this; }
    void OnDisable() { if (Activo == this) Activo = null; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseY = transform.position.y;

        if (saludPropia == null) saludPropia = GetComponent<Salud>();
        if (animador == null) animador = GetComponent<Animator>();
    }

    void Start()
    {
        if (objetivo == null)
        {
            Transform cercano = Jugadores.MasCercano(transform.position);
            if (cercano != null) objetivo = cercano;
        }
        if (objetivo == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) objetivo = p.transform;
        }
        if (jugador == null && objetivo != null) jugador = objetivo.GetComponent<PlayerController>();

        if (saludPropia != null)
        {
            saludPropia.CambioVida += RevisarVida;
            saludPropia.Muerte += AlMorir;
        }

        proximoDisparo = Time.time + 1.5f;
        proximoPortal = Time.time + 9f;
        proximoAtaqueU = Time.time + primerAtaqueU;
    }

    void OnDestroy()
    {
        if (saludPropia != null)
        {
            saludPropia.CambioVida -= RevisarVida;
            saludPropia.Muerte -= AlMorir;
        }
    }

    void FixedUpdate()
    {
        // Con dos jugadores el jefe persigue siempre al más cercano
        if (Jugadores.Todos.Count > 1 && Time.time >= proximoRetarget)
        {
            proximoRetarget = Time.time + 0.5f;
            Transform cercano = Jugadores.MasCercano(transform.position);
            if (cercano != null) objetivo = cercano;
        }

        if (objetivo == null) return;

        float objetivoX = Mathf.Clamp(objetivo.position.x, -rangoHorizontal, rangoHorizontal);
        float x = Mathf.MoveTowards(rb.position.x, objetivoX, velocidadSeguir * Time.fixedDeltaTime);

        float y;
        if (faseCaida)
        {
            // En caída libre el jefe acompaña la caída del jugador para seguir en pantalla
            float destinoY = objetivo.position.y + alturaSobreObjetivo;
            y = Mathf.Lerp(rb.position.y, destinoY, 12f * Time.fixedDeltaTime);

            // Si el jugador sube, el jefe no se sale de la parte alta de la pantalla
            Camera cam = Camera.main;
            if (cam != null) y = Mathf.Min(y, cam.transform.position.y + cam.orthographicSize - 1.3f);
        }
        else
        {
            y = baseY + Mathf.Sin(Time.time * frecuencia) * amplitud;
        }

        rb.MovePosition(new Vector2(x, y));
    }

    void Update()
    {
        if (derrotado || objetivo == null) return;

        // Ataque en U: avisa, apaga los láseres y barre los bordes de la pantalla
        if (Time.time >= proximoAtaqueU && !AtaqueEnU.Activo)
        {
            proximoAtaqueU = Time.time + (faseCaida ? cadenciaAtaqueU * 0.75f : cadenciaAtaqueU);
            AtaqueEnU.Iniciar();
        }

        // Habilidad 1: proyectiles (fase 1) u orbe del abismo (fase 2)
        if (Time.time >= proximoDisparo && !AtaqueEnU.Activo)
        {
            if (faseCaida)
            {
                proximoDisparo = Time.time + cadenciaOrbe;
                LanzarOrbe();
            }
            else
            {
                proximoDisparo = Time.time + cadenciaDisparo;
                StartCoroutine(Rafaga());
            }
        }

        // Habilidad 2: portales con esbirros
        if (portalPrefab != null && Time.time >= proximoPortal)
        {
            proximoPortal = Time.time + cadenciaPortales;
            AbrirPortales();
        }
    }

    // ---------------------------------------------------------------- Habilidad 1
    IEnumerator Rafaga()
    {
        if (balaPrefab == null || puntoDisparo == null || objetivo == null) yield break;

        if (animador != null) animador.SetTrigger(hashAtacar);

        AudioJuego.SonidoDisparoJefe();
            
        int total = Mathf.Max(1, proyectilesPorRafaga);

        for (int i = 0; i < total; i++)
        {
            if (objetivo == null) yield break;

            Vector2 baseDir = ((Vector2)objetivo.position - (Vector2)puntoDisparo.position).normalized;

            // Abanico: -dispersion/2 ... +dispersion/2
            float t = total == 1 ? 0.5f : (float)i / (total - 1);
            float angulo = Mathf.Lerp(-dispersionGrados * 0.5f, dispersionGrados * 0.5f, t);
            Vector2 dir = Rotar(baseDir, angulo);

            GameObject b = Instantiate(balaPrefab, puntoDisparo.position, Quaternion.identity);
            Bala bala = b.GetComponent<Bala>();
            if (bala != null)
            {
                bala.equipo = Equipo.Enemigo;
                bala.velocidad = velocidadProyectil;
                bala.vida = vidaProyectil;
                bala.danio = danioProyectil;
                bala.Lanzar(dir);
            }

            EfectosJuego.Fogonazo(puntoDisparo.position, dir, 0.8f);

            yield return new WaitForSeconds(esperaEntreProyectiles);
        }
    }

    void LanzarOrbe()
    {
        if (objetivo == null) return;

        if (animador != null) animador.SetTrigger(hashAtacar);
        AudioJuego.SonidoInvocacion();
        OrbeAbismo.Crear(transform, objetivo, danioOrbe, velocidadOrbe);
    }

    // ---------------------------------------------------------------- Habilidad 2
    void AbrirPortales()
    {
        int total = Mathf.Max(1, portalesPorAtaque);
        Vector3 referencia = objetivo.position;

        for (int i = 0; i < total; i++)
        {
            float lado = (i % 2 == 0) ? -1f : 1f;
            float desplazamiento = separacionPortales * (0.5f + 0.5f * (i / Mathf.Max(1, total)));
            Vector3 pos = new Vector3(
                referencia.x + lado * desplazamiento,
                referencia.y + alturaPortales,
                0f);

            GameObject g = Instantiate(portalPrefab, pos, Quaternion.identity);
            Portal portal = g.GetComponent<Portal>();
            if (portal != null) portal.objetivo = objetivo;
        }
    }

    // ---------------------------------------------------------------- Vida / fases
    void RevisarVida(float actual, float maxima)
    {
        if (faseCaida || derrotado) return;
        if (maxima <= 0f) return;

        if (actual / maxima <= umbralFase2) IniciarFaseCaida();
    }

    void IniciarFaseCaida()
    {
        faseCaida = true;

        if (plataforma != null) plataforma.Destruir();
        if (jugador != null) jugador.ActivarCaidaLibre();

        // El jefe se vuelve más agresivo en la fase final
        proximoDisparo = Time.time + 2f;
        cadenciaPortales = Mathf.Max(4.5f, cadenciaPortales * 0.7f);

        // Además, 5 láseres que apuntan al jugador desde los bordes de la pantalla
        SistemaLasers.Activar();

        if (HUDJuego.Instancia != null)
            HUDJuego.Instancia.MostrarAviso("¡LA PLATAFORMA SE DESTRUYE!\nCAÍDA LIBRE · W / S PARA SUBIR Y BAJAR", 4f);
    }

    void AlMorir()
    {
        if (derrotado) return;
        derrotado = true;

        StopAllCoroutines();

        if (animador != null) animador.SetTrigger(hashMorir);
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // La caída infinita y los láseres terminan al ganar
        CaidaInfinita.DetenerGlobal();
        AtaqueEnU.Detener();
        SistemaLasers.Detener();

        // El dragón vuela de victoria y la pantalla de victoria aparece mientras tanto
        CinematicaVictoria.Iniciar();

        StartCoroutine(EstallidoFinal());

        Destroy(gameObject, tiempoDesaparicion);
    }

    /// <summary>Cadena de explosiones que acompaña la destrucción del jefe.</summary>
    IEnumerator EstallidoFinal()
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        float radio = 1.2f;
        if (sr != null) radio = sr.bounds.extents.magnitude * 0.6f;

        Vector3 centro = sr != null ? sr.bounds.center : transform.position;

        for (int i = 0; i < 6; i++)
        {
            Vector2 desvio = Random.insideUnitCircle * radio;
            Vector3 pos = centro + new Vector3(desvio.x, desvio.y, 0f);

            EfectosJuego.ExplosionJefe(pos, Random.Range(1.2f, 2.4f));

            if (i == 2) AudioJuego.SonidoExplosionJefe();

            yield return new WaitForSeconds(Random.Range(0.10f, 0.22f));
        }
    }

    static Vector2 Rotar(Vector2 v, float grados)
    {
        float rad = grados * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
