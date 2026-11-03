using System.Collections;
using UnityEngine;

/// <summary>
/// Final del combate: tras la explosión del jefe el dragón se eleva batiendo las alas, la cámara lo sigue
/// y la pantalla de victoria aparece mientras vuela.
/// Lo lanza JefeFlotante al morir con CinematicaVictoria.Iniciar().
/// </summary>
public class CinematicaVictoria : MonoBehaviour
{
    static bool enMarcha;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpiar() { enMarcha = false; }

    public static void Iniciar()
    {
        if (enMarcha) return;
        enMarcha = true;

        GameObject go = new GameObject("CinematicaVictoria");
        go.AddComponent<CinematicaVictoria>();
    }

    void OnDestroy() { enMarcha = false; }

    IEnumerator Start()
    {
        // Deja que se vean las explosiones del jefe
        yield return new WaitForSeconds(1.1f);

        for (int i = 0; i < Jugadores.Todos.Count; i++)
            if (Jugadores.Todos[i] != null) Jugadores.Todos[i].IniciarVueloVictoria();

        // La cámara deja de estar quieta y sigue al dragón que vuela
        CamaraSeguir cam = FindAnyObjectByType<CamaraSeguir>();
        if (cam != null)
        {
            cam.Liberar();
            if (cam.objetivo == null && Jugadores.Todos.Count > 0) cam.objetivo = Jugadores.Todos[0].transform;
        }

        yield return new WaitForSeconds(1.5f);

        // La pantalla de victoria aparece mientras el dragón sigue volando
        if (HUDJuego.Instancia != null) HUDJuego.Instancia.MostrarVictoria();
    }
}
