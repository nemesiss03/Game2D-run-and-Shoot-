using UnityEngine;

/// <summary>
/// Punto de entrada de la arena. Lee lo que se eligió en el menú "Spirit Dragon"
/// y monta la partida: dispositivos de cada jugador, segundo dragón y red.
/// </summary>
public class ArranquePartida : MonoBehaviour
{
    [Header("Jugadores")]
    public GameObject prefabJugador;
    public PlayerController jugador1;
    public Vector3 posicionJugador2 = new Vector3(-1.6f, 0.5f, 0f);

    [Header("HUD")]
    public HUDJuego hud;
    public GameObject grupoJugador2;

    [Header("Cámara")]
    public CamaraSeguir camara;

    PlayerController jugador2;

    void Start()
    {
        ConfiguracionPartida.Cargar();
        ConfiguracionPartida.AplicarAjustes();

        int mando1 = 0;
        int mando2 = (ConfiguracionPartida.Dispositivo1 == TipoDispositivo.Mando &&
                      ConfiguracionPartida.Dispositivo2 == TipoDispositivo.Mando) ? 1 : 0;

        if (jugador1 == null) jugador1 = GameObject.FindGameObjectWithTag("Player") != null
            ? GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>()
            : null;

        if (jugador1 != null) jugador1.Configurar(ConfiguracionPartida.Dispositivo1, mando1, 1);

        if (ConfiguracionPartida.HayDosJugadores) CrearJugador2(mando2);

        if (grupoJugador2 != null) grupoJugador2.SetActive(jugador2 != null);

        if (camara != null && jugador2 != null) camara.objetivo2 = jugador2.transform;

        if (ConfiguracionPartida.EsOnline) ConfigurarRed();
    }

    void CrearJugador2(int indiceMando)
    {
        if (prefabJugador == null)
        {
            Debug.LogWarning("ArranquePartida: falta el prefab del jugador, no se puede crear el jugador 2.");
            return;
        }

        GameObject go = Instantiate(prefabJugador, posicionJugador2, Quaternion.identity);
        go.name = "Player2";

        jugador2 = go.GetComponent<PlayerController>();
        if (jugador2 == null) return;

        jugador2.Configurar(ConfiguracionPartida.Dispositivo2, indiceMando, 2);

        // El segundo dragón no es el objeto etiquetado como Player para no romper búsquedas
        go.tag = "Untagged";
        go.layer = 7;

        if (hud != null)
        {
            hud.jugador2 = jugador2;
            hud.saludJugador2 = go.GetComponent<Salud>();
        }
    }

    void ConfigurarRed()
    {
        SincronizacionRed red = GetComponent<SincronizacionRed>();
        if (red == null) red = gameObject.AddComponent<SincronizacionRed>();

        red.jugadorLocal = jugador1;
        red.jugadorRemoto = jugador2;
        red.jefe = JefeFlotante.Activo;
        red.spriteBala = spriteBala;
        red.escalaBala = escalaBala;
    }

    [Header("Bola de fuego (visual para el cliente)")]
    public Sprite spriteBala;
    public float escalaBala = 0.5f;
}
