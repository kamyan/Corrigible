using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>Sade önizlemesinin referans ve farklı en-boy oranı yakalamaları.</summary>
    public static class SadeCapture
    {
        public const string DirectoryPath = "Docs/UI_Sade/captures";

        [MenuItem("Corrigible/Sade UI/Capture Game View")]
        static void Menu() => Debug.Log(Capture());

        public static string Capture(int width = 1920, int height = 1080, string name = "S-Masa1")
        {
            Directory.CreateDirectory(DirectoryPath);
            return DeskCapture.Capture(name, width, height,
                Path.Combine(DirectoryPath, $"{name}_{width}x{height}_{DateTime.Now:yyyy-MM-dd_HHmmss}.png"));
        }
    }
}
