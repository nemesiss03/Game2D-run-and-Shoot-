using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D))]
public class GeneradorEnemigos : MonoBehaviour
{
    [System.Serializable]
    public class Oleada
    {
        public string nombre = "Oleada";
        [Tooltip("Se elige uno al azar de esta lista cada vez.")]
        public GameObject[] enemigos;
        public int cantidad = 5;
        public float intervalo = 1.2f;
        [Tooltip("Pausa después de derrotar a todos, antes de la siguiente oleada.")]
        public float pausaAlTerminar = 2f;
    }

    [Header("Cuándo empieza")]
    [Tooltip("Empieza cuando un jugador entra en el área (BoxCollider2D como Trigger).")]
    public bool empezarAlEntrarJugador = true;
    public bool empezarAlCargar = false;

    [Header("Dónde aparecen")]
    [Tooltip("Si se deja vacío, aparecen en la posición de este objeto.")]
    public Transform[] puntosAparicion;
    public int maxVivos = 4;
    public bool efectoAparicion = true;

    [Header("Oleadas")]
    public Oleada[] oleadas;

    [Header("Eventos")]
    public UnityEvent alIniciar;
    public UnityEvent alTerminarOleada;
    [Tooltip("Aquí conectas SalidaNivel.Abrir, una puerta, etc.")]
    public UnityEvent alTerminarTodo;

    public int OleadaActual { get; private set; }

    readonly List<GameObject> vivos = new List<GameObject>();
    bool iniciado;

    void Reset()
    {
        BoxCollider2D bc = GetComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(10f, 6f);
    }

    void Start()
    {
        if (empezarAlCargar) Iniciar();
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!empezarAlEntrarJugador || iniciado) return;
        if (otro.GetComponentInParent<PlayerController>() != null) Iniciar();
    }

    public void Iniciar()
    {
        if (iniciado) return;
        iniciado = true;
        StartCoroutine(Rutina());
    }

    int Contar()
    {
        vivos.RemoveAll(g => g == null);
        return vivos.Count;
    }

    IEnumerator Rutina()
    {
        alIniciar.Invoke();

        for (int o = 0; o < oleadas.Length; o++)
        {
            OleadaActual = o + 1;
            Oleada ola = oleadas[o];

            for (int i = 0; i < ola.cantidad; i++)
            {
                while (Contar() >= maxVivos) yield return null;
                Aparecer(ola);
                yield return new WaitForSeconds(ola.intervalo);
            }

            while (Contar() > 0) yield return null;

            alTerminarOleada.Invoke();
            yield return new WaitForSeconds(ola.pausaAlTerminar);
        }

        alTerminarTodo.Invoke();
    }

    void Aparecer(Oleada ola)
    {
        if (ola.enemigos == null || ola.enemigos.Length == 0) return;

        GameObject prefab = ola.enemigos[Random.Range(0, ola.enemigos.Length)];
        if (prefab == null) return;

        Vector3 pos = transform.position;
        if (puntosAparicion != null && puntosAparicion.Length > 0)
            pos = puntosAparicion[Random.Range(0, puntosAparicion.Length)].position;

        GameObject g = Instantiate(prefab, pos, Quaternion.identity);
        vivos.Add(g);

        if (efectoAparicion)
        {
            AudioJuego.SonidoInvocacion();
            EfectosJuego.Explosion(pos, 0.9f, new Color(0.75f, 0.45f, 1f, 1f));
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (puntosAparicion == null) return;
        for (int i = 0; i < puntosAparicion.Length; i++)
            if (puntosAparicion[i] != null) Gizmos.DrawWireSphere(puntosAparicion[i].position, 0.4f);
    }
}