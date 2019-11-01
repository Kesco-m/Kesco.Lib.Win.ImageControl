using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Kesco.Lib.Win.ImageControl
{
	/// <summary>
	/// Рисование заметок
	/// </summary>
	public class RenderAnnotations
	{
		private int WidthSelectedtRect;

		public RenderAnnotations(int widthSelectedtRect)
		{
			this.WidthSelectedtRect = widthSelectedtRect;
		}

		/// <summary>
		/// Рисование выделения для заметки
		/// </summary>
		public void DrawSelectedRectangle(Graphics g, Rectangle rect, ref Rectangle[] SelectedRectangles)
		{
			int indent = WidthSelectedtRect >> 1;
			SelectedRectangles = new Rectangle[8] { new Rectangle(rect.X - indent, rect.Y - indent, WidthSelectedtRect, WidthSelectedtRect),
													  new Rectangle(rect.X + (rect.Width >> 1) - indent, rect.Y - indent, WidthSelectedtRect, WidthSelectedtRect),
													  new Rectangle(rect.X + rect.Width - indent, rect.Y - indent, WidthSelectedtRect, WidthSelectedtRect),
													  new Rectangle(rect.X + rect.Width - indent, rect.Y + (rect.Height >> 1) - indent, WidthSelectedtRect, WidthSelectedtRect),
													  new Rectangle(rect.X + rect.Width - indent, rect.Y + rect.Height - indent, WidthSelectedtRect, WidthSelectedtRect),
													  new Rectangle(rect.X + (rect.Width >> 1) - indent, rect.Y + rect.Height - indent, WidthSelectedtRect, WidthSelectedtRect),
													  new Rectangle(rect.X - indent, rect.Y + rect.Height - indent, WidthSelectedtRect, WidthSelectedtRect),
													  new Rectangle(rect.X - indent, rect.Y + (rect.Height >> 1) - indent, WidthSelectedtRect, WidthSelectedtRect)};
			g.FillRectangle(Brushes.Black, SelectedRectangles[0]);
			g.FillRectangle(Brushes.Black, SelectedRectangles[1]);
			g.FillRectangle(Brushes.Black, SelectedRectangles[2]);
			g.FillRectangle(Brushes.Black, SelectedRectangles[3]);
			g.FillRectangle(Brushes.Black, SelectedRectangles[4]);
			g.FillRectangle(Brushes.Black, SelectedRectangles[5]);
			g.FillRectangle(Brushes.Black, SelectedRectangles[6]);
			g.FillRectangle(Brushes.Black, SelectedRectangles[7]);
		}

		public virtual void DrawAnnotation(Graphics g, object tiffAnnotation, Hashtable markGroupsVisibleList, Bitmap bitmapImg, InterpolationMode CurrentInterpolationMode, ref Rectangle[] SelectedRectangles, System.Collections.Specialized.ListDictionary notesToSelectedRectangles)
		{
			if (notesToSelectedRectangles != null)
				notesToSelectedRectangles.Clear();
			TiffAnnotation annotation = tiffAnnotation as TiffAnnotation;
			if (annotation != null)
			{
				ArrayList figuresList = annotation.GetFigures(false);
				foreach (object figure in figuresList)
				{
					TiffAnnotation.IBufferBitmap bb = figure as TiffAnnotation.IBufferBitmap;
					if (bb != null && markGroupsVisibleList.ContainsKey(bb.Attributes.OiGroup) && ((bool)markGroupsVisibleList[bb.Attributes.OiGroup]))
					{
						switch (figure.GetType().Name)
						{
							case "ImageEmbedded":
								TiffAnnotation.ImageEmbedded img = (TiffAnnotation.ImageEmbedded)figure;
								g.DrawImage(img.Img, img.LrBounds.Location.X, img.LrBounds.Location.Y, img.LrBounds.Size.Width, img.LrBounds.Size.Height);
								if (bb.Selected)
									DrawSelectedRectangle(g, bb.Rect, ref SelectedRectangles);
								break;
							case "StraightLine":
								TiffAnnotation.StraightLine line = (TiffAnnotation.StraightLine)figure;
								if (line.LinePoints == null)
									continue;
								g.DrawLine(new Pen(new SolidBrush(line.RgbColor1), Convert.ToSingle(line.ULineSize)), line.LinePoints[0], line.LinePoints[1]);
								break;
							case "FreehandLine":
								TiffAnnotation.FreehandLine fline = (TiffAnnotation.FreehandLine)figure;
								if (fline.LinePoints == null)
									continue;
								for (int i = 0; i < fline.LinePoints.Length; i += 2)
								{
									if (i != 0)
										g.DrawLine(new Pen(new SolidBrush(fline.RgbColor1), Convert.ToSingle(fline.ULineSize)), fline.LinePoints[i - 1], fline.LinePoints[i]);
									g.DrawLine(new Pen(new SolidBrush(fline.RgbColor1), Convert.ToSingle(fline.ULineSize)), fline.LinePoints[i], fline.LinePoints[i + 1]);
								}
								break;
							case "HollowRectangle":
								{
									TiffAnnotation.HollowRectangle rect = (TiffAnnotation.HollowRectangle)figure;
									Bitmap bitmapUp = bb.GetBitmap(bitmapImg, CurrentInterpolationMode);
									g.DrawImage(bitmapUp, rect.LrBounds.X, rect.LrBounds.Y);
									if (bb.Selected)
										DrawSelectedRectangle(g, rect.LrBounds, ref SelectedRectangles);
								}
								break;
							case "FilledRectangle":
								{
									TiffAnnotation.FilledRectangle frect = (TiffAnnotation.FilledRectangle)figure;
									Bitmap bitmapUp = bb.GetBitmap(bitmapImg, CurrentInterpolationMode);
									g.DrawImage(bitmapUp, frect.LrBounds.X, frect.LrBounds.Y);
									if (bb.Selected)
										DrawSelectedRectangle(g, frect.LrBounds, ref SelectedRectangles);
								}
								break;
							case "TypedText":
								TiffAnnotation.TypedText tt = (TiffAnnotation.TypedText)figure;
								StringFormat sf = new StringFormat();
								System.Drawing.Drawing2D.Matrix mx = null;
								Rectangle newRect = tt.LrBounds;
								switch (tt.TextPrivateData.NCurrentOrientation)
								{
									case 900:
										mx = new System.Drawing.Drawing2D.Matrix();
										newRect = new Rectangle(tt.LrBounds.X, tt.LrBounds.Y + tt.LrBounds.Height, tt.LrBounds.Height, tt.LrBounds.Width);
										mx.RotateAt(270, new PointF(newRect.X, newRect.Y));
										g.Transform = mx;
										break;
									case 1800:
										mx = new System.Drawing.Drawing2D.Matrix();
										newRect = tt.LrBounds;
										mx.RotateAt(180, new PointF(tt.LrBounds.Location.X + tt.LrBounds.Width / 2, tt.LrBounds.Location.Y + tt.LrBounds.Height / 2));
										g.Transform = mx;
										break;
									case 2700:
										mx = new System.Drawing.Drawing2D.Matrix();
										newRect = new Rectangle(tt.LrBounds.X + tt.LrBounds.Width, tt.LrBounds.Y, tt.LrBounds.Height, tt.LrBounds.Width);
										mx.RotateAt(90, new PointF(newRect.X, newRect.Y));
										g.Transform = mx;

										break;

								}

								g.TextRenderingHint = tt.FontRenderingHint;
								sf.Trimming = StringTrimming.Word;
								using (Font f = new Font(tt.LfFont.FontFamily, tt.LfFont.SizeInPoints * g.DpiY / (float)TiffAnnotation.GetDevicePixel(), tt.LfFont.Style))
								g.DrawString(tt.TextPrivateData.SzAnoText, f, new SolidBrush(tt.RgbColor1), newRect, sf);

								g.ResetTransform();
								g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
								if (bb.Selected)
								{
									DrawSelectedRectangle(g, bb.Rect, ref SelectedRectangles);
									Pen p = new Pen(Brushes.Black);
									p.DashStyle = DashStyle.Dash;
									g.DrawRectangle(p, new Rectangle(bb.Rect.X - 1, bb.Rect.Y - 1, bb.Rect.Width + 2, bb.Rect.Height + 2));
								}
								break;
							case "TextStump":

								TiffAnnotation.TextStump ts = (TiffAnnotation.TextStump)figure;
								StringFormat sf3 = new StringFormat();
								g.TextRenderingHint = ts.FontRenderingHint;

								g.DrawString(ts.TextPrivateData.SzAnoText, ts.LfFont, new SolidBrush(ts.RgbColor1), ts.LrBounds, sf3);
								g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
								break;
							case "TextFromFile":
								TiffAnnotation.TextFromFile tf = (TiffAnnotation.TextFromFile)figure;
								StringFormat sf2 = new StringFormat();
								g.TextRenderingHint = tf.FontRenderingHint;

								g.DrawString(tf.TextPrivateData.SzAnoText, tf.LfFont, new SolidBrush(tf.RgbColor1), tf.LrBounds, sf2);
								g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
								break;
							case "AttachANote":
								TiffAnnotation.AttachANote an = (TiffAnnotation.AttachANote)figure;
								StringFormat sf1 = new StringFormat();

								g.TextRenderingHint = an.FontRenderingHint;

								g.FillRectangle(Brushes.Black, an.LrBounds.X + 2, an.LrBounds.Y + 2, an.LrBounds.Width, an.LrBounds.Height);
								g.FillRectangle(new SolidBrush(an.RgbColor1), an.LrBounds);
								g.DrawRectangle(Pens.Black, an.LrBounds.X, an.LrBounds.Y, an.LrBounds.Width, an.LrBounds.Height);

								System.Drawing.Drawing2D.Matrix mx1 = null;
								Rectangle newRect1 = an.LrBounds;
								switch (an.TextPrivateData.NCurrentOrientation)
								{
									case 900:
										mx1 = new System.Drawing.Drawing2D.Matrix();
										newRect1 = new Rectangle(an.LrBounds.X, an.LrBounds.Y + an.LrBounds.Height, an.LrBounds.Height, an.LrBounds.Width);
										mx1.RotateAt(270, new PointF(newRect1.X, newRect1.Y));
										g.Transform = mx1;
										break;
									case 1800:
										mx1 = new System.Drawing.Drawing2D.Matrix();
										newRect1 = an.LrBounds;
										mx1.RotateAt(180, new PointF(an.LrBounds.Location.X + an.LrBounds.Width / 2, an.LrBounds.Location.Y + an.LrBounds.Height / 2));
										g.Transform = mx1;
										break;
									case 2700:
										mx1 = new System.Drawing.Drawing2D.Matrix();
										newRect1 = new Rectangle(an.LrBounds.X + an.LrBounds.Width, an.LrBounds.Y, an.LrBounds.Height, an.LrBounds.Width);
										mx1.RotateAt(90, new PointF(newRect1.X, newRect1.Y));
										g.Transform = mx1;

										break;

								}
								g.DrawString(an.TextPrivateData.SzAnoText, new Font(an.LfFont.FontFamily, an.LfFont.SizeInPoints * g.DpiY /(float)TiffAnnotation.GetDevicePixel(), an.LfFont.Style) , new SolidBrush(an.RgbColor2), newRect1, sf1);
								g.ResetTransform();
								if (bb.Selected)
								{
									DrawSelectedRectangle(g, bb.Rect, ref SelectedRectangles);
								}
								g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
								break;
						}
						if (notesToSelectedRectangles != null && SelectedRectangles != null && SelectedRectangles.Length > 0 && bb.Selected)
							notesToSelectedRectangles.Add(bb, SelectedRectangles);
					}
				}
			}
		}

		public virtual void Clear() { }
	}
}
