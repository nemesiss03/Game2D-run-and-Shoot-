using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class UltimoSuelo : MonoBehaviour
{
    [Tooltip("Cada cuántos segundos se guarda la posición mientras pisas suelo.")]
    public float intervalo = 0.25f;
    [Tooltip("Radio para comprobar que no hay una zona de daño cerca de ti.")]
    public float radioSeguro = 0.7f;

    public Vector2 PosicionSegura { get; private set; }

    Rigidbody2D rb;
    float proximo;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        PosicionSegura = rb.position;   // al empezar, el punto de inicio
    }

    void OnCollisionStay2D(Collision2D c)
    {
        if (Time.time < proximo) return;

        bool pisaSuelo = false;
        for (int i = 0; i < c.contactCount; i++)
            if (c.GetContact(i).normal.y > 0.7f) { pisaSuelo = true; break; }

        if (!pisaSuelo) return;
        if (c.collider.GetComponentInParent<Salud>() != null) return;   // no vale pisar enemigos

        // No guardar un punto que esté sobre o junto a una zona de daño
        Collider2D[] cerca = Physics2D.OverlapCircleAll(rb.position, radioSeguro);
        for (int i = 0; i < cerca.Length; i++)
            if (cerca[i].GetComponentInParent<ZonaDanio>() != null) return;

        if (!SueloFirmeBajoLosPies()) return;   // no guardar en el borde de una plataforma

        PosicionSegura = rb.position + Vector2.up * 0.1f;
        proximo = Time.time + intervalo;
    }

    /// <summary>
    /// Suelo sólido bajo los dos extremos de los pies. En un borde solo uno de los dos
    /// toca suelo, y volver ahí dejaría al jugador a medio caer o incrustado.
    /// </summary>
    bool SueloFirmeBajoLosPies()
    {
        Bounds b = default(Bounds);
        bool hay = false;

        foreach (Collider2D col in GetComponents<Collider2D>())
        {
            if (col.isTrigger) continue;
            if (!hay) { b = col.bounds; hay = true; }
            else b.Encapsulate(col.bounds);
        }

        if (!hay) return true;

        const float margen = 0.08f;
        float y = b.min.y + 0.1f;

        return HaySueloBajo(new Vector2(b.min.x + margen, y)) &&
               HaySueloBajo(new Vector2(b.max.x - margen, y));
    }

    bool HaySueloBajo(Vector2 origen)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origen, Vector2.down, 0.35f);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D c = hits[i].collider;
            if (c == null || c.isTrigger) continue;
            if (c.GetComponentInParent<Salud>() != null) continue;
            if (c.GetComponentInParent<ZonaDanio>() != null) continue;
            return true;
        }
        return false;
    }

    public void Teletransportar(Vector2 destino)
    {
        rb.position = destino;
        transform.position = destino;
        rb.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }
}