using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// HUD del combate "Sky Wars": vida de los jugadores, vida del jefe,
/// cargas de fuego con enfriamiento, avisos y pantalla final.
/// </summary>
public class HUDJuego : MonoBehaviour
{
    public static HUDJuego Instancia { get; private set; }

    [Header("Jugador 1")]
    public Salud saludJugador;
    public PlayerController jugador;
    public Image barraJugador;
    public Text textoVidaJugador;

    [Header("Jugador 2")]
    public GameObject grupoJugador2;
    public Salud saludJugador2;
    public PlayerController jugador2;
    public Image barraJugador2;
    public Text textoVidaJugador2;

    [Header("Jefe")]
    public Salud saludJefe;
    public Image barraJefe;
    public Text textoVidaJefe;
    public Text textoNombreJefe;

    [Header("Fuego")]
    public Text textoMunicion;
    public Text textoEstadoArma;

    [Header("Mensajes")]
    public Text textoAviso;
    public GameObject panelFinal;
    public Text textoFinal;
    public Text textoReinicio;

    float finAviso;
    bool finalMostrado;
    float momentoFinal;

    void Awake()
    {
        Instancia = this;
    }

    void Start()
    {
        if (panelFinal != null) panelFinal.SetActive(false);
        if (textoAviso != null) textoAviso.text = string.Empty;
        if (textoEstadoArma != null) textoEstadoArma.text = string.Empty;
        if (grupoJugador2 != null) grupoJugador2.SetActive(jugador2 != null);
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    void Update()
    {
        ActualizarBarras();
        ActualizarFuego();

        if (finAviso > 0f && Time.time >= finAviso)
        {
            finAviso = 0f;
            if (textoAviso != null) textoAviso.text = string.Empty;
        }

        var kb = Keyboard.current;

        if (finalMostrado && Time.time - momentoFinal > 1f)
        {
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                Reiniciar();
        }

        // ESC vuelve al menú principal
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            SceneManager.LoadScene(ConfiguracionPartida.EscenaMenu);
    }

    void ActualizarBarras()
    {
        PintarBarra(barraJugador, saludJugador, textoVidaJugador);
        if (jugador2 != null) PintarBarra(barraJugador2, saludJugador2, textoVidaJugador2);

        if (barraJefe != null && saludJefe != null)
        {
            barraJefe.fillAmount = saludJefe.Fraccion;

            if (textoVidaJefe != null)
                textoVidaJefe.text = Mathf.CeilToInt(saludJefe.VidaActual) + " / " + Mathf.CeilToInt(saludJefe.VidaMaxima);
        }
    }

    void PintarBarra(Image barra, Salud salud, Text texto)
    {
        if (barra == null || salud == null) return;

        barra.fillAmount = salud.Fraccion;

        if (texto != null)
            texto.text = Mathf.CeilToInt(salud.VidaActual) + " / " + Mathf.CeilToInt(salud.VidaMaxima);
    }

    void ActualizarFuego()
    {
        if (jugador == null) return;

        if (textoMunicion != null)
            textoMunicion.text = jugador.Balas + " / " + jugador.CapacidadCargador;

        if (textoEstadoArma == null) return;

        if (jugador.Recargando)
            textoEstadoArma.text = "ENFRIAMIENTO " + (jugador.ProgresoRecarga * 100f).ToString("0") + "%";
        else if (jugador.Balas <= 0)
            textoEstadoArma.text = "SIN FUEGO";
        else if (jugador.Balas < jugador.CapacidadCargador)
            textoEstadoArma.text = "PULSA R PARA ENFRIAR";
        else
            textoEstadoArma.text = string.Empty;
    }

    public void MostrarAviso(string mensaje, float duracion)
    {
        if (textoAviso == null) return;

        textoAviso.text = mensaje;
        finAviso = Time.time + duracion;
    }

    public void MostrarDerrota()
    {
        MostrarFinal("DERROTADO", new Color(1f, 0.35f, 0.35f, 1f));
    }

    public void MostrarVictoria()
    {
        MostrarFinal("¡VICTORIA!", new Color(0.45f, 1f, 0.55f, 1f));

        if (textoReinicio != null)
            textoReinicio.text = "PUNTOS: " + Puntuacion.Total + "\n" + textoReinicio.text;
    }

    void MostrarFinal(string mensaje, Color color)
    {
        if (finalMostrado) return;

        finalMostrado = true;
        momentoFinal = Time.time;

        if (panelFinal != null) panelFinal.SetActive(true);

        if (textoFinal != null)
        {
            textoFinal.text = mensaje;
            textoFinal.color = color;
        }

        if (textoReinicio != null) textoReinicio.text = "ENTER para reiniciar    ·    ESC para el menú";
        if (textoAviso != null) textoAviso.text = string.Empty;
        finAviso = 0f;
    }

    void Reiniciar()
    {
        Puntuacion.RestaurarCheckpoint();
        Scene escena = SceneManager.GetActiveScene();

        if (escena.buildIndex >= 0) SceneManager.LoadScene(escena.buildIndex);
        else SceneManager.LoadScene(escena.name);
    }
}
