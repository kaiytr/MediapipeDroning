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

        // 원본 파일/폴더 경로 (프로젝트 루트 기준)
        string sourceServerDir = Path.Combine(projectRootDir, "dist", "drone_server");
        string sourceTaskPath = Path.Combine(projectRootDir, "hand_landmarker.task");

        // 대상 빌드 결과물의 dist 폴더 경로
        string targetDistDir = Path.Combine(buildOutputDir, "dist");
        string targetServerDir = Path.Combine(targetDistDir, "drone_server");

        // 1. 빌드 결과물 디렉토리 내 dist 폴더 생성
        if (!Directory.Exists(targetDistDir))
        {
            Directory.CreateDirectory(targetDistDir);
        }

        // 2. drone_server 폴더 전체 -> 빌드폴더/dist/drone_server/ 복사
        if (Directory.Exists(sourceServerDir))
        {
            CopyDirectory(sourceServerDir, targetServerDir);
            Debug.Log("[BuildPostProcessor] drone_server 폴더 복사 완료: " + targetServerDir);
        }
        else
        {
            Debug.LogError("[BuildPostProcessor] 원본 drone_server 폴더를 찾을 수 없습니다: " + sourceServerDir);
        }

        // 3. hand_landmarker.task -> 빌드폴더/dist/ 내부 및 drone_server 내부로 복사
        if (File.Exists(sourceTaskPath))
        {
            // dist/hand_landmarker.task
            string destTaskInDist = Path.Combine(targetDistDir, "hand_landmarker.task");
            File.Copy(sourceTaskPath, destTaskInDist, true);

            // dist/drone_server/hand_landmarker.task (서버 내부 접근용)
            if (Directory.Exists(targetServerDir))
            {
                string destTaskInServer = Path.Combine(targetServerDir, "hand_landmarker.task");
                File.Copy(sourceTaskPath, destTaskInServer, true);
            }

            Debug.Log("[BuildPostProcessor] hand_landmarker.task 파일 복사 완료: " + destTaskInDist);
        }
        else
        {
            Debug.LogWarning("[BuildPostProcessor] 원본 hand_landmarker.task를 찾을 수 없습니다: " + sourceTaskPath);
        }
    }

    // 폴더 내 모든 파일 및 하위 폴더 재귀 복사
    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (string subDir in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, destSubDir);
        }
    }
}