using System.Collections.Generic;
using ColorBlockJam.SandBox.Board;
using UnityEngine;

namespace ColorBlockJam.SandBox
{
    // A booster waiting for a target: the board darkens and only the blocks that
    // can be picked stay bright. They move to the SelectableItem layer, which the selected-item camera draws over the
    // dim quad that hangs in front of the gameplay camera.
    public sealed class BoardSelection
    {
        private readonly BoardViewController _boardViewController;
        private readonly BoardCameraView _cameraView;
        private readonly BoosterAnimationSettings _animationSettings;
        private readonly HashSet<BlockState> _marked = new HashSet<BlockState>();
        private GameObject _dim;

        public BoardSelection(BoardViewController boardViewController, BoardCameraView cameraView,
            BoosterAnimationSettings animationSettings)
        {
            _boardViewController = boardViewController;
            _cameraView = cameraView;
            _animationSettings = animationSettings;
        }

        public bool Contains(BlockState block) => block != null && _marked.Contains(block);

        // showTargets: a target mark over each block (Rocket).
        public void Open(IEnumerable<BlockState> blocks, bool showTargets)
        {
            var layer = LayerMask.NameToLayer(BoardLayers.SelectableItem);
            foreach (var block in blocks)
            {
                var view = _boardViewController.GetView(block);
                if (!view) continue;
                view.SetLayer(layer);
                view.SetTargetMark(showTargets);
                _marked.Add(block);
            }

            SetDim(true);
        }

        public void Close()
        {
            var layer = LayerMask.NameToLayer(BoardLayers.Board);
            foreach (var block in _marked)
            {
                // A marked block may have left the board meanwhile (its view went back to the pool).
                var view = _boardViewController ? _boardViewController.GetView(block) : null;
                if (!view) continue;
                view.SetLayer(layer);
                view.SetTargetMark(false);
            }

            _marked.Clear();
            SetDim(false);
        }

        // One dim per gameplay scene, made on first use as a child of the gameplay camera (unloads with the scene).
        private void SetDim(bool visible)
        {
            if (_dim == null)
            {
                if (!visible) return;
                if (_animationSettings.BoardDimPrefab == null)
                {
                    Debug.LogError($"{nameof(BoardSelection)}: {nameof(BoosterAnimationSettings)}.{nameof(BoosterAnimationSettings.BoardDimPrefab)} is not set");
                    return;
                }

                _dim = Object.Instantiate(_animationSettings.BoardDimPrefab, _cameraView.GamePlayCamera.transform, false);
            }

            _dim.SetActive(visible);
        }
    }
}
