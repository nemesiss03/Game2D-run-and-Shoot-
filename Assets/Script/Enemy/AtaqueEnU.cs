using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ataque en U del jefe: una estela de energía recorre los bordes de la pantalla (baja por la izquierda,
/// cruza por abajo y sube por la derecha). Antes se dibuja el recorrido y se avisa; durante todo el ataque
/// los láseres se apagan para que nunca haya que esquivar las dos cosas a la vez.
/// El tramo de abajo pasa por la altura que tenía el jugador al avisar: se esquiva saltando o con el dash.
/// </summary>
public class AtaqueEnU : MonoBehaviour
{
    public static bool Activo { get; private set; }
    static AtaqueEnU instancia;

    const float TiempoAviso = 2.4f;
    const float Velocidad = 12f;
    const float LargoEstela = 5f;
    const float Danio = 25f;
    const float RadioEstela = 0.4f;
    const float RadioJugador = 0.35f;

    LineRenderer aviso, estela;
    GameObject cabeza;
    Vector2[] relativos;   // la U respecto al centro de la cámara

    public static void Iniciar()
    {
        if (instancia != null) return;

        GameObject g = new GameObject("AtaqueEnU");
        instancia = g.AddComponent<AtaqueEnU>();
        instancia.StartCoroutine(instancia.Ciclo());
    }

    public static void Detener()
    {
        Activo = false;
        SistemaLasers.Pausado = false;

        if (instancia != null) Destroy(instancia.gameObject);
        instancia = null;
    }

    void OnDestroy()
    {
        if (instancia == this)
        {
            instancia = null;
            Activo = false;
            SistemaLasers.Pausado = false;
        }
    }

    IEnumerator Ciclo()
    {
        Activo = true;
        SistemaLasers.Pausado = true;

        Camera cam = Camera.main;
        if (cam == null) { Detener(); yield break; }

        float mitadAlto = cam.orthographicSize;
        float mitadAncho = mitadAlto * cam.aspect;

        // La U: esquina superior izquierda -> abajo -> abajo derecha -> esquina superior derecha
        Transform jugador = Jugadores.MasCercano(cam.transform.position);
        float arriba = mitadAlto - 1.2f;
        float abajo = jugador != null ? Mathf.Clamp(jugador.position.y - cam.transform.position.y, -mitadAlto + 1f, arriba - 3f) : -mitadAlto + 1.5f;
        float izq = -mitadAncho + 1.1f, der = mitadAncho - 1.1f;

        relativos = new[] { new Vector2(izq, arriba), new Vector2(izq, abajo), new Vector2(der, abajo), new Vector2(der, arriba) };

        aviso = CrearLinea("AvisoU", 0.16f, new Color(1f, 0.3f, 0.35f, 0.6f));
        estela = CrearLinea("EstelaU", 0.8f, new Color(0.65f, 0.2f, 1f, 0.9f));
        estela.enabled = false;

        if (HUDJuego.Instancia != null)
            HUDJuego.Instancia.MostrarAviso("¡ATAQUE EN U!\nRECORRE LOS BORDES · LÁSERES APAGADOS", TiempoAviso);
        AudioJuego.SonidoInvocacion();

        // ---- Aviso: la U parpadea
        float t = 0f;
        while (t < TiempoAviso)
        {
            t += Time.deltaTime;
            float p = t / TiempoAviso;
            float parpadeo = Mathf.Sin(t * Mathf.Lerp(8f, 30f, p)) > 0f ? 1f : 0.3f;

            PintarPolilinea(aviso, Puntos(cam), 0f, float.MaxValue, new Color(1f, 0.3f, 0.35f, Mathf.Lerp(0.3f, 0.9f, p) * parpadeo));
            yield return null;
        }

        aviso.enabled = false;

        // ---- Barrido
        cabeza = new GameObject("CabezaU");
        cabeza.transform.SetParent(transform, false);
        SpriteRenderer sr = cabeza.AddComponent<SpriteRenderer>();
        sr.sprite = CrearCirculo();
        sr.color = new Color(0.85f, 0.5f, 1f, 1f);
        sr.sortingLayerName = "Player";
        sr.sortingOrder = 24;
        cabeza.transform.localScale = Vector3.one * 1.3f;

        AudioJuego.SonidoDisparoJefe();

        HashSet<Salud> golpeados = new HashSet<Salud>();
        float s = 0f;
        float largoTotal = Longitud(Puntos(cam));

        while (s < largoTotal + LargoEstela)
        {
            s += Velocidad * Time.deltaTime;
            Vector2[] pts = Puntos(cam);

            float desde = Mathf.Max(0f, s - LargoEstela);
            float hasta = Mathf.Min(s, largoTotal);

            PintarPolilinea(estela, pts, desde, hasta, new Color(0.65f, 0.2f, 1f, 0.9f));
            cabeza.transform.position = PuntoEn(pts, hasta);
            cabeza.SetActive(s <= largoTotal);

            for (float x = desde; x <= hasta; x += 0.35f) ComprobarImpacto(PuntoEn(pts, x), golpeados);

            yield return null;
        }

        Destroy(gameObject);
    }

    void ComprobarImpacto(Vector2 punto, HashSet<Salud> golpeados)
    {
        for (int i = 0; i < Jugadores.Todos.Count; i++)
        {
            PlayerController j = Jugadores.Todos[i];
            if (j == null || j.Muerto) continue;

            Salud sa = j.GetComponent<Salud>();
            if (sa == null || golpeados.Contains(sa)) continue;

            Collider2D c = j.GetComponent<Collider2D>();
            Vector2 centro = c != null ? (Vector2)c.bounds.center : (Vector2)j.transform.position;

            if (Vector2.Distance(centro, punto) <= RadioEstela + RadioJugador)
            {
                golpeados.Add(sa);
                sa.RecibirDanio(Danio);   // el dash da invulnerabilidad
            }
        }
    }

    // ------------------------------------------------------------ geometría
    Vector2[] Puntos(Camera cam)
    {
        Vector2 c = cam.transform.position;
        Vector2[] r = new Vector2[relativos.Length];
        for (int i = 0; i < r.Length; i++) r[i] = c + relativos[i];
        return r;
    }

    static float Longitud(Vector2[] p)
    {
        float l = 0f;
        for (int i = 1; i < p.Length; i++) l += Vector2.Distance(p[i - 1], p[i]);
        return l;
    }

    static Vector2 PuntoEn(Vector2[] p, float distancia)
    {
        for (int i = 1; i < p.Length; i++)
        {
            float l = Vector2.Distance(p[i - 1], p[i]);
            if (distancia <= l) return Vector2.Lerp(p[i - 1], p[i], l <= 0f ? 0f : distancia / l);
            distancia -= l;
        }
        return p[p.Length - 1];
    }

    static void PintarPolilinea(LineRenderer lr, Vector2[] p, float desde, float hasta, Color color)
    {
        float total = Longitud(p);
        desde = Mathf.Clamp(desde, 0f, total);
        hasta = Mathf.Clamp(hasta, 0f, total);

        List<Vector3> v = new List<Vector3>();
        v.Add(PuntoEn(p, desde));

        float acumulado = 0f;
        for (int i = 1; i < p.Length; i++)
        {
            acumulado += Vector2.Distance(p[i - 1], p[i]);
            if (acumulado > desde && acumulado < hasta) v.Add(p[i]);
        }

        v.Add(PuntoEn(p, hasta));

        lr.enabled = true;
        lr.positionCount = v.Count;
        lr.SetPositions(v.ToArray());
        lr.startColor = color;
        lr.endColor = color;
    }

    LineRenderer CrearLinea(string nombre, float ancho, Color color)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(transform, false);

        LineRenderer lr = g.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.numCapVertices = 6;
        lr.numCornerVertices = 6;
        lr.startWidth = ancho;
        lr.endWidth = ancho;
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingLayerName = "Player";
        lr.sortingOrder = 23;
        return lr;
    }

    static Sprite CrearCirculo()
    {
        const int tam = 64;
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float r = tam * 0.5f;
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * 4f)));
            }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, tam, tam), new Vector2(0.5f, 0.5f), tam);
    }
}
