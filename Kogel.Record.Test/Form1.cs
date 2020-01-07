using AForge.Video;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kogel.Record.Test
{
	public partial class Form1 : Form
	{
		private ScreenRecorder recorder { get; set; }
		private string recorderPath { get; set; }
		public Form1()
		{
			InitializeComponent();
			recorderPath = AppDomain.CurrentDomain.BaseDirectory + DateTime.Now.ToString("MMddHHmmss") + ".avi";
			recorder = new ScreenRecorder(recorderPath);
		}

		/// <summary>
		/// 开始录制
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnStart_Click(object sender, EventArgs e)
		{
			//开始并设置显示器
			recorder.Start(VideoStreamer_NewFrame);
		}

		/// <summary>
		/// 每帧录制帧数回调
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void VideoStreamer_NewFrame(object sender, NewFrameEventArgs eventArgs)
		{
			this.picScreen.Image = (Bitmap)eventArgs.Frame.Clone();
		}

		/// <summary>
		/// 结束
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnEnd_Click(object sender, EventArgs e)
		{
			recorder.End();
		}
	}
}
