using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Objeto interactuable de la tienda en el mundo del juego.
/// Cuando el jugador se acerca y presiona la tecla de interaccion, abre la tienda.
/// </summary>
public class TiendaInteractuable : MonoBehaviour
{
    [Header("Referencias")]
    public TiendaUI tiendaUI;

    [Header("Configuracion de Interaccion")]
    public float radioInteraccion = 3f;
    public Key teclaInteraccion = Key.E;
    public LayerMask capaJugador;
    public string tagJugador = "Player";

    [Header("UI de Interaccion")]
    public GameObject indicadorInteraccion;
    public Canvas canvasIndicador;

    [Header("Efectos")]
    public GameObject efectoTiendaActiva;
    public AudioSource audioSource;
    public AudioClip sonidoInteraccion;

    private Transform jugador;
    private bool jugadorEnRango = false;

    void Start()
    {
        // Buscar referencias
        if (tiendaUI == null)
            tiendaUI = FindFirstObjectByType<TiendaUI>();

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
            // Mostrar indicador solo si el jugador est� en rango y la tienda no est� abierta
            bool mostrar = jugadorEnRango && (tiendaUI == null || !tiendaUI.EstaAbierta());
            indicadorInteraccion.SetActive(mostrar);
        }

        // Hacer que el indicador mire hacia la c�mara
        if (canvasIndicador != null && Camera.main != null)
        {
            canvasIndicador.transform.LookAt(Camera.main.transform);
            canvasIndicador.transform.Rotate(0, 180, 0);
        }
    }

    /// <summary>
    /// Abre o cierra la tienda
    /// </summary>
    public void Interactuar()
    {
        if (tiendaUI == null) return;

        // Toggle tienda
        if (tiendaUI.tiendaAbierta)
        {
            tiendaUI.CerrarTienda();
        }
        else
        {
            tiendaUI.AbrirTienda(); // Esto ahora genera nuevas opciones automaticamente
        }

        // Efectos
        if (sonidoInteraccion != null && audioSource != null)
            audioSource.PlayOneShot(sonidoInteraccion);
    }

    void OnDrawGizmosSelected()
    {
        // Visualizar radio de interaccion
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radioInteraccion);
    }
}