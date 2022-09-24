using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

/// <summary>
/// Class responsible for controlling the sudoku game
/// </summary>
public sealed class SudokuController : MonoBehaviour
{
    private static SudokuController instance;
    public static SudokuController Instance { get { return instance; } }

#if UNITY_EDITOR
    [SerializeField, Tooltip("The number of times to repeat the build")]
    private int iterations = 1;
    private readonly List<(long, long, int)> iterationTimes = new();
#endif

    private int[] cellPossibleValues = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

    private const int AREA_SIZE = 9;
    private const int TOTAL_CELLS = 81;

    private Dictionary<int, int> startPoint = new();

    private ScoreBoard scoreBoard;

    private NumberCell[] allCells;
    private NumberCell[][] allAreas = new NumberCell[27][];

    /// <summary>
    /// The number the on click action will apply
    /// </summary>
    [HideInInspector]
    public int NumberSelected = 1;

    /// <summary>
    /// Whether the on click action should be to annotate (as opposed to replace)
    /// </summary>
    [HideInInspector]
    public bool Annotate = false;

    private delegate bool InferMethod(NumberCell cell);
    /// <summary>
    /// The method used to determine whether a cells value can be inferred.
    /// </summary>
    private InferMethod inferMethod;

    private E_SessionDifficulty difficulty;

    /// <summary>
    /// Value = target number of EMPTY cells at start.
    /// </summary>
    private enum E_SessionDifficulty
    {
        Easy = 20,
        Medium = 40,
        Hard = 60,
    }

    private void Awake()
    {
        if (instance != null && instance != this)
            Destroy(gameObject);
        else
            instance = this;
    }

    private void Start()
    {
        scoreBoard = transform.Find("ScoreBoard").GetComponent<ScoreBoard>();

        // Get all the cells (81 cells total)
        allCells = transform.Find("SudokuBoard/Grid").GetComponentsInChildren<NumberCell>();

        if (allCells.Length != TOTAL_CELLS)
        {
            Debug.LogError($"Grid must contain exactly {TOTAL_CELLS} cells");
            return;
        }

        // Add Horizontal rows
        for (int i = 0; i < AREA_SIZE; i++)
        {
            allAreas[i] = allCells[(i * AREA_SIZE)..((i + 1) * AREA_SIZE)];
        }

        // Add Vertical columns
        for (int i = 0; i < AREA_SIZE; i++)
        {
            // Every cell where: cellIndex % 9 == i
            allAreas[i + 9] = allCells.ToList().Where((cell, cellIndex) => cellIndex % AREA_SIZE == i).ToArray();
        }

        // Add 'Squares' of cells
        allAreas[18] = new NumberCell[9] { allCells[0], allCells[1], allCells[2], allCells[9], allCells[10], allCells[11], allCells[18], allCells[19], allCells[20] };
        allAreas[19] = new NumberCell[9] { allCells[3], allCells[4], allCells[5], allCells[12], allCells[13], allCells[14], allCells[21], allCells[22], allCells[23] };
        allAreas[20] = new NumberCell[9] { allCells[6], allCells[7], allCells[8], allCells[15], allCells[16], allCells[17], allCells[24], allCells[25], allCells[26] };

        allAreas[21] = new NumberCell[9] { allCells[27], allCells[28], allCells[29], allCells[36], allCells[37], allCells[38], allCells[45], allCells[46], allCells[47] };
        allAreas[22] = new NumberCell[9] { allCells[30], allCells[31], allCells[32], allCells[39], allCells[40], allCells[41], allCells[48], allCells[49], allCells[50] };
        allAreas[23] = new NumberCell[9] { allCells[33], allCells[34], allCells[35], allCells[42], allCells[43], allCells[44], allCells[51], allCells[52], allCells[53] };

        allAreas[24] = new NumberCell[9] { allCells[54], allCells[55], allCells[56], allCells[63], allCells[64], allCells[65], allCells[72], allCells[73], allCells[74] };
        allAreas[25] = new NumberCell[9] { allCells[57], allCells[58], allCells[59], allCells[66], allCells[67], allCells[68], allCells[75], allCells[76], allCells[77] };
        allAreas[26] = new NumberCell[9] { allCells[60], allCells[61], allCells[62], allCells[69], allCells[70], allCells[71], allCells[78], allCells[79], allCells[80] };
    }

    /// <summary>
    /// Starts a new game session with the specified <paramref name="sessionDifficulty"/>.
    /// </summary>
    /// <param name="sessionDifficulty"></param>
    public void NewSession(int sessionDifficulty)
    {
        difficulty = (E_SessionDifficulty)sessionDifficulty;
        scoreBoard.SetDifficultyDisplay(difficulty.ToString());

        inferMethod = difficulty == E_SessionDifficulty.Hard ? IsInferable : IsDirectlyInferable;

#if UNITY_EDITOR
        for (int i = 0; i < iterations; i++)
            GenerateGame();

        long averageBuildTime = 0L;
        long averageOtherTime = 0L;
        int averageCellsToFill = 0;

        for (int i = 0; i < iterationTimes.Count(); i++)
        {
            averageBuildTime += iterationTimes[i].Item1;
            averageOtherTime += iterationTimes[i].Item2;
            averageCellsToFill += iterationTimes[i].Item3;
        }

        averageBuildTime /= iterationTimes.Count();
        averageOtherTime /= iterationTimes.Count();
        averageCellsToFill /= iterationTimes.Count();

        Debug.Log($"Build: {averageBuildTime}  -  Other: {averageOtherTime}  -  Total: {averageBuildTime + averageOtherTime}  - Empty: {averageCellsToFill}");
#else
        GenerateGame();
#endif
    }

    /// <summary>
    /// Randommly generates a new game of <see cref="difficulty"/>.
    /// </summary>
    private void GenerateGame()
    {
        scoreBoard.ResetScoreBoard();

#if UNITY_EDITOR
        System.Diagnostics.Stopwatch buildTimer = new();
        System.Diagnostics.Stopwatch otherTimer = new();

        do
        {
            buildTimer.Start();
            BuildGame();
            buildTimer.Stop();

            // Guard clause if already removed enough
            if (allCells.Length - startPoint.Count == (int)difficulty)
                break;

            otherTimer.Start();
            AdvancedRemoveCellValue();
            otherTimer.Stop();
        } while (allCells.Length - startPoint.Count < (int)difficulty - 5);

        CheckSolution();

        var totalFilledCells = scoreBoard.NumCellsFilled + startPoint.Count;
        var cellsRemaining = allCells.Length - totalFilledCells;

        // Add elapsed time to iteration times
        iterationTimes.Add((buildTimer.ElapsedMilliseconds, otherTimer.ElapsedMilliseconds, cellsRemaining));
#else
        do
        {
            BuildGame();

            // Guard clause if already removed enough
            if (allCells.Length - startPoint.Count == (int)difficulty)
                break;

            AdvancedRemoveCellValue();
        } while (allCells.Length - startPoint.Count < (int)difficulty - 5);
#endif
    }

    /// <summary>
    /// Just for testing - use GenerateFinishedSolution() instead
    /// </summary>
    [System.Obsolete("Just for testing - use GenerateFinishedSolution() instead")]
    private void GenerateFinishedSolution2()
    {
        EmptyAllCells();

        var allRows = allAreas[0..9];

        List<int> possibleValues = new(cellPossibleValues);

        // Fill top-left cube
        for (int rowIndex = 0; rowIndex < 3; rowIndex++)
        {
            for (int cellIndex = 0; cellIndex < 3; cellIndex++)
            {
                var cell = allRows[rowIndex][cellIndex];

                // Get a random value to fill the cell with
                var valueIndex = Random.Range(0, possibleValues.Count);
                cell.CellValue = possibleValues[valueIndex];
                possibleValues.RemoveAt(valueIndex);
            }
        }

        // Fill top-middle cube
        allRows[0][3].CellValue = allRows[1][0].CellValue;
        allRows[0][4].CellValue = allRows[1][2].CellValue;
        allRows[0][5].CellValue = allRows[2][0].CellValue;

        allRows[1][3].CellValue = allRows[2][1].CellValue;
        allRows[1][4].CellValue = allRows[2][2].CellValue;
        allRows[1][5].CellValue = allRows[0][1].CellValue;

        allRows[2][3].CellValue = allRows[0][0].CellValue;
        allRows[2][4].CellValue = allRows[0][2].CellValue;
        allRows[2][5].CellValue = allRows[1][1].CellValue;

        // Fill top-right cube
        for (int rowIndex = 0; rowIndex < 3; rowIndex++)
        {
            for (int cellIndex = 6; cellIndex < allRows[rowIndex].Length; cellIndex++)
            {
                var cell = allRows[rowIndex][cellIndex];

                // Get a random value to fill the cell with
                var possible = GetCellDirectPossibleValues(cell);
                cell.CellValue = possible[Random.Range(0, possible.Count)];
            }
        }

        possibleValues = new(cellPossibleValues);
        possibleValues.Remove(allRows[0][0].CellValue);
        possibleValues.Remove(allRows[1][0].CellValue);
        possibleValues.Remove(allRows[2][0].CellValue);


        // Fill left-hand column
        for (int rowIndex = 3; rowIndex < allRows.Length; rowIndex++)
        {
            var cell = allRows[rowIndex][0];

            if (!cell.IsEmpty)
            {
                possibleValues.Remove(cell.CellValue);
                continue;
            }

            // Get a random value to fill the cell with
            var valueIndex = Random.Range(0, possibleValues.Count);
            cell.CellValue = possibleValues[valueIndex];
            possibleValues.RemoveAt(valueIndex);
        }

        // Save this as start point
        SaveStartPoint();

        // Fill the rest of the grid
        // Loop over rows
        for (int rowIndex = 3; rowIndex < allRows.Length; rowIndex++)
        {
            bool startOver = false;
            int rowAttempts = 0;

            // Loop over cells in row
            for (int cellIndex = 1; cellIndex < allRows[rowIndex].Length; cellIndex++)
            {
                if (!allRows[rowIndex][cellIndex].IsEmpty)
                    continue;

                var possValues = GetCellDirectPossibleValues(allRows[rowIndex][cellIndex]);

                // If no possible values, restart the row
                if (possValues.Count == 0)
                {
                    // If tried to build the row 10 times unsucesfully, restart puzzle
                    if (rowAttempts > 10)
                    {
                        startOver = true;
                        break;
                    }

                    rowAttempts++;
                    cellIndex = -1;
                    continue;
                }

                // Assign current cell a random value from those possible
                allRows[rowIndex][cellIndex].CellValue = possValues[Random.Range(0, possValues.Count)];
            }

            // Handle start over
            if (startOver)
            {
                GenerateFinishedSolution2();
            }
        }
    }

    /// <summary>
    /// Fills the board with legal values (Note: legal != uniquely solvable)
    /// </summary>
    private void GenerateFinishedSolution()
    {
        // Esnure all cells are empty to start
        EmptyAllCells();

        var allRows = allAreas[0..9];

        // Loop over rows
        for (int rowIndex = 0; rowIndex < allRows.Length; rowIndex++)
        {
            bool startOver = false;
            int rowAttempts = 0;

            // Loop over cells in row
            for (int cellIndex = 0; cellIndex < allRows[rowIndex].Length; cellIndex++)
            {
                if (!allRows[rowIndex][cellIndex].IsEmpty)
                    continue;

                var possibleValues = GetCellDirectPossibleValues(allRows[rowIndex][cellIndex]);

                // If no possible values, restart the row
                if (possibleValues.Count == 0)
                {
                    // If tried to build the row 10 times unsucesfully, restart puzzle
                    if (rowAttempts > 10)
                    {
                        startOver = true;
                        break;
                    }

                    rowAttempts++;
                    cellIndex = -1;
                    continue;
                }

                // Assign current cell a random value from those possible
                allRows[rowIndex][cellIndex].SetCellValueWithoutSynch(possibleValues[Random.Range(0, possibleValues.Count)]);
            }

            // Handle start over
            if (startOver)
            {
                EmptyAllCells();
                rowIndex = -1;
            }
        }
    }

    private void SaveStartPoint()
    {
        startPoint = new();
        for (int i = 0; i < allCells.Length; i++)
        {
            if (!allCells[i].IsEmpty)
            {
                startPoint.Add(i, allCells[i].CellValue);
                allCells[i].IsStartingValue = true;
            }
        }
    }

    /// <summary>
    /// Attempt to build the game board untill a solvable board is built.
    /// </summary>
    private void BuildGame()
    {
        do
        {
            GenerateFinishedSolution();
            RemoveCellValues();
            SaveStartPoint();
        } while (!Solve());

        ResetToStart();
    }

    /// <summary>
    /// Attempts to solve the current puzzle
    /// </summary>
    /// <returns>true - if solved, false if not solved</returns>
    public bool Solve()
    {
        int count = 0;

        while (!scoreBoard.HasFinished && count < 6)
        {
            // Loop over all cells
            for (int cellIndex = 0; cellIndex < allCells.Length; cellIndex++)
            {
                var cell = allCells[cellIndex];

                // Skip filled cells
                if (!cell.IsEmpty)
                    continue;

                var possible = GetCellDirectPossibleValues(cell);

                if (possible.Count == 1)
                {
                    cell.CellValue = possible[0];
                }
            }

            // Loop over all areas
            for (int i = 0; i < allAreas.Length; i++)
            {
                // Check the area for value occurance
                CheckAreaValueOccurance(allAreas[i], true);
            }

            CheckSolution();
            count++;
        }

        return scoreBoard.HasFinished;
    }

    /// <summary>
    /// Attempts to remove n cells from the board (n = difficulty).
    /// </summary>
    private void RemoveCellValues()
    {
        var availableCells = new List<NumberCell>(allCells);

        // Work backwards from solution by removing 1 value at a time
        for (int i = 0; i < (int)difficulty; i++)
        {
            bool emptiedCell = false;

            // Try and find a valid cell to empty untill one is found, or run out of cells to try
            while (!emptiedCell && availableCells.Count > 0)
            {
                // Get random cell from those available
                var cell = availableCells[Random.Range(0, availableCells.Count)];

                // Check if inferable, with difficulty dependent infer method
                if (inferMethod(cell))
                {
                    cell.ClearCell();
                    emptiedCell = true;
                }

                // Remove cell from pool once sampled
                availableCells.Remove(cell);
            }
        }
    }

    /// <summary>
    /// Remove cell values NOT required to solve the puzzle. <br></br>
    /// <b>Note:</b> Far slower, but more depth.
    /// </summary>
    private void AdvancedRemoveCellValue()
    {

        for (int i = 0; i < allCells.Length; i++)
        {
            if (allCells.Length - startPoint.Count == (int)difficulty)
                break;

            if (allCells[i].IsEmpty)
                continue;

            int value = allCells[i].CellValue;

            // Remove value
            allCells[i].ClearCell();
            startPoint.Remove(i);

            // Try to solve
            Solve();

            //If can't solve, replace value
            if (!scoreBoard.HasFinished)
            {
                allCells[i].CellValue = value;
                startPoint.Add(i, value);
            }
            else
            {
                startPoint.Remove(i);
            }

            ResetToStart();
        }

    }

    /// <summary>
    /// Method to check whether the passed <paramref name="cell"/> value can be inferred from the value of other
    /// cells in it's areas.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns>true if the cell value can be inferred, otherwise false</returns>
    private bool IsDirectlyInferable(NumberCell cell)
    {
        // Check if there is only 1 possible value for the cell
        if (GetCellDirectPossibleValues(cell).Count == 1)
            return true;

        return default;
    }

    /// <summary>
    /// Method to check whether the passed <paramref name="cell"/> value can be either directly or indirectly inferred from the value of other
    /// cells in it's areas.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns>true if the cell value can be inferred, otherwise false</returns>
    private bool IsInferable(NumberCell cell)
    {
        // Check if there is only 1 possible value for the cell
        if (GetCellPossibleValues(cell).Length == 1)
            return true;

        return default;
    }

    private NumberCell[][] GetCellAreas(NumberCell cell)
    {
        var areas = new NumberCell[3][];
        var areasIndex = 0;

        for (int i = 0; i < allAreas.Length; i++)
        {
            // Check if the area contains the target cell
            if (!allAreas[i].Contains(cell))
                continue;

            areas[areasIndex] = allAreas[i];
            areasIndex++;
        }

        return areas;
    }

    private List<int> GetCellDirectPossibleValues(NumberCell cell)
    {
        // Default to all possible numbers
        List<int> possibleNumbers = new List<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });

        // Get all areas of this cell
        var thisCellAreas = GetCellAreas(cell);
        for (int areaIndex = 0; areaIndex < thisCellAreas.Length; areaIndex++)
        {
            var currentArea = thisCellAreas[areaIndex];
            for (int i = 0; i < currentArea.Length; i++)
            {
                // Skip this cell
                if (currentArea[i] == cell)
                    continue;

                // Remove from possible numbers if already present
                if (possibleNumbers.Contains(currentArea[i].CellValue))
                    possibleNumbers.Remove(currentArea[i].CellValue);
            }
        }

        return possibleNumbers;
    }

    private int[] GetCellPossibleValues(NumberCell cell)
    {
        List<int> thisCellPossibleValues = new List<int>(GetCellDirectPossibleValues(cell));

        // Check if there is only 1 possible value for the cell
        if (thisCellPossibleValues.Count == 1)
            return thisCellPossibleValues.ToArray();

        // Check all other blank cells in this cells areas, and cache possible options

        // Get all areas of this cell
        var thisCellAreas = GetCellAreas(cell);

        // List of cells already checked
        List<NumberCell> cellsChecked = new List<NumberCell>();

        // Loop over all areas this cell is a member of
        for (int areaIndex = 0; areaIndex < thisCellAreas.Length; areaIndex++)
        {
            var currentArea = thisCellAreas[areaIndex];

            // Loop over all cells in this area
            for (int i = 0; i < currentArea.Length; i++)
            {
                // Skip this cell, full cells and cells already checked
                if (currentArea[i] == cell || !currentArea[i].IsEmpty || cellsChecked.Contains(currentArea[i]))
                    continue;

                var possibleValues = GetCellDirectPossibleValues(currentArea[i]);

                // Remove any possible values that must be in related cells from this cells possible values
                if (possibleValues.Count == 1 && thisCellPossibleValues.Contains(possibleValues[0]))
                    thisCellPossibleValues.Remove(possibleValues[0]);

                // Add cell to already checked list
                cellsChecked.Add(currentArea[i]);

                // Check for only 1 number occurance in this area
            }
        }

        return thisCellPossibleValues.ToArray();
    }

    /// <summary>
    /// Check an <paramref name="area"/> of NumberCells, to see if any possible values only occur once in the area.
    /// If <paramref name="fill"/> is true, will fill cells with only one value occurance.
    /// </summary>
    /// <param name="area">The area to check</param>
    /// <param name="fill">Whether to fill the cells when a single value occurs</param>
    /// <returns></returns>    
    private Dictionary<NumberCell, int> CheckAreaValueOccurance(NumberCell[] area, bool fill = false)
    {
        Dictionary<NumberCell, int> result = new();

        Dictionary<NumberCell, int[]> cellPossibleValues = new();
        for (int i = 0; i < area.Length; i++)
        {
            // Skip filled cells, or cells already tested
            if (!area[i].IsEmpty || cellPossibleValues.ContainsKey(area[i]))
                continue;

            // Add the possible values for the cell
            cellPossibleValues.Add(area[i], GetCellDirectPossibleValues(area[i]).ToArray());
        }

        Dictionary<int, (NumberCell, bool)> valueOccurance = new();
        foreach (var item in cellPossibleValues)
        {
            // Loop over all possible values for the cell
            for (int i = 0; i < item.Value.Length; i++)
            {
                if (valueOccurance.ContainsKey(item.Value[i]))
                {
                    // If already has value, switch flag to false (is duplicate)
                    valueOccurance[item.Value[i]] = (valueOccurance[item.Value[i]].Item1, false);
                }
                else
                {
                    // Add new value
                    valueOccurance.Add(item.Value[i], (item.Key, true));
                }
            }
        }

        foreach (var item in valueOccurance)
        {
            // If flag is true
            if (item.Value.Item2)
            {
                // Add to result
                result.Add(item.Value.Item1, item.Key);

                // Fill cell value, if required
                if (fill)
                    item.Value.Item1.CellValue = item.Key;
            }
        }

        return result;
    }

    public void ResetToStart()
    {
        EmptyAllCells();

        var cellIndexes = startPoint.Keys.ToArray();

        for (int i = 0; i < cellIndexes.Length; i++)
        {
            allCells[cellIndexes[i]].CellValue = startPoint[cellIndexes[i]];
            allCells[cellIndexes[i]].IsStartingValue = true;
        }

        scoreBoard.ResetScoreBoard();
        CheckSolution();
    }

    /// <summary>
    /// Method to empty all of the sudoku cells
    /// </summary>
    private void EmptyAllCells()
    {
        for (int i = 0; i < allCells.Length; i++)
        {
            allCells[i].ClearCell();
        }
    }

    public void GetHint()
    {
        Dictionary<NumberCell, int[]> cellPossibleValues = new Dictionary<NumberCell, int[]>();

        // Loop over all cells
        for (int i = 0; i < allCells.Length; i++)
        {
            var cell = allCells[i];

            // Skip filled cells
            //if (!cell.IsEmpty)
            //    continue;

            cellPossibleValues[cell] = GetCellPossibleValues(cell);
        }

        /*
        Dictionary<int, int[]> numOccurances = new Dictionary<int, int[]>();

        // Loop over all areas
        for (int areaIndex = 0; areaIndex < allAreas.Length; areaIndex++)
        {
            // Add occurance counter for this area
            numOccurances[areaIndex] = new int[allAreas[areaIndex].Length];

            // Loop over cells in area
            for (int cellIndex = 0; cellIndex < allAreas[areaIndex].Length; cellIndex++)
            {
                // Get the annotation mask for this cell
                var annotationBoolMask = allAreas[areaIndex][cellIndex].Annotations.AllAnnotations;

                // Loop over all annotations
                for (int annotationValue = 0; annotationValue < annotationBoolMask.Length; annotationValue++)
                {
                    // Check if the annotation value is present
                    if (annotationBoolMask[annotationValue])
                    {
                        numOccurances[areaIndex][cellIndex]++;
                    }
                }
            }


            foreach (var numOccurance in numOccurances[areaIndex])
            {
                if (numOccurance == 1)
                {
                    Debug.Log("yep");
                }
            }
        }
        */

        var keys = cellPossibleValues.Keys.ToArray();
        foreach (var key in keys)
        {
            string values = "[";

            foreach (var value in cellPossibleValues[key])
            {
                values += $" {value} ";
            }

            values += "]";

            Debug.Log($"{key} - {values}");

        }

        // Loop over cells in cell possible values

        // Get all areas for that cell

        // Check for overlap with other cell possible values in those areas

        // Increment number of hints taken
        scoreBoard.NumHintsTaken++;

        //CheckSolution();
    }

    /// <summary>
    /// Method to test whether the board is correctly filled.
    /// </summary>
    /// <returns>true if board is correctly filled</returns>
    public bool CheckSolution()
    {
        int numCorrectCells = 0;

        for (int i = 0; i < allCells.Length; i++)
        {
            var cell = allCells[i];

            // Skip empty cells, starting cells
            if (cell.IsEmpty || cell.IsStartingValue)
                continue;

            // Skip if legality already evaluated
            if (cell.LegalityEvaluated)
            {
                // Increment correct if value is legal
                if (!cell.IsIllegalValue)
                    numCorrectCells++;
                continue;
            }

            // Check legality of the cells value
            var possibleValues = GetCellDirectPossibleValues(cell);
            if (cell.EvaluteLegalityOfValue(possibleValues.ToArray()))
                numCorrectCells++;
            else
                scoreBoard.NumMistakesMade++;
        }

        scoreBoard.NumCellsFilled = numCorrectCells;
        var totalFilledCells = numCorrectCells + startPoint.Count;

        // Update score board
        scoreBoard.SetCellsRemainingDisplay((allCells.Length - totalFilledCells).ToString());

        // All cells correctly filled, trigger finish
        if (totalFilledCells == allCells.Length)
        {
            scoreBoard.HasFinished = true;
            return true;
        }

        return false;
    }
}

