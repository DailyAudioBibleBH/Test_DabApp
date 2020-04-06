using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Interop;

//namespace DABApp.Droid.DependencyServices
//{
////    class DroidAudioFocus : AudioManager.IOnAudioFocusChangeListener
//    {
//        public IntPtr Handle => throw new NotImplementedException();

//        public int JniIdentityHashCode => throw new NotImplementedException();

//        public JniObjectReference PeerReference => throw new NotImplementedException();

//        public JniPeerMembers JniPeerMembers => throw new NotImplementedException();

//        public JniManagedPeerStates JniManagedPeerState => throw new NotImplementedException();

//        public DroidAudioFocus()
//        {
//            //RequestAudioFocus();
//        }
//        //public bool RequestAudioFocus()
//        //{
//        //    //AudioManager audioManager = (AudioManager)GetSystemService(AudioService);
//        //    //AudioFocusRequest audioFocusRequest;
//        //    //if (Build.VERSION.SdkInt > BuildVersionCodes.O)
//        //    //{
//        //    //    audioFocusRequest = audioManager.RequestAudioFocus(new AudioFocusRequestClass.Builder(AudioFocus.Gain)
//        //    //    .SetAudioAttributes(new AudioAttributes.Builder().SetLegacyStreamType(Stream.Music).Build()).SetOnAudioFocusChangeListener(this)
//        //    //    .Build());
//        //    //}
//        //    //else
//        //    //{
//        //    //    audioFocusRequest = audioManager.RequestAudioFocus(this, Stream.Music, AudioFocus.Gain);
//        //    //}

//        //    //if (audioFocusRequest == AudioFocusRequest.Granted)
//        //    //{
//        //    //    return true;
//        //    //}
//        //    //return false;
//        //    return true;
//        //}

//        public void OnAudioFocusChange([GeneratedEnum] AudioFocus focusChange)
//        {
//            switch (focusChange)
//            {
//                case AudioFocus.Gain:

//                    //Gain when other Music Player app releases the audio service   
//                    break;
//                case AudioFocus.Loss:
//                    //We have lost focus stop!   

//                    break;
//                case AudioFocus.LossTransient:
//                    //We have lost focus for a short time, but likely to resume so pause   

//                    break;
//                case AudioFocus.LossTransientCanDuck:
//                    //We have lost focus but should till play at a muted 10% volume   

//                    break;
//            }
//        }

//        public void SetJniIdentityHashCode(int value)
//        {
//            throw new NotImplementedException();
//        }

//        public void SetPeerReference(JniObjectReference reference)
//        {
//            throw new NotImplementedException();
//        }

//        public void SetJniManagedPeerState(JniManagedPeerStates value)
//        {
//            throw new NotImplementedException();
//        }

//        public void UnregisterFromRuntime()
//        {
//            throw new NotImplementedException();
//        }

//        public void DisposeUnlessReferenced()
//        {
//            throw new NotImplementedException();
//        }

//        public void Disposed()
//        {
//            throw new NotImplementedException();
//        }

//        public void Finalized()
//        {
//            throw new NotImplementedException();
//        }

//        public void Dispose()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}