namespace JMor.Utility
{
	[System.Serializable]
	public class SceneWrapper
	{
		public string scenePath;
		public string sceneName;
		public bool forceReload;
		#region For Editor Property Drawer Use Only
		[UnityEngine.SerializeField]
		string newScenePath = "";
		#endregion
	}
}
