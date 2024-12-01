# Nurse-town


The primary purpose of this project is to develop a nursing student training game named “Nurse Town” that leverages Large Language Models (LLMs) and generative graphics and texts to create an immersive virtual training game for nursing students. This game aims to simulate a wide range of clinical scenarios with high fidelity, allowing students to practice and hone their skills in a controlled yet realistic setting. By doing so, the project seeks to enhance the quality and accessibility of nursing education, ensure a more uniform training experience, and better prepare students for the complexities of real-world medical care

## Set up
### Clone the repo:

To enable big file uploading, you need to use Git Large File Storage (Git LFS).

1. git lfs install
2. git clone <repository-url>

## Run the project:

1. Set up .env to the **root**, add the API key in the .env file: `OPENAI_API_KEY=your_actual_api_key_here` (normally we don't need to use quote mark with the apikey)
2. Open the project in Unity.
3. In Unity, locate and double click on "Demo".
   ![image](https://github.com/user-attachments/assets/08ece733-cce9-4afd-98a8-02bc55e89b60)


5. Import Newtonsoft.Json: In Unity, go to Window > Package Manager, click on "Add package from git URL", and enter "com.unity.nuget.newtonsoft-json", then click on "Add" to install it.
6. Go to Hierarchy, click on "Patient", in the Inspector, click on "Add Component", search for "PatientSpeech", and add it.
7. The game should start. You can see the conversation from Patient NPC from the console.

### Note:
Make sure all scripts are attached to main character

![image](https://github.com/user-attachments/assets/ccc93c24-00b2-4988-866c-1c83cbed72bf)


### Testing the Text-to-Speech System:

1. Go to Assets/Scripts/TTS/TTSManager.cs, and change the value of openAIApiKey to our secret OpenAI API key.
2. Open the TTS-Test Scene in the same folder.
3. Click Play and enter any text in the input field. Hit enter and the speech should play. With lip sync, The character should have matching mouth movement.

### Lip Sync:

Tool: uLipSync - open source Unity plugin for lip sync. Github link: https://github.com/hecomi/uLipSync

Tutorial: https://www.youtube.com/watch?v=k5CtTsIKwE4

Testing the Speech Recognition System:

1. Go to Assets/Scripts/STT/SpeechToTextController.cs, and change the value of openAIApiKey to our secret OpenAI API key.
2. Open the STT-Test Scene in the same folder.
3. Click Play and enter any text in the input field. Press and hold space bar to start recording and release to end. Transcribed text will appear below.

### 3d Modeling and Animation:
1. Models source: https://www.mixamo.com/
2. Coloring and texture:
   
   ![image](https://github.com/user-attachments/assets/dab36126-7c75-4603-81b2-81eb94203f06)
   note:
   1. if char appears pink, check your shader setting.
   2. if char appears white, check your textures setting.
4. Apply animation: https://www.youtube.com/watch?v=Vsj_UpnLFF8
5. Animation for main char preview:
   ![image](https://github.com/user-attachments/assets/4457f70e-625a-4061-beb3-5847797266f2)

  


### Debug Reminder:
1. You may see bugs like:
`NullReferenceException: Object reference not set to an instance of an objectInputManger.LateUpdate () (at Assets/Scripts/InputManger.cs:34)`
If you encounter this bug, it might be because you pressed “Play” and then “Pause.” The correct approach is to press “Play,” and if you want to stop, click “Play” again to cancel. Avoid using the “Pause” button.

### Assets Source:
Scene: https://assetstore.unity.com/packages/3d/props/interior/hospital-doctor-s-office-65226

Blood pressure icon: https://www.cleanpng.com/png-arm-blood-pressure-measurement-8274776/download-png.html

