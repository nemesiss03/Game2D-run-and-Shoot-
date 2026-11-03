using UnityEngine;

/// <summary>
/// Arena del jefe: la plataforma cubre todo lo que muestra la cámara hasta sus bordes y dos paredes
/// invisibles en los extremos de la pantalla impiden salirse. Se recalcula si cambia la proporción de la ventana.
/// </summary>
public class LimitesCamara : MonoBehaviour
{
    public PlataformaCelestial plataforma;
    public Vector2 centro = new Vector2(0f, 1.7f);
    [Tooltip("Cuánto sobresale la plataforma de los bordes de la pantalla.")]
    public float margen = 1.5f;
    public PhysicsMaterial2D materialPared;

    GameObject paredes;
    float aspectoAplicado;
    float anchoOriginalVisual = -1f;
    Vector3 escalaOriginal;
    Vector3 posicionOriginal;

    void Start()
    {
        Ajustar();
    }

    void Update()
    {
        Camera cam = Camera.main;
        if (cam != null && !Mathf.Approximately(cam.aspect, aspectoAplicado)) Ajustar();
    }

    void Ajustar()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        aspectoAplicado = cam.aspect;
        float mitadAncho = cam.orthographicSize * cam.aspect;

        AjustarPlataforma(mitadAncho);
        CrearParedes(mitadAncho);
    }

    void AjustarPlataforma(float mitadAncho)
    {
        if (plataforma == null) return;

        float anchoNecesario = (mitadAncho + margen) * 2f;

        // Collider: ancho completo, la misma altura
        BoxCollider2D col = plataforma.colisionador as BoxCollider2D;
        if (col != null)
        {
            Vector2 tam = col.size;
            tam.x = anchoNecesario;
            col.size = tam;
        }

        // Dibujo: se agranda por igual alrededor de la superficie de la plataforma
        Transform v = plataforma.visual;
        SpriteRenderer sr = v != null ? v.GetComponent<SpriteRenderer>() : null;
        if (sr == null || sr.sprite == null) return;

        if (anchoOriginalVisual < 0f)
        {
            escalaOriginal = v.localScale;
            posicionOriginal = v.position;
            anchoOriginalVisual = sr.sprite.bounds.size.x * escalaOriginal.x;
        }

        float k = Mathf.Max(1f, anchoNecesario / anchoOriginalVisual);

        // Punto ancla: la superficie de la plataforma (parte alta del collider)
        Vector3 ancla = new Vector3(plataforma.transform.position.x,
            plataforma.colisionador != null ? plataforma.colisionador.bounds.max.y : plataforma.transform.position.y, 0f);

        v.localScale = escalaOriginal * k;
        v.position = ancla + (posicionOriginal - ancla) * k;
    }

    void CrearParedes(float mitadAncho)
    {
        if (paredes != null) Destroy(paredes);

        paredes = new GameObject("ParedesArena");
        paredes.transform.SetParent(transform, false);

        CrearPared(centro.x - mitadAncho - 0.5f);
        CrearPared(centro.x + mitadAncho + 0.5f);
    }

    void CrearPared(float x)
    {
        GameObject g = new GameObject("Pared");
        g.transform.SetParent(paredes.transform, false);
        g.transform.position = new Vector3(x, centro.y, 0f);

        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(1f, 80f);
        if (materialPared != null) bc.sharedMaterial = materialPared;
    }
}
