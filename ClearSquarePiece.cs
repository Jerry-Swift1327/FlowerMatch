using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Match3;

public class ClearSquarePiece : ClearablePiece
{
    public override void Clear()
    {
        base.Clear();
        piece.GameGridRef.ClearSquare(piece.X, piece.Y);
    }
}
