using System.Linq;
using ColorBlockJam.Core.Settings;
using ColorBlockJam.LevelEditor;
using ColorBlockJam.SandBox.Board;
using UnityEditor;

namespace ColorBlockJam.SandBox.Editor
{
    // Hands the level editor's tests (Core editor code, which cannot see SandBox) the board's own solver.
    [InitializeOnLoad]
    internal static class LevelSolverBridge
    {
        static LevelSolverBridge()
        {
            LevelEditorTests.Solver = Solve;
        }

        private static LevelTestOutcome Solve(LevelData level)
        {
            var result = LevelSolver.Solve(LevelDefinitionFactory.Create(level));
            return new LevelTestOutcome
            {
                Verdict = Map(result.Verdict),
                Drags = result.Steps.Count,
                AsideMoves = result.Steps.Count(s => !s.Exits),
                StuckBlocks = result.StuckBlocks.ToArray(),
                Steps = result.Steps.Select(s => new LevelTestStep
                {
                    BlockId = s.BlockId, Col = s.To.Col, Row = s.To.Row, Exits = s.Exits, Path = s.Path.ToArray(),
                    ExitEdge = s.ExitEdge
                }).ToArray()
            };
        }

        private static LevelTestVerdict Map(SolveVerdict verdict)
        {
            switch (verdict)
            {
                case SolveVerdict.Solvable: return LevelTestVerdict.Solvable;
                case SolveVerdict.Unsolvable: return LevelTestVerdict.Unsolvable;
                default: return LevelTestVerdict.TooBig;
            }
        }
    }
}
