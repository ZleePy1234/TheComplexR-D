using UnityEngine;
using com.cyborgAssets.inspectorButtonPro;
[ExecuteAlways]
public class GridGeneration : MonoBehaviour
{
    [Range(1, 20)]
    public int gridX = 1;
    [Range(1, 20)]
    public int gridY = 1;

    public Vector3 offset = new(10, 0, 10);
    public GameObject tilePrefab;
    private int oldGridY;
    private int oldGridX;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        oldGridX = gridX;
        oldGridY = gridY;
        tilePrefab = Resources.Load<GameObject>("Placeholders/EmptyTile");

    }

    // Update is called once per frame
    void Update()
    {
        if (oldGridX != gridX || oldGridY != gridY)
        {
            GenerateGrid();
            oldGridX = gridX;
            oldGridY = gridY;
        }
    }

    [ProButton]
    void GenerateGrid()
    {
        // quitar viejo grid
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // checar tamaño total del grid considerando el offset
        float totalWidth = (gridX - 1) * offset.x;
        float totalHeight = (gridY - 1) * offset.z;

        // Centrar el grid pq sino se cagotean todos los pinches tiles mellevalaverga
        Vector3 startPos = transform.position - new Vector3(totalWidth / 2f, 0, totalHeight / 2f);

        for (int x = 0; x < gridX; x++)
        {
            for (int y = 0; y < gridY; y++)
            {
                Vector3 pos = startPos + new Vector3(x * offset.x, 0, y * offset.z);
                GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                tile.transform.position = pos;
            }
        }
    }
}
