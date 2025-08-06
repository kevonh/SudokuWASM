using Sudoku.Models;

namespace Sudoku.Services;

/// <summary>
/// Service to handle CSS class generation for Sudoku cells
/// </summary>
public class CellStylingService
{
    private readonly SudokuBoard? board;
    private readonly bool[,] wrongCells;
    private readonly (int row, int col)? selectedCell;

    public CellStylingService(SudokuBoard? board, bool[,] wrongCells, (int row, int col)? selectedCell)
    {
        this.board = board;
        this.wrongCells = wrongCells;
        this.selectedCell = selectedCell;
    }

    public string GetCellCSS(int row, int col)
    {
        if (board == null) return "";
        
        var isSelected = IsSelectedCell(row, col);
        var baseClasses = GetBaseCellClasses();
        var cellTypeClasses = GetCellTypeClasses(row, col, isSelected);
        var highlightClasses = GetHighlightClasses(row, col, isSelected);
        
        return string.Join(" ", baseClasses.Concat(cellTypeClasses).Concat(highlightClasses));
    }

    private bool IsSelectedCell(int row, int col)
    {
        return selectedCell?.row == row && selectedCell?.col == col;
    }

    private string[] GetBaseCellClasses()
    {
        return ["w-10", "h-10", "sm:w-12", "sm:h-12", "md:w-16", "md:h-16", "lg:w-20", "lg:h-20", 
                "flex", "items-center", "justify-center", "cursor-pointer", "text-base", "sm:text-lg", 
                "font-medium", "transition-all", "duration-200", "touch-manipulation", "select-none"];
    }

    private string[] GetCellTypeClasses(int row, int col, bool isSelected)
    {
        if (board!.HintCells[row, col])
            return GetHintCellClasses(isSelected);
        
        if (board.FixedCells[row, col])
            return GetFixedCellClasses(isSelected);
        
        if (board.CorrectlySolvedCells[row, col])
            return GetCorrectSolvedCellClasses(isSelected);
        
        return GetDefaultCellClasses(isSelected);
    }

    private string[] GetHintCellClasses(bool isSelected)
    {
        var classes = new List<string> { "text-green-800", "font-bold" };
        if (!isSelected)
            classes.Add("bg-white");
        return classes.ToArray();
    }

    private string[] GetFixedCellClasses(bool isSelected)
    {
        var classes = new List<string> { "text-gray-800", "font-bold" };
        if (!isSelected)
            classes.Add("bg-gray-100");
        return classes.ToArray();
    }

    private string[] GetCorrectSolvedCellClasses(bool isSelected)
    {
        var classes = new List<string> { "text-blue-900", "font-bold" };
        if (!isSelected)
            classes.Add("bg-white");
        return classes.ToArray();
    }

    private string[] GetDefaultCellClasses(bool isSelected)
    {
        if (isSelected)
            return ["text-blue-600"];
        
        return ["bg-white", "text-blue-600", "hover:bg-blue-50"];
    }

    private string[] GetHighlightClasses(int row, int col, bool isSelected)
    {
        return (isSelected, wrongCells[row, col], IsNumberHighlighted(row, col), IsMasterHighlighted(row, col)) switch
        {
            (true, _, _, _)            => ["bg-blue-200", "z-10"],
            (false, true, _, _)        => ["bg-red-100", "text-red-700"],
            (false, false, true, _)    => ["!bg-blue-400"],
            (false, false, false, true) => ["!bg-blue-300"],
            _                          => []
        };
    }

    private bool IsNumberHighlighted(int row, int col)
    {
        var selectedValue = GetSelectedCellValue();
        return selectedValue.HasValue && 
               selectedValue.Value != 0 && 
               board!.Grid[row, col] == selectedValue.Value;
    }

    private bool IsMasterHighlighted(int row, int col)
    {
        if (!selectedCell.HasValue || !IsSelectedCellSolved()) 
            return false;
        
        var (selectedRow, selectedCol) = selectedCell.Value;
        return row == selectedRow || 
               col == selectedCol || 
               IsInSameBlock(row, col, selectedRow, selectedCol);
    }

    private bool IsSelectedCellSolved()
    {
        if (!selectedCell.HasValue) return false;
        var (row, col) = selectedCell.Value;
        return board!.FixedCells[row, col] || board.Grid[row, col] != 0;
    }

    private int? GetSelectedCellValue()
    {
        if (!selectedCell.HasValue || !IsSelectedCellSolved()) return null;
        var (row, col) = selectedCell.Value;
        return board!.Grid[row, col];
    }

    private bool IsInSameBlock(int row1, int col1, int row2, int col2)
    {
        return row1 / 3 == row2 / 3 && col1 / 3 == col2 / 3;
    }
}