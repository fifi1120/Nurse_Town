using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraClipboardController : MonoBehaviour
{
    [Header("Camera References")]
    public Camera mainCamera;              // 主摄像机
    public Transform clipboardViewPosition; // clipboard查看位置

    [Header("Original Camera Settings")]
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isViewingClipboard = false;

    [Header("Animation Settings")]
    public float transitionDuration = 2f;   // 切换动画时长
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("UI References")]
    public GameObject instructionText;      // "Press Enter" 提示文本
    public GameObject clipboardReport;      // clipboard报告对象

    private static CameraClipboardController instance;
    public static CameraClipboardController Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<CameraClipboardController>();
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("CameraClipboardController Start方法执行了");

        // 保存原始摄像机位置和旋转
        if (mainCamera == null)
            mainCamera = Camera.main;

        originalPosition = mainCamera.transform.position;
        originalRotation = mainCamera.transform.rotation;

        Debug.Log($"保存的原始摄像机位置: {originalPosition}");

        // 初始状态下隐藏clipboard报告
        if (clipboardReport != null)
            clipboardReport.SetActive(false);

        // 初始状态下隐藏"Press Enter"提示文本，等待条件满足
        UpdateInstructionTextVisibility();
    }

    void Update()
    {
        // 检查时间缩放
        if (Time.timeScale == 0)
        {
            Debug.Log("游戏被暂停了，Time.timeScale = 0");
            return;
        }

        // 添加持续的心跳检测
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log($"T键按下，Time.timeScale = {Time.timeScale}");
        }

        // 持续检查并更新提示文本的显示状态
        UpdateInstructionTextVisibility();

        // 检测Enter键输入
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Debug.Log("检测到Enter键按下");

            if (!isViewingClipboard)
            {
                Debug.Log("当前不在clipboard视角");

                // 使用ScoreManager检查状态
                ScoreManager scoreManager = ScoreManager.Instance;
                if (scoreManager == null)
                {
                    scoreManager = FindObjectOfType<ScoreManager>();
                }

                if (scoreManager == null)
                {
                    Debug.LogError("找不到ScoreManager脚本");
                }
                else
                {
                    Debug.Log($"找到ScoreManager，ConversationCount: {scoreManager.GetConversationCount()}");

                    // 检查是否有足够的对话轮次（5轮）
                    if (scoreManager.CanViewReport())
                    {
                        Debug.Log("对话完成，条件满足，开始切换摄像机到clipboard视角");
                        SwitchToClipboardView();
                    }
                    else
                    {
                        int remaining = 5 - scoreManager.GetConversationCount();
                        Debug.Log($"对话不足，还需要 {remaining} 轮对话才能查看报告");
                    }
                }
            }
            else
            {
                Debug.Log("当前在clipboard视角");
            }
        }

        // 在clipboard视图时，按ESC返回
        if (Input.GetKeyDown(KeyCode.Escape) && isViewingClipboard)
        {
            Debug.Log("检测到ESC键，返回原视角");
            ReturnToOriginalView();
        }
    }

    public void SwitchToClipboardView()
    {
        if (isViewingClipboard || clipboardViewPosition == null)
        {
            Debug.Log("无法切换：已在clipboard视角或clipboardViewPosition为空");
            return;
        }

        Debug.Log("开始切换到clipboard视角");
        StartCoroutine(TransitionToClipboard());
    }

    public void ReturnToOriginalView()
    {
        if (!isViewingClipboard) return;

        Debug.Log("开始返回原始视角");
        StartCoroutine(TransitionToOriginal());
    }

    private IEnumerator TransitionToClipboard()
    {
        isViewingClipboard = true;

        // 隐藏提示文本
        if (instructionText != null)
        {
            instructionText.SetActive(false);
            Debug.Log("隐藏提示文本");
        }

        // 摄像机平滑移动到clipboard位置
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        Vector3 targetPos = clipboardViewPosition.position;
        Quaternion targetRot = clipboardViewPosition.rotation;

        Debug.Log($"摄像机从 {startPos} 移动到 {targetPos}");

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / transitionDuration;
            float curveValue = transitionCurve.Evaluate(progress);

            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, curveValue);
            mainCamera.transform.rotation = Quaternion.Lerp(startRot, targetRot, curveValue);

            yield return null;
        }

        // 确保最终位置准确
        mainCamera.transform.position = targetPos;
        mainCamera.transform.rotation = targetRot;

        Debug.Log("摄像机切换完成，显示clipboard报告");

        // 显示clipboard报告
        ShowClipboardReport();
    }

    private IEnumerator TransitionToOriginal()
    {
        // 隐藏clipboard报告
        if (clipboardReport != null)
        {
            clipboardReport.SetActive(false);
            Debug.Log("隐藏clipboard报告");
        }

        // 摄像机平滑移动回原位置
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        Debug.Log($"摄像机从 {startPos} 返回到 {originalPosition}");

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / transitionDuration;
            float curveValue = transitionCurve.Evaluate(progress);

            mainCamera.transform.position = Vector3.Lerp(startPos, originalPosition, curveValue);
            mainCamera.transform.rotation = Quaternion.Lerp(startRot, originalRotation, curveValue);

            yield return null;
        }

        // 确保最终位置准确
        mainCamera.transform.position = originalPosition;
        mainCamera.transform.rotation = originalRotation;

        // 显示提示文本（但要先检查条件）
        UpdateInstructionTextVisibility();

        isViewingClipboard = false;
        Debug.Log("返回原始视角完成");
    }

    // 新增方法：更新"Press Enter"提示文本的显示状态
    private void UpdateInstructionTextVisibility()
    {
        if (instructionText == null) return;

        // 检查ScoreManager的对话状态
        ScoreManager scoreManager = ScoreManager.Instance;
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
        }

        bool shouldShow = false;
        if (scoreManager != null)
        {
            // 只有当对话数达到5轮且不在clipboard视角时才显示
            shouldShow = scoreManager.CanViewReport() && !isViewingClipboard;
        }

        // 只在状态改变时更新，避免频繁操作
        bool currentlyActive = instructionText.activeSelf;
        if (currentlyActive != shouldShow)
        {
            instructionText.SetActive(shouldShow);
            if (shouldShow)
            {
                Debug.Log("✅ 条件满足！显示'Press Enter'提示文本");
            }
            else
            {
                Debug.Log("❌ 隐藏'Press Enter'提示文本");
            }
        }
    }

    private void ShowClipboardReport()
    {
        if (clipboardReport != null)
        {
            clipboardReport.SetActive(true);
            Debug.Log("激活clipboard报告对象");

            // 使用ScoreManager
            ScoreManager scoreManager = ScoreManager.Instance;
            if (scoreManager == null)
            {
                scoreManager = FindObjectOfType<ScoreManager>();
            }

            if (scoreManager != null)
            {
                ClipboardReportDisplay reportDisplay = clipboardReport.GetComponent<ClipboardReportDisplay>();
                if (reportDisplay != null)
                {
                    Debug.Log("找到ClipboardReportDisplay组件");
                    Debug.Log("在clipboard视角，用户可以按E键生成报告");

                    // ScoreManager会在用户按E键时自动处理Console输出和UI显示
                    // 这里不需要手动传递数据
                }
                else
                {
                    Debug.LogError("clipboardReport对象上找不到ClipboardReportDisplay组件");
                }
            }
            else
            {
                Debug.LogError("找不到ScoreManager脚本");
            }
        }
        else
        {
            Debug.LogError("clipboardReport对象为空");
        }
    }

    // 公共方法：强制切换到clipboard（供其他脚本调用）
    public void ForceShowClipboard()
    {
        if (!isViewingClipboard)
        {
            SwitchToClipboardView();
        }
    }

    // 检查当前是否在查看clipboard
    public bool IsViewingClipboard()
    {
        return isViewingClipboard;
    }
}