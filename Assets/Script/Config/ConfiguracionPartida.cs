using UnityEngine;

/// <summary>Modo de partida elegido en el menú principal.</summary>
public enum ModoJuego
{
    UnJugador,
    DosJugadores,
    OnlineHost,
    OnlineCliente
}

/// <summary>
/// Datos que viajan del menú "Spirit Dragon" a la arena: cuántos jugadores,
/// con qué dispositivo juega cada uno y si la partida es online.
/// También guarda los ajustes (volumen / pantalla completa).
/// </summary>
public static class ConfiguracionPartida
{
    public const int PuertoPorDefecto = 7777;

    public const string EscenaMenu = "MenuPrincipal";
    public const string EscenaArena = "Nivel1";   // primera escena de la partida: Nivel1 > Nivel2 (Rey Cerdo) > Nivel3 (montaña) > Boss

    public static ModoJuego Modo = ModoJuego.UnJugador;
    public static TipoDispositivo Dispositivo1 = TipoDispositivo.TecladoRaton;
    public static TipoDispositivo Dispositivo2 = TipoDispositivo.Mando;

    public static string Ip = "127.0.0.1";
    public static int Puerto = PuertoPorDefecto;

    public static bool EsOnline
    {
        get { return Modo == ModoJuego.OnlineHost || Modo == ModoJuego.OnlineCliente; }
    }

    public static bool EsHost
    {
        get { return Modo == ModoJuego.OnlineHost; }
    }

    public static bool HayDosJugadores
    {
        get { return Modo == ModoJuego.DosJugadores || EsOnline; }
    }

    // --- Ajustes ---
    public static float Volumen = 1f;
    public static bool PantallaCompleta = true;
    public const float BrilloMinimo = 0.3f;
    public static float Brillo = 1f;   // 1 = normal, BrilloMinimo = lo más oscuro

    public static void Cargar()
    {
        Volumen = PlayerPrefs.GetFloat("skywars_volumen", 1f);
        Brillo = Mathf.Clamp(PlayerPrefs.GetFloat("skywars_brillo", 1f), BrilloMinimo, 1f);
        PantallaCompleta = PlayerPrefs.GetInt("skywars_pantalla", 1) == 1;
        AplicarAjustes();
    }

    public static void Guardar()
    {
        PlayerPrefs.SetFloat("skywars_volumen", Volumen);
        PlayerPrefs.SetFloat("skywars_brillo", Brillo);
        PlayerPrefs.SetInt("skywars_pantalla", PantallaCompleta ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void AplicarAjustes()
    {
        AudioListener.volume = Mathf.Clamp01(Volumen);
        Screen.fullScreen = PantallaCompleta;
        BrilloGlobal.Actualizar();
    }
}
