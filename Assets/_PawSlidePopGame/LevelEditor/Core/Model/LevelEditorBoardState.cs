using System;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model
{
    [Serializable]
    public sealed class LevelEditorBoardState
    {
        public int width;
        public int height;
        public int[] tileLayout = Array.Empty<int>();
        public int[] overlayLayout = Array.Empty<int>();
        public int[] underlayLayout = Array.Empty<int>();
        public bool[] playableMask = Array.Empty<bool>();

        public int CellCount => Math.Max(0, width) * Math.Max(0, height);

        public void Initialize(int width, int height)
        {
            this.width = Math.Max(1, width);
            this.height = Math.Max(1, height);

            int cellCount = CellCount;
            tileLayout = new int[cellCount];
            overlayLayout = new int[cellCount];
            underlayLayout = new int[cellCount];
            playableMask = new bool[cellCount];
            for (int i = 0; i < playableMask.Length; i++)
            {
                playableMask[i] = true;
            }
        }

        public int ToIndex(int x, int y)
        {
            return (y * width) + x;
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < width && y < height;
        }

        public LevelEditorCellState GetCell(int x, int y)
        {
            int index = ToIndex(x, y);
            return new LevelEditorCellState(new LevelEditorCoordinate(x, y), tileLayout[index], overlayLayout[index], underlayLayout[index], playableMask[index]);
        }

        public void SetTileId(int x, int y, int tileId)
        {
            if (!IsInBounds(x, y))
            {
                return;
            }

            tileLayout[ToIndex(x, y)] = Mathf.Max(0, tileId);
        }

        public void SetOverlayId(int x, int y, int overlayId)
        {
            if (!IsInBounds(x, y))
            {
                return;
            }

            overlayLayout[ToIndex(x, y)] = Mathf.Max(0, overlayId);
        }

        public void SetUnderlayId(int x, int y, int underlayId)
        {
            if (!IsInBounds(x, y))
            {
                return;
            }

            underlayLayout[ToIndex(x, y)] = Mathf.Max(0, underlayId);
        }

        public void SetPlayable(int x, int y, bool playable)
        {
            if (!IsInBounds(x, y))
            {
                return;
            }

            playableMask[ToIndex(x, y)] = playable;
        }

        public void ClearCell(int x, int y, bool playable = false)
        {
            if (!IsInBounds(x, y))
            {
                return;
            }

            int index = ToIndex(x, y);
            tileLayout[index] = 0;
            overlayLayout[index] = 0;
            underlayLayout[index] = 0;
            playableMask[index] = playable;
        }

        public LevelEditorBoardState Clone()
        {
            return new LevelEditorBoardState
            {
                width = width,
                height = height,
                tileLayout = (int[])tileLayout.Clone(),
                overlayLayout = (int[])overlayLayout.Clone(),
                underlayLayout = (int[])underlayLayout.Clone(),
                playableMask = (bool[])playableMask.Clone()
            };
        }
    }
}
