using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;


namespace Aerodynamics.Editor.Templates
{
    internal class ScriptTemplates
    {
        private static readonly string _path = "Assets/Scripts/Aerodynamics/Editor/Templates";

		[MenuItem("Assets/Create/Custom/Function Script", false, 0)]
		public static void CreateFunctionScript() =>
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
				ScriptableObject.CreateInstance<DoCreateFunctionAsset>(),
				"NewFunctionSO.cs",
				(Texture2D) EditorGUIUtility.IconContent("cs Script Icon").image,
				$"{_path}/CustomFunctionTemplate.txt");

		private class DoCreateFunctionAsset : EndNameEditAction
		{
			public override void Action(int instanceId, string pathName, string resourceFile)
			{
				string text = File.ReadAllText(resourceFile);

				string fileName = Path.GetFileName(pathName);
				{
					string newName = fileName.Replace(" ", "");
					if (!newName.Contains("SO"))
					{
						newName = newName.Insert(fileName.Length - 3, "SO");
					}

					pathName = pathName.Replace(fileName, newName);
					fileName = newName;
				}

				string fileNameWithoutExtension = fileName.Substring(0, fileName.Length - 3);
				text = text.Replace("#SCRIPTNAME#", fileNameWithoutExtension);

				string runtimeName = fileNameWithoutExtension.Replace("SO", "");
				text = text.Replace("#RUNTIMENAME#", runtimeName);

				for (int i = runtimeName.Length - 1; i > 0; i--)
				{
					if (char.IsUpper(runtimeName[i]) && char.IsLower(runtimeName[i - 1]))
					{
						runtimeName = runtimeName.Insert(i, " ");
					}
				}

				text = text.Replace("#RUNTIMENAME_WITH_SPACES#", runtimeName);

				string fullPath = Path.GetFullPath(pathName);
				var encoding = new UTF8Encoding(true);
				File.WriteAllText(fullPath, text, encoding);
				AssetDatabase.ImportAsset(pathName);
				ProjectWindowUtil.ShowCreatedAsset(AssetDatabase.LoadAssetAtPath(pathName, typeof(UnityEngine.Object)));
			}
		}
    }
}