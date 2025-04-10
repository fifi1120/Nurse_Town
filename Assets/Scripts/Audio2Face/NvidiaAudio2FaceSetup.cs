using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Sets up NVIDIA Audio2Face Python environment according to official documentation
/// </summary>
public class NvidiaAudio2FaceSetup : MonoBehaviour
{
    [Header("Setup Configuration")]
    [Tooltip("Path where the Python virtual environment will be created")]
    public string venvPath = "Audio2Face_Env";
    
    [Tooltip("Path to the NVIDIA A2F samples repository")]
    public string samplesRepoPath = "";
    
    [Tooltip("Run setup automatically on start")]
    public bool setupOnStart = true;
    
    [Tooltip("Show detailed logs")]
    public bool showDetailedLogs = true;
    
    [Header("Status")]
    [SerializeField] private bool isSetupComplete = false;
    [SerializeField] private string pythonPath = "";
    [SerializeField] private string setupStatus = "Not started";
    
    // Make instance accessible
    public static NvidiaAudio2FaceSetup Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        // Set default paths if not specified
        if (string.IsNullOrEmpty(venvPath))
        {
            venvPath = Path.Combine(Application.dataPath, "../Audio2Face_Env");
        }
        
        if (string.IsNullOrEmpty(samplesRepoPath))
        {
            samplesRepoPath = Path.Combine(Application.dataPath, "../Audio2Face-3D-Samples");
        }
    }
    
    private void Start()
    {
        if (setupOnStart)
        {
            StartCoroutine(SetupEnvironment());
        }
    }
    
    /// <summary>
    /// Set up the NVIDIA Audio2Face environment following official documentation
    /// </summary>
    public IEnumerator SetupEnvironment()
    {
        setupStatus = "Starting setup...";
        Debug.Log("Starting NVIDIA Audio2Face setup...");
        
        // Step 1: Check if virtual environment already exists
        if (IsEnvironmentSetup())
        {
            setupStatus = "Environment already set up!";
            Debug.Log("NVIDIA Audio2Face environment is already set up!");
            isSetupComplete = true;
            UpdatePythonPath();
            yield break;
        }
        
        // Step 2: Create Python virtual environment
        setupStatus = "Creating virtual environment...";
        yield return StartCoroutine(CreateVirtualEnvironment());
        
        // Step 3: Check/Clone NVIDIA samples repository
        setupStatus = "Checking NVIDIA Audio2Face samples...";
        yield return StartCoroutine(CheckSamplesRepository());
        
        // Step 4: Install dependencies
        setupStatus = "Installing dependencies...";
        yield return StartCoroutine(InstallDependencies());
        
        // Step 5: Verify setup
        setupStatus = "Verifying setup...";
        yield return StartCoroutine(VerifySetup());
        
        if (isSetupComplete)
        {
            setupStatus = "Setup complete!";
            Debug.Log("NVIDIA Audio2Face setup complete!");
            UpdatePythonPath();
            
            // Notify Audio2FaceManager if it exists
            Audio2FaceManager manager = FindObjectOfType<Audio2FaceManager>();
            if (manager != null)
            {
                manager.pythonExecutablePath = pythonPath;
                manager.pythonScriptPath = Path.Combine(samplesRepoPath, "scripts/audio2face_3d_api_client/nim_a2f_3d_client.py");
                manager.configDirectoryPath = Path.Combine(samplesRepoPath, "scripts/audio2face_3d_api_client/config");
                Debug.Log("Updated Audio2FaceManager with correct paths");
            }
        }
        else
        {
            setupStatus = "Setup failed";
            Debug.LogError("NVIDIA Audio2Face setup failed. Check logs for details.");
        }
    }
    
    /// <summary>
    /// Check if the environment is already set up
    /// </summary>
    private bool IsEnvironmentSetup()
    {
        string venvPythonPath = GetVenvPythonPath();
        bool venvExists = File.Exists(venvPythonPath);
        
        if (venvExists)
        {
            Debug.Log($"Found existing virtual environment at: {venvPath}");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Create Python virtual environment
    /// </summary>
    private IEnumerator CreateVirtualEnvironment()
    {
        Debug.Log("Creating Python virtual environment...");
        
        // Find Python executable
        string pythonCommand = FindPythonCommand();
        if (string.IsNullOrEmpty(pythonCommand))
        {
            Debug.LogError("Python 3+ not found. Please install Python 3.x before continuing.");
            yield break;
        }
        
        Debug.Log($"Using Python command: {pythonCommand}");
        
        // Create directory if it doesn't exist
        Directory.CreateDirectory(Path.GetDirectoryName(venvPath));
        
        // Build the command to create virtual environment
        ProcessStartInfo psi = new ProcessStartInfo();
        
        if (Application.platform == RuntimePlatform.WindowsEditor || 
            Application.platform == RuntimePlatform.WindowsPlayer)
        {
            psi.FileName = "cmd.exe";
            psi.Arguments = $"/c {pythonCommand} -m venv \"{venvPath}\"";
        }
        else
        {
            psi.FileName = "/bin/bash";
            psi.Arguments = $"-c \"{pythonCommand} -m venv \"{venvPath}\"\"";
        }
        
        psi.UseShellExecute = false;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.CreateNoWindow = true;
        
        // Execute the process
        Process process = new Process { StartInfo = psi };
        
        StringBuilder output = new StringBuilder();
        StringBuilder error = new StringBuilder();
        
        process.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data))
            {
                output.AppendLine(e.Data);
                if (showDetailedLogs) Debug.Log($"Output: {e.Data}");
            }
        };
        
        process.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data))
            {
                error.AppendLine(e.Data);
                Debug.LogWarning($"Error: {e.Data}");
            }
        };
        
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        while (!process.HasExited)
        {
            yield return null;
        }
        
        if (process.ExitCode != 0)
        {
            Debug.LogError($"Failed to create virtual environment: {error}");
            yield break;
        }
        
        Debug.Log("Virtual environment created successfully");
    }
    
    /// <summary>
    /// Check for NVIDIA samples repository and clone if needed
    /// </summary>
    private IEnumerator CheckSamplesRepository()
    {
        Debug.Log("Checking for NVIDIA Audio2Face samples repository...");
        
        bool samplesExist = Directory.Exists(Path.Combine(samplesRepoPath, "scripts/audio2face_3d_api_client"));
        
        if (samplesExist)
        {
            Debug.Log("NVIDIA Audio2Face samples already exist");
            yield break;
        }
        
        Debug.Log("NVIDIA Audio2Face samples not found. Attempting to clone repository...");
        
        // Check if git is available
        ProcessStartInfo gitCheck = new ProcessStartInfo();
        
        if (Application.platform == RuntimePlatform.WindowsEditor || 
            Application.platform == RuntimePlatform.WindowsPlayer)
        {
            gitCheck.FileName = "cmd.exe";
            gitCheck.Arguments = "/c git --version";
        }
        else
        {
            gitCheck.FileName = "/bin/bash";
            gitCheck.Arguments = "-c \"git --version\"";
        }
        
        gitCheck.UseShellExecute = false;
        gitCheck.RedirectStandardOutput = true;
        gitCheck.CreateNoWindow = true;
        
        Process gitCheckProcess = new Process { StartInfo = gitCheck };
        gitCheckProcess.Start();
        gitCheckProcess.WaitForExit();
        
        if (gitCheckProcess.ExitCode != 0)
        {
            Debug.LogError("Git not found. Please install Git or manually clone the repository.");
            Debug.LogError("Repository URL: https://github.com/NVIDIA/Audio2Face-3D-Samples.git");
            yield break;
        }
        
        // Clone the repository
        ProcessStartInfo gitClone = new ProcessStartInfo();
        
        if (Application.platform == RuntimePlatform.WindowsEditor || 
            Application.platform == RuntimePlatform.WindowsPlayer)
        {
            gitClone.FileName = "cmd.exe";
            gitClone.Arguments = $"/c git clone https://github.com/NVIDIA/Audio2Face-3D-Samples.git \"{samplesRepoPath}\"";
        }
        else
        {
            gitClone.FileName = "/bin/bash";
            gitClone.Arguments = $"-c \"git clone https://github.com/NVIDIA/Audio2Face-3D-Samples.git \\\"{samplesRepoPath}\\\"\"";
        }
        
        gitClone.UseShellExecute = false;
        gitClone.RedirectStandardOutput = true;
        gitClone.RedirectStandardError = true;
        gitClone.CreateNoWindow = true;
        
        Process gitCloneProcess = new Process { StartInfo = gitClone };
        
        StringBuilder output = new StringBuilder();
        StringBuilder error = new StringBuilder();
        
        gitCloneProcess.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data))
            {
                output.AppendLine(e.Data);
                if (showDetailedLogs) Debug.Log($"Git: {e.Data}");
            }
        };
        
        gitCloneProcess.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data))
            {
                error.AppendLine(e.Data);
                Debug.LogWarning($"Git Error: {e.Data}");
            }
        };
        
        gitCloneProcess.Start();
        gitCloneProcess.BeginOutputReadLine();
        gitCloneProcess.BeginErrorReadLine();
        
        while (!gitCloneProcess.HasExited)
        {
            yield return null;
        }
        
        if (gitCloneProcess.ExitCode != 0)
        {
            Debug.LogError($"Failed to clone repository: {error}");
            yield break;
        }
        
        Debug.Log("NVIDIA Audio2Face samples repository cloned successfully");
    }
    
    /// <summary>
    /// Install required dependencies
    /// </summary>
    private IEnumerator InstallDependencies()
    {
        Debug.Log("Installing required dependencies...");
        
        // Get the path to the Python executable in the virtual environment
        string venvPythonPath = GetVenvPythonPath();
        string pipCommand = GetVenvPipCommand();
        
        if (!File.Exists(venvPythonPath))
        {
            Debug.LogError($"Python not found in virtual environment: {venvPythonPath}");
            yield break;
        }
        
        // Create activation command
        string activateCmd = "";
        
        if (Application.platform == RuntimePlatform.WindowsEditor || 
            Application.platform == RuntimePlatform.WindowsPlayer)
        {
            activateCmd = $"\"{Path.Combine(venvPath, "Scripts", "activate.bat")}\" && ";
        }
        else
        {
            activateCmd = $"source \"{Path.Combine(venvPath, "bin", "activate")}\" && ";
        }
        
        // Determine script and wheel paths
        string clientScriptPath = Path.Combine(samplesRepoPath, "scripts/audio2face_3d_api_client");
        string wheelPath = Path.Combine(samplesRepoPath, "proto/sample_wheel/nvidia_ace-1.2.0-py3-none-any.whl");
        
        // Check if the wheel file exists
        bool wheelExists = File.Exists(wheelPath);
        if (!wheelExists)
        {
            Debug.LogWarning($"Wheel file not found at: {wheelPath}. Will skip this step.");
        }
        
        // Check if requirements.txt exists
        string requirementsPath = Path.Combine(clientScriptPath, "requirements.txt");
        bool requirementsExist = File.Exists(requirementsPath);
        if (!requirementsExist)
        {
            // Create a requirements.txt file with the required dependencies
            string requirements = "grpcio\ngrpcio-tools\nnumpy\npyyaml\n";
            File.WriteAllText(requirementsPath, requirements);
            Debug.Log("Created requirements.txt file with basic dependencies");
        }
        
        // Install Python wheel if it exists
        if (wheelExists)
        {
            ProcessStartInfo installWheel = new ProcessStartInfo();
            
            if (Application.platform == RuntimePlatform.WindowsEditor || 
                Application.platform == RuntimePlatform.WindowsPlayer)
            {
                installWheel.FileName = "cmd.exe";
                installWheel.Arguments = $"/c {activateCmd}{pipCommand} install \"{wheelPath}\"";
            }
            else
            {
                installWheel.FileName = "/bin/bash";
                installWheel.Arguments = $"-c \"{activateCmd}{pipCommand} install \\\"{wheelPath}\\\"\"";
            }
            
            installWheel.UseShellExecute = false;
            installWheel.RedirectStandardOutput = true;
            installWheel.RedirectStandardError = true;
            installWheel.CreateNoWindow = true;
            
            Process installWheelProcess = new Process { StartInfo = installWheel };
            
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();
            
            installWheelProcess.OutputDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(e.Data);
                    if (showDetailedLogs) Debug.Log($"Pip: {e.Data}");
                }
            };
            
            installWheelProcess.ErrorDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    error.AppendLine(e.Data);
                    if (e.Data.Contains("ERROR"))
                        Debug.LogError($"Pip Error: {e.Data}");
                    else
                        Debug.LogWarning($"Pip Warning: {e.Data}");
                }
            };
            
            installWheelProcess.Start();
            installWheelProcess.BeginOutputReadLine();
            installWheelProcess.BeginErrorReadLine();
            
            while (!installWheelProcess.HasExited)
            {
                yield return null;
            }
            
            if (installWheelProcess.ExitCode != 0)
            {
                Debug.LogError($"Failed to install wheel: {error}");
                // Continue anyway, as this might not be critical
            }
            else
            {
                Debug.Log("NVIDIA wheel installed successfully");
            }
        }
        
        // Install requirements
        ProcessStartInfo installReqs = new ProcessStartInfo();
        
        if (Application.platform == RuntimePlatform.WindowsEditor || 
            Application.platform == RuntimePlatform.WindowsPlayer)
        {
            installReqs.FileName = "cmd.exe";
            installReqs.Arguments = $"/c {activateCmd}{pipCommand} install -r \"{requirementsPath}\"";
        }
        else
        {
            installReqs.FileName = "/bin/bash";
            installReqs.Arguments = $"-c \"{activateCmd}{pipCommand} install -r \\\"{requirementsPath}\\\"\"";
        }
        
        installReqs.UseShellExecute = false;
        installReqs.RedirectStandardOutput = true;
        installReqs.RedirectStandardError = true;
        installReqs.CreateNoWindow = true;
        
        Process installReqsProcess = new Process { StartInfo = installReqs };
        
        StringBuilder reqsOutput = new StringBuilder();
        StringBuilder reqsError = new StringBuilder();
        
        installReqsProcess.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data))
            {
                reqsOutput.AppendLine(e.Data);
                if (showDetailedLogs) Debug.Log($"Pip: {e.Data}");
            }
        };
        
        installReqsProcess.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data))
            {
                reqsError.AppendLine(e.Data);
                if (e.Data.Contains("ERROR"))
                    Debug.LogError($"Pip Error: {e.Data}");
                else
                    Debug.LogWarning($"Pip Warning: {e.Data}");
            }
        };
        
        installReqsProcess.Start();
        installReqsProcess.BeginOutputReadLine();
        installReqsProcess.BeginErrorReadLine();
        
        while (!installReqsProcess.HasExited)
        {
            yield return null;
        }
        
        if (installReqsProcess.ExitCode != 0)
        {
            Debug.LogError($"Failed to install requirements: {reqsError}");
            yield break;
        }
        
        Debug.Log("Dependencies installed successfully");
    }
    
    /// <summary>
    /// Verify the setup by running a simple Python check
    /// </summary>
    private IEnumerator VerifySetup()
    {
        Debug.Log("Verifying setup...");
        
        string venvPythonPath = GetVenvPythonPath();
        
        // Create a simple verification script
        string verifyScript = @"
import sys
try:
    import grpc
    import numpy
    import yaml
    print('All required modules are available!')
    print(f'Python version: {sys.version}')
    print(f'Modules: grpc, numpy, yaml')
    sys.exit(0)
except ImportError as e:
    print(f'Error: {e}')
    sys.exit(1)
";
        
        string scriptPath = Path.Combine(venvPath, "verify_setup.py");
        File.WriteAllText(scriptPath, verifyScript);
        
        // Run the verification script
        ProcessStartInfo verify = new ProcessStartInfo();
        verify.FileName = venvPythonPath;
        verify.Arguments = $"\"{scriptPath}\"";
        verify.UseShellExecute = false;
        verify.RedirectStandardOutput = true;
        verify.RedirectStandardError = true;
        verify.CreateNoWindow = true;
        
        Process verifyProcess = new Process { StartInfo = verify };
        
        StringBuilder output = new StringBuilder();
        StringBuilder error = new StringBuilder();
        
        verifyProcess.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data))
            {
                output.AppendLine(e.Data);
                Debug.Log($"Verify: {e.Data}");
            }
        };
        
        verifyProcess.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data))
            {
                error.AppendLine(e.Data);
                Debug.LogWarning($"Verify Error: {e.Data}");
            }
        };
        
        verifyProcess.Start();
        verifyProcess.BeginOutputReadLine();
        verifyProcess.BeginErrorReadLine();
        
        while (!verifyProcess.HasExited)
        {
            yield return null;
        }
        
        if (verifyProcess.ExitCode != 0)
        {
            Debug.LogError($"Verification failed: {error}");
            isSetupComplete = false;
        }
        else
        {
            Debug.Log("Verification successful!");
            isSetupComplete = true;
        }
        
        // Clean up
        try
        {
            File.Delete(scriptPath);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to delete verification script: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Find Python command on the system
    /// </summary>
    private string FindPythonCommand()
    {
        string[] possibleCommands = { "python3", "python" };
        
        foreach (string cmd in possibleCommands)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            
            if (Application.platform == RuntimePlatform.WindowsEditor || 
                Application.platform == RuntimePlatform.WindowsPlayer)
            {
                psi.FileName = "cmd.exe";
                psi.Arguments = $"/c {cmd} --version";
            }
            else
            {
                psi.FileName = "/bin/bash";
                psi.Arguments = $"-c \"{cmd} --version\"";
            }
            
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = true;
            
            try
            {
                Process process = new Process { StartInfo = psi };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                if (process.ExitCode == 0 && output.Contains("Python 3"))
                {
                    return cmd;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error checking Python command {cmd}: {ex.Message}");
            }
        }
        
        return "";
    }
    
    /// <summary>
    /// Get the path to the Python executable in the virtual environment
    /// </summary>
    private string GetVenvPythonPath()
    {
        if (Application.platform == RuntimePlatform.WindowsEditor || 
            Application.platform == RuntimePlatform.WindowsPlayer)
        {
            return Path.Combine(venvPath, "Scripts", "python.exe");
        }
        else
        {
            return Path.Combine(venvPath, "bin", "python");
        }
    }
    
    /// <summary>
    /// Get the pip command in the virtual environment
    /// </summary>
    private string GetVenvPipCommand()
    {
        if (Application.platform == RuntimePlatform.WindowsEditor || 
            Application.platform == RuntimePlatform.WindowsPlayer)
        {
            return Path.Combine(venvPath, "Scripts", "pip.exe");
        }
        else
        {
            return Path.Combine(venvPath, "bin", "pip");
        }
    }
    
    /// <summary>
    /// Update Python path variable for external access
    /// </summary>
    private void UpdatePythonPath()
    {
        pythonPath = GetVenvPythonPath();
    }
    
    /// <summary>
    /// Get the Python path for Audio2FaceManager
    /// </summary>
    public string GetPythonPath()
    {
        return pythonPath;
    }
    
    /// <summary>
    /// Check if setup is complete
    /// </summary>
    public bool IsSetupComplete()
    {
        return isSetupComplete;
    }
    
    /// <summary>
    /// Get the path to the NVIDIA client script
    /// </summary>
    public string GetClientScriptPath()
    {
        return Path.Combine(samplesRepoPath, "scripts/audio2face_3d_api_client/nim_a2f_3d_client.py");
    }
    
    /// <summary>
    /// Get the path to the config directory
    /// </summary>
    public string GetConfigDirectoryPath()
    {
        return Path.Combine(samplesRepoPath, "scripts/audio2face_3d_api_client/config");
    }
}