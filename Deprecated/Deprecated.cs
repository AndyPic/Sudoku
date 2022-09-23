/*
    int identifyAreaIndex = 0;

    /// <summary>
    /// Method to cycle between highlighting the different 'area' types
    /// ie. vertical areas, horizontal areas, and square areas
    /// </summary>
    public void CycleIdentifyAreas()
    {
        if (identifyAreaIndex >= 27)
        {
            // Reset all cells color to white
            for (int i = 0; i < allCells.Length; i++)
            {
                var button = allCells[i].GetComponent<Button>();

                // Change normal color of this cell color block to white
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;

                // Update color block
                button.colors = colors;
            }

            // Reset index to 0
            identifyAreaIndex = 0;
            return;
        }

        // Loop over the area block
        int index = 0;
        for (; index < AREA_SIZE; index++)
        {
            // Generate a random color for this area
            var color = GenerateRandomColor();

            // Loop though cells in block
            for (int i = 0; i < allAreas[index + identifyAreaIndex].Length; i++)
            {
                var button = allAreas[index + identifyAreaIndex][i].GetComponent<Button>();

                // Change normal color of this cell color block to new random color
                ColorBlock colors = button.colors;
                colors.normalColor = color;

                // Update color block
                button.colors = colors;
            }
        }

        // Increment area index
        identifyAreaIndex += index;
    }

    Color32 GenerateRandomColor()
    {
        // Note: 100-255 so colors are lighter (to read black text)
        byte r = (byte)Random.Range(100, 255);
        byte g = (byte)Random.Range(100, 255);
        byte b = (byte)Random.Range(100, 255);
        return new Color32(r, g, b, 255);
    }




    private Dictionary<NumberCell, int> GetSolution()
    {
        Dictionary<NumberCell, int> solution = new();

        int count = 0;

        while (!scoreBoard.HasFinished && count < 3)
        {
            // Loop over all cells in an area
            for (int i = 0; i < allAreas.Length; i++)
            {
                Dictionary<NumberCell, int[]> cellPossibleValues = new Dictionary<NumberCell, int[]>();
                for (int j = 0; j < allAreas[i].Length; j++)
                {
                    // Add filled cells to solution
                    if (!allAreas[i][j].IsEmpty)
                    {
                        solution[allAreas[i][j]] = allAreas[i][j].CellValue;
                        continue;
                    }

                    // Skip cells already tested
                    if (cellPossibleValues.ContainsKey(allAreas[i][j]) || solution.ContainsKey(allAreas[i][j]))
                        continue;

                    var possible = GetCellPossibleValues(allAreas[i][j]);
                    if (possible.Length == 1)
                        // Add the value to solution
                        solution[allAreas[i][j]] = possible[0];
                    else
                        // Add the possible values for the cell
                        cellPossibleValues.Add(allAreas[i][j], GetCellPossibleValues(allAreas[i][j]));
                }

                Dictionary<int, (NumberCell, bool)> valueOccurance = new();

                var keys = cellPossibleValues.Keys.ToArray();
                // Loop over all cells with possible values
                for (int k = 0; k < keys.Length; k++)
                {
                    // Loop over all possible values for the cell
                    for (int l = 0; l < cellPossibleValues[keys[k]].Length; l++)
                    {
                        int value = cellPossibleValues[keys[k]][l];

                        if (valueOccurance.ContainsKey(value))
                        {
                            // If already has value, switch flag to false (is duplicate)
                            valueOccurance[value] = (valueOccurance[value].Item1, false);
                        }
                        else
                        {
                            // Add new value
                            valueOccurance.Add(value, (keys[k], true));
                        }
                    }
                }

                var keys2 = valueOccurance.Keys.ToArray();
                foreach (var key in keys2)
                {
                    // If flag is true
                    if (valueOccurance[key].Item2)
                    {
                        // Set the corresponding cell value
                        valueOccurance[key].Item1.CellValue = key;
                    }
                }

            }

            CheckSolution();
            count++;
        }

        return solution;
    }




    private void GenerateFinishedSolution()
    {
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
        FillRest();

    }

    private void FillRest()
    {
        // Esnure all cells are empty to start
        ResetToStart();

        var rows = allAreas[3..9];
        int startOvers = 0;



        // Loop over rows
        for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            bool startOver = false;
            int rowAttempts = 0;

            Dictionary<NumberCell, List<int>> rowPossibleValues = new();
            List<int> rowRemainingValues = new List<int>(cellPossibleValues);
            rowRemainingValues.Remove(rows[rowIndex][0].CellValue);

            // Fill row with random possible values
            for (int cellIndex = 1; cellIndex < rows[rowIndex].Length; cellIndex++)
            {
                if (!rows[rowIndex][cellIndex].IsEmpty)
                    continue;



                rowPossibleValues[rows[rowIndex][cellIndex]] = GetCellDirectPossibleValues(rows[rowIndex][cellIndex]);
                int value;

                if (rowPossibleValues[rows[rowIndex][cellIndex]].Count > 0)
                {
                    value = rowPossibleValues[rows[rowIndex][cellIndex]][Random.Range(0, rowPossibleValues[rows[rowIndex][cellIndex]].Count)];
                    rows[rowIndex][cellIndex].CellValue = value;

                    // Remove the added value from all possible
                    foreach (var pair in rowPossibleValues)
                    {
                        pair.Value.Remove(value);
                    }
                    rowRemainingValues.Remove(value);
                }
                else
                {
                    


                }


                
            }




        }

        CheckSolution();
    }


                        // Loop over all cells in an area
                        for (int i = 0; i < allAreas.Length; i++)
                        {

                            Dictionary<NumberCell, int[]> cellPossibleValues = new Dictionary<NumberCell, int[]>();

                            for (int j = 0; j < allAreas[i].Length; j++)
                            {
                                // Skip filled cells, or cells already tested
                                if (!allAreas[i][j].IsEmpty || cellPossibleValues.ContainsKey(allAreas[i][j]))
                                    continue;

                                // Add the possible values for the cell
                                cellPossibleValues.Add(allAreas[i][j], GetCellPossibleValues(allAreas[i][j]));
                            }

                            Dictionary<int, (NumberCell, bool)> valueOccurance = new();

                            var keys = cellPossibleValues.Keys.ToArray();
                            // Loop over all cells with possible values
                            for (int k = 0; k < keys.Length; k++)
                            {
                                // Loop over all possible values for the cell
                                for (int l = 0; l < cellPossibleValues[keys[k]].Length; l++)
                                {
                                    int value = cellPossibleValues[keys[k]][l];

                                    if (valueOccurance.ContainsKey(value))
                                    {
                                        // If already has value, switch flag to false (is duplicate)
                                        valueOccurance[value] = (valueOccurance[value].Item1, false);
                                    }
                                    else
                                    {
                                        // Add new value
                                        valueOccurance.Add(value, (keys[k], true));
                                    }
                                }
                            }

                            var keys2 = valueOccurance.Keys.ToArray();
                            foreach (var key in keys2)
                            {
                                // If flag is true
                                if (valueOccurance[key].Item2)
                                {
                                    // Set the corresponding cell value
                                    valueOccurance[key].Item1.CellValue = key;
                                }
                            }

                        }


/// <summary>
    /// Attempts to build the game board recursively untill a solvable board is built.
    /// </summary>
    private void BuildGame()
    {
        // Build the game
        GenerateFinishedSolution();
        RemoveCellValues();
        SaveStartPoint();

        // If couldn't solve -> start over!
        if (!Solve())
            BuildGame();
        else
            ResetToStart();
    }

*/