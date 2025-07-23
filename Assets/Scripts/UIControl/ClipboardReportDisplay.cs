using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClipboardReportDisplay : MonoBehaviour
{
    [Header("Canvas UI Text Components")]
    public TextMeshProUGUI titleText;           // Title text component
    public TextMeshProUGUI scoreText;           // Score text component
    public TextMeshProUGUI summaryText;         // Summary text component
    public TextMeshProUGUI promptText;          // Prompt text component for "Press E to view report"

    [Header("Canvas References")]
    public Canvas reportCanvas;                 // Report canvas reference
    public CanvasGroup canvasGroup;            // Canvas group for fade effects

    [Header("Dynamic Text Clearing Settings")]
    public bool clearAllNonReportTexts = true; // 是否清除所有非报告相关的文本
    public bool debugTextClearing = true;      // 是否启用文本清除调试

    [Header("Display Settings")]
    public float typewriterSpeed = 0.05f;       // Typewriter effect speed
    public bool useTypewriterEffect = true;     // Enable/disable typewriter effect
    public float fadeInDuration = 1f;          // Fade in animation duration

    [Header("Text Formatting")]
    public Color titleColor = Color.black;
    public Color scoreColor = Color.blue;
    public Color summaryColor = Color.black;
    public Color promptColor = Color.green;

    [Header("Runtime Color Override")]
    public bool useCustomPromptColor = true;  // Enable to use custom color from Inspector

    private Coroutine currentDisplayCoroutine;
    private bool reportDataAvailable = false;
    private bool reportDisplayed = false;

    // Store report data
    private int storedScore;
    private int storedMaxScore; // 新增：存储最大分数
    private int storedInteractionCount;
    private List<string> storedPositiveReasons = new List<string>();
    private List<string> storedImprovementReasons = new List<string>();

    [Header("Panel References")]
    public GameObject reportPanel;              // Report panel reference

    void Start()
    {
        // Get or create CanvasGroup component
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Clear all texts on initialization
        ClearAllTexts();
        ClearDynamicTexts();

        // Show canvas but hide report panel initially
        if (reportCanvas != null)
        {
            reportCanvas.gameObject.SetActive(true);
        }

        // Hide report panel initially
        if (reportPanel != null)
        {
            reportPanel.SetActive(false);
        }

        Debug.Log("ClipboardReportDisplay initialized successfully");
    }

    void Update()
    {
        // 移除E键监听，让ScoreManager完全控制
        // 不再需要等待E键，ScoreManager会直接调用PrepareReportAndDisplay

        // Debug key for manual testing (F1)
        if (Input.GetKeyDown(KeyCode.F1) && debugTextClearing)
        {
            Debug.Log("=== Manual text clearing triggered ===");
            ClearDynamicTexts();
        }
    }

    // 新方法：接收数据并立即显示（支持动态总分）
    public void PrepareReportAndDisplay(int totalScore, int maxScore, List<string> positiveReasons, List<string> improvementReasons, int interactionCount)
    {
        Debug.Log($"PrepareReportAndDisplay called - Score: {totalScore}/{maxScore}");

        // Store report data
        storedScore = totalScore;
        storedMaxScore = maxScore;
        storedInteractionCount = interactionCount;
        storedPositiveReasons = new List<string>(positiveReasons ?? new List<string>());
        storedImprovementReasons = new List<string>(improvementReasons ?? new List<string>());

        reportDataAvailable = true;
        reportDisplayed = false;

        // Clear previous content
        ClearReportTexts();
        ClearDynamicTexts();

        // 立即开始显示报告
        StartDisplayReportImmediately();
    }

    // 重载方法：接收数据并立即显示（兼容旧的调用方式，假设10分制）
    public void PrepareReportAndDisplay(int totalScore, List<string> positiveReasons, List<string> improvementReasons, int interactionCount)
    {
        PrepareReportAndDisplay(totalScore, 10, positiveReasons, improvementReasons, interactionCount);
    }

    // 保留原有方法用于兼容性（但现在立即显示）
    public void PrepareReport(int totalScore, List<string> positiveReasons, List<string> improvementReasons, int interactionCount)
    {
        Debug.Log("PrepareReport called - using new immediate display method");
        PrepareReportAndDisplay(totalScore, positiveReasons, improvementReasons, interactionCount);
    }

    private void StartDisplayReportImmediately()
    {
        if (!reportDataAvailable)
        {
            Debug.LogError("Report data is not ready");
            return;
        }

        Debug.Log("Starting immediate report display");

        // Hide any prompt text
        if (promptText != null)
        {
            promptText.text = "";
        }

        // Show report panel
        if (reportPanel != null)
        {
            reportPanel.SetActive(true);
            Debug.Log("Showing report panel immediately");
        }

        reportDisplayed = true;

        // Start displaying report
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
        }

        currentDisplayCoroutine = StartCoroutine(DisplayReportCoroutine());
    }

    // 旧方法：等待E键的显示方式（保留用于其他可能的调用）
    private void StartDisplayReport()
    {
        StartDisplayReportImmediately(); // 现在直接调用立即显示
    }

    private void ShowPrompt()
    {
        // Hide report panel
        if (reportPanel != null)
        {
            reportPanel.SetActive(false);
        }

        // Clear texts again to ensure clean state
        ClearReportTexts();
        ClearDynamicTexts();

        if (promptText != null)
        {
            promptText.text = "Press E to view report.";

            if (useCustomPromptColor)
            {
                Debug.Log($"Keeping custom prompt color: {promptText.color}");
            }
            else
            {
                promptText.color = promptColor;
            }

            Debug.Log("Showing prompt text: Press E to view report");
        }
        else
        {
            Debug.LogWarning("Prompt text reference is null, cannot display prompt");
        }
    }

    // 核心方法：清除动态文本内容
    private void ClearDynamicTexts()
    {
        if (!clearAllNonReportTexts) return;

        if (debugTextClearing)
        {
            Debug.Log("=== Starting Dynamic Text Clearing ===");
        }

        int clearedCount = 0;
        int protectedCount = 0;

        // 查找场景中所有的 TextMeshProUGUI 组件
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>();

        foreach (var textComponent in allTexts)
        {
            // 跳过我们自己的报告相关文本组件
            if (IsReportTextComponent(textComponent))
            {
                continue;
            }

            // 如果文本组件有内容
            if (!string.IsNullOrEmpty(textComponent.text))
            {
                // 检查是否是受保护的文本
                if (IsProtectedText(textComponent.text))
                {
                    protectedCount++;
                    if (debugTextClearing)
                    {
                        Debug.Log($"Protected text preserved: '{textComponent.name}' - Content: '{textComponent.text}'");
                    }
                    continue;
                }

                // 清除非保护的文本
                if (debugTextClearing)
                {
                    Debug.Log($"Clearing dynamic text from: '{textComponent.name}'");
                    Debug.Log($"  GameObject: '{textComponent.gameObject.name}'");
                    Debug.Log($"  Content: '{textComponent.text.Substring(0, Mathf.Min(50, textComponent.text.Length))}...'");
                    Debug.Log($"  Canvas: '{GetCanvasName(textComponent)}'");
                }

                textComponent.text = "";
                clearedCount++;
            }
        }

        if (debugTextClearing)
        {
            Debug.Log($"=== Cleared {clearedCount} dynamic text components, protected {protectedCount} texts ===");
        }
    }

    // 检查文本是否受保护（不应该被清除）
    private bool IsProtectedText(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        string lowerText = text.ToLower();

        // 硬编码的受保护文本 - 特定的提示词
        if (lowerText.Contains("press") && lowerText.Contains("enter") && lowerText.Contains("to go to the report"))
        {
            return true;
        }

        return false;
    }

    // 检查是否是报告相关的文本组件
    private bool IsReportTextComponent(TextMeshProUGUI textComponent)
    {
        return textComponent == titleText ||
               textComponent == scoreText ||
               textComponent == summaryText ||
               textComponent == promptText;
    }

    // 获取文本组件所在的Canvas名称（用于调试）
    private string GetCanvasName(TextMeshProUGUI textComponent)
    {
        Canvas canvas = textComponent.GetComponentInParent<Canvas>();
        return canvas != null ? canvas.name : "No Canvas";
    }

    private IEnumerator DisplayReportCoroutine()
    {
        Debug.Log("Starting report display animation");

        // Clear all report texts
        ClearReportTexts();

        // Fade in effect (optional)
        yield return StartCoroutine(FadeInCanvas());

        yield return new WaitForSeconds(0.5f);

        // 1. Display title
        string titleContent = "NURSING EVALUATION REPORT";
        yield return StartCoroutine(DisplayTextWithEffect(titleText, titleContent, titleColor));

        yield return new WaitForSeconds(0.3f);

        // 2. Display score
        string scoreContent = GenerateScoreContent(storedScore, storedInteractionCount);
        yield return StartCoroutine(DisplayTextWithEffect(scoreText, scoreContent, GetScoreColor(storedScore)));

        yield return new WaitForSeconds(0.5f);

        // 3. Display enhanced summary with criteria details
        string summaryContent = GenerateEnhancedSummaryContent(storedScore);
        yield return StartCoroutine(DisplayTextWithEffect(summaryText, summaryContent, summaryColor));

        Debug.Log("Report display completed");
    }

    private IEnumerator FadeInCanvas()
    {
        if (canvasGroup == null) yield break;

        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator DisplayTextWithEffect(TextMeshProUGUI textComponent, string content, Color textColor)
    {
        if (textComponent == null) yield break;

        // Use custom color settings if enabled for score text
        if (textComponent == scoreText && useCustomPromptColor)
        {
            Debug.Log($"Keeping custom score color: {textComponent.color}");
        }
        else
        {
            textComponent.color = textColor;
        }

        if (useTypewriterEffect)
        {
            // Typewriter effect
            textComponent.text = "";
            for (int i = 0; i <= content.Length; i++)
            {
                textComponent.text = content.Substring(0, i);
                yield return new WaitForSeconds(typewriterSpeed);
            }
        }
        else
        {
            // Direct display
            textComponent.text = content;
        }
    }

    private string GenerateScoreContent(int totalScore, int interactionCount)
    {
        string scoreLevel = GetScoreLevel(totalScore);
        return $"Total Score: {totalScore}/{storedMaxScore}\n" +
               $"Rating: {scoreLevel}\n" +
               $"Interactions: {interactionCount}\n" +
               $"Evaluation Time: {System.DateTime.Now:yyyy-MM-dd HH:mm}";
    }

    private string GenerateEnhancedSummaryContent(int totalScore)
    {
        string recommendation = "";

        if (totalScore >= 8)
        {
            recommendation = "Excellent performance! Continue to maintain your professional nursing communication skills.";
        }
        else if (totalScore >= 6)
        {
            recommendation = "Good performance. There are some areas for improvement. Keep up the good work!";
        }
        else if (totalScore >= 4)
        {
            recommendation = "Satisfactory performance. Consider enhancing your patient communication skills through additional practice.";
        }
        else
        {
            recommendation = "Needs significant improvement. Review fundamental nursing communication principles.";
        }

        // Enhanced summary with criteria details
        string summaryContent = $"Summary:\n" +
                               $"==========================================\n" +
                               $"{recommendation}\n\n";

        // Add positive aspects
        if (storedPositiveReasons.Count > 0)
        {
            summaryContent += "Strengths:\n";
            for (int i = 0; i < storedPositiveReasons.Count && i < 3; i++) // Limit to 3 items for UI space
            {
                summaryContent += $"• {storedPositiveReasons[i]}\n";
            }
            summaryContent += "\n";
        }

        // Add improvement areas
        if (storedImprovementReasons.Count > 0)
        {
            summaryContent += "Areas for Improvement:\n";
            for (int i = 0; i < storedImprovementReasons.Count && i < 3; i++) // Limit to 3 items for UI space
            {
                summaryContent += $"• {storedImprovementReasons[i]}\n";
            }
            summaryContent += "\n";
        }

        summaryContent += "Press [ESC] to return to scene";

        return summaryContent;
    }

    // 保留原有的简单summary方法用于兼容
    private string GenerateSummaryContent(int totalScore)
    {
        return GenerateEnhancedSummaryContent(totalScore);
    }

    private Color GetScoreColor(int score)
    {
        if (score >= 8) return Color.green;      // Excellent - Green
        else if (score >= 6) return Color.blue; // Good - Blue
        else if (score >= 4) return Color.yellow; // Satisfactory - Yellow
        else return Color.red;                   // Needs Improvement - Red
    }

    private string GetScoreLevel(int score)
    {
        if (score >= 8) return "Excellent";
        else if (score >= 6) return "Good";
        else if (score >= 4) return "Satisfactory";
        else return "Needs Improvement";
    }

    private void ClearAllTexts()
    {
        ClearReportTexts();
    }

    private void ClearReportTexts()
    {
        if (titleText != null) titleText.text = "";
        if (scoreText != null) scoreText.text = "";
        if (summaryText != null) summaryText.text = "";
    }

    // Hide report
    public void HideReport()
    {
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
            currentDisplayCoroutine = null;
        }

        // Hide report panel
        if (reportPanel != null)
        {
            reportPanel.SetActive(false);
        }

        ClearAllTexts();
        ClearDynamicTexts();

        reportDisplayed = false;
        reportDataAvailable = false;
    }

    // Compatibility method for old DisplayReport calls
    public void DisplayReport(int totalScore, List<string> positiveReasons, List<string> improvementReasons, int interactionCount)
    {
        PrepareReportAndDisplay(totalScore, positiveReasons, improvementReasons, interactionCount);
    }
}