import socket
import cv2
import mediapipe as np_mp
from mediapipe.tasks import python
from mediapipe.tasks.python import vision
import numpy as np

server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server_socket.bind(('127.0.0.1', 5001))
server_socket.listen(1)
print("유니티 연결을 기다리는 중...")
conn, addr = server_socket.accept()
print(f"유니티 연결됨: {addr}")

base_options = python.BaseOptions(model_asset_path='hand_landmarker.task')
options = vision.HandLandmarkerOptions(
    base_options=base_options,
    running_mode=vision.RunningMode.IMAGE,
    num_hands=2
)
detector = vision.HandLandmarker.create_from_options(options)

cap = cv2.VideoCapture(0)

FINGER_TIPS = [8, 12, 16, 20]
FINGER_PIPS = [6, 10, 14, 18]

def check_fist(landmarks):
    wrist = landmarks[0]
    folded = 0
    for tip_idx, pip_idx in zip(FINGER_TIPS, FINGER_PIPS):
        dist_tip = np.hypot(landmarks[tip_idx].x - wrist.x, landmarks[tip_idx].y - wrist.y)
        dist_pip = np.hypot(landmarks[pip_idx].x - wrist.x, landmarks[pip_idx].y - wrist.y)
        if dist_tip < dist_pip:
            folded += 1
    return folded >= 3

def process_left_hand(landmarks):
    if check_fist(landmarks):
        return "NONE"
    
    base = landmarks[5]
    index_tip = landmarks[8]
    dx = index_tip.x - base.x
    dy = index_tip.y - base.y
    
    if abs(dy) > abs(dx):
        if dy < -0.02: return "UP"
        elif dy > 0.02: return "DOWN"
    else:
        if dx < -0.02: return "LEFT"
        elif dx > 0.02: return "RIGHT"
    return "NONE"

def process_right_hand(landmarks):
    if check_fist(landmarks):
        return "NONE"
    
    base = landmarks[5]
    index_tip = landmarks[8]
    dx = index_tip.x - base.x
    dy = index_tip.y - base.y
    
    if abs(dy) > abs(dx):
        if dy < -0.02: return "FORWARD"
        elif dy > 0.02: return "BACKWARD"
    else:
        if dx < -0.02: return "ROTATE_LEFT"
        elif dx > 0.02: return "ROTATE_RIGHT"
    return "NONE"

try:
    while cap.isOpened():
        success, frame = cap.read()
        if not success: break
            
        frame = cv2.flip(frame, 1)
        h, w, _ = frame.shape
        
        mp_image = np_mp.Image(image_format=np_mp.ImageFormat.SRGB, data=cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))
        detection_result = detector.detect(mp_image)

        left_cmd = "NONE"
        right_cmd = "NONE"

        left_is_fist = False
        right_is_fist = False

        if detection_result.hand_landmarks:
            for landmarks in detection_result.hand_landmarks:
                wrist_x = landmarks[0].x
                for lm in landmarks:
                    cv2.circle(frame, (int(lm.x * w), int(lm.y * h)), 4, (0, 255, 0), -1)

                is_fist = check_fist(landmarks)

                if wrist_x < 0.5:
                    left_is_fist = is_fist
                    left_cmd = process_left_hand(landmarks)
                else:
                    right_is_fist = is_fist
                    right_cmd = process_right_hand(landmarks)

        # 양손이 모두 화면에 감지되었고, 양쪽 다 주먹을 쥐고 있는 경우 STOP 처리
        if left_is_fist and right_is_fist:
            left_cmd = "STOP"
            right_cmd = "STOP"

        combined_command = f"{left_cmd},{right_cmd}"

        try:
            conn.sendall((combined_command + "\n").encode('utf-8'))
        except:
            print("유니티 연결 끊김")
            break

        cv2.putText(frame, f"Left: {left_cmd} | Right: {right_cmd}", (30, 50), 
                    cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 255, 0), 2)
        cv2.imshow("Dual Hand Drone Control", frame)
        
        if cv2.waitKey(1) & 0xFF == ord('q'): break
finally:
    cap.release()
    cv2.destroyAllWindows()
    conn.close()
    server_socket.close()