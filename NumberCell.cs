using UnityEngine;
using TMPro;
using System;
using System.Linq;
using static UnityEngine.Rendering.DebugUI;

public class NumberCell : MonoBehaviour
{
    private int cellValue = 0;
    private AnnotationsContainer annotations;

    private TextMeshProUGUI valueDisplay;
    private TextMeshProUGUI annotationDisplay;

    private bool isStartingValue = false;
    private bool isIllegalValue = false;
    private bool legalityEvaluated = false;

    public bool IsIllegalValue
    {
        get { return isIllegalValue; }

        set
        {
            isIllegalValue = value;

            if (value)
                valueDisplay.color = Color.red;
            else
                valueDisplay.color = Color.black;
        }
    }

    public bool IsStartingValue
    {
        get { return isStartingValue; }

        set
        {
            isStartingValue = value;

            if (value)
                valueDisplay.color = Color.blue;
            else
                valueDisplay.color = Color.black;

        }
    }

    public bool IsEmpty
    {
        get { return !Enumerable.Range(1, 9).Contains(CellValue); }
    }

    public int CellValue
    {
        get => cellValue;

        set
        {
            cellValue = value;
            legalityEvaluated = false;
            SynchValueDisplay();
        }
    }

    public AnnotationsContainer Annotations { get => annotations; }
    public bool LegalityEvaluated { get => legalityEvaluated; }

    private void Start()
    {
        annotations = new AnnotationsContainer(SynchAnnotationDisplay);

        valueDisplay = transform.Find("Value").GetComponent<TextMeshProUGUI>();
        annotationDisplay = transform.Find("Annotation").GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// <b>WARNING</b> - ONLY use this if cell will definately be reset. <br></br>
    /// Sets the cell value without synching the GameObject display of the cell.
    /// </summary>
    /// <param name="newValue"></param>
    public void SetCellValueWithoutSynch(int newValue)
    {
        cellValue = newValue;
    }

    /// <summary>
    /// Evaluates whether the current cell value is legal / present in the <paramref name="possibleValues"/> array.
    /// </summary>
    /// <param name="possibleValues"></param>
    /// <returns>true if value is <b>legal</b></returns>
    public bool EvaluteLegalityOfValue(int[] possibleValues)
    {
        bool isLegal = false;

        if (!possibleValues.Contains(CellValue))
        {
            IsIllegalValue = true;
        }
        else
        {
            IsIllegalValue = false;
            isLegal = true;
        }

        legalityEvaluated = true;
        return isLegal;
    }

    /// <summary>
    /// Completely clears the cell (reset to start)
    /// </summary>
    public void ClearCell()
    {
        CellValue = 0;
        Annotations.Clear();
        IsStartingValue = false;
        IsIllegalValue = false;
        annotationDisplay.gameObject.SetActive(true);
    }

    public void OnClickUpdateCell()
    {
        // Guard clause to prevent changing the starting values
        if (isStartingValue)
            return;

        switch (SudokuController.Instance.Annotate)
        {
            // Handle annotation change
            case true:
                Annotations[SudokuController.Instance.NumberSelected - 1] = !Annotations[SudokuController.Instance.NumberSelected - 1];
                break;

            // Handle value change
            case false:
                // Reset illegal flag
                IsIllegalValue = false;

                if (CellValue == SudokuController.Instance.NumberSelected)
                {
                    // Clear value and activate annotations
                    annotationDisplay.gameObject.SetActive(true);
                    CellValue = 0;
                }
                else
                {
                    // Disable annotations and change value
                    annotationDisplay.gameObject.SetActive(false);
                    CellValue = SudokuController.Instance.NumberSelected;
                }

                break;
        }

        // Check the solution
        SudokuController.Instance.CheckSolution();
    }

    private void SynchAnnotationDisplay()
    {
        string newText = "";

        for (int i = 0; i < Annotations.AllAnnotations.Length; i++)
        {
            if (Annotations[i])
                newText += $"{i + 1} ";
        }

        annotationDisplay.text = newText;
    }

    private void SynchValueDisplay()
    {
        if (Enumerable.Range(1, 9).Contains(CellValue))
            valueDisplay.text = $"{CellValue}";
        else
            valueDisplay.text = "";
    }

}

/// <summary>
/// Class to hold all annotations for a cell, will execute the passed (constructor) delegate
/// whenever a value within the container is modified.
/// </summary>
public class AnnotationsContainer
{
    public delegate void SyncDisplayMethod();
    private SyncDisplayMethod syncDisplay;
    private bool[] allAnnotations;

    public bool[] AllAnnotations { get { return allAnnotations; } }

    public bool this[int index]
    {
        get { return allAnnotations[index]; }
        set { allAnnotations[index] = value; syncDisplay(); }
    }

    // Constructors
    public AnnotationsContainer(SyncDisplayMethod syncDisplay)
    {
        allAnnotations = new bool[9];
        this.syncDisplay = syncDisplay;
    }

    /// <summary>
    /// Method to 'clear' annotations ie. set allAnnotations bool mask to false
    /// </summary>
    public void Clear()
    {
        allAnnotations = new bool[9];
        syncDisplay();
    }
}