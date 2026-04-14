using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[System.Serializable]
public class RouteDefinition
{
    public string RouteId;                  // String ID tujuan (misal: "Shop")
    public RouteFlags Flags;                // Flag, aturan (misal: RequiresAuth, IsAdditive)
    public NavType Type;                    // Tipe navigasi
    public string TargetScene;              // Nama scene (jika butuh pindah scene)
    public AssetReference Prefab;           // Referensi Addressable (jika tipe Window/Popup)

    // Helper method agar kode di Service tetap enak dibaca
    public bool HasFlag(RouteFlags flag) => (Flags & flag) != 0;
}

[CreateAssetMenu(fileName = "NavigationConfig", menuName = "Service/Configs/Navigation")]
public class NavigationConfig : ScriptableObject
{
    [Header("Default Routes")]
    public string LoginRouteId = "Login";
    public string DefaultRouteId = "MainMenu";

    [Header("All Routes")]
    public List<RouteDefinition> Routes;

    // Helper untuk mengubah List jadi Dictionary saat Runtime biar cepat (O(1))
    public Dictionary<string, RouteDefinition> ToDictionary()
    {
        var dict = new Dictionary<string, RouteDefinition>();
        foreach (var r in Routes) dict[r.RouteId] = r;
        return dict;
    }
}