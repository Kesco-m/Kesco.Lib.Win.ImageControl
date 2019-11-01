using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Kesco.Lib.Win.ImageControl
{
	public enum PrintOrientation
	{
		Book,
		Album,
		Auto
	}

	public delegate void PagePrintedHandler(object sender, PageEventArgs e);

	public class PrintImage
	{
		[DllImport("gdi32.dll", ExactSpelling = true)]
		public static extern Int32 GetDeviceCaps(IntPtr hdc, Int32 capindex);

		protected const Int32 HORZRES = 8;
		protected const Int32 VERTRES = 10;
		protected const Int32 PHYSICALOFFSETX = 112;
		protected const Int32 PHYSICALOFFSETY = 113;
		private const Int32 PHYSICALWIDTH = 110;
		private const Int32 PHYSICALHEIGHT = 111;

		public event PagePrintedHandler PagePrinted;

		public event EventHandler EndPrint;

		protected int startPage;
		protected int endPage;
		protected int printingPage;
		protected PrintOrientation orientation;
		protected object[] images;
		protected object[] additionalInfo;
		protected int copyCount;
		protected int scaleMode;
		protected bool annotations;

		protected void OnPagePrinded(PageEventArgs e)
		{
			if (PagePrinted != null)
				PagePrinted(this, e);
		}

		protected void OnEndPrint()
		{
			if (EndPrint != null)
				EndPrint(this, EventArgs.Empty);
		}

		public virtual void PrintPage(object[] images, object[] additionalInfo, int startPage, int endPage, int scaleMode, PrintOrientation orientation, bool annotations, int copyCount, string printer, string driverName, string portName)
		{
			this.orientation = orientation;
			this.startPage = startPage;
			this.endPage = endPage;
			this.images = images;
			this.printingPage = -1;
			this.scaleMode = scaleMode;
			this.annotations = annotations;
			this.copyCount = copyCount;
			System.Drawing.Printing.PrintDocument pd = new System.Drawing.Printing.PrintDocument();
			pd.PrinterSettings.PrinterName = printer;
			pd.PrinterSettings.MaximumPage = endPage;
			pd.PrinterSettings.Copies = (short)copyCount;
		    pd.DefaultPageSettings = pd.DefaultPageSettings;
			pd.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins((int)pd.DefaultPageSettings.HardMarginX, (int)pd.DefaultPageSettings.HardMarginX, (int)pd.DefaultPageSettings.HardMarginY, (int)pd.DefaultPageSettings.HardMarginY);

			if (pd.PrinterSettings.IsValid && images.Length > 0)
			{
				SetOrientation(IsLand(images[0] as Tiff.PageInfo), pd.DefaultPageSettings);
				pd.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(Print_PrintPage);
				pd.Print();
			}
		}

		protected virtual bool IsLand(Tiff.PageInfo info)
		{
			Image img = info.Image;
			return img.Width * img.VerticalResolution > img.Height * img.HorizontalResolution;
		}

		protected void SetOrientation(bool landscape, System.Drawing.Printing.PageSettings settings)
		{
			if (orientation == PrintOrientation.Album)
		            settings.Landscape = true;
			else if (orientation == PrintOrientation.Book)
		            settings.Landscape = false;
			else
		            settings.Landscape = landscape;
		}

		protected virtual void Print_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
		{
			if (printingPage == -1)
				printingPage = startPage;
			else
				printingPage++;
			Tiff.PageInfo pi = images[printingPage - startPage] as Tiff.PageInfo;

			if (pi != null && pi.Image != null)
			{
				OnPagePrinded(new PageEventArgs(printingPage - 1));
				System.Drawing.Image img = pi.Image;
				if (annotations)
				{
					RenderAnnotations renderAnnotations = new RenderAnnotations(0);
					byte[] notes = pi.Annotation;
					TiffAnnotation tiffAnnotation = null;
					if (notes != null)
					{
						tiffAnnotation = new TiffAnnotation(null);
						tiffAnnotation.Parse(notes);
						Bitmap bitmapImg = new Bitmap(img);

						if (img.HorizontalResolution == 0 || img.VerticalResolution == 0)
							bitmapImg.SetResolution(200, 200);
						else
							bitmapImg.SetResolution(img.HorizontalResolution, img.VerticalResolution);

						using (Graphics g = Graphics.FromImage(bitmapImg))
						{
							g.InterpolationMode = InterpolationMode.High;
							Rectangle[] rects = null;
							renderAnnotations.DrawAnnotation(g, tiffAnnotation, ImageControl.AllMarkGroupsVisibleList, bitmapImg, InterpolationMode.High, ref  rects, null);
						}
						img = bitmapImg;
					}
				}
				//e.Graphics.PageUnit = GraphicsUnit.Pixel;
				if (scaleMode == 1)
				{
					float hr = (img.HorizontalResolution > 0) ? img.HorizontalResolution : 200f; float vr = (img.VerticalResolution > 0) ? img.VerticalResolution : 200f;
					e.Graphics.DrawImage(img, 0f, 0f, img.Width * 100f / hr, img.Height * 100f / vr);
				}
				else
				{
					IntPtr hdc = e.Graphics.GetHdc();
					int realwidth = GetDeviceCaps(hdc, HORZRES);
					int realheight = GetDeviceCaps(hdc, VERTRES);
					e.Graphics.ReleaseHdc(hdc);

                    int rx = e.PageSettings.PrinterResolution.X, ry = e.PageSettings.PrinterResolution.Y, rdef = 300;
					if (rx < 1) rx = rdef; if (ry < 1) ry = rx;

					if (realwidth > 0)
                        realwidth = realwidth * 100 / rx;
					else
						realwidth = e.PageBounds.Width * 100 / rx;
					if (realheight > 0)
                        realheight = realheight * 100 / ry;
					else
						realheight = e.PageBounds.Height * 100 / ry;
					bool isScale = (float)img.Width > (float)(realwidth) * img.VerticalResolution / 100f || img.Height > realheight * img.HorizontalResolution / 100f;
					bool isWidth = img.Width / img.HorizontalResolution / realwidth > img.Height / img.VerticalResolution / realheight;
					if (isScale)
					{
						int swidth;
						int sheight;
						if (isWidth)
						{
							swidth = realwidth;
							sheight = (int)((float)swidth / (float)img.Width * img.HorizontalResolution / img.VerticalResolution * (float)img.Height);
						}
						else
						{

							sheight = realheight;
							swidth = (int)((float)sheight / (float)img.Height * img.VerticalResolution / img.HorizontalResolution * (float)img.Width);
						}
						e.Graphics.DrawImage(img, 0, 0, swidth, sheight);
					}
					else
						e.Graphics.DrawImage(img, 0f, 0f, (float)img.Width * 100f / img.HorizontalResolution, (float)img.Height * 100f / img.VerticalResolution);
				}
			}

			if (printingPage < endPage)
			{
				Image img = (images[printingPage - startPage] as Tiff.PageInfo).Image;
				SetOrientation(img.Width * img.VerticalResolution > img.Height * img.HorizontalResolution, e.PageSettings);
				e.HasMorePages = true;
			}
			else
			{
				e.HasMorePages = false;
				OnEndPrint();
			}
		}
	}

	public class PageEventArgs : EventArgs
	{
	    public int Page { get; set; }

	    public PageEventArgs(int page)
		{
			Page = page;
		}
	}
}
