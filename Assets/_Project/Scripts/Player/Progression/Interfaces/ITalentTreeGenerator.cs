using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    public interface ITalentTreeGenerator
    {
        void Generate();
        
        IReadOnlyDictionary<string, TalentTreeGenerator.NodeInstance> GetNodeRegistry();
    }
}

