using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;


/// <summary>
/// Controlador de UI para la tienda de mejoras.
/// - 3 opciones aleatorias por zona (armas o mejoras de drones)
/// - Sin opci�n de refresh
/// - Se abre al completar un nivel
/// </summary>
public class TiendaUI : MonoBehaviour
{
    public UI_PostProcesser ui_PostProcesser;
    [Header("Referencias")]
    public TiendaMejoras tiendaMejoras;

    [Header("Panel Principal")]
    public GameObject panelTienda;
    public CanvasGroup canvasGroup;

    [Header("Informacion General")]
    public TextMeshProUGUI textoDinero;
    public TextMeshProUGUI textoZona;
    public TextMeshProUGUI textoTitulo;

    [Header("3 Opciones de Compra")]
    public List<OpcionUI> opciones = new List<OpcionUI>();

    [Header("Bot�n Continuar")]
    public Button botonContinuar;
    public TextMeshProUGUI textoBotonContinuar;

    [Header("Panel de Informaci�n")]
    public TextMeshProUGUI textoBuscador;
    public TextMeshProUGUI textoAtacantes;
    public TextMeshProUGUI textoDefensores;
    public TextMeshProUGUI textoArmaActual;

    [Header("Colores")]
    public Color colorPuedeComprar = new Color(0.2f, 0.7f, 0.2f, 1f);
    public Color colorNoPuedeComprar = new Color(0.5f, 0.5f, 0.5f, 1f);
    public Color colorYaComprado = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    public Color colorTextoNormal = Color.white;
    public Color colorTextoDeshabilitado = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Color colorTextoComprado = new Color(0.4f, 0.8f, 0.4f, 1f);

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sonidoAbrir;
    public AudioClip sonidoCerrar;
    public AudioClip sonidoCompra;
    public AudioClip sonidoError;

    [System.Serializable]
    public class OpcionUI
    {
        public GameObject panelOpcion;
        public Button botonComprar;
        public TextMeshProUGUI textoNombre;
        public TextMeshProUGUI textoCosto;
        public TextMeshProUGUI textoDescripcion;
        public TextMeshProUGUI textoNivel;
        public Image imagenIcono;
        public Image imagenFondo;
    }

    public bool tiendaAbierta = false;

    void Start()
    {
        ui_PostProcesser = GetComponent<UI_PostProcesser>();
        if (tiendaMejoras == null)
            tiendaMejoras = FindFirstObjectByType<TiendaMejoras>();

        ConfigurarBotones();

        // Ocultar tienda al inicio
        if (panelTienda != null)
            panelTienda.SetActive(false);
    }

    void Update()
    {
        if (tiendaAbierta)
        {
            ActualizarUI();
        }
    }

    void ConfigurarBotones()
    {
        // Bot�n continuar
        if (botonContinuar != null)
            botonContinuar.onClick.AddListener(ContinuarJuego);

        // Botones de opciones
        for (int i = 0; i < opciones.Count; i++)
        {
            int indice = i;
            if (opciones[i].botonComprar != null)
            {
                opciones[i].botonComprar.onClick.AddListener(() => ComprarOpcion(indice));
            }
        }
    }

    #region Control de Tienda

    /// <summary>
    /// Abre la tienda y genera nuevas opciones
    /// </summary>
    public void AbrirTienda()
    {
        if (tiendaAbierta) return;

        tiendaAbierta = true;


        if (ui_PostProcesser != null)
            ui_PostProcesser.ShopToggle();
            ui_PostProcesser.HudToggle();

        // Pausar el juego
        Time.timeScale = 0f;

        // Generar nuevas opciones cada vez que se abre
        if (tiendaMejoras != null)
            tiendaMejoras.GenerarNuevasOpciones();

        // Sonido
        if (sonidoAbrir != null && audioSource != null)
            audioSource.PlayOneShot(sonidoAbrir);

        // Actualizar UI
        ActualizarUI();
    }

    /// <summary>
    /// Cierra la tienda y continua el juego
    /// </summary>
    public void CerrarTienda()
    {
        if (!tiendaAbierta) return;

        tiendaAbierta = false;


        if (ui_PostProcesser != null)
            ui_PostProcesser.ShopToggle();
            ui_PostProcesser.HudToggle();

        // Reanudar el juego
        Time.timeScale = 1f;

        // Sonido
        if (sonidoCerrar != null && audioSource != null)
            audioSource.PlayOneShot(sonidoCerrar);
    }

    void ContinuarJuego()
    {
        CerrarTienda();
    }

    #endregion

    #region Actualizaci�n de UI

    void ActualizarUI()
    {
        if (tiendaMejoras == null) return;

        // Dinero
        if (textoDinero != null)
            textoDinero.text = $"${tiendaMejoras.ObtenerDinero()}";

        // Zona
        if (textoZona != null)
            textoZona.text = $"ZONA {tiendaMejoras.zonaActual}";

        // Opciones
        ActualizarOpciones();

        // Info de drones y arma
        ActualizarInfoEstado();
    }

    void ActualizarOpciones()
    {
        var opcionesActuales = tiendaMejoras.ObtenerOpcionesActuales();

        for (int i = 0; i < opciones.Count; i++)
        {
            var opcionUI = opciones[i];

            if (i < opcionesActuales.Count)
            {
                var mejora = opcionesActuales[i];
                bool yaComprada = tiendaMejoras.OpcionYaComprada(i);

                // Mostrar panel
                if (opcionUI.panelOpcion != null)
                    opcionUI.panelOpcion.SetActive(true);

                // Textos
                if (opcionUI.textoNombre != null)
                    opcionUI.textoNombre.text = mejora.nombre;

                if (opcionUI.textoCosto != null)
                {
                    if (yaComprada)
                        opcionUI.textoCosto.text = "COMPRADO";
                    else
                        opcionUI.textoCosto.text = $"${mejora.ObtenerCostoActual()}";
                }

                if (opcionUI.textoDescripcion != null)
                    opcionUI.textoDescripcion.text = mejora.descripcion;

                if (opcionUI.textoNivel != null)
                {
                    if (mejora.nivelMaximo > 1)
                        opcionUI.textoNivel.text = $"Nv. {mejora.nivelActual + 1}/{mejora.nivelMaximo}";
                    else
                        opcionUI.textoNivel.text = "";
                }

                // Icono
                if (opcionUI.imagenIcono != null && mejora.icono != null)
                    opcionUI.imagenIcono.sprite = mejora.icono;

                // Estado del bot�n
                bool puedeComprar = !yaComprada && tiendaMejoras.ObtenerDinero() >= mejora.ObtenerCostoActual();

                if (opcionUI.botonComprar != null)
                    opcionUI.botonComprar.interactable = puedeComprar;

                // Colores seg�n estado
                if (opcionUI.imagenFondo != null)
                {
                    if (yaComprada)
                        opcionUI.imagenFondo.color = colorYaComprado;
                    else if (puedeComprar)
                        opcionUI.imagenFondo.color = colorPuedeComprar;
                    else
                        opcionUI.imagenFondo.color = colorNoPuedeComprar;
                }

                if (opcionUI.textoNombre != null)
                {
                    if (yaComprada)
                        opcionUI.textoNombre.color = colorTextoComprado;
                    else if (puedeComprar)
                        opcionUI.textoNombre.color = colorTextoNormal;
                    else
                        opcionUI.textoNombre.color = colorTextoDeshabilitado;
                }

                if (opcionUI.textoCosto != null)
                {
                    if (yaComprada)
                        opcionUI.textoCosto.color = colorTextoComprado;
                    else if (puedeComprar)
                        opcionUI.textoCosto.color = colorTextoNormal;
                    else
                        opcionUI.textoCosto.color = colorTextoDeshabilitado;
                }
            }
            else
            {
                // Ocultar si no hay suficientes opciones
                if (opcionUI.panelOpcion != null)
                    opcionUI.panelOpcion.SetActive(false);
            }
        }
    }

    void ActualizarInfoEstado()
    {
        // Buscador
        if (textoBuscador != null)
        {
            int nivelRecursos = tiendaMejoras.ObtenerNivelMejora(TiendaMejoras.TipoMejora.Drone_Buscador_Recursos);
            textoBuscador.text = nivelRecursos > 0
                ? $"Buscador: +{nivelRecursos * 25}% recursos"
                : "Buscador: Nivel base";
        }

        // Atacantes
        if (textoAtacantes != null)
        {
            int cantidad = 1 + tiendaMejoras.ObtenerNivelMejora(TiendaMejoras.TipoMejora.Drone_Atacante_Cantidad);
            int nivelDaño = tiendaMejoras.ObtenerNivelMejora(TiendaMejoras.TipoMejora.Drone_Atacante_Daño);
            string bonus = nivelDaño > 0 ? $" (+{nivelDaño * 15}% da�o)" : "";
            textoAtacantes.text = $"Atacantes: {cantidad}/3{bonus}";
        }

        // Defensores
        if (textoDefensores != null)
        {
            int cantidad = 1 + tiendaMejoras.ObtenerNivelMejora(TiendaMejoras.TipoMejora.Drone_Defensor_Cantidad);
            int nivelVida = tiendaMejoras.ObtenerNivelMejora(TiendaMejoras.TipoMejora.Drone_Defensor_Vida);
            string bonus = nivelVida > 0 ? $" (+{nivelVida * 20}% vida)" : "";
            textoDefensores.text = $"Defensores: {cantidad}/5{bonus}";
        }

        // Arma actual
        if (textoArmaActual != null && tiendaMejoras.sistemaArmas != null)
        {
            textoArmaActual.text = $"Arma: {tiendaMejoras.sistemaArmas.ObtenerNombreArmaActual()}";
        }
    }

    #endregion

    #region Compras

    void ComprarOpcion(int indice)
    {
        if (tiendaMejoras == null) return;

        var opcionesActuales = tiendaMejoras.ObtenerOpcionesActuales();

        if (indice < 0 || indice >= opcionesActuales.Count)
            return;

        var mejora = opcionesActuales[indice];
        bool puedeComprar = tiendaMejoras.ObtenerDinero() >= mejora.ObtenerCostoActual();

        if (puedeComprar)
        {
            tiendaMejoras.ComprarOpcion(indice);

            if (sonidoCompra != null && audioSource != null)
                audioSource.PlayOneShot(sonidoCompra);
        }
        else
        {
            if (sonidoError != null && audioSource != null)
                audioSource.PlayOneShot(sonidoError);
        }

        ActualizarUI();
    }

    #endregion

    #region M�todos P�blicos

    /// <summary>
    /// Llamar cuando se complete un nivel/zona
    /// </summary>
    public void OnNivelCompletado()
    {
        AbrirTienda();
    }

    /// <summary>
    /// Verifica si la tienda est� abierta
    /// </summary>
    public bool EstaAbierta()
    {
        return tiendaAbierta;
    }

    #endregion
}