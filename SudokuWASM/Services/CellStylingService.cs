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
    private readonly bool isMobile;

    public CellStylingService(SudokuBoard? board, bool[,] wrongCells, (int row, int col)? selectedCell, bool isMobile = false)
    {
        this.board = board;
        this.wrongCells = wrongCells;
        this.selectedCell = selectedCell;
        this.isMobile = isMobile;
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

    public string GetCellTextCSS(int row, int col)
    {
        if (board == null) return "";
        
        var textSizeClasses = GetTextSizeClasses(row, col);
        var textColorClasses = GetTextColorClasses(row, col);
        
        return string.Join(" ", textSizeClasses.Concat(textColorClasses));
    }

    public string GetNotesTextCSS()
    {
        // Centralized notes text sizing - increased for better visibility
        return isMobile ? "text-[10px] sm:text-xs" : "text-sm";
    }

    private bool IsSelectedCell(int row, int col)
    {
        return selectedCell?.row == row && selectedCell?.col == col;
    }

    private string[] GetBaseCellClasses()
    {
        // Removed text sizing from base classes - handle separately for better control
        return ["w-10", "h-10", "sm:w-12", "sm:h-12", "md:w-16", "md:h-16", "lg:w-20", "lg:h-20", 
                "flex", "items-center", "justify-center", "cursor-pointer",
                "font-medium", "transition-all", "duration-200", "touch-manipulation", "select-none"];
    }

    private string[] GetTextSizeClasses(int row, int col)
    {
        // Centralized text sizing logic with larger sizes to better fill the cell areas
        // Mobile: w-10/h-10 (40px) -> sm:w-12/h-12 (48px) -> md:w-16/h-16 (64px)
        // Desktop: w-10/h-10 (40px) -> sm:w-12/h-12 (48px) -> md:w-16/h-16 (64px) -> lg:w-20/h-20 (80px)
        
        if (isMobile)
        {
            // Mobile sizes: increased from text-sm/base/lg to text-lg/xl/2xl for better fill
            return ["text-2xl", "sm:text-3xl", "md:text-4xl"];
        }
        else
        {
            // Desktop sizes: increased from text-base/lg to text-xl/2xl/3xl for better fill
            return ["text-3xl", "sm:text-4xl", "md:text-5xl", "lg:text-6xl"];
        }
    }

    private string[] GetTextColorClasses(int row, int col)
    {
        // Check if this cell has a wrong value
        if (wrongCells[row, col] && board!.Grid[row, col] != 0)
        {
            return ["text-red-600", "font-bold"];
        }
        
        if (board!.HintCells[row, col])
            return ["text-green-800", "font-bold"];
        
        if (board.FixedCells[row, col])
            return ["text-gray-800", "font-bold"];
        
        if (board.CorrectlySolvedCells[row, col])
            return ["text-blue-900", "font-bold"];
        
        return ["text-blue-600"];
    }

    private string[] GetCellTypeClasses(int row, int col, bool isSelected)
    {
        // Background colors only - text colors handled in GetTextColorClasses
        if (wrongCells[row, col] && board!.Grid[row, col] != 0 && !isSelected)
        {
            return ["bg-red-50"];
        }
        
        if (board!.HintCells[row, col] && !isSelected)
            return ["bg-white"];
        
        if (board.FixedCells[row, col] && !isSelected)
            return ["bg-gray-100"];
        
        if (board.CorrectlySolvedCells[row, col] && !isSelected)
            return ["bg-white"];
        
        if (isSelected)
            return [];
        
        return ["bg-white", "hover:bg-blue-50"];
    }

    private string[] GetHighlightClasses(int row, int col, bool isSelected)
    {
        return (isSelected, wrongCells[row, col], IsNumberHighlighted(row, col), IsMasterHighlighted(row, col)) switch
        {
            (true, _, _, _)            => ["bg-blue-200", "z-10"],
            (false, true, _, _)        => ["bg-red-100"],
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