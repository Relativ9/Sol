using UnityEngine;

namespace Sol
{
    [CreateAssetMenu(fileName = "PlayerProgression", menuName = "Player/PlayerProgression")]
    public class PlayerProgressionSO : ScriptableObject
    {
        [Header("Starting Values")]
        public int startingLevel = 1;
        
        public int talentPointsPerLevel = 1;
        public int startingTalentPoints = 4;

        public int attributePointsPerLevel = 3;
        public int startingAttributePoints = 3;
    }   

}
