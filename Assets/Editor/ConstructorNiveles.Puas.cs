using UnityEditor;
using UnityEngine;

/// <summary>Púas emergentes de los niveles: sprites de Imagen/Spikes, con aviso antes de salir.</summary>
public static partial class ConstructorNiveles
{
    const string RutaPuas = "Assets/Imagen/Spikes/small_metal/small_metal_spike_0";

    static Sprite[] PrepararPuas()
    {
        Sprite[] r = new Sprite[3];
        for (int i = 0; i < 3; i++)
        {
            string ruta = RutaPuas + (i + 1) + ".png";
            TextureImporter imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (imp == null) { Debug.LogError("Falta " + ruta); continue; }

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 80f;
            imp.filterMode = FilterMode.Point;
            imp.alphaIsTransparency = true;

            TextureImporterSettings ajustes = new TextureImporterSettings();
            imp.ReadTextureSettings(ajustes);
            ajustes.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            imp.SetTextureSettings(ajustes);
            imp.SaveAndReimport();

            r[i] = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        }
        return r;
    }

    /// <summary>Grupos de 3 púas en cada x (solo donde el suelo es llano). Salen con aviso y vibración.</summary>
    static void CrearTrampasPuas(int[] xs)
    {
        Sprite[] frames = PrepararPuas();
        if (frames[0] == null) return;

        Transform padre = Padre("Trampas");
        int n = 0;

        foreach (int pista in xs)
        {
            // Cada x es una pista: se busca el suelo llano más cercano (hasta 12 casillas)
            int x = int.MinValue;
            float y = 0f;

            for (int d = 0; d <= 12 && x == int.MinValue; d++)
            {
                foreach (int cand in new[] { pista + d, pista - d })
                {
                    float yc;
                    if (!superficie.TryGetValue(cand, out yc)) continue;

                    bool llano = true;
                    for (int k = -2; k <= 2; k++)
                    {
                        float yk;
                        if (!superficie.TryGetValue(cand + k, out yk) || Mathf.Abs(yk - yc) > 0.01f) llano = false;
                    }

                    if (llano) { x = cand; y = yc; break; }
                }
            }

            if (x == int.MinValue) continue;

            GameObject g = new GameObject("Puas_" + x);
            g.transform.SetParent(padre, false);
            g.transform.position = new Vector3(x + 0.5f, y, 0f);

            BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(2.6f, 0.7f);
            bc.offset = new Vector2(0f, 0.4f);

            ZonaDanio z = g.AddComponent<ZonaDanio>();
            z.tipo = ZonaDanio.Tipo.Puas;
            z.AplicarValoresSugeridos();
            z.danio = 30f;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(g.transform, false);

            for (int k = -1; k <= 1; k++)
            {
                GameObject p = new GameObject("Pua" + k);
                p.transform.SetParent(visual.transform, false);
                p.transform.localPosition = new Vector3(k * 0.9f, 0f, 0f);

                SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = frames[0];
                sr.sortingLayerName = "Player";
                sr.sortingOrder = 2;
            }

            PuasEmergentes pe = g.AddComponent<PuasEmergentes>();
            pe.visual = visual.transform;
            pe.fotogramas = frames;
            pe.altura = 0f;
            pe.tiempoOculta = 1.8f;
            pe.tiempoAviso = 0.7f;
            pe.tiempoActiva = 1.1f;
            pe.desfase = (n % 4) * 0.5f;
            n++;
        }
    }
}
