namespace Kesco.Lib.Win.ImageControl
{
	/// <summary>
	/// Summary description for ResourcesManager.
	/// </summary>
	public class ResourcesManager
	{
		private static System.Resources.ResourceManager stringResources;

		public static System.Resources.ResourceManager StringResources
		{
			get
			{
				if(stringResources == null)
                    stringResources = new System.Resources.ResourceManager("Kesco.Lib.Win.ImageControl.StringResources", System.Reflection.Assembly.GetExecutingAssembly());
				return stringResources;
			}
		}
	}
}
