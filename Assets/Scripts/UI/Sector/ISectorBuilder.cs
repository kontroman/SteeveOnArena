using Quest;
using UnityEngine;

namespace UI.Sector
{
    public interface ISectorBuilder
    {
        void Build(GameObject questSector, DataQuest data);
    }
}