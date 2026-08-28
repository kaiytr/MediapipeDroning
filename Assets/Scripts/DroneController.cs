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
        currentMode = GameManager.SelectedMode;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

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
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            pythonProcess = Process.Start(startInfo);
            Debug.Log("파이썬 백그라운드 서버 자동 실행 시작");
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

    // 파이썬 프로세스 및 소켓 안전 완전 종료
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
                    pythonProcess.Kill(); // 프로세스 강제 종료 (카메라 해제)
                    pythonProcess.WaitForExit(1000); // 프로세스 정리 대기
                }
                pythonProcess.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning("파이썬 프로세스 종료 처리 중 예외: " + e.Message);
            }
            finally
            {
                pythonProcess = null;
                Debug.Log("파이썬 프로세스 및 웹캠 정상 종료 완료");
            }
        }
    }

    // 유니티 플레이 정지, 씬 변경, 오브젝트 파괴 시 자동 호출되는 안전 보장 이벤트 함수들
    private void OnDisable()
    {
        StopPythonProcess();
    }

    private void OnDestroy()
    {
        StopPythonProcess();
    }

    private void OnApplicationQuit()
    {
        StopPythonProcess();
    }
}