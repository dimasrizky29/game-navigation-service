using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using System.Linq;

[InitializeOnLoad]
public class DependencyAutoInstaller
{
    static DependencyAutoInstaller()
    {
        // Jalankan pengecekan secara delay sedikit agar Unity siap
        EditorApplication.delayCall += CheckAndInstallDependencies;
    }

    private static void CheckAndInstallDependencies()
    {
        bool unitaskInstalled = IsPackageInstalled("com.cysharp.unitask");
        bool vcontainerInstalled = IsPackageInstalled("jp.hadashikick.vcontainer");
        bool LoggerInstalled = IsPackageInstalled("com.yatechstudio.loggerservice");

        if (!unitaskInstalled)
        {
            Debug.Log("[NavigationService] Installing UniTask...");
            Client.Add("https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask");
        }

        if (!vcontainerInstalled)
        {
            Debug.Log("[NavigationService] Installing VContainer...");
            Client.Add("https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer#1.17.0");
        }

        if (!LoggerInstalled)
        {
            Debug.Log("[NavigationService] Installing Logger Service...");
            Client.Add("https://github.com/yatech-studio/game-logger-service.git");
        }
    }

    private static bool IsPackageInstalled(string packageId)
    {
        // Mencari di daftar package yang sudah terdaftar di project
        return UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
               .Any(p => p.name == packageId);
    }
}