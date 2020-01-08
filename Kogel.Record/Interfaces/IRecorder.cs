using AForge.Video;

namespace Kogel.Record.Interfaces
{
	public interface IRecorder
	{
		void Start(NewFrameEventHandler frameEventHandler = null);

		void Pause();

		void End();
	}
}
