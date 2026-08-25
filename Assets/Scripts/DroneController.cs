using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class DroneController : MonoBehaviour
{
    [Header("Python Server Settings")]
    [Tooltip("프로젝트 루트의 venv 기준 상대 경로")]
    public string pythonPath = @"venv\Scripts\python.exe";
    [Tooltip("drone_server.py 파일 이름")]
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
        StartPythonServer();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        Invoke(nameof(ConnectToServer), 1.5f);
    }

    void StartPythonServer()
    {
        try
        {
            string projectRoot = Path.Combine(Application.dataPath, "..");
            string fullPythonPath = Path.Combine(projectRoot, pythonPath);
            string fullScriptPath = Path.Combine(projectRoot, scriptPath);

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
        }
    }

    void ConnectToServer()
    {
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

    void OnApplicationQuit()
    {
        if (reader != null) reader.Close();
        if (client != null) client.Close();

        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill();
            pythonProcess.Dispose();
            Debug.Log("파이썬 서버 프로세스 종료 완료");
        }
    }
}