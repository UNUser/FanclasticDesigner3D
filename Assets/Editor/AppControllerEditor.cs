﻿using System.IO;
using Assets.Scripts;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Assets.Editor 
{
	[CustomEditor(typeof(AppController))]
	public class AppControllerEditor : UnityEditor.Editor, IPreprocessBuildWithReport
	{
		public int callbackOrder { get { return 0; }}
		public void OnPreprocessBuild(BuildReport report)
		{
			Call();
		}

		public void Call()
		{
			var streamingAssetsPath = Application.streamingAssetsPath;
			var demoModelsPath = Path.Combine(streamingAssetsPath, "DemoModels");
			var filesListPath = Path.Combine(demoModelsPath, "FilesList.txt");
			var filesList = Directory.GetFiles(demoModelsPath, "*.fcl", SearchOption.AllDirectories)
									 .Select(s => s.Remove(0, streamingAssetsPath.Length + 1).Replace('\\', '/'))
									 .ToArray();

			File.WriteAllLines(filesListPath, filesList);

			Debug.Log("List of demo models updated!");
		}
	}
}
