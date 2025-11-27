using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema de tienda con 3 opciones aleatorias por nivel/zona.
/// - Armas: Al comprar se equipan, solo muestra armas diferentes a la equipada
/// - Buscadores: Solo 1, mejoras aumentan recursos
/// - Atacantes: Primero cantidad (max 3), luego da�o
/// - Defensores: Primero cantidad (max 5), luego vida
/// </summary>
public class TiendaMejoras : MonoBehaviour
{
    #region Enums y Clases

    public enum TipoMejora
    {
        // Armas
        Arma_Pistola,
        Arma_HandCannon,
        Arma_MachineGun,
        Arma_SMG,
        Arma_Shotgun,

        // Drones - Buscador (solo recursos, no cantidad)
        Drone_Buscador_Recursos,

        // Drones - Atacante
        Drone_Atacante_Cantidad,
        Drone_Atacante_Daño,

        // Drones - Defensor
        Drone_Defensor_Cantidad,
        Drone_Defensor_Vida
    }

    [System.Serializable]
    public class ConfiguracionMejora
    {
        public TipoMejora tipo;
        public string nombre;
        public string descripcion;
        public Sprite icono;
        public int costoBase;
        public int incrementoCostoPorNivel;
        public int nivelMaximo;
        [HideInInspector] public int nivelActual;

        public int ObtenerCostoActual()
        {
            return costoBase + (incrementoCostoPorNivel * nivelActual);
        }

        public bool EstaAlMaximo()
        {
            return nivelActual >= nivelMaximo;
        }
    }

    #endregion

    #region Referencias

    [Header("Referencias Principales")]
    public Drones sistemaDrones;
    public WeaponUpgrades sistemaArmas;
    public PlayerStats playerStats;

    [Header("Sistema de Zona/Nivel")]
    public int zonaActual = 1;

    [Header("Configuraci�n de Mejoras")]
    public List<ConfiguracionMejora> todasLasMejoras = new List<ConfiguracionMejora>();

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sonidoCompra;
    public AudioClip sonidoError;

    #endregion

    #region Variables Privadas

    private List<ConfiguracionMejora> opcionesActuales = new List<ConfiguracionMejora>();
    private System.Random randomGenerador;

    // Constantes
    private const int MAX_DRONES_ATACANTE = 3;
    private const int MAX_DRONES_DEFENSOR = 5;
    private const int MAX_MEJORAS_RECURSOS_BUSCADOR = 5;
    private const int MAX_MEJORAS_DAÑO_ATACANTE = 5;
    private const int MAX_MEJORAS_VIDA_DEFENSOR = 5;

    // Valores base para el buscador
    private int monedasMinimasBase = 50;
    private int monedasMaximasBase = 150;

    #endregion

    #region Inicializaci�n

    void Start()
    {
        randomGenerador = new System.Random();

        // Buscar referencias si no est�n asignadas
        if (sistemaDrones == null)
            sistemaDrones = FindFirstObjectByType<Drones>();

        if (sistemaArmas == null)
            sistemaArmas = FindFirstObjectByType<WeaponUpgrades>();

        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

        // Inicializar mejoras por defecto si la lista est� vac�a
        if (todasLasMejoras.Count == 0)
            InicializarMejorasPorDefecto();

        // Guardar valores base del buscador
        if (sistemaDrones != null)
        {
            monedasMinimasBase = sistemaDrones.monedasMinimas;
            monedasMaximasBase = sistemaDrones.monedasMaximas;
        }

        // Configurar drones iniciales
        ConfigurarDronesIniciales();

        // Generar opciones para la primera zona
        GenerarOpcionesParaZona();
    }

    void InicializarMejorasPorDefecto()
    {
        // ===== ARMAS =====
        todasLasMejoras.Add(new ConfiguracionMejora
        {
            tipo = TipoMejora.Arma_Pistola,
            nombre = "Pistola",
            descripcion = "Arma b�sica, precisa y confiable",
            costoBase = 100,
            incrementoCostoPorNivel = 0,
            nivelMaximo = 1,
            nivelActual = 1 // Ya la tienes equipada al inicio
        });

        todasLasMejoras.Add(new ConfiguracionMejora
        {
            tipo = TipoMejora.Arma_HandCannon,
            nombre = "Hand Cannon",
            descripcion = "Alto da�o, baja cadencia",
            costoBase = 200,
            incrementoCostoPorNivel = 0,
            nivelMaximo = 1
        });

        todasLasMejoras.Add(new ConfiguracionMejora
        {
            tipo = TipoMejora.Arma_MachineGun,
            nombre = "Machine Pistol",
            descripcion = "Alta cadencia, da�o moderado",
            costoBase = 250,
            incrementoCostoPorNivel = 0,
            nivelMaximo = 1
        });

        todasLasMejoras.Add(new ConfiguracionMejora
        {
            tipo = TipoMejora.Arma_SMG,
            nombre = "SMG",
            descripcion = "Subfusil equilibrado",
            costoBase = 300,
            incrementoCostoPorNivel = 0,
            nivelMaximo = 1
        });

        todasLasMejoras.Add(new ConfiguracionMejora
        {
            tipo = TipoMejora.Arma_Shotgun,
            nombre = "Escopeta",
            descripcion = "Devastadora a corta distancia",
            costoBase = 350,
            incrementoCostoPorNivel = 0,
            nivelMaximo = 1
        });

        // ===== BUSCADOR (Solo mejora de recursos) =====
        todasLasMejoras.Add(new ConfiguracionMejora
        {
            tipo = TipoMejora.Drone_Buscador_Recursos,
            nombre = "Buscador: +Recursos",
            descripcion = "Aumenta monedas recolectadas (+25%)",
            costoBase = 150,
            incrementoCostoPorNivel = 75,
            nivelMaximo = MAX_MEJORAS_RECURSOS_BUSCADOR
        });

        // ===== ATACANTES =====
        todasLasMejoras.Add(new ConfiguracionMejora
        {
            tipo = TipoMejora.Drone_Atacante_Cantidad,
            nombre = "Dron Atacante +1",
            descripcion = "A�ade un dron atacante adicional",
            costoBase = 150,
            incrementoCostoPorNivel = 150,
            nivelMaximo = MAX_DRONES_ATACANTE - 1 // Ya empezamos con 1
        });

        todasLasMejoras.Add(new ConfiguracionMejora
        {
            tipo = TipoMejora.Drone_Atacante_Daño,
            nombre = "Atacantes: +Da�o",
            descripcion = "Aumenta el da�o de ataque (+15%)",
            costoBase = 200,
            incrementoCostoPorNivel = 100,
            nivelMaximo = MAX_MEJORAS_DAÑO_ATACANTE
        });

        // ===== DEFENSORES =====
        todasLasMejoras.Add(new ConfiguracionMejora
        {
            tipo = TipoMejora.Drone_Defensor_Cantidad,
            nombre = "Dron Defensor +1",
            descripcion = "A�ade un dron defensor adicional",
            costoBase = 200,
            incrementoCostoPorNivel = 200,
            nivelMaximo = MAX_DRONES_DEFENSOR - 1 // Ya empezamos con 1
        });

        todasLasMejoras.Add(new ConfiguracionMejora
        {
            tipo = TipoMejora.Drone_Defensor_Vida,
            nombre = "Defensores: +Vida",
            descripcion = "Aumenta la vida de defensores (+20%)",
            costoBase = 175,
            incrementoCostoPorNivel = 85,
            nivelMaximo = MAX_MEJORAS_VIDA_DEFENSOR
        });
    }

    void ConfigurarDronesIniciales()
    {
        if (sistemaDrones == null) return;

        // Buscadores: Solo 1, siempre activo
        if (sistemaDrones.dronesLista1.Count > 0 && sistemaDrones.dronesLista1[0] != null)
            sistemaDrones.dronesLista1[0].gameObject.SetActive(true);

        // Desactivar buscadores extra (si los hay)
        for (int i = 1; i < sistemaDrones.dronesLista1.Count; i++)
        {
            if (sistemaDrones.dronesLista1[i] != null)
                sistemaDrones.dronesLista1[i].gameObject.SetActive(false);
        }

        // Atacantes: Solo el primero activo
        for (int i = 0; i < sistemaDrones.dronesLista2.Count; i++)
        {
            if (sistemaDrones.dronesLista2[i] != null)
                sistemaDrones.dronesLista2[i].gameObject.SetActive(i == 0);
        }

        // Defensores: Solo el primero activo
        for (int i = 0; i < sistemaDrones.dronesLista3.Count; i++)
        {
            if (sistemaDrones.dronesLista3[i] != null)
                sistemaDrones.dronesLista3[i].gameObject.SetActive(i == 0);
        }
    }

    #endregion

    #region Sistema de Opciones Aleatorias

    /// <summary>
    /// Genera 3 opciones aleatorias para la zona actual
    /// Solo se llama cuando avanzas de zona
    /// </summary>
    public void GenerarOpcionesParaZona()
    {
        opcionesActuales.Clear();
        opcionesCompradas.Clear(); // Resetear compras para la nueva zona

        List<ConfiguracionMejora> mejorasDisponibles = ObtenerMejorasDisponibles();

        if (mejorasDisponibles.Count == 0)
        {
            Debug.Log("�Todas las mejoras est�n al m�ximo!");
            return;
        }

        // Mezclar la lista
        for (int i = mejorasDisponibles.Count - 1; i > 0; i--)
        {
            int j = randomGenerador.Next(i + 1);
            var temp = mejorasDisponibles[i];
            mejorasDisponibles[i] = mejorasDisponibles[j];
            mejorasDisponibles[j] = temp;
        }

        // Tomar las primeras 3 (o menos si no hay suficientes)
        int cantidadOpciones = Mathf.Min(3, mejorasDisponibles.Count);
        for (int i = 0; i < cantidadOpciones; i++)
        {
            opcionesActuales.Add(mejorasDisponibles[i]);
            opcionesCompradas.Add(false); // Ninguna comprada inicialmente
        }

        Debug.Log($"Zona {zonaActual}: Generadas {cantidadOpciones} opciones");
    }

    List<ConfiguracionMejora> ObtenerMejorasDisponibles()
    {
        List<ConfiguracionMejora> disponibles = new List<ConfiguracionMejora>();

        foreach (var mejora in todasLasMejoras)
        {
            // Verificar si est� al m�ximo
            if (mejora.EstaAlMaximo())
                continue;

            // Verificar requisitos especiales
            if (!CumpleRequisitos(mejora))
                continue;

            disponibles.Add(mejora);
        }

        return disponibles;
    }

    bool CumpleRequisitos(ConfiguracionMejora mejora)
    {
        switch (mejora.tipo)
        {
            // Armas: Solo mostrar si NO es el arma equipada actualmente
            case TipoMejora.Arma_Pistola:
                return sistemaArmas == null || sistemaArmas.GetIndiceArmaActual() != 0;
            case TipoMejora.Arma_HandCannon:
                return sistemaArmas == null || sistemaArmas.GetIndiceArmaActual() != 1;
            case TipoMejora.Arma_MachineGun:
                return sistemaArmas == null || sistemaArmas.GetIndiceArmaActual() != 2;
            case TipoMejora.Arma_SMG:
                return sistemaArmas == null || sistemaArmas.GetIndiceArmaActual() != 3;
            case TipoMejora.Arma_Shotgun:
                return sistemaArmas == null || sistemaArmas.GetIndiceArmaActual() != 4;

            // Da�o de atacantes: Solo disponible cuando tenemos max drones
            case TipoMejora.Drone_Atacante_Daño:
                var mejoraCantidadAtacante = ObtenerMejora(TipoMejora.Drone_Atacante_Cantidad);
                return mejoraCantidadAtacante == null || mejoraCantidadAtacante.EstaAlMaximo();

            // Vida de defensores: Solo disponible cuando tenemos max drones
            case TipoMejora.Drone_Defensor_Vida:
                var mejoraCantidadDefensor = ObtenerMejora(TipoMejora.Drone_Defensor_Cantidad);
                return mejoraCantidadDefensor == null || mejoraCantidadDefensor.EstaAlMaximo();
        }

        return true;
    }

    #endregion

    #region Sistema de Compras

    // Rastrea qu� opciones ya fueron compradas en esta zona
    private List<bool> opcionesCompradas = new List<bool>();

    public void ComprarOpcion(int indice)
    {
        if (indice < 0 || indice >= opcionesActuales.Count)
            return;

        // Verificar si ya fue comprada
        if (indice < opcionesCompradas.Count && opcionesCompradas[indice])
        {
            Debug.Log("Esta opci�n ya fue comprada");
            if (sonidoError != null && audioSource != null)
                audioSource.PlayOneShot(sonidoError);
            return;
        }

        var mejora = opcionesActuales[indice];

        if (ObtenerDinero() < mejora.ObtenerCostoActual() || mejora.EstaAlMaximo())
        {
            if (sonidoError != null && audioSource != null)
                audioSource.PlayOneShot(sonidoError);
            return;
        }

        // Cobrar
        RestarDinero(mejora.ObtenerCostoActual());

        // Aplicar mejora
        AplicarMejora(mejora);

        // Incrementar nivel
        mejora.nivelActual++;

        // Marcar como comprada
        if (indice < opcionesCompradas.Count)
            opcionesCompradas[indice] = true;

        if (sonidoCompra != null && audioSource != null)
            audioSource.PlayOneShot(sonidoCompra);

        Debug.Log($"Comprado: {mejora.nombre}");
    }

    /// <summary>
    /// Verifica si una opci�n ya fue comprada
    /// </summary>
    public bool OpcionYaComprada(int indice)
    {
        if (indice < 0 || indice >= opcionesCompradas.Count)
            return false;
        return opcionesCompradas[indice];
    }

    void AplicarMejora(ConfiguracionMejora mejora)
    {
        switch (mejora.tipo)
        {
            // ===== ARMAS =====
            case TipoMejora.Arma_Pistola:
                sistemaArmas?.EquiparArma(0);
                break;
            case TipoMejora.Arma_HandCannon:
                sistemaArmas?.EquiparArma(1);
                break;
            case TipoMejora.Arma_MachineGun:
                sistemaArmas?.EquiparArma(2);
                break;
            case TipoMejora.Arma_SMG:
                sistemaArmas?.EquiparArma(3);
                break;
            case TipoMejora.Arma_Shotgun:
                sistemaArmas?.EquiparArma(4);
                break;

            // ===== BUSCADOR (Solo recursos) =====
            case TipoMejora.Drone_Buscador_Recursos:
                AplicarMejoraRecursosBuscador(mejora.nivelActual + 1);
                break;

            // ===== ATACANTES =====
            case TipoMejora.Drone_Atacante_Cantidad:
                AplicarMejoraCantidadDrones(sistemaDrones.dronesLista2, mejora.nivelActual + 1);
                break;
            case TipoMejora.Drone_Atacante_Daño:
                AplicarMejoraDañoAtacante(mejora.nivelActual + 1);
                break;

            // ===== DEFENSORES =====
            case TipoMejora.Drone_Defensor_Cantidad:
                AplicarMejoraCantidadDrones(sistemaDrones.dronesLista3, mejora.nivelActual + 1);
                break;
            case TipoMejora.Drone_Defensor_Vida:
                AplicarMejoraVidaDefensor(mejora.nivelActual + 1);
                break;
        }
    }

    #endregion

    #region Aplicar Mejoras Espec�ficas

    void AplicarMejoraCantidadDrones(List<Transform> listaDrones, int nuevoNivel)
    {
        // +1 porque ya tenemos 1 dron base
        int dronesAActivar = nuevoNivel + 1;

        for (int i = 0; i < listaDrones.Count && i < dronesAActivar; i++)
        {
            if (listaDrones[i] != null)
                listaDrones[i].gameObject.SetActive(true);
        }

        sistemaDrones?.CambiarListaActiva(sistemaDrones.listaActiva);
    }

    void AplicarMejoraRecursosBuscador(int nivel)
    {
        if (sistemaDrones == null) return;

        float multiplicador = 1f + (nivel * 0.25f); // +25% por nivel

        sistemaDrones.monedasMinimas = Mathf.RoundToInt(monedasMinimasBase * multiplicador);
        sistemaDrones.monedasMaximas = Mathf.RoundToInt(monedasMaximasBase * multiplicador);

        Debug.Log($"Recursos del Buscador: {sistemaDrones.monedasMinimas}-{sistemaDrones.monedasMaximas}");
    }

    void AplicarMejoraDañoAtacante(int nivel)
    {
        if (sistemaDrones == null) return;

        float multiplicador = 1f + (nivel * 0.15f); // +15% por nivel

        foreach (Transform drone in sistemaDrones.dronesLista2)
        {
            if (drone == null) continue;

            // Buscar el modificador de ataque
            AIAttackModifier modifier = drone.GetComponent<AIAttackModifier>();
            if (modifier != null)
            {
                modifier.SetDamageMultiplier(multiplicador);
            }
            else
            {
                // Fallback: usar reflexi�n directa
                AIAttackSystem attackSystem = drone.GetComponent<AIAttackSystem>();
                if (attackSystem != null)
                {
                    var field = typeof(AIAttackSystem).GetField("attackDamage",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        float baseDamage = 10f;
                        field.SetValue(attackSystem, baseDamage * multiplicador);
                    }
                }
            }
        }

        Debug.Log($"Da�o de Atacantes: x{multiplicador:F2}");
    }

    void AplicarMejoraVidaDefensor(int nivel)
    {
        if (sistemaDrones == null) return;

        float multiplicador = 1f + (nivel * 0.20f); // +20% por nivel

        foreach (Transform drone in sistemaDrones.dronesLista3)
        {
            if (drone == null) continue;

            HealthSystem health = drone.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.SetHealthMultiplier(multiplicador);
            }
        }

        Debug.Log($"Vida de Defensores: x{multiplicador:F2}");
    }

    #endregion

    #region M�todos P�blicos

    /// <summary>
    /// Obtiene el dinero actual del jugador (playerResin)
    /// </summary>
    public int ObtenerDinero()
    {
        if (playerStats == null) return 0;
        return playerStats.playerResin;
    }

    /// <summary>
    /// Resta dinero al jugador
    /// </summary>
    private void RestarDinero(int cantidad)
    {
        if (playerStats == null) return;
        playerStats.playerResin -= cantidad;
    }

    /// <summary>
    /// Agrega dinero al jugador
    /// </summary>
    public void AgregarDinero(int cantidad)
    {
        if (playerStats == null) return;
        playerStats.playerResin += cantidad;
        Debug.Log($"Dinero agregado: {cantidad}. Total: {playerStats.playerResin}");
    }

    /// <summary>
    /// Avanza a la siguiente zona y genera nuevas opciones
    /// </summary>
    public void AvanzarZona()
    {
        zonaActual++;
        GenerarOpcionesParaZona();
        Debug.Log($"Avanzando a Zona {zonaActual}");
    }

    /// <summary>
    /// Obtiene una mejora espec�fica por tipo
    /// </summary>
    public ConfiguracionMejora ObtenerMejora(TipoMejora tipo)
    {
        return todasLasMejoras.Find(m => m.tipo == tipo);
    }

    /// <summary>
    /// Obtiene el nivel actual de una mejora
    /// </summary>
    public int ObtenerNivelMejora(TipoMejora tipo)
    {
        var mejora = ObtenerMejora(tipo);
        return mejora?.nivelActual ?? 0;
    }

    /// <summary>
    /// Obtiene las opciones actuales de la tienda
    /// </summary>
    public List<ConfiguracionMejora> ObtenerOpcionesActuales()
    {
        return opcionesActuales;
    }

    /// <summary>
    /// Obtiene el m�ximo de drones para una lista espec�fica
    /// </summary>
    public int ObtenerMaxDronesLista(int numeroLista)
    {
        if (sistemaDrones == null) return 0;

        switch (numeroLista)
        {
            case 0: // Lista 1 - Buscadores (siempre 1)
                return 1;
            case 1: // Lista 2 - Atacantes
                return 1 + ObtenerNivelMejora(TipoMejora.Drone_Atacante_Cantidad);
            case 2: // Lista 3 - Defensores
                return 1 + ObtenerNivelMejora(TipoMejora.Drone_Defensor_Cantidad);
            default:
                return 0;
        }
    }

    /// <summary>
    /// Aplica el l�mite de drones activos seg�n las mejoras compradas
    /// </summary>
    public void AplicarLimiteDronesAListaActiva(int numeroLista)
    {
        if (sistemaDrones == null) return;

        if (numeroLista == 1)
        {
            // Lista 2 (Atacantes): Limitar a los drones comprados
            int maxAtacantes = 1 + ObtenerNivelMejora(TipoMejora.Drone_Atacante_Cantidad);
            for (int i = 0; i < sistemaDrones.dronesLista2.Count; i++)
            {
                if (sistemaDrones.dronesLista2[i] != null)
                {
                    sistemaDrones.dronesLista2[i].gameObject.SetActive(i < maxAtacantes);
                }
            }
        }
        else if (numeroLista == 2)
        {
            // Lista 3 (Defensores): Limitar a los drones comprados
            int maxDefensores = 1 + ObtenerNivelMejora(TipoMejora.Drone_Defensor_Cantidad);
            for (int i = 0; i < sistemaDrones.dronesLista3.Count; i++)
            {
                if (sistemaDrones.dronesLista3[i] != null)
                {
                    sistemaDrones.dronesLista3[i].gameObject.SetActive(i < maxDefensores);
                }
            }
        }
    }

    #endregion

    #region Debug

    [ContextMenu("Debug: Agregar 1000 de dinero")]
    void DebugAgregarDinero()
    {
        AgregarDinero(1000);
    }

    [ContextMenu("Debug: Avanzar Zona")]
    void DebugAvanzarZona()
    {
        AvanzarZona();
    }

    [ContextMenu("Debug: Mostrar Estado")]
    void DebugMostrarEstado()
    {
        foreach (var mejora in todasLasMejoras)
        {
            Debug.Log($"{mejora.nombre}: Nivel {mejora.nivelActual}/{mejora.nivelMaximo} - Costo: ${mejora.ObtenerCostoActual()}");
        }
    }

    #endregion
}