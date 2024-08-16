using System;
using System.IO;
using System.Text.RegularExpressions;

using Unity.Plastic.Newtonsoft.Json.Linq;

using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace Kiwi.Package.Editor
{
	/// <summary>
	/// Package 创建工具窗口
	/// </summary>
	/// <remarks>
	///	[package-root]
	///  ├── package.json
	///  ├── README.md
	///  ├── CHANGELOG.md
	///  ├── LICENSE.md
	///  ├── Third Party Notices.md
	///  ├── Editor
	///  │   ├── [company-name].[package-name].Editor.asmdef
	///  │   └── EditorExample.cs
	///  ├── Runtime
	///  │   ├── [company-name].[package-name].asmdef
	///  │   └── RuntimeExample.cs
	///  ├── Tests
	///  │   ├── Editor
	///  │   │   ├── [company-name].[package-name].Editor.Tests.asmdef
	///  │   │   └── EditorExampleTest.cs
	///  │   └── Runtime
	///  │        ├── [company-name].[package-name].Tests.asmdef
	///  │        └── RuntimeExampleTest.cs
	///  ├── Samples~
	///  │        ├── SampleFolder1
	///  │        ├── SampleFolder2
	///  │        └── ...
	///  └── Documentation~
	///       └── [package-name].md
	/// </remarks>
	public class PackageCreateToolWindow : EditorWindow
	{
#if USE_KIWI_UTILITY
		[ EditorToolbar(EditorToolbarAttribute.Anchor.Right , "Kiwi" , "Package/Package 创建工具") ]
#else
		[ MenuItem("Kiwi/Packages/Package 创建工具") ]
#endif
		private static void Open()
		{
			var window = CreateWindow<PackageCreateToolWindow>("Package 创建工具");
			window.ShowUtility();
		}

		/// <summary>
		/// 公司名称
		/// </summary>
		private string _companyName;

		/// <summary>
		/// 公司名称
		/// </summary>
		private string companyName
		{
			get => _companyName;
			set => textField_companyName.value = value;
		}

		private TextField textField_companyName;

		private Label label_companyNameInvalidTips;

		/// <summary>
		/// package 名称
		/// </summary>
		private string _packageName;

		/// <summary>
		///  package 名称
		/// </summary>
		private string packageName
		{
			get => _packageName;
			set => textField_packageName.value = value;
		}

		private TextField textField_packageName;

		private Label label_packageNameInvalidTips;

		private const string kCompanyNameConfigKey = "Package_Company_Name";
		private const string kPackageNameConfigKey = "Package_Name";

		public void CreateGUI()
		{
			#region 公司名称

			textField_companyName = new("公司名称");
			label_companyNameInvalidTips = new("公司名称除'_',不可使用其他符号.")
			                               {
				                               style =
				                               {
					                               display   = DisplayStyle.None ,
					                               color     = Color.red ,
					                               alignSelf = Align.FlexEnd ,
				                               } ,
			                               };
			textField_companyName.RegisterCallback<ChangeEvent<string>>(evt =>
			{
				_companyName = evt.newValue;

				if (HasSpecialCharacters(_companyName))
				{
					label_companyNameInvalidTips.style.display = DisplayStyle.Flex;
					_companyName                               = null;
				}
				else
				{
					label_companyNameInvalidTips.style.display = DisplayStyle.None;
				}

				if (string.IsNullOrEmpty(packageName) || string.IsNullOrEmpty(companyName))
					rootVisualElement.Q<Button>().SetEnabled(false);
				else
					rootVisualElement.Q<Button>().SetEnabled(true);
			});

			companyName = EditorUserSettings.GetConfigValue(kCompanyNameConfigKey) ?? "company";

			rootVisualElement.Add(textField_companyName);
			rootVisualElement.Add(label_companyNameInvalidTips);

			#endregion

			#region Package 名称

			textField_packageName = new("Package 名称");
			label_packageNameInvalidTips = new("Package 名称除'_',不可使用其他符号.")
			                               {
				                               style =
				                               {
					                               display   = DisplayStyle.None ,
					                               color     = Color.red ,
					                               alignSelf = Align.FlexEnd ,
				                               } ,
			                               };
			textField_packageName.RegisterCallback<ChangeEvent<string>>(evt =>
			{
				_packageName = evt.newValue;

				if (HasSpecialCharacters(_packageName))
				{
					label_packageNameInvalidTips.style.display = DisplayStyle.Flex;
					_packageName                               = null;
				}
				else
				{
					label_packageNameInvalidTips.style.display = DisplayStyle.None;
				}

				if (string.IsNullOrEmpty(packageName) || string.IsNullOrEmpty(companyName))
					rootVisualElement.Q<Button>().SetEnabled(false);
				else
					rootVisualElement.Q<Button>().SetEnabled(true);
			});

			packageName = EditorUserSettings.GetConfigValue(kPackageNameConfigKey) ?? "package";

			rootVisualElement.Add(textField_packageName);
			rootVisualElement.Add(label_packageNameInvalidTips);

			#endregion

			#region 创建按钮

			var createButton = new Button()
			                   {
				                   text = "创建" ,
			                   };
			createButton.clicked += Create;

			rootVisualElement.Add(createButton);

			#endregion
		}

		private void OnDestroy()
		{
			textField_companyName        = null;
			textField_packageName        = null;
			label_companyNameInvalidTips = null;
			label_packageNameInvalidTips = null;
		}

		/// <summary>
		/// 检查字符串是否包含除'_'之外的特殊字符
		/// </summary>
		/// <param name="input">需要检查的字符串</param>
		/// <returns>如果包含特殊字符则返回true，否则返回false</returns>
		private static bool HasSpecialCharacters(string input)
		{
			// 正则表达式模式，匹配除字母、数字、下划线之外的任意字符
			const string pattern = "[^a-zA-Z0-9_]";

			return Regex.IsMatch(input , pattern);
		}

		/// <summary>
		/// 创建 Package 结构
		/// </summary>
		private void Create()
		{
			EditorUserSettings.SetConfigValue(kCompanyNameConfigKey , _companyName);
			EditorUserSettings.SetConfigValue(kPackageNameConfigKey , _packageName);

			if (!Create_Package_Root_Folder(out var packagePath))
			{
				Debug.LogError($"创建 Package 根目录失败 : {packagePath}");

				return;
			}

			if (!Create_Package_Json(packagePath))
			{
				Debug.LogError("创建 package.json 失败");

				return;
			}

			if (!Create_Readme_Markdown(packagePath))
			{
				Debug.LogError("创建 README.md 失败");

				return;
			}

			if (!Create_Changelog_Markdown(packagePath))
			{
				Debug.LogError("创建 CHANGELOG.md 失败");

				return;
			}

			if (!Create_Editor_Folder(packagePath))
			{
				Debug.LogError("创建 Editor 文件夹或 asmdef 程序集文件失败");

				return;
			}

			if (!Create_Runtime_Folder(packagePath))
			{
				Debug.LogError("创建 Runtime 文件夹或 asmdef 程序集文件失败");

				return;
			}

			AssetDatabase.Refresh();
		}

		/// <summary>
		/// 创建 Package 根目录
		/// </summary>
		private bool Create_Package_Root_Folder(out string packagePath)
		{
			packagePath = Path.Combine(Packages.packagesPath , $"{companyName} {packageName}");

			if (!Directory.Exists(packagePath))
				Directory.CreateDirectory(packagePath);

			return Directory.Exists(packagePath);
		}

		/// <summary>
		/// 创建 package.json
		/// </summary>
		private bool Create_Package_Json(string packagePath)
		{
			var jObj = new JObject
			           {
				           ["name"]        = $"com.{companyName.ToLower()}.{packageName.ToLower()}" ,
				           ["version"]     = "1.0.0" ,
				           ["displayName"] = $"{companyName} {packageName}" ,
			           };
			var jsonPath = Path.Combine(packagePath , "package.json");
			File.WriteAllText(jsonPath , jObj.ToString());

			return File.Exists(jsonPath);
		}

		/// <summary>
		/// 创建 Readme 文档
		/// </summary>
		private bool Create_Readme_Markdown(string packagePath)
		{
			var readmePath = Path.Combine(packagePath , "README.md");

			if (File.Exists(readmePath))
				return true;

			var sw = File.CreateText(readmePath);
			sw.Write($"{packageName}\n"
			       + "---");
			sw.Flush();
			sw.Close();
			sw.Dispose();

			return File.Exists(readmePath);
		}

		/// <summary>
		/// 创建 Changelog 文档
		/// </summary>
		private bool Create_Changelog_Markdown(string packagePath)
		{
			var changelogPath = Path.Combine(packagePath , "CHANGELOG.md");

			if (File.Exists(changelogPath))
				return true;

			var sw = File.CreateText(changelogPath);
			sw.Write("# 更新日志\n\n"
			       + "此项目的所有显著更改都将记录在此文件中.\n\n"
			       + "格式基于 [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),\n\n"
			       + "## [x.x.x] - YYYY-MM-DD\n\n"
			       + "### 新增\n\n"
			       + "- xxxx.\n"
			       + "- xxxx.\n\n"
			       + "### 更改\n\n"
			       + "- xxxx.\n"
			       + "- xxxx.\n\n"
			       + "### 移除\n\n"
			       + "- xxxx.\n"
			       + "- xxxx.\n\n");
			sw.Flush();
			sw.Close();
			sw.Dispose();

			return File.Exists(changelogPath);
		}

		/// <summary>
		/// 创建 Editor 文件夹及程序集文件
		/// </summary>
		private bool Create_Editor_Folder(string packagePath)
		{
			var editorFolderPath = Path.Combine(packagePath , "Editor");
			if (!Directory.Exists(editorFolderPath))
				Directory.CreateDirectory(editorFolderPath);

			if (!Directory.Exists(editorFolderPath))
				return false;

			var asmdefPath = Path.Combine(editorFolderPath , $"{companyName}.{packageName}.Editor.asmdef");

			if (File.Exists(asmdefPath))
				return true;

			var jObj = new JObject
			           {
				           ["name"]          = $"{companyName}.{packageName}.Editor" ,
				           ["rootNamespace"] = $"{companyName}.{packageName}.Editor" ,
				           ["references"]    = new JArray(Array.Empty<object>()) ,
				           ["includePlatforms"] = new JArray(new object[ ]
				                                             {
					                                             "Editor"
				                                             }) ,
				           ["excludePlatforms"]      = new JArray(Array.Empty<object>()) ,
				           ["allowUnsafeCode"]       = false ,
				           ["overrideReferences"]    = false ,
				           ["precompiledReferences"] = new JArray(Array.Empty<object>()) ,
				           ["autoReferenced"]        = true ,
				           ["defineConstraints"]     = new JArray(Array.Empty<object>()) ,
				           ["versionDefines"]        = new JArray(Array.Empty<object>()) ,
				           ["noEngineReferences"]    = false
			           };
			File.WriteAllText(asmdefPath , jObj.ToString());

			return File.Exists(asmdefPath) && Directory.Exists(editorFolderPath);
		}

		/// <summary>
		/// 创建 Runtime 文件夹
		/// </summary>
		private bool Create_Runtime_Folder(string packagePath)
		{
			var runtimeFolderPath = Path.Combine(packagePath , "Runtime");
			if (!Directory.Exists(runtimeFolderPath))
				Directory.CreateDirectory(runtimeFolderPath);

			if (!Directory.Exists(runtimeFolderPath))
				return false;

			var asmdefPath = Path.Combine(runtimeFolderPath , $"{companyName}.{packageName}.asmdef");

			if (File.Exists(asmdefPath))
				return true;

			var jObj = new JObject
			           {
				           ["name"]                  = $"{companyName}.{packageName}" ,
				           ["rootNamespace"]         = $"{companyName}.{packageName}" ,
				           ["references"]            = new JArray(Array.Empty<object>()) ,
				           ["includePlatforms"]      = new JArray(Array.Empty<object>()) ,
				           ["excludePlatforms"]      = new JArray(Array.Empty<object>()) ,
				           ["allowUnsafeCode"]       = false ,
				           ["overrideReferences"]    = false ,
				           ["precompiledReferences"] = new JArray(Array.Empty<object>()) ,
				           ["autoReferenced"]        = true ,
				           ["defineConstraints"]     = new JArray(Array.Empty<object>()) ,
				           ["versionDefines"]        = new JArray(Array.Empty<object>()) ,
				           ["noEngineReferences"]    = false
			           };
			File.WriteAllText(asmdefPath , jObj.ToString());

			return File.Exists(asmdefPath) && Directory.Exists(runtimeFolderPath);
		}
	}
}
