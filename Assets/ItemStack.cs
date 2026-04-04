using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemStack : MonoBehaviour, IItemSpawner
{
    public bool IsFull => stackedItems.Count >= maxFanItems;

    [SerializeField] private AnimationCurve flyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // для удобства можно настроить кривую полёта
    [SerializeField] private GameObject[] itemsForSpawn;
    [SerializeField] private Transform itemStackPoint;
    [SerializeField] private int maxFanItems; // это максимум предметов вне инвентаря
    [SerializeField] private float flyDuration;
    [SerializeField] private float fanSpread;
    [SerializeField] private float fanMaxAngle;
    [SerializeField] private float levitationAmplitude;
    [SerializeField] private float levitationSpeed;

    private readonly List<InventoryItem> stackedItems = new List<InventoryItem>();

    private bool CanSpawn()
    {
        if (stackedItems.Count >= maxFanItems)
        {
            Debug.Log("Максимум предметов");
            return false;
        }

        return true;
    }

    public InventoryItem SpawnItem(GameObject itemPrefab)
    {
        if (!CanSpawn())
            return null;

        GameObject go = Instantiate(itemPrefab, itemStackPoint);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.localEulerAngles = Vector3.zero;
        rt.localScale = Vector3.one;

        InventoryItem item = go.GetComponent<InventoryItem>();
        AddItem(item);
        return item;
    }

    public InventoryItem SpawnItem()
    {
        if (itemsForSpawn == null || itemsForSpawn.Length == 0)
        {
            return null;
        }

        if (!CanSpawn())
        {
            return null;
        }    

        GameObject prefab = itemsForSpawn[Random.Range(0, itemsForSpawn.Length)];
        GameObject go = Instantiate(prefab, itemStackPoint);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.localEulerAngles = Vector3.zero;
        InventoryItem item = go.GetComponent<InventoryItem>();
        AddItem(item);
        return item;
    }

    public void AddItem(InventoryItem item)
    {
        if (stackedItems.Contains(item))
            return;

        if (stackedItems.Count >= maxFanItems)
            return;

        stackedItems.Add(item);
        RectTransform itemRt = item.GetComponent<RectTransform>();
        itemRt.SetParent(itemStackPoint);
        itemRt.SetAsLastSibling();
        RebuildFan();
    }

    public void RemoveItem(InventoryItem item)
    {
        if (!stackedItems.Remove(item))
            return;

        item.GetComponent<RectTransform>().localEulerAngles = Vector3.zero;
        RebuildFan();
    }

    private void RebuildFan()
    {
        StopAllCoroutines();
        int count = stackedItems.Count;

        for (int i = 0; i < count; i++)
        {
            InventoryItem item = stackedItems[i];
            RectTransform rt = item.GetComponent<RectTransform>();

            float t;
            if (count > 1)
            {
                t = (float)i / (count - 1) - 0.5f;
            }
            else
            {
                t = 0f;
            }

            Vector2 fanTarget = new Vector2(t * fanSpread * 2f, 0f);
            float fanAngle = -t * fanMaxAngle * 2f;
            StartCoroutine(FlyThenLevitate(rt, fanTarget, fanAngle));
        }
    }

    private IEnumerator FlyThenLevitate(RectTransform rt, Vector2 targetPos, float targetAngle)
    {
        Vector2 startPos = rt.anchoredPosition;
        float startAngle = rt.localEulerAngles.z;
        if (startAngle > 180f) startAngle -= 360f;

        float elapsed = 0f;

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float p = flyCurve.Evaluate(Mathf.Clamp01(elapsed / flyDuration));

            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, p);
            rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(startAngle, targetAngle, p));

            yield return null;
        }

        rt.anchoredPosition = targetPos;
        rt.localEulerAngles = new Vector3(0f, 0f, targetAngle);

        float phase = Random.Range(0f, Mathf.PI * 2f);

        while (true)
        {
            float offsetY = Mathf.Sin(Time.time * levitationSpeed + phase) * levitationAmplitude;
            rt.anchoredPosition = targetPos + new Vector2(0f, offsetY);
            yield return null;
        }
    }

    public void SpawnItemUI()
    {
        SpawnItem();
    }
}