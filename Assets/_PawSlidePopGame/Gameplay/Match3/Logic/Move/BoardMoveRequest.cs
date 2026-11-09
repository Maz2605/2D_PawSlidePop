using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move
{
    public readonly struct BoardMoveRequest
    {
        public MoveAxis Axis { get; }
        public int LineIndex { get; }
        public LineSlideDirection Direction { get; }
        public int SourceX { get; }
        public int SourceY { get; }
        public bool HasSource => SourceX >= 0 && SourceY >= 0;

        public int GetRotationStep()
        {
            switch (Direction)
            {
                case LineSlideDirection.Right:
                case LineSlideDirection.Down:
                    return 1;
                case LineSlideDirection.Left:
                case LineSlideDirection.Up:
                    return -1;
                default:
                    return 0;
            }
        }
        public BoardMoveRequest(MoveAxis axis, int lineIndex, LineSlideDirection direction)
            : this(axis, lineIndex, direction, -1, -1)
        {
        }

        public BoardMoveRequest(MoveAxis axis, int lineIndex, LineSlideDirection direction, int sourceX, int sourceY)
        {
            Axis = axis;
            LineIndex = lineIndex;
            Direction = direction;
            SourceX = sourceX;
            SourceY = sourceY;
        }

        public bool IsValid()
        {
            return Axis == MoveAxis.Row
                ? Direction == LineSlideDirection.Left || Direction == LineSlideDirection.Right
                : Direction == LineSlideDirection.Up || Direction == LineSlideDirection.Down;
        }

        
    }
}

