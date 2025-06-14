﻿using System.Collections;
using UnityEngine;

namespace Match3
{
    public class MovablePiece : MonoBehaviour
    {
        private GamePiece _piece;
        private IEnumerator _moveCoroutine;

        private void Awake()
        {
            _piece = GetComponent<GamePiece>();
        }

        public void Move(int newX, int newY, float time)
        {
            if (this == null) return;

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
            }

            _moveCoroutine = MoveCoroutine(newX, newY, time);
            StartCoroutine(_moveCoroutine);
        }

        private IEnumerator MoveCoroutine(int newX, int newY, float time)
        {
            if (_piece == null || this == null) yield break;

            _piece.X = newX;
            _piece.Y = newY;

            Vector3 startPos = transform.position;
            Vector3 endPos = _piece.GameGridRef.GetWorldPosition(newX, newY);

            for (float t = 0; t <= 1 * time; t += Time.deltaTime)
            {
                if (_piece == null || _piece.gameObject == null || this == null) yield break;
                _piece.transform.position = Vector3.Lerp(startPos, endPos, t / time);
                yield return null;
            }

            if (_piece == null || this == null) yield break;
            _piece.transform.position = endPos;
        }
    }
}
