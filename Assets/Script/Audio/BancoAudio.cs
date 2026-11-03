using System;
using System.Collections.Generic;
using UnityEngine;

public enum SonidoId
{
    Disparo, DisparoJefe, Invocacion, ImpactoJefe, ImpactoEsbirro, DanioJugador,
    Dash, Curacion, ExplosionEsbirro, ExplosionJefe,
    // Nuevos. Los sonidos nuevos se agregan SIEMPRE AL FINAL de esta lista.
    Salto, Aterrizaje, Recarga, MuerteJugador
}

[Serializable]
public class EfectoAudio
{
    public SonidoId sonido;
    [Tooltip("Si pones varios clips, se elige uno al azar cada vez.")]
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volumen = 0.7f;
    public float tonoMinimo = 0.95f;
    public float tonoMaximo = 1.05f;
}

[Serializable]
public class EntradaMusica
{
    [Tooltip("Nombre EXACTO de la escena (Nivel1, Nivel2, Boss...).")]
    public string escena;
    [Tooltip("Vacío = silencio en esa escena.")]
    public AudioClip musica;
    [Range(0f, 1f)] public float volumen = 0.3f;
}

[CreateAssetMenu(fileName = "BancoAudio", menuName = "Spirit Dragon/Banco de audio")]
public class BancoAudio : ScriptableObject
{
    public const string RutaResources = "Audio/BancoAudio";

    [Header("Música por escena (si una escena no está en la lista, sigue lo que suene)")]
    public List<EntradaMusica> musicaPorEscena = new List<EntradaMusica>();

    [Header("Música de la pelea contra el jefe (suena al acabar la cinemática)")]
    public AudioClip musicaBatalla;
    [Range(0f, 1f)] public float volumenBatalla = 0.45f;

    [Header("Efectos de sonido")]
    public List<EfectoAudio> efectos = new List<EfectoAudio>();

    public EntradaMusica BuscarMusica(string nombreEscena)
    {
        for (int i = 0; i < musicaPorEscena.Count; i++)
            if (musicaPorEscena[i] != null && musicaPorEscena[i].escena == nombreEscena)
                return musicaPorEscena[i];
        return null;
    }

    public EfectoAudio BuscarEfecto(SonidoId id)
    {
        for (int i = 0; i < efectos.Count; i++)
            if (efectos[i] != null && efectos[i].sonido == id)
                return efectos[i];
        return null;
    }
}