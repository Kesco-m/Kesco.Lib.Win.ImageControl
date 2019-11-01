using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Kesco.Lib.Win.ImageControl.TwainLib;

namespace Kesco.Lib.Win.ImageControl
{
	/// <summary>
	/// Summary description for Scaner.
	/// </summary>
	public class Scaner : Control, IMessageFilter
	{
		public delegate void ImagesReceivedHandler(object sender, ScanEventArgs args);
		public delegate void DialogCloseHandler(object sender, ScanEventArgs args);
		public delegate void CallbackHandler(object arg);

		public event DialogCloseHandler DialogClose;
		public event ImagesReceivedHandler ImagesReceived;
		private bool msgfilter;
		private Twain tw;
		private ScanType currentScanType = ScanType.None;
		private CallbackHandler callback = null;
		public enum ScanType
		{
			ScanAfter,
			ScanBefore,
			ScanNewDocument,
			None
		}

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public Scaner()
		{
			InitializeComponent();

			tw = new Twain();
			tw.Init(this.Handle);
		}

		public const int WM_CREATE = 0x1;

		protected override void WndProc(ref Message m)
		{
			if(m.Msg == WM_CREATE)
			{
				if(tw == null)
				{
					tw = new Twain();
					tw.Init(this.Handle);
				}
				else
				{
					tw.Init(this.Handle);

				}
			}

			base.WndProc(ref m);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Application.RemoveMessageFilter(this);
			if(tw != null)
			{
				tw.CloseSrc();
				tw = null;
			}
			if(disposing)
			{
				if(components != null)
					components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion

		public void OnImagesReceived(List<Bitmap> bitmaps, ScanType scanType, CallbackHandler callback)
		{
			try
			{
				if(ImagesReceived != null)
					ImagesReceived(this, new ScanEventArgs(bitmaps, scanType, callback));
			}
			catch(Exception ex)
			{
				Tiff.LibTiffHelper.WriteToLog(ex);
			}
		}

		public void OnDialogClose(ScanType scanType, CallbackHandler callback)
		{
			if(DialogClose != null)
				DialogClose(this, new ScanEventArgs(scanType, callback));
		}

		bool IMessageFilter.PreFilterMessage(ref Message m)
		{
			if(m.HWnd != this.Handle)
				return false;
			TwainCommand cmd = tw.PassMessage(ref m);
			if(cmd == TwainCommand.Not)
				return false;

			switch(cmd)
			{
				case TwainCommand.Failure:
				case TwainCommand.CloseRequest:
				case TwainCommand.CloseOk:
					{
						EndingScan();
						tw.CloseSrc();
						OnDialogClose(currentScanType, callback);
						break;
					}
				case TwainCommand.DeviceEvent:
					{
						break;
					}
				case TwainCommand.TransferReady:
					{
						ArrayList pics = tw.TransferPictures();
						EndingScan();
						tw.CloseSrc();
						if(pics != null)
						{
							List<Bitmap> bitmaps = new List<Bitmap>();
							for(int n = 0; n < pics.Count; n++)
							{
								IntPtr img = (IntPtr)pics[n];
								try
								{
									IntPtr bmpptr = Twain.GlobalLock(img);
									Bitmap b = null;
									try
									{
										b = DibToImage.WithScan0(bmpptr);
									}
									catch { }
									if(b != null)
										bitmaps.Add(b);
								}
								catch(Exception ex)
								{
									Tiff.LibTiffHelper.WriteToLog(ex);
								}
								finally
								{
									Twain.GlobalFree(img);
								}
								pics[n] = null;
							}
							if(bitmaps.Count > 0)
								OnImagesReceived(bitmaps, currentScanType, callback);
						}
						break;
					}
			}
			return true;
		}

		public void StartScan(ScanType currentScanType, CallbackHandler callback)
		{
			this.currentScanType = currentScanType;
			this.callback = callback;
			if(!msgfilter)
			{
				this.Enabled = false;
				msgfilter = true;
				Application.AddMessageFilter(this);
			}
			tw.Init(this.Handle);
			tw.Acquire();
		}

		public void ScanEnd()
		{
			if(tw != null)
			{
				EndingScan();
				tw.CloseSrc();
				tw = null;
			}
		}

		private void EndingScan()
		{
			if(!msgfilter)
				return;
			Application.RemoveMessageFilter(this);
			msgfilter = false;
			this.Enabled = true;
			//this.Activate();
		}

		public void SelectScaner()
		{
			tw.Select();
		}

		public class ScanEventArgs : EventArgs
		{
			private List<Bitmap> bitmaps = new List<Bitmap>();
			private ScanType scanType;
			private CallbackHandler callback;

			public List<Bitmap> Bitmaps
			{
				get { return bitmaps; }
			}

			public ScanType CurrentScanType
			{
				get { return scanType; }
			}
			public CallbackHandler Callback
			{
				get { return callback; }
			}

			public ScanEventArgs(ScanType scanType, CallbackHandler callback)
			{
				this.scanType = scanType;
				this.callback = callback;
			}

			public ScanEventArgs(List<Bitmap> bitmaps, ScanType scanType, CallbackHandler callback)
			{
				this.bitmaps = bitmaps;
				this.scanType = scanType;
				this.callback = callback;
			}
		}
	}
}