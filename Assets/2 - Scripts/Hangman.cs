using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using System.Buffers.Text;
using System.IO;
using System.Net;
using Newtonsoft;
using Newtonsoft.Json;
using System.Text;
using UnityEngine.UI;
using TMPro;
using System;
using Newtonsoft.Json.Linq;
using UnityEditor.PackageManager.Requests;
using System.Linq;

public class Hangman : MonoBehaviour
{
    // Airtable API information
    private string apiKey = "patSvZlyhJ1yNTZYt.deab8b13df91e6a9d764ee5aca2a9b230640969fde5162e51ed8b5b2e9ec95ac";
    private string baseId = "appt3MyV6bEjM602c";
    private string tableName = "Hangman";

    private string apiUrl;

    public string word;
    public int level;

    public TMP_InputField inputField;

    // Start is called before the first frame update
    void Start()
    {
        apiUrl = $"https://api.airtable.com/v0/{baseId}/{tableName}";
        level = 1;
        // Start coroutine to fetch data from Airtable
        StartCoroutine(GetRecordsFromAirtable());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            name = inputField.text;
            EnterUser(inputField.text[0]);
            Debug.Log(inputField.text[0]);
        }
    }

    public class Airtable
    {
        public Fields fields;
    }

    public class Fields
    {
        public string Guess;
    }


    public void EnterUser(char _letterGuess)
    {
        StartCoroutine(GetUserGuesses(_letterGuess));
    }

    public void EnterPrice(int _price)
    {
        StartCoroutine(GetRecordId(name, _price));
    }

    public IEnumerator GetUserGuesses(char _letter)
    {
        string _level_url = $"https://api.airtable.com/v0/{baseId}/{tableName}?filterByFormula={{Level}}={level}";


        using (UnityWebRequest webRequest = UnityWebRequest.Get(_level_url))
        {
            webRequest.SetRequestHeader("Authorization", "Bearer " + apiKey);
            webRequest.SetRequestHeader("Content-Type", "application/json");

            // Wait for the response
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {

                JObject jsonResponse = JObject.Parse(webRequest.downloadHandler.text);
                JArray records = (JArray)jsonResponse["records"];

                if (records.Count > 0)
                {
                    string recordId = records[0]["id"].ToString();
                    string currentGuesses = records[0]["fields"]["Guesses"].ToString();
                    string newGuess = currentGuesses + ", " + _letter;
                    StartCoroutine(UpdateUserGuess(recordId, newGuess));
                    EnterGuess(newGuess);
                }
                else
                {
                    Debug.LogError("No Guesses in Airtable");
                }
            }
        }
    }

    private void EnterGuess(string _newGuess)
    {
        string[] guessChars = _newGuess.Split(", ");
        string wordToGuess = word;
        int misses = 0;

        foreach (string _char in guessChars)
        {
            if (wordToGuess.Contains(_char))
            {
                wordToGuess = wordToGuess.Replace(_char, "");
            }
            else
            {
                misses++;
            }
        }

        Debug.Log(wordToGuess + " \n" + misses + " misses so far");
    }

    private IEnumerator UpdateUserGuess(string _recordId, string _newGuess)
    {
        string url = $"https://api.airtable.com/v0/{baseId}/{tableName}/{_recordId}";

        var requestBody = new
        {
            fields = new
            {
                Guesses = _newGuess
            }
        };

        string jsonPayload = JsonConvert.SerializeObject(requestBody);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Successfully updated Airtable record: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error updating Airtable: " + request.error);
            }
        }
    }

    // Function to GET records from Airtable
    IEnumerator GetRecordsFromAirtable()
    {
        string _level_url = $"https://api.airtable.com/v0/{baseId}/{tableName}?filterByFormula={{Level}}={1}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(_level_url))
        {
            webRequest.SetRequestHeader("Authorization", "Bearer " + apiKey);
            webRequest.SetRequestHeader("Content-Type", "application/json");

            // Wait for the response
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                JObject jsonResponse = JObject.Parse(webRequest.downloadHandler.text);
                Debug.Log($"{jsonResponse.ToString()}");
                JArray records = (JArray)jsonResponse["records"];

                if (records.Count > 0)
                {
                    string recordId = records[0]["fields"]["Word"].ToString();
                    word = recordId;
                }

            }
            else
            {
                Debug.LogError("Error: " + webRequest.error);
            }
        }
    }

    private IEnumerator GetRecordId(string userName, int newScore)
    {
        string url = $"https://api.airtable.com/v0/{baseId}/{tableName}?filterByFormula={{Customer}}='{userName}'";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Response: " + request.downloadHandler.text);

                JObject jsonResponse = JObject.Parse(request.downloadHandler.text);
                JArray records = (JArray)jsonResponse["records"];

                if (records.Count > 0)
                {
                    string recordId = records[0]["id"].ToString(); // Get the first matching record ID
                    Debug.Log("Find user + " + recordId);
                    StartCoroutine(UpdateRecord(recordId, newScore));
                }
                else
                {
                    Debug.LogError("User not found in Airtable.");
                }
            }
            else
            {
                Debug.LogError("Error fetching record ID: " + request.error);
            }
        }
    }

    private IEnumerator UpdateRecord(string recordId, int newScore)
    {
        string url = $"https://api.airtable.com/v0/{baseId}/{tableName}/{recordId}";

        var requestBody = new
        {
            fields = new
            {
                Price = newScore
            }
        };

        string jsonPayload = JsonConvert.SerializeObject(requestBody);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Successfully updated Airtable record: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error updating Airtable: " + request.error);
            }
        }
    }

    public IEnumerator AddNewItemPrice(int _price)
    {
        string jsonPayload = "{ \"fields\": { \"Price\": 100 } }";

        //Test comment

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "PATCH"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Record successfully updated: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error updating record: " + request.responseCode + " - " + request.downloadHandler.text);
            }
        }
    }




}
