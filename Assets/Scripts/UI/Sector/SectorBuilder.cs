using Structs;
using UnityEngine;

namespace UI.Sector
{
    public class SectorBuilder : ISectorBuilder 
    {
        public void Build(GameObject questSector, DataAchievement data)
        {
            var uiElements = new QuestSectorUIElements(questSector);
            uiElements.Configure(data);
        }
    }
}