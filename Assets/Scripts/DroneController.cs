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

    [Header("Tilt Settings (드론 기울기 효과)")]
    public float maxTiltAngle = 15f;    // 최대 기울어지는 각도
    public float tiltSpeed = 5f;       // 기울어지는 반응 속도

    private Process pythonProcess;
    private TcpClient client;
    private StreamReader reader;

    private string leftCommand = "NONE";
    private string rightCommand = "NONE";

    // Yaw 회전각을 누적하기 위한 변수
    private float currentYaw = 0f;

    void Start()
    {
        string savedMode = PlayerPrefs.GetString("ControlType", "Hand");
        currentMode = (savedMode == "Hand") ? ControlMode.HandGesture : ControlMode.Keyboard;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        currentYaw = transform.eulerAngles.y;

        if (currentMode == ControlMode.HandGesture)
        {
            StartPythonServer();
            Invoke(nameof(ConnectToServer), 2.0f);
        }
    }

    void StartPythonServer()
    {
        // 1. 기존 중복 프로세스 강제 종료
        string procName = Path.GetFileNameWithoutExtension(serverExecutableName);
        Process[] existingExe = Process.GetProcessesByName(procName);
        foreach (var p in existingExe)
        {
            try { p.Kill(); } catch { }
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
            
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
#else
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // 1순위 경로 탐색: dist/drone_server/drone_server.exe (--onedir 기본 형태)
            string fullExePath = Path.Combine(baseDir, "dist", procName, serverExecutableName);
            string workingDir = Path.Combine(baseDir, "dist", procName);

            // 2순위 fallback: dist/drone_server.exe
            if (!File.Exists(fullExePath))
            {
                fullExePath = Path.Combine(baseDir, "dist", serverExecutableName);
                workingDir = Path.Combine(baseDir, "dist");
            }

            // 3순위 fallback: 루트 경로 (SAVE.D.exe와 같은 폴더)
            if (!File.Exists(fullExePath))
            {
                fullExePath = Path.Combine(baseDir, serverExecutableName);
                workingDir = baseDir;
            }

            if (!File.Exists(fullExePath))
            {
                Debug.LogError("[DroneController] 파이썬 서버 실행 파일을 찾을 수 없습니다: " + fullExePath);
                currentMode = ControlMode.Keyboard;
                return;
            }

            startInfo.FileName = fullExePath;
            startInfo.WorkingDirectory = workingDir;
            
            // CMD 창을 완전히 가리고 백그라운드에서 실행되도록 설정
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
#endif

            pythonProcess = Process.Start(startInfo);
            Debug.Log("[DroneController] 파이썬 서버 자동 실행 성공 (백그라운드 모드): " + startInfo.FileName);
        }
        catch (Exception e)
        {
            Debug.LogError("[DroneController] 파이썬 서버 실행 실패: " + e.Message);
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

        // 1. Yaw 회전 처리 (제자리 좌우 회전)
        if (rightCommand == "ROTATE_LEFT")
        {
            currentYaw -= rotateSpeed * Time.fixedDeltaTime;
        }
        else if (rightCommand == "ROTATE_RIGHT")
        {
            currentYaw += rotateSpeed * Time.fixedDeltaTime;
        }

        // 2. 이동 방향 벡터 계산
        Vector3 moveDir = Vector3.zero;

        if (leftCommand == "UP") moveDir += Vector3.up * moveSpeed;
        else if (leftCommand == "DOWN") moveDir += Vector3.down * moveSpeed;

        if (leftCommand == "LEFT") moveDir -= transform.right * moveSpeed;
        else if (leftCommand == "RIGHT") moveDir += transform.right * moveSpeed;

        if (rightCommand == "FORWARD") moveDir += transform.forward * moveSpeed;
        else if (rightCommand == "BACKWARD") moveDir -= transform.forward * moveSpeed;

        rb.linearVelocity = moveDir;

        // 3. 이동 방향에 따른 동적 틸팅(기울기) 계산
        float targetPitch = 0f;
        if (rightCommand == "FORWARD") targetPitch = maxTiltAngle;
        else if (rightCommand == "BACKWARD") targetPitch = -maxTiltAngle;

        float targetRoll = 0f;
        if (leftCommand == "LEFT") targetRoll = maxTiltAngle;
        else if (leftCommand == "RIGHT") targetRoll = -maxTiltAngle;

        if (leftCommand == "UP") targetPitch = maxTiltAngle * 0.5f;
        else if (leftCommand == "DOWN") targetPitch = -maxTiltAngle * 0.5f;

        Quaternion targetRotation = Quaternion.Euler(targetPitch, currentYaw, targetRoll);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, tiltSpeed * Time.fixedDeltaTime);
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