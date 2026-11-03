using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menú principal de "Spirit Dragon": Jugar / Configuración / Salir, con los modos
/// solo PC (1 o 2 jugadores, teclado o mando) y multijugador online (abrir servidor o unirse).
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelPrincipal;
    public GameObject panelJugar;
    public GameObject panelLocal;
    public GameObject panelDispositivos;
    public GameObject panelOnline;
    public GameObject panelConfig;

    [Header("Textos")]
    public Text textoJ1;
    public Text textoJ2;
    public Text textoOnline;
    public Text textoVolumen;

    [Header("Ajustes")]
    public Slider sliderVolumen;
    public Slider sliderBrillo;
    public Text textoBrillo;
    public Toggle togglePantalla;

    [Header("Online")]
    public InputField campoIp;

    bool esperandoRed;
    float finEspera;
    ModoJuego modoPendiente;

    void Awake()
    {
        ConfiguracionPartida.Cargar();

        if (sliderVolumen != null)
        {
            sliderVolumen.value = ConfiguracionPartida.Volumen;
            sliderVolumen.onValueChanged.AddListener(CambiarVolumen);
        }

        if (sliderBrillo != null)
        {
            sliderBrillo.minValue = ConfiguracionPartida.BrilloMinimo;
            sliderBrillo.maxValue = 1f;
            sliderBrillo.value = ConfiguracionPartida.Brillo;
            sliderBrillo.onValueChanged.AddListener(CambiarBrillo);
        }

        if (togglePantalla != null)
        {
            togglePantalla.isOn = ConfiguracionPartida.PantallaCompleta;
            togglePantalla.onValueChanged.AddListener(CambiarPantalla);
        }

        Mostrar(panelPrincipal);
        RefrescarEtiquetas();
    }

    void Update()
    {
        if (!esperandoRed) return;

        if (textoOnline != null) textoOnline.text = NetJuego.Estado;

        if (NetJuego.Conectado)
        {
            esperandoRed = false;
            ConfiguracionPartida.Modo = modoPendiente;
            SceneManager.LoadScene(ConfiguracionPartida.EscenaArena);
            return;
        }

        if (Time.time > finEspera)
        {
            esperandoRed = false;
            NetJuego.Detener();
            if (textoOnline != null)
                textoOnline.text = string.IsNullOrEmpty(NetJuego.UltimoError)
                    ? "No se pudo conectar. Comprueba la IP y el puerto."
                    : "Error: " + NetJuego.UltimoError;
        }
    }

    // ------------------------------------------------------------ navegación
    public void Mostrar(GameObject panel)
    {
        GameObject[] todos = { panelPrincipal, panelJugar, panelLocal, panelDispositivos, panelOnline, panelConfig };

        for (int i = 0; i < todos.Length; i++)
            if (todos[i] != null) todos[i].SetActive(todos[i] == panel);
    }

    public void IrAInicio() { Mostrar(panelPrincipal); }
    // "Empezar" lleva directo a elegir 1 o 2 jugadores (panelJugar queda para el modo online)
    public void IrAJugar() { Mostrar(panelLocal); }
    public void IrALocal() { Mostrar(panelLocal); }
    public void IrAConfig() { Mostrar(panelConfig); }

    public void IrAOnline()
    {
        Mostrar(panelOnline);

        if (textoOnline != null)
            textoOnline.text = "Tu IP: " + NetJuego.IpLocal() + "   ·   puerto " + ConfiguracionPartida.Puerto;
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------ solo PC
    public void JugarUnJugador()
    {
        ConfiguracionPartida.Modo = ModoJuego.UnJugador;
        ConfiguracionPartida.Dispositivo1 = TipoDispositivo.TecladoRaton;
        Jugar();
    }

    public void IrADosJugadores()
    {
        Mostrar(panelDispositivos);
        RefrescarEtiquetas();
    }

    public void J1Teclado() { ConfiguracionPartida.Dispositivo1 = TipoDispositivo.TecladoRaton; RefrescarEtiquetas(); }
    public void J1Mando() { ConfiguracionPartida.Dispositivo1 = TipoDispositivo.Mando; RefrescarEtiquetas(); }
    public void J2Teclado() { ConfiguracionPartida.Dispositivo2 = TipoDispositivo.Teclado; RefrescarEtiquetas(); }
    public void J2Mando() { ConfiguracionPartida.Dispositivo2 = TipoDispositivo.Mando; RefrescarEtiquetas(); }

    public void ComenzarDosJugadores()
    {
        ConfiguracionPartida.Modo = ModoJuego.DosJugadores;
        Jugar();
    }

    void Jugar()
    {
        Puntuacion.Reiniciar();
        CinematicaIntro.Reiniciar();
        SceneManager.LoadScene(ConfiguracionPartida.EscenaArena);
    }

    // ------------------------------------------------------------ online
    public void AbrirServidor()
    {
        modoPendiente = ModoJuego.OnlineHost;
        ConfiguracionPartida.Modo = ModoJuego.OnlineHost;
        esperandoRed = true;
        finEspera = Time.time + 60f;

        NetJuego.IniciarHost(ConfiguracionPartida.Puerto);

        if (textoOnline != null)
            textoOnline.text = "Servidor abierto en " + NetJuego.IpLocal() + ":" + ConfiguracionPartida.Puerto +
                               "\nEsperando a que se conecte el otro jugador...";
    }

    public void Unirse()
    {
        string ip = campoIp != null && !string.IsNullOrEmpty(campoIp.text) ? campoIp.text.Trim() : "127.0.0.1";

        modoPendiente = ModoJuego.OnlineCliente;
        ConfiguracionPartida.Modo = ModoJuego.OnlineCliente;
        ConfiguracionPartida.Ip = ip;
        esperandoRed = true;
        finEspera = Time.time + 15f;

        NetJuego.IniciarCliente(ip, ConfiguracionPartida.Puerto);

        if (textoOnline != null) textoOnline.text = "Conectando a " + ip + "...";
    }

    public void CancelarRed()
    {
        esperandoRed = false;
        NetJuego.Detener();
        IrAOnline();
    }

    // ------------------------------------------------------------ ajustes
    public void CambiarVolumen(float valor)
    {
        ConfiguracionPartida.Volumen = valor;
        ConfiguracionPartida.AplicarAjustes();
        ConfiguracionPartida.Guardar();

        if (textoVolumen != null) textoVolumen.text = Mathf.RoundToInt(valor * 100f) + "%";
    }

    public void CambiarBrillo(float valor)
    {
        ConfiguracionPartida.Brillo = valor;
        ConfiguracionPartida.AplicarAjustes();
        ConfiguracionPartida.Guardar();

        if (textoBrillo != null) textoBrillo.text = Mathf.RoundToInt(valor * 100f) + "%";
    }

    public void CambiarPantalla(bool valor)
    {
        ConfiguracionPartida.PantallaCompleta = valor;
        ConfiguracionPartida.AplicarAjustes();
        ConfiguracionPartida.Guardar();
    }

    void RefrescarEtiquetas()
    {
        if (textoJ1 != null)
            textoJ1.text = "JUGADOR 1: " + (ConfiguracionPartida.Dispositivo1 == TipoDispositivo.Mando ? "MANDO" : "TECLADO + RATÓN");

        if (textoJ2 != null)
            textoJ2.text = "JUGADOR 2: " + (ConfiguracionPartida.Dispositivo2 == TipoDispositivo.Mando ? "MANDO" : "TECLADO");

        if (textoVolumen != null)
            textoVolumen.text = Mathf.RoundToInt(ConfiguracionPartida.Volumen * 100f) + "%";

        if (textoBrillo != null)
            textoBrillo.text = Mathf.RoundToInt(ConfiguracionPartida.Brillo * 100f) + "%";
    }
}
