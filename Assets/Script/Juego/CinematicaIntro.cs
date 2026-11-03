using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Cinemática de apertura (al empezar Nivel1): el Señor del Abismo baja del cielo y lanza un ataque
/// contra el dragón; el dragón lo detiene a disparos, el jefe huye y empieza la búsqueda.
/// Se crea sola la primera vez que se entra en Nivel1 de una partida. Enter la salta.
/// </summary>
public class CinematicaIntro : MonoBehaviour
{
    public static bool Vista;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpiar() { Vista = false; }

    /// <summary>Se llama al empezar una partida nueva desde el menú.</summary>
    public static void Reiniciar() { Vista = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Registrar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        Comprobar();
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo) { Comprobar(); }

    static void Comprobar()
    {
        if (Vista) return;
        if (SceneManager.GetActiveScene().name != "Nivel1") return;
        if (FindAnyObjectByType<CinematicaIntro>() != null) return;

        new GameObject("CinematicaIntro").AddComponent<CinematicaIntro>();
    }

    // ------------------------------------------------------------------
    GameObject jefe, orbe;
    bool saltar;
    Sprite spriteOrbe;

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) saltar = true;
    }

    IEnumerator Start()
    {
        yield return null;   // deja que la escena termine de montarse (jugadores, HUD, cámara)

        if (Jugadores.Todos.Count == 0 || Camera.main == null) { Vista = true; Destroy(gameObject); yield break; }

        PlayerController j = Jugadores.Todos[0];
        bool[] invulnerables = new bool[Jugadores.Todos.Count];

        for (int i = 0; i < Jugadores.Todos.Count; i++)
        {
            PlayerController p = Jugadores.Todos[i];
            Salud s = p.GetComponent<Salud>();
            invulnerables[i] = s != null && s.invulnerable;
            if (s != null) s.invulnerable = true;
            p.controlActivo = false;
        }

        yield return EfectosCine.Instancia.Barras(true, 0.7f);

        try
        {
            yield return Secuencia(j);
        }
        finally
        {
            if (jefe != null) Destroy(jefe);
            if (orbe != null) Destroy(orbe);
        }

        for (int i = 0; i < Jugadores.Todos.Count; i++)
        {
            PlayerController p = Jugadores.Todos[i];
            if (p == null) continue;

            Salud s = p.GetComponent<Salud>();
            if (s != null) s.invulnerable = i < invulnerables.Length && invulnerables[i];
            p.controlActivo = true;
            p.MirarHacia(false);
        }

        Vista = true;
        yield return EfectosCine.Instancia.Barras(false, 0.7f);
        Destroy(gameObject);
    }

    IEnumerator Secuencia(PlayerController j)
    {
        Camera cam = Camera.main;
        j.MirarHacia(false);

        Aviso("UN MAL PRESAGIO CRUZA EL CIELO...", 2.4f);
        yield return Esperar(1.8f);
        if (saltar) yield break;

        // ---- El jefe baja del cielo, por la derecha
        GameObject prefab = Resources.Load<GameObject>("Prefabs/JefeCinematica");
        Vector3 c = cam.transform.position;
        c.z = 0f;   // la cámara está en z = -10: los actores van en el plano del juego
        Vector3 aparicion = c + new Vector3(11f, 9f, 0f);
        Vector3 posicion = c + new Vector3(5f, 2.3f, 0f);

        if (prefab != null)
        {
            jefe = Instantiate(prefab, aparicion, Quaternion.identity);
            SpriteRenderer sr = jefe.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.flipX = true;   // mira hacia el dragón
        }

        yield return Mover(jefe, aparicion, posicion, 1.5f, true);
        if (saltar) yield break;

        EfectosJuego.ExplosionJefe(posicion, 2.2f);
        AudioJuego.SonidoExplosionJefe();
        yield return EfectosCine.Instancia.Destello(0.25f);
        Aviso("¡EL SEÑOR DEL ABISMO!", 2.2f);
        yield return Esperar(1.3f);
        if (saltar) yield break;

        // ---- Carga el ataque: un orbe oscuro crece frente a él
        spriteOrbe = CrearSpriteOrbe();
        orbe = CrearOrbe(posicion + new Vector3(-1.6f, -0.4f, 0f));
        AudioJuego.SonidoInvocacion();

        float t = 0f;
        while (t < 1.4f && !saltar)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 1.4f);
            orbe.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 2.4f, k);
            orbe.transform.position = posicion + new Vector3(-1.6f, -0.4f, 0f) + (Vector3)(Random.insideUnitCircle * 0.04f * k);
            if (jefe != null) jefe.transform.position = posicion + (Vector3)(Random.insideUnitCircle * 0.05f);
            yield return null;
        }

        if (saltar) yield break;

        // ---- Lanza el ataque contra el dragón
        Aviso("¡CUIDADO!", 1.2f);
        AudioJuego.SonidoDisparoJefe();

        Vector3 objetivo = DentroDelDragon(j);
        Vector3 direccion = (objetivo - orbe.transform.position).normalized;
        const float velocidad = 7.5f;
        int disparos = 0;
        float proximoDisparo = 0f;
        bool detenido = false;

        while (!detenido && !saltar)
        {
            orbe.transform.position += direccion * velocidad * Time.deltaTime;
            orbe.transform.Rotate(0f, 0f, 240f * Time.deltaTime);

            Vector3 haciaOrbe = (orbe.transform.position - DentroDelDragon(j));
            float distancia = haciaOrbe.magnitude;

            // El dragón planta cara: apunta al orbe y dispara
            if (distancia < 11f)
            {
                j.MirarHacia(false);
                j.ApuntarCinematica(((Vector2)orbe.transform.position - (Vector2)j.arma.PosicionPunta).normalized);

                if (disparos < 5 && Time.time >= proximoDisparo)
                {
                    j.Disparar();
                    disparos++;
                    proximoDisparo = Time.time + 0.16f;
                }
            }

            // El ataque no pasa: estalla antes de llegar a él
            if (distancia < 4.2f && disparos >= 2) detenido = true;

            yield return null;
        }

        Vector3 punto = orbe.transform.position;
        Destroy(orbe);
        orbe = null;

        EfectosJuego.ExplosionJefe(punto, 2.8f);
        AudioJuego.SonidoExplosionJefe();
        yield return EfectosCine.Instancia.Destello(0.3f);
        if (saltar) yield break;

        // ---- El jefe se retira
        Aviso("¡NO PASARÁS!", 1.5f);
        yield return Esperar(0.9f);
        if (saltar) yield break;

        if (jefe != null)
        {
            AudioJuego.SonidoInvocacion();
            yield return Mover(jefe, jefe.transform.position, c + new Vector3(16f, 12f, 0f), 1.6f, false);
        }

        if (saltar) yield break;

        Aviso("EL DRAGÓN VA TRAS ÉL...\nEMPIEZA LA BÚSQUEDA", 3f);
        yield return Esperar(2.2f);
    }

    // ------------------------------------------------------------------ ayudas
    static Vector3 DentroDelDragon(PlayerController j)
    {
        Collider2D col = j.GetComponent<Collider2D>();
        return col != null ? col.bounds.center + Vector3.up * 0.3f : j.transform.position;
    }

    IEnumerator Mover(GameObject g, Vector3 desde, Vector3 hasta, float duracion, bool suave)
    {
        float t = 0f;

        while (t < duracion && !saltar)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracion);
            if (suave) k = Mathf.SmoothStep(0f, 1f, k);

            if (g != null) g.transform.position = Vector3.Lerp(desde, hasta, k);
            yield return null;
        }

        if (g != null && !saltar) g.transform.position = hasta;
    }

    IEnumerator Esperar(float segundos)
    {
        float t = 0f;
        while (t < segundos && !saltar)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    static void Aviso(string mensaje, float duracion)
    {
        if (HUDJuego.Instancia != null) HUDJuego.Instancia.MostrarAviso(mensaje, duracion);
    }

    GameObject CrearOrbe(Vector3 posicion)
    {
        GameObject g = new GameObject("OrbeDelAbismo");
        g.transform.position = posicion;
        g.transform.localScale = Vector3.one * 0.2f;

        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = spriteOrbe;
        sr.color = new Color(0.45f, 0.12f, 0.75f, 1f);
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 13;

        // Halo exterior
        GameObject halo = new GameObject("Halo");
        halo.transform.SetParent(g.transform, false);
        halo.transform.localScale = Vector3.one * 1.5f;
        SpriteRenderer sh = halo.AddComponent<SpriteRenderer>();
        sh.sprite = spriteOrbe;
        sh.color = new Color(0.75f, 0.35f, 1f, 0.35f);
        sh.sortingLayerName = "Player";
        sh.sortingOrder = 12;

        return g;
    }

    static Sprite CrearSpriteOrbe()
    {
        const int tam = 64;
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float r = tam * 0.5f;
        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01((1f - d) * 3f);
                float brillo = Mathf.Lerp(1f, 0.25f, d);   // más claro en el centro
                tex.SetPixel(x, y, new Color(brillo, brillo, brillo, a));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, tam, tam), new Vector2(0.5f, 0.5f), tam / 2f);
    }
}
