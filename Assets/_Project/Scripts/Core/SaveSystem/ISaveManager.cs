using UnityEngine;

namespace Sol
{
    public interface ISaveManager
    {
        void RequestLazySave();
        void RequestImmediateSave();
    }
}
