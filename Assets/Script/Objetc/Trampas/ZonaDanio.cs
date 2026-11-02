using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class ZonaDanio : MonoBehaviour
{
    public enum Tipo { Puas, Trampa, Abismo, Lava }

    [Header("Tipo")]
    [Tooltip("Puas y Trampa: hieren y rebotan. Abismo y Lava: hieren mucho y te devuelven al último suelo seguro.")]
    public Tipo tipo = Tipo.Puas;
    [Tooltip("Si se apaga, la zona no hace daño (lo usa PuasEmergentes).")]
    public bool activa = true;

    [Header("Daño al jugador")]
    public float danio = 20f;
    [Tooltip("Ignora el daño de arriba y baja la vida a 0.")]
    public bool mataDeUnGolpe = false;
    [Tooltip("Pausa antes de poder dañar otra vez al mismo objetivo.")]
    public float segundosEntreGolpes = 1f;

    [Header("Puas / Trampa")]
    [Tooltip("Rebote vertical al ser herido. El movimiento horizontal lo controla el jugador.")]
    public float rebote = 9f;

    [Header("Abismo / Lava")]
    [Tooltip("Opcional: punto fijo de reaparición. Si está vacío se usa el último suelo seguro.")]
    public Transform puntoReaparicion;

    [Header("Enemigos")]
    public bool afectaEnemigos = false;
    public float danioEnemigos = 9999f;

    [Header("Eventos")]
    [Tooltip("Se lanza cada vez que hiere a alguien (conecta aquí un sonido).")]
    public UnityEvent alHerir;

    readonly Dictionary<Salud, float> proximoGolpe = new Dictionary<Salud, float>();

    public bool DevuelveAlSuelo { get { return tipo == Tipo.Abismo || tipo == Tipo.Lava; } }

    void Reset()
    {
        Collider2D c = GetComponent<Collider2D>();
        c.isTrigger = true;
        AplicarValoresSugeridos();
    }

    /// <summary>Clic derecho en el componente > "Aplicar valores sugeridos del tipo".</summary>
    [ContextMenu("Aplicar valores sugeridos del tipo")]
    public void AplicarValoresSugeridos()
    {
        switch (tipo)
        {
            case Tipo.Puas: danio = 20f; segundosEntreGolpes = 1f; rebote = 9f; break;
            case Tipo.Trampa: danio = 25f; segundosEntreGolpes = 1f; rebote = 9f; break;
            case Tipo.Abismo: danio = 50f; segundosEntreGolpes = 1.5f; break;
            case Tipo.Lava: danio = 60f; segundosEntreGolpes = 1.5f; break;
        }
    }

    void OnTriggerEnter2D(Collider2D otro) { Procesar(otro); }
    void OnTriggerStay2D(Collider2D otro) { Procesar(otro); }

    void Procesar(Collider2D otro)
    {
        if (!activa) return;

        Salud s = otro.GetComponentInParent<Salud>();
        if (s == null || !s.Vivo) return;

        PlayerController pc = otro.GetComponentInParent<PlayerController>();
        if (pc != null && pc.controladoPorRed) return;   // en online manda quien simula el mundo

        bool esJugador = s.equipo == Equipo.Jugador;
        if (!esJugador && !afectaEnemigos) return;

        float ahora = Time.time;
        float proximo;
        if (proximoGolpe.TryGetValue(s, out proximo) && ahora < proximo) return;
        proximoGolpe[s] = ahora + segundosEntreGolpes;

        float d;
        if (esJugador) d = mataDeUnGolpe ? s.VidaMaxima + 1f : danio;
        else d = danioEnemigos;

        s.RecibirDanio(d);
        alHerir.Invoke();

        if (!esJugador || !s.Vivo) return;   // un enemigo o un jugador muerto no reaparece

        if (DevuelveAlSuelo) Reaparecer(s);
        else Rebotar(s);
    }

    void Reaparecer(Salud s)
    {
        UltimoSuelo suelo = s.GetComponent<UltimoSuelo>();
        if (suelo == null)
        {
            Debug.LogWarning("ZonaDanio: el jugador no tiene el componente UltimoSuelo, no puedo devolverlo.", this);
            return;
        }

        Vector2 destino = puntoReaparicion != null ? (Vector2)puntoReaparicion.position : suelo.PosicionSegura;
        suelo.Teletransportar(destino);
    }

    void Rebotar(Salud s)
    {
        Rigidbody2D rb = s.GetComponent<Rigidbody2D>();
        if (rb == null || rebote <= 0f) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, rebote);
    }

    // Siempre visible en la vista de escena
    void OnDrawGizmos()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return;

        Color color;
        switch (tipo)
        {
            case Tipo.Abismo: color = new Color(1f, 0.1f, 0.1f); break;
            case Tipo.Lava: color = new Color(1f, 0.75f, 0f); break;
            case Tipo.Trampa: color = new Color(0.9f, 0.2f, 1f); break;
            default: color = new Color(1f, 0.5f, 0f); break;
        }
        if (!activa) color = new Color(0.5f, 0.5f, 0.5f);

        Bounds b = c.bounds;
        Gizmos.color = new Color(color.r, color.g, color.b, 0.18f);
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = color;
        Gizmos.DrawWireCube(b.center, b.size);
    }
}