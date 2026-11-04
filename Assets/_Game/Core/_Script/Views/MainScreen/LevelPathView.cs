using System.Collections.Generic;
using System.Threading;
using ColorBlockJam.Core.Settings;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace ColorBlockJam.Views
{
    // The level path above the Play button, going up: the current level's node sits right on the button (unlocked),
    // the next ones climb above it, locked. The view masks its rect, so a node sliding down past the button is gone.
    // Nodes are made once from the template, one more than shown so the next one can slide in from the top.
    public class LevelPathView : BaseView
    {
        // Where the bottom (current) node's centre sits; the other nodes are LevelPathSpacing apart above it.
        [SerializeField] private RectTransform content;
        [SerializeField] private LevelPathNodeView nodeTemplate;

        private readonly List<LevelPathNodeView> _nodes = new List<LevelPathNodeView>();
        private Vector2 _restPosition;
        private bool _built;

        public void SetLevel(int level, LevelSettings settings)
        {
            Build(settings);
            content.DOKill();
            content.anchoredPosition = _restPosition;
            for (var i = 0; i < _nodes.Count; i++)
                _nodes[i].Set(level + i, i > 0);
        }

        // From level - 1 to level: the path slides down one node (the old level's node leaves under the button), then
        // the new level's lock shrinks away.
        public async UniTask PlayAdvance(int level, LevelSettings settings, CancellationToken cancellationToken)
        {
            SetLevel(level - 1, settings);
            var sequence = DOTween.Sequence().SetLink(gameObject)
                .Append(content.DOAnchorPosY(_restPosition.y - settings.LevelPathSpacing, settings.LevelPathSlideDuration)
                    .SetEase(settings.LevelPathSlideEase))
                .Append(_nodes[1].HideLock(settings.LevelPathLockHideDuration));

            // Killed early (cancelled, view destroyed) or done, the path ends up at rest on the new level.
            var finished = new UniTaskCompletionSource();
            sequence.OnKill(() =>
            {
                if (this != null) SetLevel(level, settings);
                finished.TrySetResult();
            });
            using (cancellationToken.Register(() => sequence.Kill()))
            {
                await finished.Task;
            }
        }

        private void Build(LevelSettings settings)
        {
            if (_built) return;
            _built = true;
            _restPosition = content.anchoredPosition;
            nodeTemplate.gameObject.SetActive(false);
            for (var i = 0; i <= settings.LevelPathVisibleNodes; i++)
            {
                var node = Instantiate(nodeTemplate, content);
                node.gameObject.SetActive(true);
                ((RectTransform)node.transform).anchoredPosition = new Vector2(0f, i * settings.LevelPathSpacing);
                node.SetLinkLength(settings.LevelPathSpacing);
                _nodes.Add(node);
            }
        }
    }
}
