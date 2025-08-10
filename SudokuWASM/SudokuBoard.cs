using System;
using System.Linq;
using System.Collections.Generic;

namespace Sudoku
{
    public class SudokuBoard
    {
        public int[,] Grid { get; set; } = new int[9, 9];
        public int[,] Solution { get; set; } = new int[9, 9]; // Store the complete solution
        public bool[,] FixedCells { get; private set; } = new bool[9, 9];
        public HashSet<int>[,] Notes { get; private set; } = new HashSet<int>[9, 9];
        private static Random rng = new Random();
        public bool[,] HintCells { get; private set; } = new bool[9, 9];
        public bool[,] CorrectlySolvedCells { get; private set; } = new bool[9, 9]; // Track correctly solved cells

        public SudokuBoard(bool skipGeneration = false)
        {
            InitializeNotes();
            if (!skipGeneration)
            {
                GenerateNewPuzzle();
            }
        }

        private void InitializeNotes()
        {
            for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                Notes[r, c] = new HashSet<int>();
        }

        public void GenerateNewPuzzle(int clues = 30)
        {
            int[,] full = new int[9, 9];
            FillGrid(full);
            
            // Store the complete solution
            Solution = (int[,])full.Clone();
            
            int[,] puzzle = (int[,])full.Clone();
            RemoveNumbers(puzzle, clues);
            InitializeNotes();
            for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
            {
                Grid[r, c] = puzzle[r, c];
                FixedCells[r, c] = puzzle[r, c] != 0;
                HintCells[r, c] = false; // Clear all hints on new puzzle
                CorrectlySolvedCells[r, c] = puzzle[r, c] != 0; // Initial clues are correctly solved
                Notes[r, c].Clear();
            }
        }

        private bool FillGrid(int[,] grid)
        {
            for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                if (grid[row, col] == 0)
                {
                    var nums = Enumerable.Range(1, 9).OrderBy(_ => rng.Next()).ToArray();
                    foreach (var num in nums)
                    {
                        if (new SudokuSolver().IsSafe(grid, row, col, num))
                        {
                            grid[row, col] = num;
                            if (FillGrid(grid))
                                return true;
                            grid[row, col] = 0;
                        }
                    }
                    return false;
                }
            }
            return true;
        }

        private void RemoveNumbers(int[,] grid, int clues)
        {
            var cells = Enumerable.Range(0, 81).OrderBy(_ => rng.Next()).ToList();
            int removed = 0;
            foreach (var idx in cells)
            {
                int row = idx / 9, col = idx % 9;
                if (grid[row, col] == 0) continue;
                int backup = grid[row, col];
                grid[row, col] = 0;
                var copy = (int[,])grid.Clone();
                if (new SudokuSolver().CountSolutions(copy) != 1)
                {
                    grid[row, col] = backup;
                }
                else
                {
                    removed++;
                    if (81 - removed <= clues) break;
                }
            }
        }

        public bool IsValidMove(int row, int col, int value)
        {
            if (value < 1 || value > 9) return false;
            for (int i = 0; i < 9; i++)
            {
                if (i != col && Grid[row, i] == value) return false;
                if (i != row && Grid[i, col] == value) return false;
            }
            int startRow = (row / 3) * 3;
            int startCol = (col / 3) * 3;
            for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                int checkRow = startRow + r;
                int checkCol = startCol + c;
                if ((checkRow != row || checkCol != col) && Grid[checkRow, checkCol] == value)
                    return false;
            }
            return true;
        }

        // New method to check if a move matches the solution
        public bool IsCorrectMove(int row, int col, int value)
        {
            if (row < 0 || row >= 9 || col < 0 || col >= 9) return false;
            return Solution[row, col] == value;
        }

        // Method to check if a cell can be edited (not fixed and not correctly solved)
        public bool CanEditCell(int row, int col)
        {
            if (row < 0 || row >= 9 || col < 0 || col >= 9) return false;
            return !FixedCells[row, col] && !CorrectlySolvedCells[row, col];
        }

        // Method to get the solution for debugging
        public string GetSolutionString()
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("=== SOLUTION ===");
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    result.Append($"{Solution[r, c]} ");
                    if (c == 2 || c == 5) result.Append("| ");
                }
                result.AppendLine();
                if (r == 2 || r == 5) result.AppendLine("------+-------+------");
            }
            result.AppendLine("================");
            return result.ToString();
        }

        public bool IsComplete()
        {
            for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
                if (Grid[r, c] == 0) return false;
            return true;
        }

        public void SetCell(int row, int col, int value)
        {
            // Prevent altering correctly solved cells or fixed cells
            if (!CanEditCell(row, col))
            {
                return; // Fail silently as requested
            }

            // If setting to 0 (erasing), always allow it for non-fixed cells
            if (value == 0)
            {
                Grid[row, col] = value;
                CorrectlySolvedCells[row, col] = false;
                Notes[row, col].Clear();
                return;
            }

            Grid[row, col] = value;
            Notes[row, col].Clear(); // Clear notes when value is set

            // Check if this move is correct and mark as correctly solved
            if (IsCorrectMove(row, col, value))
            {
                CorrectlySolvedCells[row, col] = true;
            }
            else
            {
                CorrectlySolvedCells[row, col] = false;
            }

            if (value >= 1 && value <= 9)
            {
                // Remove this value from notes in the same row and column
                for (int i = 0; i < 9; i++)
                {
                    if (i != col)
                        Notes[row, i].Remove(value);
                    if (i != row)
                        Notes[i, col].Remove(value);
                }

                // Remove this value from notes in the same 3x3 box
                int startRow = (row / 3) * 3;
                int startCol = (col / 3) * 3;
                for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                {
                    int checkRow = startRow + r;
                    int checkCol = startCol + c;
                    if ((checkRow != row || checkCol != col))
                        Notes[checkRow, checkCol].Remove(value);
                }
            }
        }

        public void AddHint(int row, int col, int value)
        {
            if (!FixedCells[row, col])
            {
                Grid[row, col] = value;
                FixedCells[row, col] = true;
                HintCells[row, col] = true;
                CorrectlySolvedCells[row, col] = true; // Hints are always correct
                Notes[row, col].Clear();
            }
        }

        public void AddNote(int row, int col, int value)
        {
            if (CanEditCell(row, col) && value >= 1 && value <= 9 && row >= 0 && row < 9 && col >= 0 && col < 9)
                Notes[row, col].Add(value);
        }

        public void RemoveNote(int row, int col, int value)
        {
            if (CanEditCell(row, col) && value >= 1 && value <= 9 && row >= 0 && row < 9 && col >= 0 && col < 9)
                Notes[row, col].Remove(value);
        }

        public void ToggleNote(int row, int col, int value)
        {
            if (CanEditCell(row, col) && value >= 1 && value <= 9 && row >= 0 && row < 9 && col >= 0 && col < 9)
            {
                if (Notes[row, col].Contains(value))
                    Notes[row, col].Remove(value);
                else
                    Notes[row, col].Add(value);
            }
        }

        public void ClearNotes(int row, int col)
        {
            if (CanEditCell(row, col) && row >= 0 && row < 9 && col >= 0 && col < 9)
                Notes[row, col].Clear();
        }

        public void Reset()
        {
            new SudokuBoard().CopyTo(this);
            InitializeNotes();
        }

        public void CopyTo(SudokuBoard other)
        {
            other.InitializeNotes();
            for (int r = 0; r < 9; r++)
            for (int c = 0; c < 9; c++)
            {
                other.Grid[r, c] = this.Grid[r, c];
                other.Solution[r, c] = this.Solution[r, c];
                other.FixedCells[r, c] = this.FixedCells[r, c];
                other.Notes[r, c] = new HashSet<int>(this.Notes[r, c]);
                other.HintCells[r, c] = this.HintCells[r, c];
                other.CorrectlySolvedCells[r, c] = this.CorrectlySolvedCells[r, c];
            }
        }
    }
}
