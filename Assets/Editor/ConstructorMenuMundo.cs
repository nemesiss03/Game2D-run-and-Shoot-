using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Cambia el fondo del menú: en vez del fondo pintado, el mundo del juego (paisaje con parallax)
/// visto desde fuera. Se puede ejecutar varias veces sobre MenuPrincipal.
/// El fondo pintado y las nubes se desactivan, no se borran, por si se quiere volver atrás.
/// </summary>
public static class ConstructorMenuMundo
{
    const string Carpeta = "Assets/Imagen/Montana";

    [MenuItem("Spirit Dragon/Menú: fondo de mundo exterior")]
    public static void Construir()
    {
        if (EditorSceneManager.GetActiveScene().name != "MenuPrincipal")
        {
            Debug.LogError("Abre primero la escena MenuPrincipal.");
            return;
        }

        // Quitar el fondo pintado y las nubes de UI
        string[] viejos = { "Fondo", "Nube1", "Nube2", "Nube3", "Nube4", "Nube5" };
        GameObject canvas = GameObject.Find("CanvasMenu");
        foreach (string n in viejos)
        {
            Transform t = canvas != null ? canvas.transform.Find(n) : null;
            if (t != null) t.gameObject.SetActive(false);
        }

        // Rehacer el mundo desde cero si ya existía
        GameObject previo = GameObject.Find("MundoMenu");
        if (previo != null) Object.DestroyImmediate(previo);

        GameObject go = new GameObject("MundoMenu");
        FondoParallax p = go.AddComponent<FondoParallax>();
        go.AddComponent<MenuMundo>();

        string[] capas = { "cielo", "nubes", "lejanas", "cercanas", "bosque", "pinos" };
        float[] fx = { 0f, 0.02f, 0.06f, 0.12f, 0.26f, 0.48f };
        float[] fy = { 1f, 1f, 0.95f, 0.90f, 0.80f, 0.70f };
        float[] dy = { 0f, 6.25f, 6.25f, 6.25f, 6.25f, 6.25f };

        p.capas = new CapaParalax[capas.Length];
        for (int i = 0; i < capas.Length; i++)
        {
            p.capas[i] = new CapaParalax
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Carpeta + "/fondo_" + capas[i] + ".png"),
                factorX = fx[i],
                factorY = fy[i],
                desfaseY = dy[i],
                orden = i,
                escala = 1f
            };
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Menú: fondo de mundo exterior creado (MundoMenu).");
    }
}
