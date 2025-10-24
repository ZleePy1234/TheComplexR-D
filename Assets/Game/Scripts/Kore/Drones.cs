using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
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

    [Header("Sistema de Evasión de Colisiones")]
    public bool evasionActiva = true;
    public LayerMask capasObstaculos;
    public float distanciaDeteccion = 2f;
    public float distanciaMinima = 1f;
    public int numeroRayos = 8;
    public float fuerzaEvasion = 3f;
    public bool evitarJugador = true;
    public string tagJugador = "Player";
    public float distanciaMinimaJugador = 1.5f;
    public bool evitarEnemigos = false;
    public float distanciaMinimaEnemigos = 1f;
    public bool evitarOtrosDrones = true;
    public float distanciaMinimaDrones = 0.8f;

    private List<float> angulosIniciales = new List<float>();
    private List<Transform> dronesActivos = new List<Transform>();
    private List<List<Transform>> todasLasListas = new List<List<Transform>>();
    private ModoDrone modoActual = ModoDrone.OrbitaFija;
    private ModoDrone modoAntesDeMuerte = ModoDrone.OrbitaFija;
    private TipoDron tipoActual = TipoDron.Buscador;
    private List<Vector3> posicionesLibresObjetivo = new List<Vector3>();
    private List<Transform> objetivosActualesDrones = new List<Transform>();
    private GameObject objetivoBusqueda;

    // Nuevo: Sistema para rastrear drones en proceso de revivirlos
    private Dictionary<Transform, float> dronesEnRevivir = new Dictionary<Transform, float>();
    private Dictionary<Transform, HealthSystem> dronesHealthSystems = new Dictionary<Transform, HealthSystem>();
    private Dictionary<Transform, Vector3> posicionesObjetivoOrbita = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, bool> dronesRegresandoOrbita = new Dictionary<Transform, bool>();
    private GameObject jugadorCache;

    void Start()
    {
        todasLasListas.Add(dronesLista1);
        todasLasListas.Add(dronesLista2);
        todasLasListas.Add(dronesLista3);

        CambiarListaActiva(listaActiva);
        InicializarHealthSystems();

        // Cachear referencia al jugador
        if (evitarJugador)
        {
            jugadorCache = GameObject.FindGameObjectWithTag(tagJugador);
        }
    }

    // Inicializa los health systems de todos los drones y suscribe al evento de muerte
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

    // Maneja el evento de muerte de un drone
    void OnDroneMuerto(Transform drone)
    {
        if (!regresarOrbitaAlMorir) return;

        Debug.Log($"{drone.name} ha sido destruido. Regresará a órbita en {tiempoEsperaRevivir} segundos");

        if (!dronesEnRevivir.ContainsKey(drone))
        {
            dronesEnRevivir.Add(drone, tiempoEsperaRevivir);
            dronesRegresandoOrbita[drone] = true;

            // Calcular posición de órbita para este drone
            int indiceDrone = dronesActivos.IndexOf(drone);
            if (indiceDrone >= 0)
            {
                CalcularPosicionOrbitaParaDrone(drone, indiceDrone);
            }
        }

        // Verificar si todos los drones están muertos
        VerificarTodosDronesMuertos();
    }

    // Verifica si todos los drones activos están muertos y cambia a modo órbita
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

    // Calcula la posición objetivo en la órbita para un drone específico
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
        ActualizarDronesRegresandoOrbita();

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
    }

    // Mueve gradualmente los drones muertos hacia su posición en la órbita
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

            Vector3 posicionObjetivo = posicionesObjetivoOrbita[drone];
            Vector3 direccion = (posicionObjetivo - drone.position).normalized;

            // Aplicar evasión si está activa
            if (evasionActiva)
            {
                Vector3 evasion = CalcularVectorEvasion(drone);
                direccion = (direccion + evasion).normalized;
            }

            drone.position = Vector3.MoveTowards(
                drone.position,
                drone.position + direccion * velocidadRetornoOrbita * Time.deltaTime,
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

    // Actualiza el temporizador de drones que deben revivir y regresar a órbita
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

    // Revive el drone y lo mantiene en órbita
    void RevivirDroneYRegresarOrbita(Transform drone)
    {
        if (dronesHealthSystems.ContainsKey(drone))
        {
            HealthSystem health = dronesHealthSystems[drone];
            health.Revive();

            Debug.Log($"{drone.name} ha sido revivido y permanece en órbita");

            // Limpiar referencias
            posicionesObjetivoOrbita.Remove(drone);
            dronesRegresandoOrbita.Remove(drone);
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
    }

    public void CambiarModo(ModoDrone nuevoModo)
    {
        if (modoActual == nuevoModo) return;

        modoActual = nuevoModo;

        // Limpiar el diccionario de drones en proceso de revivir al cambiar de modo manualmente
        if (nuevoModo != ModoDrone.OrbitaFija || dronesEnRevivir.Count == 0)
        {
            dronesEnRevivir.Clear();
            dronesRegresandoOrbita.Clear();
            posicionesObjetivoOrbita.Clear();
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
                    drone.gameObject.SetActive(false);
            }
        }
    }

    void ActivarDronesActivos()
    {
        TiendaMejoras tienda = FindFirstObjectByType<TiendaMejoras>();

        foreach (var drone in dronesActivos)
        {
            if (drone != null)
                drone.gameObject.SetActive(true);
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

            PosicionarObjeto(i, anguloInicial);
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

        dronesActivos[indice].position = centroDeRotacion.position + nuevaPosicion;
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
            Vector3 direccion = (posicionFinal - dronesActivos[i].position).normalized;

            // Aplicar evasión si está activa
            if (evasionActiva)
            {
                Vector3 evasion = CalcularVectorEvasion(dronesActivos[i]);
                direccion = (direccion + evasion).normalized;
            }

            dronesActivos[i].position = Vector3.MoveTowards(
                dronesActivos[i].position,
                dronesActivos[i].position + direccion * velocidadBusqueda * Time.deltaTime,
                velocidadBusqueda * Time.deltaTime
            );
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

                if (Vector3.Distance(dronesActivos[i].position, posicionesLibresObjetivo[i]) < 0.5f)
                {
                    posicionesLibresObjetivo[i] = GenerarPosicionAleatoria();
                }

                posicionObjetivo = posicionesLibresObjetivo[i];
            }

            Vector3 posicionActual = dronesActivos[i].position;
            Vector3 nuevaPosicion = Vector3.Lerp(posicionActual, posicionObjetivo, suavizadoMovimiento * Time.deltaTime);

            // Aplicar evasión si está activa
            if (evasionActiva)
            {
                Vector3 direccion = (nuevaPosicion - posicionActual).normalized;
                Vector3 evasion = CalcularVectorEvasion(dronesActivos[i]);
                direccion = (direccion + evasion).normalized;

                float distanciaMovimiento = Vector3.Distance(posicionActual, nuevaPosicion);
                nuevaPosicion = posicionActual + direccion * distanciaMovimiento;
            }

            dronesActivos[i].position = nuevaPosicion;
        }
    }

    Vector3 GenerarPosicionAleatoria()
    {
        Vector2 posicionAleatoria2D = Random.insideUnitCircle * radioMovimientoLibre;
        return centroDeRotacion.position + new Vector3(
            posicionAleatoria2D.x,
            Random.Range(-alturaOrbita, alturaOrbita),
            posicionAleatoria2D.y
        );
    }

    // Calcula el vector de evasión para evitar colisiones
    Vector3 CalcularVectorEvasion(Transform drone)
    {
        Vector3 evasionTotal = Vector3.zero;

        // 1. Evasión de obstáculos con raycasts en múltiples direcciones
        evasionTotal += DetectarObstaculosRadiales(drone);

        // 2. Evasión del jugador
        if (evitarJugador && jugadorCache != null)
        {
            evasionTotal += EvitarObjetoEspecifico(drone, jugadorCache.transform, distanciaMinimaJugador);
        }

        // 3. Evasión de enemigos
        if (evitarEnemigos)
        {
            GameObject[] enemigos = GameObject.FindGameObjectsWithTag(tagObjetivoDeteccion);
            foreach (GameObject enemigo in enemigos)
            {
                evasionTotal += EvitarObjetoEspecifico(drone, enemigo.transform, distanciaMinimaEnemigos);
            }
        }

        // 4. Evasión de otros drones
        if (evitarOtrosDrones)
        {
            foreach (Transform otroDrone in dronesActivos)
            {
                if (otroDrone != drone && otroDrone != null)
                {
                    evasionTotal += EvitarObjetoEspecifico(drone, otroDrone, distanciaMinimaDrones);
                }
            }
        }

        return evasionTotal.normalized * fuerzaEvasion;
    }

    // Detecta obstáculos usando raycasts radiales
    Vector3 DetectarObstaculosRadiales(Transform drone)
    {
        Vector3 evasion = Vector3.zero;

        for (int i = 0; i < numeroRayos; i++)
        {
            float angulo = (360f / numeroRayos) * i;
            float anguloRad = angulo * Mathf.Deg2Rad;

            Vector3 direccion = new Vector3(
                Mathf.Cos(anguloRad),
                0,
                Mathf.Sin(anguloRad)
            );

            RaycastHit hit;
            if (Physics.Raycast(drone.position, direccion, out hit, distanciaDeteccion, capasObstaculos))
            {
                // Calcular fuerza de repulsión basada en la distancia
                float fuerzaRepulsion = 1f - (hit.distance / distanciaDeteccion);
                Vector3 direccionEvasion = drone.position - hit.point;
                evasion += direccionEvasion.normalized * fuerzaRepulsion;

                // Debug visual (opcional, comentar si no se necesita)
                Debug.DrawRay(drone.position, direccion * hit.distance, Color.red);
            }
            else
            {
                // Debug visual (opcional, comentar si no se necesita)
                Debug.DrawRay(drone.position, direccion * distanciaDeteccion, Color.green);
            }
        }

        return evasion;
    }

    // Evita un objeto específico manteniéndose a cierta distancia
    Vector3 EvitarObjetoEspecifico(Transform drone, Transform objetivo, float distanciaMinima)
    {
        Vector3 direccion = drone.position - objetivo.position;
        float distancia = direccion.magnitude;

        if (distancia < distanciaMinima && distancia > 0.01f)
        {
            // Calcular fuerza de repulsión inversamente proporcional a la distancia
            float fuerzaRepulsion = 1f - (distancia / distanciaMinima);
            return direccion.normalized * fuerzaRepulsion;
        }

        return Vector3.zero;
    }
}