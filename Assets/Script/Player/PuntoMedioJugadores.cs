using UnityEngine;

/// <summary>
/// Se coloca entre los jugadores vivos. La cámara de Cinemachine lo sigue a él en vez de a un
/// solo dragón, así con 2 jugadores los dos caben en pantalla.
/// </summary>
public class PuntoMedioJugadores : MonoBehaviour
{
    void Update()
    {
        Vector3 suma = Vector3.zero;
        int cuenta = 0;

        for (int i = 0; i < Jugadores.Todos.Count; i++)
        {
            PlayerController j = Jugadores.Todos[i];
            if (j == null || j.Muerto) continue;

            suma += j.transform.position;
            cuenta++;
        }

        if (cuenta > 0) transform.position = suma / cuenta;
    }
}
