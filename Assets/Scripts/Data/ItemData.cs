using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Data/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;      // 아이템 이름
    public float itemWeight;     // 아이템 무게
}