using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// —истема координат: (0,0) = нижний левый угол, X Ч вправо, Y Ч вверх.
public class GridInventory : MonoBehaviour, IExpandInventory
{
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Transform cellsParent;
    [SerializeField] private int columns; // столбцы
    [SerializeField] private int rows; // строки
    [SerializeField] private float cellSize;
    [SerializeField] private float cellSpacing;

    private Camera canvasCamera;
    private InventoryCell[,] cells;
    private string[] shapeRows;
    private bool[,] occupied;
    private bool[,] activeMap;

    private const int maxExpansions = 3;
    private int expansionCount = 0;

    public int Columns => columns;
    public int Rows => rows;
    public float CellSize => cellSize;
    public float CellSpacing => cellSpacing;

    private void Awake()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Canvas root = canvas.rootCanvas;
            if (root.renderMode != RenderMode.ScreenSpaceOverlay)
                canvasCamera = root.worldCamera;
        }

        BuildActiveMap();
        BuildGrid();
    }

    private void BuildActiveMap()
    {
        activeMap = new bool[columns, rows];
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                if (shapeRows != null && shapeRows.Length > y && shapeRows[y].Length > x)
                {
                    activeMap[x, y] = shapeRows[y][x] == '1';
                }
                else
                {
                    activeMap[x, y] = true;
                }
            }
        }
    }

    private void BuildGrid()
    {
        cells = new InventoryCell[columns, rows];
        occupied = new bool[columns, rows];

        float step = cellSize + cellSpacing;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                GameObject cellGo = Instantiate(cellPrefab, cellsParent);
                cellGo.name = $"Cell_{x}_{y}";

                RectTransform rt = cellGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.zero;
                rt.pivot = Vector2.zero;
                rt.sizeDelta = new Vector2(cellSize, cellSize);
                rt.anchoredPosition = new Vector2(x * step, y * step);
                InventoryCell cell = cellGo.GetComponent<InventoryCell>();
                
                if (cell == null)
                {
                    cell = cellGo.AddComponent<InventoryCell>();
                }

                cell.Init(x, y, this);
                cells[x, y] = cell;

                if (!activeMap[x, y])
                {
                    cellGo.SetActive(false);
                }
            }
        }
    }

    public bool CanPlace(int originX, int originY, bool[,] shape)
    {
        int shapeW = shape.GetLength(0);
        int shapeH = shape.GetLength(1);

        for (int sx = 0; sx < shapeW; sx++)
        {
            for (int sy = 0; sy < shapeH; sy++)
            {
                if (!shape[sx, sy]) continue;

                int gx = originX + sx;
                int gy = originY + sy;

                if (gx < 0 || gx >= columns || gy < 0 || gy >= rows)
                    return false;
                if (!activeMap[gx, gy])
                    return false;
                if (occupied[gx, gy])
                    return false;
            }
        }
        return true;
    }

    // логика размещени€ предметов
    public bool PlaceItem(int originX, int originY, InventoryItem item)
    {
        if (!CanPlace(originX, originY, item.Shape))
            return false;

        int shapeW = item.Shape.GetLength(0);
        int shapeH = item.Shape.GetLength(1);

        for (int sx = 0; sx < shapeW; sx++)
        {
            for (int sy = 0; sy < shapeH; sy++)
            {
                if (!item.Shape[sx, sy]) continue;
                occupied[originX + sx, originY + sy] = true;
            }
        }

        SnapItemToGrid(originX, originY, item);
        item.PlacedOrigin = new Vector2Int(originX, originY);
        item.IsPlaced = true;
        return true;
    }

    public void RemoveItem(InventoryItem item)
    {
        if (!item.IsPlaced) return;

        int ox = item.PlacedOrigin.x;
        int oy = item.PlacedOrigin.y;
        int shapeW = item.Shape.GetLength(0);
        int shapeH = item.Shape.GetLength(1);

        for (int sx = 0; sx < shapeW; sx++)
        {
            for (int sy = 0; sy < shapeH; sy++)
            {
                if (!item.Shape[sx, sy]) continue;
                occupied[ox + sx, oy + sy] = false;
            }
        }

        item.IsPlaced = false;
    }

    // логика прив€зки предмета к сетке
    private void SnapItemToGrid(int originX, int originY, InventoryItem item)
    {
        RectTransform itemRt = item.GetComponent<RectTransform>();
        bool[,] shape = item.Shape;
        int shapeW = shape.GetLength(0);
        int shapeH = shape.GetLength(1);

        int minX = shapeW, maxX = 0, minY = shapeH, maxY = 0;
        for (int sx = 0; sx < shapeW; sx++)
        {
            for (int sy = 0; sy < shapeH; sy++)
            {
                if (!shape[sx, sy]) continue;
                if (sx < minX) minX = sx;
                if (sx > maxX) maxX = sx;
                if (sy < minY) minY = sy;
                if (sy > maxY) maxY = sy;
            }
        }

        float step = cellSize + cellSpacing;
        float posX = (originX + minX) * step;
        float posY = (originY + minY) * step;

        float width = (maxX - minX + 1) * cellSize + (maxX - minX) * cellSpacing;
        float height = (maxY - minY + 1) * cellSize + (maxY - minY) * cellSpacing;

        float centerX = posX + width * 0.5f;
        float centerY = posY + height * 0.5f;

        itemRt.SetParent(cellsParent);
        itemRt.anchorMin = Vector2.zero;
        itemRt.anchorMax = Vector2.zero;
        itemRt.pivot = new Vector2(0.5f, 0.5f);
        itemRt.anchoredPosition = new Vector2(centerX, centerY);
        itemRt.localEulerAngles = new Vector3(0, 0, -90f * item.RotationSteps);
    }

    public void HighlightCells(int originX, int originY, bool[,] shape)
    {
        ClearHighlight();

        bool canPlace = CanPlace(originX, originY, shape);
        Color color;
        if (canPlace)
        {
            color = Color.green;
        }
        else
        {
            color = Color.red;
        }

        int shapeW = shape.GetLength(0);
        int shapeH = shape.GetLength(1);

        for (int sx = 0; sx < shapeW; sx++)
        {
            for (int sy = 0; sy < shapeH; sy++)
            {
                if (!shape[sx, sy]) continue;

                int gx = originX + sx;
                int gy = originY + sy;

                if (gx >= 0 && gx < columns && gy >= 0 && gy < rows && activeMap[gx, gy])
                {
                    cells[gx, gy].SetHighlight(color);
                }
            }
        }
    }

    public void ClearHighlight()
    {
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (activeMap[x, y])
                    cells[x, y].ClearHighlight();
            }
        }
    }

    public Vector2Int ScreenToGrid(Vector2 screenPos)
    {
        RectTransform rt = cellsParent.GetComponent<RectTransform>();
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, canvasCamera, out localPos);
        float step = cellSize + cellSpacing;

        int x = Mathf.FloorToInt((localPos.x + cellSize * 0.5f) / step);
        int y = Mathf.FloorToInt((localPos.y + cellSize * 0.5f) / step);
        x = Mathf.Clamp(x, 0, columns - 1);
        y = Mathf.Clamp(y, 0, rows - 1);

        float gridWidth = columns * step - cellSpacing;
        float gridHeight = rows * step - cellSpacing;

        if (localPos.x < -cellSpacing * 0.5f || localPos.x > gridWidth + cellSpacing * 0.5f ||
            localPos.y < -cellSpacing * 0.5f || localPos.y > gridHeight + cellSpacing * 0.5f)
        {
            return new Vector2Int(-1, -1);
        }

        return new Vector2Int(x, y);
    }

    // тут логика расширени€ инвентар€. ƒобавл€ет 2-5 новых €чеек. ƒл€ тз поставил 3 расширени€
    public bool ExpandRows()
    {
        if (expansionCount >= maxExpansions)
        {
            Debug.Log("”величений больше нет");
            return false;
        }

        int cellsToAdd = Random.Range(2, 6);
        int cursor = FindNextExpandPosition();

        float step = cellSize + cellSpacing;

        for (int i = 0; i < cellsToAdd; i++)
        {
            int x = cursor % columns;
            int y = cursor / columns;

            if (y >= rows)
            {
                GrowByOneRow();
            }

            if (cells[x, y] != null)
            {
                cells[x, y].gameObject.SetActive(true);
                activeMap[x, y] = true;
            }
            else
            {
                GameObject cellGo = Instantiate(cellPrefab, cellsParent);
                cellGo.name = $"Cell_{x}_{y}";

                RectTransform rt = cellGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.zero;
                rt.pivot = Vector2.zero;
                rt.sizeDelta = new Vector2(cellSize, cellSize);
                rt.anchoredPosition = new Vector2(x * step, y * step);

                InventoryCell cell = cellGo.GetComponent<InventoryCell>();
                if (cell == null)
                {
                    cell = cellGo.AddComponent<InventoryCell>();
                }

                cell.Init(x, y, this);
                cells[x, y] = cell;
                occupied[x, y] = false;
                activeMap[x, y] = true;
            }

            cursor++;
        }

        expansionCount++;
        Debug.Log($"ƒобалвено {cellsToAdd} €чеек");
        return true;
    }

    private int FindNextExpandPosition()
    {
        for (int y = rows - 1; y >= 0; y--)
        {
            for (int x = columns - 1; x >= 0; x--)
            {
                if (activeMap[x, y])
                    return y * columns + x + 1;
            }
        }
        return 0;
    }

    private void GrowByOneRow()
    {
        int newRow = rows;
        int newRows = rows + 1;

        InventoryCell[,] newCellsArr = new InventoryCell[columns, newRows];
        bool[,] newOccupied = new bool[columns, newRows];
        bool[,] newActiveMap = new bool[columns, newRows];

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                newCellsArr[x, y] = cells[x, y];
                newOccupied[x, y] = occupied[x, y];
                newActiveMap[x, y] = activeMap[x, y];
            }
        }

        float step = cellSize + cellSpacing;

        for (int x = 0; x < columns; x++)
        {
            GameObject cellGo = Instantiate(cellPrefab, cellsParent);
            cellGo.name = $"Cell_{x}_{newRow}";

            RectTransform rt = cellGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(cellSize, cellSize);
            rt.anchoredPosition = new Vector2(x * step, newRow * step);

            InventoryCell cell = cellGo.GetComponent<InventoryCell>();
            if (cell == null)
                cell = cellGo.AddComponent<InventoryCell>();

            cell.Init(x, newRow, this);
            newCellsArr[x, newRow] = cell;
            newOccupied[x, newRow] = false;
            newActiveMap[x, newRow] = false;

            cellGo.SetActive(false);
        }

        cells = newCellsArr;
        occupied = newOccupied;
        activeMap = newActiveMap;
        rows = newRows;
    }

    public void ExpandFromUI()
    {
        ExpandRows();
    }
}