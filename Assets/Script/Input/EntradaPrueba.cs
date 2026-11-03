using UnityEngine;

/// <summary>
/// Entrada controlada por código para probar el juego sin teclado (la usan los tests desde el editor).
/// Se asigna con jugador.AsignarEntrada(new EntradaPrueba()).
/// </summary>
public class EntradaPrueba : EntradaJugador
{
    public float horizontal;
    public float vertical;
    public bool saltoMantenido;
    public bool disparo;
    public Vector2 apuntar = Vector2.right;

    bool pulsarSalto;
    bool pulsarDash;

    /// <summary>Pulsa el salto en el siguiente fotograma.</summary>
    public void PulsarSalto() { pulsarSalto = true; saltoMantenido = true; }
    public void SoltarSalto() { saltoMantenido = false; }
    public void PulsarDash() { pulsarDash = true; }

    public override float Horizontal() { return horizontal; }
    public override float Vertical() { return vertical; }

    public override bool SaltoPulsado()
    {
        bool v = pulsarSalto;
        pulsarSalto = false;
        return v;
    }

    public override bool SaltoMantenido() { return saltoMantenido; }

    public override bool DashPulsado()
    {
        bool v = pulsarDash;
        pulsarDash = false;
        return v;
    }

    public override bool DisparoMantenido() { return disparo; }
    public override bool EnfriamientoPulsado() { return false; }
    public override bool TieneAimManual { get { return true; } }

    public override Vector2 DireccionAim(Vector3 origen, Vector2 facing, Transform objetivoAuto)
    {
        return apuntar;
    }
}
