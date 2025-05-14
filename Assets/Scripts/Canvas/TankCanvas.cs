using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TankCanvas : MonoBehaviour
{
    [SerializeField] private InputField inputField; // Reference to the InputField
    [SerializeField] private Button submitButton;   // Reference to the Button

    void Start()
    {
        // Add listener to button click
        if (submitButton != null && inputField != null)
        {
            submitButton.onClick.AddListener(OnButtonClick);
        }
        else
        {
            Debug.LogError("InputField or Button is not assigned in the Inspector!");
        }
    }

    void OnButtonClick()
    {
        // Get and print the text from the InputField
        if (inputField != null)
        {
            string inputText = inputField.text;
            Debug.Log("Input Text: " + inputText);
        }
    }
}