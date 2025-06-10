using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    public class GameGrid : MonoBehaviour
    {
        public enum SpecialCombinationType
        {
            Cross,
            DoubleRow,
            DoubleColumn,
            DoubleSquare,
            SquareRow,
            SquareColumn,
            RainbowRow,
            RainbowColumn,
            RainbowSquare
        }

        [System.Serializable]
        public struct PiecePrefab
        {
            public PieceType type;
            public GameObject prefab;
        };

        [System.Serializable]
        public struct ObstaclePrefab
        {
            public ObstacleType type;
            public GameObject prefab;
        }

        [System.Serializable]
        public struct InitialPiece
        {
            public PieceType pieceType;
            public ObstacleType obstacleType;
            public int x;
            public int y;
        };

        public static GameGrid Instance;
        [Header("关卡脚本")] public Level level;

        [Header("网格尺寸")]
        public int xDim;
        public int yDim;
        public GamePiece[,] _pieces;

        [Header("功能块预制体")] public PiecePrefab[] piecePrefabs;
        [Header("障碍物预制体")] public ObstaclePrefab[] obstaclePrefabs;
        [Header("背景预制体")] public GameObject backgroundPrefab;

        protected Dictionary<PieceType, GameObject> _piecePrefabDict;
        protected Dictionary<ObstacleType, GameObject> _obstaclePrefabDict;

        [Header("填充时间")] public float fillTime;

        [Header("洗牌配置")]
        private int maxShuffleAttempts = 3;
        private int currentShuffleAttempts;
        private bool isShuffling;

        [Header("音频系统")]
        public AudioSource audioSource;
        public AudioClip clearSound;
        public AudioClip combineSound;
        public AudioClip unmatchSound;
        public AudioClip fillSound;
        public AudioClip shuffleSound;
        public AudioClip hintSound;

        [Header("初始块")] public InitialPiece[] initialPieces;

        protected bool _inverse;
        protected GamePiece _pressedPiece;
        protected GamePiece _enteredPiece;

        [HideInInspector] public bool _gameOver;

        public bool IsFilling { get; private set; }
        protected bool IsSmallGrid => Mathf.Min(xDim, yDim) < 4;

        protected void Awake()
        {
            Instance = this;

            if (!GetComponent<AudioSource>())
                audioSource = gameObject.AddComponent<AudioSource>();
            else audioSource = GetComponent<AudioSource>();

            currentShuffleAttempts = maxShuffleAttempts;

            _piecePrefabDict = new Dictionary<PieceType, GameObject>();
            for (int i = 0; i < piecePrefabs.Length; i++)
            {
                if (!_piecePrefabDict.ContainsKey(piecePrefabs[i].type))
                {
                    _piecePrefabDict.Add(piecePrefabs[i].type, piecePrefabs[i].prefab);
                }
            }

            _obstaclePrefabDict = new Dictionary<ObstacleType, GameObject>();
            for (int i = 0; i < obstaclePrefabs.Length; i++)
            {
                if (!_obstaclePrefabDict.ContainsKey(obstaclePrefabs[i].type))
                {
                    _obstaclePrefabDict.Add(obstaclePrefabs[i].type, obstaclePrefabs[i].prefab);
                }
            }

            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    GameObject background = Instantiate(backgroundPrefab,
                    GetWorldPosition(x, y), Quaternion.identity);
                    background.transform.parent = transform;
                }
            }

            _pieces = new GamePiece[xDim, yDim];

            for (int i = 0; i < initialPieces.Length; i++)
            {
                InitialPiece initialPiece = initialPieces[i];
                if (initialPiece.x < 0 || initialPiece.x >= xDim || initialPiece.y < 0 || initialPiece.y >= yDim) continue;

                if (initialPiece.pieceType == PieceType.Obstacle)
                {
                    SpawnNewObstacle(initialPiece.x, initialPiece.y, initialPiece.obstacleType, true);
                }
                else
                {
                    SpawnNewPiece(initialPiece.x, initialPiece.y, initialPiece.pieceType, true);
                }
            }

            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    if (_pieces[x, y] == null || _pieces[x, y].PieceType == PieceType.Empty)
                    {
                        SpawnNewPiece(x, y, PieceType.Empty);
                    }
                }
            }

            StartCoroutine(Fill());
        }

        public IEnumerator Fill()
        {
            bool needsRefill = true;
            IsFilling = true;
            while (needsRefill)
            {
                yield return new WaitForSeconds(fillTime);
                while (FillStep())
                {
                    _inverse = !_inverse;
                    yield return new WaitForSeconds(fillTime);
                }
                needsRefill = ClearAllValidMatches();
            }
            IsFilling = false;
            PlaySound(fillSound);

            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    GamePiece piece = _pieces[x, y];
                    if (piece != null && piece.isInitial) piece.isInitial = false;
                }
            }

            if (!isShuffling && !_gameOver)
            {
                yield return new WaitForEndOfFrame();
                if (IsDeadlock())
                {
                    if (IsSmallGrid) StartCoroutine(HandleGridReset());
                    else
                    {
                        currentShuffleAttempts = maxShuffleAttempts;
                        StartCoroutine(ShufflePieces());
                    }
                }
            }
        }

        protected bool FillStep()
        {
            bool movedPiece = false;
            for (int y = yDim - 2; y >= 0; y--)
            {
                for (int loopX = 0; loopX < xDim; loopX++)
                {
                    int x = loopX;
                    if (_inverse) { x = xDim - 1 - loopX; }
                    GamePiece piece = _pieces[x, y];
                    if (piece == null || !piece.IsMovable()) continue;

                    GamePiece pieceBelow = _pieces[x, y + 1];
                    if (pieceBelow.PieceType == PieceType.Unfillable) continue;
                    if (pieceBelow.PieceType == PieceType.Empty)
                    {
                        Destroy(pieceBelow.gameObject);
                        piece.MovableComponent.Move(x, y + 1, fillTime);
                        _pieces[x, y + 1] = piece;
                        SpawnNewPiece(x, y, PieceType.Empty);
                        movedPiece = true;
                    }
                    else
                    {
                        for (int diag = -1; diag <= 1; diag++)
                        {
                            if (diag == 0) continue;
                            int diagX = x + diag;
                            if (_inverse) diagX = x - diag;
                            if (diagX < 0 || diagX >= xDim) continue;

                            GamePiece diagonalPiece = _pieces[diagX, y + 1];
                            if (diagonalPiece == null || diagonalPiece.PieceType != PieceType.Empty) continue;

                            bool hasPieceAbove = true;
                            for (int aboveY = y; aboveY >= 0; aboveY--)
                            {
                                GamePiece pieceAbove = _pieces[diagX, aboveY];
                                if (pieceAbove == null)
                                {
                                    hasPieceAbove = false;
                                    break;
                                }
                                if (pieceAbove.IsMovable()) break;
                                else if (pieceAbove.PieceType != PieceType.Empty)
                                {
                                    hasPieceAbove = false;
                                    break;
                                }
                            }
                            if (hasPieceAbove) continue;
                            Destroy(diagonalPiece.gameObject);
                            piece.MovableComponent.Move(diagX, y + 1, fillTime);
                            _pieces[diagX, y + 1] = piece;
                            SpawnNewPiece(x, y, PieceType.Empty);
                            movedPiece = true;
                            break;
                        }
                    }
                }
            }

            for (int x = 0; x < xDim; x++)
            {
                GamePiece pieceBelow = _pieces[x, 0];
                if (pieceBelow == null || pieceBelow.PieceType != PieceType.Empty) continue;
                if (pieceBelow != null && pieceBelow.PieceType == PieceType.Empty)
                {
                    Destroy(pieceBelow.gameObject);
                    GameObject newPiece = Instantiate(_piecePrefabDict[PieceType.Normal], GetWorldPosition(x, -1), Quaternion.identity, this.transform);
                    _pieces[x, 0] = newPiece.GetComponent<GamePiece>();
                    _pieces[x, 0].Init(x, -1, this, PieceType.Normal);
                    _pieces[x, 0].MovableComponent.Move(x, 0, fillTime);
                    _pieces[x, 0].ColorComponent.SetColor((ColorType)Random.Range(0, _pieces[x, 0].ColorComponent.NumColors));

                    movedPiece = true;
                }
            }
            return movedPiece;
        }

        public void PressPiece(GamePiece piece) => _pressedPiece = piece;

        public void EnterPiece(GamePiece piece) => _enteredPiece = piece;

        public void ReleasePiece()
        {
            if (_pressedPiece == null || _enteredPiece == null)
            {
                _pressedPiece = null;
                _enteredPiece = null;
            }

            if (IsAdjacent(_pressedPiece, _enteredPiece))
            {
                StartCoroutine(SwapPieces(_pressedPiece, _enteredPiece));
            }
        }

        protected IEnumerator SwapPieces(GamePiece piece1, GamePiece piece2)
        {
            if (_gameOver || !IsAdjacent(piece1, piece2) || !piece1.IsMovable() || !piece2.IsMovable())
                yield break;

            int originalX1 = piece1.X, originalY1 = piece1.Y;
            int originalX2 = piece2.X, originalY2 = piece2.Y;

            _pieces[originalX1, originalY1] = piece2;
            _pieces[originalX2, originalY2] = piece1;
            piece1.MovableComponent.Move(originalX2, originalY2, fillTime);
            piece2.MovableComponent.Move(originalX1, originalY1, fillTime);

            yield return new WaitForSeconds(fillTime);

            GamePiece movedPiece1 = _pieces[originalX2, originalY2];
            GamePiece movedPiece2 = _pieces[originalX1, originalY1];

            bool hasValidMatch = false;

            bool hasNormalMatch = ClearAllValidMatches();
            bool hasSpecialMatch = piece1.PieceType == PieceType.Rainbow || piece2.PieceType == PieceType.Rainbow ||
                                   IsSpecialType(piece1.PieceType) || IsSpecialType(piece2.PieceType);

            hasValidMatch = hasNormalMatch || hasSpecialMatch;

            if (hasValidMatch)
            {
                yield return new WaitForSeconds(fillTime);
                if (piece1 != null && piece1.gameObject != null) HandleSpecialPieceSwap(piece1, piece2);
                if (piece2 != null && piece2.gameObject != null) HandleSpecialPieceSwap(piece2, piece1);
                DetectSpecialCombination(piece1, piece2);

                if (piece1.PieceType == PieceType.Rainbow && piece1.IsClearable() && piece2.IsColored())
                {
                    ClearColorPiece clearColor = piece1.GetComponent<ClearColorPiece>();
                    if (clearColor) clearColor.Color = piece2.ColorComponent.Color;
                    ClearPiece(piece1.X, piece1.Y);
                }

                if (piece2.PieceType == PieceType.Rainbow && piece2.IsClearable() && piece1.IsColored())
                {
                    ClearColorPiece clearColor = piece2.GetComponent<ClearColorPiece>();
                    if (clearColor) clearColor.Color = piece1.ColorComponent.Color;
                    ClearPiece(piece2.X, piece2.Y);
                }

                ClearAllValidMatches();

                if (piece1.PieceType == PieceType.RowClear || piece1.PieceType == PieceType.ColumnClear) ClearPiece(piece1.X, piece1.Y);
                if (piece2.PieceType == PieceType.RowClear || piece2.PieceType == PieceType.ColumnClear) ClearPiece(piece2.X, piece2.Y);

                _pressedPiece = null;
                _enteredPiece = null;
                StartCoroutine(Fill());
                level.OnMove();
            }
            else
            {
                yield return new WaitForSeconds(0.035f);
                _pieces[originalX1, originalY1] = piece1;
                _pieces[originalX2, originalY2] = piece2;
                piece1.MovableComponent.Move(originalX1, originalY1, fillTime);
                piece2.MovableComponent.Move(originalX2, originalY2, fillTime);
                PlaySound(unmatchSound);
            }
        }

        public bool ClearAllValidMatches()
        {
            bool needsRefill = false;
            int maxRecursion = 10;
            int currentRecursion = 0;

            while (currentRecursion < maxRecursion)
            {
                bool foundMatch = false;
                for (int y = 0; y < yDim; y++)
                {
                    for (int x = 0; x < xDim; x++)
                    {
                        if (_pieces[x, y] == null) continue;
                        if (!_pieces[x, y].IsClearable()) continue;

                        List<GamePiece> match = GetMatch(_pieces[x, y], x, y);
                        if (match == null) continue;
                        foundMatch = true;

                        PieceType specialPieceType = PieceType.Count;
                        int specialPieceX = _pressedPiece != null ? _pressedPiece.X : x;
                        int specialPieceY = _pressedPiece != null ? _pressedPiece.Y : y;

                        if (match.Count == 4)
                        {
                            PlaySound(combineSound);
                            if (IsSquarePiece(match[0].X, match[0].Y, match[0].ColorComponent.Color))
                                specialPieceType = PieceType.SquareClear;
                            else
                            {
                                if (_pressedPiece == null || _enteredPiece == null)
                                    specialPieceType = (PieceType)Random.Range((int)PieceType.RowClear, (int)PieceType.ColumnClear);
                                else if (_pressedPiece.Y == _enteredPiece.Y)
                                    specialPieceType = PieceType.RowClear;
                                else specialPieceType = PieceType.ColumnClear;
                            }
                        }
                        else if (match.Count == 5)
                        {
                            PlaySound(combineSound);
                            if (IsSquarePlusOne(match)) specialPieceType = PieceType.SquareClear;
                            else specialPieceType = PieceType.Rainbow;
                        }
                        else if (match.Count >= 6)
                        {
                            PlaySound(combineSound);
                            specialPieceType = PieceType.Rainbow;
                        }

                        foreach (var gamePiece in match)
                        {
                            if (!ClearPiece(gamePiece.X, gamePiece.Y)) continue;
                            needsRefill = true;
                            if (gamePiece != _pressedPiece && gamePiece != _enteredPiece) continue;
                            specialPieceX = gamePiece.X;
                            specialPieceY = gamePiece.Y;
                        }

                        if (specialPieceType == PieceType.Count) continue;

                        Destroy(_pieces[specialPieceX, specialPieceY]);
                        GamePiece newPiece = SpawnNewPiece(specialPieceX, specialPieceY, specialPieceType);

                        if ((specialPieceType == PieceType.RowClear || specialPieceType == PieceType.ColumnClear || specialPieceType == PieceType.SquareClear)
                            && newPiece.IsColored() && match[0].IsColored())
                        {
                            newPiece.ColorComponent.SetColor(match[0].ColorComponent.Color);
                        }
                        else if (specialPieceType == PieceType.Rainbow && newPiece.IsColored())
                            newPiece.ColorComponent.SetColor(ColorType.Any);
                    }
                }
                if (!foundMatch) break;
                currentRecursion++;
            }
            return needsRefill;
        }

        protected bool ClearPiece(int x, int y)
        {
            if (x < 0 || x >= xDim || y < 0 || y >= yDim) return false;

            GamePiece piece = _pieces[x, y];
            if (piece == null || piece.PieceType == PieceType.Unfillable) return false;
            if (_pressedPiece == piece) _pressedPiece = null;
            if (_enteredPiece == piece) _enteredPiece = null;
            if (!piece.IsClearable() || piece.ClearableComponent.IsBeingCleared) return false;

            if (piece.PieceType == PieceType.SquareClear)
            {
                ClearSquare(x, y);
                return true;
            }

            piece.ClearableComponent.Clear();
            SpawnNewPiece(x, y, PieceType.Empty);
            PlaySound(clearSound);
            ClearGrassObstacles(x, y);
            ClearBubbleObstacles(x, y);
            return true;
        }

        public void ClearRow(int row)
        {
            for (int x = 0; x < xDim; x++)
            {
                ClearPiece(x, row);
            }
        }

        public void ClearColumn(int column)
        {
            for (int y = 0; y < yDim; y++)
            {
                ClearPiece(column, y);
            }
        }

        public void ClearSquare(int x, int y)
        {
            if (_pieces[x, y].PieceType == PieceType.SquareClear)
            {
                Destroy(_pieces[x, y].gameObject);
                SpawnNewPiece(x, y, PieceType.Empty);
                PlaySound(clearSound);
            }
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int clearX = x + dx;
                    int clearY = y + dy;

                    if (clearX == x && clearY == y) continue;

                    if (clearX >= 0 && clearX < xDim && clearY >= 0 && clearY < yDim)
                    {
                        ClearPiece(clearX, clearY);
                    }
                }
            }
            StartCoroutine(Fill()); //手动触发填充
        }

        public void ClearColor(ColorType color)
        {
            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    if ((_pieces[x, y].IsColored() && _pieces[x, y].ColorComponent.Color == color)
                        || (color == ColorType.Any))
                    {
                        ClearPiece(x, y);
                    }
                }
            }
        }

        // 处理特殊块交换的通用方法
        private void HandleSpecialPieceSwap(GamePiece specialPiece, GamePiece otherPiece)
        {
            if (specialPiece == null || otherPiece == null ||
                specialPiece.gameObject == null || otherPiece.gameObject == null) return;
            if (!specialPiece.IsClearable() || otherPiece.PieceType != PieceType.Normal) return;

            switch (specialPiece.PieceType)
            {
                case PieceType.RowClear:
                    HandleRowClearSwap(specialPiece);
                    break;
                case PieceType.ColumnClear:
                    HandleColumnClearSwap(specialPiece);
                    break;
                case PieceType.SquareClear:
                    HandleSquareClearSwap(specialPiece);
                    break;
            }
        }

        // 处理行清除块交换
        private void HandleRowClearSwap(GamePiece rowClearPiece)
        {
            ClearLinePiece clearLine = rowClearPiece.GetComponent<ClearLinePiece>();
            if (clearLine != null)
            {
                clearLine.isRow = true; // 标记为行清除
                ClearPiece(rowClearPiece.X, rowClearPiece.Y); // 触发清除逻辑
            }
        }

        // 处理列清除块交换
        private void HandleColumnClearSwap(GamePiece columnClearPiece)
        {
            ClearLinePiece clearLine = columnClearPiece.GetComponent<ClearLinePiece>();
            if (clearLine != null)
            {
                clearLine.isRow = false; // 标记为列清除
                ClearPiece(columnClearPiece.X, columnClearPiece.Y); // 触发清除逻辑
            }
        }

        // 处理3x3清除块交换
        private void HandleSquareClearSwap(GamePiece squareClearPiece)
        {
            ClearSquarePiece clearSquare = squareClearPiece.GetComponent<ClearSquarePiece>();
            if (clearSquare != null)
            {
                ClearPiece(squareClearPiece.X, squareClearPiece.Y); // 触发清除逻辑
            }
        }

        public void DetectSpecialCombination(GamePiece a, GamePiece b)
        {
            if (a == null || b == null) return;

            PieceType t1 = a.PieceType;
            PieceType t2 = b.PieceType;

            // 组合1：行+列特殊块（十字消除）
            if ((t1 == PieceType.RowClear && t2 == PieceType.ColumnClear) ||
                (t1 == PieceType.ColumnClear && t2 == PieceType.RowClear))
            {
                int centerX = (a.X + b.X) / 2; // 相邻块中心坐标
                int centerY = (a.Y + b.Y) / 2;
                ClearSpecialCombination(SpecialCombinationType.Cross, centerX, centerY, 0, 0);
            }

            // 组合2：双行特殊块（行消除+分数翻倍）
            else if (t1 == PieceType.RowClear && t2 == PieceType.RowClear)
            {
                int row1 = a.Y, row2 = b.Y;
                ClearSpecialCombination(SpecialCombinationType.DoubleRow, 0, row1, 0, row2);
            }

            // 组合3：双列特殊块（列消除+分数翻倍）
            else if (t1 == PieceType.ColumnClear && t2 == PieceType.ColumnClear)
            {
                int col1 = a.X, col2 = b.X;
                ClearSpecialCombination(SpecialCombinationType.DoubleColumn, col1, 0, col2, 0);
            }

            // 组合4：双正方形特殊块（5x5消除）
            else if (t1 == PieceType.SquareClear && t2 == PieceType.SquareClear)
            {
                int centerX = (a.X + b.X) / 2;
                int centerY = (a.Y + b.Y) / 2;
                ClearSpecialCombination(SpecialCombinationType.DoubleSquare, centerX, centerY, 0, 0);
            }

            // 组合5：正方形+行/列特殊块
            else if (t1 == PieceType.SquareClear && t2 == PieceType.RowClear)
            {
                ClearSpecialCombination(SpecialCombinationType.SquareRow, 0, b.Y, a.X, a.Y);
            }
            else if (t1 == PieceType.SquareClear && t2 == PieceType.ColumnClear)
            {
                ClearSpecialCombination(SpecialCombinationType.SquareColumn, b.X, 0, a.X, a.Y);
            }

            // 组合6：彩虹块+特殊块
            else if (t1 == PieceType.Rainbow && IsSpecialType(t2))
            {
                HandleRainbowCombination(a, b);
            }
            else if (t2 == PieceType.Rainbow && IsSpecialType(t1))
            {
                HandleRainbowCombination(b, a);
            }
        }

        private void HandleRainbowCombination(GamePiece rainbow, GamePiece special)
        {
            ColorType targetColor = special.ColorComponent.Color; // 获取特殊块颜色
            switch (special.PieceType)
            {
                case PieceType.RowClear:
                    ClearRow(special.Y);
                    ClearColor(targetColor); // 消除同色（原有方法）
                    break;
                case PieceType.ColumnClear:
                    ClearColumn(special.X);
                    ClearColor(targetColor);
                    break;
                case PieceType.SquareClear:
                    ClearSquare(special.X, special.Y); // 原有3x3消除
                    ClearColor(targetColor);
                    break;
            }
        }

        // 特殊块和彩虹之间的消除逻辑
        public void ClearSpecialCombination(SpecialCombinationType type, int x1, int y1, int x2, int y2, GamePiece specialPiece = null)
        {
            switch (type)
            {
                case SpecialCombinationType.Cross:
                    ClearCross(x1, y1);
                    break;
                case SpecialCombinationType.DoubleRow:
                    ClearDoubleRow(y1, y2);
                    break;
                case SpecialCombinationType.DoubleColumn:
                    ClearDoubleColumn(x1, x2);
                    break;
                case SpecialCombinationType.DoubleSquare:
                    ClearDoubleSquare(x1, y1);
                    break;
                case SpecialCombinationType.SquareRow:
                    ClearRow(y1);
                    ClearSquare(x2, y2);
                    break;
                case SpecialCombinationType.SquareColumn:
                    ClearColumn(x1);
                    ClearSquare(x2, y2);
                    break;
                case SpecialCombinationType.RainbowRow:
                    ClearRow(y1);
                    ClearColor(specialPiece.ColorComponent.Color);
                    break;
                case SpecialCombinationType.RainbowColumn:
                    ClearColumn(x1);
                    ClearColor(specialPiece.ColorComponent.Color);
                    break;
                case SpecialCombinationType.RainbowSquare:
                    ClearSquare(x2, y2);
                    ClearColor(specialPiece.ColorComponent.Color);
                    break;
            }
            PlaySound(combineSound);
        }

        private void ClearCross(int centerX, int centerY)
        {
            ClearRow(centerY);
            ClearColumn(centerX);
        }

        private void ClearDoubleRow(int row1, int row2)
        {
            if (row1 == row2)
            {
                ClearRow(row1);
                level.MarkScoreMultiplier(2);
            }
            else
            {
                ClearRow(row1);
                ClearRow(row2);
            }
        }

        private void ClearDoubleColumn(int column1, int column2)
        {
            if (column1 == column2)
            {
                ClearColumn(column1);
                level.MarkScoreMultiplier(2);
            }
            else
            {
                ClearColumn(column1);
                ClearColumn(column2);
            }
        }
        private void ClearDoubleSquare(int centerX, int centerY)
        {
            for (int x = centerX - 2; x <= centerX + 2; x++)
            {
                for (int y = centerY - 2; y <= centerY + 2; y++)
                {
                    if (x >= 0 && x < xDim && y >= 0 && y < yDim)
                    {
                        ClearPiece(x, y); // 原有单块消除方法
                    }
                }
            }
        }

        private GamePiece SpawnNewObstacle(int x, int y, ObstacleType obstacleType, bool _isInitial = false)
        {
            if (x < 0 || x >= xDim || y < 0 || y >= yDim) return null;

            if (!_obstaclePrefabDict.TryGetValue(obstacleType, out GameObject prefab)) return null;

            if (_pieces[x, y] != null)
            {
                Destroy(_pieces[x, y].gameObject);
                _pieces[x, y] = null;
            }

            GameObject newObstacle = Instantiate(_obstaclePrefabDict[obstacleType], GetWorldPosition(x, y), Quaternion.identity, this.transform);
            GamePiece obstaclePiece = newObstacle.GetComponent<GamePiece>();
            obstaclePiece.Init(x, y, this, PieceType.Obstacle, obstacleType);
            obstaclePiece.isInitial = _isInitial;
            _pieces[x, y] = obstaclePiece;

            if (obstacleType >= ObstacleType.Bubble_1 && obstacleType <= ObstacleType.Bubble_3)
            {
                var animator = newObstacle.GetComponent<Animator>();
                if (animator != null) animator.enabled = false;
            }

            ClearablePiece clearable = newObstacle.GetComponent<ClearablePiece>();
            if (clearable != null) clearable.enabled = true;
            return obstaclePiece;
        }

        private void ClearGrassObstacles(int x, int y)
        {
            for (int adjacentX = x - 1; adjacentX <= x + 1; adjacentX++)
            {
                if (adjacentX == x || adjacentX < 0 || adjacentX >= xDim) continue;

                GamePiece adjacentPiece = _pieces[adjacentX, y];
                if (adjacentPiece.ObstacleType != ObstacleType.Grass || !_pieces[adjacentX, y].IsClearable()) continue;

                _pieces[adjacentX, y].ClearableComponent.Clear();
                SpawnNewPiece(adjacentX, y, PieceType.Empty);
            }

            for (int adjacentY = y - 1; adjacentY <= y + 1; adjacentY++)
            {
                if (adjacentY == y || adjacentY < 0 || adjacentY >= yDim) continue;

                GamePiece adjacentPiece = _pieces[x, adjacentY];
                if (adjacentPiece.ObstacleType != ObstacleType.Grass || !_pieces[x, adjacentY].IsClearable()) continue;

                _pieces[x, adjacentY].ClearableComponent.Clear();
                SpawnNewPiece(x, adjacentY, PieceType.Empty);
            }
        }

        public void ReduceBubbleState(int x, int y)
        {
            if (x < 0 || x >= xDim || y < 0 || y >= yDim) return;

            GamePiece bubble = _pieces[x, y];
            if (bubble == null || bubble.ObstacleType < ObstacleType.Bubble_1 ||
                bubble.ObstacleType > ObstacleType.Bubble_3) return;

            if (bubble.ClearableComponent.IsBeingCleared) return;
            if (bubble.ObstacleType == ObstacleType.Bubble_3)
            {
                ClearPiece(x, y);
                return;
            }

            ObstacleType nextState = bubble.ObstacleType + 1;
            bubble.ClearableComponent.IsBeingCleared = true;
            bubble.ClearableComponent.Clear();

            StartCoroutine(HandleBubbleTransformAfterAnim(x, y, nextState, bubble.ClearableComponent.clearAnimation.length));
        }

        private IEnumerator HandleBubbleTransformAfterAnim(int x, int y, ObstacleType nextState, float animationLength)
        {
            yield return new WaitForSeconds(animationLength);

            GamePiece newBubble = SpawnNewObstacle(x, y, nextState, false);
            if (newBubble != null)
            {
                ClearablePiece clearable = newBubble.GetComponent<ClearablePiece>();
                if (clearable != null)
                {
                    clearable.enabled = true;

                    var animator = newBubble.GetComponent<Animator>();
                    if (animator != null)
                        animator.enabled = false;
                }
            }
        }

        private void ClearBubbleObstacles(int x, int y)
        {
            for (int adjacentX = x - 1; adjacentX <= x + 1; adjacentX++)
            {
                if (adjacentX == x || adjacentX < 0 || adjacentX >= xDim) continue;
                ReduceBubbleState(adjacentX, y);
            }

            for (int adjacentY = y - 1; adjacentY <= y + 1; adjacentY++)
            {
                if (adjacentY == y || adjacentY < 0 || adjacentY >= yDim) continue;
                ReduceBubbleState(x, adjacentY);
            }
        }

        private void ReduceBubbleIfPossible(int x, int y)
        {
            GamePiece piece = _pieces[x, y];
            if (piece != null &&
                piece.ObstacleType >= ObstacleType.Bubble_1 &&
                piece.ObstacleType <= ObstacleType.Bubble_3)
            {
                ReduceBubbleState(x, y);
            }
        }

        private bool IsDeadlock()
        {
            bool hasMovableSpecial = false;
            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    GamePiece piece = _pieces[x, y];
                    if (piece == null) continue;
                    if ((IsSpecialType(piece.PieceType) || piece.PieceType == PieceType.Rainbow) &&
                piece.IsMovable() && piece.IsClearable())
                    {
                        if (HasAdjacentMovablePiece(x, y))
                        {
                            hasMovableSpecial = true;
                            break;
                        }
                    }
                }
                if (hasMovableSpecial) break;
            }
            if (hasMovableSpecial) return false;

            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    if (x < xDim - 1 && HasValidSwap(x, y, x + 1, y)) return false;
                    if (y < yDim - 1 && HasValidSwap(x, y, x, y + 1)) return false;
                }
            }
            return true;
        }

        private bool HasValidSwap(int x1, int y1, int x2, int y2)
        {
            GamePiece piece1 = _pieces[x1, y1];
            GamePiece piece2 = _pieces[x2, y2];

            if ((IsSpecialType(piece1.PieceType) || piece1.PieceType == PieceType.Rainbow) && !piece1.IsMovable())
                return false;
            if ((IsSpecialType(piece2.PieceType) || piece2.PieceType == PieceType.Rainbow) && !piece2.IsMovable())
                return false;

            if (piece1 == null || piece2 == null) return false;
            if (!piece1.IsMovable() || !piece2.IsMovable()) return false;

            _pieces[x1, y1] = piece2;
            _pieces[x2, y2] = piece1;

            bool hasMatch = GetMatch(piece1, x2, y2) != null || GetMatch(piece2, x1, y1) != null;

            _pieces[x1, y1] = piece1;
            _pieces[x2, y2] = piece2;

            return hasMatch || IsSpecialType(piece1.PieceType) || IsSpecialType(piece2.PieceType);
        }

        private bool HasAdjacentMovablePiece(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) != 1) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx < 0 || nx >= xDim || ny < 0 || ny >= yDim) continue;
                    GamePiece neighbor = _pieces[nx, ny];
                    if (neighbor != null && neighbor.IsMovable()) return true;
                }
            }
            return false;
        }

        private GamePiece SpawnNewPiece(int x, int y, PieceType pieceType, bool _isInitial = false)
        {
            if (x < 0 || x >= xDim || y < 0 || y >= yDim) return null;
            GameObject newPiece = Instantiate(_piecePrefabDict[pieceType], GetWorldPosition(x, y), Quaternion.identity, this.transform);
            _pieces[x, y] = newPiece.GetComponent<GamePiece>();
            _pieces[x, y].Init(x, y, this, pieceType);
            _pieces[x, y].isInitial = _isInitial;

            if (_isInitial && _pieces[x, y].IsColored() && pieceType != PieceType.Empty && pieceType != PieceType.Obstacle)
                _pieces[x, y].ColorComponent.SetColor((ColorType)Random.Range(0, _pieces[x, y].ColorComponent.NumColors));

            return _pieces[x, y];
        }

        public Vector2 GetWorldPosition(int x, int y)
        {
            return new Vector2(
                transform.position.x - xDim / 2.0f + x + 0.5f,
                transform.position.y + yDim / 2.0f - y - 0.5f);
        }

        private static bool IsAdjacent(GamePiece piece1, GamePiece piece2)
        {
            if (piece1 == null || piece2 == null) return false;
            return (piece1.X == piece2.X && Mathf.Abs(piece1.Y - piece2.Y) == 1) ||
                    (piece1.Y == piece2.Y && Mathf.Abs(piece1.X - piece2.X) == 1);
        }
            

        private bool IsSpecialType(PieceType type)
        {
            return type == PieceType.RowClear || type == PieceType.ColumnClear || type == PieceType.SquareClear;
        }

        private bool IsSquarePiece(int x, int y, ColorType color)
        {
            bool isTopLeft = CheckSquare(x, y, color);
            bool isTopRight = (x > 0) ? CheckSquare(x - 1, y, color) : false;
            bool isBottomLeft = (y > 0) ? CheckSquare(x, y - 1, color) : false;
            bool isBottomRight = (x > 0 && y > 0) ? CheckSquare(x - 1, y - 1, color) : false;

            return (isTopLeft || isTopRight || isBottomLeft || isBottomRight);
        }

        private bool IsSquarePlusOne(List<GamePiece> match)
        {
            foreach (GamePiece piece in match)
            {
                int x = piece.X;
                int y = piece.Y;

                if (CheckSquare(x, y, piece.ColorComponent.Color, match)) return true;
                if (x > 0 && CheckSquare(x - 1, y, piece.ColorComponent.Color, match)) return true;
                if (y > 0 && CheckSquare(x, y - 1, piece.ColorComponent.Color, match)) return true;
                if (x > 0 && y > 0 && CheckSquare(x - 1, y - 1, piece.ColorComponent.Color, match)) return true;
            }
            return false;
        }

        private bool CheckSquare(int startX, int startY, ColorType color, List<GamePiece> match = null)
        {
            if (startX >= xDim - 1 || startY >= yDim - 1) return false;
            for (int dx = 0; dx <= 1; dx++)
            {
                for (int dy = 0; dy <= 1; dy++)
                {
                    GamePiece piece = _pieces[startX + dx, startY + dy];
                    if (piece == null || !piece.IsColored() || piece.ColorComponent.Color != color)
                        return false;

                    if (match != null && !match.Contains(piece)) return false;
                }
            }
            return true;            
        }

        public List<GamePiece> GetMatch(GamePiece piece, int newX, int newY)
        {
            if (!piece.IsColored() || piece.ClearableComponent.IsBeingCleared) return null;
            var color = piece.ColorComponent.Color;

            List<GamePiece> squarePlusOneMatch = GetSquarePlusOneMatch(color);
            if (squarePlusOneMatch != null && squarePlusOneMatch.Count >= 5) return squarePlusOneMatch;

            if (IsSquarePiece(newX, newY, color))
            {
                List<GamePiece> squareMatch = new List<GamePiece>();
                for (int dx = 0; dx <= 1; dx++)
                {
                    for (int dy = 0; dy <= 1; dy++)
                    {
                        int x = newX + dx;
                        int y = newY + dy;
                        if (x < xDim && y < yDim)
                            squareMatch.Add(_pieces[x, y]);
                    }
                }
                return squareMatch;
            }

            var horizontalPieces = new List<GamePiece>();
            var verticalPieces = new List<GamePiece>();
            var matchingPieces = new List<GamePiece>();

            horizontalPieces.Add(piece);
            for (int dir = 0; dir <= 1; dir++)
            {
                for (int xOffset = 1; xOffset < xDim; xOffset++)
                {
                    int x = (dir == 0) ? (newX - xOffset) : (newX + xOffset);

                    if (x < 0 || x >= xDim) { break; }

                    if (_pieces[x, newY].IsColored() && _pieces[x, newY].ColorComponent.Color == color)
                        horizontalPieces.Add(_pieces[x, newY]);
                    else break;
                }
            }

            if (horizontalPieces.Count >= 3)
                matchingPieces.AddRange(horizontalPieces);

            if (horizontalPieces.Count >= 3)
            {
                for (int i = 0; i < horizontalPieces.Count; i++)
                {
                    for (int dir = 0; dir <= 1; dir++)
                    {
                        for (int yOffset = 1; yOffset < yDim; yOffset++)
                        {
                            int y = (dir == 0) ? (newY - yOffset) : (newY + yOffset);
                            if (y < 0 || y >= yDim) break;
                            if (_pieces[horizontalPieces[i].X, y].IsColored() &&
                                _pieces[horizontalPieces[i].X, y].ColorComponent.Color == color)
                                verticalPieces.Add(_pieces[horizontalPieces[i].X, y]);
                            else break;
                        }
                    }
                    if (verticalPieces.Count < 2) verticalPieces.Clear();
                    else
                    {
                        matchingPieces.AddRange(verticalPieces);
                        break;
                    }
                }
            }
            if (matchingPieces.Count >= 3) return matchingPieces;

            horizontalPieces.Clear();
            verticalPieces.Clear();
            verticalPieces.Add(piece);

            for (int dir = 0; dir <= 1; dir++)
            {
                for (int yOffset = 1; yOffset < xDim; yOffset++)
                {
                    int y = (dir == 0) ? (newY - yOffset) : (newY + yOffset);
                    if (y < 0 || y >= yDim) { break; }

                    // 检查是否同色
                    if (_pieces[newX, y].IsColored() && (_pieces[newX, y].ColorComponent.Color == color/*||_pieces[newX,y].ColorComponent.Color==ColorType.Any)*/))
                        verticalPieces.Add(_pieces[newX, y]);
                    else break;
                }
            }
            if (verticalPieces.Count >= 3)
                matchingPieces.AddRange(verticalPieces);

            if (verticalPieces.Count >= 3)
            {
                for (int i = 0; i < verticalPieces.Count; i++)
                {
                    for (int dir = 0; dir <= 1; dir++)
                    {
                        for (int xOffset = 1; xOffset < yDim; xOffset++)
                        {
                            int x = (dir == 0) ? (newX - xOffset) : (newX + xOffset);

                            if (x < 0 || x >= xDim) break;

                            if (_pieces[x, verticalPieces[i].Y].IsColored() && _pieces[x, verticalPieces[i].Y].ColorComponent.Color == color)
                                horizontalPieces.Add(_pieces[x, verticalPieces[i].Y]);
                            else break;
                        }
                    }

                    if (horizontalPieces.Count < 2) horizontalPieces.Clear();
                    else
                    {
                        matchingPieces.AddRange(horizontalPieces);
                        break;
                    }
                }
            }
            if (matchingPieces.Count >= 3) return matchingPieces;

            return null;
        }

        private List<GamePiece> GetSquarePlusOneMatch(ColorType color)
        {
            for (int startX = 0; startX < xDim - 1; startX++)
            {
                for (int startY = 0; startY < yDim - 1; startY++)
                {
                    if (!CheckSquare(startX, startY, color)) continue;

                    List<GamePiece> squarePieces = new List<GamePiece>
                    {
                        _pieces[startX,startY], _pieces[startX+1,startY],
                        _pieces[startX,startY+1],_pieces[startX+1,startY+1]
                    };

                    List<Vector2Int> edgeOffsets = new List<Vector2Int>
                    {
                        new Vector2Int(startX, startY - 1), new Vector2Int(startX + 1, startY - 1),
                        new Vector2Int(startX + 2, startY), new Vector2Int(startX + 2, startY + 1),
                        new Vector2Int(startX, startY + 2), new Vector2Int(startX + 1, startY + 2),
                        new Vector2Int(startX - 1, startY), new Vector2Int(startX - 1, startY + 1),
                    };

                    foreach (var offset in edgeOffsets)
                    {
                        int x = offset.x;
                        int y = offset.y;
                        if (x < 0 || x >= xDim || y < 0 || y >= yDim) continue;

                        GamePiece neighbor = _pieces[x, y];
                        if (neighbor == null || !neighbor.IsColored() || neighbor.ColorComponent.Color != color) continue;
                        if (squarePieces.Contains(neighbor)) continue;

                        return new List<GamePiece>(squarePieces) { neighbor };
                    }
                }
            }
            return null;
        }

        public List<GamePiece> GetPiecesOfType(PieceType type)
        {
            var piecesOfType = new List<GamePiece>();

            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    if (_pieces[x, y].PieceType == type)
                    {
                        piecesOfType.Add(_pieces[x, y]);
                    }
                }
            }

            return piecesOfType;
        }

        public List<GamePiece> GetPiecesOfObstacleType(ObstacleType type)
        {
            var pieces = new List<GamePiece>();
            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    GamePiece piece = _pieces[x, y];
                    if (piece != null && piece.PieceType == PieceType.Obstacle && piece.ObstacleType == type)
                        pieces.Add(piece);
                }
            }
            return pieces;
        }

        private IEnumerator ShufflePieces()
        {
            isShuffling = true;
            _pressedPiece = null;
            _enteredPiece = null;

            if (IsSmallGrid)
            {
                yield return HandleGridReset();
                isShuffling = false;
                yield break;
            }

            if (currentShuffleAttempts <= 0)
            {
                yield return HandleGridReset();
                isShuffling = false;
                yield break;
            }

            currentShuffleAttempts--;
            PlaySound(shuffleSound);

            List<GamePiece> movablePieces = new List<GamePiece>();
            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    GamePiece piece = _pieces[x, y];
                    if (piece != null && piece.IsMovable() && piece.PieceType != PieceType.Empty && piece.PieceType == PieceType.Normal)
                        movablePieces.Add(piece);
                }
            }

            for (int i = 0; i < movablePieces.Count; i++)
            {
                int randomIndex = Random.Range(i, movablePieces.Count);
                (movablePieces[i], movablePieces[randomIndex]) = (movablePieces[randomIndex], movablePieces[i]);
            }

            int index = 0;
            List<GamePiece> movedPieces = new List<GamePiece>();

            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    GamePiece currentPiece = _pieces[x, y];
                    if (currentPiece == null || !currentPiece.IsMovable() ||
                        currentPiece.PieceType != PieceType.Normal || currentPiece.PieceType == PieceType.Empty) continue;

                    if (index >= movablePieces.Count) break;
                    GamePiece newPiece = movablePieces[index];
                    movedPieces.Add(newPiece);
                    _pieces[x, y] = newPiece;

                    newPiece.MovableComponent.Move(x, y, 0.3f);
                    index++;
                }
            }

            yield return new WaitForSeconds(0.025f);
            ClearAllValidMatches();
            yield return StartCoroutine(Fill());

            if (IsDeadlock())
            {
                if (currentShuffleAttempts > 0) StartCoroutine(ShufflePieces());
                else
                {
                    yield return HandleGridReset();
                    currentShuffleAttempts = maxShuffleAttempts;
                }
            }
            else currentShuffleAttempts = maxShuffleAttempts;

            isShuffling = false;
        }

        private IEnumerator HandleGridReset()
        {
            PlaySound(shuffleSound);

            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    GamePiece piece = _pieces[x, y];
                    if (piece != null && piece.IsMovable() && piece.IsClearable() && piece.PieceType != PieceType.Empty)
                    {
                        Destroy(piece.gameObject);
                        SpawnNewPiece(x, y, PieceType.Empty);
                    }
                }
            }

            yield return StartCoroutine(Fill());
            currentShuffleAttempts = maxShuffleAttempts;
        }

        private void PlaySound(AudioClip sound)
        {
            if (audioSource != null && sound != null)
                audioSource.PlayOneShot(sound);
        }

        public void GameOver() => _gameOver = true;
    }
}