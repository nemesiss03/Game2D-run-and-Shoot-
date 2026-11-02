using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class SalidaNivel : MonoBehaviour
{
    [Tooltip("Nombre EXACTO de la escena siguiente (Nivel2, Boss...).")]
    public string escenaSiguiente = "Boss";
    public bool abierta = false;
    [Tooltip("Opcional: objeto que se muestra cuando la salida está abierta.")]
    public GameObject visual;

    void Start()
    {
        if (visual != null) visual.SetActive(abierta);
    }

    public void Abrir()
    {
        abierta = true;
        if (visual != null) visual.SetActive(true);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!abierta) return;
        if (otro.GetComponentInParent<PlayerController>() == null) return;
        SceneManager.LoadScene(escenaSiguiente);
    }
}