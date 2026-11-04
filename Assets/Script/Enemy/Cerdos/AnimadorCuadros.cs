using UnityEngine;

/// <summary>Reproduce una lista de sprites (hoja cortada en fotogramas) sobre un SpriteRenderer, sin Animator.</summary>
public class AnimadorCuadros : MonoBehaviour
{
    public SpriteRenderer sr;

    Sprite[] actual;
    float fps = 10f;
    bool bucle = true;
    float t;

    public bool Terminada { get; private set; }
    public int Cuadro { get; private set; }

    void Awake()
    {
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>Cambia de animación. Si ya se está reproduciendo la misma, no la reinicia (salvo que se pida).</summary>
    public void Reproducir(Sprite[] cuadros, float velocidad = 10f, bool repetir = true, bool reiniciar = false)
    {
        if (cuadros == null || cuadros.Length == 0) return;
        if (actual == cuadros && !reiniciar) return;

        actual = cuadros;
        fps = velocidad;
        bucle = repetir;
        t = 0f;
        Cuadro = 0;
        Terminada = false;
        if (sr != null) sr.sprite = cuadros[0];
    }

    void Update()
    {
        if (actual == null || sr == null) return;

        t += Time.deltaTime;
        int i = Mathf.FloorToInt(t * fps);

        if (bucle) i %= actual.Length;
        else if (i >= actual.Length) { i = actual.Length - 1; Terminada = true; }

        Cuadro = i;
        sr.sprite = actual[i];
    }
}
