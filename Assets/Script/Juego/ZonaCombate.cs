using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Zona de pelea: al entrar el jugador la cámara se queda quieta en el centro, aparecen paredes
/// invisibles en los lados y salen oleadas de enemigos. Al derrotarlos todos, la cámara se libera,
/// se dan puntos de bonus, aparece el cofre de recompensa y se activan los objetos indicados
/// (por ejemplo la bandera del final del nivel).
/// El tamaño de la zona es el del BoxCollider2D (trigger) de este objeto; úsala con un GeneradorEnemigos
/// en el mismo objeto (sin empezar al entrar).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(GeneradorEnemigos))]
public class ZonaCombate : MonoBehaviour
{
    public static ZonaCombate Actual { get; private set; }

    [Header("Pelea")]
    public string avisoInicio = "¡PELEA!";
    public string avisoFin = "¡ZONA DESPEJADA!";
    public int puntosBonus = 200;

    [Header("Paredes")]
    public PhysicsMaterial2D materialPared;

    [Header("Al terminar")]
    public GameObject recompensaPrefab;
    public Vector2 posicionRecompensa;
    [Tooltip("Objetos que se activan al despejar la zona (la bandera, una puerta...).")]
    public GameObject[] activarAlTerminar;

    GeneradorEnemigos generador;
    BoxCollider2D area;
    bool activa;
    bool terminada;
    GameObject paredes;
    Transform foco;
    Transform seguimientoOriginal;
    CinemachineCamera cine;
    CamaraSeguir camara;

    /// <summary>Rectángulo de la zona en coordenadas del mundo.</summary>
    public Rect Limites
    {
        get
        {
            Bounds b = area != null ? area.bounds : new Bounds(transform.position, new Vector3(16f, 9f, 0f));
            return new Rect(b.min.x, b.min.y, b.size.x, b.size.y);
        }
    }

    void Awake()
    {
        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true;

        generador = GetComponent<GeneradorEnemigos>();
        generador.empezarAlEntrarJugador = false;
        generador.empezarAlCargar = false;

        // Mientras la zona no se despeja, lo que tiene que aparecer después está oculto
        if (activarAlTerminar != null)
            foreach (GameObject g in activarAlTerminar)
                if (g != null) g.SetActive(false);
    }

    void OnTriggerStay2D(Collider2D otro)
    {
        if (activa || terminada) return;

        PlayerController j = otro.GetComponentInParent<PlayerController>();
        if (j == null || j.Muerto) return;

        // Solo empieza cuando el jugador está bien dentro, para que las paredes no lo empujen
        Rect r = Limites;
        float x = j.transform.position.x;
        if (x < r.xMin + 2.5f || x > r.xMax - 2.5f) return;

        Empezar();
    }

    void Empezar()
    {
        activa = true;
        Actual = this;

        CrearParedes();
        FijarCamara();

        generador.alTerminarTodo.AddListener(Terminar);
        generador.Iniciar();

        if (HUDJuego.Instancia != null) HUDJuego.Instancia.MostrarAviso(avisoInicio, 2f);
        AudioJuego.SonidoInvocacion();
    }

    void Terminar()
    {
        if (!activa) return;
        activa = false;
        terminada = true;
        if (Actual == this) Actual = null;

        if (paredes != null) Destroy(paredes);
        LiberarCamara();

        Puntuacion.Sumar(puntosBonus, transform.position + Vector3.up, new Color(0.6f, 1f, 0.7f, 1f));
        AudioJuego.SonidoCuracion();

        if (HUDJuego.Instancia != null) HUDJuego.Instancia.MostrarAviso(avisoFin, 2.5f);

        if (recompensaPrefab != null)
            Instantiate(recompensaPrefab, new Vector3(posicionRecompensa.x, posicionRecompensa.y, 0f), Quaternion.identity);

        if (activarAlTerminar != null)
            foreach (GameObject g in activarAlTerminar)
                if (g != null) g.SetActive(true);
    }

    void OnDestroy()
    {
        if (Actual == this) Actual = null;
    }

    // ------------------------------------------------------------ paredes y cámara
    void CrearParedes()
    {
        Rect r = Limites;

        paredes = new GameObject("ParedesZona");
        paredes.transform.position = Vector3.zero;

        float alto = r.height + 40f;
        float centroY = r.center.y;

        CrearPared(r.xMin - 0.5f, centroY, alto);
        CrearPared(r.xMax + 0.5f, centroY, alto);
    }

    void CrearPared(float x, float y, float alto)
    {
        GameObject g = new GameObject("Pared");
        g.transform.SetParent(paredes.transform, false);
        g.transform.position = new Vector3(x, y, 0f);

        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(1f, alto);
        if (materialPared != null) bc.sharedMaterial = materialPared;
    }

    void FijarCamara()
    {
        Vector3 centro = new Vector3(Limites.center.x, Limites.center.y, 0f);

        camara = FindAnyObjectByType<CamaraSeguir>();
        if (camara != null)
        {
            camara.FijarEn(centro);
            return;
        }

        cine = FindAnyObjectByType<CinemachineCamera>();
        if (cine == null) return;

        foco = new GameObject("CamaraZona").transform;
        foco.position = centro;

        seguimientoOriginal = cine.Follow;
        cine.Follow = foco;
    }

    void LiberarCamara()
    {
        if (camara != null) camara.Liberar();

        if (cine != null) cine.Follow = seguimientoOriginal;
        if (foco != null) Destroy(foco.gameObject);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);
        BoxCollider2D b = GetComponent<BoxCollider2D>();
        if (b != null) Gizmos.DrawWireCube(b.bounds.center, b.bounds.size);
    }
}
