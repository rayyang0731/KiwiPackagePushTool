using UnityEditor;

using UnityEngine;

namespace Kiwi.Editor.Package
{
	[ CreateAssetMenu(fileName = "PackagePushSetting" , menuName = "Kiwi/PackagePushSetting" , order = 0) ]
	public class PackagePushSetting : ScriptableObject
	{
		public DefaultAsset PackageFolder;
	}
}
