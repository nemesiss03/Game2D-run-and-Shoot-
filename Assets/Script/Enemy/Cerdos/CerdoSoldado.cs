using System.Collections;
using UnityEngine;

/// <summary>Cerdo que corre hacia el jugador y le golpea cuerpo a cuerpo.</summary>
public class CerdoSoldado : CerdoMenor
{
    [Header("Golpe")]
    public float danio = 12f;
    public float alcance = 1.35f;
    public float enfriamiento = 1.4f;

    bool atacando;
    float proximoAtaque;

    void FixedUpdate()
    {
        if (muriendo) return;

        Transform obj = Objetivo;
        Vector2 v = rb.linearVelocity;

        if (obj == null || atacando || Golpeado)
        {
            v.x = 0f;
            rb.linearVelocity = v;
            if (obj == null && !Golpeado) anim.Reproducir(idle, 8f);
            return;
        }

        float dx = obj.position.x - transform.position.x;
        float dy = obj.position.y - transform.position.y;
        Mirar(dx);

        if (Mathf.Abs(dx) <= alcance && Mathf.Abs(dy) < 1.8f)
        {
            v.x = 0f;
            rb.linearVelocity = v;

            if (Time.time >= proximoAtaque) StartCoroutine(Golpear());
            else anim.Reproducir(idle, 8f);
            return;
        }

        // Si el jugador está en una plataforma, se queda debajo esperando
        v.x = Mathf.Abs(dx) > 0.4f ? direccion * velocidad : 0f;
        rb.linearVelocity = v;
        anim.Reproducir(Mathf.Abs(v.x) > 0.1f ? correr : idle, 10f);
    }

    IEnumerator Golpear()
    {
        atacando = true;
        proximoAtaque = Time.time + enfriamiento;
        anim.Reproducir(atacar, 12f, false, true);

        yield return new WaitForSeconds(0.25f);

        Transform obj = Objetivo;
        if (!muriendo && obj != null)
        {
            float dx = obj.position.x - transform.position.x;
            float dy = obj.position.y - transform.position.y;
            if (Mathf.Abs(dx) <= alcance + 0.4f && Mathf.Abs(dy) < 1.9f && Mathf.Sign(dx) == direccion)
            {
                HacerDanio(obj, danio);
                AudioJuego.SonidoImpactoEsbirro();
            }
        }

        yield return new WaitForSeconds(0.25f);
        atacando = false;
    }
}
