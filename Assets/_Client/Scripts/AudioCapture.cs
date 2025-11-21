using UnityEngine;


namespace PPGIA.X540.Project3
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioCapture : MonoBehaviour
    {
        public enum SampleRate { 
            Hz16000 = 16000,
            Hz44100 = 44100, 
            Hz48000 = 48000, 
            Hz96000 = 96000 
        }
        
        [Header("Audio Capture Settings")]
        [SerializeField]
        private SampleRate _sampleRateInHz = SampleRate.Hz44100;

        private AudioSource _audioSource;
        private AudioClip _audioClip;

        void Start()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioClip = Microphone.Start(null, true, 1, (int)_sampleRateInHz);
            _audioSource.clip = _audioClip;
            _audioSource.loop = true;

            // Wait until the microphone starts recording
            while (!(Microphone.GetPosition(null) > 0)) { } 

            _audioSource.Play();
        }

        // void Update()
        // {
        //     var samples = new float[SAMPLE_RATE];
        //     _audioClip.GetData(samples, 0);

        //     for (int i = 0; i < samples.Length; i++)
        //     {
        //         samples[i] *= _gain;
        //         samples[i] = Mathf.Clamp(samples[i], -1.0f, 1.0f);
        //     }

        //     var processedClip = AudioClip.Create(
        //         "ProcessedClip", samples.Length, _audioClip.channels, 
        //         SAMPLE_RATE, false);
        //     processedClip.SetData(samples, 0);

        //     _audioSource.clip = processedClip;
        //     if (!_audioSource.isPlaying) _audioSource.Play();
        // }
    }
}
