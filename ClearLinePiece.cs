﻿using UnityEngine;

namespace Match3
{
    internal class ClearLinePiece : ClearablePiece
    {
        [HideInInspector] public bool isRow;
 
        public override void Clear()
        {
            base.Clear();

            if (isRow) piece.GameGridRef.ClearRow(piece.Y);
            else piece.GameGridRef.ClearColumn(piece.X);                 
        }            
    }
}
