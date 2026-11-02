using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Puntos arriba a la derecha, barra de recarga del dash y marca del salto extra abajo a la izquierda.
/// Se crea sola en cualquier escena de juego (no en el menú) y dibuja su propio Canvas.
/// </summary>
public class HUDExtras : MonoBehaviour
{
    Text textoPuntos;
    Image rellenoDash;
    Image fondoDash;
    Text etiquetaDash;
    Image marcaSalto;
    Text etiquetaSalto;
    float puntosMostrados;

    static readonly Color Dorado = new Color(1f, 0.86f, 0.3f, 1f);
    static readonly Color Azul = new Color(0.45f, 0.82f, 1f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        // Este método solo corre al arrancar; para las demás escenas escuchamos la carga
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        Crear();
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo) { Crear(); }

    static void Crear()
    {
        // En todas las escenas de juego; el menú no lleva puntos ni dash
        if (SceneManager.GetActiveScene().name == ConfiguracionPartida.EscenaMenu) return;
        if (FindAnyObjectByType<HUDExtras>() != null) return;

        GameObject go = new GameObject("HUDExtras");
        go.AddComponent<HUDExtras>();
    }

    void Awake()
    {
        Construir();
        puntosMostrados = Puntuacion.Total;
    }

    void Construir()
    {
        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler esc = gameObject.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1600f, 900f);
        esc.matchWidthOrHeight = 0.5f;

        // --- Puntos (arriba a la derecha) ---
        textoPuntos = CrearTexto("Puntos", fuente, 44, Dorado, TextAnchor.UpperRight);
        Anclar(textoPuntos.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -22f), new Vector2(520f, 60f));

        // --- Dash (abajo a la izquierda, encima de la vida) ---
        etiquetaDash = CrearTexto("EtiquetaDash", fuente, 22, Azul, TextAnchor.MiddleLeft);
        etiquetaDash.text = "DASH";
        Anclar(etiquetaDash.rectTransform, new Vector2(0f, 0f), new Vector2(34f, 178f), new Vector2(90f, 30f), new Vector2(0f, 0.5f));

        fondoDash = CrearImagen("FondoDash", new Color(0f, 0f, 0f, 0.55f));
        Anclar(fondoDash.rectTransform, new Vector2(0f, 0f), new Vector2(110f, 178f), new Vector2(200f, 18f), new Vector2(0f, 0.5f));

        rellenoDash = CrearImagen("RellenoDash", Azul);
        rellenoDash.transform.SetParent(fondoDash.transform, false);
        rellenoDash.rectTransform.anchorMin = Vector2.zero;
        rellenoDash.rectTransform.anchorMax = Vector2.one;
        rellenoDash.rectTransform.offsetMin = new Vector2(2f, 2f);
        rellenoDash.rectTransform.offsetMax = new Vector2(-2f, -2f);
        rellenoDash.type = Image.Type.Filled;
        rellenoDash.fillMethod = Image.FillMethod.Horizontal;

        // --- Salto extra ---
        etiquetaSalto = CrearTexto("EtiquetaSalto", fuente, 22, Azul, TextAnchor.MiddleLeft);
        etiquetaSalto.text = "SALTO EXTRA";
        Anclar(etiquetaSalto.rectTransform, new Vector2(0f, 0f), new Vector2(34f, 148f), new Vector2(190f, 30f), new Vector2(0f, 0.5f));

        marcaSalto = CrearImagen("MarcaSalto", Azul);
        Anclar(marcaSalto.rectTransform, new Vector2(0f, 0f), new Vector2(230f, 148f), new Vector2(18f, 18f), new Vector2(0f, 0.5f));
    }

    void Update()
    {
        // Los puntos suben contando
        puntosMostrados = Mathf.MoveTowards(puntosMostrados, Puntuacion.Total, Mathf.Max(60f, Mathf.Abs(Puntuacion.Total - puntosMostrados) * 6f) * Time.deltaTime);
        textoPuntos.text = "PUNTOS  " + Mathf.RoundToInt(puntosMostrados);

        PlayerController j = Jugadores.Todos.Count > 0 ? Jugadores.Todos[0] : null;

        bool visible = j != null;
        etiquetaDash.enabled = visible;
        fondoDash.enabled = visible;
        rellenoDash.enabled = visible;
        etiquetaSalto.enabled = visible && j.saltosExtra > 0;
        marcaSalto.enabled = visible && j.saltosExtra > 0;

        if (!visible) return;

        float p = j.ProgresoDash;
        rellenoDash.fillAmount = p;
        rellenoDash.color = p >= 1f ? Azul : new Color(0.95f, 0.6f, 0.25f, 1f);
        
        bool hayExtra = j.SaltosExtraRestantes > 0;
        marcaSalto.color = hayExtra ? Azul : new Color(0.3f, 0.3f, 0.35f, 0.8f);
    }

    // ------------------------------------------------------------ helpers
    Text CrearTexto(string nombre, Font fuente, int tam, Color color, TextAnchor anclaje)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(transform, false);

        Text t = go.AddComponent<Text>();
        t.font = fuente;
        t.fontSize = tam;
        t.color = color;
        t.alignment = anclaje;
        t.fontStyle = FontStyle.Bold;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;

        Outline o = go.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.85f);
        o.effectDistance = new Vector2(2f, -2f);

        return t;
    }

    Image CrearImagen(string nombre, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(transform, false);

        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static void Anclar(RectTransform rt, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        Anclar(rt, ancla, pos, tam, ancla);
    }

    static void Anclar(RectTransform rt, Vector2 ancla, Vector2 pos, Vector2 tam, Vector2 pivote)
    {
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }
}
