using UnityEngine;
using UnityEditor;
using System.IO;


/*
 * 这个脚本的作用是把Scripts目录下的所有脚本复制到另外一个git仓库 纯脚本方便当作AI语料使用
 */


namespace MyTools
{
    public class ScriptCopier : Editor
    {
        // ---------------- 配置区域 ----------------
        // 源目录：Unity项目中的脚本路径
        // 注意：使用 @ 符号并在路径前加 r 或直接用双反斜杠，这里使用 @ 方便复制粘贴 Windows 路径
        private static readonly string SourcePath = @"D:\UnityProject\LifeOn\Assets\Scripts";

        // 目标目录：你希望备份/导出到的位置
        private static readonly string DestinationPath = @"D:\LiveOnCodeOnly\Scripts";
        // ------------------------------------------

        // 在 Unity 顶部菜单栏创建 "Tools/Copy Scripts" 选项
        [MenuItem("SSBX/Copy Scripts (One Click)")]
        public static void CopyScripts()
        {
            // 1. 安全检查：源目录是否存在
            if (!Directory.Exists(SourcePath))
            {
                Debug.LogError($"源目录不存在: {SourcePath}");
                EditorUtility.DisplayDialog("错误", "源目录不存在，请检查路径配置。", "确定");
                return;
            }

            // 显示进度条（因为文件多的时候可能会卡顿）
            EditorUtility.DisplayProgressBar("正在处理", "正在准备目录...", 0f);

            try
            {
                // 2. 清理目标目录
                if (Directory.Exists(DestinationPath))
                {
                    // true 表示递归删除子目录和文件
                    Directory.Delete(DestinationPath, true);
                }

                // 重建目标空目录
                Directory.CreateDirectory(DestinationPath);

                // 3. 获取源目录下所有 .cs 文件 (包含子目录)
                string[] files = Directory.GetFiles(SourcePath, "*.cs", SearchOption.AllDirectories);

                int fileCount = files.Length;
                int copiedCount = 0;

                // 4. 遍历并复制
                foreach (string filePath in files)
                {
                    // 计算进度
                    float progress = (float)copiedCount / fileCount;
                    EditorUtility.DisplayProgressBar("正在复制脚本", $"正在复制: {Path.GetFileName(filePath)}", progress);

                    // 获取相对路径 (例如: SubDir\MyScript.cs)
                    // 这里的逻辑是把源路径的前缀去掉
                    string relativePath = Path.GetRelativePath(SourcePath, filePath);

                    // 组合新的目标全路径
                    string targetFilePath = Path.Combine(DestinationPath, relativePath);

                    // 确保目标文件的父文件夹存在 (保持目录结构)
                    string targetDir = Path.GetDirectoryName(targetFilePath);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    // 执行复制
                    File.Copy(filePath, targetFilePath);
                    copiedCount++;
                }

                // 5. 完成提示
                Debug.Log($"<color=green>成功复制了 {copiedCount} 个脚本文件到: {DestinationPath}</color>");
                // 打开文件夹查看结果
                EditorUtility.RevealInFinder(DestinationPath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"复制过程中发生错误: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"复制失败:\n{e.Message}", "确定");
            }
            finally
            {
                // 无论成功失败，都要关闭进度条
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
