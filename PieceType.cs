namespace Match3
{
    public enum PieceType
    {
        Empty, // ����������˼�Ŀտ飬������߼�����Ϊ����Ҫ������λ�á�
        Normal,
        RowClear,
        ColumnClear,
        Rainbow,
        SquareClear,
        Unfillable, // ��������Ҳ����ƶ��Ĺ̶��ϰ�
        Obstacle, // ��������ϰ���
        Count // ����ʵ���ϵ�һ�ֿ�����
    }
}
