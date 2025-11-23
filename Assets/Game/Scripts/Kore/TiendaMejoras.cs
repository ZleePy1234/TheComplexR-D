using TMPro;
using UnityEngine;

public class TiendaMejoras : MonoBehaviour
{
    [Header("Referencias")]
    public Drones sistemaDrones;

    [Header("Economía")]
    public int dineroActual = 0;

    [Header("Costos de Mejoras - Lista 1 (Buscadores)")]
    public int costoMejoraLista1 = 100;

    [Header("Costos de Mejoras - Lista 2 (Atacantes)")]
    public int costoMejoraLista2Nivel1 = 150;  // Para el segundo dron
    public int costoMejoraLista2Nivel2 = 300;  // Para el tercer dron

    [Header("Costos de Mejoras - Lista 3 (Defensores)")]
    public int costoMejoraLista3Nivel1 = 200;  // Para el segundo dron
    public int costoMejoraLista3Nivel2 = 400;  // Para el tercer dron
    public int costoMejoraLista3Nivel3 = 600;  // Para el cuarto dron
    public int costoMejoraLista3Nivel4 = 800;  // Para el quinto dron

    [Header("Estado de Mejoras")]
    public int nivelMejoraLista1 = 0;
    public int nivelMejoraLista2 = 1; // Empieza con 1 dron
    public int nivelMejoraLista3 = 1; // Empieza con 1 dron

    private const int MAX_NIVEL_LISTA1 = 1;
    private const int MAX_NIVEL_LISTA2 = 3; // Máximo 3 drones
    private const int MAX_NIVEL_LISTA3 = 5; // Máximo 5 drones

    [Header("UI")]
    public TextMeshProUGUI dinero;
    public TextMeshProUGUI buscador;
    public TextMeshProUGUI ataque;
    public TextMeshProUGUI defensor;

    void Start()
    {
        if (sistemaDrones == null)
        {
            sistemaDrones = FindFirstObjectByType<Drones>();
            if (sistemaDrones == null)
            {
                Debug.LogError("No se encontró el sistema de Drones en la escena");
            }
        }

        ConfigurarDronesIniciales();
    }

    private void ConfigurarDronesIniciales()
    {
        // Desactivar todos los drones de las listas 2 y 3
        DesactivarTodosLosDrones();

        // Activar solo los drones comprados
        ActivarDronesSegunNivel();
    }

    private void DesactivarTodosLosDrones()
    {
        // Lista 2
        foreach (Transform drone in sistemaDrones.dronesLista2)
        {
            if (drone != null)
                drone.gameObject.SetActive(false);
        }

        // Lista 3
        foreach (Transform drone in sistemaDrones.dronesLista3)
        {
            if (drone != null)
                drone.gameObject.SetActive(false);
        }
    }

    private void ActivarDronesSegunNivel()
    {
        // Activar drones de Lista 2 según nivel
        for (int i = 0; i < nivelMejoraLista2 && i < sistemaDrones.dronesLista2.Count; i++)
        {
            if (sistemaDrones.dronesLista2[i] != null)
                sistemaDrones.dronesLista2[i].gameObject.SetActive(true);
        }

        // Activar drones de Lista 3 según nivel
        for (int i = 0; i < nivelMejoraLista3 && i < sistemaDrones.dronesLista3.Count; i++)
        {
            if (sistemaDrones.dronesLista3[i] != null)
                sistemaDrones.dronesLista3[i].gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        ActualizarUI();
    }

    private void ActualizarUI()
    {
        dinero.text = "Dinero: " + dineroActual;
        buscador.text = "Nivel Buscador: " ;
        ataque.text = "Atacantes LV: " + nivelMejoraLista2;
        defensor.text = "Defensores LV: " + nivelMejoraLista3;
    }

    /// Agrega dinero al jugador    
    public void AgregarDinero(int cantidad)
    {
        dineroActual += cantidad;
        Debug.Log($"Dinero agregado: {cantidad}. Total: {dineroActual}");
    }
        
    /// Mejora los drones de la Lista 1 (Buscadores)
    public bool MejorarLista1()
    {
        if (nivelMejoraLista1 >= MAX_NIVEL_LISTA1)
        {
            Debug.Log("Lista 1 ya está al máximo nivel");
            return false;
        }

        if (dineroActual < costoMejoraLista1)
        {
            Debug.Log($"Dinero insuficiente. Necesitas {costoMejoraLista1}, tienes {dineroActual}");
            return false;
        }

        // Restar dinero
        dineroActual -= costoMejoraLista1;
        nivelMejoraLista1++;

        Debug.Log($"¡Lista 1 mejorada al nivel {nivelMejoraLista1}!");

        // Aquí irá la lógica de mejora cuando decidas qué mejorar
        // Por ahora está vacía
        AplicarMejoraLista1();

        return true;
    }

    /// Mejora los drones de la Lista 2 (Atacantes) - Aumenta cantidad de drones
    public void MejorarLista2()
    {
        if (nivelMejoraLista2 >= MAX_NIVEL_LISTA2)
        {
            Debug.Log("Lista 2 ya está al máximo nivel (3 drones)");
            return;
        }

        int costoActual = ObtenerCostoMejoraLista2();

        if (dineroActual < costoActual)
        {
            Debug.Log($"Dinero insuficiente. Necesitas {costoActual}, tienes {dineroActual}");
            return;
        }

        // Restar dinero
        dineroActual -= costoActual;
        nivelMejoraLista2++;

        Debug.Log($"¡Lista 2 mejorada! Ahora tienes {nivelMejoraLista2} drones atacantes");

        // Activar el siguiente dron en la lista
        AplicarMejoraLista2();
    }

    /// Mejora los drones de la Lista 3 (Defensores) - Aumenta cantidad de drones
    public void MejorarLista3()
    {
        if (nivelMejoraLista3 >= MAX_NIVEL_LISTA3)
        {
            Debug.Log("Lista 3 ya está al máximo nivel (5 drones)");
            return;
        }

        int costoActual = ObtenerCostoMejoraLista3();

        if (dineroActual < costoActual)
        {
            Debug.Log($"Dinero insuficiente. Necesitas {costoActual}, tienes {dineroActual}");
            return;
        }

        // Restar dinero
        dineroActual -= costoActual;
        nivelMejoraLista3++;

        Debug.Log($"¡Lista 3 mejorada! Ahora tienes {nivelMejoraLista3} drones defensores");

        // Activar el siguiente dron en la lista
        AplicarMejoraLista3();
    }


    /// Obtiene el costo de la siguiente mejora para la Lista 2
    public int ObtenerCostoMejoraLista2()
    {
        switch (nivelMejoraLista2)
        {
            case 1: return costoMejoraLista2Nivel1; // Para obtener el 2do dron
            case 2: return costoMejoraLista2Nivel2; // Para obtener el 3er dron
            default: return 0;
        }
    }

    /// Obtiene el costo de la siguiente mejora para la Lista 3
    public int ObtenerCostoMejoraLista3()
    {
        switch (nivelMejoraLista3)
        {
            case 1: return costoMejoraLista3Nivel1; // Para obtener el 2do dron
            case 2: return costoMejoraLista3Nivel2; // Para obtener el 3er dron
            case 3: return costoMejoraLista3Nivel3; // Para obtener el 4to dron
            case 4: return costoMejoraLista3Nivel4; // Para obtener el 5to dron
            default: return 0;
        }
    }

    /// Verifica si se puede comprar una mejora específica
    public bool PuedeComprarMejora(int numeroLista)
    {
        switch (numeroLista)
        {
            case 1:
                return nivelMejoraLista1 < MAX_NIVEL_LISTA1 && dineroActual >= costoMejoraLista1;
            case 2:
                return nivelMejoraLista2 < MAX_NIVEL_LISTA2 && dineroActual >= ObtenerCostoMejoraLista2();
            case 3:
                return nivelMejoraLista3 < MAX_NIVEL_LISTA3 && dineroActual >= ObtenerCostoMejoraLista3();
            default:
                return false;
        }
    }

    /// Aplica la mejora a la Lista 1 (función vacía por ahora)
    private void AplicarMejoraLista1()
    {
        // Esta función está vacía intencionalmente
        // Aquí irá la lógica para el dron buscador   
    }
        
    /// Aplica la mejora a la Lista 2 - Activa drones adicionales
    private void AplicarMejoraLista2()
    {
        if (sistemaDrones == null) return;

        // Activar el dron recién comprado (índice = nivel - 1)
        int indiceDronNuevo = nivelMejoraLista2 - 1;

        if (indiceDronNuevo < sistemaDrones.dronesLista2.Count &&
            sistemaDrones.dronesLista2[indiceDronNuevo] != null)
        {
            sistemaDrones.dronesLista2[indiceDronNuevo].gameObject.SetActive(true);

            // Si la lista 2 está activa, forzar actualización
            if (sistemaDrones.listaActiva == 1)
            {
                sistemaDrones.CambiarListaActiva(1);
            }
        }
    }

    /// Aplica la mejora a la Lista 3 - Activa drones adicionales
    private void AplicarMejoraLista3()
    {
        if (sistemaDrones == null) return;

        // Activar el dron recién comprado (índice = nivel - 1)
        int indiceDronNuevo = nivelMejoraLista3 - 1;

        if (indiceDronNuevo < sistemaDrones.dronesLista3.Count &&
            sistemaDrones.dronesLista3[indiceDronNuevo] != null)
        {
            sistemaDrones.dronesLista3[indiceDronNuevo].gameObject.SetActive(true);

            // Si la lista 3 está activa, forzar actualización
            if (sistemaDrones.listaActiva == 2)
            {
                sistemaDrones.CambiarListaActiva(2);
            }
        }
    }

    public int ObtenerMaxDronesLista(int numeroLista)
    {
        switch (numeroLista)
        {
            case 1: return sistemaDrones.dronesLista1.Count; // Lista 1 siempre completa
            case 2: return nivelMejoraLista2;
            case 3: return nivelMejoraLista3;
            default: return 0;
        }
    }

    public void AplicarLimiteDronesAListaActiva(int numeroLista)
    {
        if (numeroLista == 1)
        {
            // Lista 2: Limitar a los drones comprados
            for (int i = 0; i < sistemaDrones.dronesLista2.Count; i++)
            {
                if (sistemaDrones.dronesLista2[i] != null)
                {
                    sistemaDrones.dronesLista2[i].gameObject.SetActive(i < nivelMejoraLista2);
                }
            }
        }
        else if (numeroLista == 2)
        {
            // Lista 3: Limitar a los drones comprados
            for (int i = 0; i < sistemaDrones.dronesLista3.Count; i++)
            {
                if (sistemaDrones.dronesLista3[i] != null)
                {
                    sistemaDrones.dronesLista3[i].gameObject.SetActive(i < nivelMejoraLista3);
                }
            }
        }
    }

    /// Obtiene información de estado para UI
    public string ObtenerInfoMejora(int numeroLista)
    {
        switch (numeroLista)
        {
            case 1:
                return $"Lista 1 - Nivel: {nivelMejoraLista1}/{MAX_NIVEL_LISTA1}\nCosto: {costoMejoraLista1}";
            case 2:
                return $"Lista 2 - Drones: {nivelMejoraLista2}/{MAX_NIVEL_LISTA2}\nCosto: {ObtenerCostoMejoraLista2()}";
            case 3:
                return $"Lista 3 - Drones: {nivelMejoraLista3}/{MAX_NIVEL_LISTA3}\nCosto: {ObtenerCostoMejoraLista3()}";
            default:
                return "Lista inválida";
        }
    }
}
