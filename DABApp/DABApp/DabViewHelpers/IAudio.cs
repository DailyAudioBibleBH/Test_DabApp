using System;
namespace DABApp
{
	public interface IAudio
	{
		void SetAudioFile(string FileName);
		void Pause();
		void Play();
		void SeekTo(int seconds); //Go to a specific point in the audio file
		void Skip(int seconds); //Go forward/back in the audio file.
		bool IsInitialized { get;}
		bool IsPlaying { get;}
		double CurrentTime { get;}
		double RemainingTime { get;}
		double TotalTime { get;}

	}
}
