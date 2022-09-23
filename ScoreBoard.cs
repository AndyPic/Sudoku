using UnityEngine;
using TMPro;
using System;

public class ScoreBoard : MonoBehaviour
{
    private int numMistakesMade = 0;
    private int numHintsTaken = 0;
    private int numCellsFilled = 0;
    private bool hasFinished = true;

    private float timeTaken = 0;

    private TextMeshProUGUI difficultyDisplay;
    private TextMeshProUGUI cellsRemainingDisplay;
    private TextMeshProUGUI timerDisplay;

    public bool HasFinished
    {
        get { return hasFinished; }
        set
        {
            hasFinished = value;
            CheckFinished();
        }
    }
    public int NumMistakesMade { get => numMistakesMade; set => numMistakesMade = value; }
    public int NumHintsTaken { get => numHintsTaken; set => numHintsTaken = value; }
    public int NumCellsFilled { get => numCellsFilled; set => numCellsFilled = value; }

    private void Start()
    {
        timerDisplay = transform.Find("Timer").GetComponent<TextMeshProUGUI>();
        difficultyDisplay = transform.Find("Difficulty").GetComponent<TextMeshProUGUI>();
        cellsRemainingDisplay = transform.Find("CellsRemaining").GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        // Guard clause if finished the puzzle
        if (hasFinished)
            return;

        // Increment timer
        timeTaken += Time.deltaTime;
        UpdateTimeDisplay();
    }

    private void CheckFinished()
    {
        // Guard clause if not finished the puzzle
        if (!hasFinished)
            return;

        // Lock the board

        // Calculate the score
        var score = CalculateScore();

    }

    private double CalculateScore()
    {
        double difficultyModifier = 1;

        return ((numCellsFilled - numHintsTaken - (0.5 * numMistakesMade)) / timeTaken) * difficultyModifier;
    }

    private void UpdateTimeDisplay()
    {
        TimeSpan time = TimeSpan.FromSeconds(timeTaken);
        timerDisplay.text = time.ToString("hh':'mm':'ss");
    }

    public void ResetScoreBoard()
    {
        timeTaken = 0;
        UpdateTimeDisplay();

        NumMistakesMade = 0;
        HasFinished = false;

        cellsRemainingDisplay.text = "81";
    }

    public void SetCellsRemainingDisplay(string cellsRemaining)
    {
        cellsRemainingDisplay.text = $"{cellsRemaining}";
    }

    public void SetDifficultyDisplay(string difficulty)
    {
        difficultyDisplay.text = $"{difficulty}";
    }
}
