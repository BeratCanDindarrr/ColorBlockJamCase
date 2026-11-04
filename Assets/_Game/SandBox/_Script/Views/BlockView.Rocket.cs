using System;
using System.Collections.Generic;
using ColorBlockJam.Core.Level;
using DG.Tweening;
using UnityEngine;
using VContainer;

namespace ColorBlockJam.SandBox
{
    // Rocket booster parts of BlockView: the target mark shown while picking, and breaking one cell off.
    public partial class BlockView
    {
        // Shown over every cell of the block while the Rocket waits for a target; the first cell uses this one, the
        // others copies of it. Children of the block, so they follow its layer.
        [SerializeField] private GameObject targetMark;

        // Copies of targetMark for the second cell on, kept for the next selection.
        private readonly List<GameObject> _extraTargetMarks = new List<GameObject>();

        private BoosterAnimationSettings _animationSettings;

        // Apart from Construct: the level editor preview builds blocks without it (it never plays boosters).
        [Inject]
        public void ConstructBoosterAnimation(BoosterAnimationSettings animationSettings)
        {
            _animationSettings = animationSettings;
        }

        // Pieces a Rocket knocked off, still flying away; back to the pool when done (or when the block despawns).
        private readonly List<BlockPieceView> _brokenPieces = new List<BlockPieceView>();

        public void SetTargetMark(bool visible)
        {
            targetMark.SetActive(false);
            foreach (var mark in _extraTargetMarks) mark.SetActive(false);
            if (!visible) return;

            var height = targetMark.transform.localPosition.y;
            for (var i = 0; i < Block.Cells.Count; i++)
            {
                var cell = Block.Cells[i];
                var mark = TargetMark(i);
                mark.transform.localPosition = new Vector3(cell.Col, height, -cell.Row);
                mark.SetActive(true);
            }
        }

        private GameObject TargetMark(int index)
        {
            if (index == 0) return targetMark;
            while (_extraTargetMarks.Count < index) _extraTargetMarks.Add(Instantiate(targetMark, transform));
            return _extraTargetMarks[index - 1];
        }

        // The pieces of the broken cell (offset from Origin) fly off; the block rebuilds in its new shape and jolts.
        public void PlayBreak(CellCoord brokenOffset)
        {
            for (var i = _pieces.Count - 1; i >= 0; i--)
            {
                if (_pieceCells[i] != brokenOffset) continue;

                var piece = _pieces[i];
                _pieces.RemoveAt(i);
                _pieceCells.RemoveAt(i);
                _brokenPieces.Add(piece);
                FlyOff(piece);
            }

            foreach (var piece in _pieces)
            {
                if (piece) _piecePool.Despawn(piece);
            }

            _pieces.Clear();
            _pieceCells.Clear();
            BuildShape();

            transform.DOKill(true);
            transform.DOPunchScale(transform.localScale * _animationSettings.DestroyPunch,
                    _animationSettings.DestroyPunchDuration)
                .SetLink(gameObject);
        }

        // Rocket hit the block's last cell: every piece flies off at once, like the broken cell of a bigger block (no
        // jolt, no wait); onDone runs once all are gone.
        public void PlayBreakAll(Action<BlockView> onDone)
        {
            _onShattered = onDone;
            axisView.Hide();
            transform.DOKill();
            ResetHeldPose();
            BreakAllPieces();
        }

        // Hammer hit: the block squashes about its centre, then breaks apart while it springs back; onDone runs once
        // every piece is gone.
        public void PlayHammerHit(Action<BlockView> onDone)
        {
            _onShattered = onDone;
            axisView.Hide();
            transform.DOKill();
            ResetHeldPose();

            var start = transform.localPosition;
            var baseScale = transform.localScale;
            // Scaling the block about its centre: the origin moves by centre * (1 - scale).
            var centre = _centroid * baseScale.x;
            var squash = _animationSettings.HammerBlockSquash;
            var squashTime = _animationSettings.HammerBlockSquashDuration;
            var rebound = _animationSettings.HammerBlockReboundDuration;

            DOTween.Sequence().SetTarget(transform).SetLink(gameObject)
                .Append(transform.DOScale(baseScale * squash, squashTime).SetEase(Ease.OutQuad))
                .Join(transform.DOLocalMove(start + centre * (1f - squash), squashTime).SetEase(Ease.OutQuad))
                .AppendCallback(BreakAllPieces)
                .Append(transform.DOScale(baseScale, rebound).SetEase(Ease.OutBack))
                .Join(transform.DOLocalMove(start, rebound).SetEase(Ease.OutBack));
        }

        // Every piece flies off like a broken cell; FinishShatter once all are gone.
        private void BreakAllPieces()
        {
            var left = _pieces.Count;
            foreach (var piece in _pieces)
            {
                _brokenPieces.Add(piece);
                FlyOff(piece, () =>
                {
                    if (--left == 0) FinishShatter();
                });
            }

            _pieces.Clear();
            _pieceCells.Clear();
            if (left == 0) FinishShatter();
        }

        // Like an exit shatter, but upward and on its own: the piece goes back to the pool when it is gone.
        private void FlyOff(BlockPieceView piece, Action onGone = null)
        {
            var scale = transform.localScale.x;
            var spread = UnityEngine.Random.insideUnitSphere * _boardSettings.ShatterSpread;
            spread.y = Mathf.Abs(spread.y);
            var velocity = Vector3.up * _boardSettings.ShatterForce + spread;
            var duration = _boardSettings.ShatterDuration;

            piece.transform.DOLocalMove(piece.transform.localPosition + velocity * (duration / scale), duration)
                .SetEase(Ease.OutQuad).SetLink(piece.gameObject);
            piece.transform.DOScale(0f, duration).SetEase(Ease.InQuad).SetLink(piece.gameObject)
                .OnComplete(() =>
                {
                    _brokenPieces.Remove(piece);
                    _piecePool.Despawn(piece);
                    onGone?.Invoke();
                });
        }

        private void ReleaseBrokenPieces()
        {
            foreach (var piece in _brokenPieces)
            {
                if (piece) _piecePool.Despawn(piece);
            }

            _brokenPieces.Clear();
        }
    }
}
