using System;
using System.Collections.Generic;
namespace MediaBrowser.Library.RemoteControl {


    public interface IPlaybackController {
        void GoToFullScreen();
        bool IsPaused { get; }
        bool IsPlaying { get; }
        bool IsStopped { get; }
        event EventHandler<PlaybackStateEventArgs> OnProgress;
        void PlayDVD(string path);
        void PlayMedia(string path);
        void QueueMedia(IEnumerable<string> paths);
        void QueueMedia(string path);
        void Seek(long position);
        void Pause();
        bool CanPlay(string filename);
        void ProcessCommand(RemoteCommand command);
        bool CanPlay(IEnumerable<string> files);
        bool RequiresExternalPage{ get; }
    }
}
