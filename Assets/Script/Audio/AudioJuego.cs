using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Servicio de audio de "Spirit Dragon". Se crea solo y sobrevive al cambio de escena.
/// Todo se configura en Assets/Resources/Audio/BancoAudio.asset.
/// </summary>
public class AudioJuego : MonoBehaviour
{
    public static AudioJuego Instancia { get; private set; }

    [Header("Volumen")]
    [Range(0f, 1f)] public float volumenEfectos = 0.85f;
    [Range(0f, 1f)] public float volumenMusica = 0.30f;

    BancoAudio banco;
    AudioSource[] fuentes;
    AudioSource fuenteMusica;
    Coroutine fundido;
    int siguiente;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Arrancar()
    {
        if (Instancia != null) return;

        GameObject go = new GameObject("AudioJuego");
        go.AddComponent<AudioJuego>();
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        DontDestroyOnLoad(gameObject);

        banco = Resources.Load<BancoAudio>(BancoAudio.RutaResources);
        if (banco == null)
            Debug.LogWarning("AudioJuego: no encuentro Assets/Resources/Audio/BancoAudio.asset. " +
                             "Créalo con el menú Spirit Dragon > Crear banco de audio.");

        fuentes = new AudioSource[10];
        for (int i = 0; i < fuentes.Length; i++)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 1f;
            fuentes[i] = s;
        }

        fuenteMusica = gameObject.AddComponent<AudioSource>();
        fuenteMusica.playOnAwake = false;
        fuenteMusica.loop = true;
        fuenteMusica.spatialBlend = 0f;
        fuenteMusica.volume = volumenMusica;

        SceneManager.sceneLoaded += AlCargarEscena;
    }

    void Start()
    {
        // Por si la primera escena ya estaba cargada cuando nació este objeto
        AplicarMusicaDeEscena(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        if (Instancia == this) Instancia = null;
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (modo != LoadSceneMode.Single) return;
        AplicarMusicaDeEscena(escena.name);
    }

    void AplicarMusicaDeEscena(string nombreEscena)
    {
        if (banco == null) return;

        EntradaMusica entrada = banco.BuscarMusica(nombreEscena);
        if (entrada == null) return;   // escena sin entrada: sigue lo que esté sonando

        CambiarMusica(entrada.musica, entrada.volumen);
    }

    // ------------------------------------------------------------------ API
    /// <summary>Toca el sonido asignado a una acción en el banco de audio.</summary>
    public static void Tocar(SonidoId id)
    {
        if (Instancia != null) Instancia.TocarEfecto(id);
    }

    // Atajos de siempre (los demás scripts siguen usándolos)
    public static void SonidoDisparo() { Tocar(SonidoId.Disparo); }
    public static void SonidoDisparoJefe() { Tocar(SonidoId.DisparoJefe); }
    public static void SonidoInvocacion() { Tocar(SonidoId.Invocacion); }
    public static void SonidoImpactoJefe() { Tocar(SonidoId.ImpactoJefe); }
    public static void SonidoImpactoEsbirro() { Tocar(SonidoId.ImpactoEsbirro); }
    public static void SonidoDanioJugador() { Tocar(SonidoId.DanioJugador); }
    public static void SonidoDash() { Tocar(SonidoId.Dash); }
    public static void SonidoCuracion() { Tocar(SonidoId.Curacion); }
    public static void SonidoExplosionEsbirro() { Tocar(SonidoId.ExplosionEsbirro); }
    public static void SonidoExplosionJefe() { Tocar(SonidoId.ExplosionJefe); }
    public static void SonidoSalto() { Tocar(SonidoId.Salto); }

    public static void AjustarMusica(float volumen)
    {
        if (Instancia == null || Instancia.fuenteMusica == null) return;
        Instancia.volumenMusica = Mathf.Clamp01(volumen);
        Instancia.fuenteMusica.volume = Instancia.volumenMusica;
    }

    /// <summary>Tema de la pelea contra el jefe (lo llama CinematicaJefe).</summary>
    public static void MusicaBatalla()
    {
        if (Instancia == null || Instancia.banco == null || Instancia.banco.musicaBatalla == null) return;
        Instancia.CambiarMusica(Instancia.banco.musicaBatalla, Instancia.banco.volumenBatalla);
    }

    /// <summary>Vuelve a la música de la escena actual.</summary>
    public static void MusicaFondo()
    {
        if (Instancia != null) Instancia.AplicarMusicaDeEscena(SceneManager.GetActiveScene().name);
    }

    /// <summary>Pone cualquier música por código (null = silencio).</summary>
    public static void PonerMusica(AudioClip clip, float volumen)
    {
        if (Instancia != null) Instancia.CambiarMusica(clip, volumen);
    }

    // -------------------------------------------------------------- interno
    void TocarEfecto(SonidoId id)
    {
        if (banco == null) return;

        EfectoAudio e = banco.BuscarEfecto(id);
        if (e == null || e.clips == null || e.clips.Length == 0) return;

        AudioClip clip = e.clips[Random.Range(0, e.clips.Length)];
        if (clip == null) return;

        TocarInterno(clip, e.volumen, Random.Range(e.tonoMinimo, e.tonoMaximo));
    }

    void TocarInterno(AudioClip clip, float volumen, float tono)
    {
        if (fuentes == null || fuentes.Length == 0) return;

        AudioSource s = fuentes[siguiente];
        siguiente = (siguiente + 1) % fuentes.Length;

        s.pitch = tono;
        s.PlayOneShot(clip, Mathf.Clamp01(volumen * volumenEfectos));
    }

    void CambiarMusica(AudioClip clip, float volumen)
    {
        if (fuenteMusica == null) return;

        if (clip != null && fuenteMusica.clip == clip && fuenteMusica.isPlaying)
        {
            volumenMusica = volumen;
            fuenteMusica.volume = volumen;
            return;
        }

        if (clip == null && !fuenteMusica.isPlaying) return;

        if (fundido != null) StopCoroutine(fundido);
        fundido = StartCoroutine(FundirMusica(clip, volumen));
    }

    IEnumerator FundirMusica(AudioClip clip, float volumen)
    {
        const float bajo = 0.6f;
        const float sube = 0.9f;

        if (fuenteMusica.isPlaying)
        {
            float inicial = fuenteMusica.volume;
            for (float t = 0f; t < bajo; t += Time.unscaledDeltaTime)
            {
                fuenteMusica.volume = Mathf.Lerp(inicial, 0f, t / bajo);
                yield return null;
            }
        }

        fuenteMusica.volume = 0f;

        if (clip == null)
        {
            fuenteMusica.Stop();
            fuenteMusica.clip = null;
            fundido = null;
            yield break;
        }

        fuenteMusica.clip = clip;
        fuenteMusica.Play();

        for (float t = 0f; t < sube; t += Time.unscaledDeltaTime)
        {
            fuenteMusica.volume = Mathf.Lerp(0f, volumen, t / sube);
            yield return null;
        }

        fuenteMusica.volume = volumen;
        volumenMusica = volumen;
        fundido = null;
    }
}