using UnityEngine;

/// <summary>
/// Orbe del pecho del dragón: avisa de un vistazo de cómo va el fuego.
///   · Con cargas de sobra está apagado.
///   · Al bajar del umbral (PlayerController.umbralAvisoFuego) se enciende en rojo y late cada vez más rápido.
///   · Sin cargas / enfriando brilla fijo y se va templando a naranja hasta recuperarlas.
///   · Al terminar de enfriar da un destello azulado: "listo".
/// Todo se ajusta desde el PlayerController (sección "Indicador de fuego"). Lo crea el propio PlayerController.
/// </summary>
public class OrbePechoFuego : MonoBehaviour
{
    static Sprite spriteRadial;

    PlayerController j;
    SpriteRenderer cuerpo, nucleo, halo;
    float intensidad;
    float destello;
    bool estabaEnfriando;

    public static void Crear(PlayerController dueno)
    {
        GameObject g = new GameObject("OrbePechoFuego");
        g.transform.SetParent(dueno.transform, false);
        g.AddComponent<OrbePechoFuego>().j = dueno;
    }

    void Awake()
    {
        if (spriteRadial == null) spriteRadial = CrearSprite();

        halo = Capa("Halo", 0);
        nucleo = Capa("Nucleo", 1);
    }

    SpriteRenderer Capa(string nombre, int orden)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(transform, false);
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = spriteRadial;
        sr.color = new Color(1f, 1f, 1f, 0f);
        return sr;
    }

    void Start()
    {
        cuerpo = j != null ? j.GetComponent<SpriteRenderer>() : null;
        if (cuerpo != null)
        {
            foreach (SpriteRenderer sr in new[] { halo, nucleo })
            {
                sr.sortingLayerID = cuerpo.sortingLayerID;
            }
            // Por encima de todas las piezas del dragón (cabeza, alas, armadura...)
            int maximo = cuerpo.sortingOrder;
            foreach (SpriteRenderer sr in j.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr != halo && sr != nucleo && sr.sortingLayerID == cuerpo.sortingLayerID && sr.sortingOrder > maximo)
                    maximo = sr.sortingOrder;

            halo.sortingOrder = maximo + 1;
            nucleo.sortingOrder = maximo + 2;
        }
    }

    void LateUpdate()
    {
        if (j == null) { Destroy(gameObject); return; }

        // Posición en el pecho (el dibujo se voltea con flipX: el desplazamiento horizontal se invierte a mano)
        Vector2 p = j.posicionPecho;
        transform.localPosition = new Vector3(j.MirandoIzquierda ? -p.x : p.x, p.y, 0f);

        float fraccion = j.FraccionCargas;
        bool enfriando = j.Recargando;

        float objetivo, pulso = 1f;
        Color color = j.colorOrbe;
        float escala = j.tamanoOrbe;

        if (j.Muerto || !j.mostrarOrbePecho)
        {
            objetivo = 0f;
        }
        else if (enfriando)
        {
            // Sin fuego: brilla fijo y se va templando mientras se enfría
            float prog = j.ProgresoRecarga;
            objetivo = 1f;
            pulso = 0.8f + 0.2f * Mathf.Sin(Time.time * 10f);
            color = Color.Lerp(color, new Color(1f, 0.6f, 0.15f), prog * 0.85f);
            escala *= Mathf.Lerp(1.15f, 0.8f, prog);
        }
        else if (fraccion <= j.umbralAvisoFuego)
        {
            // Quedan pocas cargas: se enciende y late más rápido cuanto menos queda
            float k = 1f - Mathf.Clamp01(fraccion / Mathf.Max(0.01f, j.umbralAvisoFuego));
            objetivo = Mathf.Lerp(0.35f, 0.9f, k);
            pulso = 0.75f + 0.25f * Mathf.Sin(Time.time * Mathf.Lerp(4f, 12f, k));
            escala *= Mathf.Lerp(0.85f, 1.1f, k);
        }
        else
        {
            objetivo = 0f;
        }

        if (estabaEnfriando && !enfriando && !j.Muerto) destello = 1f;
        estabaEnfriando = enfriando;

        intensidad = Mathf.MoveTowards(intensidad, objetivo, Time.deltaTime * 4f);
        destello = Mathf.MoveTowards(destello, 0f, Time.deltaTime * 2.5f);

        // Destello azulado al estar listo otra vez
        color = Color.Lerp(color, new Color(0.6f, 0.9f, 1f), destello);
        float alfa = Mathf.Max(intensidad * pulso, destello);

        transform.localScale = Vector3.one * escala;

        halo.color = new Color(color.r, color.g * 0.6f, color.b * 0.6f, alfa * 0.45f);
        halo.transform.localScale = Vector3.one * 2.2f;

        nucleo.color = new Color(Mathf.Min(1f, color.r + 0.2f), Mathf.Min(1f, color.g + 0.35f), Mathf.Min(1f, color.b + 0.3f), Mathf.Clamp01(alfa));
        nucleo.transform.localScale = Vector3.one * 0.9f;
    }

    /// <summary>Círculo con degradado suave: centro brillante y borde transparente.</summary>
    static Sprite CrearSprite()
    {
        const int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float r = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);   // suavizado
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n);
    }
}
