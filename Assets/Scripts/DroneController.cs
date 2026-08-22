using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    private TcpClient client;
    private StreamReader reader;

    private string leftCommand = "NONE";
    private string rightCommand = "NONE";

    private Rigidbody rb;
    public float moveSpeed = 5f;
    public float rotateSpeed = 150f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        ConnectToServer();
    }

    void ConnectToServer()
    {
        try
        {
            client = new TcpClient("127.0.0.1", 5001);
            NetworkStream stream = client.GetStream();
            reader = new StreamReader(stream, Encoding.UTF8);
            Debug.Log("파이썬 서버 연결 성공");
        }
        catch (Exception e)
        {
            Debug.LogWarning("서버 연결 실패: " + e.Message);
        }
    }

    void Update()
    {
        if (client != null && client.GetStream().DataAvailable)
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
        rb.useGravity = false;

        // 양손 중 하나라도 STOP(주먹) 감지 시 즉시 모든 이동/회전 정지
        if (leftCommand == "STOP" || rightCommand == "STOP")
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        // 1. 회전 처리 (오른손)
        if (rightCommand == "ROTATE_LEFT")
        {
            transform.Rotate(0f, -rotateSpeed * Time.fixedDeltaTime, 0f, Space.World);
        }
        else if (rightCommand == "ROTATE_RIGHT")
        {
            transform.Rotate(0f, rotateSpeed * Time.fixedDeltaTime, 0f, Space.World);
        }

        // 2. 이동 벡터 계산
        Vector3 moveDir = Vector3.zero;

        // 고도 (월드 Y축 기준)
        if (leftCommand == "UP") moveDir += Vector3.up * moveSpeed;
        else if (leftCommand == "DOWN") moveDir += Vector3.down * moveSpeed;

        // 좌우 이동 (드론 회전 기준 로컬 오른쪽/왼쪽)
        if (leftCommand == "LEFT") moveDir -= transform.right * moveSpeed;
        else if (leftCommand == "RIGHT") moveDir += transform.right * moveSpeed;

        // 전후진 (드론 회전 기준 로컬 전진/후진)
        if (rightCommand == "FORWARD") moveDir += transform.forward * moveSpeed;
        else if (rightCommand == "BACKWARD") moveDir -= transform.forward * moveSpeed;

        rb.linearVelocity = moveDir;
    }

    void OnApplicationQuit()
    {
        if (reader != null) reader.Close();
        if (client != null) client.Close();
    }
}