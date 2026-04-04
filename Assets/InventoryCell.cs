using UnityEngine;
using UnityEngine.UI;

public class InventoryCell : MonoBehaviour
{
    public int GridX => gridX;
    public int GridY => gridY;

    private GridInventory inventory;
    private Image image;
    private Color defaultColor;
    private int gridX;
    private int gridY;

    public void Init(int x, int y, GridInventory inventory)
    {
        gridX = x;
        gridY = y;
        this.inventory = inventory;
        image = GetComponent<Image>();
        defaultColor = image.color;
    }

    public Vector2 GetCenterPosition()
    {
        RectTransform rt = GetComponent<RectTransform>();
        return rt.anchoredPosition + new Vector2(inventory.CellSize * 0.5f, inventory.CellSize * 0.5f);
    }

    public void SetHighlight(Color color)
    {
        image.color = color;
    }

    public void ClearHighlight()
    {
        image.color = defaultColor;
    }
}