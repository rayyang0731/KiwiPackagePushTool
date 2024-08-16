using System;
using System.Diagnostics;
using System.IO;

using Unity.Plastic.Newtonsoft.Json.Linq;

using UnityEditor;

using UnityEngine;

using Debug = UnityEngine.Debug;

namespace Kiwi.Package.Editor
{
	[ CustomEditor(typeof(PackagePushSetting)) ]
	public class PackagePushSettingEditor : UnityEditor.Editor
	{
		/// <summary>
		/// Package 文件夹对象
		/// </summary>
		private SerializedProperty _packageFolderSP;

		/// <summary>
		/// Package 文件夹路径
		/// </summary>
		private string _packageFolderPath;

		/// <summary>
		/// Package 的 json 配置文件路径
		/// </summary>
		private string _packageJsonPath;

		/// <summary>
		/// Package 的 json 配置对象
		/// </summary>
		private JObject _packageJson;

		/// <summary>
		/// 当前版本号
		/// </summary>
		private string _version;

		private void OnEnable()
		{
			_packageFolderSP = serializedObject.FindProperty(nameof(PackagePushSetting.PackageFolder));

			if (_packageFolderSP.objectReferenceValue == null)
				return;

			_packageFolderPath = AssetDatabase.GetAssetPath(_packageFolderSP.objectReferenceValue);
			_packageJsonPath   = $"{_packageFolderPath}/package.json";
			_packageJson       = GetJsonObj(_packageJsonPath);

			if (_packageJson["version"] == null)
				_packageJson["version"] = _version = "1.0.0";
			else
				_version = _packageJson["version"].ToString();
		}

		private void OnDisable()
		{
			_packageFolderSP   = null;
			_packageFolderPath = null;
			_packageJsonPath   = null;
			_packageJson       = null;
			_version           = null;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUI.BeginChangeCheck();
			{
				EditorGUILayout.PropertyField(_packageFolderSP , new GUIContent("Package 文件夹"));

				using (new EditorGUI.DisabledScope())
				{
					GUI.enabled = false;
					EditorGUILayout.TextField(new GUIContent("Package 文件夹路径") , AssetDatabase.GetAssetPath(_packageFolderSP.objectReferenceValue));
				}
			}

			if (EditorGUI.EndChangeCheck())
			{
				if (_packageFolderSP.objectReferenceValue == null)
					return;

				_packageFolderPath = AssetDatabase.GetAssetPath(_packageFolderSP.objectReferenceValue);
				_packageJsonPath   = $"{_packageFolderPath}/package.json";
				_packageJson       = GetJsonObj(_packageJsonPath);
			}

			if (_packageFolderSP.objectReferenceValue == null)
				return;

			_version = _packageJson["version"].ToString();

			EditorGUI.BeginChangeCheck();
			{
				_version = EditorGUILayout.TextField(new GUIContent("Version") , _version);
			}

			if (EditorGUI.EndChangeCheck())
			{
				_packageJson["version"] = _version;
				File.WriteAllText(_packageJsonPath , _packageJson.ToString());
			}

			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("当前版本号推送"))
				{
					Push(false);
				}

				var color = GUI.color;
				GUI.color = Color.green;

				if (GUILayout.Button("自增版本号推送"))
				{
					Push(true);
				}

				GUI.color = color;
			}
			EditorGUILayout.EndHorizontal();

			serializedObject.ApplyModifiedProperties();
		}

		/// <summary>
		/// 获得 Json 对象
		/// </summary>
		/// <param name="filePath">json 文件路径</param>
		/// <returns></returns>
		private JObject GetJsonObj(string filePath)
		{
			var jsonContent = File.ReadAllText(filePath);

			return JObject.Parse(jsonContent);
		}

		/// <summary>
		/// 推送
		/// </summary>
		/// <param name="versionIncrement">是否自增版本号</param>
		private void Push(bool versionIncrement)
		{
			if (versionIncrement)
			{
				// 获取当前版本号
				var currentVersion = _packageJson["version"].ToString();
				Debug.Log("当前版本号: " + currentVersion);

				// 增加版本号
				var newVersion = IncrementVersion(currentVersion);
				Debug.Log("新版本号: " + newVersion);

				// 更新 package.json 文件中的版本号
				_packageJson["version"] = newVersion;
				File.WriteAllText(_packageJsonPath , _packageJson.ToString());
			}

			// 确定仓库根目录
			var repoRoot = GetGitRepositoryRoot();

			Debug.Log($"仓库根目录 : {repoRoot}");

			// 获取 package 相对仓库根目录的路径
			var relativelyRepoPath = GetRelativePathToRepoRoot(repoRoot , _packageFolderPath);

			Debug.Log($"package 相对仓库根目录的路径 : {relativelyRepoPath}");

			if (string.IsNullOrEmpty(relativelyRepoPath))
			{
				Debug.Log("package 相对仓库根目录的路径获取失败.");

				return;
			}

			// 1. 拆分指定目录的提交内容到 upm 分支
			if (RunGitCommand($"subtree split --prefix=\"{relativelyRepoPath}\" --branch upm" , repoRoot))
			{
				Debug.Log("已成功拆分目录到分支 upm");
			}

			// 2. 给 upm 分支打标签
			if (RunGitCommand($"tag {_packageJson["version"]} upm" , repoRoot))
			{
				Debug.Log($"已在分支 upm 上打标签 {_packageJson["version"]}");
			}

			// 3. 将带有标签的 upm 分支推送到远程服务器
			if (RunGitCommand($"push origin upm --tags" , repoRoot))
			{
				Debug.Log($"已将分支 upm 和标签推送到远程仓库 origin");
			}
		}

		/// <summary>
		/// 获得 Git 仓库根目录
		/// </summary>
		/// <returns></returns>
		private string GetGitRepositoryRoot()
		{
			var startInfo = new ProcessStartInfo
			                {
				                FileName               = "git" ,
				                Arguments              = "rev-parse --show-toplevel" ,
				                RedirectStandardOutput = true ,
				                RedirectStandardError  = true ,
				                UseShellExecute        = false ,
				                CreateNoWindow         = true
			                };

			using (var process = Process.Start(startInfo))
			{
				var output = process.StandardOutput.ReadToEnd();
				process.WaitForExit();

				if (process.ExitCode == 0)
					return output.Trim(); // 去掉末尾的换行符

				Debug.LogError("无法确定 Git 仓库根目录");

				return null;
			}
		}

		/// <summary>
		/// 运行 Git 命令
		/// </summary>
		/// <param name="arguments"></param>
		/// <param name="workingDirectory"></param>
		/// <returns></returns>
		private bool RunGitCommand(string arguments , string workingDirectory)
		{
			var startInfo = new ProcessStartInfo
			                {
				                FileName               = "git" ,
				                Arguments              = arguments ,
				                WorkingDirectory       = workingDirectory , // 设置工作目录
				                RedirectStandardOutput = true ,
				                RedirectStandardError  = true ,
				                UseShellExecute        = false ,
				                CreateNoWindow         = true
			                };

			using (var process = Process.Start(startInfo))
			{
				var output = process.StandardOutput.ReadToEnd();
				var error  = process.StandardError.ReadToEnd();
				process.WaitForExit();

				if (!string.IsNullOrEmpty(output))
					Debug.Log(output);

				if (!string.IsNullOrEmpty(error))
				{
					Debug.LogError(error);

					return false; // 如果有错误，返回 false
				}

				return process.ExitCode == 0; // 返回是否成功
			}
		}

		/// <summary>
		/// 自增版本号
		/// </summary>
		/// <param name="version">当前版本号</param>
		/// <returns></returns>
		private string IncrementVersion(string version)
		{
			var parts = version.Split('.');
			var major = int.Parse(parts[0]);
			var minor = int.Parse(parts[1]);
			var patch = int.Parse(parts[2]);

			// 增加补丁号
			patch++;

			// 返回更新后的版本号
			return $"{major}.{minor}.{patch}";
		}

		/// <summary>
		/// Package文件夹相对 Git 仓库路径
		/// </summary>
		/// <param name="repoRoot">Git仓库根路径</param>
		/// <param name="packagePath">Package 文件夹路径</param>
		/// <returns></returns>
		private string GetRelativePathToRepoRoot(string repoRoot , string packagePath)
		{
			// 获取完整路径
			var fullPackagePath = Path.GetFullPath(packagePath);

			// 计算相对路径
			var repoUri     = new Uri(repoRoot);
			var packageUri  = new Uri(fullPackagePath);
			var relativeUri = repoUri.MakeRelativeUri(packageUri);

			var result = Uri.UnescapeDataString(relativeUri.ToString().Replace('\\' , '/'));

			return result.Substring(result.IndexOf('/') + 1);
		}
	}
}
