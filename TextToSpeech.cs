using System;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public class TextToSpeech : MonoBehaviour
{
    // API keys set in the Unity Inspector
    [SerializeField] private string openAiApiKey = "YOUR_OPENAI_API_KEY";
    [SerializeField] private string googleTtsApiKey = "YOUR_GOOGLE_API_KEY";

    // Event triggered when the TTS AudioClip is ready
    public event Action<AudioClip> OnComplete;

    void Start() { /* Initialization if needed */ }

    // Entry point to send a user question to the ChatGPT API
    public void AskQuestion(string userInput)
    {
        StartCoroutine(GetChatGptResponse(userInput));
    }

    // Coroutine that sends the user input to OpenAI's ChatGPT API and processes the response
    IEnumerator GetChatGptResponse(string userInput)
    {
        string prompt = $"Answer clearly: {userInput}";

        var requestData = new
        {
            model = "gpt-4",
            // model = "gpt-3.5-turbo",
            messages = new[] {
                new { role = "user", content = prompt }
            }
        };

        string json = JsonConvert.SerializeObject(requestData);
        string url = "https://api.openai.com/v1/chat/completions";
        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {openAiApiKey}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ChatGptResponse result = JsonConvert.DeserializeObject<ChatGptResponse>(request.downloadHandler.text);
                string responseText = result.choices[0].message.content;
                StartCoroutine(ConvertTextToSpeech(responseText));
            }
            else
            {
                Debug.LogError("OpenAI Error: " + request.error);
                Debug.LogError("Response: " + request.downloadHandler.text); // Mostra motivo específico
            }
        }
    }

    // Coroutine that sends the ChatGPT response to Google's Text-to-Speech API
    IEnumerator ConvertTextToSpeech(string input)
    {
        var requestData = new
        {
            input = new { text = input },
            voice = new
            {
                languageCode = "pt-BR",
                name = "pt-BR-Standard-E"
            },
            audioConfig = new { audioEncoding = "MP3" }
        };

        string json = JsonConvert.SerializeObject(requestData);
        string url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={googleTtsApiKey}";
        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                TtsResponse ttsResult = JsonConvert.DeserializeObject<TtsResponse>(request.downloadHandler.text);
                string audioBase64 = ttsResult.audioContent;

                byte[] audioBytes = System.Convert.FromBase64String(audioBase64);
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, "tts_audio.mp3");
                System.IO.File.WriteAllBytes(filePath, audioBytes);

                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG))
                {
                    yield return www.SendWebRequest();
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    OnComplete?.Invoke(clip);
                }
            }

            else
            {
                Debug.LogError("Google TTS Error: " + request.error);
            }
        }
    }

    // Helper classes to deserialize API responses

    [Serializable]
    public class ChatGptMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class ChatGptChoice
    {
        public ChatGptMessage message;
    }

    [Serializable]
    public class ChatGptResponse
    {
        public List<ChatGptChoice> choices;
    }

    [Serializable]
    public class TtsResponse
    {
        public string audioContent;
    }
}
