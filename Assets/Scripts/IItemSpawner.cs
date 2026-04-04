// интерфейс для спавна предметов в точке сбора.
public interface IItemSpawner
{
    InventoryItem SpawnItem();
    void SpawnItemUI();
}