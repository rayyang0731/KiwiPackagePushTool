using UnityEngine;

namespace Kiwi.Package.Editor
{
	/// <summary>
	/// Package 工具辅助代码
	/// </summary>
	public static class Packages
	{
		/// <summary>
		/// Packages 文件夹路径
		/// </summary>
		public static string packagesPath
		{
			get
			{
				var assetsPath = Application.dataPath;

				return System.IO.Path.Combine(System.IO.Directory.GetParent(assetsPath).FullName , "Packages");
			}
		}
	}
}
