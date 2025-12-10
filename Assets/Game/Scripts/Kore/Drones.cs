using System.Collections;
using System.Collections.Generic;
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

    [Header("Configuraci�n de Retorno a �rbita")]
    public bool regresarOrbitaAlMorir = true;
    public float tiempoEsperaRevivir = 10f;
    public float velocidadRetornoOrbita = 8f;

    [Header("Animación de Muerte")]
    [Tooltip("Delay antes de desactivar el drone (para que termine la animación de muerte)")]
    public float delayDesactivarDrone = 1.5f;
    [Tooltip("Prefab de partículas que aparece cuando el drone muere")]
    public GameObject prefabParticulasMuerte;
    [Tooltip("Transform donde aparecerán las partículas de muerte (si es null, usa la posición del drone)")]
    public Transform puntoParticulasMuerte;

    [Header("Configuraci�n NavMesh")]
    public float distanciaParaActualizarDestino = 0.5f;
    public float distanciaParaLlegarADestino = 0.3f;

    [Header("Habilidades Especiales")]
    [Header("Buscador - Recolecci�n de Monedas")]
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

    [Header("Animaci�n de Movimiento")]
    [Tooltip("Nombre del par�metro bool en el Animator para controlar idle/movimiento")]
    public string parametroMovimiento = "IsMoving";
    [Tooltip("Velocidad m�nima para considerar que el drone se est� moviendo")]
    public float umbralVelocidadMovimiento = 0.1f;

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

    // Animators de los drones
    private Dictionary<Transform, Animator> dronesAnimators = new Dictionary<Transform, Animator>();

    // Habilidades especiales
    private Dictionary<TipoDron, float> tiemposUltimaHabilidad = new Dictionary<TipoDron, float>();
    private Dictionary<TipoDron, bool> habilidadesEnCooldown = new Dictionary<TipoDron, bool>();
    private GameObject escudoActual;

    [SerializeField] private PlayerStats playerStats;

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

        // PRIMERO: Desactivar todos los drones de todas las listas
        DesactivarTodosLosDronesInicial();

        // Inicializar sistemas ANTES de cambiar lista para que los efectos funcionen
        InicializarHealthSystems();
        InicializarNavMeshAgents();
        InicializarAnimators();

        // Ahora si cambiar a la lista activa (esto llamara InicializarOrbitas con los efectos)
        CambiarListaActiva(listaActiva);
    }

    /// <summary>
    /// Desactiva todos los drones al inicio del juego (antes de inicializar sistemas)
    /// </summary>
    void DesactivarTodosLosDronesInicial()
    {
        // Desactivar TODOS los drones de TODAS las listas
        foreach (var drone in dronesLista1)
        {
            if (drone != null) drone.gameObject.SetActive(false);
        }
        foreach (var drone in dronesLista2)
        {
            if (drone != null) drone.gameObject.SetActive(false);
        }
        foreach (var drone in dronesLista3)
        {
            if (drone != null) drone.gameObject.SetActive(false);
        }
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
                        Debug.LogWarning($"Se agreg� NavMeshAgent a {drone.name}. Configura el radio y altura en el Inspector.");
                    }

                    // Configuracion inicial del agente
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
    /// Inicializa los Animators de todos los drones
    /// </summary>
    void InicializarAnimators()
    {
        foreach (var lista in todasLasListas)
        {
            foreach (var drone in lista)
            {
                if (drone != null)
                {
                    Animator animator = drone.GetComponent<Animator>();
                    if (animator != null)
                    {
                        dronesAnimators[drone] = animator;
                    }
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

        Debug.Log($"{drone.name} ha sido destruido. Se desactivara en {delayDesactivarDrone}s y revivira en {tiempoEsperaRevivir} segundos");

        // Iniciar coroutine para manejar la secuencia de muerte
        StartCoroutine(SecuenciaMuerteDrone(drone));

        // Verificar si todos los drones estan muertos
        VerificarTodosDronesMuertos();
    }

    /// <summary>
    /// Coroutine que maneja la secuencia de muerte del drone
    /// </summary>
    IEnumerator SecuenciaMuerteDrone(Transform drone)
    {
        // Desactivar el NavMeshAgent inmediatamente
        if (dronesNavMeshAgents.ContainsKey(drone))
        {
            dronesNavMeshAgents[drone].enabled = false;
        }

        // Esperar el delay para que termine la animacion de muerte
        yield return new WaitForSeconds(delayDesactivarDrone);

        // Instanciar particulas de muerte antes de desactivar
        if (prefabParticulasMuerte != null && drone != null)
        {
            Vector3 posicionParticulas = puntoParticulasMuerte != null ? puntoParticulasMuerte.position : drone.position;
            GameObject particulas = Instantiate(prefabParticulasMuerte, posicionParticulas, Quaternion.identity);
            Destroy(particulas, 3f);
        }

        // Desactivar el drone
        if (drone != null)
        {
            drone.gameObject.SetActive(false);
        }

        // Agregar al diccionario de drones en revivir
        if (!dronesEnRevivir.ContainsKey(drone))
        {
            dronesEnRevivir.Add(drone, tiempoEsperaRevivir);
        }
    }

    /// <summary>
    /// Verifica si todos los drones activos estan muertos y cambia a modo orbita
    /// </summary>
    void VerificarTodosDronesMuertos()
    {
        bool todosMuertos = true;

        foreach (Transform drone in dronesActivos)
        {
            // Solo verificar drones que estan realmente activos (desbloqueados)
            if (drone != null && drone.gameObject.activeInHierarchy && dronesHealthSystems.ContainsKey(drone))
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
            Debug.Log("Todos los drones han muerto. Cambiando a modo �rbita autom�ticamente.");
            modoAntesDeMuerte = modoActual;
            CambiarModo(ModoDrone.OrbitaFija);
        }
    }

    /// <summary>
    /// Calcula la posicion objetivo en la orbita para un drone especifico
    /// </summary>
    void CalcularPosicionOrbitaParaDrone(Transform drone, int indice)
    {
        // Contar solo drones activos para calcular el angulo correctamente
        int dronesActivosCount = ObtenerCantidadDronesActivos();
        float anguloInicial = (360f / dronesActivosCount) * indice;
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
        ActualizarAnimacionesMovimiento();
    }

    /// <summary>
    /// Actualiza el parametro de animacion de movimiento para todos los drones activos
    /// </summary>
    void ActualizarAnimacionesMovimiento()
    {
        List<Transform> dronesRealmenteActivos = ObtenerDronesRealmenteActivos();

        foreach (Transform drone in dronesRealmenteActivos)
        {
            if (drone == null) continue;

            // Verificar si tiene Animator
            if (!dronesAnimators.ContainsKey(drone)) continue;

            Animator animator = dronesAnimators[drone];
            if (animator == null) continue;

            // Obtener velocidad del NavMeshAgent
            bool estaMoviendose = false;

            if (dronesNavMeshAgents.ContainsKey(drone))
            {
                NavMeshAgent agent = dronesNavMeshAgents[drone];
                if (agent != null && agent.enabled)
                {
                    estaMoviendose = agent.velocity.magnitude > umbralVelocidadMovimiento;
                }
            }

            // Actualizar el parametro del Animator
            animator.SetBool(parametroMovimiento, estaMoviendose);
        }
    }

    /// <summary>
    /// Mueve gradualmente los drones muertos hacia su posicion en la orbita
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

            // Movimiento manual mientras esta muerto (sin NavMesh)
            Vector3 posicionObjetivo = posicionesObjetivoOrbita[drone];
            drone.position = Vector3.MoveTowards(
                drone.position,
                posicionObjetivo,
                velocidadRetornoOrbita * Time.deltaTime
            );

            // Si llego a la orbita, empieza a orbitar
            if (Vector3.Distance(drone.position, posicionObjetivo) < 0.1f)
            {
                dronesParaRemover.Add(drone);
            }
        }

        // Remover drones que ya llegaron a la orbita
        foreach (Transform drone in dronesParaRemover)
        {
            dronesRegresandoOrbita.Remove(drone);
        }
    }

    /// <summary>
    /// Actualiza el temporizador de drones que deben revivir y regresar a orbita
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
    /// Revive el drone, lo reactiva y lo teletransporta a orbita
    /// </summary>
    void RevivirDroneYRegresarOrbita(Transform drone)
    {
        if (drone == null) return;

        // PRIMERO: Reactivar el GameObject del drone
        drone.gameObject.SetActive(true);

        // Calcular posicion de orbita considerando solo drones activos
        // Necesitamos calcular ANTES de revivir para tener el indice correcto
        int totalDrones = 0;
        int indiceDrone = -1;

        foreach (Transform d in dronesActivos)
        {
            if (d != null)
            {
                if (d == drone)
                {
                    indiceDrone = totalDrones;
                }
                totalDrones++;
            }
        }

        if (indiceDrone >= 0 && totalDrones > 0)
        {
            float anguloInicial = (360f / totalDrones) * indiceDrone;
            float anguloRad = anguloInicial * Mathf.Deg2Rad;

            Vector3 posicionOrbita = new Vector3(
                Mathf.Cos(anguloRad) * radioOrbita,
                alturaOrbita,
                Mathf.Sin(anguloRad) * radioOrbita
            );

            Vector3 posicionFinal = centroDeRotacion.position + posicionOrbita;

            // Teletransportar - Desactivar NavMesh temporalmente
            if (dronesNavMeshAgents.ContainsKey(drone))
            {
                NavMeshAgent agent = dronesNavMeshAgents[drone];
                agent.enabled = false;
                drone.position = posicionFinal;
                agent.enabled = true;

                dronesTeletransportados.Add(drone);
            }
            else
            {
                drone.position = posicionFinal;
            }

            // Efecto de teletransporte/aparicion
            if (prefabEfectoTeletransporte != null)
            {
                Instantiate(prefabEfectoTeletransporte, posicionFinal, Quaternion.identity);
            }
        }

        // DESPUES: Revivir el HealthSystem (esto disparara el evento OnRevive que restaura la vida)
        if (dronesHealthSystems.ContainsKey(drone))
        {
            HealthSystem health = dronesHealthSystems[drone];
            health.Revive(); // Esto ya pone la vida al maximo
        }

        Debug.Log($"{drone.name} ha sido revivido y teletransportado a orbita");
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
    /// Activa la habilidad especial segun el tipo de dron activo
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

        // Seleccionar un dron aleatorio que esta vivo Y activo (desbloqueado)
        List<Transform> dronesVivos = new List<Transform>();
        foreach (Transform drone in dronesActivos)
        {
            if (drone != null && drone.gameObject.activeInHierarchy)
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

        if (playerStats != null)
        {
            playerStats.playerResin += monedasRecolectadas;
        }

        else
        {
            Debug.Log($"No tiene playerstats asignado");
        }

        Debug.Log($"{drone.name} ha regresado con {monedasRecolectadas} monedas!");

    }

    /// <summary>
    /// Habilidad del Atacante: Por implementar
    /// </summary>
    void HabilidadAtacante()
    {
        Debug.Log("Habilidad de Atacante a�n no implementada.");

        // Aqui ira la logica de la habilidad de ataque
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

        Debug.Log($"Escudo protector activado. Duraci�n: {duracionEscudo}s, Cooldown: {cooldownHabilidadDefensor}s");
    }

    /// <summary>
    /// Destruye el escudo despues del tiempo especificado
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
            // SOLO instanciar efectos en drones activos (desbloqueados)
            foreach (var drone in dronesActivos)
            {
                if (drone != null && drone.gameObject.activeInHierarchy && prefabEfectoDesaparicion != null)
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

        // Configurar velocidad de NavMeshAgents segun el modo - SOLO para drones activos
        foreach (var drone in dronesActivos)
        {
            if (drone != null && drone.gameObject.activeInHierarchy && dronesNavMeshAgents.ContainsKey(drone))
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
                Debug.Log("Modo: �rbita Fija");
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
        // Obtener lista filtrada de drones realmente activos
        List<Transform> dronesRealmenteActivos = ObtenerDronesRealmenteActivos();

        if (angulosIniciales.Count != dronesRealmenteActivos.Count)
        {
            InicializarOrbitas();
            return;
        }

        for (int i = 0; i < dronesRealmenteActivos.Count; i++)
        {
            Transform drone = dronesRealmenteActivos[i];
            if (drone != null && i < angulosIniciales.Count)
            {
                if (dronesTeletransportados.Contains(drone))
                {
                    dronesTeletransportados.Remove(drone);
                    continue;
                }

                // Si el drone esta regresando a orbita, no actualizar su posicion aqui
                if (dronesRegresandoOrbita.ContainsKey(drone))
                    continue;

                float velocidadActual = velocidadRotacion;

                if (velocidadVariada)
                {
                    float variacion = Mathf.Sin(i * 2.0f) * rangoVariacionVelocidad;
                    velocidadActual += variacion;
                }

                float nuevoAngulo = angulosIniciales[i] + (velocidadActual * Time.time);

                PosicionarObjetoActivo(drone, i, nuevoAngulo, dronesRealmenteActivos.Count);
            }
        }
    }

    public void CambiarListaActiva(int numeroLista)
    {
        if (numeroLista < 0 || numeroLista >= todasLasListas.Count)
        {
            Debug.LogWarning($"N�mero de lista inv�lido: {numeroLista}");
            return;
        }

        // Instanciar efectos de DESAPARICION en los drones actuales antes de desactivarlos
        if (prefabEfectoDesaparicion != null)
        {
            foreach (Transform drone in dronesActivos)
            {
                if (drone != null && drone.gameObject.activeInHierarchy)
                {
                    Instantiate(prefabEfectoDesaparicion, drone.position, Quaternion.identity);
                }
            }
        }

        DesactivarTodosLosDrones();

        listaActiva = numeroLista;
        dronesActivos = new List<Transform>(todasLasListas[listaActiva]);

        ActivarDronesActivos();

        CambiarModo(ModoDrone.OrbitaFija);
        CambiarTipo(numeroLista);

        // Contar solo los drones realmente activos
        int dronesActivosCount = ObtenerCantidadDronesActivos();
        Debug.Log($"Cambiado a Lista {listaActiva + 1} con {dronesActivosCount} drones activos");
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

        // Si hay tienda, usar su sistema de limites
        if (tienda != null)
        {
            // Primero activar todos los de la lista
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

            // Luego aplicar limite segun mejoras compradas
            tienda.AplicarLimiteDronesAListaActiva(listaActiva);
        }
        else
        {
            // Sin tienda: solo activar el PRIMER drone de la lista (configuracion por defecto)
            if (dronesActivos.Count > 0 && dronesActivos[0] != null)
            {
                dronesActivos[0].gameObject.SetActive(true);

                if (dronesNavMeshAgents.ContainsKey(dronesActivos[0]))
                {
                    dronesNavMeshAgents[dronesActivos[0]].enabled = true;
                }
            }

            // Asegurar que los demas estan desactivados
            for (int i = 1; i < dronesActivos.Count; i++)
            {
                if (dronesActivos[i] != null)
                {
                    dronesActivos[i].gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Obtiene la cantidad de drones realmente activos (desbloqueados y con GameObject activo)
    /// </summary>
    public int ObtenerCantidadDronesActivos()
    {
        int count = 0;
        foreach (Transform drone in dronesActivos)
        {
            if (drone != null && drone.gameObject.activeInHierarchy)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Obtiene una lista de los drones realmente activos
    /// </summary>
    public List<Transform> ObtenerDronesRealmenteActivos()
    {
        List<Transform> activos = new List<Transform>();
        foreach (Transform drone in dronesActivos)
        {
            if (drone != null && drone.gameObject.activeInHierarchy)
            {
                activos.Add(drone);
            }
        }
        return activos;
    }

    /// <summary>
    /// Obtiene el indice de un drone dentro de los drones realmente activos
    /// </summary>
    private int ObtenerIndiceEnDronesActivos(Transform drone)
    {
        List<Transform> activos = ObtenerDronesRealmenteActivos();
        return activos.IndexOf(drone);
    }

    void InicializarOrbitas()
    {
        angulosIniciales.Clear();

        // Obtener solo los drones realmente activos (desbloqueados)
        List<Transform> dronesRealmenteActivos = ObtenerDronesRealmenteActivos();
        int dronesActivosCount = dronesRealmenteActivos.Count;

        if (dronesActivosCount == 0)
        {
            Debug.LogWarning("No hay drones activos para inicializar �rbitas");
            return;
        }

        for (int i = 0; i < dronesActivosCount; i++)
        {
            Transform drone = dronesRealmenteActivos[i];
            if (drone == null) continue;

            float anguloInicial = 0f;
            if (distribucionAutomatica)
            {
                anguloInicial = (360f / dronesActivosCount) * i;
            }
            else
            {
                Vector3 direccion = drone.position - centroDeRotacion.position;
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

            // Teletransportar el drone a su posicion
            if (dronesNavMeshAgents.ContainsKey(drone))
            {
                NavMeshAgent agent = dronesNavMeshAgents[drone];
                agent.enabled = false;
                drone.position = posicionFinal;
                agent.enabled = true;
            }
            else
            {
                // Si no tiene NavMeshAgent, mover directamente
                drone.position = posicionFinal;
            }

            // Instanciar efecto de APARICION (teletransporte) - SIEMPRE si el drone esta activo
            if (prefabEfectoTeletransporte != null && drone.gameObject.activeInHierarchy)
            {
                Instantiate(prefabEfectoTeletransporte, posicionFinal, Quaternion.identity);
            }
        }
    }

    /// <summary>
    /// Posiciona un drone activo en su orbita
    /// </summary>
    void PosicionarObjetoActivo(Transform drone, int indiceEnActivos, float angulo, int totalDronesActivos)
    {
        if (drone == null) return;

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
        if (dronesNavMeshAgents.ContainsKey(drone))
        {
            NavMeshAgent agent = dronesNavMeshAgents[drone];
            if (agent.enabled && Vector3.Distance(agent.destination, posicionFinal) > distanciaParaActualizarDestino)
            {
                agent.SetDestination(posicionFinal);
            }
        }
    }

    void PosicionarObjeto(int indice, float angulo)
    {
        if (indice >= dronesActivos.Count || dronesActivos[indice] == null)
            return;

        // Verificar si el drone esta realmente activo
        if (!dronesActivos[indice].gameObject.activeInHierarchy)
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
            Debug.LogWarning($"No se encontr� ning�n objeto con el tag '{tagObjetivoBusqueda}'");
        }

        angulosIniciales.Clear();
        List<Transform> dronesRealmenteActivos = ObtenerDronesRealmenteActivos();
        int dronesActivosCount = dronesRealmenteActivos.Count;

        for (int i = 0; i < dronesActivosCount; i++)
        {
            angulosIniciales.Add((360f / dronesActivosCount) * i);
        }
    }

    void ActualizarBusquedaObjetivo()
    {
        if (objetivoBusqueda == null)
        {
            objetivoBusqueda = GameObject.FindGameObjectWithTag(tagObjetivoBusqueda);
            if (objetivoBusqueda == null) return;
        }

        List<Transform> dronesRealmenteActivos = ObtenerDronesRealmenteActivos();

        for (int i = 0; i < dronesRealmenteActivos.Count; i++)
        {
            Transform drone = dronesRealmenteActivos[i];
            if (drone == null) continue;

            if (i >= angulosIniciales.Count) continue;

            float angulo = angulosIniciales[i] + (velocidadRotacion * Time.time);
            float anguloRad = angulo * Mathf.Deg2Rad;

            Vector3 posicionObjetivo = new Vector3(
                Mathf.Cos(anguloRad) * radioBusqueda,
                alturaOrbita,
                Mathf.Sin(anguloRad) * radioBusqueda
            );

            Vector3 posicionFinal = objetivoBusqueda.transform.position + posicionObjetivo;

            // Usar NavMesh para mover el drone
            if (dronesNavMeshAgents.ContainsKey(drone))
            {
                NavMeshAgent agent = dronesNavMeshAgents[drone];
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

        List<Transform> dronesRealmenteActivos = ObtenerDronesRealmenteActivos();

        for (int i = 0; i < dronesRealmenteActivos.Count; i++)
        {
            posicionesLibresObjetivo.Add(GenerarPosicionAleatoria());
            objetivosActualesDrones.Add(null);
        }
    }

    void ActualizarMovimientoLibre()
    {
        GameObject[] enemigos = GameObject.FindGameObjectsWithTag(tagObjetivoDeteccion);

        List<Transform> dronesRealmenteActivos = ObtenerDronesRealmenteActivos();

        for (int i = 0; i < dronesRealmenteActivos.Count; i++)
        {
            Transform drone = dronesRealmenteActivos[i];
            if (drone == null) continue;

            // Verificar si el drone esta muerto o regresando a orbita
            if (dronesHealthSystems.ContainsKey(drone))
            {
                if (dronesHealthSystems[drone].IsDead)
                    continue;
            }

            if (dronesRegresandoOrbita.ContainsKey(drone))
                continue;

            Transform enemigoMasCercano = null;
            float distanciaMinima = rangoDeteccion;

            foreach (GameObject enemigo in enemigos)
            {
                float distancia = Vector3.Distance(drone.position, enemigo.transform.position);
                if (distancia < distanciaMinima)
                {
                    distanciaMinima = distancia;
                    enemigoMasCercano = enemigo.transform;
                }
            }

            Vector3 posicionObjetivo;

            if (enemigoMasCercano != null)
            {
                if (i < objetivosActualesDrones.Count)
                    objetivosActualesDrones[i] = enemigoMasCercano;

                float angulo = Time.time * velocidadRotacion + (i * 360f / dronesRealmenteActivos.Count);
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
                if (i < objetivosActualesDrones.Count)
                    objetivosActualesDrones[i] = null;

                if (dronesNavMeshAgents.ContainsKey(drone))
                {
                    NavMeshAgent agent = dronesNavMeshAgents[drone];

                    // Si llego a su destino aleatorio, generar uno nuevo
                    if (agent.enabled && !agent.pathPending && agent.remainingDistance <= distanciaParaLlegarADestino)
                    {
                        if (i < posicionesLibresObjetivo.Count)
                            posicionesLibresObjetivo[i] = GenerarPosicionAleatoria();
                    }
                }

                posicionObjetivo = i < posicionesLibresObjetivo.Count ? posicionesLibresObjetivo[i] : GenerarPosicionAleatoria();
            }

            // Usar NavMesh para mover el drone
            if (dronesNavMeshAgents.ContainsKey(drone))
            {
                NavMeshAgent agent = dronesNavMeshAgents[drone];
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

        // Asegurar que la posicion esta en el NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(posicionAleatoria, out hit, radioMovimientoLibre, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return posicionAleatoria;
    }
}