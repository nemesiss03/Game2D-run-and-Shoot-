using UnityEditor;
using UnityEngine;

/// <summary>
/// Coloca la pistola del dragón en la escena abierta y la conecta al PlayerController.
/// El disparo sale del cañón (Punta), no de la boca.
///   Menú: Spirit Dragon/Reconstruir arma
/// </summary>
public static class ConstructorArma
{
    const string RutaPistola = "Assets/Mad Scientist's Games/2D Kit - Firearms Weapon/Weapons/1. Pistols/Pistol 1/72 ppi/Pistol 1 (72 ppi).png";
    const string RutaBolaFuego = "Assets/Prefab/BolaDeFuego.prefab";

    // Posiciones locales dentro del dragón. Se recupera la geometría con la que
    // la pistola se veía bien en la arena: pivote a la altura del pecho, arma
    // separada del cuerpo y escala 0.27.
    static readonly Vector3 PosicionPivote = new Vector3(0f, -0.30f, 0f);
    static readonly Vector3 PosicionPistola = new Vector3(0.55f, 0.10f, 0f);
    const float EscalaPistola = 0.27f;

    // Boca del cañón medida sobre el sprite (184x139 px, PPU 100): el extremo
    // derecho del cañón está en el píxel (183, 122.5), es decir a 0.91 unidades
    // a la derecha y 0.53 por encima del centro del sprite.
    static readonly Vector2 PuntaEnSprite = new Vector2(0.91f, 0.53f);

    [MenuItem("Spirit Dragon/Reconstruir arma")]
    public static void Reconstruir()
    {
        GameObject player = GameObject.Find("Arena/Player");
        if (player == null)
        {
            Debug.LogError("ConstructorArma: no se encuentra Arena/Player en la escena abierta.");
            return;
        }

        PlayerController control = player.GetComponent<PlayerController>();
        if (control == null)
        {
            Debug.LogError("ConstructorArma: Arena/Player no tiene PlayerController.");
            return;
        }

        Sprite spritePistola = AssetDatabase.LoadAssetAtPath<Sprite>(RutaPistola);
        if (spritePistola == null)
        {
            Debug.LogError("ConstructorArma: no se encuentra el sprite de la pistola.");
            return;
        }

        // Fuera el arma anterior
        Transform viejo = player.transform.Find("ArmaPivot");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);

        // Pivote que gira
        GameObject pivote = new GameObject("ArmaPivot");
        pivote.transform.SetParent(player.transform, false);
        pivote.transform.localPosition = PosicionPivote;
        pivote.transform.localRotation = Quaternion.identity;
        pivote.transform.localScale = Vector3.one;

        // Sprite de la pistola
        GameObject pistola = new GameObject("Pistol 1 (72 ppi)");
        pistola.transform.SetParent(pivote.transform, false);
        pistola.transform.localPosition = PosicionPistola;
        pistola.transform.localRotation = Quaternion.identity;
        pistola.transform.localScale = Vector3.one * EscalaPistola;

        SpriteRenderer srPistola = pistola.AddComponent<SpriteRenderer>();
        srPistola.sprite = spritePistola;
        srPistola.sortingOrder = 7;

        // Boca del cañón: hijo del PIVOTE (no del sprite) para que el espejo del
        // arma no mueva la punta, y en el extremo real del cañón.
        Vector3 puntaLocal = PosicionPistola + new Vector3(PuntaEnSprite.x, PuntaEnSprite.y, 0f) * EscalaPistola;

        GameObject punta = new GameObject("Punta");
        punta.transform.SetParent(pivote.transform, false);
        punta.transform.localPosition = puntaLocal;
        punta.transform.localRotation = Quaternion.identity;
        punta.transform.localScale = Vector3.one;

        // Script del arma
        ArmaDragon arma = pivote.GetComponent<ArmaDragon>();
        if (arma == null) arma = pivote.AddComponent<ArmaDragon>();

        arma.pivote = pivote.transform;
        arma.pistola = pistola.transform;
        arma.punta = punta.transform;
        arma.anguloMaximo = 89f;
        arma.velocidadGiro = 1500f;

        // Conexión con el jugador
        control.arma = arma;
        control.bolaFuegoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaBolaFuego);

        if (control.bolaFuegoPrefab == null)
            Debug.LogWarning("ConstructorArma: no se encuentra " + RutaBolaFuego);

        EditorUtility.SetDirty(control);
        EditorUtility.SetDirty(arma);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);

        Debug.Log("ConstructorArma: pistola montada y conectada al PlayerController.");
    }
}
