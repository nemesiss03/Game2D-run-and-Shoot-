using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Jugador dragón: correr, saltar, dash con animación y aliento de fuego.
/// La cabeza gira hacia donde se apunta y de la boca salen las bolas de fuego.
/// Tiene 15 cargas por tanda: al quedarse sin fuego entra en enfriamiento.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 7f;
    public float fuerzaSalto = 12f;
    [Tooltip("Velocidad máxima de caída (evita que el dragón se acelere sin control en la caída libre).")]
    public float velocidadMaximaCaida = 9f;


    [Header("Salto (detalles)")]
    public AjustesSalto salto = new AjustesSalto();

    [Header("Doble salto")]
    [Tooltip("Saltos extra en el aire (1 = doble salto). Se recuperan al tocar suelo.")]
    public int saltosExtra = 1;
    [Tooltip("Fuerza del salto extra respecto al salto normal.")]
    [Range(0.5f, 1.2f)] public float fuerzaSaltoExtra = 0.92f;

    [Header("Dash")]
    public float velocidadDash = 22f;
    public float duracionDash = 0.18f;
    public float enfriamientoDash = 1.1f;
    [Tooltip("Segundos entre una copia de la estela y la siguiente.")]
    public float intervaloEstela = 0.025f;

    [Header("Aliento de fuego")]
    public GameObject bolaFuegoPrefab;
    public float cadencia = 0.22f;
    [Tooltip("Bolas de fuego que se pueden lanzar antes de tener que enfriarse.")]
    public int cargasMaximas = 15;
    [Tooltip("Segundos de descanso (enfriamiento) para recuperar las 15 cargas.")]
    public float tiempoEnfriamiento = 2.5f;
    public float danio = 12f;
    public float velocidadProyectil = 17f;
    public float vidaProyectil = 3f;

    [Header("Cabeza")]
    public CabezaDragon cabeza;

    [Header("Arma")]
    public ArmaDragon arma;

    [Header("Animación")]
    public Animator animador;
    public SpriteRenderer spriteCuerpo;

    [Header("Jugador")]
    public int numeroJugador = 1;
    public TipoDispositivo dispositivo = TipoDispositivo.TecladoRaton;
    public int indiceMando = 0;

    [Header("Estado")]
    public bool caidaLibre = false;      // lo activa el jefe en la fase 2
    public bool controlActivo = true;
    [Tooltip("En el cliente online el mundo lo simula el host y aquí solo se reproduce.")]
    public bool controladoPorRed = false;
    [Tooltip("Altura a la que se queda el dragón durante la caída libre (no baja de la Y).")]
    public float alturaCaida = 0f;

    // --- Lectura para el HUD ---
    public int Balas { get { return cargas; } }
    public int CapacidadCargador { get { return cargasMaximas; } }
    public bool Recargando { get { return enfriando; } }
    public bool Muerto { get { return muerto; } }
    public float ProgresoRecarga
    {
        get { return enfriando ? Mathf.Clamp01((Time.time - inicioEnfriamiento) / tiempoEnfriamiento) : 1f; }
    }

    /// <summary>0 = recién usado, 1 = el dash está listo (para la barra del HUD).</summary>
    public float ProgresoDash
    {
        get
        {
            if (Time.time < finDash) return 0f;
            return Mathf.Clamp01(1f - (proximoDash - Time.time) / Mathf.Max(0.01f, enfriamientoDash));
        }
    }

    public int SaltosExtraRestantes { get { return Mathf.Max(0, saltosExtra - saltosAireUsados); } }

    public EntradaJugador Entrada { get; private set; }

    /// <summary>Permite que la red sustituya la entrada local (jugador remoto).</summary>
    public void AsignarEntrada(EntradaJugador entrada)
    {
        Entrada = entrada;
    }

    Rigidbody2D rb;
    Salud salud;
    Camera cam;

    float h;
    bool pideSalto;
    float tiempoPulsoSalto = -99f;
    float tiempoUltimoSuelo = -99f;
    bool enSuelo;
    float proximoDisparo;

    int cargas;
    bool enfriando;
    float inicioEnfriamiento;

    int saltosAireUsados;
    bool pideSaltoAire;
    float proximaEstela;

    float finDash;
    float proximoDash;
    float direccionDash = 1f;
    bool mirandoIzquierda;
    bool muerto;

    static readonly int hashVelocidad = Animator.StringToHash("Velocidad");
    static readonly int hashDash = Animator.StringToHash("Dash");
    static readonly int hashMuerto = Animator.StringToHash("Muerto");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        salud = GetComponent<Salud>();
        cam = Camera.main;

        if (animador == null) animador = GetComponent<Animator>();
        if (spriteCuerpo == null) spriteCuerpo = GetComponent<SpriteRenderer>();

        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        rb.freezeRotation = true;

        cargas = cargasMaximas;
    }

    void OnEnable() { Jugadores.Registrar(this); }
    void OnDisable() { Jugadores.Quitar(this); }

    void Start()
    {
        if (salud != null) salud.Muerte += AlMorir;
    }

    void OnDestroy()
    {
        if (salud != null) salud.Muerte -= AlMorir;
    }

    /// <summary>Se llama desde el arranque de la partida para asignar dispositivo.</summary>
    public void Configurar(TipoDispositivo tipo, int mando, int numero)
    {
        dispositivo = tipo;
        indiceMando = mando;
        numeroJugador = numero;
        Entrada = EntradaJugador.Crear(tipo, mando);
    }

    void Update()
    {
        if (muerto) return;

        if (controladoPorRed)
        {
            ActualizarAnimacion();
            return;
        }

        if (Entrada == null) Entrada = EntradaJugador.Crear(dispositivo, indiceMando);

        ActualizarAnimacion();

        if (!controlActivo) return;

        ActualizarCabeza();

        // --- Movimiento horizontal ---
        h = Entrada.Horizontal();

        if (h > 0.01f) mirandoIzquierda = false;
        else if (h < -0.01f) mirandoIzquierda = true;

        FijarLado(mirandoIzquierda);

        // --- Salto ---
        if (Entrada.SaltoPulsado())
        {
            tiempoPulsoSalto = Time.time;
            pideSaltoAire = true;
        }
        // --- Dash ---
        if (Entrada.DashPulsado() && Time.time >= proximoDash && Time.time >= finDash)
            IniciarDash();

        // --- Enfriamiento manual ---
        if (Entrada.EnfriamientoPulsado() && !enfriando && cargas < cargasMaximas)
            IniciarEnfriamiento();

        // --- Disparo (mantener pulsado) ---
        if (!enfriando && Entrada.DisparoMantenido() && Time.time >= proximoDisparo)
        {
            if (cargas > 0)
            {
                Disparar();
                proximoDisparo = Time.time + cadencia;
            }
            else
            {
                IniciarEnfriamiento();
            }
        }

        if (enfriando && Time.time - inicioEnfriamiento >= tiempoEnfriamiento)
            TerminarEnfriamiento();
    }

    void FixedUpdate()
    {
        if (muerto || controladoPorRed) return;

        Vector2 vel = rb.linearVelocity;

        if (Time.time < finDash)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(direccionDash * velocidadDash, 0f);
            enSuelo = false;
            tiempoPulsoSalto = -99f;     // no guardar saltos pedidos durante el dash
            pideSaltoAire = false;

            if (Time.time >= proximaEstela)
            {
                proximaEstela = Time.time + intervaloEstela;
                EfectosMovimiento.Fantasma(gameObject);
            }

            return;
        }

        if (enSuelo)
        {
            tiempoUltimoSuelo = Time.time;
            // Solo cuenta como aterrizaje si no está subiendo (así el propio salto no recarga el doble salto)
            if (vel.y <= 0.5f) saltosAireUsados = 0;
        }

        vel.x = h * velocidad;

        bool pidioSalto = Time.time - tiempoPulsoSalto <= salto.tiempoBuffer;
        bool puedeSaltar = Time.time - tiempoUltimoSuelo <= salto.tiempoCoyote;

        if (pidioSalto && puedeSaltar && !caidaLibre)
        {
            vel.y = salto.VelocidadInicial;
            tiempoPulsoSalto = -99f;
            tiempoUltimoSuelo = -99f;
            pideSaltoAire = false;
            AudioJuego.Tocar(SonidoId.Salto);
        }
        else if (pideSaltoAire && !puedeSaltar && !caidaLibre &&
                 saltosAireUsados < saltosExtra && !HaySueloCerca())
        {
            // Doble salto: impulso nuevo en el aire y anillo bajo los pies
            vel.y = salto.VelocidadInicial * fuerzaSaltoExtra;
            saltosAireUsados++;
            tiempoPulsoSalto = -99f;
            AudioJuego.Tocar(SonidoId.Salto);
            EfectosMovimiento.AnilloSalto(PosicionPies());
        }

        pideSaltoAire = false;

        if (caidaLibre)
        {
            // Caída infinita: el dragón no baja de la Y, es el mundo el que sube.
            // Así el jefe y los esbirros nunca se quedan fuera de alcance.
            rb.gravityScale = 0f;
            vel.y = 0f;
            rb.linearVelocity = vel;
            rb.position = new Vector2(rb.position.x, alturaCaida);
            enSuelo = false;
            return;
        }

        // Gravedad según la fase del salto
        bool mantiene = Entrada != null && Entrada.SaltoMantenido();
        float escala = salto.Gravedad / Mathf.Abs(Physics2D.gravity.y);

        if (vel.y < 0f) escala *= salto.multiplicadorCaida;
        else if (vel.y > 0f && !mantiene) escala *= salto.multiplicadorCorte;

        if (!enSuelo && Mathf.Abs(vel.y) < salto.umbralCima) escala *= salto.multiplicadorCima;

        rb.gravityScale = escala;

        if (vel.y < -salto.velocidadMaximaCaida) vel.y = -salto.velocidadMaximaCaida;

        rb.linearVelocity = vel;

        enSuelo = false;
    }

    /// <summary>Punto bajo el dragón (donde nacen los anillos y el polvo).</summary>
    Vector3 PosicionPies()
    {
        BoxCollider2D caja = GetComponent<BoxCollider2D>();
        if (caja == null) return transform.position;

        Bounds b = caja.bounds;
        return new Vector3(b.center.x, b.min.y, 0f);
    }

    /// <summary>
    /// Hay suelo justo debajo: en ese caso un salto pulsado se guarda para el aterrizaje
    /// en vez de gastar el salto extra.
    /// </summary>
    bool HaySueloCerca()
    {
        if (rb.linearVelocity.y > 0.5f) return false;

        BoxCollider2D caja = GetComponent<BoxCollider2D>();
        if (caja == null) return false;

        Bounds b = caja.bounds;
        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.45f);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i].collider;
            if (c == null || c.isTrigger || c.transform.IsChildOf(transform)) continue;
            if (c.GetComponentInParent<Salud>() != null) continue;
            return true;
        }

        return false;
    }

    // --- Detección de suelo por contacto ---
    void OnCollisionEnter2D(Collision2D c) { RevisarContactos(c); }
    void OnCollisionStay2D(Collision2D c) { RevisarContactos(c); }

    void RevisarContactos(Collision2D c)
    {
        if (muerto) return;

        for (int i = 0; i < c.contactCount; i++)
        {
            if (c.GetContact(i).normal.y > 0.5f)
            {
                enSuelo = true;
                return;
            }
        }
    }

    void ActualizarAnimacion()
    {
        if (animador == null) return;

        float rapidez = (controlActivo && !muerto) ? Mathf.Abs(rb.linearVelocity.x) : 0f;
        animador.SetFloat(hashVelocidad, rapidez);
    }

    /// <summary>Voltea cuerpo, cabeza y arma a la vez.</summary>
    void FijarLado(bool izquierda)
    {
        if (spriteCuerpo != null) spriteCuerpo.flipX = izquierda;
        if (cabeza != null) cabeza.FijarLado(izquierda);
        if (arma != null) arma.FijarLado(izquierda);
    }

    /// <summary>Gira el arma hacia el objetivo (ratón, stick o jefe).</summary>
    void ActualizarCabeza()
    {
        if (Entrada == null) return;
        if (cabeza == null && arma == null) return;

        Vector2 facing = mirandoIzquierda ? Vector2.left : Vector2.right;
        Vector3 origen = arma != null ? arma.PosicionPunta : (cabeza.pivoteCabeza != null ? cabeza.pivoteCabeza.position : transform.position);

        Transform objetivoAuto = JefeFlotante.Activo != null ? JefeFlotante.Activo.transform : null;
        Vector2 dir = Entrada.DireccionAim(origen, facing, objetivoAuto);

        // El disparo sale del arma: la cabeza se queda quieta para que el recorte
        // del cuerpo y de la cabeza siga encajando como en el fotograma original.
        if (arma != null) arma.Apuntar(dir);
        if (cabeza != null) cabeza.Relajar();

        // Si no se mueve, el cuerpo se gira hacia donde apunta (como un tirador)
        if (Mathf.Abs(h) < 0.01f && Mathf.Abs(dir.x) > 0.12f)
        {
            bool izquierda = dir.x < 0f;
            if (izquierda != mirandoIzquierda)
            {
                mirandoIzquierda = izquierda;
                FijarLado(mirandoIzquierda);
            }
        }
    }

    /// <summary>Dispara una bola de fuego desde el cañón del arma. Público también para pruebas.</summary>
    public void Disparar()
    {
        if (bolaFuegoPrefab == null || cargas <= 0 || enfriando) return;

        Vector2 dir;
        Vector3 origen;

        if (arma != null && arma.punta != null)
        {
            dir = arma.DireccionCanon;
            origen = arma.PosicionPunta;
        }
        else if (cabeza != null && cabeza.boca != null)
        {
            // Reserva: si no hay arma colocada, se dispara desde la boca
            dir = cabeza.DireccionBoca;
            origen = cabeza.boca.position;
        }
        else
        {
            return;
        }

        if (dir.sqrMagnitude < 0.0001f) dir = mirandoIzquierda ? Vector2.left : Vector2.right;
        dir.Normalize();

        GameObject go = Instantiate(bolaFuegoPrefab, origen, Quaternion.identity);

        Bala bala = go.GetComponent<Bala>();
        if (bala != null)
        {
            bala.equipo = Equipo.Jugador;
            bala.velocidad = velocidadProyectil;
            bala.vida = vidaProyectil;
            bala.danio = danio;
            bala.Lanzar(dir);
        }

        EfectosJuego.Fogonazo(origen, dir, 0.55f);
        AudioJuego.SonidoDisparo();

        cargas--;
    }

    void IniciarDash()
    {
        float dir = h;
        if (Mathf.Abs(dir) < 0.01f) dir = mirandoIzquierda ? -1f : 1f;

        direccionDash = Mathf.Sign(dir);
        mirandoIzquierda = direccionDash < 0f;
        FijarLado(mirandoIzquierda);

        finDash = Time.time + duracionDash;
        proximoDash = Time.time + duracionDash + enfriamientoDash;

        if (animador != null) animador.SetTrigger(hashDash);

        AudioJuego.SonidoDash();
        EfectosMovimiento.PolvoDash(PosicionPies() + Vector3.up * 0.5f, direccionDash);
        proximaEstela = 0f;

        if (salud != null) salud.invulnerable = true;
        Invoke("FinInvulnerabilidadDash", duracionDash);
    }

    void FinInvulnerabilidadDash()
    {
        if (salud != null && !muerto) salud.invulnerable = false;
    }

    void IniciarEnfriamiento()
    {
        if (enfriando || cargas >= cargasMaximas) return;

        enfriando = true;
        inicioEnfriamiento = Time.time;
    }

    void TerminarEnfriamiento()
    {
        enfriando = false;
        cargas = cargasMaximas;
    }

    void AlMorir()
    {
        muerto = true;
        controlActivo = false;
        CancelInvoke();

        if (salud != null) salud.invulnerable = true;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        if (animador != null) animador.SetTrigger(hashMuerto);
        if (cabeza != null) cabeza.Relajar();
        if (arma != null) arma.Relajar();

        // Con dos jugadores solo se pierde al caer todos
        bool quedanVivos = false;
        for (int i = 0; i < Jugadores.Todos.Count; i++)
        {
            PlayerController j = Jugadores.Todos[i];
            if (j != null && j != this && !j.Muerto) { quedanVivos = true; break; }
        }

        if (!quedanVivos && HUDJuego.Instancia != null) HUDJuego.Instancia.MostrarDerrota();
    }

    /// <summary>El jefe llama a esto cuando la plataforma se destruye.</summary>
    public void ActivarCaidaLibre()
    {
        if (caidaLibre) return;

        caidaLibre = true;

        // El dragón se queda a esta altura: lo que cae es el escenario, no él.
        alturaCaida = rb != null ? rb.position.y : transform.position.y;

        CaidaInfinita.ActivarGlobal();
    }

    // ------------------------------------------------------------ Red
    /// <summary>En el cliente: este dragón deja de simularse y solo se pinta.</summary>
    public void PrepararComoRemoto()
    {
        controlActivo = false;
        controladoPorRed = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    /// <summary>Coloca el dragón según el estado que llega del host.</summary>
    public void AplicarEstadoRemoto(Vector2 pos, float velocidadX, bool izquierda, Vector2 aim)
    {
        if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
            rb.MovePosition(pos);
        else
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);

        mirandoIzquierda = izquierda;
        FijarLado(izquierda);

        if (cabeza != null)
        {
            cabeza.Apuntar(aim);
        }

        if (arma != null) arma.Apuntar(aim);

        if (animador != null) animador.SetFloat(hashVelocidad, Mathf.Abs(velocidadX));
    }

    /// <summary>Velocidad horizontal actual (para enviarla por red).</summary>
    public float VelocidadHorizontal
    {
        get { return rb != null ? rb.linearVelocity.x : 0f; }
    }

    /// <summary>Dirección actual de la boca (para enviarla por red).</summary>
    public Vector2 DireccionActual
    {
        get { return cabeza != null ? cabeza.DireccionBoca : Vector2.right; }
    }
}
