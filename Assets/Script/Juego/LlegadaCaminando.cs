using System.Collections;
using UnityEngine;

/// <summary>
/// Principio de Nivel2 cuando se llega caminando desde el bosque: sale de negro y el dragón
/// entra andando por la izquierda antes de devolver el control al jugador.
/// Lo crea TransicionCaminando; no hace falta ponerlo en la escena.
/// </summary>
public class LlegadaCaminando : MonoBehaviour
{
    IEnumerator Start()
    {
        EfectosCine cine = EfectosCine.Instancia;
        cine.FundidoInmediato(1f);
        cine.BarrasInmediato(true);

        yield return null;

        // Empieza un poco más atrás y entra andando hasta su punto de salida
        for (int i = 0; i < Jugadores.Todos.Count; i++)
        {
            PlayerController p = Jugadores.Todos[i];
            if (p == null) continue;

            Salud s = p.GetComponent<Salud>();
            if (s != null) s.invulnerable = true;

            UltimoSuelo suelo = p.GetComponent<UltimoSuelo>();
            Vector3 pos = p.transform.position;
            Vector2 inicio = new Vector2(pos.x - 4.5f - i * 1.2f, pos.y);
            if (suelo != null) suelo.Teletransportar(inicio);

            p.AsignarEntrada(new EntradaAutomatica { horizontal = 1f });
            p.controlActivo = true;
            p.MirarHacia(false);
        }

        yield return cine.Fundido(0f, 0.9f, Color.black);
        yield return new WaitForSeconds(1.4f);

        for (int i = 0; i < Jugadores.Todos.Count; i++)
        {
            PlayerController p = Jugadores.Todos[i];
            if (p == null) continue;

            // Se vuelve a una entrada normal: la que corresponde al dispositivo del jugador
            p.Configurar(p.dispositivo, p.indiceMando, p.numeroJugador);

            Salud s = p.GetComponent<Salud>();
            if (s != null) s.invulnerable = false;
        }

        yield return cine.Barras(false, 0.6f);
        Destroy(gameObject);
    }
}
