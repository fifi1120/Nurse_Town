# nurse-town


The primary purpose of this project is to develop a nursing student training game named “Nurse Town” that leverages Large Language Models (LLMs) and generative graphics and texts to create an immersive virtual training game for nursing students. This game aims to simulate a wide range of clinical scenarios with high fidelity, allowing students to practice and hone their skills in a controlled yet realistic setting. By doing so, the project seeks to enhance the quality and accessibility of nursing education, ensure a more uniform training experience, and better prepare students for the complexities of real-world medical care

How to run the project:

1. Clone the repository to your local machine.
2. Open the project in Unity.
3. In Unity, go to Assets/Doctor's office/Scene, click on "Demo", and then click on "Play".
4. Go to Assets/Doctor's office/Scripts/PatientSpeech.cs, and replace the placeholder API key with our secret OpenAI API key.
5. Import Newtonsoft.Json: In Unity, go to Window > Package Manager, click on "Add package from git URL", and enter "com.unity.nuget.newtonsoft-json", then click on "Add" to install it.
6. Go to Hierarchy, click on "Patient", in the Inspector, click on "Add Component", search for "PatientSpeech", and add it.
7. The game should start. You can see the conversation from Patient NPC from the console.

Testing the Text-to-Speech System:

1. Go to Assets/Scripts/TTS/TTSManager.cs, and change the value of openAIApiKey to our secret OpenAI API key.
2. Open the TTS-Test Scene in the same folder.
3. Click Play and enter any text in the input field. Hit enter and the speech should play. With lip sync, The character should have matching mouth movement.

Lip Sync:

Tool: uLipSync - open source Unity plugin for lip sync. Github link: https://github.com/hecomi/uLipSync

Tutorial: https://www.youtube.com/watch?v=k5CtTsIKwE4

Testing the Speech Recognition System:

1. Go to Assets/Scripts/STT/SpeechToTextController.cs, and change the value of openAIApiKey to our secret OpenAI API key.
2. Open the STT-Test Scene in the same folder.
3. Click Play and enter any text in the input field. Press and hold space bar to start recording and release to end. Transcribed text will appear below.
