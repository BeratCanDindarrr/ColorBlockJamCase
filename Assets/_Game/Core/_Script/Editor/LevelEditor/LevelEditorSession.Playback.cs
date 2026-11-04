using System.Collections.Generic;
using System.Linq;
using ColorBlockJam.Core.Level;
using ColorBlockJam.Core.Settings;
using UnityEditor;
using UnityEngine;

namespace ColorBlockJam.LevelEditor
{
    // Watching a solution of the open level step by step in the scene preview: the solver plays the working copy (unsaved
    // edits included), and the preview shows a copy of it with the first N drags done. The next drag is marked on the
    // board (LevelEditorSceneInput); stepping forward animates it along its path (PlaybackAnimate, set by SandBox editor
    // code) and Play steps on by itself. Any edit, a board press or opening another level stops it.
    public sealed partial class LevelEditorSession
    {
        // Animates one drag in the preview, then calls the action; without a listener steps just jump.
        public static event System.Action<LevelTestStep, System.Action> PlaybackAnimate;

        private LevelTestStep[] _playback;
        private bool _animating;
        private bool _autoPlay;
        private LevelTestVerdict _playbackVerdict;
        private int _playbackIndex;
        private LevelData _playbackLevel;

        internal bool IsPlayingBack => _playback != null;
        internal int PlaybackIndex => _playbackIndex;
        internal int PlaybackCount => _playback?.Length ?? 0;
        internal LevelTestVerdict PlaybackVerdict => _playbackVerdict;
        internal bool IsAnimating => _animating;
        internal bool IsAutoPlaying => _autoPlay;

        // The preview shows this copy while a solution plays; it must not take the Scene view's framing.
        public static bool IsPlaybackLevel(LevelData level) => level != null && level == instance._playbackLevel;

        internal void StartPlayback()
        {
            if (!LevelEditorTests.CanTest || EnsureWorking() == null) return;

            var outcome = LevelEditorTests.Solver(working);
            _playback = outcome.Steps ?? new LevelTestStep[0];
            _playbackVerdict = outcome.Verdict;
            if (_playbackLevel == null)
            {
                _playbackLevel = CreateInstance<LevelData>();
                _playbackLevel.hideFlags = HideFlags.DontSave;
            }

            ShowPlaybackStep(0);
        }

        internal void ShowPlaybackStep(int index)
        {
            if (!IsPlayingBack) return;

            _animating = false;
            _playbackIndex = Mathf.Clamp(index, 0, _playback.Length);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(working), _playbackLevel);
            var blocks = _playbackLevel.Blocks.ToList();
            for (var i = 0; i < _playbackIndex; i++)
            {
                var step = _playback[i];
                var block = blocks.Find(b => b.Id == step.BlockId);
                if (block == null) continue;
                block.Col = step.Col;
                block.Row = step.Row;
                if (step.Exits) blocks.Remove(block);
            }

            _playbackLevel.Blocks = blocks.ToArray();
            WorkingLevelChanged?.Invoke(_playbackLevel);
            RaiseStateChanged();
        }

        // Plays the next drag animated, then shows the board after it (and goes on when Play is on).
        internal void PlayNextStep()
        {
            if (!IsPlayingBack || _animating) return;
            if (_playbackIndex >= _playback.Length)
            {
                _autoPlay = false;
                RaiseStateChanged();
                return;
            }

            if (PlaybackAnimate == null)
            {
                ShowPlaybackStep(_playbackIndex + 1);
                return;
            }

            var played = _playbackIndex;
            _animating = true;
            RaiseStateChanged();
            PlaybackAnimate(_playback[played], () =>
            {
                // Stopped, or stepped elsewhere, while it moved.
                if (!_animating || !IsPlayingBack || _playbackIndex != played) return;
                _animating = false;
                ShowPlaybackStep(played + 1);
                if (_autoPlay) PlayNextStep();
            });
        }

        internal void ToggleAutoPlay()
        {
            _autoPlay = !_autoPlay;
            if (_autoPlay) PlayNextStep();
            else RaiseStateChanged();
        }

        internal void StopPlayback()
        {
            _animating = false;
            _autoPlay = false;
            if (!IsPlayingBack) return;
            _playback = null;
            NotifyWorkingChanged();
            RaiseStateChanged();
        }

        // "Step 3 / 12: B → 2, 4" for the drag about to be played.
        internal string PlaybackCaption()
        {
            if (!IsPlayingBack) return "";
            if (_playbackIndex >= _playback.Length)
                return _playbackVerdict == LevelTestVerdict.Solvable ? $"Done in {_playback.Length} drags" : "Stuck here: no block can leave";

            var step = _playback[_playbackIndex];
            var what = step.Exits ? $"{step.BlockId} leaves from {step.Col}, {step.Row}" : $"{step.BlockId} → {step.Col}, {step.Row}";
            return $"Step {_playbackIndex + 1} / {_playback.Length}: {what}";
        }

        // The next drag on the board: the block's cells now, where it goes, the cells its origin walks through, and
        // whether it leaves from there.
        public static bool GetPlaybackMarks(List<CellCoord> from, List<CellCoord> to, List<CellCoord> path, out bool exits)
        {
            from.Clear();
            to.Clear();
            path.Clear();
            exits = false;
            var session = instance;
            if (!session.IsPlayingBack || session._playbackIndex >= session._playback.Length) return false;

            var step = session._playback[session._playbackIndex];
            var block = session._playbackLevel.Blocks.FirstOrDefault(b => b.Id == step.BlockId);
            if (block == null) return false;

            from.AddRange(LevelValidator.AbsoluteCells(block));
            foreach (var cell in block.Cells) to.Add(new CellCoord(step.Col + cell.Col, step.Row + cell.Row));
            if (step.Path != null) path.AddRange(step.Path);
            exits = step.Exits;
            return true;
        }
    }
}
