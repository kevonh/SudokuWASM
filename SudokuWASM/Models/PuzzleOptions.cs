// File: Models/PuzzleOptions.cs
using System;

namespace Sudoku.Models
{
    public enum PuzzleVariant
    {
        Standard9x9,
        Mini6x6,
        Mega16x16,
        Irregular
    }

    public enum SymmetryStyle
    {
        None,
        Rotational,
        Diagonal,
        Both
    }

    public enum DifficultyLevel
    {
        Custom,
        Easy,
        Medium,
        Hard,
        Expert
    }

    public class PuzzleOptions
    {
        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;
        public SymmetryStyle Symmetry { get; set; } = SymmetryStyle.None;
        public PuzzleVariant Variant { get; set; } = PuzzleVariant.Standard9x9;
        public int? Seed { get; set; } = null;      // allow deterministic puzzles
        public int? ClueCount { get; set; } = null; // override default clue count
    }
}
