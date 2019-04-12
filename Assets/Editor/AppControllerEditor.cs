using Assets.Scripts;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Assets.Editor 
{
	[CustomEditor(typeof(AppController))]
	public class AppControllerEditor : UnityEditor.Editor, IPreprocessBuildWithReport
	{
		public int callbackOrder { get { return 0; }}
		public void OnPreprocessBuild(BuildReport report)
		{
			AppController.UpdateDemoModelsList();
		}
	}
}
