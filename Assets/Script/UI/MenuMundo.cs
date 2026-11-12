using UnityEngine;

/// <summary>
/// Fondo vivo del menú: el mundo del juego visto desde fuera. La cámara avanza despacio
/// por el paisaje (el parallax de FondoParallax hace el resto) y se mece un poco, como
/// si el menú fuese un plano abierto de la aventura.
/// </summary>
public class MenuMundo : MonoBehaviour
{
    [Tooltip("Unidades por segundo que avanza la cámara en horizontal.")]
    public float velocidad = 1.1f;
    [Tooltip("Cuánto sube y baja la cámara (unidades).")]
    public float oscilacionVertical = 0.25f;
    public float velocidadOscilacion = 0.35f;

    Camera cam;
    Vector3 origen;
    float tiempo;

    void Start()
    {
        cam = Camera.main;
        if (cam != null) origen = cam.transform.position;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // Time.unscaledDeltaTime: el fondo sigue moviéndose aunque se pause el tiempo.
        tiempo += Time.unscaledDeltaTime;
        Vector3 p = origen;
        p.x += tiempo * velocidad;
        p.y += Mathf.Sin(tiempo * velocidadOscilacion * Mathf.PI * 2f) * oscilacionVertical;
        cam.transform.position = p;
    }
}
