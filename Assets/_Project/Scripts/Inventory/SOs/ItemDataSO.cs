using UnityEngine;

namespace Sol
{
    [CreateAssetMenu(fileName = "New Item", menuName = "Sol/Item Data/Item")]
    public class ItemDataSO : ScriptableObject
    {
        [Header("Identity")] 
        string _itemId;
        string _displayName;
        string _description;
        ItemCategory _itemCategory;
        
        [Header("Inventory")]
        float _weight;
        int _maxStackSize; //default is 1
        Vector2Int[] _gridShape; // set in the inspector, relative to (0,0) top-left anchor
        
        [Header("Visual)")]
        Sprite _icon;
    }
}


