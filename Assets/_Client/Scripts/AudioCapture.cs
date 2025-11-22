using UnityEngine;


namespace PPGIA.X540.Project3
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioCapture : MonoBehaviour
    {
        public enum SampleRate
        {
            Hz16000 = 16000,
            Hz44100 = 44100,
            Hz48000 = 48000,
            Hz96000 = 96000
        }

        [Header("Audio Capture Settings")]
        [SerializeField]
        private SampleRate _sampleRateInHz = SampleRate.Hz44100;

        [SerializeField]
        private bool _playingBack = false;

        private AudioSource _audioSource;
        private AudioClip _audioClip;

        void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        void OnDestroy()
        {
            Stop();
        }

        [ContextMenu("Record Audio")]
        public void Record()
        {
            _audioClip = Microphone.Start(null, true, 1, (int)_sampleRateInHz);
            _audioSource.clip = _audioClip;
            _audioSource.loop = true;

            // Wait until the microphone starts recording
            while (!(Microphone.GetPosition(null) > 0)) { }

            if (_playingBack)
            {
                _audioSource.Play();
            }
        }

        [ContextMenu("Stop Audio")]
        public void Stop()
        {
            Microphone.End(null);
        }
    }
}
