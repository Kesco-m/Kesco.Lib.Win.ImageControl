using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Kesco.Lib.Win.ImageControl
{
	/// <summary>
	/// Summary description for TiffAnnotation.
	/// </summary>
	public sealed class TiffAnnotation : IDisposable
	{
		private string oiGroup;
		private string oiIndex;
		private UInt32 sizeInt;
		private UInt32 header;
		private List<OIAN_MARK_ATTRIBUTES> _attributes = new List<OIAN_MARK_ATTRIBUTES>();
		private const Int32 LOGFONTSIZE = 60;
		private const Int32 LF_FACESIZE = 32;
		private ContextMenu menu;
		private MenuItem itemProperties;

		public string OiGroup
		{
			get { return oiGroup; }
			set { oiGroup = value; }
		}
		public string OiIndex
		{
			get { return oiIndex; }
			set { oiIndex = value; }
		}

		public List<OIAN_MARK_ATTRIBUTES> MarkAttributes
		{
			get { return _attributes; }
		}

		System.Windows.Forms.Control parent;
		public TiffAnnotation(Control parent)
		{
			this.parent = parent;
			header = 196612;
			sizeInt = 1;
			oiGroup = "[Ѕез имени]";
			oiIndex = "1";
			menu = new ContextMenu();
			menu.MenuItems.Add(new MenuItem(ResourcesManager.StringResources.GetString("Delete"),
				new EventHandler(DeleteFigure)));
			menu.MenuItems.Add(itemProperties = new MenuItem(
				ResourcesManager.StringResources.GetString("Settings"), new EventHandler(ShowProperties)));
		}

		private void DeleteFigure(object sender, EventArgs args)
		{
			DeleteSelectedFigures();
		}

		public void DeleteSelectedFigures()
		{
			List<IBufferBitmap> figuresForDel = new List<IBufferBitmap>(figures.Count);
			int x = 0; int y = 0; int width = 0; int height = 0;
			foreach (IBufferBitmap figure in figures)
				if (figure.Selected)
					figuresForDel.Add(figure);
			foreach (IBufferBitmap figure in figuresForDel)
			{
				Rectangle rect = figure.Rect;
				if (width == 0 && height == 0)
				{
					x = rect.X; y = rect.Y;
					width = rect.Width; height = rect.Height;
				}
				else
				{
					if (x > rect.X)
						x = rect.X;
					if (y > rect.Y)
						y = rect.Y;
					if (x + width > rect.Right)
						width = rect.Right - x;
					if (y + height > rect.Bottom)
						height = rect.Bottom - y;
				}
				_attributes.Remove(figure.Attributes);
				figures.Remove(figure);
			}
			if (width != 0 || height != 0)
				OnModifiedFigure(this, new ModifyEventArgs(new Rectangle(x, y, width, height)));
		}

		private void ShowProperties(object sender, EventArgs args)
		{
			IBufferBitmap figuresel = null;
			foreach (IBufferBitmap figure in figures)
			{
				if (figure.Selected)
					figuresel = figure;
			}
			if (figuresel != null)
			{
				RectanglesPropertiesDialog dialog = new RectanglesPropertiesDialog(figuresel, parent);
				dialog.Show();
			}
		}

		/// <summary>
		/// ѕоказ контекстного меню дл€ заметки
		/// </summary>
		public void ContextMenuShow(Control control, Point pos)
		{
			int selectedCount = 0;
			foreach (IBufferBitmap figure in figures)
				if (figure.Selected && ++selectedCount > 1)
					break;	// больше одного - всЄ равно сколько

			// пункт виден, если выбрано что-то одно
			itemProperties.Visible = selectedCount == 1;
			menu.Show(control, pos);
		}

		private static byte[] GetBytes(byte[] srcBytes, byte[] addBytes, int length)
		{
			int index = srcBytes.Length;
			byte[] result = new byte[srcBytes.Length + (length == 0 ? addBytes.Length : length)];
			Array.Copy(srcBytes, result, srcBytes.Length);
			//Array.Copy(addBytes, 0, result, (length == 0 ? index : length - addBytes.Length), (length == 0 ? addBytes.Length : length));
			Array.Copy(addBytes, 0, result, index, addBytes.Length);

			return result;
		}

		internal byte[] GetStampsBytes()
		{
			if (MarkAttributes == null || MarkAttributes.Count == 0)
				return null;

			var stamps = MarkAttributes.FindAll(x => x.UType == AnnotationMarkType.ImageEmbedded);

			if (stamps.Count == 0)
				return null;

			byte[] result = BitConverter.GetBytes(header);
			result = GetBytes(result, BitConverter.GetBytes(sizeInt), 0);
			// данные по блоку
			result = GetBytes(result, BitConverter.GetBytes(2), 0);
			result = GetBytes(result, BitConverter.GetBytes(12), 0);

			result = GetBytes(result, ASCIIEncoding.Default.GetBytes("OiGroup" + '\0'), 8);
			byte[] boiGroup = ASCIIEncoding.Default.GetBytes(oiGroup + '\0');
			result = GetBytes(result, BitConverter.GetBytes(boiGroup.Length), 0);
			result = GetBytes(result, boiGroup, 0);
			// данные по блоку
			result = GetBytes(result, BitConverter.GetBytes(2), 0);
			result = GetBytes(result, BitConverter.GetBytes(12), 0);

			result = GetBytes(result, ASCIIEncoding.Default.GetBytes("OiIndex" + '\0'), 8);
			byte[] boiIndex = ASCIIEncoding.Default.GetBytes(oiIndex + '\0');
			result = GetBytes(result, BitConverter.GetBytes(10), 0);
			result = GetBytes(result, boiIndex, 10);

			foreach (OIAN_MARK_ATTRIBUTES attr in stamps)
			{
				// данные по блоку
				result = GetBytes(result, BitConverter.GetBytes(5), 0);
				result = GetBytes(result, BitConverter.GetBytes(164), 0);


				byte[] battr = BitConverter.GetBytes((uint)attr.UType);//UType
				battr = GetBytes(battr, BitConverter.GetBytes(attr.LrBounds.Left), 0);//lrBounds
				battr = GetBytes(battr, BitConverter.GetBytes(attr.LrBounds.Top), 0);//lrBounds
				battr = GetBytes(battr, BitConverter.GetBytes(attr.LrBounds.Right), 0);//lrBounds
				battr = GetBytes(battr, BitConverter.GetBytes(attr.LrBounds.Bottom), 0);//lrBounds

				battr = GetBytes(battr, new byte[] { attr.RgbColor1.B, attr.RgbColor1.G, attr.RgbColor1.R, 0 }, 0);//rgbColor1

				battr = GetBytes(battr, new byte[] { attr.RgbColor2.B, attr.RgbColor2.G, attr.RgbColor2.R, 0 }, 0);//rgbColor2

				battr = GetBytes(battr, BitConverter.GetBytes(Convert.ToInt32(attr.BHighlighting)), 0);// bHighlighting
				battr = GetBytes(battr, BitConverter.GetBytes(Convert.ToInt32(attr.BTransparent)), 0);// bTransparent
				battr = GetBytes(battr, BitConverter.GetBytes(Convert.ToInt32(attr.ULineSize)), 0);//ULineSize
				battr = GetBytes(battr, BitConverter.GetBytes(Convert.ToInt32(attr.UReserved1)), 0);//UReserved1
				battr = GetBytes(battr, BitConverter.GetBytes(Convert.ToInt32(attr.UReserved2)), 0);//UReserved2

				battr = GetBytes(battr, GetByteFormLogFont(attr.LfFont), 0);//lfFont    
				battr = GetBytes(battr, BitConverter.GetBytes(attr.BReserved3), 0);//BReserved3 
				battr = GetBytes(battr, BitConverter.GetBytes(Convert.ToInt32((int)attr.Time1)), 0);//time 
				battr = GetBytes(battr, BitConverter.GetBytes(Convert.ToInt32(attr.BVisible)), 0);//BVisible
				battr = GetBytes(battr, BitConverter.GetBytes(Convert.ToInt32(attr.DwReserved4)), 0);//DwReserved4
				for (int i = 0; i < attr.LReserved.Length; i++)
					battr = GetBytes(battr, BitConverter.GetBytes(attr.LReserved[i]), 0);//LReserved
				if (battr.Length != 164)
					throw new Exception("ѕлохой размер OIAN_MARK_ATTRIBUTES");
				result = GetBytes(result, battr, 0);

				if (attr.OiDIB_AN_IMAGE_STRUCT != null)
				{
					// данные по блоку
					result = GetBytes(result, BitConverter.GetBytes(6), 0);
					result = GetBytes(result, BitConverter.GetBytes(12), 0);

					result = GetBytes(result, ASCIIEncoding.Default.GetBytes("OiDIB" + '\0'), 8);
					result = GetBytes(result, BitConverter.GetBytes(attr.OiDIB_AN_IMAGE_STRUCT.DibInfo.Length), 0);
					result = GetBytes(result, attr.OiDIB_AN_IMAGE_STRUCT.DibInfo, 0);
				}
				if (attr.OiAnoDat_AN_NEW_ROTATE_STRUCT != null)
				{
					byte[] resrotate = GetBytesFromRotateStruct(attr.OiAnoDat_AN_NEW_ROTATE_STRUCT);
					// данные по блоку
					result = GetBytes(result, BitConverter.GetBytes(6), 0);
					result = GetBytes(result, BitConverter.GetBytes(12), 0);

					result = GetBytes(result, ASCIIEncoding.Default.GetBytes("OiAnoDat"), 8);
					result = GetBytes(result, BitConverter.GetBytes(resrotate.Length), 0);
					result = GetBytes(result, resrotate, 0);
				}
				// данные по блоку
				result = GetBytes(result, BitConverter.GetBytes(6), 0);
				result = GetBytes(result, BitConverter.GetBytes(12), 0);

				result = GetBytes(result, ASCIIEncoding.Default.GetBytes("OiGroup" + '\0'), 8);
				byte[] boiGroupa = ASCIIEncoding.Default.GetBytes(attr.OiGroup + '\0');
				result = GetBytes(result, BitConverter.GetBytes(boiGroupa.Length), 0);
				result = GetBytes(result, boiGroupa, 0);
				// данные по блоку
				result = GetBytes(result, BitConverter.GetBytes(6), 0);
				result = GetBytes(result, BitConverter.GetBytes(12), 0);

				result = GetBytes(result, ASCIIEncoding.Default.GetBytes("OiIndex" + '\0'), 8);
				byte[] boiIndexa = ASCIIEncoding.Default.GetBytes(attr.OiIndex + '\0');
				result = GetBytes(result, BitConverter.GetBytes(10), 0);
				result = GetBytes(result, boiIndex, 10);
			}
			return result;
		}

		public byte[] GetAnnotationBytes(bool withImageEmbedded)
		{
			if (MarkAttributes == null || MarkAttributes.Count == 0)
				return null;

			if (!withImageEmbedded && !MarkAttributes.Exists(x => x.UType != AnnotationMarkType.ImageEmbedded))
				return null;

			byte[] result = BitConverter.GetBytes(header);
			result = GetBytes(result, BitConverter.GetBytes(sizeInt), 0);
			// данные по блоку
			result = GetBytes(result, BitConverter.GetBytes(2), 0);
			result = GetBytes(result, BitConverter.GetBytes(12), 0);

			result = GetBytes(result, ASCIIEncoding.Default.GetBytes("OiGroup" + '\0'), 8);
			byte[] boiGroup = ASCIIEncoding.Default.GetBytes(oiGroup + '\0');
			result = GetBytes(result, BitConverter.GetBytes(boiGroup.Length), 0);
			result = GetBytes(result, boiGroup, 0);
			// данные по блоку
			result = GetBytes(result, BitConverter.GetBytes(2), 0);
			result = GetBytes(result, BitConverter.GetBytes(12), 0);

			result = GetBytes(result, ASCIIEncoding.Default.GetBytes("OiIndex" + '\0'), 8);
			byte[] boiIndex = ASCIIEncoding.Default.GetBytes(oiIndex + '\0');
			result = GetBytes(result, BitConverter.GetBytes(10), 0);
			result = GetBytes(result, boiIndex, 10);

			foreach (OIAN_MARK_ATTRIBUTES attr in MarkAttributes)
			{
				if (!withImageEmbedded && attr.UType == AnnotationMarkType.ImageEmbedded)
					continue;
				// данные по блоку
				result = GetBytes(result, BitConverter.GetBytes(5), 0);
				result = GetBytes(result, BitConverter.GetBytes(164), 0);


				byte[] battr = BitConverter.GetBytes((uint)attr.UType);//UType
				battr = GetBytes(battr, BitConverter.GetBytes(attr.LrBounds.Left), 0);//lrBounds
				battr = GetBytes(battr, BitConverter.GetBytes(attr.LrBounds.Top), 0);//lrBounds
				battr = GetBytes(battr, BitConverter.GetBytes(attr.LrBounds.Right), 0);//lrBounds
				battr = GetBytes(battr, BitConverter.GetBytes(attr.LrBounds.Bottom), 0);//lrBounds

				battr = GetBytes(battr, new byte[] { attr.RgbColor1.B, attr.RgbColor1.G, attr.RgbColor1.R, 0 }, 0);//rgbColor1

				battr = GetBytes(battr, new byte[] { attr.RgbColor2.B, attr.RgbColor2.G, attr.RgbColor2.R, 0 }, 0);//rgbColor2

				battr = GetBytes(battr, BitConverter.GetBytes(Convert.ToInt32(attr.BHighlighting)), 0);// bHighlighting
				battr = GetBytes(battr, BitConverter.GetBytes(Convert.ToInt32(attr.BTransparent)), 0);// bTransparent
				battr = GetBytes(battr, BitConverter.GetBytes(Convert.ToInt32(attr.ULineSize)), 0);//ULineSize
				battr = GetBytes(battr, BitConverter.GetBytes(Convert.ToInt32(attr.UReserved1)), 0);//UReserved1
				battr = GetBytes(battr, BitConverter.GetBytes(Convert.ToInt32(attr.UReserved2)), 0);//UReserved2

				battr = GetBytes(battr, GetByteFormLogFont(attr.LfFont), 0);//lfFont    
				battr = GetBytes(battr, BitConverter.GetBytes(attr.BReserved3), 0);//BReserved3 
				battr = GetBytes(battr, BitConverter.GetBytes(Convert.ToInt32((int)attr.Time1)), 0);//time 
				battr = GetBytes(battr, BitConverter.GetBytes(Convert.ToInt32(attr.BVisible)), 0);//BVisible
				battr = GetBytes(battr, BitConverter.GetBytes(Convert.ToInt32(attr.DwReserved4)), 0);//DwReserved4
				for (int i = 0; i < attr.LReserved.Length; i++)
					battr = GetBytes(battr, BitConverter.GetBytes(attr.LReserved[i]), 0);//LReserved
				if (battr.Length != 164)
					throw new Exception("ѕлохой размер OIAN_MARK_ATTRIBUTES");
				result = GetBytes(result, battr, 0);

				if (attr.OiAnText_OIAN_TEXTPRIVDATA != null)
				{

					attr.OiAnText_OIAN_TEXTPRIVDATA.SzAnoText += '\0';
					attr.OiAnText_OIAN_TEXTPRIVDATA.UAnoTextLength = (uint)attr.OiAnText_OIAN_TEXTPRIVDATA.SzAnoText.Length;

					IntPtr ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(attr.OiAnText_OIAN_TEXTPRIVDATA));
					//                    Marshal.WriteInt32(ptr, attr.OiAnText_OIAN_TEXTPRIVDATA.NCurrentOrientation);
					//					Marshal.WriteInt32(new IntPtr(ptr.ToInt32() + 4), (int)attr.OiAnText_OIAN_TEXTPRIVDATA.UReserved1);
					//					Marshal.WriteInt32(new IntPtr(ptr.ToInt32() + 8), (int)attr.OiAnText_OIAN_TEXTPRIVDATA.UCreationScale);
					//					Marshal.WriteInt32(new IntPtr(ptr.ToInt32() + 12), (int)attr.OiAnText_OIAN_TEXTPRIVDATA.UAnoTextLength);
					Marshal.StructureToPtr(attr.OiAnText_OIAN_TEXTPRIVDATA, ptr, true);
					byte[] res = new byte[16 + attr.OiAnText_OIAN_TEXTPRIVDATA.SzAnoText.Length];
					Marshal.Copy(ptr, res, 0, 16 + attr.OiAnText_OIAN_TEXTPRIVDATA.SzAnoText.Length);
					Marshal.FreeCoTaskMem(ptr);

					// данные по блоку
					result = GetBytes(result, BitConverter.GetBytes(6), 0);
					result = GetBytes(result, BitConverter.GetBytes(12), 0);

					result = GetBytes(result, ASCIIEncoding.Default.GetBytes("OiAnText"), 8);
					result = GetBytes(result, BitConverter.GetBytes(res.Length), 0);
					result = GetBytes(result, res, 0);
				}

				if (attr.OiFilNam_AN_NAME_STRUCT != null)
				{
					byte[] res = ASCIIEncoding.Default.GetBytes(attr.OiFilNam_AN_NAME_STRUCT.Name + '\0');
					// данные по блоку
					result = GetBytes(result, BitConverter.GetBytes(6), 0);
					result = GetBytes(result, BitConverter.GetBytes(12), 0);

					result = GetBytes(result, ASCIIEncoding.Default.GetBytes("OiFilNam"), 8);
					result = GetBytes(result, BitConverter.GetBytes(res.Length), 0);
					result = GetBytes(result, res, 0);
				}
				if (attr.OiDIB_AN_IMAGE_STRUCT != null)
				{
					// данные по блоку
					result = GetBytes(result, BitConverter.GetBytes(6), 0);
					result = GetBytes(result, BitConverter.GetBytes(12), 0);

					result = GetBytes(result, ASCIIEncoding.Default.GetBytes("OiDIB" + '\0'), 8);
					result = GetBytes(result, BitConverter.GetBytes(attr.OiDIB_AN_IMAGE_STRUCT.DibInfo.Length), 0);
					result = GetBytes(result, attr.OiDIB_AN_IMAGE_STRUCT.DibInfo, 0);
				}
				if (attr.OiAnoDat_AN_NEW_ROTATE_STRUCT != null)
				{
					byte[] resrotate = GetBytesFromRotateStruct(attr.OiAnoDat_AN_NEW_ROTATE_STRUCT);
					// данные по блоку
					result = GetBytes(result, BitConverter.GetBytes(6), 0);
					result = GetBytes(result, BitConverter.GetBytes(12), 0);

					result = GetBytes(result, ASCIIEncoding.Default.GetBytes("OiAnoDat"), 8);
					result = GetBytes(result, BitConverter.GetBytes(resrotate.Length), 0);
					result = GetBytes(result, resrotate, 0);
				}
				// данные по блоку
				result = GetBytes(result, BitConverter.GetBytes(6), 0);
				result = GetBytes(result, BitConverter.GetBytes(12), 0);

				result = GetBytes(result, ASCIIEncoding.Default.GetBytes("OiGroup" + '\0'), 8);
				byte[] boiGroupa = ASCIIEncoding.Default.GetBytes(attr.OiGroup + '\0');
				result = GetBytes(result, BitConverter.GetBytes(boiGroupa.Length), 0);
				result = GetBytes(result, boiGroupa, 0);
				// данные по блоку
				result = GetBytes(result, BitConverter.GetBytes(6), 0);
				result = GetBytes(result, BitConverter.GetBytes(12), 0);

				result = GetBytes(result, ASCIIEncoding.Default.GetBytes("OiIndex" + '\0'), 8);
				byte[] boiIndexa = ASCIIEncoding.Default.GetBytes(attr.OiIndex + '\0');
				result = GetBytes(result, BitConverter.GetBytes(10), 0);
				result = GetBytes(result, boiIndex, 10);
			}

			return result;
		}

		public void Parse(byte[] annotationText)
		{
			if (annotationText.Length < 1)
				return;
			header = BitConverter.ToUInt32(annotationText, 0);
			sizeInt = BitConverter.ToUInt32(annotationText, 4);//тип определ€ющий размер инта в байтах(1-нормально, 0 - два байта)
			byte[] dataMain = new byte[annotationText.Length - 8];
			Array.Copy(annotationText, 8, dataMain, 0, annotationText.Length - 8);
			Parse(dataMain, null);
		}

		/// <summary>
		/// —оздает коллекцию аттрибутов аннотации из байтового массива
		/// </summary>
		private void Parse(byte[] annotationText, OIAN_MARK_ATTRIBUTES markAttributes)
		{
			OIAN_MARK_ATTRIBUTES attributes = markAttributes;
			Int32 type = BitConverter.ToInt32(annotationText, 0);//это тип(2,5,6) следующих данных
			Int32 size = BitConverter.ToInt32(annotationText, 4);//размер данных следующего блока
			if (size == 0)
				return;
			Int32 sizen = 0;
			switch (type)
			{
				case 2://Annotation Named Blocks(тип 2)
					String name = System.Text.ASCIIEncoding.ASCII.GetString(annotationText, 8, 8).Trim('\0');//им€
					Int32 offset = 16;
					sizen = TiffBitConverter.ToInt32(annotationText, ref offset);//рамер блока
					if (size > 12)
					{
						Int32 reserv = TiffBitConverter.ToInt32(annotationText, ref offset);//резерв
						//offset6 = 4;
					}
					byte[] data = new byte[sizen];
					Array.Copy(annotationText, offset, data, 0, sizen);
					switch (name)
					{
						case "OiGroup":
							//String nameOiGroup = System.Text.ASCIIEncoding.GetEncoding(1251).GetString(data, 0, size);
							String nameOiGroup = System.Text.ASCIIEncoding.Default.GetString(data, 0, sizen).Trim('\0');
							OiGroup = nameOiGroup;
							break;
						case "OiIndex":
							String oiIndex = System.Text.ASCIIEncoding.Default.GetString(data, 0, sizen).Trim('\0');
							this.OiIndex = oiIndex;
							break;
						default:
							//throw new Exception("Ќе учтенное им€ в типе 2: " + name);
							break;
					}
					break;
				case 5://mark(тип 5)
					byte[] mark = new byte[size];
					Array.Copy(annotationText, 8, mark, 0, size);
					attributes = CreateOianMarkAttributes(mark);
					MarkAttributes.Add(attributes);
					break;
				case 6://Annotation Named Blocks(тип 6)
					if (attributes == null)
						throw new Exception("ƒолжен быть создан экземпл€р атрибута");
					String name6 = System.Text.ASCIIEncoding.ASCII.GetString(annotationText, 8, 8).Trim('\0');//им€
					Int32 offset6 = 16;
					sizen = TiffBitConverter.ToInt32(annotationText, ref offset6);//рамер блока
					if (size > 12)
					{
						Int32 reserv = TiffBitConverter.ToInt32(annotationText, ref offset6);//резерв
						//offset6 = 4;
					}
					byte[] data6 = new byte[sizen];
					Array.Copy(annotationText, offset6, data6, 0, sizen);
					switch (name6)
					{
						case "OiGroup":
							//String nameOiGroup = System.Text.ASCIIEncoding.GetEncoding(1251).GetString(data, 0, size);
							String nameOiGroup = System.Text.ASCIIEncoding.Default.GetString(data6, 0, sizen).Trim('\0');
							attributes.OiGroup = nameOiGroup;
							break;
						case "OiIndex":
							String OiIndex = System.Text.ASCIIEncoding.Default.GetString(data6, 0, sizen).Trim('\0');
							attributes.OiIndex = OiIndex;
							break;
						case "OiAnoDat":
							switch (attributes.UType)
							{
								case AnnotationMarkType.ImageEmbedded:
								case AnnotationMarkType.ImageReference:
								case AnnotationMarkType.Form:
									attributes.OiAnoDat_AN_NEW_ROTATE_STRUCT = CreateRotateStruct(data6);
									break;
								case AnnotationMarkType.StraightLine:
								case AnnotationMarkType.FreehandLine:
									attributes.OiAnoDat_AN_POINTS = CreateAnPpoints(data6);
									break;
							}
							break;
						case "OiFilNam":
							TiffAnnotation.AN_NAME_STRUCT nameStruct = new TiffAnnotation.AN_NAME_STRUCT();
							nameStruct.Name = System.Text.ASCIIEncoding.Default.GetString(data6, 0, sizen).Trim('\0');
							attributes.OiFilNam_AN_NAME_STRUCT = nameStruct;
							break;
						case "OiAnText":
							attributes.OiAnText_OIAN_TEXTPRIVDATA = CreateTextPrivateData(data6);
							break;
						case "OiDIB":
							TiffAnnotation.AN_IMAGE_STRUCT image = new TiffAnnotation.AN_IMAGE_STRUCT();
							image.DibInfo = data6;
							attributes.OiDIB_AN_IMAGE_STRUCT = image;
							break;
						case "OiHypLnk":
							throw new Exception("OiHypLnk не реализована");
						case "MarkBnds":
							//не описано в спецификации
							break;
						default:
							//throw new Exception("Ќе учтенное им€ в типе 6: " + name6);
							break;
					}
					break;
			}

			Int32 nextSizeBlock = 8 + size + sizen;
			if (annotationText.Length - nextSizeBlock < 8)
				return;
			byte[] nextdata = new byte[annotationText.Length - nextSizeBlock];
			Array.Copy(annotationText, nextSizeBlock, nextdata, 0, annotationText.Length - nextSizeBlock);
			Parse(nextdata, attributes);
		}

		private class TiffBitConverter
		{
			public static Int32 SizeInt = 1;

			/// <summary>
			/// Returns a 32-bit unsigned integer converted from four bytes at a specified position in a byte array.
			/// </summary>
			/// <param name="value">An array of bytes.</param>
			/// <param name="startIndex">The starting position within value.</param>
			/// <returns>A 32-bit unsigned integer formed by four bytes beginning at startIndex.</returns>
			/// <exception cref="System.ArgumentNullException">value is null</exception>
			/// <exception cref="System.ArgumentOutOfRangeException">startIndex is less than zero or greater than the length of value minus 4</exception>
#pragma warning disable 3021
			[CLSCompliant(false)]
			public static uint ToUInt32(byte[] value, ref int startIndex)
			{
				if (Convert.ToBoolean(SizeInt))
				{
					uint result = BitConverter.ToUInt32(value, startIndex);
					startIndex += 4;
					return result;
				}
				else
				{
					uint result = BitConverter.ToUInt16(value, startIndex);
					startIndex += 2;
					return result;
				}
			}
#pragma warning restore 3021

			/// <summary>
			/// Returns a 32-bit signed integer converted from four bytes at a specified position in a byte array.
			/// </summary>
			/// <param name="value">An array of bytes.</param>
			/// <param name="startIndex">The starting position within value.</param>
			/// <returns> A 32-bit signed integer formed by four bytes beginning at startIndex.</returns>
			/// <exception cref="System.ArgumentNullException">value is null</exception>
			/// <exception cref="System.ArgumentOutOfRangeException">startIndex is less than zero or greater than the length of value minus 4</exception>
			public static int ToInt32(byte[] value, ref int startIndex)
			{
				if (Convert.ToBoolean(SizeInt))
				{
					int result = BitConverter.ToInt32(value, startIndex);
					startIndex += 4;
					return result;
				}
				else
				{
					int result = BitConverter.ToInt16(value, startIndex);
					startIndex += 2;
					return result;
				}
			}
		}

		private OIAN_TEXTPRIVDATA CreateTextPrivateData(byte[] data)
		{
			//byte[] buffer = new byte[data.Length];
			//Array.Copy(data, 0, buffer, 0, buffer.Length);
			//OIAN_TEXTPRIVDATA lf = null;

			//GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

			//try
			//{
			//    IntPtr ptr = handle.AddrOfPinnedObject();
			//    lf = (OIAN_TEXTPRIVDATA)Marshal.PtrToStructure(ptr, typeof(OIAN_TEXTPRIVDATA));
			//}
			//finally
			//{
			//    handle.Free();
			//}

			OIAN_TEXTPRIVDATA textPrivate = new OIAN_TEXTPRIVDATA();
			int offset = 0;
			textPrivate.NCurrentOrientation = TiffBitConverter.ToInt32(data, ref offset);
			textPrivate.UReserved1 = TiffBitConverter.ToUInt32(data, ref offset);
			textPrivate.UCreationScale = TiffBitConverter.ToUInt32(data, ref offset);
			textPrivate.UAnoTextLength = TiffBitConverter.ToUInt32(data, ref offset);
			textPrivate.SzAnoText = System.Text.ASCIIEncoding.Default.GetString(data, offset, (int)textPrivate.UAnoTextLength).Trim('\0');
			return textPrivate;
		}

		private AN_POINTS CreateAnPpoints(byte[] data)
		{
			AN_POINTS points = new AN_POINTS();
			int offset = 0;
			points.NMaxPoints = TiffBitConverter.ToInt32(data, ref offset);
			points.NPoints = TiffBitConverter.ToInt32(data, ref offset);
			Point[] point = new Point[(data.Length - offset) / (TiffBitConverter.SizeInt == 1 ? 8 : 4)];
			int statOffset = offset;
			for (int i = 0; i < point.Length; i++)
			{
				offset = statOffset + i * (TiffBitConverter.SizeInt == 1 ? 8 : 4);
				point[i] = new Point(TiffBitConverter.ToInt32(data, ref offset), TiffBitConverter.ToInt32(data, ref offset));
			}
			points.PtPoint = point;
			return points;
		}

		private OIAN_MARK_ATTRIBUTES CreateOianMarkAttributes(byte[] mark)
		{
			Int32 offsetMark = 0;
			OIAN_MARK_ATTRIBUTES attr = new OIAN_MARK_ATTRIBUTES();
			attr.UType = (AnnotationMarkType)TiffBitConverter.ToUInt32(mark, ref offsetMark);
			int x1 = TiffBitConverter.ToInt32(mark, ref offsetMark);
			int y1 = TiffBitConverter.ToInt32(mark, ref offsetMark);
			int x2 = TiffBitConverter.ToInt32(mark, ref offsetMark);
			int y2 = TiffBitConverter.ToInt32(mark, ref offsetMark);
			attr.LrBounds = new Rectangle(x1, y1, x2 - x1, y2 - y1);
			Color color = Color.FromArgb(255, mark[offsetMark + 2], mark[offsetMark + 1], mark[offsetMark]);
			attr.RgbColor1 = color;
			offsetMark += 4;
			color = Color.FromArgb(255, mark[offsetMark + 2], mark[offsetMark + 1], mark[offsetMark]);
			attr.RgbColor2 = color;
			offsetMark += 4;
			attr.BHighlighting = Convert.ToBoolean(TiffBitConverter.ToInt32(mark, ref offsetMark));
			attr.BTransparent = Convert.ToBoolean(TiffBitConverter.ToInt32(mark, ref offsetMark));
			attr.ULineSize = TiffBitConverter.ToUInt32(mark, ref offsetMark);
			attr.UReserved1 = TiffBitConverter.ToUInt32(mark, ref offsetMark);
			attr.UReserved2 = TiffBitConverter.ToUInt32(mark, ref offsetMark);
			byte[] dataFont = new byte[LOGFONTSIZE];
			Array.Copy(mark, offsetMark, dataFont, 0, LOGFONTSIZE);
			System.Drawing.Text.TextRenderingHint quality = System.Drawing.Text.TextRenderingHint.SystemDefault;
			attr.LfFont = GetFontFromLogFont(dataFont, ref quality);
			attr.FontRenderingHint = quality;
			offsetMark += LOGFONTSIZE;
			attr.BReserved3 = TiffBitConverter.ToUInt32(mark, ref offsetMark);
			if (BitConverter.ToInt32(mark, offsetMark) > 0)
				attr.Time1 = TiffBitConverter.ToInt32(mark, ref offsetMark);
			attr.BVisible = Convert.ToBoolean(TiffBitConverter.ToInt32(mark, ref offsetMark));
			attr.DwReserved4 = TiffBitConverter.ToUInt32(mark, ref offsetMark);
			if (attr.DwReserved4 != 1046591)
				throw new Exception("DwReserved4  не валидный");
			int length = (mark.Length - offsetMark) / (TiffBitConverter.SizeInt == 1 ? 4 : 2);
			int statOffset = offsetMark;
			attr.LReserved = new int[length];
			for (int i = 0; i < length; i++)
			{
				offsetMark = statOffset + i * (TiffBitConverter.SizeInt == 1 ? 4 : 2);
				attr.LReserved[i] = TiffBitConverter.ToInt32(mark, ref offsetMark);
			}
			return attr;
		}

		public static byte[] GetBytesFromRotateStruct(AN_NEW_ROTATE_STRUCT rstruct)
		{
			byte[] result = BitConverter.GetBytes(rstruct.Rotation);
			result = GetBytes(result, BitConverter.GetBytes(rstruct.Scale), 0);
			result = GetBytes(result, BitConverter.GetBytes(rstruct.NHRes), 0);
			result = GetBytes(result, BitConverter.GetBytes(rstruct.NVRes), 0);
			result = GetBytes(result, BitConverter.GetBytes(rstruct.NOrigHRes), 0);
			result = GetBytes(result, BitConverter.GetBytes(rstruct.NOrigVRes), 0);
			result = GetBytes(result, BitConverter.GetBytes(Convert.ToInt32(rstruct.BReserved1)), 0);
			result = GetBytes(result, BitConverter.GetBytes(Convert.ToInt32(rstruct.BReserved2)), 0);
			for (int i = 0; i < rstruct.NReserved.Length; i++)
				result = GetBytes(result, BitConverter.GetBytes(rstruct.NReserved[i]), 0);
			return result;
		}

		private AN_NEW_ROTATE_STRUCT CreateRotateStruct(byte[] data)
		{
			AN_NEW_ROTATE_STRUCT ROTATE_STRUCT = new AN_NEW_ROTATE_STRUCT();
			int offset = 0;
			ROTATE_STRUCT.Rotation = TiffBitConverter.ToInt32(data, ref offset);
			ROTATE_STRUCT.Scale = TiffBitConverter.ToInt32(data, ref offset);
			ROTATE_STRUCT.NHRes = TiffBitConverter.ToInt32(data, ref offset);
			ROTATE_STRUCT.NVRes = TiffBitConverter.ToInt32(data, ref offset);
			ROTATE_STRUCT.NOrigHRes = TiffBitConverter.ToInt32(data, ref offset);
			ROTATE_STRUCT.NOrigVRes = TiffBitConverter.ToInt32(data, ref offset);
			try
			{
				ROTATE_STRUCT.BReserved1 = Convert.ToBoolean(TiffBitConverter.ToInt32(data, ref offset));
				ROTATE_STRUCT.BReserved2 = Convert.ToBoolean(TiffBitConverter.ToInt32(data, ref offset));
			}
			catch { }
			int count = (data.Length - offset) >> 2;
			ROTATE_STRUCT.NReserved = new int[count];
			for (int i = 0; i < count; i++)
				ROTATE_STRUCT.NReserved[i] = TiffBitConverter.ToInt32(data, ref offset);
			return ROTATE_STRUCT;
		}

		public static byte[] GetByteFormLogFont(LOGFONTA font)
		{

			IntPtr ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(font));
			Marshal.StructureToPtr(font, ptr, true);
			byte[] result = new byte[Marshal.SizeOf(font)];
			Marshal.Copy(ptr, result, 0, result.Length);
			Marshal.FreeCoTaskMem(ptr);
			return result;

		}

		private LOGFONTA GetFontFromLogFont(byte[] data, ref System.Drawing.Text.TextRenderingHint fontRenderingHint)//не дописан
		{
			LOGFONTA lf = new LOGFONTA();
			if (BitConverter.ToInt32(data, 0) > 0)
			{
				GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);

				try
				{
					IntPtr ptr = handle.AddrOfPinnedObject();
					lf = (LOGFONTA)Marshal.PtrToStructure(ptr, typeof(LOGFONTA));
					fontRenderingHint = (System.Drawing.Text.TextRenderingHint)Convert.ToUInt32(lf.lfQuality);
				}
				finally
				{
					handle.Free();
				}

			}

			return lf;
		}

		#region enums

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public class LOGFONTA
		{
			public int lfHeight = 0;
			public int lfWidth = 0;
			public int lfEscapement = 0;
			public int lfOrientation = 0;
			public int lfWeight = 0;
			public byte lfItalic = 0;
			public byte lfUnderline = 0;
			public byte lfStrikeOut = 0;
			public byte lfCharSet = 0;
			public byte lfOutPrecision = 0;
			public byte lfClipPrecision = 0;
			public byte lfQuality = 0;
			public byte lfPitchAndFamily = 0;
			[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = LF_FACESIZE)]
			public string lfFaceName = null;

			public LOGFONTA() { }
			public LOGFONTA(LOGFONTW font)
			{
				lfHeight = font.lfHeight;
				lfWidth = font.lfWidth;
				lfEscapement = font.lfEscapement;
				lfOrientation = font.lfOrientation;
				lfWeight = font.lfWeight;
				lfItalic = font.lfItalic;
				lfUnderline = font.lfUnderline;
				lfStrikeOut = font.lfStrikeOut;
				lfCharSet = font.lfCharSet;
				lfOutPrecision = font.lfOutPrecision;
				lfClipPrecision = font.lfClipPrecision;
				lfQuality = font.lfQuality;
				lfPitchAndFamily = font.lfPitchAndFamily;
				lfFaceName = font.lfFaceName;
			}
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public class LOGFONTW
		{
			public int lfHeight = 0;
			public int lfWidth = 0;
			public int lfEscapement = 0;
			public int lfOrientation = 0;
			public int lfWeight = 0;
			public byte lfItalic = 0;
			public byte lfUnderline = 0;
			public byte lfStrikeOut = 0;
			public byte lfCharSet = 0;
			public byte lfOutPrecision = 0;
			public byte lfClipPrecision = 0;
			public byte lfQuality = 0;
			public byte lfPitchAndFamily = 0;
			[System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = LF_FACESIZE /2)]
			public string lfFaceName = null;
			
			public LOGFONTW() { }
			public LOGFONTW(LOGFONTA font)
			{
				lfHeight = font.lfHeight;
				lfWidth = font.lfWidth;
				lfEscapement = font.lfEscapement;
				lfOrientation = font.lfOrientation;
				lfWeight = font.lfWeight;
				lfItalic = font.lfItalic;
				lfUnderline = font.lfUnderline;
				lfStrikeOut = font.lfStrikeOut;
				lfCharSet = font.lfCharSet;
				lfOutPrecision = font.lfOutPrecision;
				lfClipPrecision = font.lfClipPrecision;
				lfQuality = font.lfQuality;
				lfPitchAndFamily = font.lfPitchAndFamily;
				lfFaceName = font.lfFaceName;
			}
		}
		/*
		enum FontQuality
		{
			DEFAULT_QUALITY = 0x00,
			DRAFT_QUALITY = 0x01,
			PROOF_QUALITY = 0x02,
			NONANTIALIASED_QUALITY = 0x03,
			ANTIALIASED_QUALITY = 0x04,
			CLEARTYPE_QUALITY = 0x05
		}



		[Flags]
		enum ClipPrecision
		{
			CLIP_DEFAULT_PRECIS = 0x00000000,
			CLIP_CHARACTER_PRECIS = 0x00000001,
			CLIP_STROKE_PRECIS = 0x00000002,
			CLIP_LH_ANGLES = 0x00000010,
			CLIP_TT_ALWAYS = 0x00000020,
			CLIP_DFA_DISABLE = 0x00000040,
			CLIP_EMBEDDED = 0x00000080
		}

		enum OutPrecision
		{
			OUT_DEFAULT_PRECIS = 0x00000000,
			OUT_STRING_PRECIS = 0x00000001,
			OUT_STROKE_PRECIS = 0x00000003,
			OUT_TT_PRECIS = 0x00000004,
			OUT_DEVICE_PRECIS = 0x00000005,
			OUT_RASTER_PRECIS = 0x00000006,
			OUT_TT_ONLY_PRECIS = 0x00000007,
			OUT_OUTLINE_PRECIS = 0x00000008,
			OUT_SCREEN_OUTLINE_PRECIS = 0x00000009,
			OUT_PS_ONLY_PRECIS = 0x0000000A
		}

		[Flags]
		public enum LogFontPitchAndFamily
		{

		}

		enum CHARSET
		{
			ANSI_CHARSET = 0x00000000,
			DEFAULT_CHARSET = 0x00000001,
			SYMBOL_CHARSET = 0x00000002,
			MAC_CHARSET = 0x0000004D,
			SHIFTJIS_CHARSET = 0x00000080,
			HANGUL_CHARSET = 0x00000081,
			JOHAB_CHARSET = 0x00000082,
			GB2312_CHARSET = 0x00000086,
			CHINESEBIG5_CHARSET = 0x00000088,
			GREEK_CHARSET = 0x000000A1,
			TURKISH_CHARSET = 0x000000A2,
			VIETNAMESE_CHARSET = 0x000000A3,
			HEBREW_CHARSET = 0x000000B1,
			ARABIC_CHARSET = 0x000000B2,
			BALTIC_CHARSET = 0x000000BA,
			RUSSIAN_CHARSET = 0x000000CC,
			THAI_CHARSET = 0x000000DE,
			EASTEUROPE_CHARSET = 0x000000EE,
			OEM_CHARSET = 0x000000FF
		}
		*/
		#endregion

		#region классы типов аналогичные описынным в tiff anotation specification

		/// <summary>
		/// The type of the mark.
		/// </summary>
		public enum AnnotationMarkType : uint
		{
			ImageEmbedded = 1,
			ImageReference = 2,
			StraightLine = 3,
			FreehandLine = 4,
			HollowRectangle = 5,
			FilledRectangle = 6,
			TypedText = 7,
			TextFromFile = 8,
			TextStamp = 9,
			AttachNote = 10,
			Form = 12,
			OCRRegion = 13
		}

		public class OIAN_MARK_ATTRIBUTES
		{
			#region переменные и свойства самой структуры
			uint uType;	// тип, см. выше AnnotationMarkType

			/// <summary>
			/// Rectangle in FULLSIZE units; equivalent to type RECT. Can be a rectangle or two points.
			/// </summary>
			Rectangle lrBounds;
			/// <summary>
			/// The main color; for example, the color of all lines, all rectangles, and standalone text.
			/// </summary>
			Color rgbColor1;
			/// <summary>
			/// The secondary color; for example, the color of the text of
			/// an Attach-a-Note.
			/// </summary>
			Color rgbColor2;
			/// <summary>
			/// TRUE ? The mark is drawn highlighted. Highlighting
			/// performs the same function as a highlighting marker on a
			/// piece of paper. Valid only for lines, rectangles, and
			/// freehand.
			/// </summary>
			bool bHighlighting;
			/// <summary>
			/// TRUE ? The mark is drawn transparent. A transparent
			/// mark does not draw white pixels. That is, transparent
			/// replaces white pixels with whatever is behind those pixels.
			/// Available only for images.
			/// </summary>
			bool bTransparent;
			/// <summary>
			/// The width of the line in pixels.
			/// </summary>
			uint uLineSize;

			uint uReserved1;// Reserved; must be set to 0.
			uint uReserved2;// Reserved; must be set to 0.

			/// <summary>
			/// The font information for the text, consisting of standard
			// font attributes of font size, name, style, effects, and
			// background color.
			/// </summary>
			LOGFONTA lfFont;

			uint bReserved3;// Reserved; must be set to 0.

			/// <summary>
			/// The time that the mark was first saved, in seconds, from
			/// 00:00:00 1-1-1970 GMT. Every annotation mark has
			/// time as one of its attributes. If you do not set the time before
			/// the file is saved, the time is set to the date and time that the
			/// save was initiated. This time is in the form returned by the
			/// "time" C call, which is the number of seconds since
			/// midnight 00:00:00 on 1-1-1970 GMT. If necessary, refer
			/// to your C documentation for a more detailed description.
			/// </summary>
			long Time;

			/// <summary>
			/// TRUE ? The mark is currently set to be visible.
			// Annotation marks can be visible or hidden.
			/// </summary>
			bool bVisible;

			uint dwReserved4;// Reserved; must be set to 0x0FF83F.

			int[] lReserved;// Must be set to 0.

			System.Drawing.Text.TextRenderingHint fontRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;

			public System.Drawing.Text.TextRenderingHint FontRenderingHint
			{
				get { return fontRenderingHint; }
				set { fontRenderingHint = value; }
			}

			public AnnotationMarkType UType
			{
				get { return (AnnotationMarkType)uType; }
				set { uType = (uint)value; }
			}

			public Rectangle LrBounds
			{
				get { return lrBounds; }
				set { lrBounds = value; }
			}

			public Color RgbColor1
			{
				get { return rgbColor1; }
				set { rgbColor1 = value; }
			}

			public Color RgbColor2
			{
				get { return rgbColor2; }
				set { rgbColor2 = value; }
			}

			public bool BHighlighting
			{
				get { return bHighlighting; }
				set { bHighlighting = value; }
			}

			public bool BTransparent
			{
				get { return bTransparent; }
				set { bTransparent = value; }
			}

			public uint ULineSize
			{
				get { return uLineSize; }
				set { uLineSize = value; }
			}

			public uint UReserved1
			{
				get { return uReserved1; }
				set { uReserved1 = value; }
			}


			public uint UReserved2
			{
				get { return uReserved2; }
				set { uReserved2 = value; }
			}

			public LOGFONTA LfFont
			{
				get { return lfFont; }
				set { lfFont = value; }
			}

			public uint BReserved3
			{
				get { return bReserved3; }
				set { bReserved3 = value; }
			}

			public long Time1
			{
				get { return Time; }
				set { Time = value; }
			}

			public bool BVisible
			{
				get { return bVisible; }
				set { bVisible = value; }
			}

			public uint DwReserved4
			{
				get { return dwReserved4; }
				set { dwReserved4 = value; }
			}

			public int[] LReserved
			{
				get { return lReserved; }
				set { lReserved = value; }
			}
			#endregion
			string oiGroup;

			string oiIndex;

			AN_POINTS oiAnoDat_AN_POINTS;

			AN_NEW_ROTATE_STRUCT oiAnoDat_AN_NEW_ROTATE_STRUCT;

			HYPERLINK_NB oiHypLnk_HYPERLINK_NB;

			OIAN_TEXTPRIVDATA oiAnText_OIAN_TEXTPRIVDATA;

			AN_IMAGE_STRUCT oiDIB_AN_IMAGE_STRUCT;

			AN_NAME_STRUCT oiFilNam_AN_NAME_STRUCT;

			public string OiGroup
			{
				get { return oiGroup; }
				set { oiGroup = value; }
			}
			public string OiIndex
			{
				get { return oiIndex; }
				set { oiIndex = value; }
			}
			public AN_POINTS OiAnoDat_AN_POINTS
			{
				get { return oiAnoDat_AN_POINTS; }
				set { oiAnoDat_AN_POINTS = value; }
			}
			public AN_NEW_ROTATE_STRUCT OiAnoDat_AN_NEW_ROTATE_STRUCT
			{
				get { return oiAnoDat_AN_NEW_ROTATE_STRUCT; }
				set { oiAnoDat_AN_NEW_ROTATE_STRUCT = value; }
			}
			public HYPERLINK_NB OiHypLnk_HYPERLINK_NB
			{
				get { return oiHypLnk_HYPERLINK_NB; }
				set { oiHypLnk_HYPERLINK_NB = value; }
			}
			public OIAN_TEXTPRIVDATA OiAnText_OIAN_TEXTPRIVDATA
			{
				get { return oiAnText_OIAN_TEXTPRIVDATA; }
				set { oiAnText_OIAN_TEXTPRIVDATA = value; }
			}
			public AN_IMAGE_STRUCT OiDIB_AN_IMAGE_STRUCT
			{
				get { return oiDIB_AN_IMAGE_STRUCT; }
				set { oiDIB_AN_IMAGE_STRUCT = value; }
			}
			public AN_NAME_STRUCT OiFilNam_AN_NAME_STRUCT
			{
				get { return oiFilNam_AN_NAME_STRUCT; }
				set { oiFilNam_AN_NAME_STRUCT = value; }
			}
		}

		/// <summary>
		/// ѕоказан в OiAnoDat, используетс€ дл€ Straight Line, Freehand Line
		/// </summary>
		public class AN_POINTS
		{
			/// <summary>
			/// The maximum number of points; must 
			/// be equal to the value of nPoints.
			/// </summary>
			int nMaxPoints;


			/// <summary>
			/// The current number of points.
			/// </summary>
			int nPoints;

			/// <summary>
			/// Points marking the beginning and 
			/// ending of the line segment(s); in 
			/// FULLSIZE (not scaled) coordinates 
			/// relative to the upper left corner 
			/// of lrBounds in 
			/// OIAN_MARK_ATTRIBUTES.
			/// </summary>
			Point[] ptPoint;

			public int NPoints
			{
				get { return nPoints; }
				set { nPoints = value; }
			}

			public int NMaxPoints
			{
				get { return nMaxPoints; }
				set { nMaxPoints = value; }
			}

			public Point[] PtPoint
			{
				get { return ptPoint; }
				set { ptPoint = value; }
			}


		}

		/// <summary>
		/// ѕоказан в OiAnoDat, используетс€ дл€ Form, Image Embedded, Image Reference
		/// </summary>
		public class AN_NEW_ROTATE_STRUCT
		{
			/// <summary>
			/// 1=Original
			/// 2=Rotate right (90 degrees clockwise)
			/// 3=Flip (180 degrees clockwise)
			/// 4=Rotate left (270 degrees clockwise)
			/// 5=Vertical mirror (reflected around a 
			/// vertical line)
			/// 6=Vertical mirror + Rotate right
			/// 7=Vertical mirror + Flip
			/// 8=Vertical mirror + Rotate left
			/// </summary>
			int rotation;

			public int Rotation
			{
				get { return rotation; }
				set { rotation = value; }
			}

			/// <summary>
			///  Set to 1000.
			/// </summary>
			int scale;

			public int Scale
			{
				get { return scale; }
				set { scale = value; }
			}

			/// <summary>
			///  Set to value of nOrigHRes.
			/// </summary>
			int nHRes;

			public int NHRes
			{
				get { return nHRes; }
				set { nHRes = value; }
			}
			/// <summary>
			/// Set to value of nOrigVRes.
			/// </summary>
			int nVRes;

			public int NVRes
			{
				get { return nVRes; }
				set { nVRes = value; }
			}
			/// <summary>
			/// Resolution of image mark in DPI.
			/// </summary>
			int nOrigHRes;

			public int NOrigHRes
			{
				get { return nOrigHRes; }
				set { nOrigHRes = value; }
			}
			/// <summary>
			/// Resolution of image mark in DPI.
			/// </summary>
			int nOrigVRes;

			public int NOrigVRes
			{
				get { return nOrigVRes; }
				set { nOrigVRes = value; }
			}
			/// <summary>
			/// Set to 0.
			/// </summary>
			bool bReserved1;

			public bool BReserved1
			{
				get { return bReserved1; }
				set { bReserved1 = value; }
			}
			/// <summary>
			/// Set to 0.
			/// </summary>
			bool bReserved2;

			public bool BReserved2
			{
				get { return bReserved2; }
				set { bReserved2 = value; }
			}

			int[] nReserved;

			public int[] NReserved
			{
				get { return nReserved; }
				set { nReserved = value; }
			}

		}
		/// <summary>
		/// ¬ OiFilNam дл€ Form, Image Embedded, Image Reference
		/// </summary>
		public class AN_NAME_STRUCT
		{
			/// <summary>
			/// A character string designating the filename; terminated with a NULL.
			/// </summary>
			string name;

			public string Name
			{
				get { return name; }
				set { name = value; }
			}
		}

		/// <summary>
		/// OiDIB дл€ Image Embedded
		/// </summary>
		public class AN_IMAGE_STRUCT
		{
			/// <summary>
			/// Standard DIB.
			/// </summary>
			byte[] dibInfo;

			public byte[] DibInfo
			{
				get { return dibInfo; }
				set { dibInfo = value; }
			}
		}

		/// <summary>
		/// OiAnText дл€ Attach-a-Note, Typed Text, Text From File, Text Stamp, OCR region
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public class OIAN_TEXTPRIVDATA
		{
			/// <summary>
			/// Angle of text baseline to image in tenths of a degree; valid  values are 0, 900, 1800, 2700.
			/// </summary>
			[MarshalAs(UnmanagedType.I4)]
			int nCurrentOrientation;


			/// <summary>
			///  Always 1000 when writing; ignore when reading.
			/// </summary>
			[MarshalAs(UnmanagedType.U4)]
			uint uReserved1;



			/// <summary>
			/// Always 72000 divided by the vertical resolution of the  base image when writing;
			/// Used to modify the Attributes.lfFont.lfHeight variable for display. 
			/// </summary>
			[MarshalAs(UnmanagedType.U4)]
			uint uCreationScale;


			/// <summary>
			/// 64K byte limit (32K for multi- byte data) for Attach-a-Note, typed text, text from file; 255 byte limit for text stamp.
			/// </summary>
			[MarshalAs(UnmanagedType.U4)]
			uint uAnoTextLength;

			/// <summary>
			/// Text string for text mark types.
			/// </summary>
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64000)]
			string szAnoText;


			public uint UReserved1
			{
				get { return uReserved1; }
				set { uReserved1 = value; }
			}
			public uint UCreationScale
			{
				get { return uCreationScale; }
				set { uCreationScale = value; }
			}

			public int NCurrentOrientation
			{
				get { return nCurrentOrientation; }
				set { nCurrentOrientation = value; }
			}

			public uint UAnoTextLength
			{
				get { return uAnoTextLength; }
				set { uAnoTextLength = value; }
			}

			public string SzAnoText
			{
				get { return szAnoText; }
				set
				{
					szAnoText = value; //szAnoText+='\0';
				}
			}

		}

		/// <summary>
		/// OiHypLnk 
		/// </summary>
#pragma warning disable 169
		public class HYPERLINK_NB
		{
			/// <summary>
			/// The version number of this data.
			/// </summary>
			int nVersion;
			/// <summary>
			/// The size of the link string in bytes
			/// </summary>
			int nLinkSize;
			/// <summary>
			/// The variable length multi-byte name string.
			/// </summary>
			char szLinkString;

			/// <summary>
			/// The size of the location string.
			/// </summary>
			int nLocationSize;
			/// <summary>
			/// The variable length multi-byte location string.
			/// </summary>
			char szLocationString;

			/// <summary>
			/// The size of the working directory string.
			/// </summary>
			int nWorkDirSize;

			/// <summary>
			/// The variable length multi-byte working directory string.
			/// </summary>
			char szWorkDirString;

			/// <summary>
			/// One or more of the following flags
			/// 1 = Can remove hyperlink from mark.
			/// 2 = Hyperlink refers to this document.
			/// </summary>
			int nFlags;

		}
#pragma warning restore 169
		#endregion

		public void AnnotationRotate(bool isLeft, int fullWidth, int fullHeight)
		{

			ArrayList figuresList = GetFigures(false);
			foreach (object figure in figuresList)
			{
				TiffAnnotation.IBufferBitmap bb = figure as TiffAnnotation.IBufferBitmap;
				if (isLeft)
					bb.ChangeSize(new Rectangle(bb.Rect.Y, fullWidth - bb.Rect.X - bb.Rect.Width, bb.Rect.Height, bb.Rect.Width));
				else
					bb.ChangeSize(new Rectangle(fullHeight - bb.Rect.Y - bb.Rect.Height, bb.Rect.X, bb.Rect.Height, bb.Rect.Width));
				if (bb.Attributes.OiAnText_OIAN_TEXTPRIVDATA != null)
				{
					if (isLeft)
					{
						bb.Attributes.OiAnText_OIAN_TEXTPRIVDATA.NCurrentOrientation += 900;
						if (bb.Attributes.OiAnText_OIAN_TEXTPRIVDATA.NCurrentOrientation > 2700)
							bb.Attributes.OiAnText_OIAN_TEXTPRIVDATA.NCurrentOrientation = 0;
					}
					else
					{
						bb.Attributes.OiAnText_OIAN_TEXTPRIVDATA.NCurrentOrientation -= 900;
						if (bb.Attributes.OiAnText_OIAN_TEXTPRIVDATA.NCurrentOrientation < 0)
							bb.Attributes.OiAnText_OIAN_TEXTPRIVDATA.NCurrentOrientation = 2700;

					}
				}
				if (bb.Attributes.OiDIB_AN_IMAGE_STRUCT != null)
				{
					if (isLeft)
					{
						bb.Attributes.OiAnoDat_AN_NEW_ROTATE_STRUCT.Rotation--;
						if (bb.Attributes.OiAnoDat_AN_NEW_ROTATE_STRUCT.Rotation < 1)
							bb.Attributes.OiAnoDat_AN_NEW_ROTATE_STRUCT.Rotation = 4;
						((TiffAnnotation.ImageEmbedded)bb).Img.RotateFlip(RotateFlipType.Rotate270FlipNone);
					}
					else
					{
						bb.Attributes.OiAnoDat_AN_NEW_ROTATE_STRUCT.Rotation++;
						if (bb.Attributes.OiAnoDat_AN_NEW_ROTATE_STRUCT.Rotation > 4)
							bb.Attributes.OiAnoDat_AN_NEW_ROTATE_STRUCT.Rotation = 0;
						((TiffAnnotation.ImageEmbedded)bb).Img.RotateFlip(RotateFlipType.Rotate90FlipNone);
					}

				}
			}
		}

		private ArrayList figures = new ArrayList();

		public ArrayList GetFigures(bool rewrite)
		{
			if (!rewrite)
			{
				if (figures != null && figures.Count > 0)
					return figures;
			}

			figures = new ArrayList();
			foreach (OIAN_MARK_ATTRIBUTES attr in _attributes)
			{
				switch (attr.UType)
				{
					case AnnotationMarkType.ImageEmbedded:
						ImageEmbedded ie = new ImageEmbedded(attr, this);
						figures.Add(ie);
						break;
					case AnnotationMarkType.StraightLine:
						StraightLine sl = new StraightLine(attr.OiAnoDat_AN_POINTS, attr.RgbColor1, attr.BHighlighting, attr.ULineSize, attr.LrBounds);
						figures.Add(sl);
						break;
					case AnnotationMarkType.HollowRectangle:
						HollowRectangle hr = new HollowRectangle(attr, this);
						figures.Add(hr);
						break;
					case AnnotationMarkType.TypedText:
						TypedText tt = new TypedText(attr, this);
						figures.Add(tt);
						break;
					case AnnotationMarkType.FreehandLine:
						FreehandLine fl = new FreehandLine(attr.OiAnoDat_AN_POINTS, attr.RgbColor1, attr.BHighlighting, attr.ULineSize, attr.LrBounds);
						figures.Add(fl);
						break;
					case AnnotationMarkType.FilledRectangle:
						FilledRectangle fr = new FilledRectangle(attr, this);
						figures.Add(fr);
						break;
					case AnnotationMarkType.TextFromFile:
						TextFromFile tf = new TextFromFile(attr.RgbColor1, attr.LrBounds, attr.LfFont, attr.OiAnText_OIAN_TEXTPRIVDATA, attr.FontRenderingHint);
						figures.Add(tf);
						break;
					case AnnotationMarkType.TextStamp:
						TextStump ts = new TextStump(attr.RgbColor1, attr.LrBounds, attr.LfFont, attr.OiAnText_OIAN_TEXTPRIVDATA, attr.FontRenderingHint);
						figures.Add(ts);
						break;
					case AnnotationMarkType.AttachNote:
						AttachANote an = new AttachANote(attr, this);
						figures.Add(an);
						break;
				}
			}
			return figures;
		}

		private string GenerateOiIndex()
		{
			int max = 0;
			foreach (OIAN_MARK_ATTRIBUTES attr in MarkAttributes)
			{
				max = Math.Max(max, Convert.ToInt32(attr.OiIndex));
			}
			return (max + 1).ToString();

		}

		public FilledRectangle CreateFilledRectangle(Rectangle rect, string OiGroup)
		{
			OIAN_MARK_ATTRIBUTES attribute = new OIAN_MARK_ATTRIBUTES();
			attribute.UType = AnnotationMarkType.FilledRectangle;
			attribute.OiGroup = OiGroup == "" ? "[Ѕез имени]" : OiGroup;
			attribute.OiIndex = GenerateOiIndex();
			attribute.RgbColor1 = Registry.FILLED_RECT_TOOL_FILL_COLOR;//Color.FromArgb(255,255,0);
			attribute.RgbColor2 = Color.FromArgb(0, 0, 0);
			attribute.LrBounds = rect;
			attribute.LfFont = new LOGFONTA();
			attribute.BHighlighting = Registry.FILLED_RECT_TOOL_STYLE;//true;
			attribute.BVisible = true;
			attribute.BTransparent = false;
			attribute.UReserved1 = 0;
			attribute.UReserved2 = 0;
			attribute.DwReserved4 = 1046591;
			attribute.LReserved = new int[10];
			attribute.Time1 = 1235563376;
			MarkAttributes.Add(attribute);
			FilledRectangle fr = new FilledRectangle(attribute, this);
			figures.Add(fr);
			return fr;
		}

		public HollowRectangle CreateHollowRectangle(Rectangle rect, string OiGroup)
		{
			OIAN_MARK_ATTRIBUTES attribute = new OIAN_MARK_ATTRIBUTES();
			attribute.UType = AnnotationMarkType.HollowRectangle;
			attribute.OiGroup = OiGroup == "" ? "[Ѕез имени]" : OiGroup;
			attribute.OiIndex = GenerateOiIndex();
			attribute.RgbColor1 = Registry.HOLLOW_RECT_TOOL_LINE_COLOR;// Color.FromArgb(0, 0, 255);
			attribute.RgbColor2 = Color.FromArgb(0, 0, 0);
			attribute.ULineSize = Registry.HOLLOW_RECT_TOOL_LINE_WIDTH;// 4;
			attribute.LrBounds = rect;
			attribute.LfFont = new LOGFONTA();
			attribute.BHighlighting = Registry.HOLLOW_RECT_TOOL_STYLE;//true;
			attribute.BVisible = true;
			attribute.BTransparent = false;
			attribute.UReserved1 = 0;
			attribute.UReserved2 = 0;
			attribute.DwReserved4 = 1046591;
			attribute.LReserved = new int[10];
			attribute.Time1 = 1235563376;
			MarkAttributes.Add(attribute);
			HollowRectangle hr = new HollowRectangle(attribute, this);
			figures.Add(hr);
			return hr;
		}

		public TypedText CreateTypedText(Rectangle rect, string text, string OiGroup)
		{
			OIAN_MARK_ATTRIBUTES attribute = new OIAN_MARK_ATTRIBUTES();
			attribute.UType = AnnotationMarkType.TypedText;
			attribute.OiGroup = OiGroup == "" ? "[Ѕез имени]" : OiGroup;
			attribute.OiIndex = GenerateOiIndex();
			attribute.RgbColor1 = Registry.TEXT_TOOL_FONT_COLOR;//Color.FromArgb(0, 0, 0);
			attribute.RgbColor2 = Color.FromArgb(0, 0, 0);
			attribute.LrBounds = rect;
			attribute.BHighlighting = false;
			attribute.BVisible = true;
			attribute.BTransparent = false;
			attribute.UReserved1 = 0;
			attribute.UReserved2 = 0;
			attribute.DwReserved4 = 1046591;
			attribute.LReserved = new int[10];
			attribute.Time1 = 1235563376;

			OIAN_TEXTPRIVDATA tpd = new OIAN_TEXTPRIVDATA();
			tpd.NCurrentOrientation = 0;
			tpd.UAnoTextLength = Convert.ToUInt32(text.Length);
			tpd.UCreationScale = 360;
			tpd.UReserved1 = 1000;
			tpd.SzAnoText = text;
			attribute.OiAnText_OIAN_TEXTPRIVDATA = tpd;
			MarkAttributes.Add(attribute);
			TypedText tt = new TypedText(attribute, this);
			int sizeFont = Registry.TEXT_TOOL_FONT_SIZE;
			tt.LfFont = new Font(Registry.TEXT_TOOL_FONT_NAME, sizeFont, GraphicsUnit.World);

			figures.Add(tt);

			return tt;
		}

		public AttachANote CreateNote(Rectangle rect, string text, string OiGroup)
		{

			OIAN_MARK_ATTRIBUTES attribute = new OIAN_MARK_ATTRIBUTES();
			attribute.UType = AnnotationMarkType.AttachNote;
			attribute.OiGroup = OiGroup == "" ? "[Ѕез имени]" : OiGroup;
			attribute.OiIndex = GenerateOiIndex();
			attribute.RgbColor1 = Color.FromArgb(0, 0, 0);
			attribute.RgbColor2 = Color.FromArgb(0, 0, 0);
			attribute.LrBounds = rect;
			attribute.BHighlighting = false;
			attribute.BVisible = true;
			attribute.BTransparent = false;
			attribute.UReserved1 = 0;
			attribute.UReserved2 = 0;
			attribute.DwReserved4 = 1046591;
			attribute.LReserved = new int[10];
			attribute.Time1 = 1235563376;

			OIAN_TEXTPRIVDATA tpd = new OIAN_TEXTPRIVDATA();
			tpd.NCurrentOrientation = 0;
			tpd.UAnoTextLength = Convert.ToUInt32(text.Length);
			tpd.UCreationScale = 360;
			tpd.UReserved1 = 1000;
			tpd.SzAnoText = text;
			attribute.OiAnText_OIAN_TEXTPRIVDATA = tpd;
			MarkAttributes.Add(attribute);
			AttachANote tt = new AttachANote(attribute, this);

			tt.LfFont = new Font(Registry.ATTACH_A_NOTE_TOOL_FONT_NAME, Registry.ATTACH_A_NOTE_TOOL_FONT_SIZE, GraphicsUnit.World);

			tt.RgbColor1 = Registry.ATTACH_A_NOTE_TOOL_BACKCOLOR;// Color.GreenYellow;
			tt.RgbColor2 = Registry.ATTACH_A_NOTE_TOOL_FONT_COLOR;//Color.Black;
			figures.Add(tt);

			return tt;
		}

		public static readonly int LOGPIXELSY = 90;

		private static int devicePixel = 0;

		public static int GetDevicePixel()
		{
			if (devicePixel > 0)
				return devicePixel;
			using (Bitmap b = new Bitmap(1, 1))
			using (Graphics g = Graphics.FromImage(b))
			{
				IntPtr hdc = g.GetHdc();
				devicePixel = PrintImage.GetDeviceCaps(hdc, LOGPIXELSY);
				g.ReleaseHdc(hdc);
			}
			return devicePixel;
		}

		public static int GetSizeNetFontFromLogfontHeight(int s, uint UCreationScale)
		{
			int n = GetDevicePixel();
			int height = (int)Math.Round((s * (double)UCreationScale) / n);
			return height;
		}

		/// <summary>
		/// —оздание заметки-изображени€ (штампа).
		/// </summary>
		/// <param name="rect">область внутри документа, куда вписать изображение</param>
		/// <param name="image">изображение</param>
		/// <param name="OiGroup">группа заметок</param>
		/// <returns></returns>
		public ImageEmbedded CreateImage(Rectangle rect, Image image, string OiGroup)
		{
			byte[] dibBytes = null;

			// если изображение индексированное, его предварительно надо перевести в неиндексированное
			if ((image.PixelFormat & PixelFormat.Indexed) > 0)
				using (var imageStream = new MemoryStream())
				{
					if (image.PixelFormat == PixelFormat.Format1bppIndexed)
					{
						using (Image newImage = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb))
						{
							using (Graphics g = Graphics.FromImage(newImage))
								g.DrawImage(image, 0, 0, newImage.Width, newImage.Height);
							CopyDIB(newImage, ref dibBytes);
						}
					}
					else
					{
						image.Save(imageStream, ImageFormat.Png);
						using (var newImage = new Bitmap(imageStream, true))
							CopyDIB(newImage, ref dibBytes);
					}
				}
			else
				CopyDIB(image, ref dibBytes);

			var attribute = new OIAN_MARK_ATTRIBUTES()
			{
				UType = AnnotationMarkType.ImageEmbedded,
				OiGroup = OiGroup == "" ? "[Ѕез имени]" : OiGroup,
				OiIndex = GenerateOiIndex(),
				RgbColor1 = Color.FromArgb(0, 0, 0),
				RgbColor2 = Color.FromArgb(0, 0, 0),
				LrBounds = rect,
				LfFont = new LOGFONTA(),
				BHighlighting = false,
				BVisible = true,
				BTransparent = true,
				UReserved1 = 0,
				UReserved2 = 0,
				DwReserved4 = 1046591,
				LReserved = new int[10],
				Time1 = (long)((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds),
				OiDIB_AN_IMAGE_STRUCT = new AN_IMAGE_STRUCT() { DibInfo = dibBytes },
				OiAnoDat_AN_NEW_ROTATE_STRUCT = new AN_NEW_ROTATE_STRUCT()
				{
					BReserved1 = false,
					BReserved2 = false,
					NHRes = 200,
					NVRes = 200,
					NOrigHRes = (int)image.HorizontalResolution,
					NOrigVRes = (int)image.VerticalResolution,
					Rotation = 1,
					Scale = 1000,
					NReserved = new int[6]
				}
			};

			MarkAttributes.Add(attribute);

			var ie = new ImageEmbedded(attribute, this);
			figures.Add(ie);
			return ie;
		}

		/// <summary>
		///  опируем DIB в массив байтов.
		/// </summary>
		/// <param name="image">изображение</param>
		/// <param name="dibBytes">массив байтов</param>
		private void CopyDIB(Image image, ref byte[] dibBytes)
		{
			const int bitmapFileHeaderSize = 14;

			using (var imageStream = new MemoryStream())
			{
				image.Save(imageStream, ImageFormat.Bmp);
				imageStream.Seek(bitmapFileHeaderSize, SeekOrigin.Begin);
				dibBytes = new byte[(int)imageStream.Length - bitmapFileHeaderSize];
				imageStream.Read(dibBytes, 0, (int)imageStream.Length - bitmapFileHeaderSize);
			}
		}

		#region  лассы фигур аннотации, структуры дл€ тех фигур, которые могут только отрисоватьс€ без редактировани€ и сохранени€


		public class ModifyEventArgs : EventArgs
		{
			private Rectangle region;

			public Rectangle Region
			{
				get { return region; }
				set { region = value; }
			}

			public ModifyEventArgs(Rectangle region)
			{
				this.region = region;
			}
		}
		public delegate void ClearBitmapHandler(object sender, ModifyEventArgs args);
		/// <summary>
		/// —обытие дл€ перерисовки закешированных фигур
		/// </summary>
		public event ClearBitmapHandler ModifiedFigure;

		/// <summary>
		/// ƒиспетчер событи€ ModifiedFigure
		/// </summary>
		private void OnModifiedFigure(object sender, ModifyEventArgs args)
		{
			if (ModifiedFigure != null)
			{
				ModifiedFigure(sender, args);
			}
		}

		public interface IBufferBitmap
		{
			OIAN_MARK_ATTRIBUTES Attributes { get; }
			bool Selected { get; set; }
			Rectangle Rect { get; }
			Font TextFont { get; set; }
			Color Color { get; set; }
			int LineSize { get; set; }
			bool HighLighting { get; set; }
			Bitmap GetBitmap(Bitmap srcBitmap, InterpolationMode interpolation);
			Font GetZoomFont(double zoom);
			void ChangeSize(Rectangle rect);
			void ChangeText(String text);
			void ChangeFont(Font font, Color colorFont);
		}

		public class BaseFigure
		{
			protected Bitmap bitmap;
			private TiffAnnotation tiff = null;
			public BaseFigure(TiffAnnotation tiff)
			{
				this.tiff = tiff;
				tiff.ModifiedFigure += new ClearBitmapHandler(ClearBitmap);

			}
			public void ModifyEnd(Rectangle rect)
			{
				tiff.OnModifiedFigure(this, new ModifyEventArgs(rect));
			}
			public void ClearBitmap(object sender, ModifyEventArgs args)
			{
				if (sender != this)
				{
					Rectangle rect = ((IBufferBitmap)this).Rect;
					Rectangle region = args.Region;
					if (rect.X >= region.X && rect.X <= region.X + region.Width
						|| rect.Y >= region.Y && rect.Y <= region.Y + region.Height
						|| rect.Right >= region.X && rect.Right <= region.X + region.Width
						|| rect.Bottom >= region.Y && rect.Bottom <= region.Y + region.Height)
					{
						if (bitmap != null)
							bitmap.Dispose();
						bitmap = null;
					}

				}
			}
		}

		public class TypedText : BaseFigure, IBufferBitmap
		{
			protected OIAN_MARK_ATTRIBUTES attr;
			protected bool selected;
			private Font lfFont;
			private RectangleF lrBounds;


			public System.Drawing.Text.TextRenderingHint FontRenderingHint
			{
				get { return attr.FontRenderingHint; }
				set { attr.FontRenderingHint = value; }
			}

			public OIAN_TEXTPRIVDATA TextPrivateData
			{
				get { return attr.OiAnText_OIAN_TEXTPRIVDATA; }
				set { attr.OiAnText_OIAN_TEXTPRIVDATA = value; }
			}

			public Font LfFont
			{
				get { return lfFont; }
				set
				{
					LOGFONTW fontes = new LOGFONTW();
					value.ToLogFont(fontes);
					LOGFONTA lf = new LOGFONTA(fontes);
					lf.lfHeight = -fontes.lfHeight;
					attr.LfFont = lf;
					lfFont = value;
				}
			}

			public RectangleF LrBoundsF
			{
				get { return lrBounds; }
				set { lrBounds = value; }
			}
			public Rectangle LrBounds
			{
				get { return attr.LrBounds; }
				set { attr.LrBounds = value; }
			}
			public Color RgbColor1
			{
				get { return attr.RgbColor1; }
				set { attr.RgbColor1 = value; }
			}

			public TypedText(OIAN_MARK_ATTRIBUTES attr, TiffAnnotation tiff)
				: base(tiff)
			{
				this.attr = attr;
				this.lrBounds = new RectangleF(Convert.ToSingle(attr.LrBounds.X), Convert.ToSingle(attr.LrBounds.Y), Convert.ToSingle(attr.LrBounds.Width), Convert.ToSingle(attr.LrBounds.Height));
				if (attr.LfFont != null)
				{
					LOGFONTA lf = attr.LfFont;
					LOGFONTW fontes = new LOGFONTW(lf);
					fontes.lfHeight = -lf.lfHeight; //(int)Math.Round((attr.OiAnText_OIAN_TEXTPRIVDATA.UCreationScale * attr.LfFont.lfHeight) / (double) GetDevicePixel());
					this.lfFont = Font.FromLogFont(fontes);
					lfFont.ToLogFont(fontes);
				}
			}

			#region IBufferBitmap Members

			Bitmap IBufferBitmap.GetBitmap(Bitmap srcBitmap, InterpolationMode interpolation)
			{
				return null;
			}

			bool IBufferBitmap.Selected
			{
				get
				{
					return selected;
				}
				set
				{
					selected = value;
				}
			}

			Rectangle IBufferBitmap.Rect
			{
				get { return LrBounds; }
			}

			void IBufferBitmap.ChangeSize(Rectangle rect)
			{
				Rectangle oldRect = new Rectangle(LrBounds.Location, LrBounds.Size);
				LrBounds = rect;
				ModifyEnd(oldRect);
			}

			void IBufferBitmap.ChangeText(string text)
			{
				Rectangle oldRect = new Rectangle(LrBounds.Location, LrBounds.Size);
				TextPrivateData.SzAnoText = text;
				TextPrivateData.UAnoTextLength = Convert.ToUInt32(text.Length);
				ModifyEnd(oldRect);
			}


			OIAN_MARK_ATTRIBUTES IBufferBitmap.Attributes
			{
				get { return attr; }
			}

			Font IBufferBitmap.TextFont
			{
				get
				{
					return LfFont;
				}
				set
				{
					LfFont = value;
					ModifyEnd(LrBounds);
				}
			}

			Font IBufferBitmap.GetZoomFont(double zoom)
			{
				LOGFONTW lfont = new LOGFONTW();
				lfFont.ToLogFont(lfont);

				
				lfont.lfHeight = (int)Math.Round(lfont.lfHeight * zoom);

				lfont.lfFaceName = lfFont.Name;
				Font res = Font.FromLogFont(lfont);

				return res;
			}

			Color IBufferBitmap.Color
			{
				get
				{
					return attr.RgbColor1;
				}
				set
				{
					attr.RgbColor1 = value;
					if (attr.UType == AnnotationMarkType.AttachNote)
						Registry.ATTACH_A_NOTE_TOOL_BACKCOLOR = attr.RgbColor1;
					else if (attr.UType == AnnotationMarkType.TypedText)
						Registry.TEXT_TOOL_FONT_COLOR = attr.RgbColor1;
					ModifyEnd(LrBounds);
				}
			}

			int IBufferBitmap.LineSize
			{
				get
				{
					throw new NotImplementedException();
				}
				set
				{
					throw new NotImplementedException();
				}
			}


			bool IBufferBitmap.HighLighting
			{
				get
				{
					throw new NotImplementedException();
				}
				set
				{
					throw new NotImplementedException();
				}
			}


			void IBufferBitmap.ChangeFont(Font font, Color colorFont)
			{
				Rectangle oldRect = new Rectangle(LrBounds.Location, LrBounds.Size);
				Font fontNew = new Font(font.FontFamily, font.Size, font.Style, GraphicsUnit.World, font.GdiCharSet, font.GdiVerticalFont);
				LfFont = fontNew;


				if (attr.UType == AnnotationMarkType.AttachNote)
				{
					attr.RgbColor2 = colorFont;
					Registry.ATTACH_A_NOTE_TOOL_FONT_NAME = attr.LfFont.lfFaceName;
					Registry.ATTACH_A_NOTE_TOOL_FONT_SIZE = attr.LfFont.lfHeight;
					Registry.ATTACH_A_NOTE_TOOL_FONT_COLOR = attr.RgbColor2;
				}
				else if (attr.UType == AnnotationMarkType.TypedText)
				{
					attr.RgbColor1 = colorFont;
					Registry.TEXT_TOOL_FONT_NAME = attr.LfFont.lfFaceName;
					Registry.TEXT_TOOL_FONT_SIZE = attr.LfFont.lfHeight;
					Registry.TEXT_TOOL_FONT_COLOR = attr.RgbColor1;
				}
				ModifyEnd(oldRect);
			}

			#endregion
		}

		public class FilledRectangle : BaseFigure, IBufferBitmap
		{

			protected bool selected;

			public Rectangle LrBounds
			{
				get { return attr.LrBounds; }
				set { attr.LrBounds = value; }
			}

			public bool BHighlighting
			{
				get { return attr.BHighlighting; }
				set { attr.BHighlighting = value; }
			}
			public Color RgbColor1
			{
				get { return attr.RgbColor1; }
				set
				{
					attr.RgbColor1 = value;
				}
			}
			protected OIAN_MARK_ATTRIBUTES attr;
			public FilledRectangle(OIAN_MARK_ATTRIBUTES attr, TiffAnnotation tiff)
				: base(tiff)
			{
				this.attr = attr;
				if (this.bitmap != null)
					this.bitmap.Dispose();
				this.bitmap = null;
				this.selected = false;
			}

			protected virtual Bitmap GetHighlightingBitmap(Bitmap srcBitmap, InterpolationMode interpolation)
			{

				Bitmap bitmapUp = null;
				if (BHighlighting)
				{
					bitmapUp = new Bitmap(LrBounds.Size.Width, LrBounds.Size.Height);
					bitmapUp.SetResolution(srcBitmap.HorizontalResolution, srcBitmap.VerticalResolution);
					using (Graphics gr = Graphics.FromImage(bitmapUp))
					{
						gr.InterpolationMode = interpolation;
						float[][] ptsArray ={
												new float[] {(float)RgbColor1.R/255,  0,  0,  0,  0},
												new float[] {0,  (float)RgbColor1.G/255,  0,  0,  0},
												new float[] {0,  0,  (float)RgbColor1.B/255,  0,  0},
												new float[] {0,  0,  0,  1,  0},
												new float[] {0,  0,  0,  0,  1}};
						System.Drawing.Imaging.ColorMatrix clrMatrix = new System.Drawing.Imaging.ColorMatrix(ptsArray);
						System.Drawing.Imaging.ImageAttributes imgAttribs = new System.Drawing.Imaging.ImageAttributes();
						imgAttribs.SetColorMatrix(clrMatrix,
							System.Drawing.Imaging.ColorMatrixFlag.Default,
							System.Drawing.Imaging.ColorAdjustType.Default);
						gr.DrawImage(srcBitmap, new Rectangle(0, 0, LrBounds.Width, LrBounds.Height), LrBounds.X, LrBounds.Y, LrBounds.Width, LrBounds.Height, GraphicsUnit.Pixel, imgAttribs);
					}
				}
				else
				{
					bitmapUp = new Bitmap(LrBounds.Size.Width, LrBounds.Size.Height);
					bitmapUp.SetResolution(srcBitmap.HorizontalResolution, srcBitmap.VerticalResolution);
					using (Graphics gr = Graphics.FromImage(bitmapUp))
					{
						gr.InterpolationMode = interpolation;
						gr.FillRectangle(new SolidBrush(RgbColor1), new Rectangle(0, 0, LrBounds.Width, LrBounds.Height));
					}
				}

				return bitmapUp;
			}

			#region BufferBitmap Members

			Bitmap IBufferBitmap.GetBitmap(Bitmap srcBitmap, InterpolationMode interpolation)
			{
				if (bitmap == null)
					bitmap = GetHighlightingBitmap(srcBitmap, interpolation);
				return bitmap;
			}

			bool IBufferBitmap.Selected
			{
				get
				{
					return selected;
				}
				set
				{
					selected = value;
				}
			}

			Rectangle IBufferBitmap.Rect
			{
				get { return LrBounds; }
			}

			void IBufferBitmap.ChangeSize(Rectangle rect)
			{
				Rectangle oldRect = new Rectangle(LrBounds.Location, LrBounds.Size);
				if (bitmap != null)
					bitmap.Dispose();
				bitmap = null;
				LrBounds = rect;
				ModifyEnd(oldRect);
			}

			void IBufferBitmap.ChangeText(string text)
			{
				throw new NotImplementedException();
			}


			OIAN_MARK_ATTRIBUTES IBufferBitmap.Attributes
			{
				get { return attr; }
			}

			Font IBufferBitmap.TextFont
			{
				get
				{
					throw new NotImplementedException();
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			Font IBufferBitmap.GetZoomFont(double zoom)
			{
				throw new NotImplementedException();
			}


			Color IBufferBitmap.Color
			{
				get
				{
					return attr.RgbColor1;
				}
				set
				{
					Rectangle oldRect = new Rectangle(LrBounds.Location, LrBounds.Size);
					if (bitmap != null)
						bitmap.Dispose();
					bitmap = null;
					attr.RgbColor1 = value;
					Registry.FILLED_RECT_TOOL_FILL_COLOR = attr.RgbColor1;
					ModifyEnd(oldRect);
				}
			}

			int IBufferBitmap.LineSize
			{
				get
				{
					throw new NotImplementedException();
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			bool IBufferBitmap.HighLighting
			{
				get
				{
					return attr.BHighlighting;
				}
				set
				{
					Rectangle oldRect = new Rectangle(LrBounds.Location, LrBounds.Size);
					if (bitmap != null)
						bitmap.Dispose();
					bitmap = null;
					attr.BHighlighting = value;
					Registry.FILLED_RECT_TOOL_STYLE = attr.BHighlighting;
					ModifyEnd(oldRect);
				}
			}

			void IBufferBitmap.ChangeFont(Font font, Color colorFont)
			{
				throw new NotImplementedException();
			}

			#endregion
		}

		public class HollowRectangle : FilledRectangle, IBufferBitmap
		{
			public uint ULineSize
			{
				get { return attr.ULineSize; }
				set { attr.ULineSize = value; }
			}
			public HollowRectangle(OIAN_MARK_ATTRIBUTES attr, TiffAnnotation tiff)
				: base(attr, tiff)
			{
			}

			protected override Bitmap GetHighlightingBitmap(Bitmap srcBitmap, InterpolationMode interpolation)
			{
				Bitmap bitmapUp = new Bitmap(LrBounds.Size.Width, LrBounds.Size.Height);
				bitmapUp.SetResolution(srcBitmap.HorizontalResolution, srcBitmap.VerticalResolution);
				using (Graphics gr = Graphics.FromImage(bitmapUp))
				{
					gr.InterpolationMode = interpolation;
					if (BHighlighting)
					{
						float[][] ptsArray ={
												new float[] {(float)RgbColor1.R/255,  0,  0,  0,  0},
												new float[] {0,  (float)RgbColor1.G/255,  0,  0,  0},
												new float[] {0,  0,  (float)RgbColor1.B/255,  0,  0},
												new float[] {0,  0,  0,  1,  0},
												new float[] {0,  0,  0,  0,  1}};
						System.Drawing.Imaging.ColorMatrix clrMatrix = new System.Drawing.Imaging.ColorMatrix(ptsArray);
						System.Drawing.Imaging.ImageAttributes imgAttribs = new System.Drawing.Imaging.ImageAttributes();
						imgAttribs.SetColorMatrix(clrMatrix,
							System.Drawing.Imaging.ColorMatrixFlag.Default,
							System.Drawing.Imaging.ColorAdjustType.Default);
						gr.DrawImage(srcBitmap, new Rectangle(0, 0, LrBounds.Width, LrBounds.Height), LrBounds.X, LrBounds.Y, LrBounds.Width, LrBounds.Height, GraphicsUnit.Pixel, imgAttribs);
						gr.DrawImage(srcBitmap, new Rectangle((int)ULineSize, (int)ULineSize, LrBounds.Width - ((int)ULineSize * 2), LrBounds.Height - ((int)ULineSize * 2)), LrBounds.X + ULineSize, LrBounds.Y + ULineSize, LrBounds.Width - (ULineSize << 1), LrBounds.Height - (ULineSize << 1), GraphicsUnit.Pixel);
					}
					else
					{
						gr.DrawRectangle(new Pen(new SolidBrush(RgbColor1), ULineSize), new Rectangle(0, 0, LrBounds.Width, LrBounds.Height));
					}
				}
				return bitmapUp;
			}

			#region BufferBitmap Members

			Bitmap IBufferBitmap.GetBitmap(Bitmap srcBitmap, InterpolationMode interpolation)
			{
				if (bitmap == null)
					bitmap = GetHighlightingBitmap(srcBitmap, interpolation);
				return bitmap;
			}

			bool IBufferBitmap.Selected
			{
				get
				{
					return selected;
				}
				set
				{
					selected = value;
				}
			}

			Rectangle IBufferBitmap.Rect
			{
				get { return LrBounds; }
			}

			void IBufferBitmap.ChangeSize(Rectangle rect)
			{
				Rectangle oldRect = new Rectangle(LrBounds.Location, LrBounds.Size);
				if (bitmap != null)
					bitmap.Dispose();
				bitmap = null;
				LrBounds = rect;
				ModifyEnd(oldRect);
			}

			void IBufferBitmap.ChangeText(string text)
			{
				throw new NotImplementedException();
			}

			OIAN_MARK_ATTRIBUTES IBufferBitmap.Attributes
			{
				get { return attr; }
			}

			Font IBufferBitmap.TextFont
			{
				get
				{
					throw new NotImplementedException();
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			Font IBufferBitmap.GetZoomFont(double zoom)
			{
				throw new NotImplementedException();
			}

			Color IBufferBitmap.Color
			{
				get
				{
					return attr.RgbColor1;
				}
				set
				{
					Rectangle oldRect = new Rectangle(LrBounds.Location, LrBounds.Size);
					if (bitmap != null)
						bitmap.Dispose();
					bitmap = null;
					attr.RgbColor1 = value;
					Registry.HOLLOW_RECT_TOOL_LINE_COLOR = attr.RgbColor1;
					ModifyEnd(oldRect);
				}
			}

			int IBufferBitmap.LineSize
			{
				get
				{
					return Convert.ToInt32(attr.ULineSize);
				}
				set
				{
					Rectangle oldRect = new Rectangle(LrBounds.Location, LrBounds.Size);
					if (bitmap != null)
						bitmap.Dispose();
					bitmap = null;
					attr.ULineSize = Convert.ToUInt32(value);
					Registry.HOLLOW_RECT_TOOL_LINE_WIDTH = attr.ULineSize;
					ModifyEnd(oldRect);
				}
			}

			bool IBufferBitmap.HighLighting
			{
				get
				{
					return attr.BHighlighting;
				}
				set
				{
					Rectangle oldRect = new Rectangle(LrBounds.Location, LrBounds.Size);
					if (bitmap != null)
						bitmap.Dispose();
					bitmap = null;
					attr.BHighlighting = value;
					Registry.HOLLOW_RECT_TOOL_STYLE = attr.BHighlighting;
					ModifyEnd(oldRect);
				}
			}

			void IBufferBitmap.ChangeFont(Font font, Color colorFont)
			{
				throw new NotImplementedException();
			}

			#endregion
		}


		public struct StraightLine
		{
			Point[] linePoints;
			AN_POINTS points;
			Color rgbColor1;
			bool bHighlighting;
			uint uLineSize;
			Rectangle lrBounds;

			public Point[] LinePoints
			{
				get { return linePoints; }
				set { linePoints = value; }
			}
			public Rectangle LrBounds
			{
				get { return lrBounds; }
				set { lrBounds = value; }
			}

			public uint ULineSize
			{
				get { return uLineSize; }
				set { uLineSize = value; }
			}
			public bool BHighlighting
			{
				get { return bHighlighting; }
				set { bHighlighting = value; }
			}
			public Color RgbColor1
			{
				get { return rgbColor1; }
				set { rgbColor1 = value; }
			}
			public AN_POINTS Points
			{
				get { return points; }
				set { points = value; }
			}
			public StraightLine(AN_POINTS points, Color rgbColor1, bool bHighlighting, uint uLineSize, Rectangle lrBounds)
			{
				this.points = points;
				this.rgbColor1 = rgbColor1;
				this.bHighlighting = bHighlighting;
				this.uLineSize = uLineSize;
				this.lrBounds = lrBounds;
				linePoints = null;
				if (points.NPoints == 2)
				{
					linePoints = new Point[2];
					linePoints[0] = new Point(lrBounds.X + points.PtPoint[0].X, lrBounds.Y + points.PtPoint[0].Y);
					linePoints[1] = new Point(lrBounds.X + points.PtPoint[1].X, lrBounds.Y + points.PtPoint[1].Y);
				}
			}
		}

		public struct FreehandLine
		{
			Point[] linePoints;
			AN_POINTS points;
			Color rgbColor1;
			bool bHighlighting;
			uint uLineSize;
			Rectangle lrBounds;

			public Point[] LinePoints
			{
				get { return linePoints; }
				set { linePoints = value; }
			}
			public Rectangle LrBounds
			{
				get { return lrBounds; }
				set { lrBounds = value; }
			}

			public uint ULineSize
			{
				get { return uLineSize; }
				set { uLineSize = value; }
			}
			public bool BHighlighting
			{
				get { return bHighlighting; }
				set { bHighlighting = value; }
			}
			public Color RgbColor1
			{
				get { return rgbColor1; }
				set { rgbColor1 = value; }
			}
			public AN_POINTS Points
			{
				get { return points; }
				set { points = value; }
			}
			public FreehandLine(AN_POINTS points, Color rgbColor1, bool bHighlighting, uint uLineSize, Rectangle lrBounds)
			{
				this.points = points;
				this.rgbColor1 = rgbColor1;
				this.bHighlighting = bHighlighting;
				this.uLineSize = uLineSize;
				this.lrBounds = lrBounds;

				linePoints = new Point[points.NPoints];
				for (int i = 0; i < points.NPoints; i++)
				{
					linePoints[i] = new Point(lrBounds.X + points.PtPoint[i].X, lrBounds.Y + points.PtPoint[i].Y);
				}

			}
		}

		public class ImageEmbedded : BaseFigure, IBufferBitmap
		{

			protected OIAN_MARK_ATTRIBUTES attr;
			protected bool selected;

			public Image Img { get; set; }

			public Rectangle LrBounds
			{
				get { return attr.LrBounds; }
				set { attr.LrBounds = value; }
			}

			public AN_IMAGE_STRUCT ImageStruct
			{
				get { return attr.OiDIB_AN_IMAGE_STRUCT; }
				set { attr.OiDIB_AN_IMAGE_STRUCT = value; }
			}

			public AN_NAME_STRUCT NameStruct
			{
				get { return attr.OiFilNam_AN_NAME_STRUCT; }
				set { attr.OiFilNam_AN_NAME_STRUCT = value; }
			}

			public AN_NEW_ROTATE_STRUCT RotateStruct
			{
				get { return attr.OiAnoDat_AN_NEW_ROTATE_STRUCT; }
				set { attr.OiAnoDat_AN_NEW_ROTATE_STRUCT = value; }
			}

			public ImageEmbedded(OIAN_MARK_ATTRIBUTES attr, TiffAnnotation tiff)
				: base(tiff)
			{
				this.attr = attr;

				System.Runtime.InteropServices.GCHandle hand = new GCHandle();
				try
				{
					hand = GCHandle.Alloc(ImageStruct.DibInfo, System.Runtime.InteropServices.GCHandleType.Pinned);
					IntPtr pDib = hand.AddrOfPinnedObject();

					Bitmap bm = TwainLib.DibToImage.WithScan0(pDib);

					if (RotateStruct.Rotation == 2)
						bm.RotateFlip(RotateFlipType.Rotate90FlipNone);
					else if (RotateStruct.Rotation == 3)
						bm.RotateFlip(RotateFlipType.Rotate180FlipNone);
					else if (RotateStruct.Rotation == 4)
						bm.RotateFlip(RotateFlipType.Rotate270FlipNone);
					else if (RotateStruct.Rotation == 5)
						bm.RotateFlip(RotateFlipType.Rotate180FlipY);
					else if (RotateStruct.Rotation == 6)
						bm.RotateFlip(RotateFlipType.Rotate90FlipY);
					else if (RotateStruct.Rotation == 7)
						bm.RotateFlip(RotateFlipType.Rotate180FlipX);
					else if (RotateStruct.Rotation == 8)
						bm.RotateFlip(RotateFlipType.Rotate270FlipY);

					bm.MakeTransparent();
					Img = bm;
				}
				finally
				{
					hand.Free();
				}

			}

			#region IBufferBitmap Members

			OIAN_MARK_ATTRIBUTES IBufferBitmap.Attributes
			{
				get { return attr; }
			}

			bool IBufferBitmap.Selected
			{
				get
				{
					return selected;
				}
				set
				{
					selected = value;
				}
			}

			Rectangle IBufferBitmap.Rect
			{
				get { return LrBounds; }
			}

			Font IBufferBitmap.TextFont
			{
				get
				{
					throw new NotImplementedException();
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			Bitmap IBufferBitmap.GetBitmap(Bitmap srcBitmap, InterpolationMode interpolation)
			{
				throw new NotImplementedException();
			}

			Font IBufferBitmap.GetZoomFont(double zoom)
			{
				throw new NotImplementedException();
			}

			void IBufferBitmap.ChangeSize(Rectangle rect)
			{
				Rectangle oldRect = new Rectangle(LrBounds.Location, LrBounds.Size);
				LrBounds = rect;
				ModifyEnd(oldRect);
			}

			void IBufferBitmap.ChangeText(string text)
			{
				throw new NotImplementedException();
			}

			Color IBufferBitmap.Color
			{
				get
				{
					throw new NotImplementedException();
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			int IBufferBitmap.LineSize
			{
				get
				{
					throw new NotImplementedException();
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			bool IBufferBitmap.HighLighting
			{
				get
				{
					throw new NotImplementedException();
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			void IBufferBitmap.ChangeFont(Font font, Color colorFont)
			{
				throw new NotImplementedException();
			}

			#endregion
		}


		public class AttachANote : TypedText
		{
			public Color RgbColor2
			{
				get { return attr.RgbColor2; }
				set
				{
					attr.RgbColor2 = value;
					Registry.ATTACH_A_NOTE_TOOL_FONT_COLOR = attr.RgbColor2;
					ModifyEnd(LrBounds);
				}
			}
			public AttachANote(OIAN_MARK_ATTRIBUTES attr, TiffAnnotation tiff)
				: base(attr, tiff)
			{
			}

		}

		public struct TextFromFile
		{
			Font lfFont;
			Color rgbColor1;
			RectangleF lrBounds;
			OIAN_TEXTPRIVDATA textPrivateData;
			System.Drawing.Text.TextRenderingHint fontRenderingHint;

			public System.Drawing.Text.TextRenderingHint FontRenderingHint
			{
				get { return fontRenderingHint; }
				set { fontRenderingHint = value; }
			}

			public OIAN_TEXTPRIVDATA TextPrivateData
			{
				get { return textPrivateData; }
				set { textPrivateData = value; }
			}

			public Font LfFont
			{
				get { return lfFont; }
				set { lfFont = value; }
			}

			public RectangleF LrBounds
			{
				get { return lrBounds; }
				set { lrBounds = value; }
			}

			public Color RgbColor1
			{
				get { return rgbColor1; }
				set { rgbColor1 = value; }
			}

			public TextFromFile(Color rgbColor1, Rectangle lrBounds, LOGFONTA lfFont, OIAN_TEXTPRIVDATA textPrivateData, System.Drawing.Text.TextRenderingHint fontRenderingHint)
			{
				this.rgbColor1 = rgbColor1;
				this.lrBounds = new RectangleF(Convert.ToSingle(lrBounds.X), Convert.ToSingle(lrBounds.Y), Convert.ToSingle(lrBounds.Width), Convert.ToSingle(lrBounds.Height));
				LOGFONTW lf = new LOGFONTW(lfFont);
				lf.lfHeight = -lfFont.lfHeight;
				this.lfFont = Font.FromLogFont(lf);
				this.textPrivateData = textPrivateData;
				this.fontRenderingHint = fontRenderingHint;
			}
		}

		public struct TextStump
		{
			Font lfFont;
			Color rgbColor1;
			RectangleF lrBounds;
			OIAN_TEXTPRIVDATA textPrivateData;
			System.Drawing.Text.TextRenderingHint fontRenderingHint;

			public System.Drawing.Text.TextRenderingHint FontRenderingHint
			{
				get { return fontRenderingHint; }
				set { fontRenderingHint = value; }
			}

			public OIAN_TEXTPRIVDATA TextPrivateData
			{
				get { return textPrivateData; }
				set { textPrivateData = value; }
			}

			public Font LfFont
			{
				get { return lfFont; }
				set { lfFont = value; }
			}

			public RectangleF LrBounds
			{
				get { return lrBounds; }
				set { lrBounds = value; }
			}

			public Color RgbColor1
			{
				get { return rgbColor1; }
				set { rgbColor1 = value; }
			}

			public TextStump(Color rgbColor1, Rectangle lrBounds, LOGFONTA lfFont, OIAN_TEXTPRIVDATA textPrivateData, System.Drawing.Text.TextRenderingHint fontRenderingHint)
			{
				this.rgbColor1 = rgbColor1;
				this.lrBounds = new RectangleF(Convert.ToSingle(lrBounds.X), Convert.ToSingle(lrBounds.Y), Convert.ToSingle(lrBounds.Width), Convert.ToSingle(lrBounds.Height));
				LOGFONTW lf = new LOGFONTW(lfFont);
				lf.lfHeight = -lfFont.lfHeight;
				this.lfFont = Font.FromLogFont(lf);
				this.textPrivateData = textPrivateData;
				this.fontRenderingHint = fontRenderingHint;
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			this.parent = null;
			if (menu != null)
			{
				menu.Dispose();
				menu = null;
			}
			if (itemProperties != null)
			{
				itemProperties.Dispose();
				itemProperties = null;
			}
			if (_attributes != null)
			{
				_attributes.Clear();
				_attributes = null;
			}
		}

		#endregion
	}

	public class TiffCursors
	{
		//без маски
		//static string strMarker = "AAACAAEAICAAAAQAHgAwAQAAFgAAACgAAAAgAAAAQAAAAAEAAQAAAAAAgAAAAAAAAAAAAAAAAgAAAAIAAAAAAAAA////AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA///////////n////+f////Z////7v///+9////0v///+9/////v////9/////v////1////+v////1////+v////1////uv///91////uv///91////uv///91////uv///91////vv///9z////k////+////////////////8=";
		static string strHand = "AAACAAEAEBAAAAgACACwAAAAFgAAACgAAAAQAAAAIAAAAAEAAQAAAAAAQAAAAAAAAAAAAAAAAgAAAAIAAAAAAAAA////AAPwAAAD8AAAB/AAAA/4AAAf+AAAP/wAAH/8AAB3/gAAZ/4AAAf+AAANtgAADbYAAB2wAAAZsAAAAYAAAAAAAAD4BwAA+AcAAPAHAADgAwAAwAMAAIABAAAAAQAAAAAAAAAAAACQAAAA4AAAAOAAAADAAQAAwAcAAOQPAAD+fwAA";
		static string strHandDrag = "AAACAAEAEBAAAAgACACwAAAAFgAAACgAAAAQAAAAIAAAAAEAAQAAAAAAQAAAAAAAAAAAAAAAAgAAAAIAAAAAAAAA////AAPwAAAD8AAAB/AAAA/4AAAf+AAAH/wAAB/8AAAX/AAAB/wAAAf8AAAFtAAABbAAAAAAAAAAAAAAAAAAAAAAAAD4BwAA+AcAAPAHAADgAwAAwAMAAMABAADAAQAAwAEAAOABAADwAQAA8AEAAPADAAD6TwAA//8AAP//AAD//wAA";
		//c маской
		static string strMarker = "AAACAAEAICAAAAQAHgAwAQAAFgAAACgAAAAgAAAAQAAAAAEAAQAAAAAAgAAAAAAAAAAAAAAAAgAAAAIAAAAAAAAA////AAAAAAAAAAAAAAAAAAgAAAAGAAAAA4AAAAPAAAABIAAAAPAAAAD4AAAAfAAAAD4AAAAdAAAADoAAAAdAAAADoAAAAdAAAADoAAAAdAAAADoAAAAdAAAADoAAAAdAAAADoAAAAdAAAAD4AAAAcAAAABAAAAAAAAAAAAAAAAAAAAAA///////////n////+f////Z////7v///+9////0v///+9/////v////9/////v////1////+v////1////+v////1////uv///91////uv///91////uv///91////uv///91////vv///9z////k////+////////////////8=";
		static string strHRect = "AAACAAEAICAAAAgACADoAgAAFgAAACgAAAAgAAAAQAAAAAEABAAAAAAAAAIAAAAAAAAAAAAAEAAAABAAAAAAAAAAAACAAACAAAAAgIAAgAAAAIAAgACAgAAAgICAAMDAwAAAAP8AAP8AAAD//wD/AAAA/wD/AP//AAD///8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAA//////////8AAAAAAAAAAP//////////AAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD///////wAAf/9//3//f/9//3//f/9//3//f/9//3//f/9//3//f/9//3//f/8AAH//////////////////////////////////////////////////////////////////////////////////////////////////////////w==";
		static string strFRect = "AAACAAEAICAAAAgACADoAgAAFgAAACgAAAAgAAAAQAAAAAEABAAAAAAAAAIAAAAAAAAAAAAAEAAAABAAAAAAAAAAAACAAACAAAAAgIAAgAAAAIAAgACAgAAAgICAAMDAwAAAAP8AAP8AAAD//wD/AAAA/wD/AP//AAD///8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAA//////////8AAAAAAAAAAP//////////AAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD///////wAAf/8AAH//AAB//wAAf/8AAH//AAB//wAAf/8AAH//AAB//wAAf/8AAH//////////////////////////////////////////////////////////////////////////////////////////////////////////w==";
		static string strText = "AAACAAEAICAAAAgACADoAgAAFgAAACgAAAAgAAAAQAAAAAEABAAAAAAAAAIAAAAAAAAAAAAAEAAAABAAAAAAAAAAAACAAACAAAAAgIAAgAAAAIAAgACAgAAAgICAAMDAwAAAAP8AAP8AAAD//wD/AAAA/wD/AP//AAD///8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAA//////////8AAAAAAAAAAP//////////AAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD///////PkB//z5PP/8+T5//gM+f/5zPn/+czz//ycB//8nP///Jz///48///+PAP//////////////////////////////////////////////////////////////////////////////////////////////////////////w==";
		static string strNote = "AAACAAEAICAAAAgACADoAgAAFgAAACgAAAAgAAAAQAAAAAEABAAAAAAAAAIAAAAAAAAAAAAAEAAAABAAAAAAAAAAAACAAACAAAAAgIAAgAAAAIAAgACAgAAAgICAAMDAwAAAAP8AAP8AAAD//wD/AAAA/wD/AP//AAD///8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAA//////////8AAAAAAAAAAP//////////AAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAD/AAAAAAAAAAAAAAAAAAAA/wAAAAAAAAAAAAAAAAAAAP8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD///////+AAf//v/3//7/9//+//f//v/3//7/9//+//f//v/3//7/9//+//f//v/3//7/9//+/4f//v+v//7/n//+AD////////////////////////////////////////////////////////////////////////////////w==";
		static string strStump = "AAACAAEAICAAAA8AGgDoAgAAFgAAACgAAAAgAAAAQAAAAAEABAAAAAAAAAIAAAAAAAAAAAAAEAAAABAAAAAAAAAAAACAAACAAAAAgIAAgAAAAIAAgACAgAAAwMDAAICAgAAAAP8AAP8AAAD//wD/AAAA/wD/AP//AAD///8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAREREREREREREQAAAAAAAAEA8ADwAPAA8AEAAAAAAAABDw8PDw8PDw8BAAAAAAAAAfAA8ADwAPAA8QAAAAAAAAEPDw8PDw8PDwEAAAAAAAABAPAA8ADwAPABAAAAAAAAAQ8PDw8PDw8PAQAAAAAAAAHwAPAA8ADwAPEAAAAAAAAADw8PDw8PDw8AAAAAAAAAAAERERERERERAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD////////////////////////////////+AAA//gAAP/4AAD/+AAA//gAAP/4AAD/+AAA//gAAP/+AAP//gAD///gP///4D///+A////gP///8H////B///4AA//8AAH/+AAA//gAAP/4AAD//AAB//////////////////////w==";
		static string strRotateUL = "AAACAAEAICAAAA8ADwAwAQAAFgAAACgAAAAgAAAAQAAAAAEAAQAAAAAAgAAAAAAAAAAAAAAAAgAAAAIAAAAAAAAA////AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHgAAACIAAABOAAAAkAAAASAAAAFAAAACQAAAAoAAEAKAADACgADwAoADkAKADhACQBwQAUADEAEgBNAAkAkwAEwyEAAjxAAAGBgAAAfgAAAAAAAAAAAAAAAAAAAAAAAAAAAA///////////////////////////////////////h////wf///4H///8P///+H////j////w////8f//v/H//z/x//w/8f/wP/H/wD/w/4A/+P/wP/h/4D/8P8M//g8Hv/8AD///gB///+B////////////////////////////8=";
		static string strRotateDR = "AAACAAEAICAAAA8ADwAwAQAAFgAAACgAAAAgAAAAQAAAAAEAAQAAAAAAgAAAAAAAAAAAAAAAAgAAAAIAAAAAAAAA////AAAAAAAAAAAAAAAAAAAAAAAAD/gAADAGAABv+wAA0AWAAaACwANAAWACgACgAQAAUAAAAFAAAABQAAAAUAAAAFAAAABQAAAAUAAAAFAAAADQAAAAoAAAIWAAADLAAAAtgAAAJwAAACIAAAAhAAAAP4AAAAAAAAAAAAAAAAAAAAAA///////////////////////wB///wAH//4AA//8P+H/+H/w//D/+H/x//x/+//+P////j////4////+P////j////4////+P////j////w////8f///eH///zD///8B////A////wf///8D////Af/////////////////////8=";

		public static Cursor Hand;
		public static Cursor HandDrag;
		public static Cursor Marker;
		public static Cursor HRect;
		public static Cursor FRect;
		public static Cursor RectText;
		public static Cursor Note;
		public static Cursor Stamp;
		public static Cursor RotateUL;
		public static Cursor RotateDR;

		static TiffCursors()
		{
			//OpenFileDialog openFileDialog1 = new OpenFileDialog();
			//openFileDialog1.Filter = "Cursor Files|*.cur";
			//openFileDialog1.Title = "Select a Cursor File";
			//openFileDialog1.ShowDialog();

			//if (openFileDialog1.FileName != "")
			//{
			//    Stream str = openFileDialog1.OpenFile();
			//    byte[] b = new byte[str.Length];

			//    str.Read(b, 0, b.Length);
			//    //short hsx = BitConverter.ToInt16(b, 10);
			//    //short hsy = BitConverter.ToInt16(b, 12);
			//    //дл€ маркера
			//    //short hsx = 4;
			//    //short hsy = 30;

			//    short hsx = 8;
			//    short hsy = 8;
			//    //дл€ ректанглов
			//    //short hsx = 8;
			//    //short hsy = 8;
			//    //дл€ штампа
			//    //short hsx = 15;
			//    //short hsy = 26;
			//    //измен€ем hotspot
			//    b[10] = Convert.ToByte(hsx & 0x00FF);
			//    b[11] = Convert.ToByte(hsx >> 16);
			//    b[12] = Convert.ToByte(hsy & 0x00FF);
			//    b[13] = Convert.ToByte(hsy >> 16);
			//    string s = Convert.ToBase64String(b);

			//    HandDrag = new Cursor(str);

			//}
			Hand = new Cursor(GetImage(strHand));
			HandDrag = new Cursor(GetImage(strHandDrag));
			Marker = new Cursor(GetImage(strMarker));
			HRect = new Cursor(GetImage(strHRect));
			FRect = new Cursor(GetImage(strFRect));
			Note = new Cursor(GetImage(strNote));
			Stamp = new Cursor(GetImage(strStump));
			RectText = new Cursor(GetImage(strText));
			RotateUL = new Cursor(GetImage(strRotateUL));
			RotateDR = new Cursor(GetImage(strRotateDR));
		}

		private static Stream GetImage(string str)
		{
			Stream ms = null;
			try
			{

				byte[] b = Convert.FromBase64String(str);

				ms = new MemoryStream(b);
				ms.Position = 0;

			}
			finally
			{
				//ms.Flush();
				//ms.Close();
			}
			return ms;
		}
	}

	public class RectanglesPropertiesDialog : Form
	{
		bool isLine = false;
		bool isColor = true;
		bool isHighlite = false;
		bool isFont = false;
		bool isGroup = false;

		private TiffAnnotation.IBufferBitmap _figure;
		private Control _parent;

		private CheckBox chTransparent;
		private Button fontBn;
		private Button bnOk;

		public RectanglesPropertiesDialog(TiffAnnotation.IBufferBitmap figure, Control parent)
		{
			_figure = figure;
			_parent = parent;

			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			Width = 290;

			switch (figure.Attributes.UType)
			{
				case TiffAnnotation.AnnotationMarkType.FilledRectangle:
					isHighlite = true;
					Text = ResourcesManager.StringResources.GetString("MarkerSettings");
					break;

				case TiffAnnotation.AnnotationMarkType.HollowRectangle:
					isLine = true;
					isHighlite = true;
					Text = ResourcesManager.StringResources.GetString("EmptyRectProperties");
					break;

				case TiffAnnotation.AnnotationMarkType.TypedText:
				case TiffAnnotation.AnnotationMarkType.AttachNote:
					isFont = true;
					Text = ResourcesManager.StringResources.GetString("NoteProperties");
					break;

				case TiffAnnotation.AnnotationMarkType.ImageEmbedded:
					isColor = false;
					isGroup = true;
					Text = ResourcesManager.StringResources.GetString("ImageEmbededProperties");
					break;
			}

			SetStyle(ControlStyles.DoubleBuffer, true);

			int y = 0;
			if (isGroup)
			{
				Controls.Add(new Label()
					{
						AutoSize = true,
						Location = new Point(10, y + 10),
						Text = ResourcesManager.StringResources.GetString("ImageEmbededMake") +
							figure.Attributes.OiGroup + ", " +
							new DateTime(1970, 1, 1).AddSeconds(figure.Attributes.Time1).ToLocalTime()
					});
				y += 35;
			}

			if (isLine)
			{
				Controls.Add(new Label()
					{
						Location = new Point(10, y + 10),
						Text = ResourcesManager.StringResources.GetString("Width")
					});
				y += 35;

				Controls.Add(new PictureBox()
					{
						Size = new Size(80, 20),
						Location = new Point(20, y)
					});
				RedrawLine(figure.Color, figure.LineSize);

				NumericUpDown numeric = new NumericUpDown()
					{
						Size = new Size(40, 20),
						Location = new Point(110, y),
						Minimum = 1,
						Maximum = 10,
						DecimalPlaces = 0,
						Value = figure.LineSize
					};
				numeric.ValueChanged += new EventHandler(numeric_ValueChanged);
				Controls.Add(numeric);
				y += 20;
			}

			if (isColor)
			{
				Controls.Add(new Label()
					{
						Location = new Point(10, y + 10),
						Text = ResourcesManager.StringResources.GetString("FigureColor") + ":"
					});
				y += 15;

				Button palitraBn = new Button()
					{
						Location = new Point(Width - 90, y + 20),
						Text = ResourcesManager.StringResources.GetString("Palette") + ":"
					};
				palitraBn.Click += new EventHandler(palitraBn_Click);
				Controls.Add(palitraBn);

				if (isFont)
				{
					fontBn = new Button();
					fontBn.Text = ResourcesManager.StringResources.GetString("Font");
					fontBn.Location = new Point(Width - 90, y + 48);
					fontBn.Click += fontBn_Click;
					Controls.Add(fontBn);
				}

				for (int i = 0; i < 16; i++)
				{
					PictureBox pic = new PictureBox();
					pic.Size = new Size(20, 20);
					pic.Name = (i + 1).ToString();
					Bitmap bitmap = new Bitmap(20, 20);

					using (Graphics g = Graphics.FromImage(bitmap))
					{
						g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
						g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
						Color color = NameToColor(pic.Name);
						g.FillRectangle(new SolidBrush(color), new Rectangle(0, 0, 20, 20));
						if (figure.Color.ToArgb() == color.ToArgb())
						{
							Color sColor = Color.FromArgb(color.R > 128 ? 0 : 255,
								color.G > 128 ? 0 : 255, 0 > 128 ? 0 : 255);
							g.DrawRectangle(new Pen(sColor, 2), new Rectangle(0, 0, 20, 20));
							SelectedPictureBox = pic;
						}
						else
							g.DrawRectangle(Pens.Black, new Rectangle(0, 0, 20, 20));

					}
					pic.Image = bitmap;
					int x = i / 8 == 0 ? 20 + 20 * i + 2 * i : 20 + 20 * (i - 8) + 2 * (i - 8);
					pic.Location = new Point(x, y + (i / 8 == 0 ? 20 : 48));
					pic.MouseDown += pic_MouseDown;
					Controls.Add(pic);
				}
				y += 82;
			}
			if (isHighlite)
			{
				chTransparent = new CheckBox()
				{
					Location = new Point(10, y),
					Text = ResourcesManager.StringResources.GetString("Transparent"),
					Checked = figure.HighLighting
				};
				chTransparent.CheckedChanged += chTransparent_CheckedChanged;
				Controls.Add(chTransparent);
				y += 25;
			}
			Height = y + 60;
			bnOk = new Button();
			bnOk.Text = ResourcesManager.StringResources.GetString("Ok");
			bnOk.Location = new Point(Width - 190, y);
			bnOk.Click += bnOk_Click;
			Controls.Add(bnOk);
			Button bnCancel = new Button();
			bnCancel.Text = ResourcesManager.StringResources.GetString("Cancel");
			bnCancel.Location = new Point(Width - 90, y);
			bnCancel.Click += bnCancel_Click;
			Controls.Add(bnCancel);
			this.CancelButton = bnCancel;
		}

		void fontBn_Click(object sender, EventArgs e)
		{
			using (FontDialog d = new FontDialog())
			{
				d.ShowColor = true;
				if (_figure.Attributes.UType == TiffAnnotation.AnnotationMarkType.TypedText)
					d.Color = _figure.Attributes.RgbColor1;
				else
					d.Color = _figure.Attributes.RgbColor2;
				d.AllowVectorFonts = true;
				d.AllowVerticalFonts = true;
				d.ShowApply = true;

				d.AllowSimulations = true;
				Font fontNew = new Font(_figure.TextFont.FontFamily, _figure.TextFont.Size, _figure.TextFont.Style, GraphicsUnit.Point, _figure.TextFont.GdiCharSet, _figure.TextFont.GdiVerticalFont);

				d.Font = fontNew;


				if (d.ShowDialog() == DialogResult.OK)
				{
					SelectedColorFont = d.Color;
					SelectedFont = d.Font;
					if (_figure.Attributes.UType == TiffAnnotation.AnnotationMarkType.TypedText)
					{
						bnOk.PerformClick();
					}
				}
				else if (_figure.Attributes.UType == TiffAnnotation.AnnotationMarkType.TypedText)
				{
					Close();
				}
			}
		}
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if (isFont && _figure.Attributes.UType == TiffAnnotation.AnnotationMarkType.TypedText)
			{
				fontBn.PerformClick();
			}

		}
		void chTransparent_CheckedChanged(object sender, EventArgs e)
		{
			SelectedTransparent = true;
		}

		void bnCancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		void numeric_ValueChanged(object sender, EventArgs e)
		{
			SelectedLineSize = Convert.ToInt32(((NumericUpDown)sender).Value);
			if (SelectedColor != Color.Empty)
				RedrawLine(SelectedColor, SelectedLineSize);
			else
				RedrawLine(_figure.Color, SelectedLineSize);
		}

		void bnOk_Click(object sender, EventArgs e)
		{
			if (SelectedColor != Color.Empty)
				_figure.Color = SelectedColor;
			if (SelectedLineSize != 0)
				_figure.LineSize = SelectedLineSize;
			if (SelectedTransparent)
				_figure.HighLighting = chTransparent.Checked;
			if (SelectedFont != null)
				if (_figure.Attributes.UType == TiffAnnotation.AnnotationMarkType.TypedText)
				{
					_figure.ChangeFont(SelectedFont, Color.FromArgb(0, 0, 0, 0));
					_figure.Color = SelectedColorFont;
				}
				else
					_figure.ChangeFont(SelectedFont, SelectedColorFont);
			_parent.Invalidate();
			Close();
		}

		void palitraBn_Click(object sender, EventArgs e)
		{
			if (isColor)
			{
				using (ColorDialog dialog = new ColorDialog())
				{
					dialog.Color = SelectedColor;
					if (dialog.ShowDialog() == DialogResult.OK)
					{
						SelectedColor = dialog.Color;
						if (isLine)
							if (SelectedLineSize != 0)
								RedrawLine(SelectedColor, SelectedLineSize);
							else
								RedrawLine(SelectedColor, _figure.LineSize);
						else
							if (this.SelectedPictureBox != null)
								RedrawPictureBox(this.SelectedPictureBox, true, true);
					}
				}
			}
		}

		private void RedrawLine(Color color, int lineSize)
		{
			Bitmap bitmap = new Bitmap(80, 20);
			using (Graphics g = Graphics.FromImage(bitmap))
			{
				g.DrawRectangle(new Pen(Color.Black, 2), new Rectangle(0, 0, 80, 20));
				g.DrawLine(new Pen(color, lineSize), 5, 10, 70, 10);
			}
			//pictureLine.Image = bitmap;
		}
		private void RedrawPictureBox(PictureBox pic, bool selected, bool currentColor)
		{
			if (pic != null)
			{
				Bitmap bitmap = new Bitmap(20, 20);

				using (Graphics g = Graphics.FromImage(bitmap))
				{
					g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
					g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
					Color color = Color.Empty;
					if (currentColor && SelectedColor != Color.Empty)
					{
						color = SelectedColor;
						pic.Name = SelectedColor.Name;
					}
					else
						color = NameToColor(pic.Name);
					g.FillRectangle(new SolidBrush(color), new Rectangle(0, 0, 20, 20));
					if (!selected)
						g.DrawRectangle(Pens.Black, new Rectangle(0, 0, 20, 20));
					else
					{
						Color sColor = Color.FromArgb(color.R > 128 ? 0 : 255,
							color.G > 128 ? 0 : 255, 0 > 128 ? 0 : 255);
						g.DrawRectangle(new Pen(sColor, 2), new Rectangle(0, 0, 20, 20));
					}

				}
				pic.Image = bitmap;

			}
		}

		private PictureBox SelectedPictureBox;
		private Color SelectedColor = Color.Empty;
		private bool SelectedTransparent = false;
		private Color SelectedColorFont = Color.Empty;
		private Font SelectedFont = null;
		private int SelectedLineSize = 0;
		void pic_MouseDown(object sender, MouseEventArgs e)
		{
			RedrawPictureBox(SelectedPictureBox, false, false);
			SelectedPictureBox = (PictureBox)sender;
			RedrawPictureBox(SelectedPictureBox, true, false);
			SelectedColor = NameToColor(((PictureBox)sender).Name);
			if (isLine)
				if (SelectedLineSize != 0)
					RedrawLine(SelectedColor, SelectedLineSize);
				else
					RedrawLine(SelectedColor, _figure.LineSize);
		}

		private Color NameToColor(string name)
		{
			switch (name)
			{
				case "1":
				default:
					return Color.White;
				case "2":
					return Color.LightGray;
				case "3":
					return Color.Blue;
				case "4":
					return Color.Cyan;
				case "5":
					return Color.FromArgb(0, 255, 0);
				case "6":
					return Color.Yellow;
				case "7":
					return Color.Red;
				case "8":
					return Color.Magenta;
				case "9":
					return Color.Black;
				case "10":
					return Color.FromArgb(128, 128, 128);
				case "11":
					return Color.FromArgb(0, 64, 128);
				case "12":
					return Color.FromArgb(0, 128, 128);
				case "13":
					return Color.FromArgb(0, 128, 64);
				case "14":
					return Color.FromArgb(128, 128, 64);
				case "15":
					return Color.FromArgb(128, 0, 64);
				case "16":
					return Color.FromArgb(128, 0, 128);
			}
		}
	}
}