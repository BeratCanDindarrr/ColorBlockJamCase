using System.Linq;
using ColorBlockJam.Core.Level;
using ColorBlockJam.Core.Settings;
using NUnit.Framework;
using UnityEngine;

namespace ColorBlockJam.LevelEditor.Tests
{
    public class LevelEditorGateOpsTests
    {
        private LevelData _level;

        [SetUp]
        public void SetUp()
        {
            _level = ScriptableObject.CreateInstance<LevelData>();
            _level.Cols = 6;
            _level.Rows = 6;
            _level.Blocks = new BlockData[0];
            _level.Holes = new CellCoord[0];
            _level.Gates = new[]
            {
                new GateData { Edge = BoardEdge.Top, Index = 1, Length = 4, Color = BlockColor.Red, Depth = 0, Ice = 2 }
            };
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_level);

        private (int Index, int Length)[] Spans() => _level.Gates.Select(g => (g.Index, g.Length)).OrderBy(s => s.Index).ToArray();

        [Test]
        public void RemovingTheFirstSlot_ShortensTheGateFromItsStart()
        {
            LevelEditorGateOps.RemoveSlot(_level, 0, 1);

            Assert.That(Spans(), Is.EqualTo(new[] { (2, 3) }));
        }

        [Test]
        public void RemovingTheLastSlot_ShortensTheGateFromItsEnd()
        {
            LevelEditorGateOps.RemoveSlot(_level, 0, 4);

            Assert.That(Spans(), Is.EqualTo(new[] { (1, 3) }));
        }

        [Test]
        public void RemovingAMiddleSlot_SplitsTheGateKeepingItsColourIceAndDepth()
        {
            LevelEditorGateOps.RemoveSlot(_level, 0, 2);

            Assert.That(Spans(), Is.EqualTo(new[] { (1, 1), (3, 2) }));
            Assert.That(_level.Gates.All(g => g.Color == BlockColor.Red && g.Ice == 2 && g.Depth == 0 && g.Edge == BoardEdge.Top));
        }

        [Test]
        public void RemovingTheOnlySlot_DeletesTheGate()
        {
            _level.Gates[0].Length = 1;

            LevelEditorGateOps.RemoveSlot(_level, 0, 1);

            Assert.That(_level.Gates, Is.Empty);
        }

        [Test]
        public void RemovingASlotOutsideTheGate_ChangesNothing()
        {
            Assert.That(LevelEditorGateOps.RemoveSlot(_level, 0, 5), Is.False);
            Assert.That(Spans(), Is.EqualTo(new[] { (1, 4) }));
        }
    }
}
