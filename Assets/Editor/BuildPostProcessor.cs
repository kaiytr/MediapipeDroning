using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class BuildPostProcessor
{
    [PostProcessBuild(1)]
    public static void OnPostProcessBuild(BuildTarget target, string targetPath)
    {
        if (target != BuildTarget.StandaloneWindows && target != BuildTarget.StandaloneWindows64)
            return;

        // 빌드된 .exe 파일이 생성된 결과물 폴더 경로
        string buildOutputDir = Path.GetDirectoryName(targetPath);
        
        // 유니티 프로젝트 최상위 루트 경로
        string projectRootDir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        // 원본 파일 경로
        string sourceExePath = Path.Combine(projectRootDir, "dist", "drone_server.exe");
        string sourceTaskPath = Path.Combine(projectRootDir, "hand_landmarker.task");

        // 대상 dist 폴더 경로
        string targetDistDir = Path.Combine(buildOutputDir, "dist");

        if (!Directory.Exists(targetDistDir))
        {
            Directory.CreateDirectory(targetDistDir);
        }

        // 1. drone_server.exe -> 빌드폴더/dist/ 로 복사
        if (File.Exists(sourceExePath))
        {
            string destExePath = Path.Combine(targetDistDir, "drone_server.exe");
            File.Copy(sourceExePath, destExePath, true);
            Debug.Log("[BuildPostProcessor] drone_server.exe 복사 완료: " + destExePath);
        }
        else
        {
            Debug.LogError("[BuildPostProcessor] 원본 drone_server.exe를 찾을 수 없습니다: " + sourceExePath);
        }

        // 2. hand_landmarker.task -> 빌드폴더/dist/ 및 빌드폴더 루트 양쪽에 복사
        if (File.Exists(sourceTaskPath))
        {
            string destTaskPathInDist = Path.Combine(targetDistDir, "hand_landmarker.task");
            string destTaskPathInRoot = Path.Combine(buildOutputDir, "hand_landmarker.task");

            File.Copy(sourceTaskPath, destTaskPathInDist, true);
            File.Copy(sourceTaskPath, destTaskPathInRoot, true);
            Debug.Log("[BuildPostProcessor] hand_landmarker.task 복사 완료");
        }
        else
        {
            Debug.LogWarning("[BuildPostProcessor] 원본 hand_landmarker.task를 프로젝트 루트에서 찾을 수 없습니다: " + sourceTaskPath);
        }
    }
}