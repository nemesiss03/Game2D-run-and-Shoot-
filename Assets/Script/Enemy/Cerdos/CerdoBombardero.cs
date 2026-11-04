using System.Collections;
using UnityEngine;

/// <summary>Cerdo que se mantiene a distancia y lanza bombas en arco hacia el jugador (también a las plataformas).</summary>
public class CerdoBombardero : CerdoMenor
{
    [Header("Bombas")]
    public GameObject bombaPrefab;
    public float distanciaMin = 4.5f;
    public float distanciaMax = 8f;
    public float enfriamiento = 3.2f;
    public float tiempoVuelo = 1.0f;

    bool lanzando;
    float proximoLanzamiento;

    protected override void Start()
    {
        base.Start();
        proximoLanzamiento = Time.time + Random.Range(1.2f, 2.2f);
    }

    void FixedUpdate()
    {
        if (muriendo) return;

        Transform obj = Objetivo;
        Vector2 v = rb.linearVelocity;

        if (obj == null || lanzando || Golpeado)
        {
            v.x = 0f;
            rb.linearVelocity = v;
            return;
        }

        float dx = obj.position.x - transform.position.x;
        Mirar(dx);

        float distancia = Mathf.Abs(dx);
        float mover = 0f;

        if (distancia < distanciaMin) mover = -Mathf.Sign(dx);       // se aleja
        else if (distancia > distanciaMax) mover = Mathf.Sign(dx);   // se acerca

        // No se sale de la arena
        ReyCerdo rey = ReyCerdo.Actual;
        if (rey != null)
        {
            float x = transform.position.x;
            if ((mover < 0f && x <= rey.limiteIzq + 1f) || (mover > 0f && x >= rey.limiteDer - 1f)) mover = 0f;
        }

        v.x = mover * velocidad;
        rb.linearVelocity = v;
        anim.Reproducir(Mathf.Abs(mover) > 0f ? correr : idle, 10f);

        if (Time.time >= proximoLanzamiento && distancia <= distanciaMax + 2f) StartCoroutine(Lanzar());
    }

    IEnumerator Lanzar()
    {
        lanzando = true;
        proximoLanzamiento = Time.time + enfriamiento + Random.Range(0f, 1f);
        anim.Reproducir(atacar, 11f, false, true);

        yield return new WaitForSeconds(0.25f);

        Transform obj = Objetivo;
        if (!muriendo && obj != null && bombaPrefab != null)
        {
            Vector3 origen = transform.position + new Vector3(direccion * 0.5f, 1.1f, 0f);
            GameObject g = Instantiate(bombaPrefab, origen, Quaternion.identity);
            BombaCerdo b = g.GetComponent<BombaCerdo>();
            if (b != null) b.Lanzar(obj.position + Vector3.up * 0.4f, tiempoVuelo);
        }

        yield return new WaitForSeconds(0.3f);
        lanzando = false;
    }
}
