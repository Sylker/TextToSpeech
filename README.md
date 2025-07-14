# TextToSpeech

This script integrates Unity with OpenAI's ChatGPT and Google Cloud Text-to-Speech APIs to generate natural voice responses from user input. 
When a question is submitted, it is sent to the ChatGPT API, which returns a text-based answer. 
That answer is then converted to speech using Google TTS, resulting in an audio clip that can be played by a character or object in the scene. 
The script supports multilingual output and includes event-based callbacks for further integration.
