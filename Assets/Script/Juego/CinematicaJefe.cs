using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cinemática de entrada del jefe, antes de que empiece la pelea:
/// la cámara sube al cielo, el jefe se anuncia entre explosiones y baja hasta su
/// posición de combate. Cuando termina se devuelve el control al dragón, se activa
/// la IA del jefe y arranca el tema de batalla.
/// Vive en un GameObject vacío de la escena ("CinematicaJefe").
/// </summary>
public class CinematicaJefe : MonoBehaviour
{
    [Header("Actores")]
    public JefeFlotante jefe;
    public PlayerController jugador;
    public CamaraSeguir camara;

    [Header("Puesta en escena")]
    [Tooltip("Altura sobre su posición final desde la que aparece el jefe.")]
    public float alturaAparicion = 6.5f;
    public float duracionSubida = 1.1f;
    public float esperaAntesDeBajar = 0.6f;
    public float duracionDescenso = 2.3f;
    public float duracionVuelta = 1.0f;
    [Tooltip("Temblor de cámara durante el descenso.")]
    public float temblor = 0.06f;

    [Header("Textos")]
    public string textoAparicion = "EL CIELO SE OSCURECE…";
    public string textoBatalla = "¡EL DEVORADOR DE CIELOS!\nLA BATALLA COMIENZA";

    [Header("Cámara durante la pelea")]
    [Tooltip("La cámara se queda quieta en el centro de la arena mientras dura la pelea.")]
    public bool camaraFija = true;
    public Vector2 centroCamara = new Vector2(0f, 1.7f);

    [Header("Saltar")]
    public bool sePuedeSaltar = true;

    Camera cam;
    Transform foco;
    Transform objetivoCamaraOriginal;
    bool saltar;

    void Awake()
    {
        cam = Camera.main;

        if (camara == null && cam != null) camara = cam.GetComponent<CamaraSeguir>();
        if (jefe == null) jefe = JefeFlotante.Activo;
        if (jugador == null && Jugadores.Todos.Count > 0) jugador = Jugadores.Todos[0];
    }

    void Start()
    {
        if (jefe == null || jugador == null)
        {
            Debug.LogWarning("CinematicaJefe: falta el jefe o el jugador; se empieza a pelear directamente.");
            if (jugador != null) jugador.controlActivo = true;
            enabled = false;
            return;
        }

        StartCoroutine(Secuencia());
    }

    void Update()
    {
        if (!sePuedeSaltar) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame) saltar = true;
    }

    // ------------------------------------------------------------------ puesta en escena
    IEnumerator Secuencia()
    {
        // Nadie se mueve mientras dura la escena
        jugador.controlActivo = false;

        Rigidbody2D cuerpo = jefe.GetComponent<Rigidbody2D>();
        jefe.enabled = false;

        if (cuerpo != null)
        {
            cuerpo.linearVelocity = Vector2.zero;
            cuerpo.bodyType = RigidbodyType2D.Kinematic;
            cuerpo.interpolation = RigidbodyInterpolation2D.None;
        }

        Vector3 destinoJefe = jefe.transform.position;
        Vector3 aparicion = destinoJefe + Vector3.up * alturaAparicion;

        jefe.transform.position = aparicion;
        if (cuerpo != null) cuerpo.position = aparicion;

        // La cámara pasa a mirar la escena en lugar de al dragón
        GameObject goFoco = new GameObject("FocoCinematica");
        foco = goFoco.transform;
        foco.position = jugador.transform.position;

        if (camara != null)
        {
            objetivoCamaraOriginal = camara.objetivo;
            camara.objetivo = foco;
        }

        Aviso(textoAparicion, 2.4f);

        yield return Mover(aparicion, duracionSubida);

        if (saltar) { Terminar(destinoJefe, cuerpo); yield break; }

        // El jefe se anuncia
        AudioJuego.SonidoInvocacion();
        EfectosJuego.ExplosionJefe(aparicion, 2.2f);

        yield return Esperar(esperaAntesDeBajar);

        if (saltar) { Terminar(destinoJefe, cuerpo); yield break; }

        // Descenso con estela de explosiones
        float t = 0f;
        while (t < duracionDescenso)
        {
            t += Time.fixedDeltaTime;

            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duracionDescenso));
            Vector3 p = Vector3.Lerp(aparicion, destinoJefe, k);

            if (cuerpo != null) cuerpo.MovePosition(p);
            else jefe.transform.position = p;

            Vector2 desvio = Random.insideUnitCircle * temblor;
            if (foco != null) foco.position = p + new Vector3(desvio.x, desvio.y, 0f);

            if (Random.value < 0.14f)
                EfectosJuego.ExplosionJefe(p, Random.Range(0.7f, 1.5f));

            yield return new WaitForFixedUpdate();
        }

        // Aterrizaje
        if (cuerpo != null) cuerpo.position = destinoJefe;
        jefe.transform.position = destinoJefe;

        EfectosJuego.ExplosionJefe(destinoJefe, 2.8f);
        AudioJuego.SonidoExplosionJefe();
        Aviso(textoBatalla, 3f);

        if (foco != null) foco.position = destinoJefe;

        yield return Esperar(0.5f);

        if (saltar) { Terminar(destinoJefe, cuerpo); yield break; }

        // La cámara vuelve al dragón
        yield return Mover(jugador.transform.position, duracionVuelta);

        Terminar(destinoJefe, cuerpo);
    }

    void Terminar(Vector3 destinoJefe, Rigidbody2D cuerpo)
    {
        if (jefe != null)
        {
            jefe.transform.position = destinoJefe;

            if (cuerpo != null)
            {
                cuerpo.bodyType = RigidbodyType2D.Dynamic;
                cuerpo.interpolation = RigidbodyInterpolation2D.Interpolate;
                cuerpo.position = destinoJefe;
            }

            jefe.enabled = true;
        }

        if (jugador != null) jugador.controlActivo = true;

        if (camara != null)
        {
            camara.objetivo = objetivoCamaraOriginal != null ? objetivoCamaraOriginal : (jugador != null ? jugador.transform : null);
            if (camaraFija) camara.FijarEn(new Vector3(centroCamara.x, centroCamara.y, 0f));
        }

        if (foco != null) Destroy(foco.gameObject);

        AudioJuego.MusicaBatalla();

        enabled = false;
    }

    // ------------------------------------------------------------------ ayudas
    IEnumerator Mover(Vector3 destino, float duracion)
    {
        if (foco == null) yield break;

        Vector3 desde = foco.position;
        float t = 0f;

        while (t < duracion)
        {
            if (saltar) yield break;

            t += Time.deltaTime;
            foco.position = Vector3.Lerp(desde, destino, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duracion)));
            yield return null;
        }

        foco.position = destino;
    }

    IEnumerator Esperar(float segundos)
    {
        float t = 0f;

        while (t < segundos)
        {
            if (saltar) yield break;

            t += Time.deltaTime;
            yield return null;
        }
    }

    static void Aviso(string mensaje, float duracion)
    {
        if (HUDJuego.Instancia != null) HUDJuego.Instancia.MostrarAviso(mensaje, duracion);
    }
}
