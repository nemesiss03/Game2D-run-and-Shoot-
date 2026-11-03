using UnityEditor;
using UnityEngine;

public static class CrearBancoAudio
{
    const string Carpeta = "Assets/Resources/Audio";
    const string Ruta = Carpeta + "/BancoAudio.asset";

    [MenuItem("Spirit Dragon/Crear banco de audio")]
    static void Crear()
    {
        BancoAudio existente = AssetDatabase.LoadAssetAtPath<BancoAudio>(Ruta);
        if (existente != null)
        {
            Selection.activeObject = existente;
            EditorGUIUtility.PingObject(existente);
            Debug.Log("El banco de audio ya existe, lo dejé seleccionado.");
            return;
        }

        BancoAudio banco = ScriptableObject.CreateInstance<BancoAudio>();

        Efecto(banco, SonidoId.Disparo, "disparo", 0.55f, 0.94f, 1.08f);
        Efecto(banco, SonidoId.DisparoJefe, "disparo_jefe", 0.75f, 0.92f, 1.06f);
        Efecto(banco, SonidoId.Invocacion, "invocacion", 0.70f, 0.96f, 1.06f);
        Efecto(banco, SonidoId.ImpactoJefe, "impacto_jefe", 0.65f, 0.92f, 1.10f);
        Efecto(banco, SonidoId.ImpactoEsbirro, "impacto_esbirro", 0.55f, 0.90f, 1.14f);
        Efecto(banco, SonidoId.DanioJugador, "danio_jugador", 0.80f, 0.95f, 1.05f);
        Efecto(banco, SonidoId.Dash, "dash", 0.65f, 0.95f, 1.08f);
        Efecto(banco, SonidoId.Curacion, "curar", 0.70f, 0.98f, 1.06f);
        Efecto(banco, SonidoId.ExplosionEsbirro, "explosion_esbirro", 0.75f, 0.90f, 1.12f);
        Efecto(banco, SonidoId.ExplosionJefe, "explosion_jefe", 1.00f, 0.98f, 1.02f);

        // Nuevos, sin clip todavía: arrastra el sonido en el Inspector
        Efecto(banco, SonidoId.Salto, null, 0.60f, 0.97f, 1.05f);
        Efecto(banco, SonidoId.Aterrizaje, null, 0.50f, 0.95f, 1.05f);
        Efecto(banco, SonidoId.Recarga, null, 0.60f, 1.00f, 1.00f);
        Efecto(banco, SonidoId.MuerteJugador, null, 0.80f, 1.00f, 1.00f);

        banco.musicaBatalla = Clip("musica_batalla.mp3");
        banco.volumenBatalla = 0.45f;

        banco.musicaPorEscena.Add(new EntradaMusica { escena = "Boss", musica = Clip("musica_fondo.wav"), volumen = 0.30f });
        banco.musicaPorEscena.Add(new EntradaMusica { escena = "Nivel1", musica = null, volumen = 0.30f });

        AssetDatabase.CreateAsset(banco, Ruta);
        AssetDatabase.SaveAssets();

        Selection.activeObject = banco;
        EditorGUIUtility.PingObject(banco);
        Debug.Log("Banco de audio creado en " + Ruta);
    }

    static AudioClip Clip(string archivo)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(Carpeta + "/" + archivo);
    }

    static void Efecto(BancoAudio banco, SonidoId id, string archivo, float volumen, float tonoMin, float tonoMax)
    {
        AudioClip clip = archivo != null ? Clip(archivo + ".wav") : null;
        banco.efectos.Add(new EfectoAudio
        {
            sonido = id,
            clips = clip != null ? new[] { clip } : new AudioClip[0],
            volumen = volumen,
            tonoMinimo = tonoMin,
            tonoMaximo = tonoMax
        });
    }
}