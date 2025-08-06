using System;
using System.Collections.Generic;

namespace Sudoku
{
    public class SudokuSolver
    {
        public bool Solve(int[,] grid)
        {
            for (var row = 0; row < 9; row++)
                for (var col = 0; col < 9; col++)
                    if (grid[row, col] == 0)
                    {
                        for (var num = 1; num <= 9; num++)
                            if (IsSafe(grid, row, col, num))
                            {
                                grid[row, col] = num;
                                if (Solve(grid))
                                    return true;
                                grid[row, col] = 0;
                            }
                        return false;
                    }
            return true;
        }

        public int CountSolutions(int[,] grid)
        {
            int count = 0;
            CountSolutionsHelper(grid, ref count);
            return count;
        }

        private void CountSolutionsHelper(int[,] grid, ref int count)
        {
            for (var row = 0; row < 9; row++)
                for (var col = 0; col < 9; col++)
                    if (grid[row, col] == 0)
                    {
                        for (var num = 1; num <= 9; num++)
                            if (IsSafe(grid, row, col, num))
                            {
                                grid[row, col] = num;
                                CountSolutionsHelper(grid, ref count);
                                grid[row, col] = 0;
                            }
                        return;
                    }
            count++;
        }

        public bool IsSafe(int[,] grid, int row, int col, int num)
        {
            for (var i = 0; i < 9; i++)
                if (grid[row, i] == num || grid[i, col] == num)
                    return false;
            var boxRow = row - row % 3;
            var boxCol = col - col % 3;
            for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                    if (grid[boxRow + i, boxCol + j] == num)
                        return false;
            return true;
        }
    }
}
