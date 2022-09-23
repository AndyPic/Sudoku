using UnityEngine;
using TMPro;
using System;

public class NumberPicker : MonoBehaviour
{
    private GameObject currentSelectedNumber;
    private float valueFontSize = 82;
    private float annotationFontSize = 12.67f;

    private void Start()
    {
        // Start current selected as the first number slector
        currentSelectedNumber = transform.GetChild(0).gameObject;
    }

    /// <summary>
    /// Either toggle number annotation (if clicking on same number again) or
    /// update to the new number (if clicking on a new number).
    /// </summary>
    /// <exception cref="FormatException"> Text must be valid number from 1-9 (inclusive) </exception>
    /// <param name="numberClicked"></param>
    public void HandleNumberSelection(GameObject numberClicked)
    {
        TextMeshProUGUI numberText = numberClicked.transform.Find("Number").GetComponent<TextMeshProUGUI>();
        GameObject border = numberClicked.transform.Find("Border").gameObject;

        // Try parse the number clicked text as int
        if (int.TryParse(numberText.text, out int numberValue))
        {
            if (numberValue < 1 || numberValue > 9)
            {
                // Log exception
                FormatException e = new("Number must be from 1-9 (inc)");
                Debug.LogException(e, this);
                return;
            }

            // If clicking on same number again, toggle whether to annotate or replace
            if (SudokuController.Instance.NumberSelected == numberValue)
            {
                ToggleNumberAnnotation();

                // Set correct font size
                if (SudokuController.Instance.Annotate)
                    numberText.fontSize = annotationFontSize;
                else
                    numberText.fontSize = valueFontSize;
            }
            // If clicking on new number, update the number selected
            else
            {
                UpdateNumberSelected(numberValue);

                // Reset 'previously' selected number appearance to default
                if (currentSelectedNumber != null)
                {
                    currentSelectedNumber.transform.Find("Number").GetComponent<TextMeshProUGUI>().fontSize = valueFontSize;
                    currentSelectedNumber.transform.Find("Border").gameObject.SetActive(false);
                }

                // Activate the border
                border.SetActive(true);

                // Update current Selected Number to the one clicked
                currentSelectedNumber = numberClicked;
            }

        }
        else
        {
            // Log exception
            FormatException e = new("Text must be a valid number");
            Debug.LogException(e, this);
            return;
        }
    }

    /// <summary>
    /// Change the currently selected number to <paramref name="newNum"/>
    /// </summary>
    /// <param name="newNum"></param>
    private void UpdateNumberSelected(int newNum)
    {
        // Update the number selected
        SudokuController.Instance.NumberSelected = newNum;

        // Ensure action is not annotate
        SudokuController.Instance.Annotate = false;
    }

    private void ToggleNumberAnnotation()
    {
        SudokuController.Instance.Annotate = !SudokuController.Instance.Annotate;
    }
}
