/* **************************************************************************
             Converting memory DIB to .NET 'Bitmap' object
                  EXPERIMENTAL, USE AT YOUR OWN RISK     
                       http://dnetmaster.net/
*****************************************************************************/
//
// The 'DibToImage' class provides three different methods [Stream/scan0/HBITMAP alive]
//
// The parameter 'IntPtr dibPtr' is a pointer to
// a classic GDI 'packed DIB bitmap', starting with a BITMAPINFOHEADER
//
// Note, all this methods will use MUCH memory! 
//   (multiple copies of pixel datas)
//
// Whatever I used, all Bitmap/Image constructors
// return objects still beeing backed by the underlying Stream/scan0/HBITMAP.
// Thus you would have to keep the Stream/scan0/HBITMAP alive!
//
// So I tried to make an exact copy/clone of the Bitmap:
// But e.g. Bitmap.Clone() doesn't make a stand-alone duplicate.
// The working method I used here is :   Bitmap copy = new Bitmap( original );
// Unfortunately, the returned Bitmap will always have a pixel-depth of 32bppARGB !
// But this is a pure GDI+/.NET problem... maybe somebody else can help?
// 
//
//             ----------------------------
// Note, Microsoft should really wrap GDI+ 'GdipCreateBitmapFromGdiDib' in .NET!
// This would be very useful!
//
// There is a :
//        Bitmap Image.FromHbitmap( IntPtr hbitmap )
// so there is NO reason to not add a:
//        Bitmap Image.FromGdiDib( IntPtr dibptr )
//
// PLEASE SEND EMAIL TO:  netfwsdk@microsoft.com
//   OR  mswish@microsoft.com
//   OR  http://register.microsoft.com/mswish/suggestion.asp
// ------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Kesco.Lib.Win.ImageControl.TwainLib
{
	public class DibToImage
	{
		/// <summary>
		/// Get .NET 'Bitmap' object from memory DIB via stream constructor.
		/// This should work for most DIBs.
		/// </summary>
		/// <param name="dibPtr">Pointer to memory DIB, starting with BITMAPINFOHEADER.</param>
		public static Bitmap WithStream(IntPtr dibPtr)
		{
			BITMAPFILEHEADER fh = new BITMAPFILEHEADER();
			Type bmiTyp = typeof(BITMAPINFOHEADER);
			BITMAPINFOHEADER bmi = (BITMAPINFOHEADER)Marshal.PtrToStructure(dibPtr, bmiTyp);
			float resolutionX = bmi.biXPelsPerMeter * 0.0254f;
			float resolutionY = bmi.biYPelsPerMeter * 0.0254f;
			if (bmi.biSizeImage == 0)
				bmi.biSizeImage = ((((bmi.biWidth * bmi.biBitCount) + 31) & ~31) >> 3) * Math.Abs(bmi.biHeight);
			if ((bmi.biClrUsed == 0) && (bmi.biBitCount < 16))
				bmi.biClrUsed = 1 << bmi.biBitCount;

			int fhSize = Marshal.SizeOf(typeof(BITMAPFILEHEADER));
			int dibSize = bmi.biSize + (bmi.biClrUsed * 4) + bmi.biSizeImage;  // info + rgb + pixels

			fh.Type = new Char[] { 'B', 'M' };						// "BM"
			fh.bfSize = fhSize + dibSize;								// final file size
			fh.bfOffBits = fhSize + bmi.biSize + (bmi.biClrUsed * 4);	// offset to pixels

			byte[] data = new byte[fh.bfSize];					// file-sized byte[] 
			RawSerializeInto(fh, data);						// serialize BITMAPFILEHEADER into byte[]
			Marshal.Copy(dibPtr, data, fhSize, dibSize);		// mem-copy DIB into byte[]
			Bitmap tmp = null;
			MemoryStream stream = null;
			try
			{
				stream = new MemoryStream(data);
				stream.Position = 0;
				tmp = new Bitmap(stream);					// 'tmp' is wired to stream (unfortunately)
			}
			catch { }
			finally
			{
				if (stream != null)
				{
					stream.Close();
					stream = null;
				}
				data = null;
			}
			tmp = new Bitmap(tmp);
			tmp.SetResolution(resolutionX, resolutionY);
			return tmp;
		}

		/// <summary>
		/// Некорректный способ
		/// </summary>
		/// <param name="dibPtr"></param>
		/// <returns></returns>
		public static Bitmap WithScan0(IntPtr dibPtr)
		{
			Type bmiTyp = typeof(BITMAPINFOHEADER);
			BITMAPINFOHEADER bmi = (BITMAPINFOHEADER)Marshal.PtrToStructure(dibPtr, bmiTyp);
			if (bmi.biCompression != 0)
				throw new ArgumentException("Invalid bitmap format (non-RGB)", "BITMAPINFOHEADER.biCompression");
			if ((bmi.biClrUsed == 0) && (bmi.biBitCount < 16))
				bmi.biClrUsed = 1 << bmi.biBitCount;
			float resolutionX = bmi.biXPelsPerMeter * 0.0254f;
			float resolutionY = bmi.biYPelsPerMeter * 0.0254f;
			System.Drawing.Imaging.PixelFormat fmt = System.Drawing.Imaging.PixelFormat.Undefined;
			if (bmi.biBitCount == 8)
				return WithStream(dibPtr);
			else if (bmi.biBitCount == 1 || bmi.biBitCount == 8)
			{
				Bitmap tmp = null;
				BitmapData bd = null;
				GCHandle handle = new GCHandle();
				try
				{
					if (bmi.biBitCount == 1)
						fmt = System.Drawing.Imaging.PixelFormat.Format1bppIndexed;
					else if (bmi.biBitCount == 8)
						fmt = System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
					int stride = (((bmi.biWidth * bmi.biBitCount) + 31) & ~31) >> 3;	// bytes/line
					tmp = new Bitmap(bmi.biWidth, Math.Abs(bmi.biHeight), fmt);
					tmp.SetResolution(resolutionX, resolutionY);
					bd = tmp.LockBits(new Rectangle(0, 0, bmi.biWidth, Math.Abs(bmi.biHeight)), ImageLockMode.WriteOnly, fmt);

					BITMAPFILEHEADER fh = new BITMAPFILEHEADER();
					int fhSize = Marshal.SizeOf(typeof(BITMAPFILEHEADER));
					int dibSize = bmi.biSize + (bmi.biClrUsed * 4) + stride * Math.Abs(bmi.biHeight);  // info + rgb + pixels
					int headers = fhSize + bmi.biSize;
					fh.Type = new Char[] { 'B', 'M' };						// "BM"
					fh.bfSize = fhSize + dibSize;								// final file size
					fh.bfOffBits = headers + (bmi.biClrUsed * 4);	// offset to pixels

					byte[] data = new byte[fh.bfSize];					// file-sized byte[] 
					RawSerializeInto(fh, data);						// serialize BITMAPFILEHEADER into byte[]
					Marshal.Copy(dibPtr, data, fhSize, dibSize);		// mem-copy DIB into byte[]
					handle = GCHandle.Alloc(data, GCHandleType.Pinned);
					IntPtr buffer = handle.AddrOfPinnedObject();


					Marshal.Copy(data, fh.bfOffBits, bd.Scan0, data.Length - fh.bfOffBits);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Ошибка создания картинки со сканера " + ex.Message);
				}
				finally
				{
					handle.Free();
					tmp.UnlockBits(bd);
					tmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
				}
				return tmp;
			}
			else if (bmi.biBitCount == 24)
				fmt = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
			else if (bmi.biBitCount == 32)
				fmt = System.Drawing.Imaging.PixelFormat.Format32bppRgb;
			else if (bmi.biBitCount == 16)
				fmt = System.Drawing.Imaging.PixelFormat.Format16bppRgb555;
			else if (bmi.biBitCount == 4)
				fmt = System.Drawing.Imaging.PixelFormat.Format4bppIndexed;
			else
				throw new ArgumentException("Invalid pixel depth", "BITMAPINFOHEADER.biBitCount");

			int scan0 = ((int)dibPtr) + bmi.biSize + (bmi.biClrUsed * 4);		// pointer to pixels
			int strideRGB = (((bmi.biWidth * bmi.biBitCount) + 31) & ~31) >> 3;	// bytes/line

			if (bmi.biHeight > 0)
			{													// bottom-up
				scan0 += strideRGB * (bmi.biHeight - 1);
				strideRGB = -strideRGB;
			}


			Bitmap tmpRGB = new Bitmap(bmi.biWidth, Math.Abs(bmi.biHeight),
				strideRGB, fmt, (IntPtr)scan0);			// 'tmp' is wired to scan0 (unfortunately)

			Bitmap result = tmpRGB;
			try
			{
				result = new Bitmap(tmpRGB);								// 'result' is a copy (stand-alone)
				tmpRGB.Dispose();
				tmpRGB = null;

			}
			catch
			{
				result = tmpRGB;
			}
			return result;
		}

		/// <summary> Copy structure into Byte-Array. </summary>
		private static void RawSerializeInto(object anything, byte[] datas)
		{
			int rawsize = Marshal.SizeOf(anything);
			if (rawsize > datas.Length)
				throw new ArgumentException(" buffer too small ", " byte[] datas ");
			GCHandle handle = GCHandle.Alloc(datas, GCHandleType.Pinned);
			IntPtr buffer = handle.AddrOfPinnedObject();
			Marshal.StructureToPtr(anything, buffer, false);
			handle.Free();
		}

		[DllImport("gdi32.dll", ExactSpelling = true)]
		private static extern bool DeleteObject(IntPtr obj);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		private static extern int GdipCreateBitmapFromGdiDib(IntPtr bminfo, IntPtr pixdat, ref IntPtr image);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		private static extern int GdipCreateHBITMAPFromBitmap(IntPtr image, out IntPtr hbitmap, int bkg);

		[DllImport("gdiplus.dll", ExactSpelling = true)]
		private static extern int GdipDisposeImage(IntPtr image);

		[StructLayout(LayoutKind.Sequential)]
		internal struct RGBQUAD
		{
			internal byte rgbBlue;
			internal byte rgbGreen;
			internal byte rgbRed;
			internal byte rgbReserved;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct BITMAPINFO
		{
			internal BITMAPINFOHEADER bmiHeader;
			internal RGBQUAD bmiColors;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 2)]
		internal struct BITMAPINFOHEADER
		{
			internal int biSize;
			internal int biWidth;
			internal int biHeight;
			internal Int16 biPlanes;
			internal Int16 biBitCount;
			internal int biCompression;
			internal int biSizeImage;
			internal int biXPelsPerMeter;
			internal int biYPelsPerMeter;
			internal int biClrUsed;
			internal int biClrImportant;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
		internal struct BITMAPFILEHEADER
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			internal Char[] Type;
			internal int bfSize;
			internal Int16 bfReserved1;
			internal Int16 bfReserved2;
			internal int bfOffBits;
		}
	} // class DibToImage
} // namespace
