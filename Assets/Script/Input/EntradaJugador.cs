using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Con qué dispositivo juega un jugador.</summary>
public enum TipoDispositivo
{
    TecladoRaton,   // A/D + ratón (jugador 1 por defecto)
    Teclado,        // flechas + apuntado automático (jugador 2 en la misma PC)
    Mando           // gamepad
}

/// <summary>
/// Capa fina sobre el Input System para que el mismo PlayerController
/// funcione con teclado+ratón, teclado solo o mando.
/// </summary>
public abstract class EntradaJugador
{
    public abstract float Horizontal();
    public abstract bool SaltoPulsado();
    public virtual bool SaltoMantenido() { return true; }

    /// <summary>Subir (+1) o bajar (-1) durante la caída libre del jefe.</summary>
    public virtual float Vertical() { return 0f; }
    public abstract bool DashPulsado();
    public abstract bool DisparoMantenido();
    public abstract bool EnfriamientoPulsado();
    public abstract bool TieneAimManual { get; }

    /// <summary>Apunta a un punto de la pantalla (el ratón): cerca del jugador ese punto es ambiguo y se necesita una zona muerta.</summary>
    public virtual bool ApuntaAUnPunto { get { return false; } }

    /// <summary>Dirección hacia la que debe apuntar la cabeza del dragón.</summary>
    public abstract Vector2 DireccionAim(Vector3 origen, Vector2 facing, Transform objetivoAuto);

    public static EntradaJugador Crear(TipoDispositivo tipo, int indiceMando)
    {
        switch (tipo)
        {
            case TipoDispositivo.Teclado: return new EntradaTeclado();
            case TipoDispositivo.Mando: return new EntradaMando(indiceMando);
            default: return new EntradaTecladoRaton();
        }
    }

    protected static Vector2 AutoAim(Vector2 facing, Transform objetivoAuto, Vector3 origen)
    {
        if (facing.sqrMagnitude < 0.01f) facing = Vector2.right;

        if (objetivoAuto == null) return facing.normalized;

        // Se apunta al jefe, pero nunca se fuerza un ángulo absurdo hacia atrás
        Vector2 alJefe = (Vector2)objetivoAuto.position - (Vector2)origen;
        if (alJefe.sqrMagnitude < 0.01f) return facing.normalized;

        if (Vector2.Dot(alJefe.normalized, facing.normalized) < -0.2f)
            return (facing.normalized + Vector2.up * 0.6f).normalized;

        return alJefe.normalized;
    }
}

/// <summary>Jugador 1 clásico: mover con A/D, apuntar y disparar con el ratón.</summary>
public class EntradaTecladoRaton : EntradaJugador
{
    public override float Horizontal()
    {
        var kb = Keyboard.current;
        if (kb == null) return 0f;

        float h = 0f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
        return h;
    }

    public override bool SaltoPulsado()
    {
        var kb = Keyboard.current;
        if (kb == null) return false;
        return kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
    }

    public override bool SaltoMantenido()
    {
        var kb = Keyboard.current;
        return kb != null && (kb.spaceKey.isPressed || kb.wKey.isPressed);
    }

    public override float Vertical()
    {
        var kb = Keyboard.current;
        if (kb == null) return 0f;

        float v = 0f;
        if (kb.wKey.isPressed || kb.spaceKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
        return v;
    }

    public override bool DashPulsado()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        bool tecla = kb != null &&
            (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame);
        bool clic = mouse != null && mouse.rightButton.wasPressedThisFrame;
        return tecla || clic;
    }

    public override bool DisparoMantenido()
    {
        var mouse = Mouse.current;
        return mouse != null && mouse.leftButton.isPressed;
    }

    public override bool EnfriamientoPulsado()
    {
        var kb = Keyboard.current;
        return kb != null && kb.rKey.wasPressedThisFrame;
    }

    public override bool TieneAimManual { get { return true; } }
    public override bool ApuntaAUnPunto { get { return true; } }

    public override Vector2 DireccionAim(Vector3 origen, Vector2 facing, Transform objetivoAuto)
    {
        var mouse = Mouse.current;
        Camera cam = Camera.main;

        if (mouse == null || cam == null) return AutoAim(facing, objetivoAuto, origen);

        Vector3 mundo = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        Vector2 dir = (Vector2)mundo - (Vector2)origen;

        return dir.sqrMagnitude < 0.0001f ? facing : dir;
    }
}

/// <summary>Jugador 2 en la misma PC: flechas y apuntado automático al jefe.</summary>
public class EntradaTeclado : EntradaJugador
{
    public override float Horizontal()
    {
        var kb = Keyboard.current;
        if (kb == null) return 0f;

        float h = 0f;
        if (kb.rightArrowKey.isPressed) h += 1f;
        if (kb.leftArrowKey.isPressed) h -= 1f;
        return h;
    }

    public override bool SaltoPulsado()
    {
        var kb = Keyboard.current;
        return kb != null && kb.upArrowKey.isPressed;
    }

    public override float Vertical()
    {
        var kb = Keyboard.current;
        if (kb == null) return 0f;

        float v = 0f;
        if (kb.upArrowKey.isPressed) v += 1f;
        if (kb.downArrowKey.isPressed) v -= 1f;
        return v;
    }

    public override bool DashPulsado()
    {
        var kb = Keyboard.current;
        if (kb == null) return false;
        return kb.rightCtrlKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame;
    }

    public override bool DisparoMantenido()
    {
        var kb = Keyboard.current;
        if (kb == null) return false;
        return kb.periodKey.isPressed || kb.slashKey.isPressed || kb.numpad0Key.isPressed;
    }

    public override bool EnfriamientoPulsado()
    {
        var kb = Keyboard.current;
        return kb != null && kb.commaKey.wasPressedThisFrame;
    }

    public override bool TieneAimManual { get { return false; } }

    public override Vector2 DireccionAim(Vector3 origen, Vector2 facing, Transform objetivoAuto)
    {
        Vector2 dir = AutoAim(facing, objetivoAuto, origen);

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.downArrowKey.isPressed) dir = new Vector2(dir.x, -Mathf.Abs(dir.y) - 0.4f);
            else if (kb.upArrowKey.isPressed) dir = new Vector2(dir.x, Mathf.Abs(dir.y) + 0.4f);
        }

        return dir;
    }
}

/// <summary>Cualquier jugador con mando: stick izquierdo para moverse, derecho para apuntar.</summary>
public class EntradaMando : EntradaJugador
{
    readonly int indice;

    public EntradaMando(int indiceMando) { indice = Mathf.Max(0, indiceMando); }

    Gamepad Mando
    {
        get
        {
            if (Gamepad.all.Count == 0) return null;
            if (indice < Gamepad.all.Count) return Gamepad.all[indice];
            return Gamepad.all[0];
        }
    }

    public override float Horizontal()
    {
        var g = Mando;
        if (g == null) return 0f;

        float h = g.leftStick.x.ReadValue();
        return Mathf.Abs(h) < 0.2f ? 0f : h;
    }

    public override bool SaltoPulsado()
    {
        var g = Mando;
        return g != null && g.buttonSouth.isPressed;
    }

    public override float Vertical()
    {
        var g = Mando;
        if (g == null) return 0f;

        float v = g.leftStick.y.ReadValue();
        if (Mathf.Abs(v) < 0.25f) v = 0f;
        if (g.buttonSouth.isPressed) v = 1f;
        return v;
    }

    public override bool DashPulsado()
    {
        var g = Mando;
        if (g == null) return false;
        return g.buttonWest.wasPressedThisFrame || g.rightShoulder.wasPressedThisFrame;
    }

    public override bool DisparoMantenido()
    {
        var g = Mando;
        if (g == null) return false;
        return g.rightTrigger.ReadValue() > 0.35f || g.buttonEast.isPressed;
    }

    public override bool EnfriamientoPulsado()
    {
        var g = Mando;
        return g != null && g.buttonNorth.wasPressedThisFrame;
    }

    public override bool TieneAimManual { get { return true; } }

    public override Vector2 DireccionAim(Vector3 origen, Vector2 facing, Transform objetivoAuto)
    {
        var g = Mando;
        if (g == null) return AutoAim(facing, objetivoAuto, origen);

        Vector2 stick = g.rightStick.ReadValue();
        if (stick.sqrMagnitude > 0.12f) return stick.normalized;

        return AutoAim(facing, objetivoAuto, origen);
    }
}
