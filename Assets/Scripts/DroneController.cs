using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public class DroneController : MonoBehaviour
{
    [Header("Control Settings")]
    public ControlMode currentMode;

    [Header("Python Server Settings")]
    public string pythonPath = @"venv\Scripts\python.exe";
    public string scriptPath = "drone_server.py";

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
        // GameManager에서 설정된 모드를 가져옴
        currentMode = GameManager.SelectedMode;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        // 손 제스처 모드일 경우에만 파이썬 서버 자동 실행
        if (currentMode == ControlMode.HandGesture)
        {
            StartPythonServer();
            Invoke(nameof(ConnectToServer), 1.5f);
        }
    }

    void StartPythonServer()
    {
        try
        {
            string projectRoot = Path.Combine(Application.dataPath, "..");
            string fullPythonPath = Path.Combine(projectRoot, pythonPath);
            string fullScriptPath = Path.Combine(projectRoot, scriptPath);

            if (!File.Exists(fullScriptPath))
            {
                fullScriptPath = Path.Combine(Application.streamingAssetsPath, scriptPath);
            }

            if (!File.Exists(fullPythonPath))
            {
                Debug.LogWarning("파이썬 실행 환경을 찾지 못해 키보드 모드로 자동 전환됩니다.");
                currentMode = ControlMode.Keyboard;
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fullPythonPath,
                Arguments = $"\"{fullScriptPath}\"",
                WorkingDirectory = projectRoot,
                UseShellExecute = true,
                CreateNoWindow = false
            };

            pythonProcess = Process.Start(startInfo);
            Debug.Log("파이썬 서버 자동 실행 시작");
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

        try
        {
            client = new TcpClient("127.0.0.1", 5001);
            NetworkStream stream = client.GetStream();
            reader = new StreamReader(stream, Encoding.UTF8);
            Debug.Log("파이썬 서버 연결 성공!");
        }
        catch (Exception e)
        {
            Debug.LogWarning("서버 연결 재시도 중...: " + e.Message);
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

        // ESC 키 입력 시 Start 씬(메인 메뉴)으로 돌아가기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToStartScene();
        }
    }

    private void ProcessKeyboardInput()
    {
        // 왼손 매핑 (WASD)
        if (Input.GetKey(KeyCode.W)) leftCommand = "UP";
        else if (Input.GetKey(KeyCode.S)) leftCommand = "DOWN";
        else if (Input.GetKey(KeyCode.A)) leftCommand = "LEFT";
        else if (Input.GetKey(KeyCode.D)) leftCommand = "RIGHT";
        else leftCommand = "NONE";

        // 오른손 매핑 (방향키)
        if (Input.GetKey(KeyCode.UpArrow)) rightCommand = "FORWARD";
        else if (Input.GetKey(KeyCode.DownArrow)) rightCommand = "BACKWARD";
        else if (Input.GetKey(KeyCode.LeftArrow)) rightCommand = "ROTATE_LEFT";
        else if (Input.GetKey(KeyCode.RightArrow)) rightCommand = "ROTATE_RIGHT";
        else rightCommand = "NONE";

        // 양손 긴급 정지 (Space 키)
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

        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            try { pythonProcess.Kill(); pythonProcess.Dispose(); } catch { }
            pythonProcess = null;
        }
    }

    void OnApplicationQuit()
    {
        StopPythonProcess();
    }
}