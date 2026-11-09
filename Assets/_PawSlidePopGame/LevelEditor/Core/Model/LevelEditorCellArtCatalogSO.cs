using System.Collections.Generic;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model
{
    [CreateAssetMenu(fileName = "LevelEditorCellArtCatalog", menuName = "_PawSlidePopGame/LevelEditor/Cell Art Catalog")]
    public sealed class LevelEditorCellArtCatalogSO : ScriptableObject
    {
        [SerializeField] private List<LevelEditorCellArtDefinition> entries = new List<LevelEditorCellArtDefinition>();

        public IReadOnlyList<LevelEditorCellArtDefinition> Entries => entries;

        public LevelEditorCellArtDefinition GetEntry(int id)
        {
            if (id <= 0)
            {
                return null;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                LevelEditorCellArtDefinition entry = entries[i];
                if (entry != null && entry.Id == id)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
