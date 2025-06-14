LipSync Shape Keys Update
Overview
This update implements comprehensive phoneme-based shape keys for uLipSync integration, featuring natural facial asymmetry based on ARKit standards.

Update Contents
Shape Keys Added
	•	34 Phoneme Shape Keys (Phoneme_AA, Phoneme_AE, etc.)
	•	Natural Asymmetry - Subtle left-right variations (5-10% difference) for realistic speech
	•	ARKit-based Parameters - Following Apple's facial tracking standards
Files Included
LipSync_Test_MK/
├── Models/
│   └── Character_WithPhonemes.fbx    # Model with all shape keys
├── Scenes/
│   └── LipSyncTest_Scene.unity        # Test scene with lighting setup
├── Materials/
│   └── [Material files if any]        # Adjusted materials
├── Documentation/
│   └── README.md                      # This file
└── LipSyncTestCharacter.prefab        # Configured prefab with components

Testing Instructions
Quick Test
	1	Open Scenes/LipSyncTest_Scene.unity
	2	Add an Audio Source component to the character (if testing with audio)
	3	Assign audio clip or use microphone input
	4	Press Play
	5	Observe mouth movements matching speech
Note: Audio Source component needs to be added manually for testing.
Component Setup
The prefab includes:
	•	uLipSync - Main lip sync component
	•	uLipSyncBlendShape - Controls blend shapes
	•	Audio Source - To be added for testing (not yet implemented)
Verifying Shape Keys
	1	Select the character in scene
	2	Check Skinned Mesh Renderer → BlendShapes
	3	Confirm all Phoneme_XX shapes are present

Integration Guide
For Team Members
	1	Testing First
	◦	Checkout this branch: feature/lipsync-shapekeys-update
	◦	Open the test scene
	◦	Verify functionality before integration
	2	Integration Steps
	◦	Import the tested shape keys to your character
	◦	Copy component settings from test prefab
	◦	Adjust blend shape weights as needed

Shape Key Naming Convention
All phonemes follow the pattern: Phoneme_[PHONEME_CODE]
Vowels:
	•	Phoneme_AA (father, car)
	•	Phoneme_AE (cat, hat)
	•	Phoneme_AH (cup, love)
	•	Phoneme_AO (law, saw)
	•	Phoneme_EH (bed, head)
	•	Phoneme_ER (her, bird)
	•	Phoneme_IH (bit, sit)
	•	Phoneme_IY (see, tree)
	•	Phoneme_UH (put, book)
	•	Phoneme_UW (moon, food)
Consonants:
	•	Phoneme_B, Phoneme_CH, Phoneme_D, etc.
	•	Full list: B, CH, D, DH, F, G, HH, JH, K, L, M, N, NG, P, R, S, SH, T, TH, V, W, Y, Z, ZH

Technical Details
Asymmetry Implementation
	•	Right side of mouth slightly more active (common in natural speech)
	•	Differences kept within 5-10% range
	•	Rounded lip shapes (UW, UH) remain relatively symmetric
Performance Considerations
	•	All shape keys optimized for real-time use
	•	Smooth interpolation between phonemes
	•	Minimal impact on frame rate

Version History
	•	Date: June13, 2025
	•	Author: Mingkai
	•	Version: 1.0

For questions or issues, please comment on the Pull Request or contact the author.
