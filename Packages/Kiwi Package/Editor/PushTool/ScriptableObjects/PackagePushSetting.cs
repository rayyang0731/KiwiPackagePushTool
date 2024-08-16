using UnityEditor;

using UnityEngine;

namespace Kiwi.Package.Editor
{
	[ CreateAssetMenu(fileName = "PackagePushSetting" , menuName = "Kiwi/PackagePushSetting" , order = 0) ]
	public class PackagePushSetting : ScriptableObject
	{
		public DefaultAsset PackageFolder;
	}
}
