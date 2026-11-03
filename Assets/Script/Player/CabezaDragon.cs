using UnityEngine;

/// <summary>
/// Cabeza articulada del dragón. El cuerpo y la cabeza son dos recortes del mismo
/// fotograma: la cabeza gira hacia donde se apunta y la boca es el punto de salida
/// de las bolas de fuego.
/// </summary>
public class CabezaDragon : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Nodo que gira. Su pivote está en el cuello.")]
    public Transform pivoteCabeza;
    public SpriteRenderer cuerpo;
    public SpriteRenderer cabeza;
    [Tooltip("Punto de la boca por el que sale el fuego.")]
    public Transform boca;

    [Header("Movimiento")]
    public float anguloMaximo = 78f;
    public float velocidadGiro = 720f;

    [Tooltip("Sprite de cabeza correspondiente a cada sprite de cuerpo (mismo nombre).")]
    public Sprite[] cabezas;

    /// <summary>Dirección de la boca en coordenadas de mundo.</summary>
    public Vector2 DireccionBoca
    {
        get { return ArmaDragon.DireccionDeEje(boca); }
    }

    float anguloActual;
    bool mirandoIzquierda;
    Sprite ultimoCuerpo;

    void Reset()
    {
        pivoteCabeza = transform;
    }

    void LateUpdate()
    {
        SincronizarSprite();
    }

    /// <summary>El cuerpo se ha volteado: la cabeza se refleja con él.</summary>
    public void FijarLado(bool izquierda)
    {
        if (mirandoIzquierda == izquierda) return;

        mirandoIzquierda = izquierda;

        if (pivoteCabeza != null)
        {
            Vector3 s = pivoteCabeza.localScale;
            s.x = izquierda ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
            pivoteCabeza.localScale = s;
        }
    }

    /// <summary>Gira la cabeza hacia la dirección indicada (mundo).</summary>
    public void Apuntar(Vector2 direccionMundo)
    {
        if (pivoteCabeza == null) return;

        if (direccionMundo.sqrMagnitude < 0.0001f) direccionMundo = Vector2.right;

        float lado = mirandoIzquierda ? -1f : 1f;
        float objetivo = Mathf.Atan2(direccionMundo.y, direccionMundo.x * lado) * Mathf.Rad2Deg;

        // Se limita el giro para que la cabeza no se descoyunte
        objetivo = Mathf.Clamp(objetivo, -anguloMaximo, anguloMaximo);

        anguloActual = Mathf.MoveTowardsAngle(anguloActual, objetivo, velocidadGiro * Time.deltaTime);

        // Si el nodo está reflejado en X, el giro local va invertido
        float local = mirandoIzquierda ? -anguloActual : anguloActual;
        pivoteCabeza.localRotation = Quaternion.Euler(0f, 0f, local);
    }

    /// <summary>La cabeza deja de apuntar (muerte, cinemática...).</summary>
    public void Relajar()
    {
        if (pivoteCabeza == null) return;

        anguloActual = Mathf.MoveTowards(anguloActual, 0f, velocidadGiro * Time.deltaTime);
        float local = mirandoIzquierda ? -anguloActual : anguloActual;
        pivoteCabeza.localRotation = Quaternion.Euler(0f, 0f, local);
    }

    void SincronizarSprite()
    {
        if (cuerpo == null || cabeza == null) return;

        Sprite s = cuerpo.sprite;
        if (s == ultimoCuerpo) return;

        ultimoCuerpo = s;
        cabeza.sprite = BuscarCabeza(s);
        // El volteo se hace con la escala del pivote (FijarLado), nunca con flipX,
        // para que el giro de la cabeza siga siendo correcto al mirar a la izquierda.
        cabeza.flipX = false;
    }

    Sprite BuscarCabeza(Sprite spriteCuerpo)
    {
        if (spriteCuerpo == null || cabezas == null) return null;

        string baseCuerpo = Base(spriteCuerpo.name);

        for (int i = 0; i < cabezas.Length; i++)
        {
            if (cabezas[i] == null) continue;
            if (Base(cabezas[i].name) == baseCuerpo) return cabezas[i];
        }

        // Fotogramas sin cabeza propia (el vuelo, por ejemplo): se usa la de reposo
        for (int i = 0; i < cabezas.Length; i++)
            if (cabezas[i] != null && Base(cabezas[i].name) == "idle_01") return cabezas[i];

        return null;
    }

    /// <summary>"idle_01_cuerpo" y "idle_01_cabeza" comparten la misma base.</summary>
    static string Base(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return string.Empty;

        return nombre.Replace("_cuerpo", string.Empty).Replace("_cabeza", string.Empty);
    }
}
