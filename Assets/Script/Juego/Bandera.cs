using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Bandera de final de nivel: al tocarla pasa a la escena siguiente.</summary>
[RequireComponent(typeof(Collider2D))]
public class Bandera : MonoBehaviour
{
    [Tooltip("Nombre EXACTO de la escena siguiente (Nivel2, Boss...).")]
    public string escenaSiguiente = "Nivel2";
    public float esperaAntesDeCargar = 2.2f;

    [Header("Aspecto")]
    public Sprite[] frames;
    public float fps = 7f;

    SpriteRenderer sr;
    float tiempoAnim;
    int frameActual = -1;
    bool alcanzada;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Update()
    {
        if (sr == null || frames == null || frames.Length == 0) return;

        tiempoAnim += Time.deltaTime * (alcanzada ? 2f : 1f);
        int indice = Mathf.FloorToInt(tiempoAnim * fps) % frames.Length;

        if (indice != frameActual)
        {
            frameActual = indice;
            sr.sprite = frames[indice];
        }
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (alcanzada) return;

        PlayerController jugador = otro.GetComponentInParent<PlayerController>();
        if (jugador == null || jugador.Muerto) return;

        alcanzada = true;
        StartCoroutine(Terminar());
    }

    IEnumerator Terminar()
    {
        Puntuacion.GuardarCheckpoint();

        AudioJuego.SonidoCuracion();
        EfectosJuego.Explosion(transform.position + Vector3.up, 1.3f, new Color(0.5f, 1f, 0.6f, 1f));

        if (HUDJuego.Instancia != null)
            HUDJuego.Instancia.MostrarAviso("¡NIVEL COMPLETADO!   " + Puntuacion.Total + " puntos", esperaAntesDeCargar + 1f);

        yield return new WaitForSeconds(esperaAntesDeCargar);

        SceneManager.LoadScene(escenaSiguiente);
    }
}
