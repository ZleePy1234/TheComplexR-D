using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Objeto interactuable de la tienda en el mundo del juego.
/// Cuando el jugador se acerca y presiona la tecla de interacción, abre la tienda.
/// </summary>
public class TiendaInteractuable : MonoBehaviour
{
    [Header("Referencias")]
    public TiendaUI tiendaUI;
    public TiendaMejoras tiendaMejoras;

    [Header("Configuración de Interacción")]
    public float radioInteraccion = 3f;
    public Key teclaInteraccion = Key.E;
    public LayerMask capaJugador;
    public string tagJugador = "Player";

    [Header("UI de Interacción")]
    public GameObject indicadorInteraccion; // Texto o icono que muestra "Presiona E para abrir"
    public Canvas canvasIndicador;

    [Header("Efectos")]
    public GameObject efectoTiendaActiva;
    public AudioSource audioSource;
    public AudioClip sonidoInteraccion;

    private Transform jugador;
    private bool jugadorEnRango = false;
    private bool tiendaYaUsada = false; // Para saber si ya se generaron las opciones de esta zona

    void Start()
    {
        // Buscar referencias
        if (tiendaUI == null)
            tiendaUI = FindFirstObjectByType<TiendaUI>();

        if (tiendaMejoras == null)
            tiendaMejoras = FindFirstObjectByType<TiendaMejoras>();

        // Buscar jugador
        GameObject jugadorObj = GameObject.FindGameObjectWithTag(tagJugador);
        if (jugadorObj != null)
            jugador = jugadorObj.transform;

        // Ocultar indicador al inicio
        if (indicadorInteraccion != null)
            indicadorInteraccion.SetActive(false);
    }

    void Update()
    {
        VerificarDistanciaJugador();
        VerificarInput();
        ActualizarIndicador();
    }

    void VerificarDistanciaJugador()
    {
        if (jugador == null)
        {
            // Intentar buscar de nuevo
            GameObject jugadorObj = GameObject.FindGameObjectWithTag(tagJugador);
            if (jugadorObj != null)
                jugador = jugadorObj.transform;
            else
                return;
        }

        float distancia = Vector3.Distance(transform.position, jugador.position);
        jugadorEnRango = distancia <= radioInteraccion;
    }

    void VerificarInput()
    {
        if (!jugadorEnRango) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[teclaInteraccion].wasPressedThisFrame)
        {
            Interactuar();
        }
    }

    void ActualizarIndicador()
    {
        if (indicadorInteraccion != null)
        {
            // Mostrar indicador solo si el jugador está en rango y la tienda no está abierta
            bool mostrar = jugadorEnRango && (tiendaUI == null || !tiendaUI.EstaAbierta());
            indicadorInteraccion.SetActive(mostrar);
        }

        // Hacer que el indicador mire hacia la cámara
        if (canvasIndicador != null && Camera.main != null)
        {
            canvasIndicador.transform.LookAt(Camera.main.transform);
            canvasIndicador.transform.Rotate(0, 180, 0);
        }
    }

    /// <summary>
    /// Abre la tienda
    /// </summary>
    public void Interactuar()
    {
        if (tiendaUI == null || tiendaMejoras == null) return;

        // Si es la primera vez que se abre en esta zona, generar opciones
        if (!tiendaYaUsada)
        {
            tiendaMejoras.GenerarOpcionesParaZona();
            tiendaYaUsada = true;
        }

        // Abrir la tienda
        tiendaUI.AbrirTienda();

        // Efectos
        if (sonidoInteraccion != null && audioSource != null)
            audioSource.PlayOneShot(sonidoInteraccion);

        Debug.Log("Tienda abierta");
    }

    /// <summary>
    /// Resetear la tienda para una nueva zona (llamar cuando el jugador avance de zona)
    /// </summary>
    public void ResetearParaNuevaZona()
    {
        tiendaYaUsada = false;
        Debug.Log("Tienda reseteada para nueva zona");
    }

    /// <summary>
    /// Forzar la generación de nuevas opciones (para cuando se avanza de zona)
    /// </summary>
    public void GenerarNuevasOpciones()
    {
        if (tiendaMejoras != null)
        {
            tiendaMejoras.GenerarOpcionesParaZona();
            tiendaYaUsada = true;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualizar radio de interacción
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radioInteraccion);
    }
}