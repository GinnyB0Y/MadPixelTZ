using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Vector2Int PlacedOrigin { get; set; }
    public bool[,] Shape => shape;
    public bool IsPlaced { get; set; }
    public int RotationSteps => rotationSteps;
    
    [SerializeField] private int horizontalCells = 1;
    [SerializeField] private int verticalCells = 1;
    [SerializeField] private float rotateDuration;
    [SerializeField] private bool isSolid = true; // если true, то заполняется все ячейки между крайней по вертикали и горизонтали

    private bool[,] shape; // матрица формы предмета
    private RectTransform rectTransform;
    private GridInventory gridInventory;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private ItemStack itemStack;
    private Vector2Int shapeOffset;
    private Vector2 originalPosition;
    private Vector2 originalSizeDelta;
    private Vector2 originalPivot;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;
    private int dragSiblingIndex;
    private int rotationSteps;
    private bool wasInStack;
    private bool isDragging;
    private bool isRotating;


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();

        while (rootCanvas != null && !rootCanvas.isRootCanvas)
        {
            rootCanvas = rootCanvas.transform.parent.GetComponentInParent<Canvas>();
        }

        gridInventory = FindFirstObjectByType<GridInventory>();
        itemStack = FindFirstObjectByType<ItemStack>();
        BuildShape();
    }

    private void Update()
    {
        if (!isDragging) return;

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RotataItem();
            Vector2 screenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero; // я вообще не фанат тернарника, но решил показать, что умею пользоваться :)
            Vector2Int cursorCell = gridInventory.ScreenToGrid(screenPos);
            if (cursorCell.x >= 0)
            {
                Vector2Int origin = GetOriginFromCursor(cursorCell);
                gridInventory.HighlightCells(origin.x, origin.y, shape);
            }
            else
            {
                gridInventory.ClearHighlight();
            }
        }
    }

    private void BuildShape()
    {
        int w = Mathf.Max(1, horizontalCells);
        int h = Mathf.Max(1, verticalCells);

        shape = new bool[w, h];

        if (isSolid)
        {
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    shape[x, y] = true;
        }
        else
        {
            for (int y = 0; y < h; y++)
                shape[0, y] = true;

            for (int x = 0; x < w; x++)
                shape[x, 0] = true;
        }

        shapeOffset = new Vector2Int(w / 2, h / 2);
        rotationSteps = 0;
    }

    private void RotataItem()
    {
        if (isRotating) return;

        int oldW = shape.GetLength(0);
        int oldH = shape.GetLength(1);

        int newW = oldH;
        int newH = oldW;
        bool[,] newShape = new bool[newW, newH];

        for (int x = 0; x < oldW; x++)
        {
            for (int y = 0; y < oldH; y++)
            {
                newShape[y, oldW - 1 - x] = shape[x, y];
            }
        }

        shape = newShape;
        shapeOffset = new Vector2Int(newW / 2, newH / 2);

        float fromAngle = -90f * rotationSteps;
        rotationSteps = (rotationSteps + 1) % 4;
        float toAngle = -90f * rotationSteps;

        if (toAngle - fromAngle > 180f) toAngle -= 360f;
        if (toAngle - fromAngle < -180f) toAngle += 360f;

        StartCoroutine(RotateCoroutine(fromAngle, toAngle));
    }

    private IEnumerator RotateCoroutine(float fromAngle, float toAngle)
    {
        isRotating = true;

        float elapsed = 0f;
        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotateDuration);
            t = 1f - (1f - t) * (1f - t);
            float angle = Mathf.LerpUnclamped(fromAngle, toAngle, t);
            rectTransform.localEulerAngles = new Vector3(0, 0, angle);
            yield return null;
        }

        rectTransform.localEulerAngles = new Vector3(0, 0, toAngle);
        isRotating = false;
    }

    // это кастоманая настройка распознавания курсора относительно формы предмета, у всех настраивается по-своему
    private Vector2Int GetOriginFromCursor(Vector2Int cursorCell)
    {
        return new Vector2Int(cursorCell.x - shapeOffset.x, cursorCell.y - shapeOffset.y);
    }

    private Vector2 ScreenToCanvasLocal(Vector2 screenPos)
    {
        Camera cam;
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            cam = null;
        }
        else
        {
            cam = rootCanvas.worldCamera;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rootCanvas.transform as RectTransform, screenPos, cam, out Vector2 localPos);
        return localPos;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        wasInStack = itemStack != null && !IsPlaced;

        if (wasInStack)
        {
            itemStack.RemoveItem(this);
        }

        if (IsPlaced)
        {
            gridInventory.RemoveItem(this);
        }

        originalParent = rectTransform.parent;
        originalPosition = rectTransform.anchoredPosition;
        originalSizeDelta = rectTransform.sizeDelta;
        originalPivot = rectTransform.pivot;
        originalAnchorMin = rectTransform.anchorMin;
        originalAnchorMax = rectTransform.anchorMax;
        
        dragSiblingIndex = rectTransform.GetSiblingIndex();
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localEulerAngles = new Vector3(0, 0, -90f * rotationSteps);
        rectTransform.SetParent(rootCanvas.transform);
        rectTransform.SetAsLastSibling();
        rectTransform.localPosition = ScreenToCanvasLocal(eventData.position);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.75f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.localPosition = ScreenToCanvasLocal(eventData.position);
        Vector2Int cursorCell = gridInventory.ScreenToGrid(eventData.position);
        if (cursorCell.x >= 0)
        {
            Vector2Int origin = GetOriginFromCursor(cursorCell);
            gridInventory.HighlightCells(origin.x, origin.y, shape);
        }
        else
        {
            gridInventory.ClearHighlight();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        isRotating = false;
        StopAllCoroutines();
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        gridInventory.ClearHighlight();
        Vector2Int cursorCell = gridInventory.ScreenToGrid(eventData.position);
        bool placed = false;

        if (cursorCell.x >= 0)
        {
            Vector2Int origin = GetOriginFromCursor(cursorCell);
            placed = gridInventory.PlaceItem(origin.x, origin.y, this);
        }

        if (placed)
        {
        }
        else
        {
            if (itemStack != null)
            {
                if (itemStack.IsFull)
                {
                    Destroy(gameObject);
                }
                else
                {
                    rectTransform.localEulerAngles = Vector3.zero;
                    itemStack.AddItem(this);
                }
            }
            else
            {
                rectTransform.SetParent(originalParent);
                rectTransform.SetSiblingIndex(dragSiblingIndex);
                rectTransform.pivot = originalPivot;
                rectTransform.anchorMin = originalAnchorMin;
                rectTransform.anchorMax = originalAnchorMax;
                rectTransform.sizeDelta = originalSizeDelta;
                rectTransform.anchoredPosition = originalPosition;
                rectTransform.localEulerAngles = Vector3.zero;
            }
        }
    }
}