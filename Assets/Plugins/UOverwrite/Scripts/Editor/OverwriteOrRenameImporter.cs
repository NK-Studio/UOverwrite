using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections.Generic; // List 사용
using System.Linq; // Linq 사용 (First() 등)
using System; // Exception 사용

public class OverwriteOrRenameImporter : AssetPostprocessor
{
    // 사용자 선택 열거형 (동일)
    private enum FileConflictChoice
    {
        Overwrite,
        KeepBoth,
        Cancel,
        Undecided
    }

    // 충돌 정보 구조체 (동일)
    private struct ConflictedAssetInfo
    {
        public string ImportedAssetPath;
        public string OriginalPath;
        public string ImportedAssetFullPath;
        public string OriginalAssetFullPath;
        public string OriginalFileName; // 원본 파일 이름 추가 (다이얼로그용)
        public FileConflictChoice UserChoice;
    }

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        // 텍스처와 오디오 클립만 허용 (다른 타입은 무시)
        if (importedAssets.Length == 0 || !importedAssets.Any(asset => asset.EndsWith(".png") || asset.EndsWith(".jpg") || asset.EndsWith(".jpeg") || asset.EndsWith(".tga") || asset.EndsWith(".wav") || asset.EndsWith(".mp3")  || asset.EndsWith(".mp4") || asset.EndsWith(".egg")))
        {
            return;
        }
      
        
        List<ConflictedAssetInfo> conflictedAssets = new List<ConflictedAssetInfo>();

        // --- 1단계: 모든 충돌 감지 (다이얼로그 없이) ---
        foreach (string importedAssetPath in importedAssets)
        {
            Match match = Regex.Match(Path.GetFileName(importedAssetPath), @"^(.*)\s(\d+)\.([^.]+)$");

            if (match.Success)
            {
                string baseName = match.Groups[1].Value;
                string extension = match.Groups[3].Value;
                string originalFileName = $"{baseName}.{extension}";
                string directoryPath = Path.GetDirectoryName(importedAssetPath);

                if (!string.IsNullOrEmpty(directoryPath))
                {
                    string originalPath = Path.Combine(directoryPath, originalFileName).Replace("\\", "/");
                    UnityEngine.Object originalAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(originalPath);

                    if (importedAssetPath != originalPath && originalAsset != null)
                    {
                        // 충돌 감지 -> 정보 저장만 함
                        string importedFullPath = Path.GetFullPath(importedAssetPath);
                        string originalFullPath = Path.GetFullPath(originalPath);

                        if (!File.Exists(importedFullPath))
                        {
                            Debug.LogWarning(
                                $"[OverwriteImporter] 충돌 감지됨 '{importedAssetPath}', 하지만 소스 파일 '{importedFullPath}' 없음. 건너뜁니다.");
                            continue;
                        }

                        conflictedAssets.Add(new ConflictedAssetInfo
                        {
                            ImportedAssetPath = importedAssetPath,
                            OriginalPath = originalPath,
                            ImportedAssetFullPath = importedFullPath,
                            OriginalAssetFullPath = originalFullPath,
                            OriginalFileName = originalFileName, // 이름 저장
                            UserChoice = FileConflictChoice.Undecided // 초기 상태
                        });
                    }
                }
            }
        }

        // 충돌이 없으면 종료
        if (conflictedAssets.Count == 0)
        {
            return;
        }

        // --- 2단계: 충돌 수에 따른 사용자 결정 요청 ---
        FileConflictChoice bulkChoice = FileConflictChoice.Undecided; // 일괄 결정 저장용

        if (conflictedAssets.Count == 1)
        {
            // --- 단일 충돌 처리 ---
            ConflictedAssetInfo singleConflict = conflictedAssets[0]; // First() 대신 인덱스 사용

            string title = Application.systemLanguage == SystemLanguage.Korean
                ? "파일 이름 충돌"
                : "File Name Conflict";
            string message = Application.systemLanguage == SystemLanguage.Korean
                ? $"'{singleConflict.OriginalFileName}' 파일이 이미 존재합니다.\n새로 가져온 파일 ('{Path.GetFileName(singleConflict.ImportedAssetPath)}') 처리 방법을 선택하세요."
                : $"The file '{singleConflict.OriginalFileName}' already exists.\nPlease choose how to handle the newly imported file ('{Path.GetFileName(singleConflict.ImportedAssetPath)}').";
            string optionOverwrite = Application.systemLanguage == SystemLanguage.Korean ? "덮어쓰기" : "Overwrite";
            string optionKeepBoth = Application.systemLanguage == SystemLanguage.Korean ? "둘 다 유지 (이름 변경됨)" : "Keep Both (Renamed)";
            string optionCancel = Application.systemLanguage == SystemLanguage.Korean ? "취소" : "Cancel";

            int choice = EditorUtility.DisplayDialogComplex(title, message, optionOverwrite, optionKeepBoth, optionCancel);

            switch (choice)
            {
                case 0: conflictedAssets[0] = SetChoice(singleConflict, FileConflictChoice.Overwrite); break;
                case 1: conflictedAssets[0] = SetChoice(singleConflict, FileConflictChoice.KeepBoth); break;
                case 2: conflictedAssets[0] = SetChoice(singleConflict, FileConflictChoice.Cancel); break;
            }
        }
        else // conflictedAssets.Count > 1
        {
            // --- 다중 충돌 처리 (일괄) ---

            string title = Application.systemLanguage == SystemLanguage.Korean
                ? "다중 파일 이름 충돌"
                : "Multiple File Name Conflicts";
            string message = Application.systemLanguage == SystemLanguage.Korean
                ? $"{conflictedAssets.Count}개의 파일 이름 충돌이 감지되었습니다.\n모든 충돌 항목을 어떻게 처리하시겠습니까?"
                : $"{conflictedAssets.Count} file name conflicts detected.\nHow do you want to handle all of them?";
            string optionOverwriteAll = Application.systemLanguage == SystemLanguage.Korean ? "모두 덮어쓰기" : "Overwrite All";
            string optionKeepAll = Application.systemLanguage == SystemLanguage.Korean ? "모두 유지 (이름 변경됨)" : "Keep All (Renamed)";
            string optionCancelAll = Application.systemLanguage == SystemLanguage.Korean ? "모두 취소" : "Cancel All";

            int choice = EditorUtility.DisplayDialogComplex(title, message, optionOverwriteAll, optionKeepAll, optionCancelAll);

            switch (choice)
            {
                case 0: bulkChoice = FileConflictChoice.Overwrite; break;
                case 1: bulkChoice = FileConflictChoice.KeepBoth; break;
                case 2: bulkChoice = FileConflictChoice.Cancel; break;
            }

            // 일괄 선택 결과를 모든 충돌 항목에 적용
            for (int i = 0; i < conflictedAssets.Count; i++)
            {
                conflictedAssets[i] = SetChoice(conflictedAssets[i], bulkChoice);
            }
        }

        // --- 3단계: 결정된 내용 처리 (기존 코드 재활용) ---
        List<string> pathsToDelete = new List<string>();
        List<string> pathsToReimport = new List<string>();
        bool requiresDelayedRefresh = false;

        foreach (var conflictInfo in conflictedAssets)
        {
            // UserChoice가 Undecided이면 (예: 사용자가 다이얼로그를 그냥 닫음), Cancel로 처리
            FileConflictChoice finalChoice = conflictInfo.UserChoice == FileConflictChoice.Undecided
                                             ? FileConflictChoice.Cancel
                                             : conflictInfo.UserChoice;

            switch (finalChoice)
            {
                case FileConflictChoice.Overwrite:
                    // Debug.Log($"[OverwriteImporter] 처리: 덮어쓰기 - {conflictInfo.OriginalPath}");
                    try
                    {
                        if (File.Exists(conflictInfo.ImportedAssetFullPath))
                        {
                            File.Copy(conflictInfo.ImportedAssetFullPath, conflictInfo.OriginalAssetFullPath, true);
                            pathsToReimport.Add(conflictInfo.OriginalPath);
                            pathsToDelete.Add(conflictInfo.ImportedAssetPath);
                            requiresDelayedRefresh = true;
                        }
                        else
                        {
                            Debug.LogWarning($"[OverwriteImporter] 처리 중 소스 파일 '{conflictInfo.ImportedAssetFullPath}' 없음. 덮어쓰기 건너뜀 '{conflictInfo.OriginalPath}'.");
                        }
                    }
                    catch (IOException ex) { Debug.LogError($"[OverwriteImporter] 파일 복사/덮어쓰기 오류 '{conflictInfo.OriginalPath}': {ex.Message}"); }
                    catch (Exception ex) { Debug.LogError($"[OverwriteImporter] 덮어쓰기 처리 중 예외 '{conflictInfo.OriginalPath}': {ex.Message}"); }
                    break;

                case FileConflictChoice.KeepBoth:
                    // Debug.Log($"[OverwriteImporter] 처리: 둘 다 유지 - {conflictInfo.ImportedAssetPath}");
                    // 별도 작업 불필요
                    break;

                case FileConflictChoice.Cancel:
                    // Debug.Log($"[OverwriteImporter] 처리: 가져오기 취소 - {conflictInfo.ImportedAssetPath}");
                    pathsToDelete.Add(conflictInfo.ImportedAssetPath);
                    requiresDelayedRefresh = true;
                    break;
            }
        }

        // --- 4단계: 다시 가져오기 트리거 (즉시) ---
        if (pathsToReimport.Count > 0)
        {
            // Debug.Log($"[OverwriteImporter] {pathsToReimport.Count}개 에셋 다시 가져오기 트리거.");
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string pathToReimport in pathsToReimport)
                {
                    AssetDatabase.ImportAsset(pathToReimport, ImportAssetOptions.ForceUpdate);
                }
            }
            catch (Exception ex) { Debug.LogError($"[OverwriteImporter] 일괄 다시 가져오기 오류: {ex.Message}"); }
            finally { AssetDatabase.StopAssetEditing(); }
        }

        // --- 5단계: 삭제 예약 및 최종 새로 고침 (지연) ---
        if (pathsToDelete.Count > 0)
        {
            EditorApplication.delayCall += () =>
            {
                // Debug.Log($"[OverwriteImporter] {pathsToDelete.Count}개 에셋 삭제 예약됨.");
                bool deletedAny = false;
                List<string> successfullyDeleted = new List<string>(); // 추적용

                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (string pathToDelete in pathsToDelete)
                    {
                        if (AssetDatabase.DeleteAsset(pathToDelete))
                        {
                            // Debug.Log($"[OverwriteImporter] 삭제 성공: '{pathToDelete}'");
                            successfullyDeleted.Add(pathToDelete);
                            deletedAny = true;
                        }
                        else
                        {
                            Debug.LogWarning($"[OverwriteImporter] 에셋 삭제 실패 (이미 없을 수 있음): '{pathToDelete}'");
                        }
                    }
                }
                catch (Exception ex) { Debug.LogError($"[OverwriteImporter] 일괄 삭제 중 오류: {ex.Message}"); }
                finally { AssetDatabase.StopAssetEditing(); }

                if (requiresDelayedRefresh || deletedAny)
                {
                    // Debug.Log("[OverwriteImporter] 에셋 데이터베이스 새로고침 (지연됨).");
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                }
            };
        }
        else if (requiresDelayedRefresh) // 덮어쓰기만 하고 삭제할 것이 없는 경우
        {
            // Debug.Log("[OverwriteImporter] 에셋 데이터베이스 새로고침 예약됨 (덮어쓰기만).");
            EditorApplication.delayCall += () => { AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); };
        }
    }

    // 구조체의 값을 변경하기 위한 헬퍼 메서드 (구조체는 값 타입이므로)
    private static ConflictedAssetInfo SetChoice(ConflictedAssetInfo info, FileConflictChoice choice)
    {
        info.UserChoice = choice;
        return info;
    }
}