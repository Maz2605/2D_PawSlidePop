using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View.Factory
{
    public readonly struct TileViewSpawnData
    {
        public Match3TileView Prefab { get; }
        public TileModel Tile { get; }
        public BoardContentDefinitionSO Definition { get; }
        public Vector3 LocalPosition { get; }
        public bool IsIdleEnabled { get; }
        public string Name { get; }

        public TileViewSpawnData(
            Match3TileView prefab,
            TileModel tile,
            BoardContentDefinitionSO definition,
            Vector3 localPosition,
            bool isIdleEnabled,
            string name)
        {
            Prefab = prefab;
            Tile = tile;
            Definition = definition;
            LocalPosition = localPosition;
            IsIdleEnabled = isIdleEnabled;
            Name = name;
        }
    }
}

