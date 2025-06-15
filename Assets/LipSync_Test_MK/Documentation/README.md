## Testing Instructions

### Quick Test
1. Open `Scenes/LipSyncTest_Scene.unity`
2. The scene contains properly oriented character with camera setup
3. Select the character in the scene
4. In the Inspector, find Skinned Mesh Renderer component
5. Manually adjust BlendShape sliders to test each phoneme shape

### Component Setup
The prefab includes:
- **uLipSync** - Main lip sync component (configuration pending)
- **uLipSyncBlendShape** - Controls blend shapes (configuration pending)

### Verifying Shape Keys
1. Select the character in scene
2. Check Skinned Mesh Renderer → BlendShapes
3. Confirm all MTH.XX shapes are present (34 phonemes)
4. Test each shape key by adjusting its slider value

## Integration Guide

### For Team Members

#### 1. Testing First
- Checkout this branch: `feature/lipsync-MK`
- Open the test scene
- Verify shape keys are working correctly

#### 2. Integration Steps
- Import the tested shape keys to your character
- Use the provided Blender script for consistent results
- Apply the MTH naming convention

### Shape Key Naming Convention
All phonemes now follow the MTH format: `MTH.[PHONEME_CODE]`

**Vowels:**
- MTH.AA (father, car)
- MTH.AE (cat, hat)
- MTH.AH (cup, love)
- MTH.AO (law, saw)
- MTH.EH (bed, head)
- MTH.ER (her, bird)
- MTH.IH (bit, sit)
- MTH.IY (see, tree)
- MTH.UH (put, book)
- MTH.UW (moon, food)

**Consonants:**
- MTH.B, MTH.CH, MTH.D, etc.
- Full list: B, CH, D, DH, F, G, HH, JH, K, L, M, N, NG, P, R, S, SH, T, TH, V, W, Y, Z, ZH

## Technical Details

### Balanced Parameter Philosophy
- **Preserved essential phoneme characteristics** (e.g., M/B/P closure, F/V lip-teeth contact)
- **Reduced exaggerated parameters** (e.g., IY smile from 0.75 to 0.6, AA jaw from 0.7 to 0.55)
- **Added subtle dimples** for smile-related phonemes (IY, EH, AE, IH, S, Y, Z, R)
- **Maintained phoneme distinctiveness** while achieving natural conversation amplitude

### Asymmetry Implementation
- Right side of mouth slightly more active (common in natural speech)
- Differences kept within 2-5% range (reduced from previous 5-10%)
- Rounded lip shapes (UW, UH) remain relatively symmetric

### Performance Considerations
- All shape keys optimized for real-time use
- Smooth interpolation between phonemes
- Minimal impact on frame rate
- Unity weight adjustment capability for different scenarios

## Version History
- **Date**: June 14, 2025
- **Author**: Mingkai Gao
- **Version**: 2.0
---
For questions or issues, please comment on the Pull Request or contact the author.
