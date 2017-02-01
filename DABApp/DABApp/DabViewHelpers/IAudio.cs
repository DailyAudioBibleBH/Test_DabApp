using System;
namespace DABApp
{
	public interface IAudio
	{
		void SetAudioFile(string FileName);
		void Pause();
		void Play();
		bool IsInitialized();
		bool IsPlaying();
		double CurrentTime();
		double RemainingTime();
	}
}
