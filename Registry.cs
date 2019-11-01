using System;
using System.Drawing;
using Kesco.Lib.Win.Options;

namespace Kesco.Lib.Win.ImageControl
{

	internal class Registry
	{
		private static Folder Settings;
		private static Folder HOLLOW_RECT_TOOL;
		private static Folder _HOLLOW_RECT_TOOL_LINE_COLOR;
		private static Folder _HOLLOW_RECT_TOOL_LINE_WIDTH;
		private static Folder _HOLLOW_RECT_TOOL_STYLE;
		private static Folder FILLED_RECT_TOOL;
		private static Folder _FILLED_RECT_TOOL_FILL_COLOR;
		private static Folder _FILLED_RECT_TOOL_STYLE;
		private static Folder ATTACH_A_NOTE_TOOL;
		private static Folder _ATTACH_A_NOTE_TOOL_BACKCOLOR;
//		private static Folder _ATTACH_A_NOTE_TOOL_FONT_CHARACTERISTICS;
//		private static Folder _ATTACH_A_NOTE_TOOL_FONT_CHARSET;
		private static Folder _ATTACH_A_NOTE_TOOL_FONT_COLOR;
		private static Folder _ATTACH_A_NOTE_TOOL_FONT_NAME;
		private static Folder _ATTACH_A_NOTE_TOOL_FONT_SIZE;

		private static Folder TEXT_TOOL;
//		private static Folder _TEXT_TOOL_FONT_CHARACTERISTICS;
//		private static Folder _TEXT_TOOL_FONT_CHARSET;
		private static Folder _TEXT_TOOL_FONT_COLOR;
		private static Folder _TEXT_TOOL_FONT_NAME;
		private static Folder _TEXT_TOOL_FONT_SIZE;

		internal static string TEXT_TOOL_FONT_NAME
		{
			get { return _TEXT_TOOL_FONT_NAME.LoadStringOption("FONT_NAME", "Arial"); }
			set 
			{
				IOption opt = _TEXT_TOOL_FONT_NAME.OptionForced<string>("FONT_NAME");
				opt.Value = value;
				opt.Save();
			}
		}

		internal static int TEXT_TOOL_FONT_SIZE
		{
			get { return _TEXT_TOOL_FONT_SIZE.LoadIntOption("FONT_SIZE", 12); }
			set 
			{
				IOption opt = _TEXT_TOOL_FONT_SIZE.OptionForced<int>("FONT_SIZE");
				opt.Value = value;
				opt.Save();
			}
		}

		internal static Color TEXT_TOOL_FONT_COLOR
		{
			get 
			{
				string color = _TEXT_TOOL_FONT_COLOR.LoadStringOption("FONT_COLOR", "0,0,0");
				string[] rgb = color.Split(new char[]{','}, 3);
				return Color.FromArgb(Convert.ToInt32(rgb[0]),Convert.ToInt32(rgb[1]), Convert.ToInt32(rgb[2]) ); 
			}
			set
			{
				IOption opt = _TEXT_TOOL_FONT_COLOR.OptionForced<string>("FONT_COLOR");
				opt.Value = value.R.ToString() + "," + value.G.ToString() + "," + value.B.ToString();
				opt.Save();
			}
		}

		internal static string ATTACH_A_NOTE_TOOL_FONT_NAME
		{
			get { return _ATTACH_A_NOTE_TOOL_FONT_NAME.LoadStringOption("FONT_NAME", "Arial"); }
			set 
			{
				IOption opt = _ATTACH_A_NOTE_TOOL_FONT_NAME.OptionForced<string>("FONT_NAME");
				opt.Value = value;
				opt.Save();
			}
		}

		internal static int ATTACH_A_NOTE_TOOL_FONT_SIZE
		{
			get { return _ATTACH_A_NOTE_TOOL_FONT_SIZE.LoadIntOption("FONT_SIZE", 12); }
			set 
			{
				IOption opt = _ATTACH_A_NOTE_TOOL_FONT_SIZE.OptionForced<int>("FONT_SIZE");
				opt.Value = value;
				opt.Save();
			}
		}

		internal static Color ATTACH_A_NOTE_TOOL_FONT_COLOR
		{
			get 
			{
				string color = _ATTACH_A_NOTE_TOOL_FONT_COLOR.LoadStringOption("FONT_COLOR", "0,0,0");
				string[] rgb = color.Split(new char[]{','}, 3);
				return Color.FromArgb(Convert.ToInt32(rgb[0]),Convert.ToInt32(rgb[1]), Convert.ToInt32(rgb[2]) ); 
			}
			set
			{
				IOption opt = _ATTACH_A_NOTE_TOOL_FONT_COLOR.OptionForced<string>("FONT_COLOR");
				opt.Value = value.R.ToString() + "," + value.G.ToString() + "," + value.B.ToString();
				opt.Save();
			}
		}

		internal static Color ATTACH_A_NOTE_TOOL_BACKCOLOR
		{
			get 
			{
				string color = _ATTACH_A_NOTE_TOOL_BACKCOLOR.LoadStringOption("BACKCOLOR", "255,255,0");
				string[] rgb = color.Split(new char[]{','}, 3);
				return Color.FromArgb(Convert.ToInt32(rgb[0]),Convert.ToInt32(rgb[1]), Convert.ToInt32(rgb[2]) ); 
			}
			set
			{
				IOption opt = _ATTACH_A_NOTE_TOOL_BACKCOLOR.OptionForced<string>("BACKCOLOR");
				opt.Value = value.R.ToString() + "," + value.G.ToString() + "," + value.B.ToString();
				opt.Save();
			}
		}

		internal static Color FILLED_RECT_TOOL_FILL_COLOR
		{
			get 
			{
				string color = _FILLED_RECT_TOOL_FILL_COLOR.LoadStringOption("FILL_COLOR", "255,255,0");
				string[] rgb = color.Split(new char[]{','}, 3);
				return Color.FromArgb(Convert.ToInt32(rgb[0]),Convert.ToInt32(rgb[1]), Convert.ToInt32(rgb[2]) ); 
			}
			set
			{
				IOption opt = _FILLED_RECT_TOOL_FILL_COLOR.OptionForced<string>("FILL_COLOR");
				opt.Value = value.R.ToString() + "," + value.G.ToString() + "," + value.B.ToString();
				opt.Save();
			}
		}

		internal static bool FILLED_RECT_TOOL_STYLE
		{
			get 
			{ 
				return Convert.ToBoolean(_FILLED_RECT_TOOL_STYLE.LoadIntOption("STYLE", 1)); 
			}
			set 
			{
				IOption opt = _FILLED_RECT_TOOL_STYLE.OptionForced<int>("STYLE");
				opt.Value = value ? 1 : 0;
				opt.Save();
			}
		}

		internal static Color HOLLOW_RECT_TOOL_LINE_COLOR
		{
			get 
			{
				string color = _HOLLOW_RECT_TOOL_LINE_COLOR.LoadStringOption("LINE_COLOR", "0,0,255");
				string[] rgb = color.Split(new char[]{','}, 3);
				return Color.FromArgb(Convert.ToInt32(rgb[0]),Convert.ToInt32(rgb[1]), Convert.ToInt32(rgb[2]) ); 
			}
			set
			{
				IOption opt = _HOLLOW_RECT_TOOL_LINE_COLOR.OptionForced<string>("LINE_COLOR");
			    opt.Value = value.R.ToString() + "," + value.G.ToString() + "," + value.B.ToString();
				opt.Save();
			}
		}

		internal static uint HOLLOW_RECT_TOOL_LINE_WIDTH
		{
			get { return (UInt32)_HOLLOW_RECT_TOOL_LINE_WIDTH.LoadIntOption("LINE_WIDTH", 4); }
			set 
			{
				IOption opt = _HOLLOW_RECT_TOOL_LINE_WIDTH.OptionForced<int>("LINE_WIDTH");
				opt.Value = (int)value;
				opt.Save();
			}
		}

		internal static bool HOLLOW_RECT_TOOL_STYLE
		{
			get 
			{ 
				return Convert.ToBoolean(_HOLLOW_RECT_TOOL_STYLE.LoadIntOption("STYLE", 1)); 
			}
			set 
			{
				IOption opt = _HOLLOW_RECT_TOOL_STYLE.OptionForced<int>("STYLE");
				opt.Value = value ? 1 : 0;
				opt.Save();
			}
		}

		static Registry()
		{
			Settings = new Folder("Software\\Kodak\\WOI", "ANNOTATION_TOOL_PALETTE");
			HOLLOW_RECT_TOOL = Settings.Folders.Add("HOLLOW_RECT_TOOL");
			_HOLLOW_RECT_TOOL_LINE_COLOR = HOLLOW_RECT_TOOL.Folders.Add("LINE_COLOR");
			_HOLLOW_RECT_TOOL_LINE_WIDTH = HOLLOW_RECT_TOOL.Folders.Add("LINE_WIDTH");
			_HOLLOW_RECT_TOOL_STYLE = HOLLOW_RECT_TOOL.Folders.Add("STYLE");

			FILLED_RECT_TOOL = Settings.Folders.Add("FILLED_RECT_TOOL");
			_FILLED_RECT_TOOL_FILL_COLOR = FILLED_RECT_TOOL.Folders.Add("FILL_COLOR");
			_FILLED_RECT_TOOL_STYLE = FILLED_RECT_TOOL.Folders.Add("STYLE");

			ATTACH_A_NOTE_TOOL = Settings.Folders.Add("ATTACH_A_NOTE_TOOL");
			_ATTACH_A_NOTE_TOOL_BACKCOLOR = ATTACH_A_NOTE_TOOL.Folders.Add("BACKCOLOR");
//			_ATTACH_A_NOTE_TOOL_FONT_CHARACTERISTICS;
//			_ATTACH_A_NOTE_TOOL_FONT_CHARSET;
			_ATTACH_A_NOTE_TOOL_FONT_COLOR = ATTACH_A_NOTE_TOOL.Folders.Add("FONT_COLOR");
			_ATTACH_A_NOTE_TOOL_FONT_NAME = ATTACH_A_NOTE_TOOL.Folders.Add("FONT_NAME");
			_ATTACH_A_NOTE_TOOL_FONT_SIZE = ATTACH_A_NOTE_TOOL.Folders.Add("FONT_SIZE");

			TEXT_TOOL = Settings.Folders.Add("TEXT_TOOL");
//			_TEXT_TOOL_FONT_CHARACTERISTICS;
//			_TEXT_TOOL_FONT_CHARSET;
			_TEXT_TOOL_FONT_COLOR = TEXT_TOOL.Folders.Add("FONT_COLOR");
			_TEXT_TOOL_FONT_NAME = TEXT_TOOL.Folders.Add("FONT_NAME");
			_TEXT_TOOL_FONT_SIZE = TEXT_TOOL.Folders.Add("FONT_SIZE");
		}
	}
}
