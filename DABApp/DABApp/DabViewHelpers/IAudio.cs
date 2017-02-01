using System;
namespace DABApp
{
	public interface IAudio
	{
		void PlayAudioFile(string FileName);
		void Pause();
		void Play();
		bool IsInitialized();
		bool IsPlaying();
	}
}
