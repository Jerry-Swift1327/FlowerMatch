namespace Match3
{
    public enum PieceType
    {
        Empty, // 并非字面意思的空块，被填充逻辑被视为“需要被填充的位置”
        Normal,
        RowClear,
        ColumnClear,
        Rainbow,
        SquareClear,
        Unfillable, // 不可填充且不可移动的固定障碍
        Obstacle, // 可清除的障碍物
        Count // 不是实际上的一种块类型
    }
}
