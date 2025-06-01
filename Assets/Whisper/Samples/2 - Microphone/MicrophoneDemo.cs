using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Whisper.Utils;
using Button = UnityEngine.UI.Button;
using Toggle = UnityEngine.UI.Toggle;

namespace Whisper.Samples
{
    /// <summary>
    /// Record audio clip from microphone and make a transcription.
    /// </summary>
    public class MicrophoneDemo : MonoBehaviour
    {
        public WhisperManager whisper;
        public MicrophoneRecord microphoneRecord;

        private string _buffer;

        private void Awake()
        {
            whisper.OnNewSegment += OnNewSegment;
        }

        
        private void OnNewSegment(WhisperSegment segment)
        {
            _buffer += segment.Text;
            UnityEngine.Debug.Log(_buffer);
        }
    }
}