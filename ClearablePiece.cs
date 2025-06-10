using System.Collections;
using UnityEngine;


namespace Match3
{
    public class ClearablePiece : MonoBehaviour
    {
        public AnimationClip clearAnimation;
        private Level level;

        public bool IsBeingCleared { get; set; }

        protected GamePiece piece;

        protected virtual void Awake()
        {
            piece = GetComponent<GamePiece>();
        }

        public virtual void Clear()
        {
            if (piece.PieceType != PieceType.Obstacle || piece.ObstacleType < ObstacleType.Bubble_1 || piece.ObstacleType > ObstacleType.Bubble_2)
            {
                piece.GameGridRef.level.OnPieceCleared(piece);

            }
            IsBeingCleared = true;
            StartCoroutine(ClearCoroutine());
        }

        private IEnumerator ClearCoroutine()
        {
            var animator = GetComponent<Animator>();

            if (animator)
            {
                animator.enabled = true;
                animator.Play(clearAnimation.name);

                yield return new WaitForSeconds(clearAnimation.length);
                animator.enabled = false;

                if (gameObject != null) Destroy(gameObject);
            }
            else Destroy(gameObject);
        }
    }
}