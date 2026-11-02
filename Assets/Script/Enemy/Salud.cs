using System;
using UnityEngine;
using UnityEngine.Events;

public enum Equipo { Jugador, Enemigo }

/// <summary>
/// Vida genérica para jugador, jefe y esbirros. Avisa a la UI mediante eventos.
/// </summary>
public class Salud : MonoBehaviour
{
    [Header("Configuración")]
    public Equipo equipo = Equipo.Enemigo;
    public float vidaMaxima = 10f;
    public bool destruirAlMorir = true;
    [Tooltip("Mientras esté activo no recibe daño (por ejemplo durante el dash).")]
    public bool invulnerable = false;
    [Tooltip("Puntos que da al morir (solo enemigos). El jefe da 1000 si se deja en 100.")]
    public int puntosAlMorir = Puntuacion.PuntosEnemigo;

    [Header("Eventos")]
    public UnityEvent alMorir = new UnityEvent();
    public UnityEvent<float, float> alCambiarVida = new UnityEvent<float, float>();

    public float VidaActual { get; private set; }
    public float VidaMaxima { get { return vidaMaxima; } }
    public bool Vivo { get { return VidaActual > 0f; } }
    public float Fraccion { get { return vidaMaxima <= 0f ? 0f : Mathf.Clamp01(VidaActual / vidaMaxima); } }

    /// <summary>Vida actual, vida máxima.</summary>
    public event Action<float, float> CambioVida;
    public event Action Muerte;

    bool esJefe;
    bool murio;

    void Awake()
    {
        VidaActual = vidaMaxima;
        esJefe = GetComponent<JefeFlotante>() != null;
        if (esJefe && puntosAlMorir == Puntuacion.PuntosEnemigo) puntosAlMorir = 1000;
    }

    void Start()
    {
        Notificar();
    }

    public void RecibirDanio(float danio)
    {
        if (danio <= 0f || invulnerable || VidaActual <= 0f) return;

        VidaActual = Mathf.Max(0f, VidaActual - danio);
        Notificar();

        SonarImpacto();

        if (VidaActual <= 0f) Morir();
    }

    void SonarImpacto()
    {
        if (equipo == Equipo.Jugador)
        {
            AudioJuego.SonidoDanioJugador();
            return;
        }

        if (esJefe) AudioJuego.SonidoImpactoJefe();
        else AudioJuego.SonidoImpactoEsbirro();
    }

    public void Curar(float cantidad)
    {
        if (cantidad <= 0f || VidaActual <= 0f) return;

        VidaActual = Mathf.Min(vidaMaxima, VidaActual + cantidad);
        Notificar();
    }

    /// <summary>Fija la vida desde la red (no dispara muerte, el host manda).</summary>
    public void FijarVidaRemota(float valor)
    {
        VidaActual = Mathf.Clamp(valor, 0f, vidaMaxima);
        Notificar();
    }

    public void Morir()
    {
        if (murio) return;
        murio = true;

        if (VidaActual > 0f) VidaActual = 0f;

        if (equipo == Equipo.Enemigo)
        {
            EfectoDestruccion();
            Puntuacion.Sumar(puntosAlMorir, transform.position, new Color(1f, 0.55f, 0.35f, 1f));
        }

        if (Muerte != null) Muerte();
        if (alMorir != null) alMorir.Invoke();

        if (destruirAlMorir) Destroy(gameObject);
    }

    /// <summary>Explosión y sonido de destrucción de un enemigo.</summary>
    void EfectoDestruccion()
    {
        Vector3 centro = transform.position;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) centro = sr.bounds.center;

        if (esJefe)
        {
            AudioJuego.SonidoExplosionJefe();
            EfectosJuego.ExplosionJefe(centro, 2.6f);
            return;
        }

        AudioJuego.SonidoExplosionEsbirro();
        EfectosJuego.ExplosionEsbirro(centro);
    }

    void Notificar()
    {
        if (CambioVida != null) CambioVida(VidaActual, vidaMaxima);
        if (alCambiarVida != null) alCambiarVida.Invoke(VidaActual, vidaMaxima);
    }
}
