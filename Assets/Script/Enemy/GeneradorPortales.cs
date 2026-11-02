using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abre portales en lugares al azar del nivel. Cada portal suelta 3 o 4 enemigos y desaparece.
/// Solo se eligen puntos que estén a una distancia razonable del jugador (ni encima ni lejísimos).
/// </summary>
public class GeneradorPortales : MonoBehaviour
{
    [Header("Qué sale")]
    public GameObject portalPrefab;
    public GameObject enemigoPrefab;
    public int minEnemigos = 3;
    public int maxEnemigos = 4;
    [Tooltip("Segundos entre un enemigo y el siguiente dentro del mismo portal.")]
    public float intervaloEnemigos = 0.9f;

    [Header("Dónde")]
    [Tooltip("Lugares posibles. Si se deja vacío, aparecen alrededor del jugador.")]
    public Transform[] anclas;
    public float distanciaMinima = 7f;
    public float distanciaMaxima = 20f;

    [Header("Cuándo")]
    public float primeraEspera = 6f;
    public float esperaMinima = 9f;
    public float esperaMaxima = 15f;
    public int maxPortalesAbiertos = 1;

    readonly List<GameObject> abiertos = new List<GameObject>();
    float proximo;

    void Start()
    {
        proximo = Time.time + primeraEspera;
    }

    void Update()
    {
        if (Time.time < proximo) return;
        if (portalPrefab == null || enemigoPrefab == null) return;

        abiertos.RemoveAll(g => g == null);
        if (abiertos.Count >= maxPortalesAbiertos) { proximo = Time.time + 1f; return; }

        Transform jugador = Jugadores.MasCercano(transform.position);
        if (jugador == null) return;

        Vector3 punto;
        if (!ElegirPunto(jugador.position, out punto)) { proximo = Time.time + 1f; return; }

        Abrir(punto);
        proximo = Time.time + Random.Range(esperaMinima, esperaMaxima);
    }

    bool ElegirPunto(Vector3 jugador, out Vector3 punto)
    {
        punto = Vector3.zero;

        if (anclas != null && anclas.Length > 0)
        {
            // Mezcla simple: se prueban las anclas en orden aleatorio
            int inicio = Random.Range(0, anclas.Length);

            for (int i = 0; i < anclas.Length; i++)
            {
                Transform a = anclas[(inicio + i) % anclas.Length];
                if (a == null) continue;

                float d = Vector2.Distance(a.position, jugador);
                if (d < distanciaMinima || d > distanciaMaxima) continue;

                punto = a.position;
                return true;
            }

            return false;
        }

        float lado = Random.value < 0.5f ? -1f : 1f;
        punto = jugador + new Vector3(lado * Random.Range(distanciaMinima, distanciaMaxima), Random.Range(0f, 3f), 0f);
        return true;
    }

    void Abrir(Vector3 punto)
    {
        GameObject g = Instantiate(portalPrefab, punto, Quaternion.identity);

        Portal p = g.GetComponent<Portal>();
        if (p != null)
        {
            int cantidad = Random.Range(minEnemigos, maxEnemigos + 1);

            p.esbirroPrefab = enemigoPrefab;
            p.maxEsbirros = cantidad;
            p.intervalo = intervaloEnemigos;
            p.duracion = p.tiempoApertura + cantidad * intervaloEnemigos + 1.2f;
        }

        AudioJuego.SonidoInvocacion();
        abiertos.Add(g);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.4f, 1f, 1f);
        if (anclas == null) return;
        for (int i = 0; i < anclas.Length; i++)
            if (anclas[i] != null) Gizmos.DrawWireSphere(anclas[i].position, 0.5f);
    }
}
