using System;
using System.Linq;
using ColorBlockJam.LevelEditor;
using DG.DOTweenEditor;
using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.SandBox.Editor
{
    // Moves the block of a solution's next drag through the scene preview: a cell at a time along its path, then, when it
    // leaves, on out through the gate while it shrinks away. Edit-mode tweens run through DOTween's editor preview.
    [InitializeOnLoad]
    internal static class LevelEditorPlaybackAnimator
    {
        private const float SecondsPerCell = 0.12f;
        private const float ExitSeconds = 0.35f;
        // How far past its last cell a leaving block slides while it shrinks.
        private const float ExitCells = 1.5f;

        static LevelEditorPlaybackAnimator()
        {
            LevelEditorSession.PlaybackAnimate += Animate;
        }

        private static void Animate(LevelTestStep step, Action done)
        {
            var board = LevelEditorScenePreview.Board;
            var block = LevelEditorScenePreview.Simulation?.Blocks.FirstOrDefault(b => b.Id == step.BlockId);
            var view = block != null && board != null ? board.GetView(block) : null;
            if (view == null || step.Path == null || step.Path.Length == 0)
            {
                done();
                return;
            }

            var layout = board.Layout;
            var target = view.transform;
            var sequence = DOTween.Sequence().SetTarget(target).SetLink(view.gameObject);
            for (var i = 1; i < step.Path.Length; i++)
                sequence.Append(target.DOLocalMove(layout.CellToLocal(step.Path[i]), SecondsPerCell).SetEase(Ease.Linear));

            if (step.Exits)
            {
                var last = layout.CellToLocal(step.Path[step.Path.Length - 1]);
                var away = layout.OutwardLocal(step.ExitEdge) * (layout.CellSize * ExitCells);
                sequence.Append(target.DOLocalMove(last + away, ExitSeconds).SetEase(Ease.InQuad));
                sequence.Join(target.DOScale(0f, ExitSeconds).SetEase(Ease.InQuad));
            }

            sequence.OnComplete(() => done());
            // Keep the OnComplete (the default would clear callbacks) and let the tween die when it ends.
            DOTweenEditorPreview.PrepareTweenForPreview(sequence, false, false);
            DOTweenEditorPreview.Start();
        }
    }
}
