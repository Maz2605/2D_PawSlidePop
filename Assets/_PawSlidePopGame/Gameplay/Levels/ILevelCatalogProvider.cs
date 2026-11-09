using System.Collections.Generic;

namespace _PawSlidePopGame._Scripts.Data.LevelProvider
{
    public interface ILevelCatalogProvider
    {
        IReadOnlyList<string> GetLevelIds(string filter = null);
    }
}
