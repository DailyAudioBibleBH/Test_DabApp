using System;
namespace DABApp
{
	public interface IAudio
	{
		void SetAudioFile(string FileName);
		void Pause();
		void Play();
		void SeekTo(int seconds);
		bool IsInitialized { get;}
		bool IsPlaying { get;}
		double CurrentTime { get;}
		double RemainingTime { get;}
		double TotalTime { get;}
	}
}
