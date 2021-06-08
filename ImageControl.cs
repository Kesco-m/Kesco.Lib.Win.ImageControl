using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Kesco.Lib.Win.Tiff;

namespace Kesco.Lib.Win.ImageControl
{
	public class ImageControl : Control
	{
		#region Константы
		private const int DEFAULT_WIDTH_SPLITTER = 3;
		private const int DEFAULT_HEIGHT_SPLITTER = 3;
		/// <summary>
		/// Включение автозагрузчика страниц
		/// </summary>
		private const bool IS_ENABLE_AUTOLOAD_PAGE = false;
		/// <summary>
		/// Максимальное количество страниц загружаемых сразу при открытии файл
		/// </summary>
		private const int COUNT_SYNC_LOAD_PAGE = 2;
		/// <summary>
		/// отступ в рядах
		/// </summary>
		private const int INDENT_BETWEEN_PREVIEW = 30;
		/// <summary>
		/// отступ от одной превьюшки до другой в сторону скрола
		/// </summary>
		private const int INDENT_THUMBNAIL_IMAGE = 40;
		private const int DEFAULT_HEIGHT_THUMBNAIL_PANEL = 180;
		private const int DEFAULT_WIDTH_THUMBNAIL_PANEL = 140;

		public const int MaxZoom = 6400;
		public const int MinZoom = 2;

		/// <summary>
		/// Ширина и высота выделения для фигуры
		/// </summary>
		protected const int WidthSelectedtRect = 8;

		private const bool SHOW_FILE_CHANGED_MESSAGE = true;

		protected float dpi = 96f;
		protected float ppi = 96f;

		#endregion

		#region	Переменные превьшек
		Bitmap ThumbnailImagesBitmap = null;
		/// <summary>
		/// Ориентация панели превьюшек
		/// </summary>
		private TypeThumbnailPanelOrientation thumbnailPanelOrientation = TypeThumbnailPanelOrientation.Left;
		/// <summary>
		/// Размер превьюшки для обсчетов
		/// </summary>
		int sizePreview = 0;
		/// <summary>
		/// Область превью
		/// </summary>
		protected Rectangle rectThumbnailPanel = new Rectangle();
		/// <summary>
		/// Показывать панель страниц или нет
		/// </summary>
		private bool showThumbPanel = true;
		/// <summary>
		/// По умлочание значению ширины страниц
		/// </summary>
		private int defaultWidthThumbnailImage;
		private int userWidthThumbnailImage;
		/// <summary>
		/// По умлочание значению ширины страниц
		/// </summary>
		private int defaultHeightThumbnailImage;
		private int userHeightThumbnailImage;
		/// <summary>
		/// Центрировать выбранную превьюшку
		/// </summary>
		private bool isToAlignPreview = true;

		private FileInfo fileInfo = null;
		private DateTime filesData;
		private long filesLenght;
		/// <summary>
		/// Информация о файле
		/// </summary>
		private FileInfo fi
		{
			get { return fileInfo; }
			set
			{
				fileInfo = value;
				if(value != null)
				{
					filesData = value.LastWriteTime;
					filesLenght = value.Length;
				}
				else
				{
					filesLenght = -1;
				}
			}
		}
		/// <summary>
		/// Кеш для хранения рисунка контрола
		/// </summary>
		private Bitmap fullCahedBitmap = null;
		/// <summary>
		/// толщина рамки(учтено не везде)
		/// </summary>
		protected int thicknessFrame = 1;
		/// <summary>
		/// Текущий индекс выбранной страницы
		/// </summary>
		private int SelectedIndex = -1;
		/// <summary>
		/// ширина превьюшек
		/// </summary>
		private int widthThumbnailImage = 0;
		/// <summary>
		/// высота преывьюшек
		/// </summary>
		private int heightThumbnailImage = 0;
		/// <summary>
		/// ширина области для рисования картинки и скрола
		/// </summary>
		private int widthImage = 0;
		/// <summary>
		/// высота области для рисования картинки и скрола
		/// </summary>
		private int heightImage = 0;
		/// <summary>
		/// ширина области для рисования картинки
		/// </summary>
		protected int realWidthImage = 0;
		/// <summary>
		/// высота области для рисования картинки
		/// </summary>
		protected int realHeightImage = 0;
		/// <summary>
		/// индекс первой видимой превюшки
		/// </summary>
		private int beginThumbnailImageIndex = 0;
		/// <summary>
		/// общее количество страниц
		/// </summary>
		private int countPreview;
		/// <summary>
		/// максимальный размер со всеми првеьюшками
		/// </summary>
		private int maxThumbnailSize;
		/// <summary>
		/// максимальное количество видимых превьюшек
		/// </summary>
		private int maxCountVisibleThumbnailImages;
		/// <summary>
		/// смещение скрола превьюшек глобальное соответствует скролу
		/// </summary>
		private int globalOffsetYThumbnail = 0;
		/// <summary>
		/// перерисовывать ли меню превьюшек
		/// </summary>
		private bool isLockThumbnailImages = false;
		/// <summary>
		/// размер того, что будет крутить скрол превьюшек
		/// </summary>
		int scrollSize = 0;

		/// <summary>
		/// для скрола. Работы с превьюшкао
		/// </summary>
		bool isScrollThumbnailImagesForShadow = false;
		/// <summary>
		/// сбособ работы с тифом
		/// </summary>
		protected ControlTypeWork TypeWork = ControlTypeWork.ReadWithCloseTiffHandle | ControlTypeWork.DrawShadowPreview;

		/// <summary>
		///  Список видимых превьюшек
		/// </summary>
		private SynchronizedCollection<VisiblePreview> listvis = new SynchronizedCollection<VisiblePreview>();

		private SynchronizedCollection<KeyValuePair<Bitmap, bool>> previews = null;
		//скрол превьюшек
		private VScrollBar scrollThumbnailImage = new VScrollBar();
		private HScrollBar scrollThumbnailImageHorizontal = new HScrollBar();

		/// <summary>
		/// переключатель работы с временным файлом.
		/// </summary>
		private bool useTempImage;

		private object lo = new object();
		/// <summary>
		/// коллекция видимых пользователю страниц
		/// </summary>
		//internal static SynchronizedCollection<int> visiblePages;

		/// <summary>
		/// Временный файл (для сканирования)
		/// </summary>
		private string tempFileName;

		/// <summary>
		/// Координаты сплитера
		/// </summary>
		private Rectangle rectSplitter;
		#endregion

		#region Перечисления и структуры
		/// <summary>
		/// Способ работы с тифом.
		/// Если тиф гигантский наверное разумно использовать ReadWithoutCloseTiffHandle совместно с CreateFullPagesBitmapAfterAllPagesGot.
		/// Если маленький - ReadAllTif
		/// </summary>
		[Flags]
		protected enum ControlTypeWork
		{
			/// <summary>
			/// чтение из единожды открытого файла(не протестировано)
			/// </summary>
			ReadWithoutCloseTiffHandle = 1,
			/// <summary>
			/// чтение, открывая и закрывая файл
			/// </summary>
			ReadWithCloseTiffHandle = 2,
			/// <summary>
			/// чтение сразу всего файла, битмап рисуется и кэшируется, устанавливается флаг isLockThumbnailImages, после рисуется закешированнный битмап
			/// </summary>
			//ReadAllTif = 4,
			/// <summary>
			/// Во время скроллинга сонтрол не перерисовывается, рисуются тени превьюшек. После остановки происходит загрузка картинок из файла
			/// </summary>
			DrawShadowPreview = 8,
			/// <summary>
			/// Превьюшки рисуются в едином масштабе
			/// </summary>
			DrawCorrectScale = 16

		}

		/// <summary>
		/// Чем занят пользователь на контроле
		/// </summary>
		protected enum UsersActionsTypes
		{
			Splitter,
			MoveImage,
			DrawFRect,
			DrawMarker,
			DrawHRect,
			DrawRectText,
			DrawNote,
			DrawImage,
			EditNote,
			SelectionMode,
			SelectionNotes,
			None
		}

		/// <summary>
		/// В каком режиме работает картинка
		/// </summary>
		protected enum TypeWorkImage
		{
			MoveImage,
			EditNotes,
			CreateNotes,
			SelectionMode
		}

		/// <summary>
		/// Сведения о превьюшке
		/// </summary>
		private struct VisiblePreview
		{
			public Rectangle rect;
			public Rectangle rectPaint;
			public int index;
			public VisiblePreview(Rectangle rect, int index, Rectangle rectPaint)
			{
				this.rect = rect;
				this.rectPaint = rectPaint;
				this.index = index;
			}
		}

		/// <summary>
		/// Направления курсора при масштабировании
		/// </summary>
		private enum Direction
		{
			UL,
			U,
			UR,
			R,
			DR,
			D,
			DL,
			L
		}
		/// <summary>
		/// Типы ориентации превьюшек
		/// </summary>
		public enum TypeThumbnailPanelOrientation
		{
			Top,
			Left,
			Right,
			Bottom
		}
		/// <summary>
		/// Типы фигур
		/// </summary>
		public enum Figures
		{
			FilledRectangler,
			HollowRectangle,
			Marker,
			Text,
			Note,
			EmbeddedImage,
			TextStamp
		}
		/// <summary>
		/// Состояния в которых находятся фигуры заметок
		/// </summary>
		protected enum AnnotationsState
		{
			Drag,
			NotDrag,
			Create,
			CreateText,
			EditText,
			None
		}
		#endregion

		#region Переменные картинок и заметок

		/// <summary>
		/// Отображается картинка другого типа
		/// </summary>
		private bool anotherFormat;

		/// <summary>
		/// Проверять файл перед сохранением
		/// </summary>
		private bool isVerifyFile = true;

		/// <summary>
		/// Класс, непосредственно рисующий заметки
		/// </summary>
		protected RenderAnnotations renderAnnotations = new RenderAnnotations(WidthSelectedtRect);

		/// <summary>
		/// Тип работы пользователя с контролом
		/// </summary>
		private TypeWorkImage typeWorkAnimatedImage = TypeWorkImage.MoveImage;

		/// <summary>
		/// Рисуем ли заметки
		/// </summary>
		private bool IsAnnotationDraw = false;
		/// <summary>
		/// Список груп заметок
		/// </summary>
		private static Hashtable markGroupsVisibleList = new Hashtable();

		public static Hashtable AllMarkGroupsVisibleList
		{
			get { return ImageControl.markGroupsVisibleList; }
		}
		/// <summary>
		/// Слудующая страница
		/// </summary>
		private int newPage = 0;
		/// <summary>
		/// Хранится оригинальная текущая страница, если есть изменения
		/// </summary>
		private Tiff.PageInfo changedPage;
		/// <summary>
		/// Изменения картинки
		/// </summary>
		protected bool modified = false;
		/// <summary>
		/// Изменения заметок
		/// </summary>
		private bool modifiedMarks = false;
		/// <summary>
		/// Изменение штампов
		/// </summary>
		protected bool modifiedStamps = false;
		/// <summary>
		/// Сохранять штампы в изображении
		/// </summary>
		private bool saveStampsInternal = true;
		/// <summary>
		/// Текщая интерполяция
		/// </summary>
		protected InterpolationMode CurrentInterpolationMode = InterpolationMode.High;
		/// <summary>
		/// Текщий формат
		/// </summary>
		private static PixelFormat CurrentPixelFormat = PixelFormat.Format24bppRgb;
		/// <summary>
		/// Область выделения
		/// </summary>
		private Rectangle SelectionModeRectangle = Rectangle.Empty;

		public Rectangle SelectionRectangle
		{
			get { return SelectionModeRectangle; }
		}

		/// <summary>
		/// переманая отвечающая за использование блокировки
		/// </summary>
		public bool UseLock
		{
			get { return libTiff.IsUseLock; }
			set { libTiff.IsUseLock = value; }
		}


		public bool AnotherFormat
		{
			get { return anotherFormat; }
		}

        /// <summary>
        /// Угол виртуального поворота
        /// </summary>
        public virtual void SetVirtualRotation()
        {
        }

        /// <summary>
        /// Сброс флага изменено.
        /// Используется в функционале поворота подписанного изображения
        /// </summary>
        public void ResetModified()
        {
           modified = false;
           modifiedMarks = false;
		   this.modifiedPages.Clear();
        }

		/// <summary>
		/// Область выделения для выбора заметок
		/// </summary>
		protected Rectangle selectionNotesRectangle = Rectangle.Empty;

		/// <summary>
		/// Массив выделений для фигур
		/// </summary>
		protected Rectangle[] selectedRectangles = null;

		private ListDictionary notesToSelectedRectangles = new ListDictionary();

		/// <summary>
		/// Выделенная заметка для редактирования
		/// </summary>
		private TiffAnnotation.IBufferBitmap SelectedBitmap = null;
		/// <summary>
		/// Выделенные заметки для редактирования
		/// </summary>
		private SynchronizedCollection<TiffAnnotation.IBufferBitmap> SelectedBitmaps = new SynchronizedCollection<TiffAnnotation.IBufferBitmap>();
		/// <summary>
		/// Чем занят пользователь на контроле
		/// </summary>
		protected UsersActionsTypes UserAction = UsersActionsTypes.None;
		/// <summary>
		/// Координата последней позиции заметки
		/// </summary>
		protected Point lastPositionForDrag;
		/// <summary>
		/// Текст бох для создания или редактирования текстовых заметок
		/// </summary>
		private RichTextBox rTextBox;
		/// <summary>
		/// Ширина сплитера если вертикальный
		/// </summary>
		private int widthSplitter = DEFAULT_WIDTH_SPLITTER;
		/// <summary>
		/// Высота сплитера если горизонтальный
		/// </summary>
		private int heightSplitter = DEFAULT_HEIGHT_SPLITTER;
		/// <summary>
		/// Класс заметок
		/// </summary>
		protected TiffAnnotation tiffAnnotation = null;
		/// <summary>
		/// Ширина зуммированной картинки
		/// </summary>
		protected int zoomWigth = 0;
		/// <summary>
		/// Выстота зуммированной картинки
		/// </summary>
		protected int zoomHeigth = 0;
		/// <summary>
		/// Величина зума
		/// </summary>
		protected double zoom = 1;
		/// <summary>
		/// Отступ скролла по горизонтали
		/// </summary>
		protected int scrollX;
		/// <summary>
		/// Отступ скролла по вертикали
		/// </summary>
		protected int scrollY;
		/// <summary>
		/// Задан внеший скролинг
		/// </summary>
		private bool externalScroll;
		/// <summary>
		/// Последний x мыши при драге картинки
		/// </summary>
		private int x0;
		/// <summary>
		/// Последний y мыши при драге картинки
		/// </summary>
		private int y0;
		/// <summary>
		/// Надо ли скролить горизонталь
		/// </summary>
		private bool needScrollX = false;
		/// <summary>
		/// Надо ли скролить вертикаль
		/// </summary>
		private bool needScrollY = false;
		/// <summary>
		/// Направление курсора при изменении размеров заметки
		/// </summary>
		private Direction CursorDirection;
		/// <summary>
		/// Вспомогательный состояния заметок
		/// </summary>
		protected AnnotationsState AnnotationState;
		/// <summary>
		/// Координаты и размеры поcле изменения заметки
		/// </summary>
		protected Rectangle oldRect = new Rectangle();
		/// <summary>
		/// Координаты и размеры для инвалидейта, включают старую область и новую - измененную
		/// </summary>
		protected Rectangle invalidRect = new Rectangle();
		/// <summary>
		/// Изменился ли размер заметки
		/// </summary>
		protected bool isSizeChanged = false;
		/// <summary>
		/// Надо ли перирисовывать закешированный битмап рисунка
		/// </summary>
		protected bool IsRefreshBitmap = true;
		/// <summary>
		/// Кеш для хранения рисунка
		/// </summary>
		protected Bitmap cachedBitmap = null;
		/// <summary>
		/// Область для рисования картинки, скролов и рамки
		/// </summary>
		protected Rectangle rectAnimatedImage;
		/// <summary>
		/// Картинка текущая
		/// </summary>
		protected Bitmap animatedImage = null;
		/// <summary>
		/// Вертикальный скрол картинок
		/// </summary>
		protected VScrollBar scrollImageVertical = new VScrollBar();
		/// <summary>
		/// Горизонтальный скрол картинок
		/// </summary>
		protected HScrollBar scrollImageHorizontal = new HScrollBar();

		private Bitmap image;

		/// <summary>
		/// Обертка либтифа
		/// </summary>
		protected Tiff.LibTiffHelper libTiff = null;

		/// <summary>
		/// Название файла
		/// </summary>
		protected string fileName;

		/// <summary>
		/// Тип маштабирования
		/// </summary>
		private short fitValue = 3;

		/// <summary>
		/// Изображение текущего выбранного штампа
		/// </summary>
		public Image CurrentStamp
		{
			get { return _currentStamp; }
			set
			{
				if(_currentStamp != null)
					_currentStamp.Dispose();
				_currentStamp = value;
				lastPositionForDrag = new Point(scrollX, scrollY);
			}
		}

		private Image _currentStamp;

		private Dictionary<int, Tuple<int, int, bool>> modifiedPages;

		#endregion

		#region Events

		public event FileNameChangedHandler FileMoved;
		public void OnFileMoved()
		{
			if(FileMoved != null)
				FileMoved(this, new FileNameChangedArgs { FileName = fileName });
		}

		public delegate void ScanCompleteHandler(object sender, ScanCompleteArgs e);
		public event ScanCompleteHandler ScanComplete;

		public void OnScanComplete(string fileName, Scaner.ScanType scanType)
		{
			if(ScanComplete != null)
				ScanComplete(this, new ScanCompleteArgs { ScanType = scanType, FileName = fileName });
		}

		public delegate void FileNameChangedHandler(object sender, FileNameChangedArgs e);
		public event FileNameChangedHandler FileNameChanged;

		public void OnFileNameChanged()
		{
			if(FileNameChanged != null)
				FileNameChanged(this, new FileNameChangedArgs { FileName = fileName });
		}

		public event System.Windows.Forms.SplitterEventHandler SplinterPlaceChanged;

		protected internal void OnSplinterChange(object sender, System.Windows.Forms.SplitterEventArgs e)
		{
			if(SplinterPlaceChanged != null)
				SplinterPlaceChanged(sender, e);
		}

		public event EventHandler PageChanged;
		protected virtual void OnPageChange()
		{
			try
			{
				if(PageChanged != null)
					PageChanged(this, EventArgs.Empty);
			}
			catch(Exception ex)
			{
				Tiff.LibTiffHelper.WriteToLog(ex);
			}
		}

		public event EventHandler ScanStart;
		protected internal void OnScanStart()
		{
			if(ScanStart != null)
				ScanStart(this, EventArgs.Empty);
		}

		public event SaveEventHandler NeedSave;
		protected internal void OnNeedSave(SaveEventHandler handler)
		{
			OnNeedSave(handler, null);
		}
		protected internal void OnNeedSave(SaveEventHandler handler, SaveEventArgs args)
		{
			if(args == null)
				args = new SaveEventArgs();
			args.AfterSave += handler;
			if(NeedSave != null)
				NeedSave(args);
		}

		public delegate void MarkEndEventHandler(object sender, MarkEndEventArgs e);
		public event MarkEndEventHandler MarkEnd;
		protected internal void OnMarkEnd(object sender, MarkEndEventArgs e)
		{
			try
			{
				if(MarkEnd != null)
					MarkEnd(sender, e);
			}
			catch(Exception ex)
			{
				Tiff.LibTiffHelper.WriteToLog(ex);
			}
		}

		public event System.EventHandler ImageLoad;
		internal void OnImageLoad()
		{
			try
			{
				if(ImageLoad != null)
					ImageLoad(this, new System.EventArgs());
			}
			catch(Exception ex)
			{
				Tiff.LibTiffHelper.WriteToLog(ex);
			}
		}

		/// <summary>
		/// Выбор режима работы с контролом произошел
		/// </summary>
		protected void OnToolSelected(ToolSelectedEventArgs e)
		{
			try
			{
				if(ToolSelected != null)
				{
					ToolSelected.DynamicInvoke(new object[] { this, e });
				}
			}
			catch(Exception ex)
			{
				Tiff.LibTiffHelper.WriteToLog(ex);
			}
		}

		#endregion

		#region Свойства

		/// <summary>
		/// Ориентация панели превьюшек
		/// </summary>
		[System.ComponentModel.Browsable(true)]
		public TypeThumbnailPanelOrientation ThumbnailPanelOrientation
		{
			get { return thumbnailPanelOrientation; }
			set
			{
				thumbnailPanelOrientation = value;
				InitDefaultValueThumbnailPanel();
			}
		}

		/// <summary>
		/// разрешение экрана
		/// </summary>
		[System.ComponentModel.Browsable(false)]
		public float DPI
		{
			get { return dpi; }
			set { dpi = value; }
		}

		/// <summary>
		/// Проверять ли файл?
		/// </summary>
		[System.ComponentModel.Browsable(true)]
		public bool IsVerifyFile
		{
			get { return isVerifyFile; }
			set { isVerifyFile = value; }
		}

		[System.ComponentModel.Browsable(false)]
		public int ImageHeight
		{
			get
			{
				if(this.animatedImage != null)
					return this.animatedImage.Height;
				else
					return 0;
			}
		}
		[System.ComponentModel.Browsable(false)]
		public int ImageWidth
		{
			get
			{
				if(this.animatedImage != null)
					return this.animatedImage.Width;
				else
					return 0;
			}
		}
		[System.ComponentModel.Browsable(false)]
		public int ImageResolutionY
		{
			get
			{
				if(this.animatedImage != null)
					return Convert.ToInt32(this.animatedImage.VerticalResolution);
				else
					return 1;
			}
			set
			{
				if(this.animatedImage == null)
					return;
				this.animatedImage.SetResolution(this.animatedImage.HorizontalResolution, value);
				this.IsRefreshBitmap = true;
				Invalidate(this.rectAnimatedImage, false);
			}
		}

		[System.ComponentModel.Browsable(false)]
		public int ImageResolutionX
		{
			get
			{
				if(this.animatedImage != null)
					return Convert.ToInt32(this.animatedImage.HorizontalResolution);
				else
					return 1;
			}
			set
			{
				if(this.animatedImage == null)
					return;
				this.animatedImage.SetResolution(value, this.animatedImage.VerticalResolution);
				this.IsRefreshBitmap = true;
				Invalidate(this.rectAnimatedImage, false);
			}
		}

		[System.ComponentModel.Browsable(false)]
		public PixelFormat ImagePixelFormat
		{
			get
			{
				if(this.animatedImage != null)
					return this.animatedImage.PixelFormat;
				else
					return PixelFormat.Undefined;
			}
		}

		[System.ComponentModel.Browsable(false)]
		public ColorPalette ImagePalette
		{
			get
			{
				if(this.animatedImage != null)
					return this.animatedImage.Palette;
				else
					return null;
			}
		}

        /// <summary>
        /// Значение Value скролинга относительно Max value (100% - 1, 0% - 0)
        /// </summary>
        public double ActualImageVerticalScrollValue
	    {
	        get
	        {
	            var max = ActualImageVerticalScrollMaxValue;

                if (max == 0)
	            {
	                if (scrollImageVertical.Value != 0)
	                    return 1;

	                return 0;
	            }

                return (double)scrollImageVertical.Value /max;
	        }
	    }

        /// <summary>
        /// Эффективная максимальная граница значения скролинга Value (min значение 0)
        /// </summary>
        public int ActualImageVerticalScrollMaxValue
        {
            get
            {
                return scrollImageVertical.Maximum - scrollImageVertical.LargeChange;
            }
        }

        /// <summary>
        /// Значение Value скролинга относительно Max value (100% - 1, 0% - 0)
        /// </summary>
        public double ActualImageHorisontallScrollValue
        {
            get
            {
                var max = ActualImageHorisontalScrollMaxValue;

                if (max == 0)
                {
                    if (scrollImageHorizontal.Value != 0)
                        return 1;

                    return 0;
                }

                return (double)scrollImageHorizontal.Value / max;
            }
        }

        /// <summary>
        /// Эффективная максимальная граница значения скролинга Value (min значение 0)
        /// </summary>
        public int ActualImageHorisontalScrollMaxValue
        {
            get
            {
                return scrollImageHorizontal.Maximum - scrollImageHorizontal.LargeChange;
            }
        }

	    /// <summary>
		/// Способ работы пользователя с контролом
		/// </summary>
		protected TypeWorkImage TypeWorkAnimatedImage
		{
			get { return typeWorkAnimatedImage; }
			set
			{
				bool isInvalidate = false;
				if(AnnotationState == AnnotationsState.CreateText || AnnotationState == AnnotationsState.EditText)
				{
					EndEditFiguresText();
					IsRefreshBitmap = true;
					isInvalidate = true;
				}
				if(typeWorkAnimatedImage == TypeWorkImage.SelectionMode && value != TypeWorkImage.SelectionMode)
				{
					SelectionModeRectangle = Rectangle.Empty;
					IsRefreshBitmap = true;
					isInvalidate = true;
				}
				if(isInvalidate)
					Invalidate();
				typeWorkAnimatedImage = value;
			}

		}
		/// <summary>
		/// Изменения картинки для сохранения нового документа
		/// </summary>
		public void SetModifiedForNewDocument(bool modified)
		{
			this.modified = modified;
			this.modifiedMarks = modified;
		}

		/// <summary>
		/// Изменения картинки. Ставить до того как произодет само изменение. Установка через метод касается только изменений animatedImage
		/// </summary>
		private void SetModified(bool modified)
		{
			if(modified && this.modified != modified && SelectedIndex >= 0)
			{
				changedPage = new Tiff.PageInfo() { Annotation = tiffAnnotation != null ? tiffAnnotation.GetAnnotationBytes(false) : null, Image = (Bitmap)animatedImage.Clone() };
			}
			else
				if(changedPage != null && changedPage.Image != null)
				{
					changedPage.Image.Dispose();
					changedPage.Image = null;
					changedPage.Annotation = null;
				}
			this.modified = modified;
		}

		/// <summary>
		/// Установщик изменений в заметках
		/// </summary>
		private void SetModifiedMarks(bool modifiedMarks)
		{
			this.modifiedMarks = modifiedMarks;
			OnMarkEnd(this, MarkEndEventArgs.Empty);
		}

		[System.ComponentModel.Browsable(false)]
		public int ScrollPositionX
		{
			get { return scrollX; }
			set
			{
				if(value <= 0)
				{
					scrollX = value;
					externalScroll = true;
					try
					{
						if(-value < scrollImageHorizontal.Maximum)
							scrollImageHorizontal.Value = -value;
					}
					catch
					{ }
				}
				else
					scrollY = 0;
			}
		}

		[System.ComponentModel.Browsable(false)]
		public int ScrollPositionY
		{
			get { return scrollY; }
			set
			{
				if(value <= 0)
				{
					scrollY = value;
					externalScroll = true;
					try
					{
						if(scrollImageVertical.Maximum > -value)
							scrollImageVertical.Value = -value;
					}
					catch { }
				}
				else
					scrollY = 0;
			}
		}

        /// <summary>
        /// Одна или более страниц виртуально повернута.
        /// Для подписки на изменение изображения
        /// </summary>
        public virtual bool HaveVirtualRotation
        {
            get { return false; }
        }

        /// <summary>
        /// Одна или более страниц виртуально БЫЛА повернута.
        /// Для подписки на изменение изображения
        /// </summary>
        public virtual bool RotationChanged
        {
            get { return false; }
        }

		/// <summary>
		/// Обратный вызов после сохранения 
		/// </summary>
		protected void ChangePage(SaveEventArgs args)
		{
			if(!args.Save)
			{
				if(modified || modifiedPages.Count > 0)
				{
					isLockThumbnailImages = false;

                    if (changedPage != null && changedPage.Image != null)
						animatedImage = changedPage.Image;
					List<int> pages = null;
					if(modifiedPages.Count > 0)
					{
						pages = modifiedPages.Keys.ToList();
						modifiedPages.Clear();
					}
					if((pages == null && SelectedIndex > -1) || (pages != null && pages.Count == 1 && SelectedIndex == pages[0]))
					{
						if(previews != null)
						{
							previews.RemoveAt(SelectedIndex);
							previews.Insert(SelectedIndex, new KeyValuePair<Bitmap, bool>(GetPreview(animatedImage, true), tiffAnnotation != null));
						}
						listvis = FillCoordinatesVisiblePreview(true);
					}
					if(pages != null && pages.Count > 1)
					{
						RefreshPage( pages, false);
					}
				}
				if(modifiedMarks)
				{
					if(this.tiffAnnotation != null)
					{
						this.tiffAnnotation.ModifiedFigure -= annotation_ModifiedFigure;
						this.tiffAnnotation.Dispose();
						this.tiffAnnotation = null;
					}
					if(changedPage != null && changedPage.Annotation != null)
					{
						this.tiffAnnotation = new TiffAnnotation(this);
						this.tiffAnnotation.Parse(changedPage.Annotation);
					}
				}
				IsRefreshBitmap = true;
			}
			else if(modifiedMarks)
				if(previews != null && (previews.Count > SelectedIndex) && previews[SelectedIndex].Value != (this.tiffAnnotation != null))
					previews[SelectedIndex] = new KeyValuePair<Bitmap, bool>(previews[SelectedIndex].Key, this.tiffAnnotation != null);

			SetModifiedMarks(false);

			SetModified(false);

			modifiedStamps = false;
			if(newPage > 0)
				Page = newPage;
			else
				Page = SelectedIndex + 1;
		}

		private void MoveScrol(int index, bool allTheSameChange)
		{
			if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
			{
				int x = (int)Math.Ceiling((double)(INDENT_BETWEEN_PREVIEW + (100 + INDENT_THUMBNAIL_IMAGE) * (index / countWithRow)));
				if(allTheSameChange || x < this.globalOffsetYThumbnail || x > globalOffsetYThumbnail + this.widthThumbnailImage)
				{
					if(x > scrollSize)
						scrollThumbnailImageHorizontal.Value = scrollThumbnailImageHorizontal.Maximum;
					else if(x < scrollThumbnailImageHorizontal.Minimum)
						scrollThumbnailImageHorizontal.Value = scrollThumbnailImageHorizontal.Minimum;
					else
						scrollThumbnailImageHorizontal.Value = x;
					ScrollValueChanged(scrollThumbnailImageHorizontal.Value);
				}
			}
			else
			{
				int y = (int)Math.Ceiling((double)(INDENT_BETWEEN_PREVIEW + (140 + INDENT_THUMBNAIL_IMAGE) * (index / countWithRow)));
				if(allTheSameChange || y < this.globalOffsetYThumbnail || y > globalOffsetYThumbnail + this.heightThumbnailImage)
				{
					if(y > scrollSize)
						scrollThumbnailImage.Value = scrollThumbnailImage.Maximum;
					else if(y < scrollThumbnailImage.Minimum)
						scrollThumbnailImage.Value = scrollThumbnailImage.Minimum;
					else
						scrollThumbnailImage.Value = y;
					ScrollValueChanged(scrollThumbnailImage.Value);
				}
			}
		}

		/// <summary>
		/// Смена страниц страниц в многостраничном документе
		/// </summary>
		[System.ComponentModel.Browsable(true)]
		public virtual int Page
		{
			get
			{
				return (SelectedIndex >= 0) ? SelectedIndex + 1 : 0;
			}
			set
			{
				if(value < 1 || value > PageCount)
					return;
				VerifyEndEditTextsMark(false);
				TryChangePage(value - 1);
				externalScroll = false;
			}
		}

		[System.ComponentModel.Browsable(false)]
		public bool AnnotationDraw
		{
			get
			{
				return IsAnnotationDraw;
			}
			set
			{
				IsAnnotationDraw = value;
			}
		}

		/// <summary>
		/// Количество групп
		/// </summary>
		[System.ComponentModel.Browsable(false)]
		public int AnnotationGroupCount
		{
			get
			{
				if(tiffAnnotation == null || tiffAnnotation.MarkAttributes == null)
					return 0;
				else
					return markGroupsVisibleList.Count;
			}
		}

		/// <summary>
		/// Показывается- ли изображение
		/// </summary>
		[System.ComponentModel.Browsable(false)]
		public bool ImageDisplayed
		{
			get { return animatedImage != null; }
		}

		/// <summary>
		/// Установка размера превьюшек только для docview
		/// </summary>
		[System.ComponentModel.Browsable(true)]
		public int SplinterPlace
		{
			get
			{
				if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
					return userHeightThumbnailImage;
				else
					return userWidthThumbnailImage;
			}
			set
			{
				try
				{
					if(value < 1)
						return;
					int tempPreviewWithRow = 1;
					if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
					{
						userHeightThumbnailImage = value;
						if(!showThumbPanel)
							return;
						SetSizeThumbnailPanel(true);
						heightThumbnailImage = userHeightThumbnailImage;
						defaultHeightThumbnailImage = heightThumbnailImage;

						heightImage = Height - heightThumbnailImage - heightSplitter;
						rectAnimatedImage = new Rectangle(0, heightThumbnailImage + heightSplitter, widthImage, heightImage);
						scrollThumbnailImage.Location = new Point(widthThumbnailImage - scrollThumbnailImage.Width, 1);
						rectSplitter = new Rectangle(0, heightThumbnailImage + 1, widthImage, heightSplitter);
						rectThumbnailPanel = new Rectangle(0, 0, widthThumbnailImage, heightThumbnailImage);
						tempPreviewWithRow = GetCountWithRow(heightThumbnailImage, 140);
					}
					else
					{
						userWidthThumbnailImage = value;
						if(!showThumbPanel)
							return;
						SetSizeThumbnailPanel(true);
						widthThumbnailImage = userWidthThumbnailImage;
						defaultWidthThumbnailImage = userWidthThumbnailImage;

						widthImage = Width - widthThumbnailImage - widthSplitter;
						rectAnimatedImage = new Rectangle(widthThumbnailImage + widthSplitter, 0, widthImage, heightImage);
						scrollThumbnailImage.Location = new Point(widthThumbnailImage - scrollThumbnailImage.Width - 1, 1);
						rectSplitter = new Rectangle(widthThumbnailImage + 1, 0, widthSplitter, heightImage);
						rectThumbnailPanel = new Rectangle(0, 0, widthThumbnailImage, heightThumbnailImage);
						tempPreviewWithRow = GetCountWithRow(widthThumbnailImage, 100);
					}
					CalculationForDrawImageAfterSelectRotateScale(1.0);
					if(tempPreviewWithRow != countWithRow)
					{
						countWithRow = tempPreviewWithRow;
						CalculationForDraw();
						MoveScrol(SelectedIndex, true);
					}
					else
					{
						if(this.fitValue <= 2)
							FitTo(fitValue, true);
						else
							CalculationForDrawImageAfterSelectRotateScale(1.0);
						listvis = FillCoordinatesVisiblePreview(true);
						Refresh();
					}
				}
				catch(Exception ex)
				{
					Log.Logger.WriteEx(ex);
				}
			}
		}

		/// <summary>
		/// Изображение изменено
		/// </summary>
		[System.ComponentModel.Browsable(false)]
		public bool Modified
		{
			get { return modified || modifiedMarks || modifiedPages.Count > 0; }
		}

		/// <summary>
		/// на изображение поставлены печати
		/// </summary>
		[System.ComponentModel.Browsable(false)]
		public bool ModifiedStamps
		{
			get { return modifiedStamps; }
		}

		/// <summary>
		/// Режим сохранения штампов
		/// </summary>
		public bool SaveStampsInternal
		{
			get { return saveStampsInternal; }
			set { saveStampsInternal = value; }
		}

		/// <summary>
		/// Показ панели страниц
		/// </summary>
		[System.ComponentModel.Browsable(true)]
		public bool ShowThumbPanel
		{
			get { return showThumbPanel; }
			set
			{
				if(showThumbPanel == value)
					return;
				showThumbPanel = value;
				if(value)
				{
					try
					{
						if((thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
							&& userHeightThumbnailImage <= 0)
						{
							userHeightThumbnailImage = DEFAULT_HEIGHT_THUMBNAIL_PANEL;
						}
						else if((thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Left || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Right)
							&& userWidthThumbnailImage <= 0)
						{
							userWidthThumbnailImage = DEFAULT_WIDTH_THUMBNAIL_PANEL;
						}
						SetSizeThumbnailPanel(value);
						widthSplitter = DEFAULT_WIDTH_SPLITTER;
						heightSplitter = DEFAULT_HEIGHT_SPLITTER;
						SetAllSizes();
						CalculationForDraw();
					}
					catch
					{
						ShowThumbPanel = false;
						return;
					}
					try
					{
						if(SelectedIndex < 0)
							SelectedIndex = 0;
						MoveScrol(SelectedIndex, true);
					}
					catch { }
				}
				else
				{
					defaultWidthThumbnailImage = 0;
					defaultHeightThumbnailImage = 0;
					widthSplitter = 0;
					heightSplitter = 0;
					SetAllSizes();
					CalculationForDraw();
					Invalidate();
				}
			}
		}

		/// <summary>
		/// Количество страниц
		/// </summary>
		[System.ComponentModel.Browsable(false)]
		public int PageCount
		{
			get
			{
				if(modifiedPages != null && modifiedPages.Count(x => x.Value.Item3) > 0)
					return countPreview - modifiedPages.Count + modifiedPages.Count(x => x.Value.Item3);
				return countPreview;
			}
		}

		/// <summary>
		/// Управление работой превьюшек
		/// </summary>
		[System.ComponentModel.Browsable(true)]
		public int TypeWorkThumbnailImagesPanel
		{
			get
			{
				if((TypeWork & (ControlTypeWork.ReadWithoutCloseTiffHandle | ControlTypeWork.DrawShadowPreview)) == (ControlTypeWork.ReadWithoutCloseTiffHandle | ControlTypeWork.DrawShadowPreview))
					return 0;
				else if((TypeWork & ControlTypeWork.ReadWithoutCloseTiffHandle) == ControlTypeWork.ReadWithoutCloseTiffHandle)
					return 1;
				//else if ((TypeWork & ControlTypeWork.ReadAllTif) == ControlTypeWork.ReadAllTif)
				//    return 2;
				else if((TypeWork & (ControlTypeWork.ReadWithCloseTiffHandle | ControlTypeWork.DrawShadowPreview)) == (ControlTypeWork.ReadWithCloseTiffHandle | ControlTypeWork.DrawShadowPreview))
					return 3;
				else if((TypeWork & ControlTypeWork.ReadWithCloseTiffHandle) == ControlTypeWork.ReadWithCloseTiffHandle)
					return 4;
				else
					return 3;
			}
			set
			{
				if(value == 0)
					TypeWork = ControlTypeWork.ReadWithoutCloseTiffHandle | ControlTypeWork.DrawShadowPreview;
				else if(value == 1)
					TypeWork = ControlTypeWork.ReadWithoutCloseTiffHandle;
				//else if (value == 2)
				//    TypeWork = ControlTypeWork.ReadAllTif;
				else if(value == 3)
					TypeWork = ControlTypeWork.ReadWithCloseTiffHandle | ControlTypeWork.DrawShadowPreview | ControlTypeWork.DrawCorrectScale;
				else if(value == 4)
					TypeWork = ControlTypeWork.ReadWithCloseTiffHandle;

				//TypeWork |=ControlTypeWork.DrawCorrectScale;
				FileName = fileName;
			}
		}

		/// <summary>
		/// Установка варианта отрисовки панели превьюшек
		/// </summary>
		[System.ComponentModel.Browsable(true)]
		public bool IsCorrectScaleDrawThumbnailPanel
		{
			get
			{
				return ControlTypeWork.DrawCorrectScale == (TypeWork & ControlTypeWork.DrawCorrectScale);
			}
			set
			{
				if(value)
					TypeWork |= ControlTypeWork.DrawCorrectScale;
				else
					TypeWork ^= ControlTypeWork.DrawCorrectScale;
				//preveiw
				this.isLockThumbnailImages = false;
				Invalidate(new Rectangle(0, 0, this.widthThumbnailImage, this.heightThumbnailImage));
			}
		}

		protected void SetSizeThumbnailPanel(bool isShow)
		{
			if(isShow)
			{
				if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
				{
					if(Height == 0)
					{
						defaultHeightThumbnailImage = 0;
						return;
					}
					else if(Height < userHeightThumbnailImage)
					{
						userHeightThumbnailImage = Height >> 1;

					}
					defaultHeightThumbnailImage = userHeightThumbnailImage;
					if(defaultHeightThumbnailImage == 1)
						defaultHeightThumbnailImage = 0;
				}
				else
				{
					if(Width == 0)
					{
						defaultWidthThumbnailImage = 0;
						return;
					}
					else if(Width < userWidthThumbnailImage)
					{
						userWidthThumbnailImage = Width >> 1;
					}
					defaultWidthThumbnailImage = userWidthThumbnailImage;
					if(defaultWidthThumbnailImage == 1)
						defaultWidthThumbnailImage = 0;
				}
			}
		}

		protected override void OnResize(EventArgs e)
		{
			SetSizeThumbnailPanel(showThumbPanel);
			SetAllSizes();
			base.OnResize(e);
		}

		/// <summary>
		/// Смена файла
		/// </summary>
		[System.ComponentModel.Browsable(true)]
		[DefaultValue(null), Editor("System.Windows.Forms.Design.SelectedPathEditor, System.Design", typeof(System.Drawing.Design.UITypeEditor))]
		public virtual string FileName
		{
			get { return fileName; }
			set
			{
				externalScroll = false;
				LoadFile(value, 0);
			}
		}

		/// <summary>
		/// Зуммер
		/// </summary>
		[System.ComponentModel.Browsable(true)]
		public int Zoom
		{
			get { return (int)(zoom * 100); }
			set
			{
				double oldZoom = zoom;
				bool isInvalidate = false;
				fitValue = 3;
				if(AnnotationState == AnnotationsState.CreateText || AnnotationState == AnnotationsState.EditText)
				{
					EndEditFiguresText();
					IsRefreshBitmap = true;
					isInvalidate = true;
				}
				if(value > MaxZoom)
				{
					OnErrorMessage(new Exception("MaxZoom error"));
					//throw new Exception(Environment.StringResources.GetString("Image.ImageControl.Zoom.Error1"));
				}

				if(value < MinZoom)
				{
					OnErrorMessage(new Exception("MinZoom error"));
					//throw new Exception(Environment.StringResources.GetString("Image.ImageControl.Zoom.Error2"));

				}
				if(value > 0 && value < (double)int.MaxValue)
				{
					zoom = (value / 100d);
					if(animatedImage != null)
					{
						double dz = 1;
						if(oldZoom > 0 && oldZoom < int.MaxValue)
							dz = zoom / oldZoom;
						this.CalculationForDrawImageAfterSelectRotateScale(dz);
						isInvalidate = true;
					}
				}
				if(isInvalidate)
					this.Invalidate(this.rectAnimatedImage);

			}
		}

		/// <summary>
		/// Находится-ли контрол в режиме выделения
		/// </summary>
		[Browsable(false)]
		public bool IsSelectionMode
		{
			get
			{
				return TypeWorkAnimatedImage == TypeWorkImage.SelectionMode;
			}
			set
			{
				if(value)
				{
					TypeWorkAnimatedImage = TypeWorkImage.SelectionMode;
					this.Cursor = Cursors.Default;
					ClearSelectedNotes();
				}
			}
		}
		/// <summary>
		/// Включение рижима передвижения картинки
		/// </summary>
		[System.ComponentModel.Browsable(false)]
		public bool IsMoveImage
		{
			get { return TypeWorkAnimatedImage == TypeWorkImage.MoveImage; }
			set
			{
				if(value)
				{
					TypeWorkAnimatedImage = TypeWorkImage.MoveImage;
					ClearSelectedNotes();
				}
			}
		}

		/// <summary>
		/// Включение режима редактирования заметок
		/// </summary>
		[System.ComponentModel.Browsable(false)]
		public bool IsEditNotes
		{
			get { return TypeWorkAnimatedImage == TypeWorkImage.EditNotes; }
			set
			{
				if(value)
				{
					TypeWorkAnimatedImage = TypeWorkImage.EditNotes;
					ClearSelectedNotes();
				}

			}
		}

		[System.ComponentModel.Browsable(false)]
		public Bitmap Image
		{
			get { return this.animatedImage; }
			set
			{
				FileName = null;
				if(image != null)
					image.Dispose();
				if(value != null)
					image = value;
				Refresh();
			}
		}
		#endregion

		#region Конструктор

		public delegate void ErrorMessageHandler(Exception ex);
		protected ErrorMessageHandler errorMessageHandler;

		protected void OnErrorMessage(Exception ex)
		{
			if(this.errorMessageHandler != null)
				this.errorMessageHandler(ex);
		}

		public ImageControl()
		{
			ResetEventGetPagesStart.Set();
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
			if(!this.DesignMode)
				libTiff = new Tiff.LibTiffHelper();
			InitDefaultValueThumbnailPanel();
			Init();
			modifiedPages = new Dictionary<int, Tuple<int, int, bool>>();
			Clear();
		}

		void ImageControl_HandleDestroyed(object sender, EventArgs e)
		{
			if(imgScan != null)
				imgScan.ScanEnd();
		}


		/// <summary>
		/// Подключение событя информирования об ошибке
		/// </summary>
		/// <param name="errHandler"></param>
		public void AddErrorHandle(ErrorMessageHandler errHandler)
		{
			errorMessageHandler = errHandler;
		}

		private void InitDefaultValueThumbnailPanel()
		{
			if(!ShowThumbPanel)
				return;

			if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
			{
				userWidthThumbnailImage = 0;
				userHeightThumbnailImage = DEFAULT_HEIGHT_THUMBNAIL_PANEL;
			}
			else
			{
				userWidthThumbnailImage = DEFAULT_WIDTH_THUMBNAIL_PANEL;
				userHeightThumbnailImage = 0;
			}
			defaultHeightThumbnailImage = userHeightThumbnailImage;
			defaultWidthThumbnailImage = userWidthThumbnailImage;
			SetSizeThumbnailPanel(showThumbPanel);
		}

		private void Init()
		{
			scrollThumbnailImage.Maximum = 0;
			scrollThumbnailImage.Scroll += scrollThumbnailImage_Scroll;
			scrollThumbnailImage.MouseEnter += scrollImageVertical_MouseEnter;
			Controls.Add(scrollThumbnailImage);

			scrollThumbnailImageHorizontal.Maximum = 0;
			scrollThumbnailImageHorizontal.Scroll += scrollThumbnailImage_Scroll;
			scrollThumbnailImageHorizontal.MouseEnter += scrollImageVertical_MouseEnter;
			Controls.Add(scrollThumbnailImageHorizontal);


			scrollImageHorizontal.ValueChanged += scrollImageHorizontal_ValueChanged;
			scrollImageHorizontal.MouseEnter += scrollImageVertical_MouseEnter;
			scrollImageVertical.ValueChanged += scrollImageVertical_ValueChanged;
			scrollImageVertical.MouseEnter += scrollImageVertical_MouseEnter;
			Controls.Add(scrollImageHorizontal);
			Controls.Add(scrollImageVertical);
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(animatedImage != null)
					animatedImage.Dispose();
				if(tiffAnnotation != null)
				{
					this.tiffAnnotation.ModifiedFigure -= annotation_ModifiedFigure;
					this.tiffAnnotation.Dispose();
					tiffAnnotation = null;
				}
				if(imgScan != null)
					imgScan.Dispose();
				if(ThumbnailImagesBitmap != null)
					ThumbnailImagesBitmap.Dispose();
				if(fullCahedBitmap != null)
					fullCahedBitmap.Dispose();
				if(cachedBitmap != null)
					cachedBitmap.Dispose();
				if(_currentStamp != null)
					_currentStamp.Dispose();
				if(libTiff != null)
					libTiff.Dispose();
			}
			base.Dispose(disposing);
		}

		#endregion

		#region Автозагрузчик страниц
		delegate void PageLoaderHandler(string fileName);
		string filesNamesPageLoader;
#if DEBUG
		int deb;
		bool isWorkPageLoader;
#endif
		private void PageLoaderStart()
		{
			if(!string.IsNullOrEmpty(fileName) && filesNamesPageLoader != fileName)
			{
#if DEBUG
				isWorkPageLoader = true;
#endif
				filesNamesPageLoader = fileName;
				PageLoaderHandler handler = PageLoaderBegin;
				handler.BeginInvoke(fileName, PageLoaderEnd, handler);
			}
		}

		System.Threading.ManualResetEvent rwSyncMainPageLoader = new System.Threading.ManualResetEvent(true);
		private void PageLoaderBegin(string fileName)
		{
			try
			{
#if DEBUG
				System.Threading.Interlocked.Increment(ref deb);
				System.Diagnostics.Debug.WriteLine(deb.ToString());
#endif
				Bitmap bmp = null;
				IntPtr tifr = libTiff.TiffOpenRead(ref fileName, out bmp, false);
				if(tifr == IntPtr.Zero && bmp != null)
					bmp.Dispose();
				if(tifr != IntPtr.Zero && !UseLock)
					libTiff.TiffCloseRead(ref tifr);
			}
			catch(Exception ex)
			{
				OnErrorMessage(new Exception("PageLoaderBegin error", ex));
			}
		}

		private void PageLoaderEnd(IAsyncResult res)
		{
#if DEBUG
			System.Threading.Interlocked.Decrement(ref deb);
			System.Diagnostics.Debug.WriteLine(isWorkPageLoader.ToString() + deb.ToString());
			isWorkPageLoader = false;
#endif


			PageLoaderHandler handler = (PageLoaderHandler)res.AsyncState;
			handler.EndInvoke(res);
		}
		#endregion

		#region Сканер

		private Scaner imgScan;
		private string scanFileName = null;
		private delegate string ScanHandler(string scanFileName);

		private void InitScan()
		{
			imgScan = new Scaner();
			imgScan.ImagesReceived += imgScan_ImagesReceived;
			imgScan.DialogClose += imgScan_DialogClose;
			imgScan.Show();
			Controls.Add(imgScan);
		}

		public void SelectScanner()
		{
			if(imgScan == null)
				InitScan();
			imgScan.SelectScaner();
		}

		/// <summary>
		/// Метод сохранения и отображения результата сканирования 
		/// </summary>
		/// <param name="scanType">тип сканиировния</param>
		/// <param name="tempPages">массив картинок</param>
		/// <param name="callback"></param>
		private void ScanDialog(Scaner.ScanType scanType, List<Bitmap> tempPages, Scaner.CallbackHandler callback)
		{
			if(tempPages != null && tempPages.Count > 0)
			{
				//запоминаем текуще состояние
				string tempFileName = fileName;
				int tempIndex = SelectedIndex;
				//    Tiff.ImageList currentPages = new Tiff.ImageList();
				//    if (pages != null)
				//        currentPages = (Tiff.ImageList)pages.Clone();

				//заполняем новым содержимым
				String tempImage = "";
				Clear();
				//pages = tempPages;

				if(scanType == Scaner.ScanType.ScanNewDocument)
				{
					this.Visible = true;
					fileName = scanFileName;
				}
				else
				{

					fileName = tempFileName;
					tempImage = Path.GetTempPath() + "~" + Guid.NewGuid().ToString() + ".tif";
					File.Copy(fileName, tempImage, true);

					//SaveAs(tempImage);
					fileName = tempImage;
				}
				libTiff.SaveBitmapsCollectionToFile(fileName, tempPages.Select<Bitmap, Tiff.PageInfo>(x => new Tiff.PageInfo() { Image = x }).ToList(), true);
				countPreview = tempPages.Count;
				LoadFile(fileName, 0);
				IsRefreshBitmap = true;
				isLockThumbnailImages = false;


				Refresh();
				object result = null;
				if(scanType == Scaner.ScanType.ScanNewDocument)
				{
					result = fileName;
					if(result != null)
					{
						//libTiff.SaveBitmapsCollectionToFile(fileName, tempPages.Select<Bitmap, Tiff.PageInfo>(x => new Tiff.PageInfo() { Image = x }).ToList(), true);
						fi = new FileInfo(fileName);
						OnScanComplete(fileName, scanType);
					}
				}
				else if(MessageBox.Show("Изображение получено. Сохранить изображение?", "Сохранение", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					if(File.Exists(fileName))
						File.Delete(fileName);
					if(scanType == Scaner.ScanType.ScanAfter)
						libTiff.InsertAfterPage(tempFileName, tempIndex, tempPages.Select<Bitmap, Tiff.PageInfo>(x => new Tiff.PageInfo() { Image = x }).ToList());
					else if(scanType == Scaner.ScanType.ScanBefore)
						libTiff.InsertBeforePage(tempFileName, tempIndex, tempPages.Select<Bitmap, Tiff.PageInfo>(x => new Tiff.PageInfo() { Image = x }).ToList());
					if(File.Exists(tempFileName + ".tmp"))
					{
						File.Copy(tempFileName + ".tmp", tempFileName, true);
						File.Delete(tempFileName + ".tmp");
					}
					fileName = tempFileName;
					SelectedIndex = -1;
					OnScanComplete(fileName, scanType);

				}
				else
					result = true;
				if(tempPages != null)
				{
					foreach(Bitmap bmp in tempPages)
						bmp.Dispose();
					tempPages.Clear();
					tempPages = null;
				}
				if(scanType == Scaner.ScanType.ScanNewDocument)
				{
					if(callback != null)
						callback(result);
					else
					{
						if(File.Exists(scanFileName))
							File.Delete(scanFileName);
						scanFileName = null;
					}
				}
				else if(!string.IsNullOrEmpty(tempFileName))
				{
					////загружаем предудущий документ
					//Clear();

					//fileName = tempFileName;
					//pages = currentPages;
					//countPreview = currentPages.Count;
					//CalculationForDraw();

					//SelectedIndex = tempIndex;
					//IsRefreshBitmap = true;
					//isLockThumbnailImages = false;

					//if (SelectedIndex >= 0)
					//    TryChangePage(SelectedIndex);
					//if (System.IO.File.Exists(fileName))
					//    fi = new FileInfo(fileName);

					//Refresh();
					if(result is bool)
					{
						fileName = tempFileName;
						//CalculationForDraw();

						SelectedIndex = tempIndex;
						IsRefreshBitmap = true;
						isLockThumbnailImages = false;

						if(SelectedIndex >= 0)
							TryChangePage(SelectedIndex);
						if(System.IO.File.Exists(fileName))
							fi = new FileInfo(fileName);
					}
				}
				if(!String.IsNullOrEmpty(tempImage))
					File.Delete(tempImage);
			}
		}

		private void imgScan_ImagesReceived(object sender, Scaner.ScanEventArgs args)
		{
			List<Bitmap> bmps = args.Bitmaps;
			ScanDialog(args.CurrentScanType, bmps, args.Callback);
		}

		private void imgScan_DialogClose(object sender, Scaner.ScanEventArgs args)
		{
			if(args.Callback != null)
				args.Callback(null);
		}

		public bool ScanAfter(int numberPage)
		{
			GC.Collect();
			if(imgScan == null)
				InitScan();
			imgScan.StartScan(Scaner.ScanType.ScanAfter, null);

			return false;
		}

		public bool ScanBefore(int numberPage)
		{
			GC.Collect();
			if(imgScan == null)
				InitScan();
			imgScan.StartScan(Scaner.ScanType.ScanBefore, null);

			return false;
		}

		public void Scan(Scaner.CallbackHandler callback, string scanFileName)
		{
			GC.Collect();
			this.scanFileName = scanFileName;
			if(File.Exists(scanFileName))
				File.Delete(scanFileName);

			if(imgScan == null)
				InitScan();
			imgScan.StartScan(Scaner.ScanType.ScanNewDocument, callback);

		}

		#endregion

		#region Принтер

		public delegate void PrintActionHandler(bool print, int docID, int id, string fileName, int startPage, int endPage, int countPage, string docName, short copyCount);

		private void PrintAction(PrintActionHandler handler, bool print, string fileName, int docID, int id, int startPage, int endPage, int countPage, string docName, short copyCount)
		{
			if(handler != null)
				handler(print, docID, id, fileName, startPage, endPage, this.countPreview, docName, copyCount);
			else
			{
				PrintImage printImageInst = new PrintImage();
				printImageInst.PrintPage((docID > 0 ? " #" + docID.ToString() : !string.IsNullOrWhiteSpace(fileName) ? " " + fileName : ""),/*printImages*/null, null, startPage, endPage, 1, PrintOrientation.Auto, true, copyCount, "", null, null);
			}
		}

		public void PrintPage(PrintActionHandler handler, string filesName, int docID, int id, int page, string docString, short copyCount)
		{
			PrintAction(handler, true, filesName, docID, id, page, page, this.countPreview, docString, copyCount);
		}

		public void PrintPage(PrintActionHandler handler, int docID, int id, int page, string docString, short copyCount)
		{
			PrintAction(handler, true, this.fileName, docID, id, page, page, this.countPreview, docString, copyCount);
		}

		public void Print(PrintActionHandler handler, int docID, int id, string docString, short copyCount)
		{
			PrintAction(handler, true, this.fileName, docID, id, 1, PageCount, this.countPreview, docString, copyCount);
		}

		public void PrintSelection(PrintActionHandler handler, int docID, int id, string docString, short copyCount)
		{
			try
			{
				string tempImage = CreateSelectedImage();
				if(tempImage.Length > 0)
				{
					PrintPage(handler, tempImage, docID, -1, 1, docString, copyCount);
				}
			}
			catch(Exception ex)
			{
				OnErrorMessage(ex);
			}
		}


		#endregion
		                                                                                     
		#region Пользовательские методы

		public class ToolSelectedEventArgs : EventArgs
		{
			public int EventType { get; set; }
		}
		public delegate void ToolSelectedHandler(object sender, ToolSelectedEventArgs e);
		public event ToolSelectedHandler ToolSelected;
		/// <summary>
		/// Выбор работы с заметками
		/// </summary>
		/// <param name="tool"></param>
		public virtual void SelectTool(short tool)
		{
			if(tool == 2 || tool == 4 || tool == 6)
				return;
			switch(tool)
			{
				case 0:
					IsMoveImage = true;
					break;
				case 1:
					IsEditNotes = true;
					this.Select();
					break;
				case 3:
					BeginCreationFigure(Figures.Marker);
					break;
				case 5:
					BeginCreationFigure(Figures.HollowRectangle);
					break;
				case 7:
					BeginCreationFigure(Figures.Text);
					break;
				case 8:
					BeginCreationFigure(Figures.Note);
					break;
				case 9:
					BeginCreationFigure(Figures.EmbeddedImage);
					this.Focus();
					break;
			}

			OnToolSelected(new ToolSelectedEventArgs { EventType = tool });
		}

		private TypeShowNotes currentTypeShowNotes = TypeShowNotes.AllShow;

		private enum TypeShowNotes
		{
			AllShow,
			AllHide,
			OnlySelf
		}

		public bool IsNotesAllShow
		{
			get { return currentTypeShowNotes == TypeShowNotes.AllShow; }
		}

		public virtual bool IsNotesAllHide
		{
			get { return currentTypeShowNotes == TypeShowNotes.AllHide; }
		}

		public bool IsNotesOnlySelfShow
		{
			get { return currentTypeShowNotes == TypeShowNotes.OnlySelf; }
		}

		/// <summary>
		/// Скрыть заметки определенной группы(System.Reflection.Missing скрывает все)
		/// </summary>
		/// <param name="groupName"></param>
		public void HideAnnotationGroup(object groupName)
		{
			Hashtable temp = new Hashtable();
			foreach(string name in markGroupsVisibleList.Keys)
			{
				if(name == groupName.ToString())
				{
					markGroupsVisibleList[groupName] = false;
					IsRefreshBitmap = true;
					Invalidate(rectAnimatedImage);
					break;
				}
				if(groupName is System.Reflection.Missing)
					temp.Add(name, false);
			}
			if(groupName is System.Reflection.Missing)
			{
				currentTypeShowNotes = TypeShowNotes.AllHide;
				markGroupsVisibleList = temp;
				IsRefreshBitmap = true;
				Invalidate(rectAnimatedImage);
			}
		}

		/// <summary>
		/// Показать заметки определенной группы(System.Reflection.Missing показывает все)
		/// </summary>
		/// <param name="groupName"></param>
		public void ShowAnnotationGroup(object groupName)
		{
			Hashtable temp = new Hashtable();
			foreach(string name in markGroupsVisibleList.Keys)
			{
				if(name == groupName.ToString())
				{
					markGroupsVisibleList[groupName] = true;
					IsRefreshBitmap = true;
					Invalidate(rectAnimatedImage);
					break;
				}
				if(groupName is System.Reflection.Missing)
					temp.Add(name, true);
			}
			if(groupName is System.Reflection.Missing)
			{
				currentTypeShowNotes = TypeShowNotes.AllShow;
				markGroupsVisibleList = temp;
				IsRefreshBitmap = true;
				Invalidate(rectAnimatedImage);
			}
			else if(groupName.ToString() == GetCurrentAnnotationGroup())
				currentTypeShowNotes = TypeShowNotes.OnlySelf;
		}

		public void ShowAttribsDialog(object mark)
		{
		}

		/// <summary>
		/// Получить название группы по индексу
		/// </summary>
		public string GetAnnotationGroup(short index)
		{
			string result = "";
			int n = 0;
			foreach(string name in markGroupsVisibleList.Keys)
			{
				if(n == index)
				{
					result = name;
					break;
				}
				n++;
			}
			return result;
		}

		string currentAnnotationGroup = "";
		/// <summary>
		/// Завести текущую группу
		/// </summary>
		/// <returns></returns>
		public void SetCurrentAnnotationGroup(string currentAnnotationGroup)
		{
			this.currentAnnotationGroup = currentAnnotationGroup;
		}
		/// <summary>
		/// Получить текущую группу
		/// </summary>
		/// <returns></returns>
		public string GetCurrentAnnotationGroup()
		{
			return currentAnnotationGroup;
		}

		/// <summary>
		/// Зуммировать по выделенному
		/// </summary>
		public void ZoomToSelection()
		{
			if(SelectionModeRectangle.Width <= 0 || SelectionModeRectangle.Height <= 0)
				return;
			double dz = 1;
			double xz = (SelectionModeRectangle.Width * zoom * ppi / animatedImage.HorizontalResolution) / realWidthImage;
			double yz = (SelectionModeRectangle.Height * zoom * ppi / animatedImage.VerticalResolution) / realHeightImage;
			if(xz > yz)
			{
				dz /= xz;
				zoom /= xz;
			}
			else
			{
				dz /= yz;
				zoom /= yz;
			}
			CalculationForDrawImageAfterSelectRotateScale(dz);
			int scrollValue = (int)(SelectionModeRectangle.X * zoom);
			if(scrollValue <= scrollImageHorizontal.Maximum - scrollImageHorizontal.LargeChange)
				scrollImageHorizontal.Value = scrollValue;
			scrollValue = (int)(SelectionModeRectangle.Y * zoom);
			if(scrollValue <= scrollImageVertical.Maximum - scrollImageVertical.LargeChange)
				scrollImageVertical.Value = scrollValue;
			Invalidate(rectAnimatedImage, true);
		}

		/// <summary>
		/// Проверка, на изменения
		/// </summary>
		public void TestImage()
		{
			if(animatedImage != null && Modified)
				OnNeedSave(ChangePage);
		}

		/// <summary>
		/// Попытка сменить страницу
		/// <para>
		/// <note type="caution"> Нумерация <paramref name="index"/> начинается с 0</note>
		/// </para>
		/// </summary>
		private void TryChangePage(int index)
		{
			try
			{
				if(index >= 0 && index < PageCount)
				{
					SelectedIndex = index;
					isSuccessfullyChangePage = true;
					if(showThumbPanel)
					{
						MoveScrol(index, false);
						if(listvis.Count > 0)
						{
							foreach(VisiblePreview vis in listvis)
							{
								if(vis.index == SelectedIndex)
								{
									var y = vis.rect.Y + 1;
									if(y < 0)
										y = 0;

									OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, vis.rect.X + 1, y, 1));
									break;
								}
							}
						}
					}
					else//смена страниц, если панель превьюшек не видна
					{
						if(modified || modifiedMarks || modifiedStamps)
						{
							newPage = index + 1;
							OnNeedSave(ChangePage);
						}
						else
						{
							Cursor = Cursors.WaitCursor;

							IntPtr fileptr = IntPtr.Zero;
							Bitmap bmp = null;
							if(IntPtr.Zero == libTiff.TiffHandle)
							{
								try
								{
									fileptr = libTiff.TiffOpenRead(ref fileName, out bmp, false);
									if(fileptr == IntPtr.Zero && bmp != null)
										SelectPage(new PageInfo { Image = bmp });
									else
										SelectPage(GetImageFromTiff(fileptr, SelectedIndex));
								}
								finally
								{
									if(fileptr != IntPtr.Zero)
										libTiff.TiffCloseRead(ref fileptr);
								}
							}
							else
							{
								SelectPage(GetImageFromTiff(fileptr, SelectedIndex));
							}

							OnPageChange();
							newPage = 0;
							Refresh();

							if(CurrentStamp == null)
								Cursor = Cursors.Default;
						}

					}
					if(animatedImage != null)
						OnImageLoad();
				}
			}
			catch(Exception ex)
			{
				Tiff.LibTiffHelper.WriteToLog(new Log.DetailedException(fileName, ex));
			}
		}

		private int GetCurrentIndex(int index)
		{
			if(modifiedPages.ContainsKey(index) && modifiedPages[index].Item3)
				return modifiedPages[index].Item1;
			return index;
		}

		private void SelectPageAfterFileNameChange(int n)
		{
			try
			{
				Cursor = Cursors.WaitCursor;
				SelectedIndex = n;
				if(n < 0)
					return;
				IntPtr fileptr = IntPtr.Zero;
				Bitmap bmp = null;
				Tiff.PageInfo pi = null;
				if(IntPtr.Zero == libTiff.TiffHandle)
				{
					try
					{
						fileptr = libTiff.TiffOpenRead(ref fileName, out bmp, false);
						if(fileptr == IntPtr.Zero && bmp != null)
							pi = new PageInfo { Image = bmp };
						else
							pi = GetImageFromTiff(fileptr, SelectedIndex);
					}
					finally
					{
						if(fileptr != IntPtr.Zero)
							libTiff.TiffCloseRead(ref fileptr);
					}
				}
				else
					pi = GetImageFromTiff(libTiff.TiffHandle, SelectedIndex);

				SelectPage(pi);
				if(previews == null)
					previews = new SynchronizedCollection<KeyValuePair<Bitmap, bool>>();
				if(previews.Count != PageCount)
				{
					previews.Clear();
					for(int i = 0; i < PageCount; i++)
						if(i == n)
							previews.Add(new KeyValuePair<Bitmap, bool>(GetPreview(pi.Image, true), pi.Annotation != null));
						else
							previews.Add(new KeyValuePair<Bitmap, bool>(null, false));
				}
				else
					previews[SelectedIndex] =new KeyValuePair<Bitmap, bool>(GetPreview(pi.Image, true), pi.Annotation != null);
				if(fileptr != IntPtr.Zero)
					libTiff.TiffCloseRead(ref fileptr);
				Refresh();
				OnPageChange();
				newPage = 0;
				Cursor = Cursors.Default;
				if(animatedImage != null)
					OnImageLoad();
			}
			catch(Exception ex)
			{
				Tiff.LibTiffHelper.WriteToLog(ex);
			}
		}

		/// <summary>
		/// Перезагрузка контрола
		/// </summary>
		public void RefreshPages()
		{
			FileName = fileName;
		}

		public void SetSelection()
		{
			//Заглушка
		}

		/// <summary>
		/// Проверка можно ли масштабировать по фрагменту
		/// </summary>
		public bool RectDrawn()
		{
			if(TypeWorkAnimatedImage == TypeWorkImage.SelectionMode && SelectionModeRectangle != Rectangle.Empty)
				return true;
			else
				return false;
		}

		public bool SetImagePalette(int type)
		{
			bool result = false;
			switch(type)
			{
				case 1:
					CurrentPixelFormat = PixelFormat.Format24bppRgb;
					result = true;
					break;
				case 2:
					CurrentPixelFormat = PixelFormat.Format8bppIndexed;
					result = true;
					break;
				case 3:
					CurrentPixelFormat = PixelFormat.Format1bppIndexed;
					result = true;
					break;
			}
			if(result)
			{
				IsRefreshBitmap = true;
				Invalidate(rectAnimatedImage);
			}
			return result;
		}

		public int GetImagePalette()
		{
			switch(CurrentPixelFormat)
			{
				case PixelFormat.Format24bppRgb:
					return 1;

				case PixelFormat.Format8bppIndexed:
					return 2;

				case PixelFormat.Format1bppIndexed:
					return 3;
			}
			return 0;
		}

		protected int sin = 4;

		public bool SetDisplayScaleAlgorithm(int type)
		{
			bool result = false;
			switch(type)
			{
				case 1:
					CurrentInterpolationMode = InterpolationMode.Low;
					result = true;
					break;
				case 2:
					CurrentInterpolationMode = InterpolationMode.High;
					result = true;
					break;
				case 3:
					CurrentInterpolationMode = InterpolationMode.NearestNeighbor;
					result = true;
					break;
			}
			if(result)
			{
				IsRefreshBitmap = true;
				Invalidate(rectAnimatedImage);
				if(CurrentInterpolationMode == InterpolationMode.High)
					sin = 8;
				else
					sin = 4;
			}
			return result;
		}

		public int GetDisplayScaleAlgorithm()
		{
			switch(CurrentInterpolationMode)
			{
				case InterpolationMode.Low:
					return 1;

				case InterpolationMode.High:
					return 2;

				case InterpolationMode.NearestNeighbor:
					return 3;
			}
			return 0;
		}

		/// <summary>
		/// Загрузка изображения из файла.
		/// <para>
		/// <note type="caution"> Нумерация <paramref name="page"/> начинается с 0</note>
		/// </para>
		/// </summary>
		/// <param name="filename">имя файла</param>
		/// <param name="page">номер страницы </param>
		public void LoadFile(string filename, int page)
		{
			if(string.IsNullOrEmpty(filename))
			{
				if(string.IsNullOrEmpty(fileName))
				{
					if(libTiff != null)
					{
						IntPtr fileHandle = IntPtr.Zero;
						if(IntPtr.Zero != libTiff.TiffHandle)
							libTiff.TiffCloseRead(ref fileHandle);
					}
					return;
				}
			}
			else
			{
				this.Cursor = Cursors.WaitCursor;
			}

			bool needFeedTo = false;

			if(filename == fileName && !string.IsNullOrEmpty(fileName))
			{
				FileInfo tempfi = new FileInfo(filename);

				if(fi != null && tempfi.Length == fi.Length && tempfi.LastWriteTime == fi.LastWriteTime)
				{
					if(!(HaveVirtualRotation || RotationChanged))
					{
						if(this.fitValue < 3)
							FitTo(fitValue, true);
						else
							CalculationForDrawImageAfterSelectRotateScale(1.0);

						return;
					}
					else
					{
						needFeedTo = true;
					}
				}
			}

			Clear();

			fileName = filename ?? string.Empty;

			if(libTiff != null)
			{
				if(IntPtr.Zero != libTiff.TiffHandle)
				{
					IntPtr fileHandle = IntPtr.Zero;
					libTiff.TiffCloseRead(ref fileHandle);
				}//закрываем хендл, если был открыт

				if(!string.IsNullOrEmpty(filename) && File.Exists(filename))
				{
					bool isPages = CalculationForDraw();
					fi = new FileInfo(filename);

					SelectedIndex = page;

					isSuccessfullyChangePage = false;
					if(countPreview < COUNT_SYNC_LOAD_PAGE)
					{
						if(this.InvokeRequired)
							this.BeginInvoke((MethodInvoker)(delegate()
							{
								SelectPageAfterFileNameChange(page);
								Refresh();
							}));
						else
						{
							SelectPageAfterFileNameChange(page);
							Refresh();
						}
					}
					else
					{
						GetPagesStart(beginThumbnailImageIndex, maxCountVisibleThumbnailImages, listvis);
						Refresh();
					}
					if(isPages && IS_ENABLE_AUTOLOAD_PAGE)//это не тиф, страниц в нем нету
						PageLoaderStart();
				}
				else
					Refresh();

			}

			if(needFeedTo)
			{
				if(this.fitValue < 3)
					FitTo(fitValue, true);
				else
					CalculationForDrawImageAfterSelectRotateScale(1.0);
			}

		    if(ResetEventGetPagesStart.WaitOne(60000, true))
				OnFileNameChanged();
			this.Cursor = TiffCursors.Hand;
		}

		/// <summary>
		/// Изменение позиции страницы
		/// <para>
		/// <note type="caution"> Нумерация <paramref name="page"/> начинается с 1</note>
		/// </para>
		/// </summary>
		/// <param name="page">номер страницы</param>
		public virtual void MovePage(int page)
		{
			if(FileName == null || page < 1 || page >= PageCount || PageCount < 2)
				return;
			if(modifiedMarks)
			{
				newPage = SelectedIndex;
				OnNeedSave(ChangePage);
			}
			int index = page - 1;
			int newIndex = index + 1;
			Tuple<int, int, bool> newFirstValue = new Tuple<int, int, bool>(newIndex, 0, true);
			bool moveIndex = SelectedIndex == index;
			Tuple<int, int, bool> newSecondValue = new Tuple<int,int,bool> (index,0, true);
			if(modifiedPages.ContainsKey(newIndex))
				newFirstValue = modifiedPages[newIndex];
			if(modifiedPages.ContainsKey(index))
				newSecondValue = modifiedPages[index];
			if(modifiedPages.ContainsKey(newIndex))
			{
				newFirstValue = modifiedPages[newIndex];
				if(newIndex == newSecondValue.Item1 && newSecondValue.Item2 == 0 && newSecondValue.Item3)
					modifiedPages.Remove(newIndex);
				else
					modifiedPages[newIndex] = newSecondValue;
			}
			else
				modifiedPages.Add(newIndex, newSecondValue);
			if(modifiedPages.ContainsKey(index))
			{
				if(index == newFirstValue.Item1 && newFirstValue.Item2 == 0 && newFirstValue.Item3)
					modifiedPages.Remove(index);
				else
					modifiedPages[index] = newFirstValue;
			}
			else
				modifiedPages.Add(index, newFirstValue);
			System.Collections.Generic.KeyValuePair<System.Drawing.Bitmap, bool> p;
			if(previews.Count > newIndex)
			{
				p = previews[newIndex];
				previews[newIndex] = previews[index];
				previews[index] = p;
			}
			else
			{
				Tiff.PageInfo pi = null;
				if(IntPtr.Zero == libTiff.TiffHandle)
				{
					IntPtr fileptr = IntPtr.Zero;
					try
					{
						Bitmap bmp = null;
						fileptr = libTiff.TiffOpenRead(ref fileName, out bmp, false);
						if(fileptr == IntPtr.Zero && bmp != null)
							pi = new PageInfo { Image = bmp };
						else
							pi = GetImageFromTiff(fileptr, newIndex);
					}
					finally
					{
						if(fileptr != IntPtr.Zero)
							libTiff.TiffCloseRead(ref fileptr);
					}
				}
				else
					pi = GetImageFromTiff(libTiff.TiffHandle, SelectedIndex);
				p = new KeyValuePair<Bitmap, bool>(GetPreview(pi.Image, true), pi.Annotation != null);
				previews.Insert(newIndex, previews[index]);
				previews[index] = p;
			}
			listvis = FillCoordinatesVisiblePreview(true);
			MoveScrol(moveIndex ? newIndex : index, false);
			SelectPageAfterFileNameChange(moveIndex ? newIndex : index);
			Refresh();
		}

		/// <summary>
		/// Масштаб по размерам
		/// </summary>
		/// <param name="option"></param>
		/// <param name="repaint"></param>
		public void FitTo(short option, bool repaint)
		{
			FitTo(option, repaint, true);
		}

		/// <summary>
		/// Масштаб по размерам
		/// </summary>
		/// <param name="option"></param>
		/// <param name="repaint"></param>
		/// <param name="dopaint"></param>
		public void FitTo(short option, bool repaint, bool dopaint)
		{
			fitValue = option;
			bool newZ = true;
			if(animatedImage == null)
				return;
			if(repaint)
			{
				double oldZoom = zoom;
				try
				{
					newZ = animatedImage.HorizontalResolution != animatedImage.VerticalResolution;
					if(!newZ)
						newZ = animatedImage.HorizontalResolution != ppi;
				}
				catch
				{
					newZ = false;
				}
				try
				{
					switch(option)
					{
						case 0:
							{
								if(newZ)
								{
									double zw = ((double)rectAnimatedImage.Width * animatedImage.HorizontalResolution / ppi) / (double)animatedImage.Width;
									double zh = ((double)rectAnimatedImage.Height * animatedImage.VerticalResolution / ppi) / (double)animatedImage.Height;
									if(zw > zh)
										zoom = zh;
									else
										zoom = zw;
								}
								else
								{
									double zw = (double)rectAnimatedImage.Width / (double)animatedImage.Width;
									double zh = (double)rectAnimatedImage.Height / (double)animatedImage.Height;
									if(zw > zh)
										zoom = zh;
									else
										zoom = zw;
								}
								CalculationForDrawImageAfterSelectRotateScale(zoom / oldZoom);
								if(dopaint)
									Invalidate(rectAnimatedImage, true);
								break;
							}
						case 1:
							{
								if(newZ)
								{
									double zw = ((double)rectAnimatedImage.Width * animatedImage.HorizontalResolution / ppi) / (double)animatedImage.Width;
									double zoomSizeW = (double)animatedImage.Width * zw;
									double zoomSizeH = (double)animatedImage.Height * zw * animatedImage.VerticalResolution / animatedImage.HorizontalResolution;
									if(zoomSizeH > (double)rectAnimatedImage.Height * animatedImage.VerticalResolution / ppi)
										zw = (((double)rectAnimatedImage.Width - 20) * animatedImage.HorizontalResolution / ppi) / (double)animatedImage.Width;
									zoom = zw;
								}
								else
								{
									double zw = (double)rectAnimatedImage.Width / (double)animatedImage.Width;
									double zoomSizeW = (double)animatedImage.Width * zw;
									double zoomSizeH = (double)animatedImage.Height * zw;
									if(zoomSizeH > (double)rectAnimatedImage.Height)
										zw = ((double)rectAnimatedImage.Width - 20) / (double)animatedImage.Width;
									zoom = zw;
								}
								CalculationForDrawImageAfterSelectRotateScale(zoom / oldZoom);
								if(dopaint)
									Invalidate(rectAnimatedImage, true);
								break;
							}
						case 2:
							{
								if(newZ)
								{
									double zh = ((double)rectAnimatedImage.Height * animatedImage.VerticalResolution / ppi) / animatedImage.Height;
									double zoomSizeW = animatedImage.Width * zh * animatedImage.HorizontalResolution / animatedImage.VerticalResolution;
									double zoomSizeH = animatedImage.Height * zh;
									if(zoomSizeW > (double)rectAnimatedImage.Width * animatedImage.HorizontalResolution / ppi)
										zh = (((double)rectAnimatedImage.Height - 20) * animatedImage.VerticalResolution / ppi) / animatedImage.Height;
									zoom = zh;
								}
								else
								{
									double zh = rectAnimatedImage.Height / (double)animatedImage.Height;
									double zoomSizeW = animatedImage.Width * zh;
									double zoomSizeH = animatedImage.Height * zh;
									if(zoomSizeW > rectAnimatedImage.Width)
										zh = ((double)rectAnimatedImage.Height - 20) / animatedImage.Height;
									zoom = zh;
								}
								CalculationForDrawImageAfterSelectRotateScale(zoom / oldZoom);
								if(dopaint)
									Invalidate(rectAnimatedImage, true);
								break;
							}
						default:
							{
								OnErrorMessage(new Exception("FitTo error"));
								break;
							}
					}
				}
				catch(ArgumentException ex)
				{
					Tiff.LibTiffHelper.WriteToLog(ex, "FitTo error option = " + option.ToString() + ", repaint = " + repaint.ToString() + ",  dopaint = " + dopaint.ToString() + "\nFitTo error дополнительно animatedImage.Width = " + animatedImage.Width.ToString() + ", rectAnimatedImage.Width = " + rectAnimatedImage.Width.ToString());
				}
			}
		}

		/// <summary>
		/// Получить коллекцию с выделенной картинкой 
		/// </summary>
		/// <returns></returns>
		public Image CreateImageListFromSelectedImage()
		{
			try
			{
				Bitmap image = new Bitmap(SelectionModeRectangle.Width, SelectionModeRectangle.Height);
				image.SetResolution(animatedImage.HorizontalResolution, animatedImage.VerticalResolution);
				using(Graphics g = Graphics.FromImage(image))
				{
					g.SmoothingMode = SmoothingMode.HighQuality;
					g.InterpolationMode = CurrentInterpolationMode;
					g.DrawImage(animatedImage, new Rectangle(0, 0, image.Width, image.Height), SelectionModeRectangle, GraphicsUnit.Pixel);
				}
				image = libTiff.ConvertTo(animatedImage.PixelFormat, image);
				return image;
			}
			catch(Exception ex)
			{
				OnErrorMessage(new Exception("CreateSelectedImage error", ex));
			}
			return null;
		}

		/// <summary>
		/// Сохраняет выделенную часть в отдельный документ
		/// </summary>
		/// <returns></returns>
		public string CreateSelectedImage()
		{
			return CreateSelectedImage(animatedImage.PixelFormat);
		}

		/// <summary>
		/// Сохраняет выделенную часть в отдельный документ
		/// </summary>
		/// <returns></returns>
		public string CreateSelectedImage(PixelFormat format)
		{
			try
			{

				string tempImage = Path.Combine(Path.GetTempPath(), "~" + Guid.NewGuid().ToString() + ".tif");
				Bitmap image = new Bitmap(SelectionModeRectangle.Width, SelectionModeRectangle.Height);
				using(Graphics g = Graphics.FromImage(image))
				{
					g.SmoothingMode = SmoothingMode.HighQuality;
					g.InterpolationMode = CurrentInterpolationMode;
					g.DrawImage(animatedImage, new Rectangle(0, 0, image.Width, image.Height), SelectionModeRectangle, GraphicsUnit.Pixel);
				}
				image = libTiff.ConvertTo(format, image);
				bool isIndexed = image.PixelFormat == PixelFormat.Format1bppIndexed;
				image.SetResolution(animatedImage.VerticalResolution, animatedImage.HorizontalResolution);
				libTiff.SaveBitmapToFile(tempImage, image, true);
				return tempImage;
			}
			catch(Exception ex)
			{
				OnErrorMessage(new Exception("CreateSelectedImage error", ex));
			}
			return "";
		}

		/// <summary>
		/// Поворот влево
		/// </summary>
		public virtual bool RotateLeft()
		{
			if(animatedImage != null)
			{
				SetModified(true);
				int oldImageWidth = animatedImage.Width;
				int oldImageHeiight = animatedImage.Height;
				try
				{
					animatedImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
				}
				catch(Exception ex)
				{
					Tiff.LibTiffHelper.WriteToLog(ex, "Не удалось повернуть изображение");
					return false;
				}

				if(modifiedPages.ContainsKey(SelectedIndex))
					modifiedPages[SelectedIndex] = new Tuple<int, int, bool>(modifiedPages[SelectedIndex].Item1, (modifiedPages[SelectedIndex].Item2 + 270) % 360, modifiedPages[SelectedIndex].Item3);
				else
					modifiedPages.Add(SelectedIndex, new Tuple<int, int, bool>(SelectedIndex, 270, true));
				if(SelectedIndex > -1 && previews != null)
				{
					if(previews.Count > SelectedIndex)
					{
						if(previews[SelectedIndex].Key != null)
							previews[SelectedIndex].Key.Dispose();
						previews.RemoveAt(SelectedIndex);
					}
					previews.Insert(SelectedIndex, new KeyValuePair<Bitmap, bool>(GetPreview(animatedImage, true), this.tiffAnnotation != null));
				}
				CalculationForDrawImageAfterSelectRotateScale(1.0);
				listvis = FillCoordinatesVisiblePreview(true);
				TiffAnnotation annotation = this.tiffAnnotation;
				if(annotation != null)
					annotation.AnnotationRotate(true, oldImageWidth, oldImageHeiight);

				isLockThumbnailImages = false;
				IsRefreshBitmap = true;
				if(fitValue < 3)
				{
					FitTo(fitValue, true, false);
				}
				Refresh();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Поворот вправо
		/// </summary>
		/// <returns></returns>
		public virtual bool RotateRight()
		{
			if(animatedImage != null)
			{
				SetModified(true);
				int oldImageWidth = animatedImage.Width;
				int oldImageHeiight = animatedImage.Height;
				try
				{
					animatedImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
				}
				catch(Exception ex)
				{
					LibTiffHelper.WriteToLog(ex, "Не удалось повернуть изображение");
					return false;
				}
				if(modifiedPages.ContainsKey(SelectedIndex))
					modifiedPages[SelectedIndex] = new Tuple<int, int, bool>(modifiedPages[SelectedIndex].Item1, (modifiedPages[SelectedIndex].Item2 + 90) % 360, modifiedPages[SelectedIndex].Item3);
				else
					modifiedPages.Add(SelectedIndex, new Tuple<int, int, bool>(SelectedIndex, 90, true));
				if(SelectedIndex > -1 && previews != null)
				{
					if(previews.Count > SelectedIndex)
					{
						if(previews[SelectedIndex].Key != null)
							previews[SelectedIndex].Key.Dispose();
						previews.RemoveAt(SelectedIndex);
					}
					previews.Insert(SelectedIndex, new KeyValuePair<Bitmap, bool>(GetPreview(animatedImage, true), this.tiffAnnotation != null));
				}
				CalculationForDrawImageAfterSelectRotateScale(1.0);
				listvis = FillCoordinatesVisiblePreview(true);
				TiffAnnotation annotation = this.tiffAnnotation;
				if(annotation != null)
					annotation.AnnotationRotate(false, oldImageWidth, oldImageHeiight);

				isLockThumbnailImages = false;
				IsRefreshBitmap = true;
				if(fitValue < 3)
				{
					FitTo(fitValue, true, false);
				}
				Refresh();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Начало создания фигуры
		/// </summary>
		/// <param name="figure">Тип создаваемой фигуры</param>
		public void BeginCreationFigure(Figures figure)
		{
			if(animatedImage == null)
				return;
			if(IsNotesAllHide)
				ShowAnnotationGroup(GetCurrentAnnotationGroup());
			TypeWorkAnimatedImage = TypeWorkImage.CreateNotes;

			SelectedBitmaps.Clear();
			notesToSelectedRectangles.Clear();

			selectedRectangles = null;
			if(SelectedBitmap != null)
			{
				SelectedBitmap.Selected = false;
				SelectedBitmap = null;
			}
			ClearSelectedNotes();

			AnnotationState = AnnotationsState.None;
			Invalidate();
			switch(figure)
			{
				case Figures.FilledRectangler:
					Cursor = TiffCursors.FRect;
					UserAction = UsersActionsTypes.DrawFRect;
					break;
				case Figures.Marker:
					Cursor = TiffCursors.Marker;
					UserAction = UsersActionsTypes.DrawMarker;
					break;
				case Figures.HollowRectangle:
					Cursor = TiffCursors.HRect;
					UserAction = UsersActionsTypes.DrawHRect;
					break;
				case Figures.Text:
					Cursor = TiffCursors.RectText;
					UserAction = UsersActionsTypes.DrawRectText;
					break;
				case Figures.Note:
					Cursor = TiffCursors.Note;
					UserAction = UsersActionsTypes.DrawNote;
					break;
				case Figures.EmbeddedImage:
					UserAction = UsersActionsTypes.DrawImage;
					Cursor = Cursors.NoMove2D;
					break;
			}
		}

		/// <summary>
		/// Сохраняет часть страниц из документа
		/// </summary>
		public string SavePart(int startPage, int endPage)
		{
			string tempFileName = "";
			try
			{
				tempFileName = System.IO.Path.GetTempFileName();
				if(File.Exists(tempFileName))
					File.Delete(tempFileName);
				libTiff.SavePart(fileName, startPage - 1, endPage - startPage + 1, tempFileName, null);
			}
			catch(Exception ex)
			{
				OnErrorMessage(new Exception("SavePart error", ex));
			}
			return tempFileName;
		}

		/// <summary>
		/// получение количества страниц из файла
		/// </summary>
		/// <param name="fileName">имя файла</param>
		/// <returns>количество страниц</returns>
		public int GetFilePagesCount(string fileName)
		{
			return libTiff.GetCountPages(fileName);
		}

		/// <summary>
		/// Удаление из документа части страниц
		/// </summary>
		public void DelPart(string fileName, int startPage, int endPage, bool isClearImage, bool isColorSave)
		{
			string tempFileName = (string)fileName.Clone();
			int tempIndex = 0;//Серега опять решил вернуть опцию, я решил так.
			bool compare = false;
			try
			{
				compare = (this.fileName == tempFileName);

				tempFileName = libTiff.DeletePart(fileName, startPage - 1, endPage - startPage + 1, tempFileName);
				if(!fileName.Equals(tempFileName) && tempFileName != null)
				{
					File.Copy(tempFileName, fileName, true);
					File.Delete(tempFileName);
				}
				if(compare)
				{
					Clear();
					this.fileName = fileName;
					countPreview = libTiff.GetCountPages(this.fileName);
					CalculationForDraw();
					SelectedIndex = tempIndex;
					if(SelectedIndex >= countPreview)
						SelectedIndex = countPreview - 1;
					isLockThumbnailImages = false;
					IsRefreshBitmap = true;
					if(SelectedIndex >= 0)
						SelectPageAfterFileNameChange(SelectedIndex);
					if(System.IO.File.Exists(this.fileName))
						fi = new FileInfo(this.fileName);
					Refresh();

				}
				else if(isClearImage)
					Clear();
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.ToString());
				Console.WriteLine(startPage.ToString() + ", " + endPage.ToString() + " , " + (endPage - startPage + 1).ToString());
			}
		}
		/// <summary>
		/// Удаление из документа части страниц
		/// </summary>
		public void DelPart(string fileName, int startPage, int endPage, bool isClearImage)
		{
			DelPart(fileName, startPage, endPage, isClearImage, true);
		}

		/// <summary>
		/// Сохранение текущих изменений
		/// </summary>
		public void Save()
		{
			this.SaveCurrentPage();
			ResetModified();
			fi = new FileInfo(fileName);
		}
		/// <summary>
		/// Сохранение текущих изменений(конвертит в 1bpp, если newFormat не PixelFormat.Format8bppIndexed)
		/// Метод предназначен для сохранения картинок 8bpp для секурити в общем
		/// </summary>
		public void Save(PixelFormat newFormat)
		{
			if(string.IsNullOrEmpty(fileName))
				return;
			if(newFormat == PixelFormat.Format1bppIndexed || newFormat == PixelFormat.Format8bppIndexed || newFormat == PixelFormat.Format24bppRgb)
			{
				if(!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
					fi = new FileInfo(fileName);
			}
		}

		/// <summary>
		/// Сохранение картинок. Этот метод применять только для сохранения текущих изменений.
		/// Он получает оставшиеся страницы, если не получены, и пересохраняет текущий файл
		/// </summary>
		private void SaveCurrentPage()
		{
			if(!VerifyFile(fileName))
				return;
			string tempFileName = fileName + ".tmp";
			int n = 0;
			while(File.Exists(tempFileName))
			{
				tempFileName = fileName + ".t" + n++.ToString();
			}
			libTiff.SaveBitmapToFile(fileName, tempFileName, SelectedIndex, new Tiff.PageInfo() { Image = animatedImage, Annotation = tiffAnnotation != null ? tiffAnnotation.GetAnnotationBytes(saveStampsInternal) : null }, true, modifiedPages);
			if(File.Exists(tempFileName))
			{
				File.Copy(tempFileName, fileName, true);
				File.Delete(tempFileName);
			}
			fi = new FileInfo(fileName);
		}

		/// <summary>
		/// Сохранение картинок. В новый файл. Получает картинки из текущего файла, если не получены. Сохраняет как есть
		/// </summary>
		public bool SaveAs(string newFileName)
		{
			return SaveAs(newFileName, false);
		}

		/// <summary>
		/// Сохранение картинок. В новый файл. Получает картинки из текущего файла, если не получены
		/// </summary>
		public bool SaveAs(string newFileName, bool isColorSave)
		{
			bool result = true;
			try
			{
				if(VerifyFile(fileName))
				{
                    IntPtr fileptr = IntPtr.Zero;

				    if (IntPtr.Zero == libTiff.TiffHandle)
                    {
                        try
                        {
                            Bitmap bmp;
                            fileptr = libTiff.TiffOpenRead(ref fileName, out bmp, false);

							var changedImages = new Dictionary<int, PageInfo>();

							for(int i = 0; i < PageCount; i++)
							{
								int page = GetCurrentIndex(i);
								if(i == SelectedIndex &&  modifiedMarks)
								{
									changedImages[i] = new PageInfo { Image = animatedImage, Annotation = (tiffAnnotation != null) ? tiffAnnotation.GetAnnotationBytes(false) : null };
                                    continue;
                                }

                                UpdatePageRotation(i);
                                var rotation = GetPageRotation(i);

                                if (rotation != 0)
                                {
                                    var pi = GetImageFromTiff(fileptr, i);
									changedImages[i] = pi;
                                }
                            }

							result = libTiff.SaveBitmapToFile(fileName, newFileName, changedImages, true, modifiedPages);
							if(result)
							{
								CleanRotation();
								modifiedPages.Clear();
							}
                        }
                        finally
                        {
                            if (fileptr != IntPtr.Zero)
                                libTiff.TiffCloseRead(ref fileptr);
                        }
                    }

					// result = libTiff.SaveBitmapToFile(fileName, newFileName, Page - 1, new PageInfo { Image = animatedImage, Annotation = (tiffAnnotation != null) ? tiffAnnotation.GetAnnotationBytes(false) : null }, true);

					fi = new FileInfo(newFileName);
				}
				else
					fi = new FileInfo(fileName);
			}
			catch(Exception ex) { Log.Logger.WriteEx(ex); }
			finally
			{
				SetModified(false);
			}

			return result;
		}

		/// <summary>
		/// Сохранение текущей картинки в новый файл с конвертацией. 
		/// </summary>
		public void SaveWithConvert(string newFileName, PixelFormat newFormat)
		{
			//SaveCurrentPageToImageList();
			//libTiff.GetAbsentPages(fileName, ref pages, true);
			//if (newFormat == PixelFormat.Format1bppIndexed || newFormat == PixelFormat.Format8bppIndexed || newFormat == PixelFormat.Format24bppRgb)
			//{
			//    foreach (Tiff.ImageInfo img in pages)
			//    {
			//        img.Image = this.ConvertTo(newFormat, img.Image, false);
			//    }
			//    libTiff.SaveBitmapsCollectionToFile(newFileName, pages, true);
			//}
		}

		/// <summary>
		/// Сохранение текущей картинки в новый файл с конвертацией. В новый файл.
		/// </summary>
		public void SaveCurrentPageAs(string newFileName, PixelFormat newFormat)
		{
			if(animatedImage != null && newFormat != animatedImage.PixelFormat)
			{
				if(newFormat == PixelFormat.Format1bppIndexed || newFormat == PixelFormat.Format8bppIndexed || newFormat == PixelFormat.Format24bppRgb)
				{
					animatedImage = libTiff.ConvertTo(newFormat, animatedImage);
					List<Bitmap> imgs = new List<Bitmap>();
					imgs.Add(animatedImage);
					List<byte[]> anns = new List<byte[]>();
					anns.Add(tiffAnnotation != null ? tiffAnnotation.GetAnnotationBytes(saveStampsInternal) : null);
					libTiff.SaveBitmapsCollectionToFile(newFileName, imgs, anns, true);
				}
			}
		}

		public bool BurnInEmbeddedImages(bool removeMark)
		{
			if(tiffAnnotation != null && tiffAnnotation.MarkAttributes != null)
			{
				var images = tiffAnnotation.MarkAttributes.FindAll(x => x.UType == TiffAnnotation.AnnotationMarkType.ImageEmbedded);
				if(images.Count > 0)
				{
					Bitmap image = new Bitmap(animatedImage.Width, animatedImage.Height, PixelFormat.Format24bppRgb);
					using(Graphics g = Graphics.FromImage(image))
					{
						g.DrawImage(animatedImage, 0, 0, animatedImage.Width, animatedImage.Height);
						foreach(TiffAnnotation.OIAN_MARK_ATTRIBUTES atrr in images)
						{
							TiffAnnotation.ImageEmbedded img = new TiffAnnotation.ImageEmbedded(atrr, tiffAnnotation);
							g.DrawImage(img.Img, img.LrBounds.Location.X, img.LrBounds.Location.Y, img.LrBounds.Size.Width, img.LrBounds.Size.Height);
							tiffAnnotation.MarkAttributes.Remove(atrr);
							img.Img.Dispose();
							img.Img = null;
						}
						image = libTiff.ConvertTo(animatedImage.PixelFormat, image);
						image.SetResolution(animatedImage.HorizontalResolution, animatedImage.VerticalResolution);
						animatedImage.Dispose();
						animatedImage = image;
					}
					if(removeMark)
						tiffAnnotation.MarkAttributes.Clear();
					return true;
				}
				if(removeMark && tiffAnnotation.MarkAttributes.Count > 0)
				{
					tiffAnnotation.MarkAttributes.Clear();
					return true;
				}
			}
			return false;
		}

		#endregion

		#region Методы обработчики и диспетчеры и т.д.

		/// <summary>
		/// Проверка изменений в файле 
		/// </summary>
		private bool VerifyFile(string fileName)
		{
			if(!isVerifyFile || string.IsNullOrEmpty(fileName))
				return true;
			string tempFileName = fileName;
			bool result = false;
			if(File.Exists(tempFileName))
			{
				FileInfo fiNew = new FileInfo(tempFileName);
				if(fi != null && (fiNew.Length != fi.Length || fiNew.LastWriteTime != fi.LastWriteTime || filesLenght != fiNew.Length || filesData != fiNew.LastWriteTime))
				{
					if(SHOW_FILE_CHANGED_MESSAGE)
						MessageBox.Show("Изображение было изменено другим пользователем. После закрытия окна изображение будет перегружено.", "Ошибка файла");
					SetModified(false);
					SetModifiedMarks(false);
					FileName = tempFileName;
				}
				else
					result = true;
			}
			else
				DialogForSaveUnexistFile(tempFileName);
			return result;
		}

		/// <summary>
		/// Вывод диалогов, если файл уже не существует
		/// </summary>
		private void DialogForSaveUnexistFile(string tempFileName)
		{
			if(fileName == tempFileName)
				MessageBox.Show("Файл на диске был удален.");
			SetModified(false);
			SetModifiedMarks(false);
			FileName = null;
		}

		/// <summary>
		/// Очитска контрола
		/// </summary>
		protected void Clear()
		{
			if(previews != null)
			{
				for(int i = 0; i < previews.Count; i++)
				{
					if(previews[i].Key != null)
						previews[i].Key.Dispose();
				}
				previews.Clear();
				previews = null;
			}
			fi = null;
			SelectedIndex = -1;
			SelectionModeRectangle = Rectangle.Empty;
			ResetModified();
			externalScroll = false;
			anotherFormat = false;
			newPage = 0;
			fileName = null;
			globalOffsetYThumbnail = 0;
			beginThumbnailImageIndex = 0;
			if(animatedImage != null)
			{
				animatedImage.Dispose();
				animatedImage = null;
			}
			typeWorkAnimatedImage = TypeWorkImage.MoveImage;
			if(tiffAnnotation != null)
				tiffAnnotation.ModifiedFigure -= new TiffAnnotation.ClearBitmapHandler(annotation_ModifiedFigure);
			tiffAnnotation = null;
			if(ThumbnailImagesBitmap != null)
			{
				ThumbnailImagesBitmap.Dispose();
				ThumbnailImagesBitmap = null;
			}
			scrollX = 0;
			scrollY = 0;
			scrollThumbnailImage.Visible = false;
			scrollImageHorizontal.Visible = false;
			scrollImageVertical.Visible = false;
			scrollThumbnailImage.Value = 0;
			scrollImageHorizontal.Value = 0;
			scrollImageVertical.Value = 0;
		}

		int countWithRow = 1;//количество в ряду или колонне(если превью топ или ботом)

		private int GetCountWithRow(int sizePreviewPanel, int sizePreview)
		{
			int tempCountWithRow = sizePreviewPanel / (sizePreview + INDENT_BETWEEN_PREVIEW);
			if(tempCountWithRow < 1)
				tempCountWithRow = 1;
			return tempCountWithRow;
		}

		private void SetAllSizes()
		{
			widthThumbnailImage = thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom ? Width : defaultWidthThumbnailImage;
			heightThumbnailImage = thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom ? defaultHeightThumbnailImage : Height;
			widthImage = thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom ? Width : Width - widthThumbnailImage - widthSplitter;//2 - размер разделителя
			heightImage = thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom ? Height - heightThumbnailImage - heightSplitter : Height;
			if(widthImage < 0)
				widthImage = 0;
			if(heightImage < 0)
				heightImage = 0;
			countWithRow = 1;
			if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Left)
			{
				rectAnimatedImage = new Rectangle(widthThumbnailImage + widthSplitter, 0, widthImage, heightImage);
				rectSplitter = new Rectangle(widthThumbnailImage + 1, 0, widthSplitter, heightImage);
				rectThumbnailPanel = new Rectangle(0, 0, widthThumbnailImage, heightThumbnailImage);
				sizePreview = 140;
				scrollThumbnailImageHorizontal.Visible = false;
				scrollThumbnailImage.Size = new Size(scrollThumbnailImage.Width, heightThumbnailImage - 2);
				countWithRow = GetCountWithRow(widthThumbnailImage, 120);
				scrollThumbnailImage.Location = new Point(widthThumbnailImage - scrollThumbnailImage.Width - 1, 1);
			}
			else if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top)
			{
				rectAnimatedImage = new Rectangle(0, heightThumbnailImage + heightSplitter, widthImage, heightImage);
				rectSplitter = new Rectangle(0, heightThumbnailImage + 1, widthImage, heightSplitter);
				rectThumbnailPanel = new Rectangle(0, 0, widthThumbnailImage, heightThumbnailImage);
				sizePreview = 120;
				scrollThumbnailImageHorizontal.Size = new Size(widthThumbnailImage - 2, scrollThumbnailImageHorizontal.Height);
				scrollThumbnailImage.Visible = false;
				countWithRow = GetCountWithRow(heightThumbnailImage, 140);
				scrollThumbnailImageHorizontal.Location = new Point(1, heightThumbnailImage - scrollThumbnailImageHorizontal.Height - 1);
			}
			else if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
			{
				rectAnimatedImage = new Rectangle(0, 0, widthImage, heightImage);
				rectSplitter = new Rectangle(0, heightImage + 1, widthImage, heightSplitter);
				rectThumbnailPanel = new Rectangle(0, rectAnimatedImage.Height + rectSplitter.Height, widthThumbnailImage, heightThumbnailImage);
				sizePreview = 100;
				scrollThumbnailImageHorizontal.Size = new Size(widthThumbnailImage - 2, scrollThumbnailImageHorizontal.Height);
				scrollThumbnailImage.Visible = false;
				countWithRow = GetCountWithRow(heightThumbnailImage, 140);
				scrollThumbnailImageHorizontal.Location = new Point(1, Height - scrollThumbnailImageHorizontal.Height - 1);
			}
			else if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Right)
			{
				rectAnimatedImage = new Rectangle(0, 0, widthImage, heightImage);
				rectSplitter = new Rectangle(widthImage + 1, 0, widthSplitter, heightImage);
				rectThumbnailPanel = new Rectangle(rectAnimatedImage.Width + rectSplitter.Width, 0, widthThumbnailImage, heightThumbnailImage);
				sizePreview = 140;
				scrollThumbnailImageHorizontal.Visible = false;
				scrollThumbnailImage.Size = new Size(scrollThumbnailImage.Width, heightThumbnailImage - 2);
				countWithRow = GetCountWithRow(widthThumbnailImage, 100);
				scrollThumbnailImage.Location = new Point(Width - scrollThumbnailImage.Width - 1, 1);
			}
		}

		/// <summary>
		/// Пересчеты для превьюшки
		/// </summary>
		/// <returns>Возвращает false  когда существеут файл, но открыть его не удалось или количество страниц 0</returns>
		private bool CalculationForDraw()
		{
			bool result = true;
			anotherFormat = false;

			beginThumbnailImageIndex = 0;
			maxCountVisibleThumbnailImages = 0;
			int countRow = 0;//количество рядов или колонн(если превью топ или ботом)
			if(!string.IsNullOrEmpty(fileName))
			{
				//общее количество страниц
				countPreview = libTiff.GetCountPages(fileName);
				if(countPreview == 0)
					result = false;
				else
				{
					if(countPreview == -1)
					{
						anotherFormat = true;
						countPreview = 1;
					}
					countRow = countWithRow == 1 ? countPreview : (int)Math.Ceiling(countPreview / (double)countWithRow);
				}
			}

			//максимальная высота всех превьюшек для скрола надо
			maxThumbnailSize = 20 + countRow * (sizePreview + INDENT_THUMBNAIL_IMAGE);

			//количество превьюшек видимых при текушей высоте контрола + 2 про запас
			if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
			{
				scrollSize = maxThumbnailSize - widthThumbnailImage;
				maxCountVisibleThumbnailImages = (widthThumbnailImage / (sizePreview + INDENT_THUMBNAIL_IMAGE)) * countWithRow + countWithRow;

				scrollThumbnailImageHorizontal.Maximum = scrollSize + (scrollSize > 180 ? 180 : scrollSize > 0 ? scrollSize : 0);
				scrollThumbnailImageHorizontal.Minimum = 0;
				scrollThumbnailImageHorizontal.Value = scrollThumbnailImageHorizontal.Minimum;
				if(scrollSize > 0)
					scrollThumbnailImageHorizontal.Visible = true;
				else
					scrollThumbnailImageHorizontal.Visible = false;
				scrollThumbnailImageHorizontal.LargeChange = scrollSize > 180 ? 180 : scrollSize > 0 ? scrollSize : 0;
				scrollThumbnailImageHorizontal.SmallChange = scrollThumbnailImageHorizontal.LargeChange;
			}
			else
			{
				scrollSize = maxThumbnailSize - heightThumbnailImage;
				maxCountVisibleThumbnailImages = (heightThumbnailImage / (sizePreview + INDENT_THUMBNAIL_IMAGE)) * countWithRow + countWithRow;
				scrollThumbnailImage.Maximum = scrollSize + (scrollSize > 180 ? 180 : scrollSize > 0 ? scrollSize : 0);
				scrollThumbnailImage.Minimum = 0;

				scrollThumbnailImage.Value = scrollThumbnailImage.Minimum;
				if(scrollSize > 0)
					scrollThumbnailImage.Visible = true;
				else
					scrollThumbnailImage.Visible = false;
				scrollThumbnailImage.LargeChange = scrollSize > 180 ? 180 : scrollSize > 0 ? scrollSize : 0;
				scrollThumbnailImage.SmallChange = scrollThumbnailImage.LargeChange;
			}

			if(maxCountVisibleThumbnailImages > countPreview)
				maxCountVisibleThumbnailImages = countPreview;
			isLockThumbnailImages = false;

			//узанаем координаты превьюшек
			listvis = FillCoordinatesVisiblePreview(true);

			if(this.fitValue <= 2)
				FitTo(fitValue, true);
			else
				CalculationForDrawImageAfterSelectRotateScale(1.0);
			return result;
		}

		/// <summary>
		/// Пересчеты после работы с картинкой
		/// </summary>
		private void CalculationForDrawImageAfterSelectRotateScale(double dz)
		{
			scrollImageHorizontal.ValueChanged -= scrollImageHorizontal_ValueChanged;
			scrollImageVertical.ValueChanged -= scrollImageVertical_ValueChanged;
			scrollImageHorizontal.Maximum = 0;
			scrollImageHorizontal.Minimum = 0;
			scrollImageVertical.Maximum = 0;
			scrollImageVertical.Minimum = 0;

			if(animatedImage != null)
			{
				zoomWigth = ImageZoom(animatedImage.Width, animatedImage.HorizontalResolution);
				zoomHeigth = ImageZoom(animatedImage.Height, animatedImage.VerticalResolution);
			}

			scrollImageHorizontal.Size = new Size(rectAnimatedImage.Width > 20 ? rectAnimatedImage.Width - 20 : rectAnimatedImage.Width, scrollImageHorizontal.Height);
			scrollImageVertical.Size = new Size(scrollImageVertical.Width, rectAnimatedImage.Height > SystemInformation.VerticalScrollBarWidth ? rectAnimatedImage.Height - SystemInformation.VerticalScrollBarWidth : rectAnimatedImage.Height);
			scrollImageHorizontal.Location = new Point(rectAnimatedImage.X + 1, rectAnimatedImage.Y + rectAnimatedImage.Height - scrollImageHorizontal.Height - 1);
			scrollImageVertical.Location = new Point(rectAnimatedImage.X + rectAnimatedImage.Width - scrollImageVertical.Width - 1, rectAnimatedImage.Y + 1);

			if(zoomWigth <= widthImage)
			{
				scrollImageHorizontal.Visible = false;
				realHeightImage = rectAnimatedImage.Height;

			}
			else
			{
				scrollImageHorizontal.Visible = true;
				realHeightImage = scrollImageHorizontal.Location.Y - rectAnimatedImage.Y;
				if(realHeightImage < 0)
					realHeightImage = 1;
			}

			if(zoomHeigth <= heightImage)
			{
				scrollImageVertical.Visible = false;
				realWidthImage = rectAnimatedImage.Width;
			}
			else
			{
				scrollImageVertical.Visible = true;
				realWidthImage = scrollImageVertical.Location.X - rectAnimatedImage.X;
				if(realWidthImage < 0)
					realWidthImage = 1;
			}

			if(animatedImage == null)
			{
				scrollImageHorizontal.Visible = false;
				scrollImageVertical.Visible = false;
			}
			if(scrollImageHorizontal.Visible && !scrollImageVertical.Visible)
				scrollImageHorizontal.Width += 19;
			if(!scrollImageHorizontal.Visible && scrollImageVertical.Visible)
				scrollImageVertical.Height += 19;
			if(animatedImage != null)
			{
				if(scrollImageVertical.Visible)
				{
					scrollImageVertical.Maximum = Math.Abs(zoomHeigth);
					scrollImageVertical.Minimum = 0;
				}
				if(scrollImageHorizontal.Visible)
				{
					scrollImageHorizontal.Maximum = Math.Abs(zoomWigth);
					scrollImageHorizontal.Minimum = 0;
				}
			}
			scrollImageHorizontal.LargeChange = scrollImageHorizontal.Maximum > realWidthImage ? realWidthImage : scrollImageHorizontal.Maximum;
			scrollImageVertical.LargeChange = scrollImageVertical.Maximum > realHeightImage ? realHeightImage : scrollImageVertical.Maximum;

			scrollImageHorizontal.ValueChanged += scrollImageHorizontal_ValueChanged;
			scrollImageVertical.ValueChanged += scrollImageVertical_ValueChanged;


			if(scrollX < 0 && scrollImageHorizontal.Maximum > -scrollX * dz)
				scrollImageHorizontal.Value = (scrollImageHorizontal.Maximum - scrollImageHorizontal.LargeChange > -scrollX * dz) ? -(int)(scrollX * dz) : scrollImageHorizontal.Maximum - scrollImageHorizontal.LargeChange;
			else
				scrollX = 0;

			if(scrollY < 0 && scrollImageVertical.Maximum > -scrollY * dz)
				scrollImageVertical.Value = (scrollImageVertical.Maximum - scrollImageVertical.LargeChange > -scrollY * dz) ? -(int)(scrollY * dz) : scrollImageVertical.Maximum - scrollImageVertical.LargeChange;
			else
				scrollY = 0;
		}

		private void scrollImageVertical_ValueChanged(object sender, EventArgs e)
		{
			if(AnnotationState == AnnotationsState.CreateText || AnnotationState == AnnotationsState.EditText)
			{
				EndEditFiguresText();
				IsRefreshBitmap = true;
				Invalidate(rectAnimatedImage);
			}
			if(!needScrollY)
			{
				scrollY = -scrollImageVertical.Value;
				Invalidate(this.rectAnimatedImage);
			}
		}

		private void scrollImageHorizontal_ValueChanged(object sender, EventArgs e)
		{
			if(AnnotationState == AnnotationsState.CreateText || AnnotationState == AnnotationsState.EditText)
			{
				EndEditFiguresText();
				IsRefreshBitmap = true;
				Invalidate(rectAnimatedImage);
			}
			if(!needScrollX)
			{
				scrollX = -scrollImageHorizontal.Value;
				Invalidate(this.rectAnimatedImage);
			}
		}

		private void scrollThumbnailImage_Scroll(object sender, ScrollEventArgs e)
		{

			if(ControlTypeWork.DrawShadowPreview == (TypeWork & ControlTypeWork.DrawShadowPreview))
			{

				if(e.Type == ScrollEventType.ThumbTrack)
				{
					isScrollThumbnailImagesForShadow = true;
				}
				else if(e.Type == ScrollEventType.EndScroll)
				{
					isScrollThumbnailImagesForShadow = false;
					this.Cursor = Cursors.WaitCursor;
				}
			}
			ScrollValueChanged(e.NewValue);

		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if(!this.Disposing)
			{
				CalculationForDraw();
				ScrollValueChanged(scrollThumbnailImage.Value);
				Refresh();
			}
		}

		private delegate bool GetPagesHandler(int first, int length, string fileName, SynchronizedCollection<VisiblePreview> listvis);

		private delegate bool ActionWithFile(String fileName);

		private delegate void ActionWithPage(int n);

		private int _IsGetPagesStart = 0;
		System.Threading.ManualResetEvent ResetEventGetPagesStart = new System.Threading.ManualResetEvent(true);

		/// <summary>
		/// Получение страниц из файла
		/// <para>
		/// <note type="caution"> Нумерация <paramref name="first"/> начинается с 0</note>
		/// </para>
		/// </summary>
		/// <param name="first">номер первой страницы</param>
		/// <param name="length">количество страниц</param>
		/// <param name="listvis">коллекция данных о видемых превью</param>
		private void GetPagesStart(int first, int length, SynchronizedCollection<VisiblePreview> listvis)
		{
			if(String.IsNullOrEmpty(this.fileName))
				return;
			System.Threading.Interlocked.Increment(ref _IsGetPagesStart);
			GetPagesHandler handler = new GetPagesHandler(GetPagesBegin);
			rwSyncMainPageLoader.Reset();
			handler.BeginInvoke(first, length, fileName, listvis, new AsyncCallback(GetPagesEnd), handler);
		}

		/// <summary>
		/// Ассинхронная функция получения страниц из файла. 
		/// <para>
		/// <note type="caution"> Нумерация <paramref name="first"/> начинается с 0</note>
		/// </para>
		/// </summary>
		/// <param name="first">номер первой страницы</param>
		/// <param name="length">количество страниц</param>
		/// <param name="fileName"></param>
		/// <param name="listvis">коллекция данных о видемых превью</param>
		/// <returns>резултат операции</returns>
		private bool GetPagesBegin(int first, int length, string fileName, SynchronizedCollection<VisiblePreview> listvis)
		{
			if(!ResetEventGetPagesStart.WaitOne(60000, true))
			{
				LibTiffHelper.WriteToLog("Ошибка загрузки страниц ImageControl");
				return false;
			}

			if(listvis == null || string.IsNullOrEmpty(fileName) || libTiff == null)
				return false;

			ResetEventGetPagesStart.Reset();
			IntPtr filePtr = libTiff.TiffHandle;

			if(previews == null)
				previews = new SynchronizedCollection<KeyValuePair<Bitmap, bool>>();
			if(previews.Count != PageCount)
			{
				previews.Clear();
				for(int i = 0; i < PageCount; i++)
					previews.Add(new KeyValuePair<Bitmap, bool>(null, false));
			}

			try
			{
				Rectangle invalideteRect = Rectangle.Empty;
				for(int n = first; n < first + length && n < PageCount; n++)
				{
					if(_IsGetPagesStart > 1 || isScrollThumbnailImagesForShadow)
						break;
					if(fileName != this.fileName)
						return true;
					Bitmap bmp = null;
					if(filePtr == IntPtr.Zero)
						filePtr = libTiff.TiffOpenRead(ref fileName, out bmp, false);
					if(bmp != null && n == 0)
					{
						animatedImage = bmp;
						if(previews != null && previews[n].Key == null)
						{
							previews[n] = new KeyValuePair<Bitmap, bool>(GetPreview(bmp, true), false);
						}
						return false;
					}

					PageInfo il = GetImageFromTiff(filePtr, n, true);
					if(il != null)
					{
						if(fileName != this.fileName)
							return true;
						if(SelectedIndex == n)
							animatedImage = il.Image;
						if(previews != null && previews[n].Key == null)
							previews[n] = new KeyValuePair<Bitmap, bool>(GetPreview(il.Image, true), il.Annotation != null);
					}
					if(listvis.Count < 1)
						continue;

					foreach(VisiblePreview vpc in listvis)
					{
						if(fileName != this.fileName)
							return true;
						if(vpc.index != n)
							continue;
						if(invalideteRect == Rectangle.Empty)
							invalideteRect = vpc.rect;
						else
							invalidRect = Rectangle.Union(invalidRect, vpc.rect);
						break;
					}
				}
				if(showThumbPanel)
				{
					isLockThumbnailImages = false;
					if(invalideteRect != Rectangle.Empty)
						Invalidate(invalideteRect);
				}
				return false;
			}
			finally
			{
				if(filePtr != IntPtr.Zero)
					if(!UseLock)
						libTiff.TiffCloseRead(ref filePtr);
					else
						if(filePtr != libTiff.TiffHandle)
							libTiff.TiffHandle = filePtr;
			}
		}

        /// <summary>
        /// Обновить страницу изображения
        /// </summary>
        /// <param name="page"></param>
		protected void RefreshPage(List<int> pages, bool check)
		{
			IntPtr filePtr = IntPtr.Zero;

			try
			{
				filePtr = libTiff.TiffHandle;

				Bitmap bmp;

				if(filePtr == IntPtr.Zero)
					filePtr = libTiff.TiffOpenRead(ref fileName, out bmp, false);
				foreach(int page in pages)
				{
					PageInfo il = GetImageFromTiff(filePtr, page, check);

					if(il != null)
					{
						if(SelectedIndex == page)
						{
							animatedImage = il.Image;

							Invalidate();
						}


						if(previews != null)
						{
							Bitmap old = null;

							if(previews[page].Key != null)
								old = previews[page].Key;

							previews[page] = new KeyValuePair<Bitmap, bool>(GetPreview(il.Image, true), il.Annotation != null);

							if(old != null)
								old.Dispose();

							if(showThumbPanel)
							{
								Rectangle invalideteRect = Rectangle.Empty;

								Rectangle rect = Rectangle.Empty;

								foreach(VisiblePreview vpc in listvis)
								{
									if(vpc.index != page)
										continue;

									if(invalideteRect == Rectangle.Empty)
										invalideteRect = vpc.rect;
									else
										invalidRect = Rectangle.Union(invalidRect, vpc.rect);
									rect = vpc.rect;

									break;
								}

								if(rect == Rectangle.Empty)
									return;

								// двигаем превьюшку на середину
								if(isToAlignPreview)
								{
									int sizeThumbnailImage;
									int coordinate;
									ScrollBar scrollBar;

									if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
									{
										sizeThumbnailImage = widthThumbnailImage;
										coordinate = rect.X;
										scrollBar = scrollThumbnailImageHorizontal;
									}
									else
									{
										sizeThumbnailImage = heightThumbnailImage;
										coordinate = rect.Y;
										scrollBar = scrollThumbnailImage;
									}
									int middle = (sizeThumbnailImage >> 1) - 70;
									int currentScrollValue = scrollBar.Value;

									int difference = middle - coordinate;
									currentScrollValue -= difference;
									if(currentScrollValue < 0)
										currentScrollValue = 0;
									if(currentScrollValue > scrollBar.Maximum)
										currentScrollValue = scrollBar.Maximum;
									scrollBar.Value = currentScrollValue;
									ScrollValueChanged(currentScrollValue);
								}

								isLockThumbnailImages = false;

								if(invalideteRect != Rectangle.Empty)
									Invalidate(invalideteRect);
							}
						}
					}
				}
			}
			catch(Exception ex)
			{
				LibTiffHelper.WriteToLog(ex);
			}
			finally
			{
				if(filePtr != IntPtr.Zero)
					if(!UseLock)
						libTiff.TiffCloseRead(ref filePtr);
					else
						libTiff.TiffHandle = filePtr;
			}
		}

		/// <summary>
		/// Обновить страницу изображения
		/// </summary>
		/// <param name="page"></param>
		protected void RefreshPage(int page)
		{
			IntPtr filePtr = IntPtr.Zero;

			try
			{
				filePtr = libTiff.TiffHandle;

				Bitmap bmp;

				if(filePtr == IntPtr.Zero)
					filePtr = libTiff.TiffOpenRead(ref fileName, out bmp, false);

				PageInfo il = GetImageFromTiff(filePtr, page);

				if(il != null)
				{
					if(SelectedIndex == page)
					{
						animatedImage = il.Image;

						Invalidate();
					}


					if(previews != null)
					{
						Bitmap old = null;

						if(previews[page].Key != null)
							old = previews[page].Key;

						previews[page] = new KeyValuePair<Bitmap, bool>(GetPreview(il.Image, true), il.Annotation != null);

						if(old != null)
							old.Dispose();

						if(showThumbPanel)
						{
							Rectangle invalideteRect = Rectangle.Empty;

							Rectangle rect = Rectangle.Empty;

							foreach(VisiblePreview vpc in listvis)
							{
								if(vpc.index != page)
									continue;

								if(invalideteRect == Rectangle.Empty)
									invalideteRect = vpc.rect;
								else
									invalidRect = Rectangle.Union(invalidRect, vpc.rect);
								rect = vpc.rect;

								break;
							}

							if(rect == Rectangle.Empty)
								return;

							// двигаем превьюшку на середину
							if(isToAlignPreview)
							{
								int sizeThumbnailImage;
								int coordinate;
								ScrollBar scrollBar;

								if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
								{
									sizeThumbnailImage = widthThumbnailImage;
									coordinate = rect.X;
									scrollBar = scrollThumbnailImageHorizontal;
								}
								else
								{
									sizeThumbnailImage = heightThumbnailImage;
									coordinate = rect.Y;
									scrollBar = scrollThumbnailImage;
								}
								int middle = (sizeThumbnailImage >> 1) - 70;
								int currentScrollValue = scrollBar.Value;

								int difference = middle - coordinate;
								currentScrollValue -= difference;
								if(currentScrollValue < 0)
									currentScrollValue = 0;
								if(currentScrollValue > scrollBar.Maximum)
									currentScrollValue = scrollBar.Maximum;
								scrollBar.Value = currentScrollValue;
								ScrollValueChanged(currentScrollValue);
							}

							isLockThumbnailImages = false;

							if(invalideteRect != Rectangle.Empty)
								Invalidate(invalideteRect);
						}
					}
				}
			}
			catch(Exception ex)
			{
				LibTiffHelper.WriteToLog(ex);
			}
			finally
			{
				if(filePtr != IntPtr.Zero)
					if(!UseLock)
						libTiff.TiffCloseRead(ref filePtr);
					else
						libTiff.TiffHandle = filePtr;
			}
		}


	    public Bitmap GetPreview(Bitmap image, bool isCorrectScale)
		{
			if(image == null)
				return null;
			Bitmap preview = null;
			Image.GetThumbnailImageAbort myCallback = new Image.GetThumbnailImageAbort(ThumbnailCallback);

			if(isCorrectScale)
			{
				lock(image)
				{
					try
					{
						int w = 140;
						int h = 140;
						try
						{
							if(image.Width * image.VerticalResolution > image.Height * image.HorizontalResolution)
								h = (int)(140 * image.Height * image.HorizontalResolution / image.Width / image.VerticalResolution);
							else
								w =(int)(140 * image.Width * image.HorizontalResolution / image.Height / image.VerticalResolution);
						}
						catch(Exception ex)
						{
							Tiff.LibTiffHelper.WriteToLog(ex);
						}
						preview = (Bitmap)image.GetThumbnailImage(w, h, myCallback, IntPtr.Zero);
						//preview = new Bitmap(w, h);
						//using(Graphics gr = Graphics.FromImage(preview))
						//{
						//	gr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
						//	gr.DrawImage(image, 0, 0, w, h);
						//}
					}
					catch(Exception ex)
					{
						Tiff.LibTiffHelper.WriteToLog(ex);
					}
				}
			}
			else
			{
				preview = new Bitmap(100, 140);
				try
				{
					using(Graphics gr = Graphics.FromImage(preview))
					{

						gr.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
						gr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
						gr.FillRectangle(Brushes.White, 0, 0, 100, 140);
						int y = 0;
						int h = (int)(image.Height * image.HorizontalResolution * 100 / (float)image.Width / image.VerticalResolution);
						if(image.Width >= image.Height)
							y = 70 - (h >> 1);
						gr.DrawImage(image, 0, y, 100, h);
					}
				}
				catch(Exception ex)
				{
					Tiff.LibTiffHelper.WriteToLog(ex);
				}
			}
			return preview;
		}

		public bool ThumbnailCallback()
		{
			return false;
		}

		/// <summary>
		/// обработчик получения страниц
		/// </summary>
		/// <param name="ar"></param>
		private void GetPagesEnd(IAsyncResult ar)
		{
			if(ar == null)
				return;
			GetPagesHandler handler = ar.AsyncState as GetPagesHandler;

			bool isFileChange = false;
			try
			{
				isFileChange = handler.EndInvoke(ar);
			}
			catch(Exception ex)
			{
				Log.Logger.WriteEx(ex);
				return;
			}
			finally
			{
				System.Threading.Interlocked.Decrement(ref _IsGetPagesStart);
				ResetEventGetPagesStart.Set();
				rwSyncMainPageLoader.Set();
			}
			try
			{
				if(isFileChange)
				{
					if(!string.IsNullOrEmpty(fileName) && !File.Exists(fileName))
					{
						FileName = "";
						OnFileMoved();
					}
					else
					{

						if(this != null && !this.IsDisposed && IsHandleCreated)
							if(this.InvokeRequired)
								this.Invoke(new ActionWithFile(VerifyFile), new object[] { fileName });
							else
								VerifyFile(fileName);
					}
					return;
				}
				if(!isSuccessfullyChangePage && SelectedIndex >= 0)
				{
					if(this != null && !this.IsDisposed && IsHandleCreated)
						if(this.InvokeRequired)
							this.Invoke(new ActionWithPage(TryChangePage), new object[] { SelectedIndex });
						else
							TryChangePage(SelectedIndex);
				}
				else if(SelectedIndex < 0)//сичтаем, чтотолько что выбрали файл
				{
					if(this != null && !this.IsDisposed && IsHandleCreated)
						if(this.InvokeRequired)
							this.Invoke(new ActionWithPage(SelectPageAfterFileNameChange), new object[] { 0 });
						else
							SelectPageAfterFileNameChange(0);
				}
				else if(showThumbPanel)
				{
					if(this != null && !this.IsDisposed && IsHandleCreated)
						if(InvokeRequired)
							this.Invoke((MethodInvoker)(Refresh), null);
						else
							Refresh();
				}
			}
			catch(Exception ex)
			{
				Log.Logger.WriteEx(ex);
			}
		}

		/// <summary>
		/// Обработчик скролинга превьюшек
		/// </summary>
		/// <param name="val">Значение смещения</param>
		private void ScrollValueChanged(int val)
		{
			if(string.IsNullOrEmpty(fileName))
				return;
			isLockThumbnailImages = false;
			if(AnnotationState == AnnotationsState.CreateText || AnnotationState == AnnotationsState.EditText)
			{
				EndEditFiguresText();
				IsRefreshBitmap = true;
				Invalidate(rectAnimatedImage);
			}
			int scrollValue = val;
			globalOffsetYThumbnail = scrollValue;


			//////////////////////////////////////////////////////////////////
			//создание коллекции содержащей только координаты видмых превьюшек
			listvis = FillCoordinatesVisiblePreview(false);
			/////////////////////////////////////////////////////////////////////////
			int index = 0;//индекс первого видимого элемента
			if(listvis.Count > 0)
				index = (listvis[0]).index;
			float panelSize = 0;
			if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
				panelSize = (float)widthThumbnailImage;
			else
				panelSize = (float)heightThumbnailImage;
			int countVisPreview = (int)Math.Ceiling((panelSize / (float)(sizePreview + INDENT_THUMBNAIL_IMAGE))) * this.countWithRow + this.countWithRow;
			if(countVisPreview > this.countPreview)
				countVisPreview = this.countPreview;


			if(!isScrollThumbnailImagesForShadow)//если тени выключены или закончили рисоваться
			{
				if(countPreview != listvis.Count)//если количестов получено полностью, то обсчеты уже не нужны
				{
					if(countVisPreview > 0)
					{
						GetPagesStart(index, countVisPreview, listvis);
					}
				}
			}

			///////////////////////////////////////////////////////////////////////////////////
			//рисование теней превьюшек
			if(isScrollThumbnailImagesForShadow)
			{
				Graphics gr = Graphics.FromHwnd(Handle);
				using(Bitmap bitmap = new Bitmap(widthThumbnailImage - 2, heightThumbnailImage - 2))
				{
					using(Graphics grPreview = Graphics.FromImage(bitmap))
					{
						//grPreview.SmoothingMode = SmoothingMode.HighQuality;

						grPreview.FillRectangle(Brushes.White, 0, 0, widthThumbnailImage - 2, heightThumbnailImage - 2);
						foreach(VisiblePreview vp in listvis)
						{
							if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
							{
								grPreview.FillRectangle(Brushes.White, vp.rectPaint.X - 1, vp.rectPaint.Y - 1, 102, 142);
								grPreview.FillRectangle(Brushes.Gray, vp.rectPaint.X, vp.rectPaint.Y, 100, 140);
								grPreview.DrawRectangle(Pens.Black, vp.rectPaint.X - 2, vp.rectPaint.Y - 2, 104, 144);
								grPreview.DrawString((vp.index + 1).ToString(), Font, Brushes.Black, vp.rectPaint.X, vp.rectPaint.Y + 146);
							}
							else
							{
								grPreview.FillRectangle(Brushes.White, vp.rectPaint.X - 1, vp.rectPaint.Y - 1, 102, 142);
								grPreview.FillRectangle(Brushes.Gray, vp.rectPaint.X, vp.rectPaint.Y, 100, 140);
								grPreview.DrawRectangle(Pens.Black, vp.rectPaint.X - 2, vp.rectPaint.Y - 2, 104, 144);
								grPreview.DrawString((vp.index + 1).ToString(), Font, Brushes.Black, vp.rectPaint.X, vp.rectPaint.Y + 146);

							}
						}

						gr.DrawImage(bitmap, rectThumbnailPanel.X + 1, rectThumbnailPanel.Y + 1);
					}
				}
				///////////////////////////////////////////////////////////////////////////////////////
			}


			if(!isScrollThumbnailImagesForShadow)//перерисовка контрола только если в данный момент тини не рисуются
			{
				if(ControlTypeWork.DrawCorrectScale == (TypeWork & ControlTypeWork.DrawCorrectScale))
					listvis = FillCoordinatesVisiblePreview(true);

				Refresh();
			}
		}

		/// <summary>
		/// Создание коллекции содержащей только координаты видмых превьюшек и их индексы
		/// </summary>
		private SynchronizedCollection<VisiblePreview> FillCoordinatesVisiblePreview(bool isCalculationSizePreview)
		{
			List<VisiblePreview> listvis = new List<VisiblePreview>();
			if(showThumbPanel)
			{
				if(this.countPreview > 0)
				{
					int index = ((globalOffsetYThumbnail - INDENT_BETWEEN_PREVIEW) / (sizePreview + INDENT_THUMBNAIL_IMAGE)) * this.countWithRow;//индекс первого видимого элемента
					if(index > countPreview - 1)
						return new SynchronizedCollection<VisiblePreview>(new object(), listvis);

					float fl = (float)(globalOffsetYThumbnail - INDENT_BETWEEN_PREVIEW) % (float)(sizePreview + INDENT_THUMBNAIL_IMAGE);//остаток от превиюшки и отступа
					int offset = (int)((sizePreview + INDENT_THUMBNAIL_IMAGE) - fl);//смещение в координатах либо горизонатльных, либо вертикальных следующей превьюшки


					int w = 0;//ширина превьюшки для вертикальной панели, высота для горизонтальной панели
					int h = 0;//высота превьюшки для вертикальной панели, ширина для горизонтальной панели
					float nextHeight = 0;//размер области для расчета
					int generalWidth = 0;
					int generalHeight = 0;
					int sizeWithRow = 0;

					if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
					{
						h = 140;
						w = 140;
						nextHeight = widthThumbnailImage;
						generalWidth = h;
						generalHeight = w;
						sizeWithRow = w;
						if(offset != 0)
						{
							offset = offset - (h + INDENT_THUMBNAIL_IMAGE);
						}
					}
					else
					{
						w = 140;
						h = 140;
						nextHeight = heightThumbnailImage;
						generalWidth = w;
						generalHeight = h;
						sizeWithRow = w;
						if(offset != 0)
						{
							offset = offset - (h + INDENT_THUMBNAIL_IMAGE);
						}
					}

					int countVisPreview = (int)Math.Ceiling((nextHeight / (sizePreview + INDENT_THUMBNAIL_IMAGE))) * this.countWithRow + this.countWithRow;
					int ofssetAdd = 0;

					for(int n = 0, colRow = 0; n < countVisPreview; n++, colRow++)
					{
						if(index > countPreview - 1)
							return new SynchronizedCollection<VisiblePreview>(new object(), listvis);
						if(colRow > this.countWithRow - 1)
						{
							colRow = 0;
							offset += sizePreview + INDENT_THUMBNAIL_IMAGE;
						}
						ofssetAdd = colRow * (sizeWithRow + INDENT_BETWEEN_PREVIEW);
						int offsetTurnX = 0;
						int offsetTurnY = 0;
						if(isCalculationSizePreview && ControlTypeWork.DrawCorrectScale == (TypeWork & ControlTypeWork.DrawCorrectScale))
						{
							Bitmap preview = null;
							if(previews != null && previews.Count > index)
								preview = previews[index].Key;
							if(preview != null)
							{
								w = preview.Width;
								h = preview.Height;
							}
							generalWidth = w;
							generalHeight = h;

							if(generalWidth > generalHeight)
							{
								offsetTurnY = ((140 - h) / 2);
								offsetTurnX = 20;
							}
						}
						if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
						{
							Rectangle rect = new Rectangle(rectThumbnailPanel.X + offset - offsetTurnX, rectThumbnailPanel.Y + INDENT_BETWEEN_PREVIEW + ofssetAdd + offsetTurnY, generalWidth, generalHeight);
							Rectangle rectPaint = new Rectangle(rect.X - rectThumbnailPanel.X, rect.Y - rectThumbnailPanel.Y, rect.Width, rect.Height);
							VisiblePreview view = new VisiblePreview(rect, index, rectPaint);
							listvis.Add(view);
						}
						else
						{
							Rectangle rect = new Rectangle(rectThumbnailPanel.X + INDENT_BETWEEN_PREVIEW + ofssetAdd - offsetTurnX, rectThumbnailPanel.Y + offset + offsetTurnY, generalWidth, generalHeight);
							Rectangle rectPaint = new Rectangle(rect.X - rectThumbnailPanel.X, rect.Y - rectThumbnailPanel.Y, rect.Width, rect.Height);
							VisiblePreview view = new VisiblePreview(rect, index, rectPaint);
							listvis.Add(view);
						}
						index++;

					}


				}
			}
			return new SynchronizedCollection<VisiblePreview>(new object(), listvis);
		}

		/// <summary>
		/// Проверка попадания мыши в координатах контрола
		/// </summary>
		protected bool IsMouseOnRectMain(Rectangle rect, Point mousePos)
		{
			return mousePos.X >= rect.X && mousePos.X <= rect.X + rect.Width
				&& mousePos.Y >= rect.Y && mousePos.Y <= rect.Y + rect.Height;
		}

		/// <summary>
		/// Проверка попадания мыши в координатах рисунка, с учетом масштаба и скрола
		/// </summary>
		protected bool IsMouseOnRect(Rectangle rect, Point mousePos)
		{
			return mousePos.X >= rect.X * zoom * ppi / animatedImage.HorizontalResolution + scrollX && mousePos.X <= rect.X * zoom * ppi / animatedImage.HorizontalResolution + scrollX + rect.Width * zoom * ppi / animatedImage.HorizontalResolution
				&& mousePos.Y >= rect.Y * zoom * ppi / animatedImage.VerticalResolution + scrollY && mousePos.Y <= rect.Y * zoom * ppi / animatedImage.VerticalResolution + scrollY + rect.Height * zoom * ppi / animatedImage.VerticalResolution;
		}

		/// <summary>
		/// Проверка нахождения заметки в области выделения в координатах рисунка, с учетом масштаба и скрола
		/// </summary>
		protected bool IsRectOnRect(Rectangle rect, Rectangle selRect)
		{
			return rect.IntersectsWith(selRect);
		}
		/// <summary>
		/// Событие от заметок о каких либо изменениях
		/// </summary>
		protected void annotation_ModifiedFigure(object sender, TiffAnnotation.ModifyEventArgs args)
		{
			SelectedBitmaps.Clear();
			SelectedBitmap = null;
			TiffAnnotation annotation = this.tiffAnnotation;
			if(annotation != null)
			{
				ArrayList figuresList = annotation.GetFigures(false);
				foreach(object figure in figuresList)
				{
					TiffAnnotation.IBufferBitmap bb = figure as TiffAnnotation.IBufferBitmap;
					if(bb != null)
					{
						if(bb.Selected)
							if(!SelectedBitmaps.Contains(bb))
								SelectedBitmaps.Add(bb);
					}
				}
				if(SelectedBitmaps != null && SelectedBitmaps.Count > 0)
					SelectedBitmap = SelectedBitmaps[0];
			}
			this.SetModifiedMarks(true);
			IsRefreshBitmap = true;
			Refresh();
		}

		/// <summary>
		/// Снятие выделений с заметок, очистка фрагмента
		/// </summary>
		private void ClearSelectedNotes()
		{
			TiffAnnotation annotation = this.tiffAnnotation;
			if(annotation != null)
			{
				ArrayList figuresList = annotation.GetFigures(false);
				foreach(object figure in figuresList)
				{
					TiffAnnotation.IBufferBitmap bb = figure as TiffAnnotation.IBufferBitmap;
					if(bb != null)
					{
						bb.Selected = false;
					}
				}

				SelectedBitmaps.Clear();
				notesToSelectedRectangles.Clear();

				selectedRectangles = null;
				SelectedBitmap = null;
				IsRefreshBitmap = true;
				Invalidate();
			}
			SelectionModeRectangle = Rectangle.Empty;
		}

        /// <summary>
        /// Выбор страницы изображения
        /// </summary>
        /// <param name="info"></param>
		private void SelectPage(Tiff.PageInfo info)
		{
			animatedImage = info.Image;
			TypeWorkAnimatedImage = TypeWorkImage.MoveImage;
			OnToolSelected(new ToolSelectedEventArgs { EventType = 0 });
			if(!externalScroll)
			{
				scrollX = 0;
				scrollY = 0;
			}
			if(this.fitValue <= 2)
				FitTo(fitValue, true);
			else
				CalculationForDrawImageAfterSelectRotateScale(1.0);
			if(tiffAnnotation != null)
				tiffAnnotation.ModifiedFigure -= annotation_ModifiedFigure;

			if(info.Annotation != null)
			{
				if(tiffAnnotation != null)
					tiffAnnotation.Dispose();
				tiffAnnotation = new TiffAnnotation(this);
				tiffAnnotation.Parse(info.Annotation);
				tiffAnnotation.ModifiedFigure += annotation_ModifiedFigure;
				if(markGroupsVisibleList == null)
					markGroupsVisibleList = new Hashtable();
				markGroupsVisibleList.Clear();
				foreach(TiffAnnotation.OIAN_MARK_ATTRIBUTES attr in tiffAnnotation.MarkAttributes)
				{
					if(!markGroupsVisibleList.ContainsKey(attr.OiGroup))
					{
						markGroupsVisibleList.Add(attr.OiGroup, false);
					}
				}
			}
			else
			{
				if(tiffAnnotation != null)
					tiffAnnotation.Dispose();
				tiffAnnotation = null;
			}
			SelectionModeRectangle = Rectangle.Empty;

			SelectedBitmaps.Clear();
			notesToSelectedRectangles.Clear();

			selectedRectangles = null;
			if(SelectedBitmap != null)
				SelectedBitmap.Selected = false;
			isLockThumbnailImages = false;
			IsRefreshBitmap = true;
		}

	    #region Мышиные события

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);
			if(showThumbPanel && IsMouseOnRectMain(rectThumbnailPanel, new Point(e.X, e.Y)))
			{
				int currentScrollValue = scrollThumbnailImage.Value - e.Delta;
				if(currentScrollValue < 0)
					currentScrollValue = 0;
				else if(currentScrollValue > scrollThumbnailImage.Maximum)
					currentScrollValue = scrollThumbnailImage.Maximum;
				scrollThumbnailImage.Value = currentScrollValue;
				ScrollValueChanged(currentScrollValue);
			}
			else if(IsMouseOnRectMain(rectAnimatedImage, new Point(e.X, e.Y)))
			{
				if(!needScrollY)
				{
					int currentScrollValue = scrollImageVertical.Value - e.Delta;
					if(currentScrollValue < 0)
						currentScrollValue = 0;
					else if(currentScrollValue > scrollImageVertical.Maximum - scrollImageVertical.LargeChange)
						currentScrollValue = scrollImageVertical.Maximum - scrollImageVertical.LargeChange;
					scrollImageVertical.Value = currentScrollValue;
					scrollY = -scrollImageVertical.Value;
					Invalidate(rectAnimatedImage);
				}
			}
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if(!Focused && (rTextBox == null || (rTextBox != null && !rTextBox.Focused)))
				this.Focus();

			if(IsMouseOnRectMain(this.rectAnimatedImage, new Point(e.X, e.Y)))
			{
				if(animatedImage == null || fileName == null)
					return;
				string extension = Path.GetExtension(fileName).ToLower();
				if(TypeWorkAnimatedImage != TypeWorkImage.SelectionMode && TypeWorkAnimatedImage != TypeWorkImage.MoveImage && !(extension == ".tif" || extension == ".tiff"))
					return;

				Point mPoint = new Point(e.X - this.rectAnimatedImage.X, e.Y - this.rectAnimatedImage.Y);
				if(TypeWorkAnimatedImage == TypeWorkImage.SelectionMode) // создание выделения
				{
					UserAction = UsersActionsTypes.SelectionMode;
					this.Cursor = Cursors.Default;
					lastPositionForDrag = mPoint;
				}
				else if(TypeWorkAnimatedImage == TypeWorkImage.CreateNotes) // создание заметок
				{
					if(AnnotationState == AnnotationsState.CreateText || AnnotationState == AnnotationsState.EditText)
					{
						EndEditFiguresText();
						IsRefreshBitmap = true;
						Invalidate();
						return;
					}

					if(Cursor == TiffCursors.Marker || Cursor == TiffCursors.HRect || Cursor == TiffCursors.FRect || Cursor == TiffCursors.RectText || Cursor == TiffCursors.Note || Cursor == Cursors.NoMove2D)
					{
						lastPositionForDrag = mPoint;
						AnnotationState = AnnotationsState.Create;
						IsRefreshBitmap = true;
					}
				}
				else if(TypeWorkAnimatedImage == TypeWorkImage.MoveImage) // движение картинки
				{
					if(this.animatedImage != null && e.Button == MouseButtons.Left)
					{
						UserAction = UsersActionsTypes.MoveImage;
						needScrollX = scrollImageVertical.Visible;
						needScrollY = scrollImageHorizontal.Visible;
						this.Cursor = TiffCursors.HandDrag;
						if(needScrollX || needScrollY)
						{
							if(scrollImageHorizontal.Value != 0 || scrollImageVertical.Value != 0)
							{
								scrollX = -scrollImageHorizontal.Value;
								scrollY = -scrollImageVertical.Value;
							}
							else
							{
								scrollX = 0;
								scrollY = 0;
							}
							x0 = e.X;
							y0 = e.Y;
						}
					}
				}
				else if(TypeWorkAnimatedImage == TypeWorkImage.EditNotes) // редактирование заметок (свойства, удаление, масштабирование, перемещение)
				{
					if(IsNotesAllHide)
						return;
					if(AnnotationState == AnnotationsState.CreateText || AnnotationState == AnnotationsState.EditText)
					{
						EndEditFiguresText();
						IsRefreshBitmap = true;
						Invalidate(this.rectAnimatedImage);
						return;
					}
					if(tiffAnnotation != null && e.Button == MouseButtons.Left)
					{
						bool selectOne = false;
						foreach(object figure in tiffAnnotation.GetFigures(false))
						{
							TiffAnnotation.IBufferBitmap bb = figure as TiffAnnotation.IBufferBitmap;
							if(bb == null)
								continue;
							bool select = false;
							if(IsFigureHit(bb, mPoint))
							{
								selectOne = select = true;
								if(SelectedBitmap != null && SelectedBitmap.Selected)
									SelectedBitmap.Selected = false;
								SelectedBitmap = bb;
							}
							if(bb.Selected != select)
							{
								bb.Selected = select;
							}
						}

						if(!selectOne)
						{
							SelectedBitmaps.Clear();
							notesToSelectedRectangles.Clear();

							selectedRectangles = null;
							SelectedBitmap = null;
						}
						else
						{
							Invalidate();
							Rectangle forMove = new Rectangle(SelectedBitmap.Rect.X + (WidthSelectedtRect >> 1), SelectedBitmap.Rect.Y + (WidthSelectedtRect >> 1), SelectedBitmap.Rect.Width - WidthSelectedtRect, SelectedBitmap.Rect.Height - WidthSelectedtRect);
							if(IsMouseOnRect(forMove, mPoint))
							{
								Cursor = Cursors.NoMove2D;
								lastPositionForDrag = mPoint;
							}
						}
						if(SelectedBitmap != null)
							UserAction = UsersActionsTypes.EditNote;
						IsRefreshBitmap = true;
					}
					if(SelectedBitmap == null && e.Button == MouseButtons.Left)
					{
						UserAction = UsersActionsTypes.SelectionNotes;
						selectionNotesRectangle = Rectangle.Empty;
						lastPositionForDrag = mPoint;
					}
					else if(e.Button == MouseButtons.Right)
						OnRightClick(e);
				}
			}
			else
			{
				VerifyEndEditTextsMark(true);
				//Сплитер
				if(showThumbPanel && e.Button == MouseButtons.Left && IsMouseOnRectMain(rectSplitter, new Point(e.X, e.Y)))
				{
					UserAction = UsersActionsTypes.Splitter;
					isLockThumbnailImages = false;
					Refresh();//Нужна синхронная перерисовка, чтобы заполучить кеш рисунка контрола
				}
			}
		}

		/// <summary>
		/// Обработчик ПКМ - контекстное меню.
		/// </summary>
		protected virtual void OnRightClick(MouseEventArgs e)
		{
			Point mPoint = new Point(e.X - this.rectAnimatedImage.X, e.Y - this.rectAnimatedImage.Y);
			if(SelectedBitmap != null)	// выделен один элемент
			{
				if(IsFigureHit(SelectedBitmap, mPoint)) // попали в этот элемент
				{
					if(tiffAnnotation != null)
						tiffAnnotation.ContextMenuShow(this, e.Location);
				}
				else // не попали - снимаем выделение
				{
					SelectedBitmap.Selected = false;
					IsRefreshBitmap = true;
					Invalidate();
				}
			}
			if(SelectedBitmaps == null || SelectedBitmaps.Count < 2)
				return;
			if(tiffAnnotation == null)
				return;
			// выделено несколько элементов
			foreach(TiffAnnotation.IBufferBitmap figure in tiffAnnotation.GetFigures(false))
				if(figure.Selected && IsFigureHit(figure, mPoint))
				{
					tiffAnnotation.ContextMenuShow(this, e.Location);
					return;
				}

			foreach(TiffAnnotation.IBufferBitmap figure in tiffAnnotation.GetFigures(false))
				figure.Selected = false;
			IsRefreshBitmap = true;
			Invalidate();
		}

		/// <summary>
		/// Проверка, попадает ли точка в фигуру (используется в обработчиках событий мыши).
		/// </summary>
		private bool IsFigureHit(TiffAnnotation.IBufferBitmap figure, Point p)
		{
			Rectangle rectWithSelected = new Rectangle(
				(int)Math.Round(figure.Rect.X * ppi / animatedImage.HorizontalResolution) - (WidthSelectedtRect >> 1),
				(int)Math.Round(figure.Rect.Y * ppi / animatedImage.VerticalResolution) - (WidthSelectedtRect >> 1),
				(int)Math.Round(figure.Rect.Width * ppi / animatedImage.HorizontalResolution) + WidthSelectedtRect,
				(int)Math.Round(figure.Rect.Height * ppi / animatedImage.VerticalResolution) + WidthSelectedtRect);
			return IsMouseOnRect(figure.Rect, p) || IsMouseOnRect(rectWithSelected, p) && Cursor != Cursors.Default && Cursor != Cursors.Hand;
		}

		private void VerifyEndEditTextsMark(bool isSaveTextsMarks)
		{
			if(AnnotationState != AnnotationsState.CreateText && AnnotationState != AnnotationsState.EditText)
				return;

			if(isSaveTextsMarks)
			{
				EndEditFiguresText();
				IsRefreshBitmap = true;
				Invalidate(this.rectAnimatedImage);
			}
			else
			{
				if(rTextBox == null)
					return;

				invalidRect = new Rectangle();
				oldRect = new Rectangle();
				rTextBox.LostFocus -= new EventHandler(rTextBox_LostFocus);
				this.Controls.Remove(rTextBox);
				rTextBox = null;

				AnnotationState = AnnotationsState.None;
				Cursor = Cursors.Arrow;
			}
		}

		public void Replace(string sourceFile, int sourcePage, string addFile, int destPage, string destFile, int numPage)
		{
			libTiff.Replace(sourceFile, addFile, destFile, sourcePage, numPage, destPage);
		}

		private bool isSuccessfullyChangePage = true;

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if(UserAction == UsersActionsTypes.MoveImage) // окончание движения картинки
			{
				Cursor = Cursors.Default;
				UserAction = UsersActionsTypes.None;
				needScrollX = false;
				needScrollY = false;
			}
			else if(UserAction == UsersActionsTypes.Splitter) // окончание движения сплитера
			{
				UserAction = UsersActionsTypes.None;
				int tempCounteWithRow = 1;
				if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top)
				{
					heightThumbnailImage = rectSplitter.Y;
					defaultHeightThumbnailImage = heightThumbnailImage;
					userHeightThumbnailImage = defaultHeightThumbnailImage;
					heightImage = Height - heightThumbnailImage - heightSplitter;
					rectAnimatedImage = new Rectangle(0, heightThumbnailImage + heightSplitter, widthImage, heightImage);
					rectThumbnailPanel = new Rectangle(0, 0, widthThumbnailImage, heightThumbnailImage);
					scrollThumbnailImageHorizontal.Location = new Point(1, heightThumbnailImage - scrollThumbnailImageHorizontal.Height - 1);
					tempCounteWithRow = GetCountWithRow(heightThumbnailImage, 140);
				}
				else if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
				{
					heightThumbnailImage = Height - rectSplitter.Y - rectSplitter.Height;
					defaultHeightThumbnailImage = heightThumbnailImage;
					userHeightThumbnailImage = defaultHeightThumbnailImage;
					heightImage = Height - heightThumbnailImage - heightSplitter;
					rectAnimatedImage = new Rectangle(0, 0, widthImage, heightImage);
					rectThumbnailPanel = new Rectangle(0, rectAnimatedImage.Height + rectSplitter.Height, widthThumbnailImage, heightThumbnailImage);
					scrollThumbnailImageHorizontal.Location = new Point(1, Height - scrollThumbnailImageHorizontal.Height - 1);
					tempCounteWithRow = GetCountWithRow(heightThumbnailImage, 140);
				}
				else if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Left)
				{

					widthThumbnailImage = rectSplitter.X;
					defaultWidthThumbnailImage = widthThumbnailImage;
					userWidthThumbnailImage = defaultWidthThumbnailImage;
					widthImage = Width - widthThumbnailImage - widthSplitter;
					rectAnimatedImage = new Rectangle(widthThumbnailImage + widthSplitter, 0, widthImage, heightImage);
					rectThumbnailPanel = new Rectangle(0, 0, widthThumbnailImage, heightThumbnailImage);
					scrollThumbnailImage.Location = new Point(widthThumbnailImage - scrollThumbnailImage.Width - 1, 1);
					tempCounteWithRow = GetCountWithRow(widthThumbnailImage, 100);
				}
				else
				{
					widthThumbnailImage = Width - rectSplitter.X - rectSplitter.Width;
					defaultWidthThumbnailImage = widthThumbnailImage;
					userWidthThumbnailImage = defaultWidthThumbnailImage;
					widthImage = Width - widthThumbnailImage - widthSplitter;
					rectAnimatedImage = new Rectangle(0, 0, widthImage, heightImage);
					rectThumbnailPanel = new Rectangle(rectAnimatedImage.Width + rectSplitter.Width, 0, widthThumbnailImage, heightThumbnailImage);
					scrollThumbnailImage.Location = new Point(Width - scrollThumbnailImage.Width - 1, 1);
					tempCounteWithRow = GetCountWithRow(widthThumbnailImage, 100);
				}

				if(tempCounteWithRow != countWithRow)
				{
					countWithRow = tempCounteWithRow;
					CalculationForDraw();
					MoveScrol(SelectedIndex, true);
				}
				else
				{
					if(this.fitValue <= 2)
						FitTo(fitValue, true);
					else
						CalculationForDrawImageAfterSelectRotateScale(1.0);
					listvis = FillCoordinatesVisiblePreview(true);
					isLockThumbnailImages = false;
					Refresh();
				}
				OnSplinterChange(this, new SplitterEventArgs(rectSplitter.X, rectSplitter.Y, rectSplitter.Width, rectSplitter.Height));
			}
			else if(UserAction == UsersActionsTypes.EditNote) // окончание редактирования
			{
				if(AnnotationState == AnnotationsState.Drag)
				{
					AnnotationState = AnnotationsState.None;
					if(SelectedBitmap != null && isSizeChanged)
					{
						SetModifiedMarks(true);
						if(oldRect.Width == 0)
							oldRect.Width = 1;
						if(oldRect.Height == 0)
							oldRect.Height = 1;
						SelectedBitmap.ChangeSize(oldRect);
						selectedRectangles = null;

					}

					IsRefreshBitmap = true;
					Invalidate(rectAnimatedImage);
					isSizeChanged = false;
				}
				UserAction = UsersActionsTypes.None;
			}
			// если было нажатие на превьюшке
			else if(e.Button == MouseButtons.Left && IsMouseOnRectMain(rectThumbnailPanel, new Point(e.X, e.Y)))
			{
				bool isMouseClick = false;
				foreach(VisiblePreview rect in listvis)
				{
					if(IsMouseOnRectMain(rect.rect, e.Location))
					{
						isSuccessfullyChangePage = true;
						if(Modified)
						{
							if(newPage == rect.index + 1)
								return;
							newPage = rect.index + 1;
							OnNeedSave(ChangePage);
						}
						else
						{
							Cursor = Cursors.WaitCursor;
							isMouseClick = true;

						    bool changed = SelectedIndex != rect.index;

							SelectedIndex = rect.index;
							PageInfo info = new PageInfo();
							if(IntPtr.Zero != libTiff.TiffHandle)
								info = GetImageFromTiff(libTiff.TiffHandle, SelectedIndex);
							else
							{
								Bitmap bmp;
								IntPtr ptr = libTiff.TiffOpenRead(ref fileName, out bmp, false);
								if(IntPtr.Zero == ptr)
								{
									if(bmp != null && SelectedIndex == 0)
										info = new PageInfo { Image = bmp };
								}
								else
								{
									info = GetImageFromTiff(ptr, SelectedIndex);
									libTiff.TiffCloseRead(ref ptr);
								}
							}
							if(info.Image != null)
							{
                                // При смене страницы, скролинг в (0,0)
                                // TODO Проверить, ничего не сломалось
                                if (changed)
                                    externalScroll = false;

                                SelectPage(info);

								// двигаем превьюшку на середину
								if(isToAlignPreview)
								{
									int sizeThumbnailImage = 0;
									int coordinate = 0;
									ScrollBar scrollThumbnailImage = null;
									if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
									{
										sizeThumbnailImage = widthThumbnailImage;
										coordinate = rect.rect.X;
										scrollThumbnailImage = this.scrollThumbnailImageHorizontal;
									}
									else
									{
										sizeThumbnailImage = heightThumbnailImage;
										coordinate = rect.rect.Y;
										scrollThumbnailImage = this.scrollThumbnailImage;
									}
									int middle = (sizeThumbnailImage >> 1) - 70;
									int currentScrollValue = scrollThumbnailImage.Value;
									if(coordinate == middle)
										break;
									int difference = middle - coordinate;
									currentScrollValue -= difference;
									if(currentScrollValue < 0)
										currentScrollValue = 0;
									if(currentScrollValue > scrollThumbnailImage.Maximum)
										currentScrollValue = scrollThumbnailImage.Maximum;
									scrollThumbnailImage.Value = currentScrollValue;
									ScrollValueChanged(currentScrollValue);
								}
								else // если не на середину, то двигаем только не полностью видимые превьюшки
								{
									int sizeThumbnailImage = 0, coordinate = 0, sizeMovedPreview = 0;
									ScrollBar scrollThumbnailImage = null;

									if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
									{
										sizeThumbnailImage = widthThumbnailImage;
										coordinate = rect.rect.X;
										sizeMovedPreview = rect.rect.Width;
										scrollThumbnailImage = this.scrollThumbnailImageHorizontal;
									}
									else
									{
										sizeThumbnailImage = heightThumbnailImage;
										coordinate = rect.rect.Y;
										sizeMovedPreview = rect.rect.Height;
										scrollThumbnailImage = this.scrollThumbnailImage;
									}
									if(coordinate < INDENT_BETWEEN_PREVIEW)
									{
										int currentScrollValue = scrollThumbnailImage.Value;
										currentScrollValue -= Math.Abs(coordinate) + INDENT_BETWEEN_PREVIEW;
										if(currentScrollValue < 0)
											currentScrollValue = 0;
										if(currentScrollValue > scrollThumbnailImage.Maximum)
											currentScrollValue = scrollThumbnailImage.Maximum;
										scrollThumbnailImage.Value = currentScrollValue;
										ScrollValueChanged(currentScrollValue);
									}
									else if(coordinate > sizeThumbnailImage - sizeMovedPreview)
									{
										int currentScrollValue = scrollThumbnailImage.Value;
										currentScrollValue += (coordinate + sizeMovedPreview) - sizeThumbnailImage + 25;
										if(currentScrollValue < 0)
											currentScrollValue = 0;
										if(currentScrollValue > scrollThumbnailImage.Maximum)
											currentScrollValue = scrollThumbnailImage.Maximum;
										scrollThumbnailImage.Value = currentScrollValue;
										ScrollValueChanged(currentScrollValue);
									}
									else
									{
										isLockThumbnailImages = false;
										Refresh();
									}
								}
							}
							else
							{
								Cursor = Cursors.Default;
								SelectPage(info);
								isSuccessfullyChangePage = false;
							}
						}

						break;
					}
				}
				if(isMouseClick && isSuccessfullyChangePage)
				{
					OnPageChange();
					newPage = 0;
					isLockThumbnailImages = false;
					Refresh();

					if(CurrentStamp != null)
						SelectTool(9);
					else
						Cursor = Cursors.Default;
				}
			}
			else if(UserAction == UsersActionsTypes.SelectionMode)
			{
				SelectionModeRectangle = new Rectangle(invalidRect.X + sin, invalidRect.Y + sin, invalidRect.Width - sin * 2, invalidRect.Height - sin * 2);
				UserAction = UsersActionsTypes.None;
				IsRefreshBitmap = true;
				Invalidate(rectAnimatedImage);
			}
			else if(UserAction == UsersActionsTypes.SelectionNotes)
			{
				UserAction = UsersActionsTypes.None;
				SelectedBitmaps.Clear();
				if(tiffAnnotation != null && selectionNotesRectangle != Rectangle.Empty)
				{
					ArrayList figuresList = tiffAnnotation.GetFigures(false);
					foreach(object figure in figuresList)
					{
						TiffAnnotation.IBufferBitmap bb = figure as TiffAnnotation.IBufferBitmap;
						if(bb != null)
						{
							if(IsRectOnRect(bb.Rect, selectionNotesRectangle))
							{
								bb.Selected = true;
								if(!SelectedBitmaps.Contains(bb))
									SelectedBitmaps.Add(bb);
							}
						}
					}
				}
				if(SelectedBitmaps.Count > 0 || selectionNotesRectangle != Rectangle.Empty)
				{
					if(SelectedBitmaps.Count == 1)
					{
						IsRefreshBitmap = true;
						Invalidate(rectAnimatedImage);
						if(SelectedBitmap != null && SelectedBitmap.Selected)
							SelectedBitmap.Selected = false;
						SelectedBitmap = SelectedBitmaps[0];
						TypeWorkAnimatedImage = TypeWorkImage.EditNotes;
					}
					else
					{
						IsRefreshBitmap = true;
						Invalidate(rectAnimatedImage);
					}
				}
				else if(notesToSelectedRectangles.Count > 0)
				{
					notesToSelectedRectangles.Clear();
					IsRefreshBitmap = true;
					Invalidate(rectAnimatedImage);
				}
			}
			else if(UserAction == UsersActionsTypes.DrawFRect || UserAction == UsersActionsTypes.DrawHRect || UserAction == UsersActionsTypes.DrawImage || UserAction == UsersActionsTypes.DrawMarker || UserAction == UsersActionsTypes.DrawNote || UserAction == UsersActionsTypes.DrawRectText)
				CreateAnnotation(e.Location);
		}

		/// <summary>
		/// Создание заметки
		/// </summary>
		protected virtual void CreateAnnotation(Point mouseUpPt)
		{
			if(AnnotationState == AnnotationsState.EditText)
				return;
			if(AnnotationState == AnnotationsState.CreateText)
			{
				OnMarkEnd(this, MarkEndEventArgs.Empty);
				return;
			}
			if(AnnotationState == AnnotationsState.Create)
			{
				if(Cursor == TiffCursors.Stamp)
				{
					if(CurrentStamp != null)
					{
						// коэффициенты для масштабирования штампа под разрешение основной картинки
						float horizRatio = Image.HorizontalResolution / CurrentStamp.HorizontalResolution,
							vertRatio = Image.VerticalResolution / CurrentStamp.VerticalResolution;
						// помещаем изображение штампа таким образом, чтобы центр был в месте отпускания мыши
						Rectangle imageRect = new Rectangle(
							(int)((mouseUpPt.X - rectAnimatedImage.X - scrollX) / zoom - CurrentStamp.Width * horizRatio / 2),
							(int)((mouseUpPt.Y - rectAnimatedImage.Y - scrollY) / zoom - CurrentStamp.Height * vertRatio / 2),
							(int)(CurrentStamp.Width * horizRatio), (int)(CurrentStamp.Height * vertRatio));
						SelectedBitmap = TiffAnnotation.CreateImage(imageRect, CurrentStamp, GetCurrentAnnotationGroup());
						if(SelectedBitmap != null)
							SetModifiedMarks(true);
						UserAction = UsersActionsTypes.EditNote;
					}
				}
				else if(invalidRect.X > 0 && invalidRect.Y > 0)
				{
					Rectangle figuresRectangle = new Rectangle(invalidRect.X + sin, invalidRect.Y + sin, invalidRect.Width - sin * 2, invalidRect.Height - sin * 2);
					if(figuresRectangle.Width <= 0 || figuresRectangle.Height <= 0)
						return;
					if(Cursor == TiffCursors.Marker || Cursor == TiffCursors.FRect)
					{
						SelectedBitmap = TiffAnnotation.CreateFilledRectangle(figuresRectangle, GetCurrentAnnotationGroup());
						if(SelectedBitmap != null)
						{
							SetModifiedMarks(true);
						}
					}
					else if(Cursor == TiffCursors.HRect)
					{
						SelectedBitmap = TiffAnnotation.CreateHollowRectangle(figuresRectangle, GetCurrentAnnotationGroup());
						if(SelectedBitmap != null)
						{
							SetModifiedMarks(true);
						}
					}
					else if(Cursor == TiffCursors.RectText || Cursor == TiffCursors.Note)
					{
						rTextBox = new RichTextBox();
						rTextBox.ScrollBars = RichTextBoxScrollBars.None;
						rTextBox.BorderStyle = BorderStyle.None;
						rTextBox.LostFocus += new EventHandler(rTextBox_LostFocus);
						rTextBox.Location = new Point(ImageZoom(oldRect.Location.X, animatedImage.HorizontalResolution) + rectAnimatedImage.X, ImageZoom(oldRect.Location.Y, animatedImage.VerticalResolution) + rectAnimatedImage.Y);
						rTextBox.Size = new Size(ImageZoom(oldRect.Size.Width, animatedImage.HorizontalResolution), ImageZoom(oldRect.Size.Height, animatedImage.VerticalResolution));

						if(Cursor == TiffCursors.Note)
						{
							rTextBox.Font = new Font(Registry.ATTACH_A_NOTE_TOOL_FONT_NAME, (float)(Registry.ATTACH_A_NOTE_TOOL_FONT_SIZE * zoom ), GraphicsUnit.World);

							rTextBox.BackColor = Registry.ATTACH_A_NOTE_TOOL_BACKCOLOR;//Color.GreenYellow;
							rTextBox.ForeColor = Registry.ATTACH_A_NOTE_TOOL_FONT_COLOR;//Color.Black;
						}
						else
						{
							rTextBox.Font = new Font(Registry.TEXT_TOOL_FONT_NAME, (float)(Registry.ATTACH_A_NOTE_TOOL_FONT_SIZE * zoom ), GraphicsUnit.World);
							rTextBox.ForeColor = Registry.TEXT_TOOL_FONT_COLOR;
						}
						this.Controls.Add(rTextBox);
						rTextBox.Focus();
						AnnotationState = AnnotationsState.CreateText;
						Invalidate();
						return;
					}
					IsRefreshBitmap = true;
				}
				invalidRect = new Rectangle();

				oldRect = new Rectangle();
				AnnotationState = AnnotationsState.Drag;
				Invalidate();
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if(UserAction == UsersActionsTypes.MoveImage && e.Button == MouseButtons.Left) // после нажатия для перемещения картики
			{
				#region Движение картинки
				if(this.animatedImage != null && e.Button == MouseButtons.Left)
				{
					//движение картинки
					if((needScrollX && x0 != e.X) || (needScrollY && y0 != e.Y))
					{

						this.Capture = true;
						if((x0 & 1 << 4) != (e.X & 1 << 4) || (y0 & 1 << 4) != (e.Y & 1 << 4))
						{
							int scrollXT = 0;
							int scrollYT = 0;

							if(x0 > e.X)
							{
								scrollXT = x0 - e.X;

								if(scrollImageHorizontal.Value + scrollXT > scrollImageHorizontal.Maximum - realWidthImage)
								{
									if(scrollImageHorizontal.Maximum > 0)
										scrollImageHorizontal.Value = scrollImageHorizontal.Maximum - realWidthImage;
								}
								else
									scrollImageHorizontal.Value += scrollXT;
							}
							else
							{
								scrollXT = e.X - x0;
								if(scrollImageHorizontal.Value - scrollXT < 0)
									scrollImageHorizontal.Value = 0;
								else
									scrollImageHorizontal.Value -= scrollXT;
							}

							if(y0 > e.Y)
							{
								scrollYT = y0 - e.Y;

								if(scrollImageVertical.Value + scrollYT > scrollImageVertical.Maximum - realHeightImage)
								{
									if(scrollImageVertical.Maximum > 0)
										scrollImageVertical.Value = scrollImageVertical.Maximum - realHeightImage;
								}
								else
									scrollImageVertical.Value += scrollYT;
							}
							else
							{
								scrollYT = e.Y - y0;
								if(scrollImageVertical.Value - scrollYT < 0)
									scrollImageVertical.Value = 0;
								else
									scrollImageVertical.Value -= scrollYT;
							}
							scrollY = -scrollImageVertical.Value;
							scrollX = -scrollImageHorizontal.Value;
							x0 = e.X;
							y0 = e.Y;
							this.Invalidate(rectAnimatedImage);
						}
						else
						{
							scrollImageHorizontal.Value = -scrollX;
							scrollImageVertical.Value = -scrollY;
						}
					}
				}
				#endregion
			}
			else if(UserAction == UsersActionsTypes.Splitter && e.Button == MouseButtons.Left) // после нажатия на сплитере
			{
				#region Обработка сплитера (курсор и движение, установка курсора на превьюшках)
				if(e.Button == MouseButtons.Left && fullCahedBitmap != null)
				{
					if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
					{
						if(e.Y > this.Height - 30)
							return;
						if(e.Y < 32)
							return;
						SplitterMoveDraw(rectSplitter.X, e.Y);
					}
					else
					{
						if(e.X > this.Width - 30)
							return;
						if(e.X < 32)
							return;

						SplitterMoveDraw(e.X, rectSplitter.Y);
					}
					return;
				}
				#endregion
			}
			else if(UserAction == UsersActionsTypes.EditNote && e.Button == MouseButtons.Left)
			{
				#region Изменение размеров и перетаскивание фигур
				//координаты перемещаем в координаты рисунка
				Point mPointOffset = new Point(e.X - this.rectAnimatedImage.X, e.Y - this.rectAnimatedImage.Y);
				if(selectedRectangles != null)
				{
					AnnotationState = AnnotationsState.NotDrag;
					if(SelectedBitmap != null)
					{
						if(Cursor != Cursors.Default)
						{
							int indent = WidthSelectedtRect >> 1;

							int x = 0;
							int y = 0;
							int width = 0;
							int height = 0;
							AnnotationState = AnnotationsState.Drag;
							int xb = selectedRectangles[0].X + indent;// +scrollX;
							int yb = selectedRectangles[0].Y + indent;// +scrollX;


							if(Cursor == Cursors.NoMove2D)
							{
								isSizeChanged = true;
								x = xb + (int)(mPointOffset.X * animatedImage.HorizontalResolution / zoom / ppi) - (int)(lastPositionForDrag.X * animatedImage.HorizontalResolution / zoom / ppi);
								y = yb + (int)(mPointOffset.Y * animatedImage.VerticalResolution / zoom / ppi) - (int)(lastPositionForDrag.Y * animatedImage.VerticalResolution / zoom / ppi);
								width = SelectedBitmap.Rect.Width;
								height = SelectedBitmap.Rect.Height;
								//подсчеты и учет крайних положений для рисования в последующем надо изменить и двигать скрол
								int leftEdge = 10;//это потому что картинкареально больше, причем неизвестно на сколько
								int topEdge = 10;
								double xMax = (zoomWigth + (int)(scrollX * zoom * ppi / animatedImage.HorizontalResolution) - width * zoom * ppi / animatedImage.HorizontalResolution - scrollX * zoom * ppi / animatedImage.HorizontalResolution) * animatedImage.HorizontalResolution / zoom / ppi - 10;
								double yMax = (zoomHeigth + (int)(scrollY * zoom * ppi / animatedImage.VerticalResolution) - height * zoom * ppi / animatedImage.VerticalResolution - scrollY * zoom * ppi / animatedImage.VerticalResolution) * animatedImage.VerticalResolution / zoom / ppi - 10;

								if(x > xMax)
									x = (int)Math.Round((zoomWigth - width * zoom * ppi / animatedImage.HorizontalResolution - 1) * animatedImage.HorizontalResolution / zoom / ppi) - 10;
								else if(x < leftEdge)
									x = leftEdge;
								if(y > yMax)
									y = (int)Math.Round((zoomHeigth - height * zoom * ppi / animatedImage.VerticalResolution - 1) * animatedImage.VerticalResolution / zoom / ppi) - 10;
								else if(y < topEdge)
									y = topEdge;
							}
							else if(Cursor == Cursors.SizeWE)
							{
								y = yb;
								height = SelectedBitmap.Rect.Height;
								if(CursorDirection == Direction.L)
								{
									isSizeChanged = true;
									x = (int)((mPointOffset.X - scrollX) * animatedImage.HorizontalResolution / zoom / ppi);
									width = SelectedBitmap.Rect.Width + (xb - x);

									if(x > xb + SelectedBitmap.Rect.Width)
									{
										//										int rightEdge = (int)(realWidthImage/ zoom)  - width - (int)(scrollX / zoom) - (int)(10);
										//										if(x > rightEdge)
										//											x = rightEdge;
										width = x - (xb + SelectedBitmap.Rect.Width);// +scrollX;

										x = xb + SelectedBitmap.Rect.Width;// +scrollX;

									}
								}
								else if(CursorDirection == Direction.R)
								{
									isSizeChanged = true;
									x = xb;

									width = (int)((mPointOffset.X - scrollX) * animatedImage.HorizontalResolution / zoom / ppi) - x;
									if(x > (int)((mPointOffset.X - scrollX) * animatedImage.HorizontalResolution / zoom / ppi))
									{
										x = (int)((mPointOffset.X - scrollX) * animatedImage.HorizontalResolution / zoom / ppi);
										width = Math.Abs(x - xb);
									}

								}
							}
							else if(Cursor == Cursors.SizeNS)
							{

								x = xb;
								width = SelectedBitmap.Rect.Width;
								if(CursorDirection == Direction.U)
								{
									isSizeChanged = true;
									y = (int)((mPointOffset.Y - scrollY) * animatedImage.VerticalResolution / zoom / ppi);
									height = SelectedBitmap.Rect.Height + (yb - y);
									if(y > yb + SelectedBitmap.Rect.Height)
									{
										height = y - (yb + SelectedBitmap.Rect.Height);// +scrollY;
										y = yb + SelectedBitmap.Rect.Height;// +scrollY;

									}
								}
								else if(CursorDirection == Direction.D)
								{
									isSizeChanged = true;
									y = yb;
									height = (int)((mPointOffset.Y - scrollY) * animatedImage.VerticalResolution / zoom / ppi) - y;
									if(y > (int)((mPointOffset.Y - scrollY) * animatedImage.VerticalResolution / zoom / ppi))
									{
										y = (int)((mPointOffset.Y - scrollY) * animatedImage.VerticalResolution / zoom / ppi);
										height = Math.Abs(y - yb);
									}

								}

							}
							else if(Cursor == Cursors.SizeNWSE)
							{
								if(CursorDirection == Direction.UL)
								{
									isSizeChanged = true;
									x = (int)((mPointOffset.X - scrollX) * animatedImage.HorizontalResolution / zoom / ppi);
									y = (int)((mPointOffset.Y - scrollY) * animatedImage.VerticalResolution / zoom / ppi);
									width = SelectedBitmap.Rect.Width + (xb - x);
									height = SelectedBitmap.Rect.Height + (yb - y);
									if(x > xb + SelectedBitmap.Rect.Width)
									{
										width = x - (xb + SelectedBitmap.Rect.Width);// +scrollX;
										x = xb + SelectedBitmap.Rect.Width;// +scrollX;

									}
									if(y > yb + SelectedBitmap.Rect.Height)
									{
										height = y - (yb + SelectedBitmap.Rect.Height);// +scrollY;
										y = yb + SelectedBitmap.Rect.Height;// +scrollY;

									}
								}
								else if(CursorDirection == Direction.DR)
								{
									isSizeChanged = true;
									x = xb;
									y = yb;
									width = (int)((mPointOffset.X - scrollX) * animatedImage.HorizontalResolution / zoom / ppi) - x;
									height = (int)((mPointOffset.Y - scrollY) * animatedImage.VerticalResolution / zoom / ppi) - y;
									if(x > (int)((mPointOffset.X - scrollX) * animatedImage.HorizontalResolution / zoom / ppi))
									{
										x = (int)((mPointOffset.X - scrollX) * animatedImage.HorizontalResolution / zoom / ppi);
										width = Math.Abs(x - xb);
									}
									if(y > (int)((mPointOffset.Y - scrollY) * animatedImage.VerticalResolution / zoom / ppi))
									{
										y = (int)((mPointOffset.Y - scrollY) * animatedImage.VerticalResolution / zoom / ppi);
										height = Math.Abs(y - yb);
									}
								}


							}
							else if(Cursor == Cursors.SizeNESW)
							{
								if(CursorDirection == Direction.UR)
								{
									isSizeChanged = true;
									x = xb;
									y = (int)((mPointOffset.Y - scrollY) * animatedImage.VerticalResolution / zoom / ppi);
									width = (int)((mPointOffset.X - scrollX) * animatedImage.HorizontalResolution / zoom / ppi) - x;
									height = SelectedBitmap.Rect.Height + (yb - y);
									if(x > (int)((mPointOffset.X - scrollX) * animatedImage.HorizontalResolution / zoom / ppi))
									{
										x = (int)((mPointOffset.X - scrollX) * animatedImage.HorizontalResolution / zoom / ppi);
										width = Math.Abs(x - xb);
									}
									if(y > yb + SelectedBitmap.Rect.Height)
									{
										height = y - (yb + SelectedBitmap.Rect.Height);// +scrollY;
										y = yb + SelectedBitmap.Rect.Height;// +scrollY;

									}
								}
								else if(CursorDirection == Direction.DL)
								{
									isSizeChanged = true;
									x = (int)((mPointOffset.X - scrollX) * animatedImage.HorizontalResolution / zoom / ppi);
									y = yb;
									width = SelectedBitmap.Rect.Width + (xb - x);
									height = (int)((mPointOffset.Y - scrollY) * animatedImage.VerticalResolution / zoom / ppi) - y;
									if(x > xb + SelectedBitmap.Rect.Width)
									{
										width = x - (xb + SelectedBitmap.Rect.Width);
										x = xb + SelectedBitmap.Rect.Width;

									}
									if(y > (int)((mPointOffset.Y - scrollY) * animatedImage.VerticalResolution / zoom / ppi))
									{
										y = (int)((mPointOffset.Y - scrollY) * animatedImage.VerticalResolution / zoom / ppi);
										height = Math.Abs(y - yb);
									}
								}
							}
							//Учет орграничений для масштабирования
							if(Cursor != Cursors.NoMove2D)
							{
								int leftEdge = 10;//это потому что картинкареально больше, причем неизвестно на сколько
								int topEdge = 10;
								double xMax = (zoomWigth + scrollX * zoom * ppi / animatedImage.HorizontalResolution - width * zoom * ppi / animatedImage.HorizontalResolution - scrollX * zoom * ppi / animatedImage.HorizontalResolution) * animatedImage.HorizontalResolution / zoom / ppi - 10;
								double yMax = (zoomHeigth + scrollY * zoom * ppi / animatedImage.VerticalResolution - height * zoom * ppi / animatedImage.VerticalResolution - scrollY * zoom * ppi / animatedImage.VerticalResolution) * animatedImage.VerticalResolution / zoom / ppi - 10;

								if(x > xMax)
									width = animatedImage.Width - x - 10;
								else if(x < leftEdge)
								{
									x = leftEdge;
									if(0 <= CursorDirection.ToString().IndexOf("L"))
										width = xb + SelectedBitmap.Rect.Width - x;
									else
										width = xb - x;
								}
								if(y > yMax)
								{
									height = animatedImage.Height - y - 10;
								}
								else if(y < topEdge)
								{
									y = topEdge;
									if(0 <= CursorDirection.ToString().IndexOf("U"))
										height = yb + SelectedBitmap.Rect.Height - y;
									else
										height = yb - y;
								}
							}



							oldRect = new Rectangle(x, y, width, height);

							using(Bitmap bit = new Bitmap(invalidRect.Width > 0 ? invalidRect.Width : 1, invalidRect.Height > 0 ? invalidRect.Height : 1))
							{
								bit.SetResolution(cachedBitmap.HorizontalResolution, cachedBitmap.VerticalResolution);
								using(Graphics gr = Graphics.FromImage(bit))
								{
									//								gr.SmoothingMode = SmoothingMode.HighQuality;
									gr.InterpolationMode = CurrentInterpolationMode;

									gr.DrawImage(cachedBitmap, new Rectangle(0, 0, bit.Width, bit.Height),
										new Rectangle(invalidRect.X, invalidRect.Y, (invalidRect.Width > 0 ? invalidRect.Width : 1), (invalidRect.Height > 0 ? invalidRect.Height : 1)), GraphicsUnit.Pixel);
									using(Graphics g = Graphics.FromHwnd(Handle))
									{

										//g.SmoothingMode = SmoothingMode.HighQuality;
										g.InterpolationMode = CurrentInterpolationMode;
										if(CurrentInterpolationMode == InterpolationMode.High)
											g.PixelOffsetMode = PixelOffsetMode.HighQuality;
										RectangleF figuresInvRect = new RectangleF(invalidRect.X + (float)(scrollX * animatedImage.HorizontalResolution / zoom / ppi), invalidRect.Y + (float)(scrollY * animatedImage.VerticalResolution / zoom / ppi), invalidRect.Width, invalidRect.Height);
										RectangleF figuresRect = new RectangleF(oldRect.X + (float)(scrollX * animatedImage.HorizontalResolution / zoom / ppi), oldRect.Y + (float)(scrollY * animatedImage.VerticalResolution / zoom / ppi), oldRect.Width, oldRect.Height);

										float xbit = figuresInvRect.X;
										float ybit = figuresInvRect.Y;
										float ix = xbit > figuresRect.X ? figuresRect.X : xbit;
										float iy = ybit > figuresRect.Y ? figuresRect.Y : ybit;
										int iwidth = (int)(xbit + bit.Width > figuresRect.Right ? xbit + bit.Width - ix : figuresRect.Right - ix) + 1;
										int iheight = (int)(ybit + bit.Height > figuresRect.Bottom ? ybit + bit.Height - iy : figuresRect.Bottom - iy) + 1;
										using(Bitmap generalBitmap = new Bitmap(iwidth, iheight))
										{
											generalBitmap.SetResolution(cachedBitmap.HorizontalResolution, cachedBitmap.VerticalResolution);
											using(Graphics ggr = Graphics.FromImage(generalBitmap))
											{
												//											ggr.SmoothingMode = SmoothingMode.HighQuality;
												ggr.InterpolationMode = CurrentInterpolationMode;
												if(invalidRect != Rectangle.Empty)
													ggr.DrawImage(bit, xbit > figuresRect.X ? xbit - figuresRect.X : 0, ybit > figuresRect.Y ? ybit - figuresRect.Y : 0);

												if(SelectedBitmap.Attributes.UType == TiffAnnotation.AnnotationMarkType.HollowRectangle)
												{
													ggr.DrawRectangle(new Pen(new SolidBrush(Color.FromArgb(100, 100, 100, 100)), 4), figuresRect.X > xbit ? figuresRect.X - xbit : 0, figuresRect.Y > ybit ? figuresRect.Y - ybit : 0, figuresRect.Width, figuresRect.Height);
												}
												else
													ggr.FillRectangle(new SolidBrush(Color.FromArgb(100, 100, 100, 100)), figuresRect.X > xbit ? figuresRect.X - xbit : 0, figuresRect.Y > ybit ? figuresRect.Y - ybit : 0, figuresRect.Width, figuresRect.Height);

											}
											//учитываем смещение
											//далее создаем ограничеие области рисования
											float xGImgLeft = (float)(ix * zoom * ppi / animatedImage.HorizontalResolution) + rectAnimatedImage.X;
											float yGImgTop = (float)(iy * zoom * ppi / animatedImage.VerticalResolution) + rectAnimatedImage.Y;
											float xGImgRight = xGImgLeft + (float)(iwidth * zoom * ppi / animatedImage.HorizontalResolution);
											float yGImgBottom = yGImgTop + (float)(iheight * zoom * ppi / animatedImage.VerticalResolution);
											float xGImgLeftNew = xGImgLeft;
											float xGImgRightNew = xGImgRight;
											float yGImgTopNew = yGImgTop;
											float yGImgBottomNew = yGImgBottom;
											if(xGImgLeft < this.rectAnimatedImage.X + this.thicknessFrame)
											{
												xGImgLeftNew = this.rectAnimatedImage.X + (this.thicknessFrame);
											}
											else if(xGImgRight > this.realWidthImage + (float)this.rectAnimatedImage.X - (this.thicknessFrame))
											{
												xGImgRightNew = this.realWidthImage + (float)this.rectAnimatedImage.X - (this.thicknessFrame);
											}
											if(yGImgTopNew < this.rectAnimatedImage.Y + (this.thicknessFrame))
											{
												yGImgTopNew = this.rectAnimatedImage.Y + this.thicknessFrame;
											}
											else if(yGImgBottomNew > this.realHeightImage + (float)this.rectAnimatedImage.Y - (this.thicknessFrame))
											{
												yGImgBottomNew = this.realHeightImage + (float)this.rectAnimatedImage.Y - (this.thicknessFrame);
											}

											RectangleF dst = new RectangleF(xGImgLeftNew, yGImgTopNew, xGImgRightNew - xGImgLeftNew, yGImgBottomNew - yGImgTopNew);
											RectangleF src = new RectangleF((xGImgLeftNew - xGImgLeft) * animatedImage.HorizontalResolution / ppi / (float)zoom, (yGImgTopNew - yGImgTop) * animatedImage.VerticalResolution / ppi / (float)zoom, (xGImgRightNew - xGImgLeftNew) * animatedImage.HorizontalResolution / ppi / (float)zoom, (yGImgBottomNew - yGImgTopNew) * animatedImage.VerticalResolution / ppi / (float)zoom);
											g.DrawImage(generalBitmap, dst, src, GraphicsUnit.Pixel);
										}

									}
								}
							}
							invalidRect = new Rectangle(x - sin, y - sin, oldRect.Width + (sin << 1) - 1, oldRect.Height + (sin << 1) - 1);
							return;
						}
					}

				}

				#endregion
			}
			else if(((TypeWorkAnimatedImage == TypeWorkImage.CreateNotes && AnnotationState == AnnotationsState.Create) || //создание заметок
				UserAction == UsersActionsTypes.SelectionMode || UserAction == UsersActionsTypes.SelectionNotes) && //создание фрагмента
				IsMouseOnRectMain(this.rectAnimatedImage, new Point(e.X, e.Y)) && e.Button == MouseButtons.Left)
			{
				#region Создание заметок
				Point mPointOffset = new Point(e.X - this.rectAnimatedImage.X, e.Y - this.rectAnimatedImage.Y);
				if((Cursor == TiffCursors.Marker || Cursor == TiffCursors.HRect || Cursor == TiffCursors.RectText || Cursor == TiffCursors.Note || Cursor == Cursors.NoMove2D) || UserAction == UsersActionsTypes.SelectionMode || UserAction == UsersActionsTypes.SelectionNotes)
				{
					int x = (int)(lastPositionForDrag.X * animatedImage.HorizontalResolution / (zoom * ppi));
					int y = (int)(lastPositionForDrag.Y * animatedImage.VerticalResolution / (zoom * ppi));
					int xnew = (int)(mPointOffset.X * animatedImage.HorizontalResolution / (zoom * ppi));
					int ynew = (int)(mPointOffset.Y * animatedImage.VerticalResolution / (zoom * ppi));
					int left = 0, top = 0, rigth = 0, bottom = 0;

					if(x > xnew)
					{
						left = xnew;
						rigth = mPointOffset.X > zoomWigth ? (int)(zoomWigth * animatedImage.HorizontalResolution / (zoom * ppi)) : x;
					}
					else
					{
						left = x;
						rigth = mPointOffset.X > zoomWigth ? (int)(zoomWigth * animatedImage.HorizontalResolution / (zoom * ppi)) : xnew;
					}
					if(y > ynew)
					{
						top = ynew;
						bottom = mPointOffset.Y > zoomHeigth ? (int)(zoomHeigth * animatedImage.VerticalResolution / (zoom * ppi)) : y;
					}
					else
					{
						top = y;
						bottom = mPointOffset.Y > zoomHeigth ? (int)(zoomHeigth * animatedImage.VerticalResolution / (zoom * ppi)) : ynew;
					}

					oldRect = new Rectangle(left, top, rigth - left, bottom - top);

					using(Bitmap bit = new Bitmap(invalidRect.Width > 0 ? invalidRect.Width : 1, invalidRect.Height > 0 ? invalidRect.Height : 1))
					using(Graphics gr = Graphics.FromImage(bit))
					{
						//						gr.SmoothingMode = SmoothingMode.HighQuality;
						gr.InterpolationMode = CurrentInterpolationMode;
						gr.DrawImage(cachedBitmap, new Rectangle(0, 0, (invalidRect.Width > 0 ? invalidRect.Width : 1), (invalidRect.Height > 0 ? invalidRect.Height : 1)),
							new Rectangle(invalidRect.X, invalidRect.Y, (invalidRect.Width > 0 ? invalidRect.Width : 1), (invalidRect.Height > 0 ? invalidRect.Height : 1)), GraphicsUnit.Pixel);
						using(Graphics g = Graphics.FromHwnd(Handle))
						{
							//							g.SmoothingMode = SmoothingMode.HighQuality;
							g.InterpolationMode = CurrentInterpolationMode;
							if(CurrentInterpolationMode == InterpolationMode.High)
								g.PixelOffsetMode = PixelOffsetMode.HighQuality;
							int xbit = invalidRect.Location.X + (int)(scrollX * animatedImage.HorizontalResolution / (zoom * ppi));
							int ybit = invalidRect.Location.Y + (int)(scrollY * animatedImage.VerticalResolution / (zoom * ppi));
							int ix = xbit > oldRect.X ? oldRect.X : xbit;
							int iy = ybit > oldRect.Y ? oldRect.Y : ybit;
							int dx = oldRect.X > xbit ? oldRect.X - xbit : 0;
							int dy = oldRect.Y > ybit ? oldRect.Y - ybit : 0;
							int iwidth = (xbit + bit.Width > oldRect.Right ? xbit + bit.Width - ix : oldRect.Right - ix) + 1;
							int iheight = (ybit + bit.Height > oldRect.Bottom ? ybit + bit.Height - iy : oldRect.Bottom - iy) + 1;

							using(Bitmap generalBitmap = new Bitmap(iwidth, iheight))
							{
								using(Graphics ggr = Graphics.FromImage(generalBitmap))
								{
									// ggr.SmoothingMode = SmoothingMode.HighQuality;
									ggr.InterpolationMode = CurrentInterpolationMode;
									// зарисовываем старый
									ggr.DrawImage(bit, xbit > oldRect.X ? xbit - oldRect.X : 0, ybit > oldRect.Y ? ybit - oldRect.Y : 0);
									if(Cursor == TiffCursors.HRect)
									{
										ggr.DrawRectangle(new Pen(new SolidBrush(Color.FromArgb(100, 100, 100, 100)), 2), dx, dy, oldRect.Width, oldRect.Height);
										// g.FillRectangle(new SolidBrush(Color.FromArgb(100, 100, 100, 100)), new Rectangle(oldRect.X  + 100, oldRect.Y, oldRect.Width, oldRect.Height));
									}
									else if(Cursor != Cursors.NoMove2D && UserAction != UsersActionsTypes.SelectionMode && UserAction != UsersActionsTypes.SelectionNotes)
										ggr.FillRectangle(new SolidBrush(Color.FromArgb(100, 100, 100, 100)), dx, dy, oldRect.Width, oldRect.Height);
								}

								// учитываем смещение, далее создаем ограничение области рисования
								float xGImgLeft = (float)(ix * zoom * ppi / animatedImage.HorizontalResolution) + rectAnimatedImage.X;
								float yGImgTop = (float)(iy * zoom * ppi / animatedImage.VerticalResolution) + rectAnimatedImage.Y;
								float xGImgRight = xGImgLeft + (float)(iwidth * zoom * ppi / animatedImage.HorizontalResolution);
								float yGImgBottom = yGImgTop + (float)(iheight * zoom * ppi / animatedImage.VerticalResolution);
								float xGImgLeftNew = xGImgLeft, xGImgRightNew = xGImgRight, yGImgTopNew = yGImgTop, yGImgBottomNew = yGImgBottom;

								if(xGImgLeft < this.rectAnimatedImage.X + this.thicknessFrame)
									xGImgLeftNew = this.rectAnimatedImage.X + (this.thicknessFrame);
								else if(xGImgRight > this.realWidthImage + (float)this.rectAnimatedImage.X - (this.thicknessFrame))
									xGImgRightNew = this.realWidthImage + (float)this.rectAnimatedImage.X - (this.thicknessFrame);
								if(yGImgTopNew < this.rectAnimatedImage.Y + (this.thicknessFrame))
									yGImgTopNew = this.rectAnimatedImage.Y + this.thicknessFrame;
								else if(yGImgBottomNew > this.realHeightImage + (float)this.rectAnimatedImage.Y - (this.thicknessFrame))
									yGImgBottomNew = this.realHeightImage + (float)this.rectAnimatedImage.Y - (this.thicknessFrame);

								RectangleF dst = new RectangleF(xGImgLeftNew, yGImgTopNew, xGImgRightNew - xGImgLeftNew, yGImgBottomNew - yGImgTopNew);
								RectangleF src = new RectangleF((xGImgLeftNew - xGImgLeft) * animatedImage.HorizontalResolution / ((float)zoom * ppi), (yGImgTopNew - yGImgTop) * animatedImage.VerticalResolution / ((float)zoom * ppi), (xGImgRightNew - xGImgLeftNew) * animatedImage.HorizontalResolution / ((float)zoom * ppi), (yGImgBottomNew - yGImgTopNew) * animatedImage.VerticalResolution / ((float)zoom * ppi));
								g.DrawImage(generalBitmap, dst, src, GraphicsUnit.Pixel);
								if(UserAction == UsersActionsTypes.SelectionMode)
								{
									DrawRectangle(g, DashStyle.Dot);
								}
								else if(UserAction == UsersActionsTypes.SelectionNotes)
								{
									DrawRectangle(g, DashStyle.Dash);
								}
							}
						}
					}
					invalidRect = new Rectangle(left - (int)(scrollX * animatedImage.HorizontalResolution / (zoom * ppi)) - sin, top - (int)(scrollY * animatedImage.VerticalResolution / (zoom * ppi)) - sin, oldRect.Width + (sin << 1) - 1, oldRect.Height + (sin << 1) - 1);
					if(UserAction == UsersActionsTypes.SelectionNotes)
						selectionNotesRectangle = new Rectangle(invalidRect.X + sin, invalidRect.Y + sin, invalidRect.Width - sin * 2, invalidRect.Height - sin * 2);

					return;
				}
				if(AnnotationState == AnnotationsState.CreateText || AnnotationState == AnnotationsState.EditText)
					return;
				#endregion
			}
			else // никаких действий пользователь не сделал, только изменение курсоров над объектами
			{
				#region Показ курсоров
				//Показ сплитера
				if(showThumbPanel && IsMouseOnRectMain(rectSplitter, new Point(e.X, e.Y)))
				{
					if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
					{
						if(Cursor != Cursors.HSplit)
							Cursor = Cursors.HSplit;
					}
					else
					{
						if(Cursor != Cursors.VSplit)
							Cursor = Cursors.VSplit;

					}
				}
				//Показ руки над превьюшками
				else if(IsMouseOnRectMain(rectThumbnailPanel, new Point(e.X, e.Y)))
				{
					bool isMouseHover = false;
					foreach(VisiblePreview rect in listvis)
					{
						if(IsMouseOnRectMain(rect.rect, new Point(e.X, e.Y)))
						{
							isMouseHover = true;
							break;
						}
					}
					if(isMouseHover)
						Cursor = Cursors.Hand;
					else
						Cursor = Cursors.Default;
				}
				//показ курсоров заметок если выбран режим создания заметок
				else if(TypeWorkAnimatedImage == TypeWorkImage.CreateNotes && IsMouseOnRectMain(new Rectangle(rectAnimatedImage.X, rectAnimatedImage.Y, realWidthImage, realHeightImage), new Point(e.X, e.Y)))
				{
					if(UserAction == UsersActionsTypes.DrawFRect)
					{
						if(Cursor != TiffCursors.FRect)
							Cursor = TiffCursors.FRect;
					}
					else if(UserAction == UsersActionsTypes.DrawMarker)
					{
						if(Cursor != TiffCursors.Marker)
							Cursor = TiffCursors.Marker;
					}
					else if(UserAction == UsersActionsTypes.DrawHRect)
					{
						if(Cursor != TiffCursors.HRect)
							Cursor = TiffCursors.HRect;
					}
					else if(UserAction == UsersActionsTypes.DrawRectText)
					{
						if(Cursor != TiffCursors.RectText)
							Cursor = TiffCursors.RectText;
					}
					else if(UserAction == UsersActionsTypes.DrawNote)
					{
						if(Cursor != TiffCursors.Note)
							Cursor = TiffCursors.Note;
					}
					else if(UserAction == UsersActionsTypes.DrawImage)
					{
						if(Cursor != Cursors.NoMove2D)
							Cursor = Cursors.NoMove2D;
					}
					else
						Cursor = Cursors.Default;
				}
				else if(TypeWorkAnimatedImage == TypeWorkImage.EditNotes && IsMouseOnRectMain(this.rectAnimatedImage, new Point(e.X, e.Y)))
					ShowSizeCursor(e.Location);	// показ курсоров для изменения размеров
				else
					if(typeWorkAnimatedImage == TypeWorkImage.SelectionMode)
						this.Cursor = Cursors.Default;
					else
						Cursor = TiffCursors.Hand;
				#endregion
			}
		}

		private void DrawRectangle(Graphics g, DashStyle ds)
		{
			using(Pen dotedPen = new Pen(new SolidBrush(Color.Black), 1))
			{
				dotedPen.DashStyle = ds;
				dotedPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Bevel;
				g.DrawLine(dotedPen, new PointF(this.rectAnimatedImage.X + (oldRect.X * (float)zoom * ppi / animatedImage.HorizontalResolution), this.rectAnimatedImage.Y + (oldRect.Y * (float)zoom * ppi / animatedImage.VerticalResolution)), new PointF(this.rectAnimatedImage.X + (oldRect.X * (float)zoom * ppi / animatedImage.HorizontalResolution), this.rectAnimatedImage.Y + (oldRect.Y * (float)zoom * ppi / animatedImage.VerticalResolution) + (oldRect.Height * (float)zoom * ppi / animatedImage.VerticalResolution)));
				g.DrawLine(dotedPen, new PointF(this.rectAnimatedImage.X + (oldRect.X * (float)zoom * ppi / animatedImage.HorizontalResolution), this.rectAnimatedImage.Y + (oldRect.Y * (float)zoom * ppi / animatedImage.VerticalResolution)), new PointF(this.rectAnimatedImage.X + (oldRect.X * (float)zoom * ppi / animatedImage.HorizontalResolution) + (oldRect.Width * (float)zoom * ppi / animatedImage.HorizontalResolution), this.rectAnimatedImage.Y + (oldRect.Y * (float)zoom * ppi / animatedImage.VerticalResolution)));
				g.DrawLine(dotedPen, new PointF(this.rectAnimatedImage.X + (oldRect.X * (float)zoom * ppi / animatedImage.HorizontalResolution), this.rectAnimatedImage.Y + (oldRect.Y * (float)zoom * ppi / animatedImage.VerticalResolution) + (oldRect.Height * (float)zoom * ppi / animatedImage.VerticalResolution)), new PointF(this.rectAnimatedImage.X + (oldRect.X * (float)zoom * ppi / animatedImage.HorizontalResolution) + (oldRect.Width * (float)zoom * ppi / animatedImage.HorizontalResolution), this.rectAnimatedImage.Y + (oldRect.Y * (float)zoom * ppi / animatedImage.VerticalResolution) + (oldRect.Height * (float)zoom * ppi / animatedImage.VerticalResolution)));
				g.DrawLine(dotedPen, new PointF(this.rectAnimatedImage.X + (oldRect.X * (float)zoom * ppi / animatedImage.HorizontalResolution) + (oldRect.Width * (float)zoom * ppi / animatedImage.HorizontalResolution), this.rectAnimatedImage.Y + (oldRect.Y * (float)zoom * ppi / animatedImage.VerticalResolution)), new PointF(this.rectAnimatedImage.X + (oldRect.X * (float)zoom * ppi / animatedImage.HorizontalResolution) + (float)(oldRect.Width * (float)zoom * ppi / animatedImage.HorizontalResolution), this.rectAnimatedImage.Y + (oldRect.Y * (float)zoom * ppi / animatedImage.VerticalResolution) + (oldRect.Height * (float)zoom * ppi / animatedImage.VerticalResolution)));
				dotedPen.Dispose();
			}
		}

		private void SplitterMoveDraw(int x, int y)
		{
			using(Bitmap bmps = new Bitmap(rectSplitter.Width, rectSplitter.Height))
			{
				using(Graphics grold = Graphics.FromImage(bmps))
				{
					using(Bitmap bm = (Bitmap)fullCahedBitmap.Clone())
					{
						grold.DrawImage(bm, new Rectangle(0, 0, rectSplitter.Width, rectSplitter.Height), rectSplitter, GraphicsUnit.Pixel);
					}
				}
				using(Graphics gr = Graphics.FromHwnd(this.Handle))
				{
					gr.DrawImage(bmps, rectSplitter.X, rectSplitter.Y);
					rectSplitter = new Rectangle(x, y, rectSplitter.Width, rectSplitter.Height);
					gr.FillRectangle(Brushes.Gray, rectSplitter);
				}
			}
		}

		/// <summary>
		/// Показ курсоров для изменения размеров заметок
		/// </summary>
		private void ShowSizeCursor(Point mouseMovePt)
		{
			if(selectedRectangles != null)
			{
				Point mPointOffset = new Point(mouseMovePt.X - this.rectAnimatedImage.X, mouseMovePt.Y - this.rectAnimatedImage.Y);
				Cursor = Cursors.Default;
				foreach(System.Collections.DictionaryEntry kvp in notesToSelectedRectangles)
				{
					// разрешаем изменять размеры всех заметок, кроме штампов
					if(!(kvp.Key is TiffAnnotation.ImageEmbedded))
					{
						Rectangle[] noteSelectedRectangles = (Rectangle[])kvp.Value;
						if(IsMouseOnRect(noteSelectedRectangles[0], mPointOffset))
						{
							Cursor = Cursors.SizeNWSE;
							CursorDirection = Direction.UL;
							break;
						}
						else if(IsMouseOnRect(noteSelectedRectangles[1], mPointOffset))
						{
							Cursor = Cursors.SizeNS;
							CursorDirection = Direction.U;
							break;
						}
						else if(IsMouseOnRect(noteSelectedRectangles[2], mPointOffset))
						{
							Cursor = Cursors.SizeNESW;
							CursorDirection = Direction.UR;
							break;
						}
						else if(IsMouseOnRect(noteSelectedRectangles[3], mPointOffset))
						{
							Cursor = Cursors.SizeWE;
							CursorDirection = Direction.R;
							break;
						}
						else if(IsMouseOnRect(noteSelectedRectangles[4], mPointOffset))
						{
							Cursor = Cursors.SizeNWSE;
							CursorDirection = Direction.DR;
							break;
						}
						else if(IsMouseOnRect(noteSelectedRectangles[5], mPointOffset))
						{
							Cursor = Cursors.SizeNS;
							CursorDirection = Direction.D;
							break;
						}
						else if(IsMouseOnRect(noteSelectedRectangles[6], mPointOffset))
						{
							Cursor = Cursors.SizeNESW;
							CursorDirection = Direction.DL;
						}
						else if(IsMouseOnRect(noteSelectedRectangles[7], mPointOffset))
						{
							Cursor = Cursors.SizeWE;
							CursorDirection = Direction.L;
							break;
						}
					}
				}
			}
			else
				Cursor = Cursors.Default;
		}

		protected override void OnDoubleClick(EventArgs e)
		{
			if(SelectedBitmap != null && SelectedBitmap is TiffAnnotation.TypedText && SelectedBitmap.Attributes.OiGroup == GetCurrentAnnotationGroup())
			{
				rTextBox = new RichTextBox();
				rTextBox.ScrollBars = RichTextBoxScrollBars.None;
				rTextBox.BorderStyle = BorderStyle.None;
				if(SelectedBitmap is TiffAnnotation.AttachANote)
				{
					rTextBox.BackColor = ((TiffAnnotation.AttachANote)SelectedBitmap).RgbColor1;
					rTextBox.ForeColor = ((TiffAnnotation.AttachANote)SelectedBitmap).RgbColor2;
				}
				else
					rTextBox.ForeColor = ((TiffAnnotation.TypedText)SelectedBitmap).RgbColor1;
				rTextBox.Font = SelectedBitmap.GetZoomFont(zoom);
				rTextBox.LostFocus += new EventHandler(rTextBox_LostFocus);
				rTextBox.Location = new Point((int)Math.Round((SelectedBitmap.Rect.Location.X * zoom * ppi / animatedImage.HorizontalResolution)) + this.scrollX + this.rectAnimatedImage.X + 1, (int)Math.Round((SelectedBitmap.Rect.Location.Y * zoom * ppi / animatedImage.HorizontalResolution)) + this.scrollY + this.rectAnimatedImage.Y + 1);
				rTextBox.Size = new Size((int)(SelectedBitmap.Rect.Size.Width * zoom * ppi / animatedImage.HorizontalResolution), (int)(SelectedBitmap.Rect.Size.Height * zoom * ppi / animatedImage.VerticalResolution));
				rTextBox.Text = SelectedBitmap.Attributes.OiAnText_OIAN_TEXTPRIVDATA.SzAnoText;
				this.Controls.Add(rTextBox);
				rTextBox.Focus();
				AnnotationState = AnnotationsState.EditText;
			}
		}

		#endregion

		protected override bool ProcessDialogKey(Keys keyData)
		{
			switch(keyData)
			{
				case Keys.PageUp:
					TryChangePage(SelectedIndex - 1);
					break;
				case Keys.PageDown:
					TryChangePage(SelectedIndex + 1);
					break;
				case Keys.PageUp | Keys.Shift:
					MovePage(SelectedIndex);
					//TryChangePage(SelectedIndex - 1);
					break;
				case Keys.PageDown | Keys.Shift:
					MovePage(SelectedIndex + 1);
					//TryChangePage(SelectedIndex + 1);
					break;

			}
			return base.ProcessDialogKey(keyData);

		}

		void rTextBox_LostFocus(object sender, EventArgs e)
		{
			EndEditFiguresText();
			IsRefreshBitmap = true;
			Invalidate(this.rectAnimatedImage);
		}

		/// <summary>
		/// Конец редактирования текстовых заметок
		/// </summary>
		private void EndEditFiguresText()
		{
			if(rTextBox == null)
				return;
			if(!string.IsNullOrEmpty(rTextBox.Text))
			{
				if(SelectedBitmap == null)
				{
					Rectangle figuresRectangle = new Rectangle(invalidRect.X + sin, invalidRect.Y + sin, invalidRect.Width - sin * 2, invalidRect.Height - sin * 2);

					switch(UserAction)
					{
						case UsersActionsTypes.DrawNote:
							TiffAnnotation.CreateNote(figuresRectangle, rTextBox.Text, GetCurrentAnnotationGroup());
							break;
						case UsersActionsTypes.DrawRectText:
							TiffAnnotation.CreateTypedText(figuresRectangle, rTextBox.Text, GetCurrentAnnotationGroup());
							break;
					}
				}
				else
					SelectedBitmap.ChangeText(rTextBox.Text);
				SetModifiedMarks(true);

			}
			invalidRect = new Rectangle();
			oldRect = new Rectangle();
			rTextBox.LostFocus -= rTextBox_LostFocus;
			Controls.Remove(rTextBox);
			if(rTextBox != null)
			{
				rTextBox.Dispose();
				rTextBox = null;
			}

			AnnotationState = AnnotationsState.None;
			Cursor = Cursors.Arrow;
		}

		/// <summary>
		/// Создание заметок
		/// </summary>
		public TiffAnnotation TiffAnnotation
		{
			get
			{
				if(tiffAnnotation == null)
				{
					tiffAnnotation = new TiffAnnotation(this);
					tiffAnnotation.ModifiedFigure += annotation_ModifiedFigure;
				}

				if(markGroupsVisibleList == null)
					markGroupsVisibleList = new Hashtable();

				if(!markGroupsVisibleList.ContainsKey(GetCurrentAnnotationGroup()))
					markGroupsVisibleList.Add(GetCurrentAnnotationGroup(), true);

				return tiffAnnotation;
			}
		}

		/// <summary>
		/// установка ограничений на отображаемые страницы документа
		/// </summary>
		/// <param name="pages">массив страниц</param>
		/// <returns>корректность установки ограничений</returns>
		public bool SetVisiblePages(List<int> pages)
		{
			if(pages == null)
			{
				ResetVisiblePages();
				return true;
			}
			if(pages.Count < 1)
				return false;
			pages.Sort();
			if(pages[0] < 0 || pages[pages.Count - 1] > countPreview)
				return false;
			ResetVisiblePages();
			//visiblePages = new SynchronizedCollection<int>();
			//foreach(var i in pages)
			//    visiblePages.Add(i);
			return true;
		}

		public void ResetVisiblePages()
		{
			
		}

		private GraphicsPath GetIconNotes(Point point)
		{
			GraphicsPath gp = new GraphicsPath(System.Drawing.Drawing2D.FillMode.Alternate);
			gp.StartFigure();
			gp.AddLine(point.X, point.Y + 2, point.X + 8, point.Y + 2);
			gp.AddLine(point.X + 8, point.Y + 11, point.X, point.Y + 11);
			gp.AddLine(point.X, point.Y + 11, point.X, point.Y + 2);
			gp.AddLine(point.X + 4, point.Y + 2, point.X, point.Y + 6);
			gp.CloseFigure();
			gp.StartFigure();
			gp.AddLine(point.X + 3, point.Y + 6, point.X + 9, point.Y);
			gp.AddLine(point.X + 10, point.Y + 2, point.X + 4, point.Y + 8);
			gp.AddLine(point.X + 2, point.Y + 8, point.X + 3, point.Y + 6);
			gp.CloseAllFigures();
			return gp;
		}

		/// <summary>
		/// Отрисовка контрола
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
#if AdvancedLogging
			Log.Logger.EnterMethod(this, "OnPaint(PaintEventArgs e)");
#endif
			// битмап, на котором будем рисовать
			try
			{
				using(Bitmap bitmap = new Bitmap(this.Width, this.Height))
				{
					using(Graphics gr = Graphics.FromImage(bitmap))
					{
						//gr.SmoothingMode = SmoothingMode.HighQuality;

						#region Рисование превьюшек
						// высота битмапа (либо равна высоте контрола либо высоте всех превьюшек)
						int thumbnailImagesBitmapHeight = heightThumbnailImage;
						int thumbnailImagesBitmapWidth = widthThumbnailImage;
						if(showThumbPanel && !string.IsNullOrEmpty(fileName))
						{
							if(thumbnailImagesBitmapWidth > 1 && thumbnailImagesBitmapHeight > 1)
							{
								if(ThumbnailImagesBitmap == null || !this.isLockThumbnailImages)
								{
									try
									{
										ThumbnailImagesBitmap = new Bitmap(thumbnailImagesBitmapWidth - 1, thumbnailImagesBitmapHeight - 1);
									}
									catch(Exception ex)
									{
										ThumbnailImagesBitmap = null;
										Tiff.LibTiffHelper.WriteToLog(ex);
									}
									if(ThumbnailImagesBitmap != null)
									{
										using(Graphics grPreview = Graphics.FromImage(ThumbnailImagesBitmap))
										{
											//grPreview.SmoothingMode = SmoothingMode.HighQuality;
											foreach(VisiblePreview vp in listvis)
											{
												if(previews == null)
													break;

												Bitmap img = null;
												if(previews.Count > vp.index)
													img = previews[vp.index].Key;
												if(img != null)
												{

													if(thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Top || thumbnailPanelOrientation == TypeThumbnailPanelOrientation.Bottom)
													{
														grPreview.FillRectangle(Brushes.White, vp.rectPaint.X - 1, vp.rectPaint.Y - 1, img.Width + 2, img.Height + 2);
														grPreview.FillRectangle(Brushes.Gray, vp.rectPaint.X, vp.rectPaint.Y, img.Width, img.Height);
														grPreview.DrawRectangle(Pens.Black, vp.rectPaint.X - 2, vp.rectPaint.Y - 2, img.Width + 4, img.Height + 4);
														grPreview.DrawImage(img, vp.rectPaint.X, vp.rectPaint.Y, img.Width, img.Height);
														if(SelectedIndex == vp.index)
															grPreview.DrawRectangle(new Pen(Brushes.Black, 2), vp.rectPaint.X - 4, vp.rectPaint.Y - 4, img.Width + 8, img.Height + 8);

														grPreview.DrawString((vp.index + 1).ToString(), Font, Brushes.Black, vp.rectPaint.X, vp.rectPaint.Y + img.Height + 6);
														if(previews[vp.index].Value)
														{
															SizeF st = grPreview.MeasureString((vp.index + 1).ToString(), Font);
															using(GraphicsPath path = GetIconNotes(new Point(vp.rectPaint.X + (int)st.Width + 3, vp.rectPaint.Y + img.Height + 6)))
																grPreview.DrawPath(Pens.Black, path);
														}
													}
													else
													{
														grPreview.FillRectangle(Brushes.White, vp.rectPaint.X - 1, vp.rectPaint.Y - 1, img.Width + 2, img.Height + 2);
														grPreview.FillRectangle(Brushes.Gray, vp.rectPaint.X, vp.rectPaint.Y, img.Width, img.Height);
														grPreview.DrawRectangle(Pens.Black, vp.rectPaint.X - 2, vp.rectPaint.Y - 2, img.Width + 4, img.Height + 4);
														grPreview.DrawImage(img, vp.rectPaint.X, vp.rectPaint.Y, img.Width, img.Height);

														if(SelectedIndex == vp.index)
															grPreview.DrawRectangle(new Pen(Brushes.Black, 2), vp.rectPaint.X - 4, vp.rectPaint.Y - 4, img.Width + 8, img.Height + 8);
														grPreview.DrawString((vp.index + 1).ToString(), Font, Brushes.Black, vp.rectPaint.X, vp.rectPaint.Y + img.Height + 6);
														if(previews[vp.index].Value)
														{
															SizeF st = grPreview.MeasureString((vp.index + 1).ToString(), Font);
															using(GraphicsPath path = GetIconNotes(new Point(vp.rectPaint.X + (int)st.Width + 3, vp.rectPaint.Y + img.Height + 6)))
																grPreview.DrawPath(Pens.Black, path);
														}
													}

												}
												else
												{
													grPreview.FillRectangle(Brushes.White, vp.rectPaint.X - 1, vp.rectPaint.Y - 1, 102, 142);
													grPreview.FillRectangle(Brushes.Gray, vp.rectPaint.X, vp.rectPaint.Y, 100, 140);
													grPreview.DrawRectangle(Pens.Black, vp.rectPaint.X - 2, vp.rectPaint.Y - 2, 104, 144);
													if(SelectedIndex == vp.index)
														grPreview.DrawRectangle(new Pen(Brushes.Black, 2), vp.rectPaint.X - 4, vp.rectPaint.Y - 4, 108, 148);
													grPreview.DrawString((vp.index + 1).ToString(), Font, Brushes.Black, vp.rectPaint.X, vp.rectPaint.Y + 146);
												}
											}
										}
									}
									this.isLockThumbnailImages = true;
								}
								try
								{
									gr.FillRectangle(Brushes.White, rectThumbnailPanel.X + 1, rectThumbnailPanel.Y + 1, widthThumbnailImage - 3, heightThumbnailImage - 3);
									if(ThumbnailImagesBitmap != null && ThumbnailImagesBitmap.Width > 0 && ThumbnailImagesBitmap.Height > 0)
										gr.DrawImage(ThumbnailImagesBitmap, rectThumbnailPanel.X + 1, rectThumbnailPanel.Y + 1);
									else
										gr.FillRectangle(Brushes.White, 0, 0, this.Width, this.Height);
									gr.DrawRectangle(Pens.Black, rectThumbnailPanel.X, rectThumbnailPanel.Y, widthThumbnailImage - 1, heightThumbnailImage - 1);
								}
								catch(Exception ex)
								{
									Tiff.LibTiffHelper.WriteToLog(ex);
								}

							}
							else
							{
								try
								{
									gr.FillRectangle(Brushes.White, 0, 0, this.Width, this.Height);
									gr.IntersectClip(rectSplitter);
									gr.DrawRectangle(Pens.Black, 0, 0, this.Width - 1, this.Height - 1);
								}
								catch(Exception ex)
								{
									Tiff.LibTiffHelper.WriteToLog(ex);
								}
							}
						}
						else if(thumbnailImagesBitmapWidth > 1 && thumbnailImagesBitmapHeight > 1)
						{
							try
							{
								gr.FillRectangle(Brushes.White, rectThumbnailPanel.X + 1, rectThumbnailPanel.Y + 1, widthThumbnailImage - 3, heightThumbnailImage - 3);
								gr.DrawRectangle(Pens.Black, rectThumbnailPanel.X, rectThumbnailPanel.Y, widthThumbnailImage - 1, heightThumbnailImage - 1);
							}
							catch(Exception ex)
							{
								Tiff.LibTiffHelper.WriteToLog(ex);
							}
						}

						///конец рисования превьюшек///////////////////////////////////////////////////
						///////////////////////////////////////////////////////////////////////////////
						#endregion


						/////////////////////////////////////////////////////////////////////////////////
						//Рисование картинки////////////////////////////////////////////////////////////
						#region Рисование картинки
						try
						{
							lock(lo)
							using(Bitmap bitmapMainImage = new Bitmap(realWidthImage, realHeightImage))
							{
								using(Graphics grMainImage = Graphics.FromImage(bitmapMainImage))
								{
									//grMainImage.SmoothingMode = SmoothingMode.HighQuality;
									grMainImage.InterpolationMode = CurrentInterpolationMode;
									if(animatedImage != null)
									{
										if(IsRefreshBitmap)
										{
											Bitmap bufferAnimatedImage = animatedImage;
											if(animatedImage.PixelFormat != CurrentPixelFormat || (CurrentPixelFormat == PixelFormat.Format8bppIndexed && !libTiff.IsPalleteGrayscale(animatedImage.Palette)))
											{
												Bitmap tempImage = libTiff.ConvertTo(CurrentPixelFormat, bufferAnimatedImage.Clone() as Bitmap);
												if(tempImage != null)
													bufferAnimatedImage = tempImage;
											}
											if(HasAnnotation() || IsAnnuled)
											{
												Bitmap bitmapImg = null;
												//if(bufferAnimatedImage.PixelFormat == PixelFormat.Format1bppIndexed)
												//	bitmapImg = bufferAnimatedImage.Clone(new Rectangle(0, 0, animatedImage.Width, animatedImage.Height), PixelFormat.Format24bppRgb);
												//else
												bitmapImg = bufferAnimatedImage.Clone(new Rectangle(0, 0, animatedImage.Width, animatedImage.Height), PixelFormat.Format24bppRgb);
												if(bufferAnimatedImage != animatedImage)
												bufferAnimatedImage.Dispose();
												bufferAnimatedImage = null;
												if(animatedImage.HorizontalResolution == 0)
													bitmapImg.SetResolution(200, 200);
												else
													bitmapImg.SetResolution(this.animatedImage.HorizontalResolution, this.animatedImage.VerticalResolution);
												using(Graphics g = Graphics.FromImage(bitmapImg))
												{
													g.InterpolationMode = CurrentInterpolationMode;
													renderAnnotations.DrawAnnotation(g, this.tiffAnnotation, markGroupsVisibleList, bitmapImg, CurrentInterpolationMode, ref this.selectedRectangles, this.notesToSelectedRectangles);
													if(AnnotationState == AnnotationsState.CreateText)
													{
														g.FillRectangle(Brushes.Black, new RectangleF(invalidRect.X + sin - 1, invalidRect.Y + sin - 1, invalidRect.Width - sin * 2 - 1, invalidRect.Height - sin * 2 - 1));
														DrawSelectedRectangle(g, new Rectangle(invalidRect.X + sin, invalidRect.Y + sin, invalidRect.Width - sin * 2, invalidRect.Height - sin * 2));
													}
												}

												if(cachedBitmap != null)
													cachedBitmap.Dispose();
												cachedBitmap = (Bitmap)bitmapImg.Clone();
												bitmapImg.Dispose();
												bitmapImg = null;
											}
											else
												cachedBitmap = (Bitmap)bufferAnimatedImage.Clone();

											IsRefreshBitmap = false;

											if(bufferAnimatedImage != null && bufferAnimatedImage != animatedImage)
											{
												bufferAnimatedImage.Dispose();
												bufferAnimatedImage = null;
											}
										}
										grMainImage.DrawImage(cachedBitmap, scrollX, scrollY, zoomWigth, zoomHeigth);
									}
								}

								try
								{
									gr.FillRectangle(SystemBrushes.Control, rectAnimatedImage.X + 1, rectAnimatedImage.Y + 1, widthImage - 3, heightImage - 3);
									if(bitmapMainImage != null && bitmapMainImage.Width > 0 && bitmapMainImage.Height > 0)
										gr.DrawImage(bitmapMainImage, rectAnimatedImage.X, rectAnimatedImage.Y);
									gr.DrawRectangle(Pens.Black, rectAnimatedImage.X, rectAnimatedImage.Y, widthImage - 1, heightImage - 1);
									if(TypeWorkAnimatedImage == TypeWorkImage.SelectionMode)
									{
										using(Pen dotedPen = new Pen(new SolidBrush(Color.Black), 1))
										{
											dotedPen.DashStyle = DashStyle.Dot;
											gr.DrawRectangle(dotedPen, rectAnimatedImage.X + scrollX + (float)(SelectionModeRectangle.X * zoom * ppi / animatedImage.HorizontalResolution), rectAnimatedImage.Y + scrollY + (float)(SelectionModeRectangle.Y * zoom * ppi / animatedImage.VerticalResolution), (float)(SelectionModeRectangle.Width * zoom * ppi / animatedImage.HorizontalResolution), (float)(SelectionModeRectangle.Height * zoom * ppi / animatedImage.HorizontalResolution));
											dotedPen.Dispose();
										}
									}
								}
								catch(Exception ex)
								{
									Tiff.LibTiffHelper.WriteToLog(ex);
								}
							}
						}
						catch(Exception ex) { Tiff.LibTiffHelper.WriteToLog(ex); };
						#endregion
					}

					try
					{
						if(bitmap != null)
						{
							e.Graphics.InterpolationMode = CurrentInterpolationMode;
							e.Graphics.DrawImage(bitmap, 0, 0);
						}
					}
					catch(Exception ex)
					{
						Tiff.LibTiffHelper.WriteToLog(ex);
					}

					if(fullCahedBitmap != null)
					{
						fullCahedBitmap.Dispose();
						fullCahedBitmap = null;
					}

					if(UserAction == UsersActionsTypes.Splitter)
						fullCahedBitmap = bitmap.Clone() as Bitmap;
				}
			}
			catch(Exception ex)
			{
				Tiff.LibTiffHelper.WriteToLog(ex);
			}
#if AdvancedLogging
			Log.Logger.LeaveMethod(this, "OnPaint(PaintEventArgs e)");
#endif
		}

		public virtual bool HasAnnotation()
		{
			return this.tiffAnnotation != null;
		}

		public virtual bool IsAnnuled { get; set; }

		public virtual bool CanSave { get; set; }

		/// <summary>
		/// Рисование выделения для заметки
		/// </summary>
		private void DrawSelectedRectangle(Graphics g, Rectangle rect)
		{
			int indent = WidthSelectedtRect >> 1;
			selectedRectangles = new Rectangle[8] { new Rectangle(rect.X - indent, rect.Y - indent, WidthSelectedtRect, WidthSelectedtRect),
													  new Rectangle(rect.X + (rect.Width >> 1) - indent, rect.Y - indent, WidthSelectedtRect, WidthSelectedtRect),
													  new Rectangle(rect.X + rect.Width - indent, rect.Y - indent, WidthSelectedtRect, WidthSelectedtRect),
													  new Rectangle(rect.X + rect.Width - indent, rect.Y + (rect.Height >> 1) - indent, WidthSelectedtRect, WidthSelectedtRect),
													  new Rectangle(rect.X + rect.Width - indent, rect.Y + rect.Height - indent, WidthSelectedtRect, WidthSelectedtRect),
													  new Rectangle(rect.X + (rect.Width >> 1) - indent, rect.Y + rect.Height - indent, WidthSelectedtRect, WidthSelectedtRect),
													  new Rectangle(rect.X - indent, rect.Y + rect.Height - indent, WidthSelectedtRect, WidthSelectedtRect),
													  new Rectangle(rect.X - indent, rect.Y + (rect.Height >> 1) - indent, WidthSelectedtRect, WidthSelectedtRect)};
			g.FillRectangle(Brushes.Black, selectedRectangles[0]);
			g.FillRectangle(Brushes.Black, selectedRectangles[1]);
			g.FillRectangle(Brushes.Black, selectedRectangles[2]);
			g.FillRectangle(Brushes.Black, selectedRectangles[3]);
			g.FillRectangle(Brushes.Black, selectedRectangles[4]);
			g.FillRectangle(Brushes.Black, selectedRectangles[5]);
			g.FillRectangle(Brushes.Black, selectedRectangles[6]);
			g.FillRectangle(Brushes.Black, selectedRectangles[7]);
		}

		/// <summary>
		/// Изменение масштаба
		/// </summary>
		private int ImageZoom(int size, float resolution)
		{
			double width = size;
			double zoomSize = width * zoom * ppi / resolution;
			if(zoomSize > (double)int.MaxValue - 1)
				return int.MaxValue;
			if(zoomSize < 1)
				return 1;
			return (int)(zoomSize + 0.5);
		}

		/// <summary>
		/// Обработчик для обоих скролов картинки, основное назначение прекратить редактирование текстовых заметок
		/// </summary>
		private void scrollImageVertical_MouseEnter(object sender, EventArgs e)
		{
			if(Cursor != Cursors.Hand)
				Cursor = Cursors.Hand;
		}

		/// <summary>
		/// Удаление выделенных элементов по клавише Delete.
		/// </summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if(!e.Handled && e.Shift && e.KeyCode == Keys.Delete && tiffAnnotation != null)
			{
				e.Handled = true;
				tiffAnnotation.DeleteSelectedFigures();
			}
			else if(!e.Handled && e.KeyData == Keys.Escape && (UserAction == UsersActionsTypes.DrawFRect || UserAction == UsersActionsTypes.DrawHRect || UserAction == UsersActionsTypes.DrawImage || UserAction == UsersActionsTypes.DrawMarker || UserAction == UsersActionsTypes.DrawNote || UserAction == UsersActionsTypes.DrawRectText)) // окончание рисования
			{
				UserAction = UsersActionsTypes.None;
				SelectTool(1);
			}
			base.OnKeyDown(e);
		}
		#endregion

        #region Загрузка изображения

        /// <summary>
        /// Загрузить страницу изображения(из файла)
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="index"></param>
        /// <returns></returns>
		private PageInfo GetImageFromTiff(IntPtr handle, int index, bool check = true)
		{
			int page = index;
			if(check)
				page = GetCurrentIndex(index);
			var pageInfo = libTiff.GetImageFromTiff(handle, page);

			if(check && pageInfo != null && pageInfo.Image != null)
			{
				UpdatePageRotation(index);
				var virtualRotationAngle = GetPageRotation(index);

				if(virtualRotationAngle != 0)
				{
					var rotateFlipType = RotateFlipType.RotateNoneFlipNone;

					switch(virtualRotationAngle)
					{
						case 90:
							rotateFlipType = RotateFlipType.Rotate90FlipNone;
							break;

						case 180:
							rotateFlipType = RotateFlipType.Rotate180FlipNone;
							break;

						case 270:
							rotateFlipType = RotateFlipType.Rotate270FlipNone;
							break;
					}

					if(rotateFlipType != RotateFlipType.RotateNoneFlipNone)
					{
						try
						{
							pageInfo.Image.RotateFlip(rotateFlipType);
						}
						catch(Exception ex)
						{
							Log.Logger.WriteEx(new Exception("Не удалось повернуть изображение", ex));
						}
					}
				}
			}

			return pageInfo;
		}

	    #endregion

        /// <summary>
        /// Получить угол поворота страницы текущего изображения
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
		protected virtual int GetPageRotation(int page)
		{
			if(modifiedPages.ContainsKey(page) && modifiedPages[page].Item3)
				return modifiedPages[page].Item2;
			return 0;
		}

        /// <summary>
        /// Обновить(получить из бд) угол поворота страницы текущего изображения
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
		protected virtual void UpdatePageRotation(int page)
        {
        }

		/// <summary>
		/// Сбросить уголы поворота страниц текущего изображения
		/// </summary>
		protected virtual void CleanRotation()
		{
			//difiedPages.
		}
	}
}