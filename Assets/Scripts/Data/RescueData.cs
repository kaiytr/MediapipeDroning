using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRescueData", menuName = "Data/Rescue Data")]
public class RescueData : ScriptableObject
{
    public string structureName;                                 // 구조 이름
    public float goldenTime;                                     // 골든 타임 (초)
    public Vector3 rescuerPosition;                              // 구조자 위치
    public List<ItemData> requiredItems = new List<ItemData>();  // 필요한 아이템 목록 (ItemData 에셋 할당)
    public List<string> locationHints = new List<string>();      // 힌트 목록
}