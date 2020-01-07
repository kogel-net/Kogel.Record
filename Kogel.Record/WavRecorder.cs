using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DirectX;
using Microsoft.DirectX.DirectSound;
using System.Windows.Forms;

namespace Kogel.Record
{
	/// <summary>
	/// 声音录制
	/// </summary>
	public class WavRecorder
	{
		#region Fields
		private Capture mCapDev = null;              // 音频捕捉设备  
		private CaptureBuffer mRecBuffer = null;     // 缓冲区对象  
		private WaveFormat mWavFormat;               // 录音的格式  

		private int mNextCaptureOffset = 0;         // 该次录音缓冲区的起始点  
		private int mSampleCount = 0;               // 录制的样本数目  

		private Notify mNotify = null;               // 消息通知对象  
		private const int cNotifyNum = 16;           // 通知的个数  
		private int mNotifySize = 0;                // 每次通知大小  
		private int mBufferSize = 0;                // 缓冲队列大小  
		private Thread mNotifyThread = null;                 // 处理缓冲区消息的线程  
		private AutoResetEvent mNotificationEvent = null;    // 通知事件  

		private string mFileName = string.Empty;     // 文件保存路径  
		private FileStream mWaveFile = null;         // 文件流  
		private BinaryWriter mWriter = null;         // 写文件  
		public string WavFilePath { get; set; }
		#endregion

		/// <summary>
		/// 声音录制
		/// </summary>
		/// <param name="wavFilePath"></param>
		public WavRecorder(string  wavFilePath)
		{
			this.WavFilePath = wavFilePath;
		}


		#region 对外操作函数
		/// 
		/// 构造函数,设定录音设备,设定录音格式.
		/// 
		private void SoundRecorder()
		{
			// 初始化音频捕捉设备
			//InitCaptureDevice();
			// 设定录音格式

		}

		/// <summary>
		/// 创建录音格式,此处使用16bit,16KHz,Mono的录音格式
		/// </summary>
		/// <returns></returns>
		private WaveFormat CreateWaveFormat()
		{
			WaveFormat format = new WaveFormat();
			format.FormatTag = WaveFormatTag.Pcm;   // PCM
			format.SamplesPerSecond = 16000;        // 采样率：16KHz
			format.BitsPerSample = 16;              // 采样位数：16Bit
			format.Channels = 1;                    // 声道：Mono
			format.BlockAlign = (short)(format.Channels * (format.BitsPerSample / 8));  // 单位采样点的字节数 
			format.AverageBytesPerSecond = format.BlockAlign * format.SamplesPerSecond;
			return format;
			// 按照以上采样规格，可知采样1秒钟的字节数为 16000*2=32000B 约为31K
		}

		/// 
		/// 设定录音结束后保存的文件,包括路径
		/// 
		/// 保存wav文件的路径名
		private void SetFileName(string filename)
		{
			mFileName = filename;
		}

		/// 
		/// 开始录音
		/// 
		private void RecStart()
		{
			// 创建录音文件
			CreateSoundFile();
			// 创建一个录音缓冲区，并开始录音
			CreateCaptureBuffer();
			// 建立通知消息,当缓冲区满的时候处理方法
			InitNotifications();
			try
			{
				mRecBuffer.Start(true);
			}
			catch (NullReferenceException) { }

		}

		/// 
		/// 停止录音
		/// 
		private void RecStop()
		{
			mRecBuffer.Stop();      // 调用缓冲区的停止方法，停止采集声音
			if (null != mNotificationEvent)
				mNotificationEvent.Set();       //关闭通知
			mNotifyThread.Abort();  //结束线程
			RecordCapturedData();   // 将缓冲区最后一部分数据写入到文件中

			// 写WAV文件尾
			mWriter.Seek(4, SeekOrigin.Begin);
			mWriter.Write((int)(mSampleCount + 36));   // 写文件长度
			mWriter.Seek(40, SeekOrigin.Begin);
			mWriter.Write(mSampleCount);                // 写数据长度

			mWriter.Close();
			mWaveFile.Close();
			mWriter = null;
			mWaveFile = null;
		}
		#endregion

		#region 对内操作函数
		/// 
		/// 初始化录音设备,此处使用主录音设备.
		/// 
		/// 调用成功返回true,否则返回false
		/*private */
		private bool InitCaptureDevice()
		{
			// 获取默认音频捕捉设备
			CaptureDevicesCollection devices = new CaptureDevicesCollection();  // 枚举音频捕捉设备
			Guid deviceGuid = Guid.Empty;

			if (devices.Count > 0)
				deviceGuid = devices[0].DriverGuid;
			else
			{
				MessageBox.Show("系统中没有音频捕捉设备");
				return false;
			}

			// 用指定的捕捉设备创建Capture对象
			try
			{
				mCapDev = new Capture(deviceGuid);
			}
			catch (DirectXException e)
			{
				MessageBox.Show(e.ToString());
				return false;
			}
			return true;
		}

		/// 
		/// 创建录音使用的缓冲区
		/// 
		private void CreateCaptureBuffer()
		{
			// 缓冲区的描述对象
			CaptureBufferDescription bufferdescription = new CaptureBufferDescription();
			if (null != mNotify)
			{
				mNotify.Dispose();
				mNotify = null;
			}
			if (null != mRecBuffer)
			{
				mRecBuffer.Dispose();
				mRecBuffer = null;
			}
			// 设定通知的大小,默认为1s钟
			mNotifySize = (1024 > mWavFormat.AverageBytesPerSecond / 8) ? 1024 : (mWavFormat.AverageBytesPerSecond / 8);

			if (mWavFormat.BlockAlign != 0)
				mNotifySize -= mNotifySize % mWavFormat.BlockAlign;
			// 设定缓冲区大小
			mBufferSize = mNotifySize * cNotifyNum;
			// 创建缓冲区描述
			bufferdescription.BufferBytes = mBufferSize;
			bufferdescription.Format = mWavFormat;           // 录音格式
															 // 创建缓冲区
			try
			{
				mRecBuffer = new CaptureBuffer(bufferdescription, mCapDev);
			}
			catch (ArgumentNullException)
			{ }

			mNextCaptureOffset = 0;
		}

		/// 
		/// 初始化通知事件,将原缓冲区分成16个缓冲队列,在每个缓冲队列的结束点设定通知点.
		/// 
		/// 是否成功
		private bool InitNotifications()
		{
			if (null == mRecBuffer)
			{
				MessageBox.Show("未创建录音缓冲区");
				return false;
			}
			// 创建一个通知事件,当缓冲队列满了就激发该事件.
			mNotificationEvent = new AutoResetEvent(false);
			// 创建一个线程管理缓冲区事件
			if (null == mNotifyThread)
			{
				mNotifyThread = new Thread(new ThreadStart(WaitThread));
				mNotifyThread.Start();
			}
			// 设定通知的位置
			BufferPositionNotify[] PositionNotify = new BufferPositionNotify[cNotifyNum + 1];
			for (int i = 0; i < cNotifyNum; i++)
			{
				PositionNotify[i].Offset = (mNotifySize * i) + mNotifySize - 1;
				PositionNotify[i].EventNotifyHandle = mNotificationEvent.SafeWaitHandle.DangerousGetHandle();
			}
			mNotify = new Notify(mRecBuffer);
			mNotify.SetNotificationPositions(PositionNotify, cNotifyNum);
			return true;
		}

		/// 
		/// 接收缓冲区满消息的处理线程
		/// 
		private void WaitThread()
		{
			while (true)
			{
				// 等待缓冲区的通知消息
				mNotificationEvent.WaitOne(Timeout.Infinite, true);
				// 录制数据
				RecordCapturedData();
			}
		}

		/// 
		/// 将录制的数据写入wav文件
		/// 
		private void RecordCapturedData()
		{
			byte[] CaptureData = null;
			int ReadPos = 0, CapturePos = 0, LockSize = 0;
			mRecBuffer.GetCurrentPosition(out CapturePos, out ReadPos);
			LockSize = ReadPos - mNextCaptureOffset;
			if (LockSize < 0)       // 因为是循环的使用缓冲区，所以有一种情况下为负：当文以载读指针回到第一个通知点，而Ibuffeoffset还在最后一个通知处
				LockSize += mBufferSize;
			LockSize -= (LockSize % mNotifySize);   // 对齐缓冲区边界,实际上由于开始设定完整,这个操作是多余的.
			if (0 == LockSize)
				return;

			// 读取缓冲区内的数据
			CaptureData = (byte[])mRecBuffer.Read(mNextCaptureOffset, typeof(byte), LockFlag.None, LockSize);
			// 写入Wav文件
			mWriter.Write(CaptureData, 0, CaptureData.Length);
			// 更新已经录制的数据长度.
			mSampleCount += CaptureData.Length;
			// 移动录制数据的起始点,通知消息只负责指示产生消息的位置,并不记录上次录制的位置
			mNextCaptureOffset += CaptureData.Length;
			mNextCaptureOffset %= mBufferSize; // Circular buffer
		}

		/// 
		/// 创建保存的波形文件,并写入必要的文件头.
		/// 
		private void CreateSoundFile()
		{
			// Open up the wave file for writing.
			try
			{
				mWaveFile = new FileStream(mFileName, FileMode.Create);
			}
			catch (IOException) { }
			try
			{
				mWriter = new BinaryWriter(mWaveFile);
			}
			catch (ArgumentNullException e) { }

			/************************************************************************** 
               Here is where the file will be created. A 
               wave file is a RIFF file, which has chunks 
               of data that describe what the file contains. 
               A wave RIFF file is put together like this: 
               The 12 byte RIFF chunk is constructed like this: 
               Bytes 0 - 3 :  'R' 'I' 'F' 'F' 
               Bytes 4 - 7 :  Length of file, minus the first 8 bytes of the RIFF description. 
                                 (4 bytes for "WAVE" + 24 bytes for format chunk length + 
                                 8 bytes for data chunk description + actual sample data size.) 
                Bytes 8 - 11: 'W' 'A' 'V' 'E' 
                The 24 byte FORMAT chunk is constructed like this: 
                Bytes 0 - 3 : 'f' 'm' 't' ' ' 
                Bytes 4 - 7 : The format chunk length. This is always 16. 
                Bytes 8 - 9 : File padding. Always 1. 
                Bytes 10- 11: Number of channels. Either 1 for mono,  or 2 for stereo. 
                Bytes 12- 15: Sample rate. 
                Bytes 16- 19: Number of bytes per second. 
                Bytes 20- 21: Bytes per sample. 1 for 8 bit mono, 2 for 8 bit stereo or 
                                16 bit mono, 4 for 16 bit stereo. 
                Bytes 22- 23: Number of bits per sample. 
                The DATA chunk is constructed like this: 
                Bytes 0 - 3 : 'd' 'a' 't' 'a' 
                Bytes 4 - 7 : Length of data, in bytes. 
                Bytes 8 -: Actual sample data. 
              ***************************************************************************/
			// Set up file with RIFF chunk info.
			char[] ChunkRiff = { 'R', 'I', 'F', 'F' };
			char[] ChunkType = { 'W', 'A', 'V', 'E' };
			char[] ChunkFmt = { 'f', 'm', 't', ' ' };
			char[] ChunkData = { 'd', 'a', 't', 'a' };

			short shPad = 1;                // File padding
			int nFormatChunkLength = 0x10;  // Format chunk length.
			int nLength = 0;                // File length, minus first 8 bytes of RIFF description. This will be filled in later.
			short shBytesPerSample = 0;     // Bytes per sample.

			// 一个样本点的字节数目
			if (8 == mWavFormat.BitsPerSample && 1 == mWavFormat.Channels)
				shBytesPerSample = 1;
			else if ((8 == mWavFormat.BitsPerSample && 2 == mWavFormat.Channels) || (16 == mWavFormat.BitsPerSample && 1 == mWavFormat.Channels))
				shBytesPerSample = 2;
			else if (16 == mWavFormat.BitsPerSample && 2 == mWavFormat.Channels)
				shBytesPerSample = 4;

			// RIFF 块
			try
			{
				mWriter.Write(ChunkRiff);
				mWriter.Write(nLength);
				mWriter.Write(ChunkType);



				// WAVE块
				mWriter.Write(ChunkFmt);
				mWriter.Write(nFormatChunkLength);
				mWriter.Write(shPad);
				mWriter.Write(mWavFormat.Channels);
				mWriter.Write(mWavFormat.SamplesPerSecond);
				mWriter.Write(mWavFormat.AverageBytesPerSecond);
				mWriter.Write(shBytesPerSample);
				mWriter.Write(mWavFormat.BitsPerSample);

				// 数据块
				mWriter.Write(ChunkData);
				mWriter.Write((int)0);   // The sample length will be written in later.

			}
			catch (NullReferenceException) { }
		}
        #endregion

		/// <summary>
		/// 开始
		/// </summary>
		public void Start()
		{
			InitCaptureDevice();
			mWavFormat = CreateWaveFormat();
			SetFileName(WavFilePath);
			RecStart();
		}

		/// <summary>
		/// 结束
		/// </summary>
		public void End()
		{
			RecStop();
		}
	}

}
