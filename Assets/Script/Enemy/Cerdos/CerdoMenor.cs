using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Base de los cerdos que invoca el Rey: vida, giro del sprite, animaciones y muerte.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Salud), typeof(AnimadorCuadros))]
public abstract class CerdoMenor : MonoBehaviour
{
    public static readonly List<CerdoMenor> Todos = new List<CerdoMenor>();

    [Header("Animaciones")]
    public Sprite[] idle, correr, atacar, golpe, muerte;
    [Tooltip("El dibujo original mira a la izquierda.")]
    public bool spriteMiraIzquierda = true;

    [Header("Movimiento")]
    public float velocidad = 3.2f;

    protected Rigidbody2D rb;
    protected Salud salud;
    protected AnimadorCuadros anim;
    protected SpriteRenderer sr;
    protected bool muriendo;
    protected int direccion = -1;
    float finGolpe;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        salud = GetComponent<Salud>();
        anim = GetComponent<AnimadorCuadros>();
        sr = GetComponentInChildren<SpriteRenderer>();
        rb.freezeRotation = true;
    }

    protected virtual void OnEnable() { Todos.Add(this); }
    protected virtual void OnDisable() { Todos.Remove(this); }

    protected virtual void Start()
    {
        salud.Muerte += AlMorir;
        salud.CambioVida += AlCambiarVida;

        // Los cerdos no se empujan entre sí ni empujan al Rey
        Collider2D mio = GetComponent<Collider2D>();
        foreach (CerdoMenor otro in Todos)
        {
            if (otro == this) continue;
            Collider2D c = otro.GetComponent<Collider2D>();
            if (c != null && mio != null) Physics2D.IgnoreCollision(mio, c);
        }

        if (ReyCerdo.Actual != null)
        {
            Collider2D c = ReyCerdo.Actual.GetComponent<Collider2D>();
            if (c != null && mio != null) Physics2D.IgnoreCollision(mio, c);
        }
    }

    protected virtual void OnDestroy()
    {
        if (salud != null)
        {
            salud.Muerte -= AlMorir;
            salud.CambioVida -= AlCambiarVida;
        }
    }

    protected Transform Objetivo { get { return Jugadores.MasCercano(transform.position); } }

    protected void Mirar(float dx)
    {
        if (Mathf.Abs(dx) < 0.05f) return;
        direccion = dx > 0f ? 1 : -1;
        if (sr != null) sr.flipX = spriteMiraIzquierda ? direccion > 0 : direccion < 0;
    }

    protected bool Golpeado { get { return Time.time < finGolpe; } }

    void AlCambiarVida(float actual, float maxima)
    {
        if (muriendo || actual <= 0f) return;
        finGolpe = Time.time + 0.25f;
        anim.Reproducir(golpe, 12f, false, true);
        StartCoroutine(Parpadeo());
    }

    IEnumerator Parpadeo()
    {
        if (sr == null) yield break;
        sr.color = new Color(1f, 0.5f, 0.5f);
        yield return new WaitForSeconds(0.12f);
        if (sr != null) sr.color = Color.white;
    }

    void AlMorir()
    {
        if (muriendo) return;
        muriendo = true;
        StopAllCoroutines();

        Collider2D c = GetComponent<Collider2D>();
        if (c != null) c.enabled = false;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        anim.Reproducir(muerte, 10f, false, true);
        Destroy(gameObject, 0.55f);
    }

    /// <summary>Lo llama el Rey al morir: todos los cerdos caen con él.</summary>
    public void Matar()
    {
        if (!muriendo) salud.Morir();
    }

    protected void HacerDanio(Transform objetivo, float danio)
    {
        if (objetivo == null) return;
        Salud s = objetivo.GetComponent<Salud>();
        if (s != null) s.RecibirDanio(danio);
    }
}
