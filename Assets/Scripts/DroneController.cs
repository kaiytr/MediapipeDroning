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
    [Tooltip("PyInstaller로 생성한 단일 파이썬 실행 파일 이름")]
    public string serverExecutableName = "drone_server.exe";

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
            // 웹캠 및 MediaPipe 초기화 시간을 고려하여 3초 후 첫 접속 시도
            Invoke(nameof(ConnectToServer), 3.0f);
        }
    }

    void StartPythonServer()
    {
        try
        {
            string rootPath = Application.isEditor 
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "..")) 
                : AppDomain.CurrentDomain.BaseDirectory;

            string fullExePath = Path.Combine(rootPath, "dist", serverExecutableName);
            string workingDir = Path.Combine(rootPath, "dist");

            if (!File.Exists(fullExePath))
            {
                fullExePath = Path.Combine(rootPath, serverExecutableName);
                workingDir = rootPath;
            }

            if (!File.Exists(fullExePath))
            {
                Debug.LogError("파이썬 서버 실행 파일을 찾을 수 없습니다: " + fullExePath);
                currentMode = ControlMode.Keyboard;
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fullExePath,
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            pythonProcess = Process.Start(startInfo);
            Debug.Log("파이썬 서버(EXE) 백그라운드 자동 실행 성공: " + fullExePath);
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
            // 타임아웃 1초 설정 후 접속 시도
            var result = client.BeginConnect("127.0.0.1", 5001, null, null);
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
            Invoke(nameof(ConnectToServer), 1.5f);
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