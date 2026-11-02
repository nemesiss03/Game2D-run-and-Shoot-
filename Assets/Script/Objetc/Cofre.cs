using System.Collections;
using UnityEngine;

/// <summary>
/// Cofre: se abre al tocarlo y suelta monedas (50 puntos) y gemas (15 puntos).
/// Cuántos objetos salen y de qué tipo se decide al azar con las probabilidades de abajo.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Cofre : MonoBehaviour
{
    [Header("Contenido")]
    public GameObject monedaPrefab;
    public GameObject gemaPrefab;
    public int minObjetos = 3;
    public int maxObjetos = 6;
    [Tooltip("Probabilidad de que cada objeto sea moneda. El resto son gemas.")]
    [Range(0f, 1f)] public float probabilidadMoneda = 0.65f;
    [Tooltip("Probabilidad de que el cofre sea un botín especial con el doble de objetos.")]
    [Range(0f, 1f)] public float probabilidadBotin = 0.15f;

    [Header("Aspecto")]
    [Tooltip("Fotogramas de apertura: el primero es el cofre cerrado y el último abierto.")]
    public Sprite[] frames;
    public float fps = 9f;

    SpriteRenderer sr;
    bool abierto;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        Collider2D c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    void Start()
    {
        if (sr != null && frames != null && frames.Length > 0) sr.sprite = frames[0];
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (abierto) return;
        if (otro.GetComponentInParent<PlayerController>() == null) return;

        abierto = true;
        StartCoroutine(Abrir());
    }

    IEnumerator Abrir()
    {
        AudioJuego.SonidoInvocacion();

        if (sr != null && frames != null)
        {
            for (int i = 1; i < frames.Length; i++)
            {
                yield return new WaitForSeconds(1f / fps);
                sr.sprite = frames[i];
            }
        }

        Soltar();
    }

    void Soltar()
    {
        int cantidad = Random.Range(minObjetos, maxObjetos + 1);
        if (Random.value < probabilidadBotin) cantidad *= 2;

        Vector3 origen = transform.position + Vector3.up * 0.6f;

        EfectosJuego.Explosion(origen, 0.7f, new Color(1f, 0.9f, 0.4f, 1f));

        for (int i = 0; i < cantidad; i++)
        {
            GameObject prefab = Random.value < probabilidadMoneda ? monedaPrefab : gemaPrefab;
            if (prefab == null) continue;

            GameObject g = Instantiate(prefab, origen, Quaternion.identity);
            Recolectable r = g.GetComponent<Recolectable>();
            if (r == null) continue;

            float lado = Random.Range(-4.5f, 4.5f);
            r.Lanzar(new Vector2(lado, Random.Range(7f, 11f)));
        }
    }
}
