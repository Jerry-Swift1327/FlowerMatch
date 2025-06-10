using UnityEngine;

namespace Match3
{
    public class GamePiece : MonoBehaviour
    {
        public int score;

        private int _x;
        private int _y;
        public int X
        {
            get => _x;
            set { if (IsMovable()) { _x = value; } }
        }
        public int Y
        {
            get => _y;
            set { if (IsMovable()) { _y = value; } }
        }

        private PieceType _pieceType;
        public  ObstacleType _obstacleType;

        public bool isInitial { get; set; }

        public PieceType PieceType => _pieceType;
        public ObstacleType ObstacleType => _obstacleType;

        private GameGrid _gameGrid;

        public GameGrid GameGridRef => _gameGrid;

        private MovablePiece _movableComponent;

        public MovablePiece MovableComponent => _movableComponent;

        private ColorPiece _colorComponent;

        public ColorPiece ColorComponent => _colorComponent;

        private ClearablePiece _clearableComponent;

        public ClearablePiece ClearableComponent => _clearableComponent;

        private void Awake()
        {
            _movableComponent = GetComponent<MovablePiece>();
            _colorComponent = GetComponent<ColorPiece>();
            _clearableComponent = GetComponent<ClearablePiece>();
        }

        public void Init(int x, int y, GameGrid gameGrid, PieceType pieceType, ObstacleType obstacleType=ObstacleType.Bubble)
        {
            _x = x;
            _y = y;
            _gameGrid = gameGrid;
            _pieceType = pieceType;
            _obstacleType = obstacleType;
            transform.position = gameGrid.GetWorldPosition(x, y); //（可选）
        }

        private void OnMouseDown()
        {
            if (GameCanvasManager.Instance.IsInputBlocked) return;
            if (this == null || _gameGrid == null) return;
            _gameGrid.PressPiece(this);
        }

        private void OnMouseEnter()
        {
            if (GameCanvasManager.Instance.IsInputBlocked) return;
            if (this == null || _gameGrid == null) return;
            _gameGrid.EnterPiece(this);
        }

        private void OnMouseUp()
        {
            if (GameCanvasManager.Instance.IsInputBlocked) return;
            if (this == null || _gameGrid == null) return;
            _gameGrid.ReleasePiece();
        }

        public bool IsMovable() => _movableComponent != null && !isInitial && PieceType != PieceType.Unfillable;

        public bool IsColored() => _colorComponent != null;

        public bool IsClearable() => _clearableComponent != null;
    }
}