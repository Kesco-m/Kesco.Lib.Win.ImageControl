using System;

namespace Kesco.Lib.Win.ImageControl
{
	// События для ImageControl
	public class SaveEventArgs : EventArgs
	{
		private bool save = false;

		public event SaveEventHandler AfterSave;

		protected internal void OnAfterSave()
		{
			if (AfterSave != null)
				AfterSave(this);
		}

		public bool Save
		{
			get { return save; }
			set
			{
				save = value;
				OnAfterSave();
			}
		}
	}

	public delegate void SaveEventHandler(SaveEventArgs NeedChange);

	public class FileNameChangedArgs : EventArgs
	{
		private string fileName;
		public string FileName
		{
			get { return fileName; }
			set { fileName = value; }
		}
	}

	public class ScanCompleteArgs : FileNameChangedArgs
	{
		public Scaner.ScanType ScanType { get; set; }

	}

	public class MarkEndEventArgs : EventArgs
	{
		private static readonly MarkEndEventArgs empty = new MarkEndEventArgs { Left = 0, Top = 0, Height = 0, Width = 0, MarkType = 0, GroupName = null };
		public int Left { get; set; }
		public int Top { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public int MarkType { get; set; }
		public string GroupName { get; set; }

		public static new MarkEndEventArgs Empty
		{
			get { return empty; }
		}
	}
}
