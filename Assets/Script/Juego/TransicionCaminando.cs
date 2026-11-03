using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Paso de Nivel1 a Nivel2 caminando: al tocar la bandera el dragón sigue andando hacia la derecha,
/// el fondo del bosque se funde con el de la montaña, las hojas dejan paso a la nieve y, al final,
/// la pantalla se funde a negro y Nivel2 empieza con el dragón entrando andando.
/// </summary>
public static class TransicionCaminando
{
    /// <summary>Verdadero entre un nivel y el siguiente: Nivel2 sabe que el dragón llega caminando.</summary>
    public static bool Pendiente;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpiar() { Pendiente = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Registrar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (!Pendiente) return;
        Pendiente = false;

        new GameObject("LlegadaCaminando").AddComponent<LlegadaCaminando>();
    }

    /// <summary>Corrutina completa de la salida: se usa con "yield return" desde la bandera.</summary>
    public static IEnumerator Ejecutar(string escenaSiguiente, float segundos)
    {
        EfectosCine cine = EfectosCine.Instancia;

        // Los jugadores caminan solos hacia la derecha
        for (int i = 0; i < Jugadores.Todos.Count; i++)
        {
            PlayerController p = Jugadores.Todos[i];
            if (p == null) continue;

            Salud s = p.GetComponent<Salud>();
            if (s != null) s.invulnerable = true;

            p.AsignarEntrada(new EntradaAutomatica { horizontal = 1f });
            p.controlActivo = true;
            p.MirarHacia(false);
        }

        yield return cine.Barras(true, 0.6f);

        FondoParallax montana = BuscarFondoMontana();
        AmbienteCayendo hojas = null, nieve = null;
        foreach (AmbienteCayendo a in Resources.FindObjectsOfTypeAll<AmbienteCayendo>())
        {
            if (!a.gameObject.scene.IsValid()) continue;
            if (a.tipo == AmbienteCayendo.Tipo.Hojas) hojas = a;
            else nieve = a;
        }

        float t = 0f;
        bool cambiado = false;

        while (t < segundos)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / segundos);

            // El fondo cambia poco a poco del bosque a la montaña
            if (montana != null) montana.FijarAlfa(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - 0.1f) / 0.7f)));

            // A mitad de camino dejan de caer hojas y empieza a nevar
            if (!cambiado && k > 0.45f)
            {
                cambiado = true;
                if (hojas != null) hojas.gameObject.SetActive(false);
                if (nieve != null) nieve.gameObject.SetActive(true);
            }

            yield return null;
        }

        // Fundido a negro y carga del siguiente nivel
        yield return cine.Fundido(1f, 0.8f, Color.black);

        Pendiente = true;
        SceneManager.LoadScene(escenaSiguiente);
    }

    static FondoParallax BuscarFondoMontana()
    {
        foreach (FondoParallax f in Object.FindObjectsByType<FondoParallax>(FindObjectsSortMode.None))
            if (f.name == "FondoMontana") return f;

        return null;
    }
}
