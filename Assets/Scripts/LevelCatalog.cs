using System.Collections.Generic;
using UnityEngine;

// The ordered list of levels the game plays through.
[CreateAssetMenu(fileName = "LevelCatalog", menuName = "Fuck Gravity/Level Catalog")]
public class LevelCatalog : ScriptableObject
{
    public List<LevelData> levels = new List<LevelData>();
}
