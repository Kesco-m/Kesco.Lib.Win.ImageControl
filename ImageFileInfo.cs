namespace Kesco.Lib.Win.ImageControl
{
	public class ImageFileInfo
	{
		private string fullFileName;
		private long fileSize;

		public ImageFileInfo(string fullFileName)
		{
			if(!System.IO.File.Exists(fullFileName))
				return;
			this.fullFileName = fullFileName;
			System.IO.FileInfo fi = new System.IO.FileInfo(fullFileName);
			fileSize = fi.Length;
			fi = null;
		}
	}
}