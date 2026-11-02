using UnityEngine;

[System.Serializable]
public class CapaParalax
{
    public Sprite sprite;
    [Tooltip("Cuánto sigue a la cámara en horizontal. 0 = pegada a la cámara (cielo lejano), 1 = fija al mundo.")]
    [Range(0f, 1f)] public float factorX = 0.1f;
    [Tooltip("Lo mismo en vertical.")]
    [Range(0f, 1f)] public float factorY = 0.9f;
    public int orden = 0;
    public float escala = 1f;
}

/// <summary>
/// Fondo con parallax: varias capas que se mueven a distinta velocidad según la cámara.
/// Cada capa se repite en horizontal, así el escenario nunca se acaba.
/// </summary>
public class FondoParallax : MonoBehaviour
{
    public CapaParalax[] capas;
    public string capaDeOrdenamiento = "Fondo";

    Transform[][] copias;
    float[] anchos;
    Vector2 origenCamara;
    bool listo;

    const int Copias = 3;

    void Start()
    {
        Camera cam = Camera.main;
        if (cam == null || capas == null) return;

        origenCamara = cam.transform.position;

        copias = new Transform[capas.Length][];
        anchos = new float[capas.Length];

        for (int i = 0; i < capas.Length; i++)
        {
            CapaParalax c = capas[i];
            if (c.sprite == null) continue;

            anchos[i] = c.sprite.bounds.size.x * c.escala;
            copias[i] = new Transform[Copias];

            for (int k = 0; k < Copias; k++)
            {
                GameObject g = new GameObject("Capa" + i + "_" + k);
                g.transform.SetParent(transform, false);
                g.transform.localScale = Vector3.one * c.escala;

                SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = c.sprite;
                sr.sortingLayerName = capaDeOrdenamiento;
                sr.sortingOrder = c.orden;

                copias[i][k] = g.transform;
            }
        }

        listo = true;
        Colocar();
    }

    void LateUpdate()
    {
        if (listo) Colocar();
    }

    void Colocar()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 p = cam.transform.position;

        for (int i = 0; i < capas.Length; i++)
        {
            if (copias[i] == null) continue;

            CapaParalax c = capas[i];
            float w = anchos[i];

            // La capa se desplaza solo una parte de lo que se mueve la cámara
            float desplazamientoX = (p.x - origenCamara.x) * c.factorX;
            float centroX = origenCamara.x + desplazamientoX;
            float y = origenCamara.y + (p.y - origenCamara.y) * c.factorY;

            // Copia más cercana al centro de la cámara; las otras van a sus lados
            float indiceCentral = Mathf.Round((p.x - centroX) / w);

            for (int k = 0; k < Copias; k++)
            {
                float x = centroX + (indiceCentral + (k - 1)) * w;
                copias[i][k].position = new Vector3(x, y, 5f);
            }
        }
    }
}
