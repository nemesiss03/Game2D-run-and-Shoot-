using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(ZonaDanio))]
public class PuasEmergentes : MonoBehaviour
{
    enum Fase { Oculta, Aviso, Activa }

    [Header("Visual")]
    [Tooltip("Hijo con el sprite de las púas. Colócalo escondido DENTRO del suelo.")]
    public Transform visual;
    public float altura = 0.8f;
    public float velocidadSubida = 14f;
    public float velocidadBajada = 5f;
    [Tooltip("Cuánto tiembla el sprite durante el aviso.")]
    public float vibracionAviso = 0.04f;

    [Header("Fotogramas (opcional)")]
    [Tooltip("Oculta, aviso y activa. Si se rellena, las púas cambian de dibujo en vez de subir desde el suelo.")]
    public Sprite[] fotogramas;

    [Header("Ciclo (segundos)")]
    public float tiempoOculta = 2f;
    public float tiempoAviso = 0.5f;
    public float tiempoActiva = 1f;
    [Tooltip("Retraso inicial. Usa valores distintos para que no salgan todas a la vez.")]
    public float desfase = 0f;

    [Header("Eventos")]
    public UnityEvent alAvisar;
    public UnityEvent alEmerger;

    ZonaDanio zona;
    Fase fase = Fase.Oculta;
    float reloj;
    float alturaActual;
    Vector3 posBase;
    SpriteRenderer[] dibujos;

    void Awake()
    {
        zona = GetComponent<ZonaDanio>();
    }

    void Start()
    {
        if (visual != null)
        {
            posBase = visual.localPosition;
            dibujos = visual.GetComponentsInChildren<SpriteRenderer>();
        }
        PonerDibujo(Fase.Oculta);
        reloj = -desfase;
        zona.activa = false;
    }

    void Update()
    {
        reloj += Time.deltaTime;

        switch (fase)
        {
            case Fase.Oculta:
                if (reloj >= tiempoOculta) CambiarFase(Fase.Aviso);
                break;
            case Fase.Aviso:
                if (reloj >= tiempoAviso) CambiarFase(Fase.Activa);
                break;
            case Fase.Activa:
                if (reloj >= tiempoActiva) CambiarFase(Fase.Oculta);
                break;
        }

        float objetivo = fase == Fase.Activa ? altura : (fase == Fase.Aviso ? altura * 0.15f : 0f);
        float vel = objetivo > alturaActual ? velocidadSubida : velocidadBajada;
        alturaActual = Mathf.MoveTowards(alturaActual, objetivo, vel * Time.deltaTime);

        if (visual != null)
        {
            float temblor = fase == Fase.Aviso ? Mathf.Sin(Time.time * 60f) * vibracionAviso : 0f;
            visual.localPosition = posBase + new Vector3(temblor, alturaActual, 0f);
        }
    }

    void CambiarFase(Fase nueva)
    {
        fase = nueva;
        reloj = 0f;
        PonerDibujo(nueva);
        zona.activa = nueva == Fase.Activa;

        if (nueva == Fase.Aviso) alAvisar.Invoke();
        if (nueva == Fase.Activa) alEmerger.Invoke();
    }

    void PonerDibujo(Fase f)
    {
        if (fotogramas == null || fotogramas.Length == 0 || dibujos == null) return;

        int i = Mathf.Min((int)f, fotogramas.Length - 1);
        for (int k = 0; k < dibujos.Length; k++) dibujos[k].sprite = fotogramas[i];
    }
}
