using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Genera por código los sonidos del juego (no hay ningún AudioClip importado en el
/// proyecto) y los prefabs de efectos. Todo queda en Assets/Resources para poder
/// cargarse en tiempo de ejecución.
///   Menú: Spirit Dragon/Generar audio y efectos
/// </summary>
public static class GeneradorAudioEfectos
{
    const int SR = 22050;

    const string CarpetaAudio = "Assets/Resources/Audio";
    const string CarpetaPrefabs = "Assets/Resources/Prefabs";

    const string RutaHojaExplosion = "Assets/Imagen/Explosion/DieEnemy/Explotion_RP1 (48x48).png";
    const string RutaFogonazo = "Assets/Imagen/Explosion/BasicShot_MuzzleFlash_RP1 (16x16).png";
    const string RutaBolaFuego = "Assets/Generated_Images/bola_fuego.png";
    const string RutaEsbirro = "Assets/Prefab/Esbirro.prefab";

    [MenuItem("Spirit Dragon/Generar audio y efectos")]
    public static void Generar()
    {
        CrearCarpetas();

        Escribir("disparo", Disparo());
        Escribir("disparo_jefe", DisparoJefe());
        Escribir("invocacion", Invocacion());
        Escribir("impacto_jefe", ImpactoJefe());
        Escribir("impacto_esbirro", ImpactoEsbirro());
        Escribir("danio_jugador", DanioJugador());
        Escribir("dash", Dash());
        Escribir("curar", Curar());
        Escribir("explosion_esbirro", ExplosionEsbirro());
        Escribir("explosion_jefe", ExplosionJefe());
        Escribir("musica_fondo", Musica());

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        AjustarImportacion();

        CrearPrefabExplosion();
        CrearPrefabFogonazo();
        CrearPrefabOrbe();
        ConectarEsbirro();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("GeneradorAudioEfectos: " + 11 + " sonidos y 3 prefabs listos en Assets/Resources.");
    }

    // ------------------------------------------------------------- carpetas
    static void CrearCarpetas()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        if (!AssetDatabase.IsValidFolder(CarpetaAudio))
            AssetDatabase.CreateFolder("Assets/Resources", "Audio");

        if (!AssetDatabase.IsValidFolder(CarpetaPrefabs))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
    }

    // ------------------------------------------------------------- escritura
    static void Escribir(string nombre, float[] muestras)
    {
        string ruta = CarpetaAudio + "/" + nombre + ".wav";
        EscribirWav(ruta, muestras);
    }

    static void EscribirWav(string ruta, float[] muestras)
    {
        if (muestras == null || muestras.Length == 0) return;

        int bytesDatos = muestras.Length * 2;

        using (FileStream fs = new FileStream(ruta, FileMode.Create, FileAccess.Write))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            EscribirTexto(bw, "RIFF");
            bw.Write(36 + bytesDatos);
            EscribirTexto(bw, "WAVE");

            EscribirTexto(bw, "fmt ");
            bw.Write(16);              // tamaño del bloque fmt
            bw.Write((short)1);        // PCM
            bw.Write((short)1);        // mono
            bw.Write(SR);              // frecuencia
            bw.Write(SR * 2);          // bytes por segundo
            bw.Write((short)2);        // alineación de bloque
            bw.Write((short)16);       // bits por muestra

            EscribirTexto(bw, "data");
            bw.Write(bytesDatos);

            for (int i = 0; i < muestras.Length; i++)
            {
                float v = Mathf.Clamp(muestras[i], -1f, 1f);
                bw.Write((short)Mathf.RoundToInt(v * 32000f));
            }
        }
    }

    static void EscribirTexto(BinaryWriter bw, string texto)
    {
        for (int i = 0; i < texto.Length; i++) bw.Write((byte)texto[i]);
    }

    static void AjustarImportacion()
    {
        string[] nombres = {
            "disparo", "disparo_jefe", "invocacion", "impacto_jefe", "impacto_esbirro",
            "danio_jugador", "dash", "curar", "explosion_esbirro", "explosion_jefe"
        };

        for (int i = 0; i < nombres.Length; i++)
        {
            string ruta = CarpetaAudio + "/" + nombres[i] + ".wav";
            AudioImporter imp = AssetImporter.GetAtPath(ruta) as AudioImporter;
            if (imp == null) continue;

            AudioImporterSampleSettings ajustes = imp.defaultSampleSettings;
            ajustes.loadType = AudioClipLoadType.DecompressOnLoad;
            ajustes.compressionFormat = AudioCompressionFormat.PCM;
            ajustes.preloadAudioData = true;
            imp.defaultSampleSettings = ajustes;
            imp.forceToMono = true;
            imp.SaveAndReimport();
        }
    }

    // ------------------------------------------------------------- prefabs
    static void CrearPrefabExplosion()
    {
        Sprite[] fotogramas = CargarFotogramas(RutaHojaExplosion, 31);
        if (fotogramas == null || fotogramas.Length == 0)
        {
            Debug.LogWarning("GeneradorAudioEfectos: no se encuentra la hoja de explosión.");
            return;
        }

        GameObject go = new GameObject("Explosion");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = fotogramas[0];
        sr.sortingOrder = 30;

        ExplosionSimple e = go.AddComponent<ExplosionSimple>();
        e.fotogramas = fotogramas;
        e.fps = 34f;
        e.escala = 1f;
        e.ordenDibujo = 30;

        string ruta = CarpetaPrefabs + "/Explosion.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, ruta);
        UnityEngine.Object.DestroyImmediate(go);
    }

    static void CrearPrefabFogonazo()
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(RutaFogonazo);
        if (s == null)
        {
            Debug.LogWarning("GeneradorAudioEfectos: no se encuentra el fogonazo.");
            return;
        }

        GameObject go = new GameObject("Fogonazo");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = 25;

        FogonazoSimple f = go.AddComponent<FogonazoSimple>();
        f.sprite = s;
        f.duracion = 0.07f;
        f.escala = 1f;
        f.ordenDibujo = 25;

        string ruta = CarpetaPrefabs + "/Fogonazo.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, ruta);
        UnityEngine.Object.DestroyImmediate(go);
    }

    static void CrearPrefabOrbe()
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(RutaBolaFuego);
        if (s == null)
        {
            Debug.LogWarning("GeneradorAudioEfectos: no se encuentra la bola de fuego.");
            return;
        }

        GameObject go = new GameObject("OrbeCuracion");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.color = new Color(0.55f, 1f, 0.72f, 1f);
        sr.sortingOrder = 12;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        Bala bala = go.AddComponent<Bala>();
        bala.equipo = Equipo.Enemigo;
        bala.cura = true;
        bala.curacion = 18f;
        bala.velocidad = 7f;
        bala.vida = 3.5f;
        bala.danio = 0f;
        bala.escalaImpacto = 0.6f;

        string ruta = CarpetaPrefabs + "/OrbeCuracion.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, ruta);
        UnityEngine.Object.DestroyImmediate(go);
    }

    static void ConectarEsbirro()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaEsbirro);
        if (prefab == null) return;

        GameObject orbe = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPrefabs + "/OrbeCuracion.prefab");
        if (orbe == null) return;

        Esbirro esbirro = prefab.GetComponent<Esbirro>();
        if (esbirro == null) return;

        esbirro.orbeCuracionPrefab = orbe;
        esbirro.probabilidadOrbe = 0.35f;
        esbirro.intervaloOrbe = 3.2f;
        esbirro.primeraEsperaOrbe = 1.4f;
        esbirro.velocidadOrbe = 7f;
        esbirro.vidaOrbe = 3.5f;
        esbirro.curacionOrbe = 18f;

        EditorUtility.SetDirty(esbirro);
        PrefabUtility.SavePrefabAsset(prefab);
    }

    static Sprite[] CargarFotogramas(string rutaTextura, int total)
    {
        UnityEngine.Object[] todo = AssetDatabase.LoadAllAssetsAtPath(rutaTextura);
        Sprite[] resultado = new Sprite[total];

        for (int i = 0; i < todo.Length; i++)
        {
            Sprite s = todo[i] as Sprite;
            if (s == null) continue;

            int indice = IndiceFinal(s.name);
            if (indice < 0 || indice >= total) continue;

            resultado[indice] = s;
        }

        List<Sprite> lista = new List<Sprite>();
        for (int i = 0; i < total; i++)
        {
            if (resultado[i] != null) lista.Add(resultado[i]);
        }

        return lista.ToArray();
    }

    /// <summary>Devuelve el número que va detrás del último guion bajo del nombre.</summary>
    static int IndiceFinal(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return -1;

        int guion = nombre.LastIndexOf('_');
        if (guion < 0 || guion + 1 >= nombre.Length) return -1;

        int valor;
        if (int.TryParse(nombre.Substring(guion + 1), out valor)) return valor;

        return -1;
    }

    // ============================================================ síntesis
    static float[] Nuevo(float segundos)
    {
        return new float[Mathf.Max(1, (int)(segundos * SR))];
    }

    static float[] Disparo()
    {
        float[] b = Nuevo(0.17f);
        System.Random rnd = new System.Random(11);
        float fase = 0f;

        for (int i = 0; i < b.Length; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-t * 26f);
            float f = Mathf.Lerp(980f, 150f, Mathf.Clamp01(t / 0.12f));

            fase += 2f * Mathf.PI * f / SR;
            float tono = Mathf.Sin(fase) >= 0f ? 1f : -1f;
            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0);

            b[i] = (tono * 0.40f + ruido * 0.60f * Mathf.Exp(-t * 55f)) * env;
        }

        b = PasoBajo(b, 7000f);
        return Normalizar(SuavizarExtremos(b), 0.85f);
    }

    static float[] DisparoJefe()
    {
        float[] b = Nuevo(0.36f);
        System.Random rnd = new System.Random(23);
        float fase = 0f;

        for (int i = 0; i < b.Length; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-t * 13f);
            float f = Mathf.Lerp(380f, 62f, Mathf.Clamp01(t / 0.26f));

            fase += 2f * Mathf.PI * f / SR;
            float tono = Mathf.Sin(fase) >= 0f ? 1f : -1f;
            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0);

            float vibrato = 1f + 0.12f * Mathf.Sin(t * 2f * Mathf.PI * 31f);

            b[i] = (tono * 0.45f * vibrato + ruido * 0.30f * Mathf.Exp(-t * 18f)) * env;
        }

        b = PasoBajo(b, 2600f);
        return Normalizar(SuavizarExtremos(b), 0.90f);
    }

    static float[] Invocacion()
    {
        float[] b = Nuevo(0.85f);
        float fase = 0f;
        float fase2 = 0f;

        for (int i = 0; i < b.Length; i++)
        {
            float t = (float)i / SR;
            float k = Mathf.Clamp01(t / 0.62f);

            float f = Mathf.Lerp(150f, 920f, k) * (1f + 0.03f * Mathf.Sin(t * 2f * Mathf.PI * 14f));

            fase += 2f * Mathf.PI * f / SR;
            fase2 += 2f * Mathf.PI * f * 2.01f / SR;

            float env = Mathf.Clamp01(t / 0.05f) * Mathf.Exp(-t * 3.4f);

            b[i] = (Mathf.Sin(fase) * 0.55f + Mathf.Sin(fase2) * 0.22f) * env;
        }

        return Normalizar(SuavizarExtremos(b), 0.75f);
    }

    static float[] ImpactoJefe()
    {
        float[] b = Nuevo(0.22f);
        System.Random rnd = new System.Random(41);

        for (int i = 0; i < b.Length; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-t * 20f);

            float metal =
                Mathf.Sin(2f * Mathf.PI * 720f * t) * 0.42f +
                Mathf.Sin(2f * Mathf.PI * 1090f * t) * 0.30f +
                Mathf.Sin(2f * Mathf.PI * 1530f * t) * 0.18f;

            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.35f * Mathf.Exp(-t * 90f);

            b[i] = (metal + ruido) * env;
        }

        b = PasoBajo(b, 8500f);
        return Normalizar(SuavizarExtremos(b), 0.80f);
    }

    static float[] ImpactoEsbirro()
    {
        float[] b = Nuevo(0.14f);
        System.Random rnd = new System.Random(47);

        for (int i = 0; i < b.Length; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-t * 34f);

            float tono =
                Mathf.Sin(2f * Mathf.PI * 1350f * t) * 0.45f +
                Mathf.Sin(2f * Mathf.PI * 2100f * t) * 0.20f;

            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.30f * Mathf.Exp(-t * 120f);

            b[i] = (tono + ruido) * env;
        }

        return Normalizar(SuavizarExtremos(b), 0.70f);
    }

    static float[] DanioJugador()
    {
        float[] b = Nuevo(0.38f);
        System.Random rnd = new System.Random(59);
        float fase = 0f;

        for (int i = 0; i < b.Length; i++)
        {
            float t = (float)i / SR;
            float env = Mathf.Exp(-t * 8f);

            float f = Mathf.Lerp(420f, 105f, Mathf.Clamp01(t / 0.30f));
            fase += 2f * Mathf.PI * f / SR;

            // Onda de sierra suave
            float sierra = (fase / (2f * Mathf.PI)) % 1f;
            sierra = sierra * 2f - 1f;

            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.18f;

            float v = (sierra * 0.45f + ruido) * env;
            b[i] = (float)Math.Tanh(v * 1.8f) * 0.8f;
        }

        b = PasoBajo(b, 3800f);
        return Normalizar(SuavizarExtremos(b), 0.85f);
    }

    static float[] Dash()
    {
        float[] b = Nuevo(0.30f);
        System.Random rnd = new System.Random(71);

        for (int i = 0; i < b.Length; i++)
        {
            float t = (float)i / SR;
            float k = t / 0.30f;

            // Envolvente de ráfaga de aire: sube rápido y cae
            float env = Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI);
            env = env * env;

            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0);
            b[i] = ruido * env;
        }

        // Ruido "soplado": se le quita el grave
        float[] grave = PasoBajo(b, 900f);
        for (int i = 0; i < b.Length; i++) b[i] = b[i] - grave[i] * 0.85f;

        b = PasoBajo(b, 9000f);
        return Normalizar(SuavizarExtremos(b), 0.75f);
    }

    static float[] Curar()
    {
        float[] b = Nuevo(0.62f);

        float[] notas = { 523.25f, 659.25f, 783.99f, 1046.50f };

        for (int n = 0; n < notas.Length; n++)
        {
            float inicio = n * 0.10f;
            float dur = 0.34f;

            int desde = (int)(inicio * SR);
            int cuantas = (int)(dur * SR);

            for (int i = 0; i < cuantas && desde + i < b.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Clamp01(t / 0.02f) * Mathf.Exp(-t * 7f);

                float v = Mathf.Sin(2f * Mathf.PI * notas[n] * t) * 0.5f +
                          Mathf.Sin(2f * Mathf.PI * notas[n] * 2f * t) * 0.15f;

                b[desde + i] += v * env * 0.35f;
            }
        }

        return Normalizar(SuavizarExtremos(b), 0.75f);
    }

    static float[] ExplosionEsbirro()
    {
        float[] b = Nuevo(0.55f);
        System.Random rnd = new System.Random(83);
        float fase = 0f;

        for (int i = 0; i < b.Length; i++)
        {
            float t = (float)i / SR;

            float envRuido = Mathf.Exp(-t * 8f);
            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0) * envRuido;

            float f = Mathf.Lerp(140f, 42f, Mathf.Clamp01(t / 0.42f));
            fase += 2f * Mathf.PI * f / SR;
            float sub = Mathf.Sin(fase) * Mathf.Exp(-t * 6f) * 0.7f;

            b[i] = ruido * 0.8f + sub;
        }

        b = PasoBajo(b, 4500f);
        return Normalizar(SuavizarExtremos(b), 0.90f);
    }

    static float[] ExplosionJefe()
    {
        float[] b = Nuevo(1.6f);
        System.Random rnd = new System.Random(97);
        float fase = 0f;

        for (int i = 0; i < b.Length; i++)
        {
            float t = (float)i / SR;

            float envRuido = Mathf.Exp(-t * 2.6f);
            float ruido = (float)(rnd.NextDouble() * 2.0 - 1.0) * envRuido * 0.85f;

            float f = Mathf.Lerp(78f, 26f, Mathf.Clamp01(t / 1.2f));
            fase += 2f * Mathf.PI * f / SR;
            float sub = Mathf.Sin(fase) * Mathf.Exp(-t * 1.8f) * 0.9f;

            // Chispazos
            float chispa = 0f;
            if (rnd.NextDouble() < 0.0025) chispa = (float)(rnd.NextDouble() * 2.0 - 1.0) * 0.5f * Mathf.Exp(-t * 0.8f);

            b[i] = ruido + sub + chispa;
        }

        b = PasoBajo(b, 5200f);
        return Normalizar(SuavizarExtremos(b), 0.95f);
    }

    // ------------------------------------------------------------- música
    static float[] Musica()
    {
        const float beat = 0.6f;      // 100 BPM
        const int tiempos = 32;       // 8 compases de 4/4

        float[] b = new float[(int)(beat * tiempos * SR)];

        // Am - F - C - G (2 compases cada uno sería largo: 1 compás cada uno, x2 vueltas)
        float[][] acordes = new float[][]
        {
            new float[] { 110.00f, 220.00f, 261.63f, 329.63f }, // Am
            new float[] {  87.31f, 174.61f, 220.00f, 261.63f }, // F
            new float[] { 130.81f, 261.63f, 329.63f, 392.00f }, // C
            new float[] {  98.00f, 196.00f, 246.94f, 293.66f }  // G
        };

        int[] arpegio = { 1, 2, 3, 2, 1, 3, 2, 1 };

        for (int c = 0; c < 4; c++)
        {
            float inicio = (c * 8) * beat;
            float[] ac = acordes[c];

            // Bajo: una nota por tiempo
            for (int t = 0; t < 8; t++)
                SumarNota(b, inicio + t * beat, beat * 0.90f, ac[0], 0.26f, 0.006f, 2, 400f);

            // Arpegio a corcheas
            for (int e = 0; e < 16; e++)
            {
                float f = ac[arpegio[e % arpegio.Length]];
                if (e % 8 >= 4) f *= 2f;
                SumarNota(b, inicio + e * beat * 0.5f, beat * 0.44f, f, 0.085f, 0.003f, 1, 5000f);
            }
        }

        // Segunda mitad: melodía suave una octava arriba (4 compases de 4 tiempos)
        for (int c = 0; c < 4; c++)
        {
            float inicio = (16 + c * 4) * beat;
            float[] ac = acordes[c];

            for (int e = 0; e < 4; e++)
            {
                float f = ac[arpegio[e % arpegio.Length]] * 2f;
                SumarNota(b, inicio + e * beat, beat * 0.85f, f, 0.075f, 0.02f, 0, 6000f);
            }
        }

        // Percusión
        for (int t = 0; t < tiempos; t++)
        {
            float inicio = t * beat;

            if (t % 4 == 0 || t % 4 == 2) SumarGolpe(b, inicio, 0.24f, 130f, 45f, 0.55f);
            SumarRuido(b, inicio, 0.05f, 0.10f, 60f);
            SumarRuido(b, inicio + beat * 0.5f, 0.035f, 0.055f, 80f);
        }

        PasoBajoEnSitio(b, 9000f);

        return Normalizar(b, 0.80f);
    }

    // ------------------------------------------------------------- utilidades
    /// <summary>Añade una nota con envolvente a un buffer.</summary>
    static void SumarNota(float[] destino, float inicio, float duracion, float frecuencia,
                          float amplitud, float ataque, int forma, float corte)
    {
        int desde = (int)(inicio * SR);
        int cuantas = (int)(duracion * SR);

        if (desde >= destino.Length) return;

        float fase = 0f;
        float paso = 2f * Mathf.PI * frecuencia / SR;

        float rc = 1f / (2f * Mathf.PI * Mathf.Max(50f, corte));
        float dt = 1f / SR;
        float alfa = dt / (rc + dt);
        float filtro = 0f;

        for (int i = 0; i < cuantas; i++)
        {
            int indice = desde + i;
            if (indice >= destino.Length) break;

            float t = (float)i / SR;
            float k = (float)i / cuantas;

            float env = Mathf.Clamp01(t / Mathf.Max(0.001f, ataque));
            env *= 1f - k;

            float v;
            if (forma == 2)
            {
                v = Mathf.Sin(fase) >= 0f ? 1f : -1f;
            }
            else if (forma == 1)
            {
                float s = (fase / (2f * Mathf.PI)) % 1f;
                v = 4f * Mathf.Abs(s - 0.5f) - 1f;
                v = -v;
            }
            else
            {
                v = Mathf.Sin(fase);
            }

            filtro += alfa * (v - filtro);
            destino[indice] += filtro * env * amplitud;

            fase += paso;
            if (fase > 2f * Mathf.PI * 1000f) fase -= 2f * Mathf.PI * 1000f;
        }
    }

    static void SumarGolpe(float[] destino, float inicio, float duracion, float f0, float f1, float amplitud)
    {
        int desde = (int)(inicio * SR);
        int cuantas = (int)(duracion * SR);

        float fase = 0f;

        for (int i = 0; i < cuantas; i++)
        {
            int indice = desde + i;
            if (indice >= destino.Length) break;

            float k = (float)i / cuantas;
            float f = Mathf.Lerp(f0, f1, k);
            fase += 2f * Mathf.PI * f / SR;

            float env = Mathf.Exp(-k * 5f);

            destino[indice] += Mathf.Sin(fase) * env * amplitud;
        }
    }

    static void SumarRuido(float[] destino, float inicio, float duracion, float amplitud, float corte)
    {
        int desde = (int)(inicio * SR);
        int cuantas = (int)(duracion * SR);

        System.Random rnd = new System.Random(inicio.GetHashCode());

        float rc = 1f / (2f * Mathf.PI * Mathf.Max(100f, corte));
        float dt = 1f / SR;
        float alfa = dt / (rc + dt);
        float filtro = 0f;

        for (int i = 0; i < cuantas; i++)
        {
            int indice = desde + i;
            if (indice >= destino.Length) break;

            float k = (float)i / cuantas;
            float env = Mathf.Exp(-k * 6f);

            float n = (float)(rnd.NextDouble() * 2.0 - 1.0);
            filtro += alfa * (n - filtro);

            destino[indice] += filtro * env * amplitud;
        }
    }

    static float[] PasoBajo(float[] origen, float corte)
    {
        float rc = 1f / (2f * Mathf.PI * Mathf.Max(50f, corte));
        float dt = 1f / SR;
        float alfa = dt / (rc + dt);

        float[] r = new float[origen.Length];
        float y = 0f;

        for (int i = 0; i < origen.Length; i++)
        {
            y += alfa * (origen[i] - y);
            r[i] = y;
        }

        return r;
    }

    static void PasoBajoEnSitio(float[] b, float corte)
    {
        float rc = 1f / (2f * Mathf.PI * Mathf.Max(50f, corte));
        float dt = 1f / SR;
        float alfa = dt / (rc + dt);

        float y = 0f;
        for (int i = 0; i < b.Length; i++)
        {
            y += alfa * (b[i] - y);
            b[i] = y;
        }
    }

    static float[] SuavizarExtremos(float[] b)
    {
        int rampa = Mathf.Min(160, b.Length / 6);

        for (int i = 0; i < rampa; i++)
        {
            float k = (float)i / rampa;
            b[i] *= k;
            b[b.Length - 1 - i] *= k;
        }

        return b;
    }

    static float[] Normalizar(float[] b, float pico)
    {
        float maximo = 0f;

        for (int i = 0; i < b.Length; i++)
        {
            float a = Mathf.Abs(b[i]);
            if (a > maximo) maximo = a;
        }

        if (maximo <= 0.0001f) return b;

        float escala = pico / maximo;
        for (int i = 0; i < b.Length; i++) b[i] *= escala;

        return b;
    }
}
