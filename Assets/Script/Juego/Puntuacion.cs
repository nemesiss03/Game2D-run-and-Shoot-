using System;
using UnityEngine;

/// <summary>
/// Puntos de la partida. Viajan de un nivel al siguiente; si mueres y reinicias,
/// vuelves a los puntos con los que entraste al nivel.
/// </summary>
public static class Puntuacion
{
    public const int PuntosEnemigo = 100;
    public const int PuntosMoneda = 50;
    public const int PuntosGema = 15;

    public static int Total { get; private set; }
    static int checkpoint;

    public static event Action<int> Cambio;

    // Con "Enter Play Mode" sin recargar dominio los estáticos sobrevivirían a la partida anterior
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpiar()
    {
        Total = 0;
        checkpoint = 0;
        Cambio = null;
    }

    /// <summary>Nueva partida desde el menú.</summary>
    public static void Reiniciar()
    {
        Total = 0;
        checkpoint = 0;
        Avisar();
    }

    /// <summary>Se llama al terminar un nivel: estos puntos ya no se pierden al reiniciar.</summary>
    public static void GuardarCheckpoint()
    {
        checkpoint = Total;
    }

    /// <summary>Se llama al reiniciar el nivel tras morir.</summary>
    public static void RestaurarCheckpoint()
    {
        Total = checkpoint;
        Avisar();
    }

    public static void Sumar(int puntos, Vector3 posicion, Color color)
    {
        if (puntos <= 0) return;

        Total += puntos;
        Avisar();
        TextoFlotante.Crear(posicion, "+" + puntos, color);
    }

    static void Avisar()
    {
        if (Cambio != null) Cambio(Total);
    }
}

/// <summary>Texto "+50" que sube y se desvanece donde ocurrió el punto.</summary>
public class TextoFlotante : MonoBehaviour
{
    static Font fuente;

    TextMesh texto;
    float edad;
    const float Duracion = 0.9f;

    public static void Crear(Vector3 posicion, string contenido, Color color)
    {
        if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fuente == null) return;

        GameObject go = new GameObject("TextoFlotante");
        go.transform.position = posicion + Vector3.up * 0.6f;

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.font = fuente;
        tm.text = contenido;
        tm.characterSize = 0.09f;
        tm.fontSize = 64;
        tm.fontStyle = FontStyle.Bold;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = color;

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = fuente.material;
        mr.sortingLayerName = "Player";
        mr.sortingOrder = 300;

        TextoFlotante t = go.AddComponent<TextoFlotante>();
        t.texto = tm;
    }

    void Update()
    {
        edad += Time.deltaTime;
        transform.position += Vector3.up * (1.6f * Time.deltaTime);

        Color c = texto.color;
        c.a = Mathf.Clamp01(1f - (edad / Duracion) * (edad / Duracion));
        texto.color = c;

        if (edad >= Duracion) Destroy(gameObject);
    }
}
