using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View.Factory
{
    public readonly struct CellViewSpawnData
    {
        public Match3CellView Prefab { get; }
        public CellModel Cell { get; }
        public Vector3 LocalPosition { get; }
        public string Name { get; }

        public CellViewSpawnData(Match3CellView prefab, CellModel cell, Vector3 localPosition, string name)
        {
            Prefab = prefab;
            Cell = cell;
            LocalPosition = localPosition;
            Name = name;
        }
    }
}

