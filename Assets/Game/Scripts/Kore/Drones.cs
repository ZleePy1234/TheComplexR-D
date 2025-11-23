using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class Drones : MonoBehaviour
{
    public enum ModoDrone
    {
        OrbitaFija,
        BuscarObjetivo,
        MovimientoLibre
    }

    public enum TipoDron
    {
        Buscador,
        Atacante,
        Defensor
    }

    private HashSet<Transform> dronesTeletransportados = new HashSet<Transform>();
    public Transform centroDeRotacion;
    public List<Transform> dronesLista1 = new List<Transform>();
    public List<Transform> dronesLista2 = new List<Transform>();
    public List<Transform> dronesLista3 = new List<Transform>();
    public int listaActiva = 0;
    public float radioOrbita = 5f;
    public float alturaOrbita = 0f;
    public float velocidadRotacion = 3f;
    public bool velocidadVariada = false;
    public float rangoVariacionVelocidad = 10f;
    public Vector3 ejeRotacion = Vector3.up;
    public bool distribucionAutomatica = true;
    public Key teclaCiclar = Key.Space;
    public Key teclaModoOrbita = Key.E;
    public Key teclaBuscarObjetivo = Key.R;
    public Key teclaMovimientoLibre = Key.F;
    public Key teclaHabilidadEspecial = Key.Q;
    public string tagObjetivoBusqueda = "Objetivo";
    public float velocidadBusqueda = 5f;
    public float radioBusqueda = 3f;
    public string tagObjetivoDeteccion = "Enemigo";
    public float radioMovimientoLibre = 10f;
    public float velocidadMovimientoLibre = 3f;
    public float rangoDeteccion = 8f;
    public float suavizadoMovimiento = 5f;

    [Header("Configuración de Retorno a Órbita")]
    public bool regresarOrbitaAlMorir = true;
    public float tiempoEsperaRevivir = 5f;
    public float velocidadRetornoOrbita = 8f;

    [Header("Configuración NavMesh")]
    public float distanciaParaActualizarDestino = 0.5f;
    public float distanciaParaLlegarADestino = 0.3f;

    [Header("Habilidades Especiales")]
    [Header("Buscador - Recolección de Monedas")]
    public float tiempoDesaparicionBuscador = 3f;
    public int monedasMinimas = 50;
    public int monedasMaximas = 150;
    public float cooldownHabilidadBuscador = 10f;

    [Header("Atacante - Habilidad Pendiente")]
    public float cooldownHabilidadAtacante = 15f;

    [Header("Defensor - Escudo Protector")]
    public GameObject prefabEscudo;
    public Transform jugador;
    public float duracionEscudo = 5f;
    public float cooldownHabilidadDefensor = 20f;

    [Header("Efectos Visuales")]
    public GameObject prefabEfectoTeletransporte;
    public GameObject prefabEfectoDesaparicion;

    private List<float> angulosIniciales = new List<float>();
    private List<Transform> dronesActivos = new List<Transform>();
    private List<List<Transform>> todasLasListas = new List<List<Transform>>();
    private ModoDrone modoActual = ModoDrone.OrbitaFija;
    private ModoDrone modoAntesDeMuerte = ModoDrone.OrbitaFija;
    private TipoDron tipoActual = TipoDron.Buscador;
    private List<Vector3> posicionesLibresObjetivo = new List<Vector3>();
    private List<Transform> objetivosActualesDrones = new List<Transform>();
    private GameObject objetivoBusqueda;

    // Sistema para rastrear drones en proceso de revivirlos
    private Dictionary<Transform, float> dronesEnRevivir = new Dictionary<Transform, float>();
    private Dictionary<Transform, HealthSystem> dronesHealthSystems = new Dictionary<Transform, HealthSystem>();
    private Dictionary<Transform, Vector3> posicionesObjetivoOrbita = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, bool> dronesRegresandoOrbita = new Dictionary<Transform, bool>();

    // NavMesh
    private Dictionary<Transform, NavMeshAgent> dronesNavMeshAgents = new Dictionary<Transform, NavMeshAgent>();

    // Habilidades especiales
    private Dictionary<TipoDron, float> tiemposUltimaHabilidad = new Dictionary<TipoDron, float>();
    private Dictionary<TipoDron, bool> habilidadesEnCooldown = new Dictionary<TipoDron, bool>();
    private GameObject escudoActual;

    void Start()
    {
        todasLasListas.Add(dronesLista1);
        todasLasListas.Add(dronesLista2);
        todasLasListas.Add(dronesLista3);

        tiemposUltimaHabilidad[TipoDron.Buscador] = 0f;
        tiemposUltimaHabilidad[TipoDron.Atacante] = 0f;
        tiemposUltimaHabilidad[TipoDron.Defensor] = 0f;

        habilidadesEnCooldown[TipoDron.Buscador] = false;
        habilidadesEnCooldown[TipoDron.Atacante] = false;
        habilidadesEnCooldown[TipoDron.Defensor] = false;

        CambiarListaActiva(listaActiva);
        InicializarHealthSystems();
        InicializarNavMeshAgents();
    }

    /// <summary>
    /// Inicializa los NavMeshAgents de todos los drones
    /// </summary>
    void InicializarNavMeshAgents()
    {
        foreach (var lista in todasLasListas)
        {
            foreach (var drone in lista)
            {
                if (drone != null)
                {
                    NavMeshAgent agent = drone.GetComponent<NavMeshAgent>();
                    if (agent == null)
                    {
                        agent = drone.gameObject.AddComponent<NavMeshAgent>();
                        Debug.LogWarning($"Se agregó NavMeshAgent a {drone.name}. Configura el radio y altura en el Inspector.");
                    }

                    // Configuración inicial del agente
                    agent.speed = velocidadBusqueda;
                    agent.angularSpeed = 120f;
                    agent.acceleration = 5f;
                    agent.stoppingDistance = distanciaParaLlegarADestino;
                    agent.autoBraking = false;

                    dronesNavMeshAgents[drone] = agent;
                }
            }
        }
    }

    /// <summary>
    /// Inicializa los health systems de todos los drones y suscribe al evento de muerte
    /// </summary>
    void InicializarHealthSystems()
    {
        foreach (var lista in todasLasListas)
        {
            foreach (var drone in lista)
            {
                if (drone != null)
                {
                    HealthSystem health = drone.GetComponent<HealthSystem>();
                    if (health != null)
                    {
                        dronesHealthSystems[drone] = health;
                        health.OnDeath.AddListener(() => OnDroneMuerto(drone));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Maneja el evento de muerte de un drone
    /// </summary>
    void OnDroneMuerto(Transform drone)
    {
        if (!regresarOrbitaAlMorir) return;

        Debug.Log($"{drone.name} ha sido destruido. Regresará a órbita en {tiempoEsperaRevivir} segundos");

        if (!dronesEnRevivir.ContainsKey(drone))
        {
            dronesEnRevivir.Add(drone, tiempoEsperaRevivir);
            //dronesRegresandoOrbita[drone] = true;

            // Desactivar el NavMeshAgent mientras está muerto
            if (dronesNavMeshAgents.ContainsKey(drone))
            {
                dronesNavMeshAgents[drone].enabled = false;
            }

            // Calcular posición de órbita para este drone
            //int indiceDrone = dronesActivos.IndexOf(drone);
            //if (indiceDrone >= 0)
            //{
            //    CalcularPosicionOrbitaParaDrone(drone, indiceDrone);
            //}
        }

        // Verificar si todos los drones están muertos
        VerificarTodosDronesMuertos();
    }

    /// <summary>
    /// Verifica si todos los drones activos están muertos y cambia a modo órbita
    /// </summary>
    void VerificarTodosDronesMuertos()
    {
        bool todosMuertos = true;

        foreach (Transform drone in dronesActivos)
        {
            if (drone != null && dronesHealthSystems.ContainsKey(drone))
            {
                if (!dronesHealthSystems[drone].IsDead)
                {
                    todosMuertos = false;
                    break;
                }
            }
        }

        if (todosMuertos && modoActual != ModoDrone.OrbitaFija)
        {
            Debug.Log("Todos los drones han muerto. Cambiando a modo órbita automáticamente.");
            modoAntesDeMuerte = modoActual;
            CambiarModo(ModoDrone.OrbitaFija);
        }
    }

    /// <summary>
    /// Calcula la posición objetivo en la órbita para un drone específico
    /// </summary>
    void CalcularPosicionOrbitaParaDrone(Transform drone, int indice)
    {
        float anguloInicial = (360f / dronesActivos.Count) * indice;
        float anguloRad = anguloInicial * Mathf.Deg2Rad;

        Vector3 posicionOrbita = Vector3.zero;

        if (ejeRotacion == Vector3.up)
        {
            posicionOrbita = new Vector3(
                Mathf.Cos(anguloRad) * radioOrbita,
                alturaOrbita,
                Mathf.Sin(anguloRad) * radioOrbita
            );
        }
        else if (ejeRotacion == Vector3.right)
        {
            posicionOrbita = new Vector3(
                alturaOrbita,
                Mathf.Cos(anguloRad) * radioOrbita,
                Mathf.Sin(anguloRad) * radioOrbita
            );
        }
        else if (ejeRotacion == Vector3.forward)
        {
            posicionOrbita = new Vector3(
                Mathf.Cos(anguloRad) * radioOrbita,
                Mathf.Sin(anguloRad) * radioOrbita,
                alturaOrbita
            );
        }
        else
        {
            Vector3 perpendicular1 = Vector3.Cross(ejeRotacion, Vector3.up);
            if (perpendicular1.magnitude < 0.1f)
                perpendicular1 = Vector3.Cross(ejeRotacion, Vector3.right);
            perpendicular1.Normalize();

            Vector3 perpendicular2 = Vector3.Cross(ejeRotacion, perpendicular1);
            perpendicular2.Normalize();

            posicionOrbita = perpendicular1 * Mathf.Cos(anguloRad) * radioOrbita +
                           perpendicular2 * Mathf.Sin(anguloRad) * radioOrbita +
                           ejeRotacion * alturaOrbita;
        }

        posicionesObjetivoOrbita[drone] = centroDeRotacion.position + posicionOrbita;
    }

    private void Update()
    {
        ControlTeclado();

        // Actualizar drones que están regresando a órbita
        //ActualizarDronesRegresandoOrbita();

        switch (modoActual)
        {
            case ModoDrone.OrbitaFija:                
                ActualizarPosicionesDrones();
                break;
            case ModoDrone.BuscarObjetivo:
                ActualizarBusquedaObjetivo();
                break;
            case ModoDrone.MovimientoLibre:
                ActualizarMovimientoLibre();
                break;
        }

        ActualizarDronesEnRevivir();
        ActualizarCooldownHabilidad();
    }

    /// <summary>
    /// Mueve gradualmente los drones muertos hacia su posición en la órbita
    /// </summary>
    void ActualizarDronesRegresandoOrbita()
    {
        List<Transform> dronesParaRemover = new List<Transform>();

        foreach (var kvp in dronesRegresandoOrbita)
        {
            Transform drone = kvp.Key;

            if (drone == null || !posicionesObjetivoOrbita.ContainsKey(drone))
            {
                dronesParaRemover.Add(drone);
                continue;
            }

            // Movimiento manual mientras está muerto (sin NavMesh)
            Vector3 posicionObjetivo = posicionesObjetivoOrbita[drone];
            drone.position = Vector3.MoveTowards(
                drone.position,
                posicionObjetivo,
                velocidadRetornoOrbita * Time.deltaTime
            );

            // Si llegó a la órbita, empieza a orbitar
            if (Vector3.Distance(drone.position, posicionObjetivo) < 0.1f)
            {
                dronesParaRemover.Add(drone);
            }
        }

        // Remover drones que ya llegaron a la órbita
        foreach (Transform drone in dronesParaRemover)
        {
            dronesRegresandoOrbita.Remove(drone);
        }
    }

    /// <summary>
    /// Actualiza el temporizador de drones que deben revivir y regresar a órbita
    /// </summary>
    void ActualizarDronesEnRevivir()
    {
        if (dronesEnRevivir.Count == 0) return;

        List<Transform> dronesParaRevivir = new List<Transform>();
        List<Transform> dronesToUpdate = new List<Transform>(dronesEnRevivir.Keys);

        foreach (Transform drone in dronesToUpdate)
        {
            float tiempoRestante = dronesEnRevivir[drone] - Time.deltaTime;

            if (tiempoRestante <= 0)
            {
                dronesParaRevivir.Add(drone);
            }
            else
            {
                dronesEnRevivir[drone] = tiempoRestante;
            }
        }

        foreach (Transform drone in dronesParaRevivir)
        {
            RevivirDroneYRegresarOrbita(drone);
            dronesEnRevivir.Remove(drone);
        }
    }

    /// <summary>
    /// Revive el drone y lo mantiene en órbita
    /// </summary>
    void RevivirDroneYRegresarOrbita(Transform drone)
    {
        if (dronesHealthSystems.ContainsKey(drone))
        {
            HealthSystem health = dronesHealthSystems[drone];
            health.Revive();

            // Calcular posición de órbita
            int indiceDrone = dronesActivos.IndexOf(drone);
            if (indiceDrone >= 0)
            {
                float anguloInicial = (360f / dronesActivos.Count) * indiceDrone;
                float anguloRad = anguloInicial * Mathf.Deg2Rad;

                Vector3 posicionOrbita = new Vector3(
                    Mathf.Cos(anguloRad) * radioOrbita,
                    alturaOrbita,
                    Mathf.Sin(anguloRad) * radioOrbita
                );

                Vector3 posicionFinal = centroDeRotacion.position + posicionOrbita;

                // TELETRANSPORTE INSTANTÁNEO - Desactivar NavMesh temporalmente
                if (dronesNavMeshAgents.ContainsKey(drone))
                {
                    NavMeshAgent agent = dronesNavMeshAgents[drone];
                    agent.enabled = false;
                    drone.position = posicionFinal; // Teletransporte directo
                    agent.enabled = true;

                    dronesTeletransportados.Add(drone);
                }
                else
                {
                    drone.position = posicionFinal;
                }

                // Efecto de teletransporte
                if (prefabEfectoDesaparicion != null)
                {
                    Instantiate(prefabEfectoDesaparicion, posicionFinal, Quaternion.identity);
                }
            }

            Debug.Log($"{drone.name} ha sido revivido y teletransportado a órbita");
        }
    }
    

    void ControlTeclado()
    {
        var keyboard = Keyboard.current;
        if (keyboard[teclaCiclar].wasPressedThisFrame)
            CiclarLista();

        if (keyboard[teclaModoOrbita].wasPressedThisFrame)
            CambiarModo(ModoDrone.OrbitaFija);
        else if (keyboard[teclaBuscarObjetivo].wasPressedThisFrame && tipoActual == TipoDron.Buscador)
            CambiarModo(ModoDrone.BuscarObjetivo);
        else if (keyboard[teclaMovimientoLibre].wasPressedThisFrame && tipoActual == TipoDron.Atacante)
            CambiarModo(ModoDrone.MovimientoLibre);
        else if (keyboard[teclaMovimientoLibre].wasPressedThisFrame && tipoActual == TipoDron.Defensor)
            CambiarModo(ModoDrone.MovimientoLibre);

        // Habilidad especial
        if (keyboard[teclaHabilidadEspecial].wasPressedThisFrame)
            ActivarHabilidadEspecial();
    }

    #region Habilidades Especiales

    /// <summary>
    /// Activa la habilidad especial según el tipo de dron activo
    /// </summary>
    public void ActivarHabilidadEspecial()
    {
        if (habilidadesEnCooldown[tipoActual])
        {
            Debug.Log("Habilidad en cooldown. Espera un momento.");
            return;
        }

        switch (tipoActual)
        {
            case TipoDron.Buscador:
                HabilidadBuscador();
                break;
            case TipoDron.Atacante:
                HabilidadAtacante();
                break;
            case TipoDron.Defensor:
                HabilidadDefensor();
                break;
        }
    }

    /// <summary>
    /// Habilidad del Buscador: Desaparece y regresa con monedas
    /// </summary>
    void HabilidadBuscador()
    {
        if (dronesActivos.Count == 0)
        {
            Debug.LogWarning("No hay drones activos para usar la habilidad.");
            return;
        }

        // Seleccionar un dron aleatorio que esté vivo
        List<Transform> dronesVivos = new List<Transform>();
        foreach (Transform drone in dronesActivos)
        {
            if (drone != null)
            {
                dronesVivos.Add(drone);
            }
        }

        if (dronesVivos.Count == 0)
        {
            Debug.LogWarning("No hay drones vivos para usar la habilidad.");
            return;
        }

        Transform dronSeleccionado = dronesVivos[Random.Range(0, dronesVivos.Count)];
        StartCoroutine(RecoleccionMonedas(dronSeleccionado));

        habilidadesEnCooldown[TipoDron.Buscador] = true;
        tiemposUltimaHabilidad[TipoDron.Buscador] = Time.time;

        Debug.Log($"Habilidad Buscador activada. Cooldown: {cooldownHabilidadBuscador}s");
    }

    /// <summary>
    /// Corrutina que hace desaparecer el drone y lo regresa con monedas
    /// </summary>
    IEnumerator RecoleccionMonedas(Transform drone)
    {
        Vector3 posicionOriginal = drone.position;

        // Desactivar visualmente el drone (puedes usar un renderer o toda la mesh)
        Renderer[] renderers = drone.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        // Desactivar NavMeshAgent temporalmente
        if (dronesNavMeshAgents.ContainsKey(drone))
        {
            dronesNavMeshAgents[drone].enabled = false;

        }

        if (prefabEfectoDesaparicion != null)
        {
            Instantiate(prefabEfectoDesaparicion, drone.position, Quaternion.identity);
        }

        Debug.Log($"{drone.name} ha desaparecido para recolectar monedas...");

        yield return new WaitForSeconds(tiempoDesaparicionBuscador);

        // Reactivar el drone
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
        }

        if (dronesNavMeshAgents.ContainsKey(drone))
        {
            dronesNavMeshAgents[drone].enabled = true;
        }

        if (prefabEfectoTeletransporte != null)
        {
            Instantiate(prefabEfectoTeletransporte, drone.position, Quaternion.identity);
        }

        // Generar monedas aleatorias
        int monedasRecolectadas = Random.Range(monedasMinimas, monedasMaximas + 1);

        // Aquí debes llamar a tu sistema de monedas del juego
        // Ejemplo: GameManager.Instance.AgregarMonedas(monedasRecolectadas);
        Debug.Log($"{drone.name} ha regresado con {monedasRecolectadas} monedas!");

        // Si tienes un GameManager con sistema de monedas, descomenta esto:
        // GameManager gameManager = FindFirstObjectByType<GameManager>();
        // if (gameManager != null)
        // {
        //     gameManager.AgregarMonedas(monedasRecolectadas);
        // }
    }

    /// <summary>
    /// Habilidad del Atacante: Por implementar
    /// </summary>
    void HabilidadAtacante()
    {
        Debug.Log("Habilidad de Atacante aún no implementada.");

        // Aquí irá la lógica de la habilidad de ataque
        // Por ahora solo activamos el cooldown para probar
        habilidadesEnCooldown[TipoDron.Atacante] = true;
        tiemposUltimaHabilidad[TipoDron.Atacante] = Time.time;
    }

    /// <summary>
    /// Habilidad del Defensor: Invoca un escudo protector
    /// </summary>
    void HabilidadDefensor()
    {
        if (jugador == null)
        {
            Debug.LogWarning("No se ha asignado el Transform del jugador.");
            return;
        }

        if (prefabEscudo == null)
        {
            Debug.LogWarning("No se ha asignado el prefab del escudo.");
            return;
        }

        // Si ya hay un escudo activo, no crear otro
        if (escudoActual != null)
        {
            Debug.Log("Ya hay un escudo activo.");
            return;
        }

        // Instanciar el escudo como hijo del jugador
        escudoActual = Instantiate(prefabEscudo, jugador.position, Quaternion.identity, jugador);

        StartCoroutine(DestruirEscudoDespuesDeTiempo());

        habilidadesEnCooldown[TipoDron.Defensor] = true;
        tiemposUltimaHabilidad[TipoDron.Defensor] = Time.time;

        Debug.Log($"Escudo protector activado. Duración: {duracionEscudo}s, Cooldown: {cooldownHabilidadDefensor}s");
    }

    /// <summary>
    /// Destruye el escudo después del tiempo especificado
    /// </summary>
    IEnumerator DestruirEscudoDespuesDeTiempo()
    {
        yield return new WaitForSeconds(duracionEscudo);

        if (escudoActual != null)
        {
            Destroy(escudoActual);
            Debug.Log("El escudo ha desaparecido.");
        }
    }

    /// <summary>
    /// Actualiza el cooldown de las habilidades
    /// </summary>
    void ActualizarCooldownHabilidad()
    {
        foreach (TipoDron tipo in System.Enum.GetValues(typeof(TipoDron)))
        {
            if (!habilidadesEnCooldown[tipo]) continue;

            float cooldownActual = 0f;
            switch (tipo)
            {
                case TipoDron.Buscador:
                    cooldownActual = cooldownHabilidadBuscador;
                    break;
                case TipoDron.Atacante:
                    cooldownActual = cooldownHabilidadAtacante;
                    break;
                case TipoDron.Defensor:
                    cooldownActual = cooldownHabilidadDefensor;
                    break;
            }

            if (Time.time - tiemposUltimaHabilidad[tipo] >= cooldownActual)
            {
                habilidadesEnCooldown[tipo] = false;
                if (tipo == tipoActual)
                {
                    Debug.Log($"Habilidad {tipo} lista para usar.");
                }
            }
        }
    }

    /// <summary>
    /// Obtiene el tiempo restante de cooldown
    /// </summary>
    public float ObtenerTiempoRestanteCooldown()
    {
        if (!habilidadesEnCooldown[tipoActual]) return 0f;

        float cooldownActual = 0f;
        switch (tipoActual)
        {
            case TipoDron.Buscador:
                cooldownActual = cooldownHabilidadBuscador;
                break;
            case TipoDron.Atacante:
                cooldownActual = cooldownHabilidadAtacante;
                break;
            case TipoDron.Defensor:
                cooldownActual = cooldownHabilidadDefensor;
                break;
        }

        float tiempoRestante = cooldownActual - (Time.time - tiemposUltimaHabilidad[tipoActual]);
        return Mathf.Max(0f, tiempoRestante);
    }

    #endregion

    public void CambiarModo(ModoDrone nuevoModo)
    {
        if (modoActual == nuevoModo) return;

        if (nuevoModo == ModoDrone.OrbitaFija && modoActual != ModoDrone.OrbitaFija)
        {
            foreach (var drone in dronesActivos)
            {
                if (drone != null && prefabEfectoDesaparicion != null)
                {
                    Instantiate(prefabEfectoDesaparicion, drone.position, Quaternion.identity);
                }
            }
        }

        modoActual = nuevoModo;

        // Limpiar el diccionario de drones en proceso de revivir al cambiar de modo manualmente
        if (nuevoModo != ModoDrone.OrbitaFija || dronesEnRevivir.Count == 0)
        {
            dronesEnRevivir.Clear();
            dronesRegresandoOrbita.Clear();
            posicionesObjetivoOrbita.Clear();
        }

        // Configurar velocidad de NavMeshAgents según el modo
        foreach (var drone in dronesActivos)
        {
            if (drone != null && dronesNavMeshAgents.ContainsKey(drone))
            {
                NavMeshAgent agent = dronesNavMeshAgents[drone];

                switch (nuevoModo)
                {
                    case ModoDrone.OrbitaFija:
                        agent.speed = 3f;
                        break;
                    case ModoDrone.BuscarObjetivo:
                        agent.speed = velocidadBusqueda;
                        break;
                    case ModoDrone.MovimientoLibre:
                        agent.speed = velocidadMovimientoLibre;
                        break;
                }
            }
        }

        switch (nuevoModo)
        {
            case ModoDrone.OrbitaFija:
                InicializarOrbitas();
                Debug.Log("Modo: Órbita Fija");
                break;
            case ModoDrone.BuscarObjetivo:
                InicializarBusquedaObjetivo();
                Debug.Log("Modo: Buscar Objetivo");
                break;
            case ModoDrone.MovimientoLibre:
                InicializarMovimientoLibre();
                Debug.Log("Modo: Movimiento Libre");
                break;
        }
    }

    public void CambiarTipo(int tipoDron)
    {
        if (tipoDron == 0)
        {
            tipoActual = TipoDron.Buscador;
            Debug.Log("Tipo Buscador");
        }
        else if (tipoDron == 1)
        {
            tipoActual = TipoDron.Atacante;
            Debug.Log("Tipo Atacante");
        }
        else if (tipoDron == 2)
        {
            tipoActual = TipoDron.Defensor;
            Debug.Log("Tipo Defensor");
        }
    }

    void ActualizarPosicionesDrones()
    {
        if (angulosIniciales.Count != dronesActivos.Count)
        {
            InicializarOrbitas();
            return;
        }

        for (int i = 0; i < dronesActivos.Count; i++)
        {
            if (dronesActivos[i] != null && i < angulosIniciales.Count)
            {
                if (dronesTeletransportados.Contains(dronesActivos[i]))
                {
                    dronesTeletransportados.Remove(dronesActivos[i]);
                    continue;
                }

                // Si el drone está regresando a órbita, no actualizar su posición aquí
                if (dronesRegresandoOrbita.ContainsKey(dronesActivos[i]))
                    continue;

                float velocidadActual = velocidadRotacion;

                if (velocidadVariada)
                {
                    float variacion = Mathf.Sin(i * 2.0f) * rangoVariacionVelocidad;
                    velocidadActual += variacion;
                }

                float nuevoAngulo = angulosIniciales[i] + (velocidadActual * Time.time);

                PosicionarObjeto(i, nuevoAngulo);
            }
        }
    }

    public void CambiarListaActiva(int numeroLista)
    {
        if (numeroLista < 0 || numeroLista >= todasLasListas.Count)
        {
            Debug.LogWarning($"Número de lista inválido: {numeroLista}");
            return;
        }

        DesactivarTodosLosDrones();

        listaActiva = numeroLista;
        dronesActivos = new List<Transform>(todasLasListas[listaActiva]);

        ActivarDronesActivos();

        CambiarModo(ModoDrone.OrbitaFija);
        CambiarTipo(numeroLista);

        Debug.Log($"Cambiado a Lista {listaActiva + 1} con {dronesActivos.Count} drones");
    }

    public void CiclarLista()
    {
        int siguienteLista = (listaActiva + 1) % todasLasListas.Count;
        CambiarListaActiva(siguienteLista);
    }

    void DesactivarTodosLosDrones()
    {
        foreach (var lista in todasLasListas)
        {
            foreach (var drone in lista)
            {
                if (drone != null)
                {
                    drone.gameObject.SetActive(false);

                    // Desactivar NavMeshAgent
                    if (dronesNavMeshAgents.ContainsKey(drone))
                    {
                        dronesNavMeshAgents[drone].enabled = false;
                    }
                }
            }
        }
    }

    void ActivarDronesActivos()
    {
        TiendaMejoras tienda = FindFirstObjectByType<TiendaMejoras>();

        foreach (var drone in dronesActivos)
        {
            if (drone != null)
            {
                drone.gameObject.SetActive(true);

                // Activar NavMeshAgent
                if (dronesNavMeshAgents.ContainsKey(drone))
                {
                    dronesNavMeshAgents[drone].enabled = true;
                }
            }
        }

        // Aplicar límite de drones comprados si existe el sistema de mejoras
        if (tienda != null)
        {
            tienda.AplicarLimiteDronesAListaActiva(listaActiva);
        }
    }

    void InicializarOrbitas()
    {
        angulosIniciales.Clear();

        for (int i = 0; i < dronesActivos.Count; i++)
        {
            if (dronesActivos[i] == null) continue;

            float anguloInicial = 0f;
            if (distribucionAutomatica)
            {
                anguloInicial = (360f / dronesActivos.Count) * i;
            }
            else
            {
                Vector3 direccion = dronesActivos[i].position - centroDeRotacion.position;
                anguloInicial = Mathf.Atan2(direccion.z, direccion.x) * Mathf.Rad2Deg;
            }
            angulosIniciales.Add(anguloInicial);

            float anguloRad = anguloInicial * Mathf.Deg2Rad;
            Vector3 posicionOrbita = new Vector3(
                Mathf.Cos(anguloRad) * radioOrbita,
                alturaOrbita,
                Mathf.Sin(anguloRad) * radioOrbita
            );
            Vector3 posicionFinal = centroDeRotacion.position + posicionOrbita;

            if (dronesNavMeshAgents.ContainsKey(dronesActivos[i]))
            {
                NavMeshAgent agent = dronesNavMeshAgents[dronesActivos[i]];
                agent.enabled = false;
                dronesActivos[i].position = posicionFinal;
                agent.enabled = true;

                if (prefabEfectoTeletransporte != null)
                {
                    Instantiate(prefabEfectoTeletransporte, posicionFinal, Quaternion.identity);
                }
            }

            //PosicionarObjeto(i, anguloInicial);
        }
    }

    void PosicionarObjeto(int indice, float angulo)
    {
        if (indice >= dronesActivos.Count || dronesActivos[indice] == null)
            return;

        float anguloRad = angulo * Mathf.Deg2Rad;
        Vector3 nuevaPosicion = Vector3.zero;

        if (ejeRotacion == Vector3.up)
        {
            nuevaPosicion = new Vector3(
                Mathf.Cos(anguloRad) * radioOrbita,
                alturaOrbita,
                Mathf.Sin(anguloRad) * radioOrbita
            );
        }
        else if (ejeRotacion == Vector3.right)
        {
            nuevaPosicion = new Vector3(
                alturaOrbita,
                Mathf.Cos(anguloRad) * radioOrbita,
                Mathf.Sin(anguloRad) * radioOrbita
            );
        }
        else if (ejeRotacion == Vector3.forward)
        {
            nuevaPosicion = new Vector3(
                Mathf.Cos(anguloRad) * radioOrbita,
                Mathf.Sin(anguloRad) * radioOrbita,
                alturaOrbita
            );
        }
        else
        {
            Vector3 perpendicular1 = Vector3.Cross(ejeRotacion, Vector3.up);
            if (perpendicular1.magnitude < 0.1f)
                perpendicular1 = Vector3.Cross(ejeRotacion, Vector3.right);
            perpendicular1.Normalize();

            Vector3 perpendicular2 = Vector3.Cross(ejeRotacion, perpendicular1);
            perpendicular2.Normalize();

            nuevaPosicion = perpendicular1 * Mathf.Cos(anguloRad) * radioOrbita +
                           perpendicular2 * Mathf.Sin(anguloRad) * radioOrbita +
                           ejeRotacion * alturaOrbita;
        }

        Vector3 posicionFinal = centroDeRotacion.position + nuevaPosicion;

        // Usar NavMesh para mover el drone
        if (dronesNavMeshAgents.ContainsKey(dronesActivos[indice]))
        {
            NavMeshAgent agent = dronesNavMeshAgents[dronesActivos[indice]];
            if (agent.enabled && Vector3.Distance(agent.destination, posicionFinal) > distanciaParaActualizarDestino)
            {
                agent.SetDestination(posicionFinal);
            }
        }
    }

    void InicializarBusquedaObjetivo()
    {
        objetivoBusqueda = GameObject.FindGameObjectWithTag(tagObjetivoBusqueda);

        if (objetivoBusqueda == null)
        {
            Debug.LogWarning($"No se encontró ningún objeto con el tag '{tagObjetivoBusqueda}'");
        }

        angulosIniciales.Clear();
        for (int i = 0; i < dronesActivos.Count; i++)
        {
            angulosIniciales.Add((360f / dronesActivos.Count) * i);
        }
    }

    void ActualizarBusquedaObjetivo()
    {
        if (objetivoBusqueda == null)
        {
            objetivoBusqueda = GameObject.FindGameObjectWithTag(tagObjetivoBusqueda);
            if (objetivoBusqueda == null) return;
        }

        for (int i = 0; i < dronesActivos.Count; i++)
        {
            if (dronesActivos[i] == null) continue;

            float angulo = angulosIniciales[i] + (velocidadRotacion * Time.time);
            float anguloRad = angulo * Mathf.Deg2Rad;

            Vector3 posicionObjetivo = new Vector3(
                Mathf.Cos(anguloRad) * radioBusqueda,
                alturaOrbita,
                Mathf.Sin(anguloRad) * radioBusqueda
            );

            Vector3 posicionFinal = objetivoBusqueda.transform.position + posicionObjetivo;

            // Usar NavMesh para mover el drone
            if (dronesNavMeshAgents.ContainsKey(dronesActivos[i]))
            {
                NavMeshAgent agent = dronesNavMeshAgents[dronesActivos[i]];
                if (agent.enabled && Vector3.Distance(agent.destination, posicionFinal) > distanciaParaActualizarDestino)
                {
                    agent.SetDestination(posicionFinal);
                }
            }
        }
    }

    void InicializarMovimientoLibre()
    {
        posicionesLibresObjetivo.Clear();
        objetivosActualesDrones.Clear();

        for (int i = 0; i < dronesActivos.Count; i++)
        {
            posicionesLibresObjetivo.Add(GenerarPosicionAleatoria());
            objetivosActualesDrones.Add(null);
        }
    }

    void ActualizarMovimientoLibre()
    {
        GameObject[] enemigos = GameObject.FindGameObjectsWithTag(tagObjetivoDeteccion);

        for (int i = 0; i < dronesActivos.Count; i++)
        {
            if (dronesActivos[i] == null) continue;

            // Verificar si el drone está muerto o regresando a órbita
            if (dronesHealthSystems.ContainsKey(dronesActivos[i]))
            {
                if (dronesHealthSystems[dronesActivos[i]].IsDead)
                    continue;
            }

            if (dronesRegresandoOrbita.ContainsKey(dronesActivos[i]))
                continue;

            Transform enemigoMasCercano = null;
            float distanciaMinima = rangoDeteccion;

            foreach (GameObject enemigo in enemigos)
            {
                float distancia = Vector3.Distance(dronesActivos[i].position, enemigo.transform.position);
                if (distancia < distanciaMinima)
                {
                    distanciaMinima = distancia;
                    enemigoMasCercano = enemigo.transform;
                }
            }

            Vector3 posicionObjetivo;

            if (enemigoMasCercano != null)
            {
                objetivosActualesDrones[i] = enemigoMasCercano;

                float angulo = Time.time * velocidadRotacion + (i * 360f / dronesActivos.Count);
                float anguloRad = angulo * Mathf.Deg2Rad;

                Vector3 offset = new Vector3(
                    Mathf.Cos(anguloRad) * radioBusqueda,
                    alturaOrbita,
                    Mathf.Sin(anguloRad) * radioBusqueda
                );

                posicionObjetivo = enemigoMasCercano.position + offset;
            }
            else
            {
                objetivosActualesDrones[i] = null;

                if (dronesNavMeshAgents.ContainsKey(dronesActivos[i]))
                {
                    NavMeshAgent agent = dronesNavMeshAgents[dronesActivos[i]];

                    // Si llegó a su destino aleatorio, generar uno nuevo
                    if (agent.enabled && !agent.pathPending && agent.remainingDistance <= distanciaParaLlegarADestino)
                    {
                        posicionesLibresObjetivo[i] = GenerarPosicionAleatoria();
                    }
                }

                posicionObjetivo = posicionesLibresObjetivo[i];
            }

            // Usar NavMesh para mover el drone
            if (dronesNavMeshAgents.ContainsKey(dronesActivos[i]))
            {
                NavMeshAgent agent = dronesNavMeshAgents[dronesActivos[i]];
                if (agent.enabled && Vector3.Distance(agent.destination, posicionObjetivo) > distanciaParaActualizarDestino)
                {
                    agent.SetDestination(posicionObjetivo);
                }
            }
        }
    }

    Vector3 GenerarPosicionAleatoria()
    {
        Vector2 posicionAleatoria2D = Random.insideUnitCircle * radioMovimientoLibre;
        Vector3 posicionAleatoria = centroDeRotacion.position + new Vector3(
            posicionAleatoria2D.x,
            Random.Range(-alturaOrbita, alturaOrbita),
            posicionAleatoria2D.y
        );

        // Asegurar que la posición esté en el NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(posicionAleatoria, out hit, radioMovimientoLibre, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return posicionAleatoria;
    }
}