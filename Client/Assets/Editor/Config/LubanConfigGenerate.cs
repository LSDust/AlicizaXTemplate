// using UnityEngine;
// using System.Diagnostics;
// using System.IO;
// using AlicizaX.Editor.Extension;
//
// public static class LubanConfigGenerate
// {
//     [EditorToolFunction("Config/打表")]
//     static void GenerateLubanConfig()
//     {
//         ExecuteBatch("gen_code_bin_to_client_en.bat");
//         ExecuteBatch("gen_code_bin_to_client_zh.bat");
//         ExecuteBatch("gen_code_bin_to_client_jp.bat");
//     }
//
//     static void ExecuteBatch(string fileName)
//     {
//         // 构建正确路径：向上退两层到工程目录的父级，再进入Data/Config
//         string configPath = Path.GetFullPath(
//             Path.Combine(Application.dataPath, "..", "..", "Data", "Config")
//         );
//
//         ProcessStartInfo psi = new ProcessStartInfo()
//         {
//             FileName = fileName,
//             WorkingDirectory = configPath,
//             UseShellExecute = true,
//             CreateNoWindow = false,
//             WindowStyle = ProcessWindowStyle.Normal
//         };
//
//         try
//         {
//             using (Process process = Process.Start(psi))
//             {
//                 process.WaitForExit(); // 等待执行完成（可选）
//                 UnityEngine.Debug.Log($"已执行 {fileName}");
//             }
//         }
//         catch (System.Exception e)
//         {
//             UnityEngine.Debug.LogError($"执行失败：{e.Message}");
//         }
//     }
// }
