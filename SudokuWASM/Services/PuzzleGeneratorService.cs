using Sudoku.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sudoku.Services
{
    public class PuzzleGeneratorService : IPuzzleGeneratorService
    {
        private readonly Random random;

        public PuzzleGeneratorService()
        {
            random = new Random();
        }

        public Task<SudokuBoard> GeneratePuzzleAsync(PuzzleOptions options, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                // Determine clue count from difficulty or explicit override
                int clues = options.ClueCount ?? options.Difficulty switch
                {
                    DifficultyLevel.Easy => 40,
                    DifficultyLevel.Medium => 30,
                    DifficultyLevel.Hard => 25,
                    DifficultyLevel.Expert => 20,
                    _ => 30
                };

                // Initialise a new board but don't auto?generate puzzle
                var board = new SudokuBoard(skipGeneration: true);

                // Fill the grid completely
                int[,] full = new int[9, 9];
                FillGrid(full, options.Seed);

                // Copy solution into board
                board.Solution = (int[,])full.Clone();

                // Remove clues while maintaining symmetry if requested
                int[,] puzzle = (int[,])full.Clone();
                RemoveNumbersWithSymmetry(puzzle, clues, options.Symmetry);

                // Populate board state
                board.Grid = puzzle;
                for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                {
                    board.FixedCells[r, c] = puzzle[r, c] != 0;
                    board.HintCells[r, c] = false;
                    board.CorrectlySolvedCells[r, c] = puzzle[r, c] != 0;
                }
                return board;
            }, cancellationToken);
        }

        private void FillGrid(int[,] grid, int? seed)
        {
            var rng = seed.HasValue ? new Random(seed.Value) : random;
            if (Fill(grid))
                return;

            bool Fill(int[,] g)
            {
                for (int row = 0; row < 9; row++)
                for (int col = 0; col < 9; col++)
                {
                    if (g[row, col] == 0)
                    {
                        var nums = Enumerable.Range(1, 9).OrderBy(_ => rng.Next()).ToArray();
                        foreach (var num in nums)
                        {
                            if (new SudokuSolver().IsSafe(g, row, col, num))
                            {
                                g[row, col] = num;
                                if (Fill(g)) return true;
                                g[row, col] = 0;
                            }
                        }
                        return false;
                    }
                }
                return true;
            }
        }

        private void RemoveNumbersWithSymmetry(int[,] grid, int clues, SymmetryStyle symmetry)
        {
            var cells = Enumerable.Range(0, 81).OrderBy(_ => random.Next()).ToList();
            int removed = 0;
            foreach (var idx in cells)
            {
                int row = idx / 9;
                int col = idx % 9;
                if (grid[row, col] == 0) continue;

                // backup both primary and symmetric cells
                int backup = grid[row, col];
                (int symRow, int symCol) = symmetry switch
                {
                    SymmetryStyle.Rotational => (8 - row, 8 - col),
                    SymmetryStyle.Diagonal => (col, row),
                    SymmetryStyle.Both => (8 - row, 8 - col),
                    _ => (row, col)
                };
                int backup2 = grid[symRow, symCol];
                grid[row, col] = 0;
                grid[symRow, symCol] = 0;

                // verify unique solution
                var copy = (int[,])grid.Clone();
                if (new SudokuSolver().CountSolutions(copy) != 1)
                {
                    grid[row, col] = backup;
                    grid[symRow, symCol] = backup2;
                }
                else
                {
                    removed += (row == symRow && col == symCol) ? 1 : 2;
                    if (81 - removed <= clues) break;
                }
            }
        }
    }
}
