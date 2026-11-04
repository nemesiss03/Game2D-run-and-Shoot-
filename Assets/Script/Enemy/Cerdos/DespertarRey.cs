using UnityEngine;

/// <summary>Zona de entrada a la arena: cuando un jugador pasa, el Rey Cerdo despierta y la arena se cierra.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DespertarRey : MonoBehaviour
{
    public ReyCerdo rey;

    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    void OnTriggerStay2D(Collider2D otro)
    {
        if (rey == null) { Destroy(this); return; }

        PlayerController j = otro.GetComponentInParent<PlayerController>();
        if (j == null || j.Muerto) return;

        rey.Despertar();
        Destroy(gameObject);
    }
}
