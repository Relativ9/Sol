using UnityEngine;

namespace Sol
{
    public class TalentNodeTooltipAdapter : ITooltipContent
    {
        private readonly TalentNodeDataSO _data;
        
        public TalentNodeTooltipAdapter(TalentNodeDataSO data)
        {
            if (data == null)
                throw new System.ArgumentNullException(nameof(data), 
                    "TalentNodeTooltipAdapter requires a non-null TalentNodeData.");
            
            _data = data;   
        }

        public string Title => _data.displayName;
        public string Description => _data.description;
        public string SubText => _data.isActiveSkill ? $"Active | Max Points : {_data.maxPoints}" : $"Passive | Max Points: {_data.maxPoints}";
        public Sprite Icon => _data.icon;
        public Color IconColor => _data.parentTree != null ? _data.parentTree.treeColor : Color.white;
    }
}
