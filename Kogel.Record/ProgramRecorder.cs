using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using Kogel.Record.Extension;
using Kogel.Record.Interfaces;

namespace Kogel.Record
{
	/// <summary>
	/// 程序界面录制
	/// </summary>
	public class ProgramRecorder : ScreenRecorder, IRecorder
	{
		/// <summary>
		/// 程序名称
		/// </summary>
		private string programName;

		/// <summary>
		/// 程序句柄
		/// </summary>
		private IntPtr hwnd;

		/// <summary>
		/// 每帧回调
		/// </summary>
		private NewFrameEventHandler frameEventHandler { get; set; }

		/// <summary>
		/// 程序界面录制
		/// </summary>
		/// <param name="hwnd">程序句柄</param>
		/// <param name="programName">程序名称</param>
		/// <param name="aviFilePath">视频路径</param>
		/// <param name="defaultFrameRate">默认帧数</param>
		/// <param name="isLoopingWav">是否录制声音(默认不录制)</param>
		public ProgramRecorder(IntPtr hwnd, string programName, string aviFilePath, int defaultFrameRate = 10, bool isLoopingWav = false)
			: base(aviFilePath, defaultFrameRate, isLoopingWav)
		{
			this.programName = programName;
			this.hwnd = hwnd;
		}

		/// <summary>
		/// 开始
		/// </summary>
		/// <param name="frameEventHandler"></param>
		public override void Start(NewFrameEventHandler frameEventHandler = null)
		{
			if (hwnd == IntPtr.Zero)
			{
				MessageBox.Show($"没有找到程序【{programName}】!");
				return;
			}
			//首先获取一张，并设置成此大小
			Bitmap programBmp = WindowApi.GetWindowCapture(hwnd);
			base.ScreenWidth = programBmp.Width % 2 == 0 ? programBmp.Width : programBmp.Width - 1;
			base.ScreenHight = programBmp.Height % 2 == 0 ? programBmp.Height : programBmp.Height - 1;
			//开始录制
			base.Start(null);
			this.frameEventHandler = frameEventHandler;
		}

		/// <summary>
		/// 捕捉每帧
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		protected override void VideoStreamer_NewFrame(object sender, NewFrameEventArgs eventArgs)
		{
			Bitmap programBmp = WindowApi.GetWindowCapture(hwnd);
			Bitmap newBitmap = new Bitmap(this.ScreenWidth, this.ScreenHight);
			//修改图片大小
			using(Graphics graphics = Graphics.FromImage(newBitmap))
			{
				graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				graphics.DrawImage(programBmp, 0, 0, base.ScreenWidth, base.ScreenHight);
			}
			var newFrameEvent = new NewFrameEventArgs(newBitmap);
			base.VideoStreamer_NewFrame(sender, newFrameEvent);
			try
			{
				frameEventHandler.Invoke(sender, newFrameEvent);
			}
			catch { }
		}

		/// <summary>
		/// 结束
		/// </summary>
		public override void End()
		{
			base.End();
		}
	}
}
