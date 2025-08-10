using System.Threading;
using System.Threading.Tasks;
using Sudoku.Models;

namespace Sudoku.Services
{
    public interface IPuzzleGeneratorService
    {
        /// <summary>
        /// Generates a new puzzle according to the supplied options.  Returns a new
        /// SudokuBoard pre?populated with the puzzle and solution.
        /// </summary>
        Task<SudokuBoard> GeneratePuzzleAsync(PuzzleOptions options, CancellationToken cancellationToken = default);
    }
}
