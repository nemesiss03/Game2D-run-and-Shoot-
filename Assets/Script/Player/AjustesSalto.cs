using UnityEngine;

[System.Serializable]
public class AjustesSalto
{
    [Tooltip("Altura máxima del salto en unidades (el dragón mide cerca de 1).")]
    public float altura = 2.4f;
    [Tooltip("Segundos hasta llegar al punto más alto. Menos tiempo = salto más seco.")]
    public float tiempoSubida = 0.36f;
    [Tooltip("1 = cae igual que sube. 1.8 = cae más pesado.")]
    public float multiplicadorCaida = 1.8f;
    [Tooltip("Si sueltas el botón mientras sube, la gravedad se multiplica por esto (salto corto).")]
    public float multiplicadorCorte = 2.8f;
    [Tooltip("Tope de velocidad al caer (unidades por segundo).")]
    public float velocidadMaximaCaida = 20f;
    [Tooltip("Segundos de margen para saltar justo después de salir de un borde.")]
    public float tiempoCoyote = 0.08f;
    [Tooltip("Segundos que se recuerda un salto pulsado justo antes de aterrizar.")]
    public float tiempoBuffer = 0.10f;
    [Tooltip("Por debajo de esta velocidad vertical se considera la cima del salto.")]
    public float umbralCima = 1.5f;
    [Tooltip("Gravedad en la cima. 1 = igual, 0.6 = flota un poco para dar control.")]
    public float multiplicadorCima = 0.6f;

    public float Gravedad
    {
        get { float t = Mathf.Max(0.05f, tiempoSubida); return 2f * altura / (t * t); }
    }

    public float VelocidadInicial
    {
        get { return 2f * altura / Mathf.Max(0.05f, tiempoSubida); }
    }
}