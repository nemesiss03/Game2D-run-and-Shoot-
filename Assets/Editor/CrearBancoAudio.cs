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

    /// <summary>
    /// Asigna la música generada con Tools/GenerarMusica.py: Nivel1, Nivel2 y Nivel3 por escena y la del jefe
    /// como música de batalla (suena al acabar la cinemática del jefe). Los volúmenes igualan la sonoridad.
    /// </summary>
    [MenuItem("Spirit Dragon/Asignar música de los niveles")]
    static void AsignarMusicaNiveles()
    {
        BancoAudio banco = AssetDatabase.LoadAssetAtPath<BancoAudio>(Ruta);
        if (banco == null) { Debug.LogError("No existe el banco de audio: usa antes 'Crear banco de audio'."); return; }

        string[] archivos = { "musica_nivel1.wav", "musica_nivel2.wav", "musica_nivel3.wav", "musica_boss.wav" };
        foreach (string a in archivos)
        {
            string ruta = Carpeta + "/Musica/" + a;
            AudioImporter imp = AssetImporter.GetAtPath(ruta) as AudioImporter;
            if (imp == null) { Debug.LogError("Falta " + ruta); return; }

            AudioImporterSampleSettings s = imp.defaultSampleSettings;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.7f;
            imp.defaultSampleSettings = s;
            imp.forceToMono = true;
            imp.SaveAndReimport();
        }

        Escena(banco, "Nivel1", "musica_nivel1.wav", 0.30f);
        Escena(banco, "Nivel2", "musica_nivel2.wav", 0.28f);
        Escena(banco, "Nivel3", "musica_nivel3.wav", 0.17f);

        banco.musicaBatalla = AssetDatabase.LoadAssetAtPath<AudioClip>(Carpeta + "/Musica/musica_boss.wav");
        banco.volumenBatalla = 0.40f;

        EditorUtility.SetDirty(banco);
        AssetDatabase.SaveAssets();
        Debug.Log("Música asignada: Nivel1, Nivel2, Nivel3 y batalla del jefe.");
    }

    static void Escena(BancoAudio banco, string escena, string archivo, float volumen)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Carpeta + "/Musica/" + archivo);
        EntradaMusica e = banco.BuscarMusica(escena);
        if (e == null)
        {
            e = new EntradaMusica { escena = escena };
            banco.musicaPorEscena.Add(e);
        }
        e.musica = clip;
        e.volumen = volumen;
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