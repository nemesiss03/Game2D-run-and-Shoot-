using UnityEngine;

/// <summary>
/// Entrada que mueve al jugador sola (cinemáticas: caminar de un escenario a otro).
/// Solo camina; no salta, no dispara ni hace dash.
/// </summary>
public class EntradaAutomatica : EntradaJugador
{
    public float horizontal = 1f;

    public override float Horizontal() { return horizontal; }
    public override bool SaltoPulsado() { return false; }
    public override bool DashPulsado() { return false; }
    public override bool DisparoMantenido() { return false; }
    public override bool EnfriamientoPulsado() { return false; }
    public override bool TieneAimManual { get { return false; } }

    public override Vector2 DireccionAim(Vector3 origen, Vector2 facing, Transform objetivoAuto)
    {
        return facing.sqrMagnitude < 0.01f ? Vector2.right : facing.normalized;
    }
}
