using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public enum ControlMode
{
    HandGesture,
    Keyboard
}

public class DroneController : MonoBehaviour
{
    [Header("Control Settings")]
    public ControlMode currentMode = ControlMode.HandGesture;

    [Header("Python Server Settings")]
    [Tooltip("PyInstaller로 생성한 단일 파이썬 실행 파일 이름")]
    public string serverExecutableName = "drone_server.exe";
    public int serverPort = 5001;

    [Header("Drone Movement Settings")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 150f;

    private Process pythonProcess;
    private TcpClient client;
    private StreamReader reader;

    private string leftCommand = "NONE";
    private string rightCommand = "NONE";

    void Start()
    {
        string savedMode = PlayerPrefs.GetString("ControlType", "Hand");
        currentMode = (savedMode == "Hand") ? ControlMode.HandGesture : ControlMode.Keyboard;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        if (currentMode == ControlMode.HandGesture)
        {
            StartPythonServer();
            Invoke(nameof(ConnectToServer), 2.0f);
        }
    }

    void StartPythonServer()
    {
        Process[] existingExe = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(serverExecutableName));
        if (existingExe.Length > 0)
        {
            Debug.Log("파이썬 서버가 이미 백그라운드에서 실행 중입니다.");
            return;
        }

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();

#if UNITY_EDITOR
            string rootPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string venvPython = Path.Combine(rootPath, "venv", "Scripts", "python.exe");
            string scriptPath = Path.Combine(rootPath, "drone_server.py");

            string pythonExec = File.Exists(venvPython) ? venvPython : "python";

            startInfo.FileName = pythonExec;
            startInfo.Arguments = $"\"{scriptPath}\"";
            startInfo.WorkingDirectory = rootPath;
            
            // 터미널 창(검은 창) 숨기기 세팅
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
#else
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string fullExePath = Path.Combine(baseDir, "dist", serverExecutableName);
            string workingDir = Path.Combine(baseDir, "dist");

            if (!File.Exists(fullExePath))
            {
                fullExePath = Path.Combine(baseDir, serverExecutableName);
                workingDir = baseDir;
            }

            if (!File.Exists(fullExePath))
            {
                Debug.LogError("파이썬 서버 실행 파일을 찾을 수 없습니다: " + fullExePath);
                currentMode = ControlMode.Keyboard;
                return;
            }

            startInfo.FileName = fullExePath;
            startInfo.WorkingDirectory = workingDir;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
#endif

            pythonProcess = Process.Start(startInfo);
            Debug.Log("파이썬 서버 자동 실행 성공 (터미널 숨김)");
        }
        catch (Exception e)
        {
            Debug.LogError("파이썬 서버 실행 실패: " + e.Message);
            currentMode = ControlMode.Keyboard;
        }
    }

    void ConnectToServer()
    {
        if (currentMode != ControlMode.HandGesture) return;
        if (client != null && client.Connected) return;

        try
        {
            client = new TcpClient();
            var result = client.BeginConnect("127.0.0.1", serverPort, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

            if (!success)
            {
                throw new SocketException();
            }

            client.EndConnect(result);
            NetworkStream stream = client.GetStream();
            reader = new StreamReader(stream, Encoding.UTF8);
            Debug.Log("파이썬 서버 연결 성공!");
        }
        catch (Exception)
        {
            Debug.LogWarning("서버 연결 대기 중... 재시도합니다.");
            if (client != null) { client.Close(); client = null; }
            Invoke(nameof(ConnectToServer), 1.0f);
        }
    }

    void Update()
    {
        if (currentMode == ControlMode.Keyboard)
        {
            ProcessKeyboardInput();
        }
        else if (currentMode == ControlMode.HandGesture)
        {
            ProcessSocketInput();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToStartScene();
        }
    }

    private void ProcessKeyboardInput()
    {
        if (Input.GetKey(KeyCode.W)) leftCommand = "UP";
        else if (Input.GetKey(KeyCode.S)) leftCommand = "DOWN";
        else if (Input.GetKey(KeyCode.A)) leftCommand = "LEFT";
        else if (Input.GetKey(KeyCode.D)) leftCommand = "RIGHT";
        else leftCommand = "NONE";

        if (Input.GetKey(KeyCode.UpArrow)) rightCommand = "FORWARD";
        else if (Input.GetKey(KeyCode.DownArrow)) rightCommand = "BACKWARD";
        else if (Input.GetKey(KeyCode.LeftArrow)) rightCommand = "ROTATE_LEFT";
        else if (Input.GetKey(KeyCode.RightArrow)) rightCommand = "ROTATE_RIGHT";
        else rightCommand = "NONE";

        if (Input.GetKey(KeyCode.Space))
        {
            leftCommand = "STOP";
            rightCommand = "STOP";
        }
    }

    private void ProcessSocketInput()
    {
        if (client != null && client.Connected && client.GetStream().DataAvailable)
        {
            try
            {
                string data = reader.ReadLine();
                if (!string.IsNullOrEmpty(data))
                {
                    string[] cmds = data.Trim().Split(',');
                    if (cmds.Length == 2)
                    {
                        leftCommand = cmds[0];
                        rightCommand = cmds[1];
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("데이터 수신 오류: " + e.Message);
            }
        }
    }

    void FixedUpdate()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.useGravity = false;

        if (leftCommand == "STOP" && rightCommand == "STOP")
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        if (rightCommand == "ROTATE_LEFT")
        {
            transform.Rotate(0f, -rotateSpeed * Time.fixedDeltaTime, 0f, Space.World);
        }
        else if (rightCommand == "ROTATE_RIGHT")
        {
            transform.Rotate(0f, rotateSpeed * Time.fixedDeltaTime, 0f, Space.World);
        }

        Vector3 moveDir = Vector3.zero;

        if (leftCommand == "UP") moveDir += Vector3.up * moveSpeed;
        else if (leftCommand == "DOWN") moveDir += Vector3.down * moveSpeed;

        if (leftCommand == "LEFT") moveDir -= transform.right * moveSpeed;
        else if (leftCommand == "RIGHT") moveDir += transform.right * moveSpeed;

        if (rightCommand == "FORWARD") moveDir += transform.forward * moveSpeed;
        else if (rightCommand == "BACKWARD") moveDir -= transform.forward * moveSpeed;

        rb.linearVelocity = moveDir;
    }

    public void ReturnToStartScene()
    {
        StopPythonProcess();
        SceneManager.LoadScene("Start");
    }

    private void StopPythonProcess()
    {
        CancelInvoke(nameof(ConnectToServer));

        if (reader != null) { reader.Close(); reader = null; }
        if (client != null) { client.Close(); client = null; }

        if (pythonProcess != null)
        {
            try
            {
                if (!pythonProcess.HasExited)
                {
                    pythonProcess.Kill();
                    pythonProcess.WaitForExit(1000);
                }
                pythonProcess.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning("파이썬 프로세스 종료 중 예외: " + e.Message);
            }
            finally
            {
                pythonProcess = null;
            }
        }
    }

    private void OnDisable() => StopPythonProcess();
    private void OnDestroy() => StopPythonProcess();
    private void OnApplicationQuit() => StopPythonProcess();
}