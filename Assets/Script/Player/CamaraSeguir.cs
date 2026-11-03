using UnityEngine;

/// <summary>
/// La cámara sigue al jugador (o al punto medio de los dos jugadores).
/// Como el cielo es hijo de la cámara, la caída libre es infinita.
/// Durante la caída se añade un temblor suave para dar sensación de velocidad.
/// </summary>
public class CamaraSeguir : MonoBehaviour
{
    public Transform objetivo;
    public Transform objetivo2;
    public Vector3 desplazamiento = new Vector3(0f, 1.2f, -10f);
    public float suavizado = 7f;

    [Header("Caída libre")]
    public float temblorCaida = 0.06f;
    public float velocidadTemblor = 34f;

    PlayerController jugador;
    bool fija;
    Vector3 centroFijo;

    /// <summary>La cámara deja de seguir al jugador y se queda quieta en este punto (sin temblor).</summary>
    public void FijarEn(Vector3 centro)
    {
        fija = true;
        centroFijo = centro;
    }

    public void Liberar()
    {
        fija = false;
    }

    void LateUpdate()
    {
        if (fija)
        {
            float kf = 1f - Mathf.Exp(-suavizado * Time.deltaTime);
            Vector3 p = Vector3.Lerp(transform.position, centroFijo, kf);
            p.z = desplazamiento.z;
            transform.position = p;
            return;
        }

        if (objetivo == null && objetivo2 == null) return;

        Vector3 centro;

        if (objetivo != null && objetivo2 != null)
            centro = (objetivo.position + objetivo2.position) * 0.5f;
        else
            centro = objetivo != null ? objetivo.position : objetivo2.position;

        Vector3 destino = centro + desplazamiento;

        if (EnCaida())
        {
            destino.y += Mathf.Sin(Time.time * velocidadTemblor) * temblorCaida;
            destino.x += Mathf.Sin(Time.time * velocidadTemblor * 0.7f) * temblorCaida * 0.4f;
        }

        float k = 1f - Mathf.Exp(-suavizado * Time.deltaTime);

        Vector3 pos = Vector3.Lerp(transform.position, destino, k);
        pos.z = desplazamiento.z;

        transform.position = pos;
    }

    bool EnCaida()
    {
        if (jugador == null && objetivo != null) jugador = objetivo.GetComponent<PlayerController>();
        if (jugador != null) return jugador.caidaLibre;

        return objetivo2 != null && CaidaInfinita.EstaActiva;
    }
}
