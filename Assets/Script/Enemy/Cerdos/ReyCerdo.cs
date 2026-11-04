using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minijefe del Nivel 2: el Rey Cerdo. Corre hacia el jugador y le golpea con su mazo (avisando antes),
/// invoca cerdos soldado y cerdos bombarderos y, al bajar al 50 % de vida, se enfurece: cada dos mazazos
/// golpea el suelo y lanza una onda en las dos direcciones que se esquiva saltando o subiendo a las plataformas.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Salud), typeof(AnimadorCuadros))]
public class ReyCerdo : MonoBehaviour
{
    public static ReyCerdo Actual { get; private set; }

    [Header("Animaciones")]
    public Sprite[] idle, correr, atacar, golpe, muerte, salto;
    [Tooltip("El dibujo original mira a la izquierda.")]
    public bool spriteMiraIzquierda = true;

    [Header("Arena (x del suelo donde se pelea)")]
    public float limiteIzq = 4f;
    public float limiteDer = 40f;
    [Tooltip("Material sin fricción de las paredes (para que no se agarre al saltar).")]
    public PhysicsMaterial2D materialPared;

    [Header("Movimiento y mazo")]
    public float velocidad = 3.6f;
    public float alcanceMazo = 2.7f;
    public float danioMazo = 28f;
    public float danioContacto = 8f;
    public float avisoMazo = 0.6f;

    [Header("Onda (fase 2)")]
    public float danioOnda = 40f;
    public float velocidadOnda = 9f;
    public float avisoOnda = 0.95f;

    [Header("Invocación")]
    public GameObject soldadoPrefab;
    public GameObject bombarderoPrefab;
    public float cadenciaInvocar = 15f;
    public int maxCerdos = 4;

    [Header("Al derrotarlo")]
    public GameObject[] activarAlMorir;
    public GameObject recompensaPrefab;

    Rigidbody2D rb;
    Salud salud;
    AnimadorCuadros anim;
    SpriteRenderer sr;
    Collider2D col;

    bool despierto, fase2, enfurecer, muerto;
    int direccion = -1;
    float proximoContacto;
    GameObject paredes, avisoZona;
    RectTransform relleno;
    GameObject canvasBarra;
    float suelo;

    void Awake()
    {
        Actual = this;
        rb = GetComponent<Rigidbody2D>();
        salud = GetComponent<Salud>();
        anim = GetComponent<AnimadorCuadros>();
        sr = GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true;

        // Lo que aparece al ganar (la bandera) está oculto hasta entonces
        if (activarAlMorir != null)
            foreach (GameObject g in activarAlMorir)
                if (g != null) g.SetActive(false);
    }

    void Start()
    {
        suelo = transform.position.y;
        salud.CambioVida += AlCambiarVida;
        salud.Muerte += AlMorir;
        salud.invulnerable = true;   // hasta que despierte
        anim.Reproducir(idle, 8f);
        Mirar(-1f);
    }

    void OnDestroy()
    {
        if (Actual == this) Actual = null;
        if (paredes != null) Destroy(paredes);
        if (canvasBarra != null) Destroy(canvasBarra);
    }

    // ------------------------------------------------------------ despertar
    public void Despertar()
    {
        if (despierto || muerto) return;
        despierto = true;

        CrearParedes();
        CrearBarra();
        StartCoroutine(Ciclo());
    }

    void CrearParedes()
    {
        paredes = new GameObject("ParedesArena");
        PhysicsMaterial2D mat = materialPared;
        foreach (float x in new[] { limiteIzq - 0.5f, limiteDer + 0.5f })
        {
            GameObject g = new GameObject("Pared");
            g.transform.SetParent(paredes.transform, false);
            g.transform.position = new Vector3(x, suelo + 10f, 0f);
            BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(1f, 60f);
            if (mat != null) bc.sharedMaterial = mat;
        }
    }

    // ------------------------------------------------------------ ciclo de combate
    IEnumerator Ciclo()
    {
        if (HUDJuego.Instancia != null) HUDJuego.Instancia.MostrarAviso("¡EL REY CERDO!\nDERRÓTALO PARA SEGUIR", 3f);
        AudioJuego.SonidoInvocacion();

        anim.Reproducir(atacar, 6f, false, true);
        yield return new WaitForSeconds(1.2f);
        salud.invulnerable = false;

        float proximaInvocacion = Time.time + 5f;
        int mazos = 0;

        while (!muerto)
        {
            if (enfurecer) { enfurecer = false; yield return Enfurecerse(); continue; }

            Transform obj = Jugadores.MasCercano(transform.position);
            if (obj == null) { Parar(); anim.Reproducir(idle, 8f); yield return null; continue; }

            if (Time.time >= proximaInvocacion && CerdoMenor.Todos.Count < maxCerdos)
            {
                yield return Invocar();
                proximaInvocacion = Time.time + (fase2 ? cadenciaInvocar * 0.65f : cadenciaInvocar);
                continue;
            }

            float dx = obj.position.x - transform.position.x;
            Mirar(dx);

            if (Mathf.Abs(dx) > alcanceMazo)
            {
                Vector2 v = rb.linearVelocity;
                v.x = direccion * velocidad * (fase2 ? 1.3f : 1f);
                rb.linearVelocity = v;
                anim.Reproducir(correr, 10f);
                yield return new WaitForFixedUpdate();
                continue;
            }

            Parar();
            mazos++;
            yield return Mazazo(fase2 && mazos % 2 == 0);
        }
    }

    void Parar()
    {
        Vector2 v = rb.linearVelocity;
        v.x = 0f;
        rb.linearVelocity = v;
    }

    IEnumerator Mazazo(bool conOnda)
    {
        float aviso = conOnda ? avisoOnda : avisoMazo;
        MostrarAviso(conOnda);

        // Se pone rojo mientras prepara el golpe
        anim.Reproducir(idle, 8f);
        float t = 0f;
        while (t < aviso)
        {
            t += Time.deltaTime;
            if (sr != null) sr.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.3f), Mathf.PingPong(t * 6f, 1f));
            yield return null;
        }
        if (sr != null) sr.color = Color.white;
        OcultarAviso();

        anim.Reproducir(atacar, 11f, false, true);
        while (anim.Cuadro < 2) yield return null;

        AudioJuego.SonidoDisparoJefe();
        Vector2 centro = new Vector2(transform.position.x + direccion * 1.9f, transform.position.y + 1.1f);
        EfectosJuego.Impacto(new Vector3(centro.x, suelo + 0.2f, 0f), 1.6f);

        for (int i = 0; i < Jugadores.Todos.Count; i++)
        {
            PlayerController j = Jugadores.Todos[i];
            if (j == null || j.Muerto) continue;

            Vector3 p = CentroDe(j);
            if (Mathf.Abs(p.x - centro.x) <= 1.9f && Mathf.Abs(p.y - centro.y) <= 1.5f)
            {
                Salud s = j.GetComponent<Salud>();
                if (s != null) s.RecibirDanio(danioMazo);
            }
        }

        if (conOnda)
        {
            float x = transform.position.x + direccion * 1.6f;
            OndaChoque.Crear(x, suelo, -1, velocidadOnda, danioOnda, limiteIzq - 1f);
            OndaChoque.Crear(x, suelo, 1, velocidadOnda, danioOnda, limiteDer + 1f);
            EfectosJuego.ExplosionJefe(new Vector3(x, suelo + 0.3f, 0f), 1.4f);
        }

        yield return new WaitForSeconds(conOnda ? 0.9f : 0.55f);
    }

    IEnumerator Invocar()
    {
        Parar();
        anim.Reproducir(atacar, 6f, false, true);
        if (HUDJuego.Instancia != null) HUDJuego.Instancia.MostrarAviso("¡EL REY LLAMA A SUS CERDOS!", 2f);
        AudioJuego.SonidoInvocacion();

        yield return new WaitForSeconds(0.7f);

        int cantidad = fase2 ? 3 : 2;
        for (int i = 0; i < cantidad; i++)
        {
            if (CerdoMenor.Todos.Count >= maxCerdos + 1) break;

            bool bomba = (i % 2 == 1) || Random.value < 0.35f;
            GameObject prefab = bomba ? bombarderoPrefab : soldadoPrefab;
            if (prefab == null) continue;

            float x = (i % 2 == 0) ? limiteIzq + 1.5f : limiteDer - 1.5f;
            Vector3 pos = new Vector3(x, suelo + 0.3f, 0f);
            Instantiate(prefab, pos, Quaternion.identity);
            EfectosJuego.Explosion(pos + Vector3.up * 0.5f, 0.9f, new Color(0.8f, 0.75f, 0.7f, 1f));
        }

        yield return new WaitForSeconds(0.8f);
    }

    IEnumerator Enfurecerse()
    {
        fase2 = true;
        Parar();
        salud.invulnerable = true;

        if (HUDJuego.Instancia != null) HUDJuego.Instancia.MostrarAviso("¡EL REY SE ENFURECE!\nSALTA SUS ONDAS", 4f);
        AudioJuego.SonidoExplosionJefe();
        anim.Reproducir(atacar, 6f, false, true);

        float t = 0f;
        while (t < 1.6f)
        {
            t += Time.deltaTime;
            if (sr != null) sr.color = Color.Lerp(Color.white, new Color(1f, 0.3f, 0.25f), Mathf.PingPong(t * 5f, 1f));
            yield return null;
        }

        if (sr != null) sr.color = new Color(1f, 0.82f, 0.8f);   // se queda algo rojizo
        salud.invulnerable = false;
    }

    // ------------------------------------------------------------ avisos en el suelo
    void MostrarAviso(bool onda)
    {
        OcultarAviso();
        avisoZona = new GameObject("AvisoMazo");

        if (onda)
        {
            // La onda recorre toda la arena: se marca el suelo a los dos lados
            CrearFranja(avisoZona.transform, limiteIzq, transform.position.x - 0.5f);
            CrearFranja(avisoZona.transform, transform.position.x + 0.5f, limiteDer);
        }
        else
        {
            float cx = transform.position.x + direccion * 1.9f;
            CrearFranja(avisoZona.transform, cx - 1.9f, cx + 1.9f);
        }
    }

    void CrearFranja(Transform padre, float x0, float x1)
    {
        if (x1 <= x0) return;

        GameObject g = new GameObject("Franja");
        g.transform.SetParent(padre, false);
        g.transform.position = new Vector3((x0 + x1) * 0.5f, suelo + 0.12f, 0f);
        g.transform.localScale = new Vector3(x1 - x0, 0.24f, 1f);

        SpriteRenderer s = g.AddComponent<SpriteRenderer>();
        s.sprite = Cuadro();
        s.color = new Color(1f, 0.25f, 0.2f, 0.55f);
        s.sortingLayerName = "Player";
        s.sortingOrder = 1;
    }

    void OcultarAviso()
    {
        if (avisoZona != null) Destroy(avisoZona);
        avisoZona = null;
    }

    static Sprite cuadro;
    static Sprite Cuadro()
    {
        if (cuadro != null) return cuadro;
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, Color.white);
        t.Apply();
        cuadro = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return cuadro;
    }

    // ------------------------------------------------------------ vida, daño y muerte
    void AlCambiarVida(float actual, float maxima)
    {
        if (relleno != null) relleno.anchorMax = new Vector2(Mathf.Clamp01(actual / maxima), 1f);

        if (!fase2 && !muerto && maxima > 0f && actual > 0f && actual / maxima <= 0.5f) enfurecer = true;
    }

    void FixedUpdate()
    {
        if (muerto || !despierto) return;

        // Tocar al rey duele un poco
        if (Time.time >= proximoContacto && col != null)
        {
            for (int i = 0; i < Jugadores.Todos.Count; i++)
            {
                PlayerController j = Jugadores.Todos[i];
                if (j == null || j.Muerto) continue;

                Collider2D c = j.GetComponent<Collider2D>();
                if (c != null && c.bounds.Intersects(col.bounds))
                {
                    Salud s = j.GetComponent<Salud>();
                    if (s != null) s.RecibirDanio(danioContacto);
                    proximoContacto = Time.time + 1.2f;
                    break;
                }
            }
        }
    }

    void AlMorir()
    {
        if (muerto) return;
        muerto = true;
        StopAllCoroutines();
        OcultarAviso();

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (col != null) col.enabled = false;
        if (sr != null) sr.color = Color.white;
        anim.Reproducir(muerte, 6f, false, true);

        if (paredes != null) Destroy(paredes);
        if (canvasBarra != null) Destroy(canvasBarra);

        foreach (CerdoMenor c in CerdoMenor.Todos.ToArray()) if (c != null) c.Matar();
        foreach (BombaCerdo b in FindObjectsByType<BombaCerdo>(FindObjectsSortMode.None)) Destroy(b.gameObject);

        if (HUDJuego.Instancia != null) HUDJuego.Instancia.MostrarAviso("¡REY CERDO DERROTADO!", 3.5f);
        AudioJuego.SonidoExplosionJefe();
        StartCoroutine(Final());
    }

    IEnumerator Final()
    {
        for (int i = 0; i < 6; i++)
        {
            Vector2 d = Random.insideUnitCircle * 1.2f;
            EfectosJuego.ExplosionJefe(transform.position + new Vector3(d.x, 1f + d.y, 0f), Random.Range(1.2f, 2.2f));
            yield return new WaitForSeconds(Random.Range(0.1f, 0.22f));
        }

        yield return new WaitForSeconds(0.8f);

        if (recompensaPrefab != null)
            Instantiate(recompensaPrefab, transform.position + new Vector3(-2f, 0f, 0f), Quaternion.identity);

        if (activarAlMorir != null)
            foreach (GameObject g in activarAlMorir)
                if (g != null) g.SetActive(true);
    }

    // ------------------------------------------------------------ utilidades
    void Mirar(float dx)
    {
        if (Mathf.Abs(dx) < 0.05f) return;
        direccion = dx > 0f ? 1 : -1;
        if (sr != null) sr.flipX = spriteMiraIzquierda ? direccion > 0 : direccion < 0;
    }

    static Vector3 CentroDe(PlayerController j)
    {
        Collider2D c = j.GetComponent<Collider2D>();
        return c != null ? c.bounds.center : j.transform.position;
    }

    // ------------------------------------------------------------ barra de vida
    void CrearBarra()
    {
        canvasBarra = new GameObject("BarraRey");
        Canvas cv = canvasBarra.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 20;

        RectTransform fondo = Panel(canvasBarra.transform, "Fondo", new Color(0f, 0f, 0f, 0.65f));
        fondo.anchorMin = fondo.anchorMax = new Vector2(0.5f, 1f);
        fondo.pivot = new Vector2(0.5f, 1f);
        fondo.sizeDelta = new Vector2(520f, 22f);
        fondo.anchoredPosition = new Vector2(0f, -62f);

        RectTransform r = Panel(fondo, "Relleno", new Color(0.85f, 0.2f, 0.25f, 1f));
        r.anchorMin = new Vector2(0f, 0f);
        r.anchorMax = new Vector2(salud.Fraccion, 1f);
        r.offsetMin = new Vector2(3f, 3f);
        r.offsetMax = new Vector2(-3f, -3f);
        relleno = r;

        GameObject tg = new GameObject("Nombre");
        tg.transform.SetParent(fondo, false);
        Text txt = tg.AddComponent<Text>();
        txt.text = "REY CERDO";
        txt.alignment = TextAnchor.LowerCenter;
        txt.fontSize = 20;
        txt.fontStyle = FontStyle.Bold;
        txt.color = new Color(1f, 0.9f, 0.7f);
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform tr = txt.rectTransform;
        tr.anchorMin = new Vector2(0f, 1f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0.5f, 0f);
        tr.sizeDelta = new Vector2(0f, 28f);
        tr.anchoredPosition = Vector2.zero;
    }

    static RectTransform Panel(Transform padre, string nombre, Color color)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        Image im = g.AddComponent<Image>();
        im.color = color;
        im.raycastTarget = false;
        return im.rectTransform;
    }
}
