using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Kesco.Lib.Win.ImageControl
{
	public enum TwainCommand
	{
		Not = -1,
		Null = 0,
		Failure = 1,
		TransferReady = 2,
		CloseRequest = 3,
		CloseOk = 4,
		DeviceEvent = 5
	}

	public class Twain
	{
		public static IntPtr pDll = IntPtr.Zero;
		[DllImport("kernel32.dll")]
		public static extern IntPtr LoadLibrary(string dllToLoad);

		[DllImport("kernel32.dll")]
		public static extern bool FreeLibrary(IntPtr hModule);


		private const short CountryUSA = 1;
		private const short LanguageUSA = 13;

		public Twain()
		{
			if (IntPtr.Size == 4 && pDll == IntPtr.Zero && System.IO.File.Exists(System.IO.Path.Combine(Environment.GetEnvironmentVariable("SYSTEMROOT"), Twain32.TwainLibPath)))
			    pDll = LoadLibrary(System.IO.Path.Combine(Environment.GetEnvironmentVariable("SYSTEMROOT"), Twain32.TwainLibPath));
			appid = new TwIdentity();
			appid.Id = IntPtr.Zero;
			appid.Version.MajorNum = 2;
			appid.Version.MinorNum = 1;
			appid.Version.Language = LanguageUSA;
			appid.Version.Country = CountryUSA;
			appid.Version.Info = "ImageControl 1";
			appid.ProtocolMajor = TwProtocol.Major;
			appid.ProtocolMinor = TwProtocol.Minor;
			appid.SupportedGroups = (int)(TwDG.Image | TwDG.Control);
			appid.Manufacturer = "Kesco";
			appid.ProductFamily = "Freeware";
			appid.ProductName = "ImageControl";

			srcds = new TwIdentity();
			srcds.Id = IntPtr.Zero;

			evtmsg.EventPtr = Marshal.AllocHGlobal(Marshal.SizeOf(winmsg));
		}

		~Twain()
		{
			Marshal.FreeHGlobal(evtmsg.EventPtr);
			if (pDll != IntPtr.Zero)
			{
			    FreeLibrary(pDll);
			    pDll = IntPtr.Zero;
			}
		}

		public void Init(IntPtr hwndp)
		{
			Finish();
			TwRC rc = DSMparent(appid, IntPtr.Zero, TwDG.Control, TwDAT.Parent, TwMSG.OpenDSM, ref hwndp);
			if (rc == TwRC.Success)
			{
				rc = DSMident(appid, IntPtr.Zero, TwDG.Control, TwDAT.Identity, TwMSG.GetDefault, srcds);

				if (rc == TwRC.Success)
				{
					hwnd = hwndp;
				}
				else
					rc = DSMparent(appid, IntPtr.Zero, TwDG.Control, TwDAT.Parent, TwMSG.CloseDSM, ref hwndp);
			}
		}

		public void Select()
		{
			TwRC rc;
			CloseSrc();
			if (appid.Id == IntPtr.Zero)
			{
				Init(hwnd);
				if (appid.Id == IntPtr.Zero)
					return;
			}
			rc = DSMident(appid, IntPtr.Zero, TwDG.Control, TwDAT.Identity, TwMSG.UserSelect, srcds);
		}

		public void Acquire()
		{
			TwRC rc;
			CloseSrc();
			if (appid.Id == IntPtr.Zero)
			{
				Init(hwnd);
				if (appid.Id == IntPtr.Zero)
					return;
			}

			rc = DSMident(appid, IntPtr.Zero, TwDG.Control, TwDAT.Identity, TwMSG.OpenDS, srcds);
			if (rc != TwRC.Success)
				return;

			TwCapability cap = new TwCapability(TwCap.XferCount, -1);
			rc = DScap(appid, srcds, TwDG.Control, TwDAT.Capability, TwMSG.Set, cap);
			if (rc != TwRC.Success)
			{
				CloseSrc();
				return;
			}

			TwUserInterface guif = new TwUserInterface();
			guif.ShowUI = 1;
			guif.ModalUI = 1;
			guif.ParentHand = hwnd;
			rc = DSuserif(appid, srcds, TwDG.Control, TwDAT.UserInterface, TwMSG.EnableDS, guif);
			if (rc != TwRC.Success)
			{
				CloseSrc();
				return;
			}
		}

		public ArrayList TransferPictures()
		{
			ArrayList pics = new ArrayList();
			if (srcds.Id == IntPtr.Zero)
				return pics;

			TwRC rc;
			IntPtr hbitmap = IntPtr.Zero;
			TwPendingXfers pxfr = new TwPendingXfers();

			do
			{
				pxfr.Count = 0;
				hbitmap = IntPtr.Zero;

				TwImageInfo iinf = new TwImageInfo();
				rc = DSiinf(appid, srcds, TwDG.Image, TwDAT.ImageInfo, TwMSG.Get, iinf);
				if (rc != TwRC.Success)
				{
					CloseSrc();
					return pics;
				}

				rc = DSixfer(appid, srcds, TwDG.Image, TwDAT.ImageNativeXfer, TwMSG.Get, ref hbitmap);
				if (rc != TwRC.XferDone)
				{
					CloseSrc();
					return pics;
				}

				rc = DSpxfer(appid, srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.EndXfer, pxfr);
				if (rc != TwRC.Success)
				{
					CloseSrc();
					return pics;
				}

				pics.Add(hbitmap);
			}
			while (pxfr.Count != 0);

			rc = DSpxfer(appid, srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.Reset, pxfr);
			return pics;
		}

		public TwainCommand PassMessage(ref Message m)
		{
			if (srcds.Id == IntPtr.Zero)
				return TwainCommand.Not;

			int pos = GetMessagePos();

			winmsg.hwnd = m.HWnd;
			winmsg.message = m.Msg;
			winmsg.wParam = m.WParam;
			winmsg.lParam = m.LParam;
			winmsg.time = GetMessageTime();
			winmsg.x = (short)pos;
			winmsg.y = (short)(pos >> 16);

			Marshal.StructureToPtr(winmsg, evtmsg.EventPtr, false);
			evtmsg.Message = 0;
			TwRC rc = DSevent(appid, srcds, TwDG.Control, TwDAT.Event, TwMSG.ProcessEvent, ref evtmsg);
			if (rc == TwRC.Failure)
				return TwainCommand.Failure;
			if (rc == TwRC.NotDSEvent)
				return TwainCommand.Not;
			if (evtmsg.Message == (short)TwMSG.XFerReady)
				return TwainCommand.TransferReady;
			if (evtmsg.Message == (short)TwMSG.CloseDSReq)
				return TwainCommand.CloseRequest;
			if (evtmsg.Message == (short)TwMSG.CloseDSOK)
				return TwainCommand.CloseOk;
			if (evtmsg.Message == (short)TwMSG.DeviceEvent)
				return TwainCommand.DeviceEvent;

			return TwainCommand.Null;
		}

		public void CloseSrc()
		{
			TwRC rc;
			if (srcds.Id != IntPtr.Zero)
			{
				TwUserInterface guif = new TwUserInterface();
				rc = DSuserif(appid, srcds, TwDG.Control, TwDAT.UserInterface, TwMSG.DisableDS, guif);
				rc = DSMident(appid, IntPtr.Zero, TwDG.Control, TwDAT.Identity, TwMSG.CloseDS, srcds);
			}
		}

		public void Finish()
		{
			TwRC rc;
			CloseSrc();
			if (appid.Id != IntPtr.Zero)
				rc = DSMparent(appid, IntPtr.Zero, TwDG.Control, TwDAT.Parent, TwMSG.CloseDSM, ref hwnd);
			appid.Id = IntPtr.Zero;
		}

		private IntPtr hwnd;
		private TwIdentity appid;
		private TwIdentity srcds;
		private TwEvent evtmsg;
		private WINMSG winmsg;

		#region TwainX

		private TwRC DScap(TwIdentity origin, TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, TwCapability capa)
		{
			if (IntPtr.Size == 8)
				return Twain64.DScap(origin, dest, dg, dat, msg, capa);
			else
				return Twain32.DScap(origin, dest, dg, dat, msg, capa);
		}

		private TwRC DSpxfer(TwIdentity origin, TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, TwPendingXfers pxfr)
		{
			if (IntPtr.Size == 8)
				return Twain64.DSpxfer(origin, dest, dg, dat, msg, pxfr);
			else
				return Twain32.DSpxfer(origin, dest, dg, dat, msg, pxfr);
		}

		private TwRC DSixfer(TwIdentity origin, TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr hbitmap)
		{
			if (IntPtr.Size == 8)
				return Twain64.DSixfer(origin, dest, dg, dat, msg, ref  hbitmap);
			else
				return Twain32.DSixfer(origin, dest, dg, dat, msg, ref  hbitmap);
		}

		private TwRC DSiinf(TwIdentity origin, TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, TwImageInfo imginf)
		{
			if (IntPtr.Size == 8)
				return Twain64.DSiinf(origin, dest, dg, dat, msg, imginf);
			else
				return Twain32.DSiinf(origin, dest, dg, dat, msg, imginf);
		}

		private TwRC DSevent(TwIdentity origin, TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref TwEvent evt)
		{
			if (IntPtr.Size == 8)
				return Twain64.DSevent(origin, dest, dg, dat, msg, ref  evt);
			else
				return Twain32.DSevent(origin, dest, dg, dat, msg, ref  evt);
		}

		private TwRC DSMident(TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, TwIdentity idds)
		{
			if (IntPtr.Size == 8)
				return Twain64.DSMident(origin, zeroptr, dg, dat, msg, idds);
			else
				return Twain32.DSMident(origin, zeroptr, dg, dat, msg, idds);
		}

		private TwRC DSuserif(TwIdentity origin, TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, TwUserInterface guif)
		{
			if (IntPtr.Size == 8)
				return Twain64.DSuserif(origin, dest, dg, dat, msg, guif);
			else
				return Twain32.DSuserif(origin, dest, dg, dat, msg, guif);
		}

		private TwRC DSMparent(TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr refptr)
		{
			if (IntPtr.Size == 8)
				return Twain64.DSMparent(origin, zeroptr, dg, dat, msg, ref  refptr);
			else
				return Twain32.DSMparent(origin, zeroptr, dg, dat, msg, ref  refptr);
		}

		#endregion
		
		[DllImport("kernel32.dll", ExactSpelling = true)]
		internal static extern IntPtr GlobalAlloc(int flags, int size);
		[DllImport("kernel32.dll", ExactSpelling = true)]
		internal static extern IntPtr GlobalLock(IntPtr handle);
		[DllImport("kernel32.dll", ExactSpelling = true)]
		internal static extern bool GlobalUnlock(IntPtr handle);
		[DllImport("kernel32.dll", ExactSpelling = true)]
		internal static extern IntPtr GlobalFree(IntPtr handle);

		[DllImport("user32.dll", ExactSpelling = true)]
		private static extern int GetMessagePos();
		[DllImport("user32.dll", ExactSpelling = true)]
		private static extern int GetMessageTime();

		[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr CreateDC(string szdriver, string szdevice, string szoutput, IntPtr devmode);

		[DllImport("gdi32.dll", ExactSpelling = true)]
		private static extern bool DeleteDC(IntPtr hdc);

		public static int ScreenBitDepth
		{
			get
			{
				IntPtr screenDC = CreateDC("DISPLAY", null, null, IntPtr.Zero);
				int bitDepth = PrintImage.GetDeviceCaps(screenDC, 12);
				bitDepth *= PrintImage.GetDeviceCaps(screenDC, 14);
				DeleteDC(screenDC);
				return bitDepth;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		internal struct WINMSG
		{
			public IntPtr hwnd;
			public int message;
			public IntPtr wParam;
			public IntPtr lParam;
			public int time;
			public int x;
			public int y;
		}
	}

	public class Twain32
	{
		internal const string TwainLibPath = "twain_32.dll";
		// ------ DSM entry point DAT_ variants:
		[DllImport(Twain32.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSMparent([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr refptr);

		[DllImport(Twain32.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSMident([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwIdentity idds);

		[DllImport(Twain32.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSMident([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, IntPtr idds);


		[DllImport(Twain32.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSMstatus([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwStatus dsmstat);


		// ------ DSM entry point DAT_ variants to DS:
		[DllImport(Twain32.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSuserif([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, TwUserInterface guif);

		[DllImport(Twain32.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSevent([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref TwEvent evt);

		[DllImport(Twain32.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSstatus([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwStatus dsmstat);

		[DllImport(Twain32.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DScap([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwCapability capa);

		[DllImport(Twain32.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSiinf([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwImageInfo imginf);

		[DllImport(Twain32.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSixfer([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr hbitmap);

		[DllImport(Twain32.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSpxfer([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwPendingXfers pxfr);
	}

	public class Twain64
	{
		public const string TwainLibPath = "twaindsm.dll";
		// ------ DSM entry point DAT_ variants:
		[DllImport(Twain64.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSMparent([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr refptr);

		[DllImport(Twain64.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSMident([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwIdentity idds);

		[DllImport(Twain64.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSMident([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, IntPtr idds);


		[DllImport(Twain64.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSMstatus([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwStatus dsmstat);


		// ------ DSM entry point DAT_ variants to DS:
		[DllImport(Twain64.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSuserif([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, TwUserInterface guif);

		[DllImport(Twain64.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSevent([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref TwEvent evt);

		[DllImport(Twain64.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSstatus([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwStatus dsmstat);

		[DllImport(Twain64.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DScap([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwCapability capa);

		[DllImport(Twain64.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSiinf([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwImageInfo imginf);

		[DllImport(Twain64.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSixfer([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr hbitmap);

		[DllImport(Twain64.TwainLibPath, EntryPoint = "#1")]
		internal static extern TwRC DSpxfer([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwPendingXfers pxfr);
	}
}
