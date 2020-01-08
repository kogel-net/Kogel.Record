using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kogel.Record
{
	public class Global
	{
		/// <summary>
		/// 初始化dll路径（copy依赖项到输出目录）
		/// </summary>
		public static void InitDllPath()
		{
			//输出目录
			string outPath = AppDomain.CurrentDomain.BaseDirectory;
			//依赖程序集
			string[] dllFiles = Directory.GetFiles(outPath + "DLL");
			//遍历复制
			foreach (string file in dllFiles)
			{
				string fileName = file.Substring(file.LastIndexOf("\\") + 1);
				//存在就不复制
				if (File.Exists(outPath + fileName))
					continue;
				File.Copy(file, outPath + fileName);
			}
		}
	}
}
