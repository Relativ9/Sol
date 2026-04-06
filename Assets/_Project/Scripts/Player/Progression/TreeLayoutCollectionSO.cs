using UnityEngine;
using System.Collections.Generic;

namespace Sol
{
    [CreateAssetMenu(fileName = "TreeCollection", menuName = "Talent System/Tree Layout Collection")]
    public class TreeLayoutCollectionSO : ScriptableObject
    {
        public List<TreeLayoutSO> trees = new();
    }
}
