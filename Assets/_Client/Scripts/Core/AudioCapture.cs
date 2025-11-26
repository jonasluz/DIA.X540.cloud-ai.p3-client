using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        #region -- Fields & Properties ----------------------------------------
        [Header("Audio Capture Settings")]
        [SerializeField]
        private SampleRate _sampleRateInHz = SampleRate.Hz44100;
        [SerializeField]
        private int _maxRecordingSeconds = 300; // safety cap
        [SerializeField] 
        private string _fileName = "RecordedAudio.wav";
        [SerializeField, Range(0f, 1f)] 
        private float _playbackVolume = 1f; // volume while monitoring
        [SerializeField] 
        private bool _enableMonitoring = true; // if true, playback your mic
    [SerializeField, Tooltip("Latency in milliseconds to offset monitoring playback from the microphone write head")]
    private int _monitorLatencyMs = 80;
        [SerializeField] 
        private string[] _microphones;
        [SerializeField] 
        private int _selectedMicrophoneIndex = 0;

        public string SelectedDevice =>
            _microphones.Length > 0 &&
            _selectedMicrophoneIndex < _microphones.Length &&
            _selectedMicrophoneIndex >= 0
            ? _microphones[_selectedMicrophoneIndex]
            : null;
        public bool IsRecording => _isRecording;
        public string LastSavedFilePath { get; private set; }

        public event Action<string> OnRecordingSaved; // path
        public event Action OnRecordingStarted;
        public event Action OnRecordingStopped;

        private AudioSource _audioSource;
        private bool _isRecording;
        private List<short> _capturedSamples = 
            new List<short>(1024 * 32); // filled only on Stop
        private int _channels = 1; // microphone channel count (Unity usually mono)
        
        private AudioClip _recordingClip;
        public AudioClip GetRecordedClip() => _recordingClip;
        
        private string _currentDevice;
        #endregion ------------------------------------------------------------

        #region -- MonoBehaviour Methods --------------------------------------
        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _microphones = Microphone.devices;
        }

        private void OnDestroy()
        {
            StopRecording();
        }
        #endregion ------------------------------------------------------------

        [ContextMenu("Start recording audio")]
        public void StartRecording()
        {
            if (_isRecording)
            {
                Debug.LogWarning("Already recording.");
                return;
            }
            if (_microphones == null || _microphones.Length == 0)
            {
                Debug.LogError("No microphone devices found.");
                return;
            }

            _currentDevice = SelectedDevice;
            int frequency = GetSupportedFrequency(_currentDevice, (int)_sampleRateInHz);

            // start as looped for safe monitoring without underruns
            _recordingClip = Microphone.Start(
                _currentDevice, true, _maxRecordingSeconds, frequency);
            if (_recordingClip == null)
            {
                Debug.LogError("Failed to start microphone recording.");
                return;
            }
            _channels = Mathf.Max(1, _recordingClip.channels);

            _capturedSamples.Clear();
            _isRecording = true;

            StartCoroutine(WaitAndStartMonitoring());
            OnRecordingStarted?.Invoke();
        }

        private IEnumerator WaitAndStartMonitoring()
        {
            // Wait until microphone has started providing data
            while (Microphone.GetPosition(_currentDevice) <= 0)
            {
                yield return null;
            }

            // Recording might have been stopped early
            if (!_isRecording) yield break; 

            if (_enableMonitoring)
            {
                _audioSource.loop = true;
                _audioSource.clip = _recordingClip;
                _audioSource.volume = _playbackVolume;
                // Start playback slightly behind the microphone write position to avoid underruns/noise
                int pos = Microphone.GetPosition(_currentDevice);
                int latency = Mathf.Clamp((int)(_recordingClip.frequency * (_monitorLatencyMs / 1000f)), 0, _recordingClip.samples - 1);
                int start = pos - latency;
                if (start < 0) start += _recordingClip.samples; // wrap around for looped clip
                _audioSource.timeSamples = start % _recordingClip.samples;
                _audioSource.Play();
            }
        }

        [ContextMenu("Stop recording audio")]
        public void StopRecording()
        {
            if (!_isRecording)
            {
                Debug.LogWarning("Not currently recording.");
                return;
            }

            if (_audioSource.isPlaying) _audioSource.Stop();
            var position = Microphone.GetPosition(_currentDevice);
            Microphone.End(_currentDevice);
            _isRecording = false;

            // Capture only the recorded portion (GetPosition tells how many samples per channel were written)
            if (position <= 0)
            {
                Debug.LogWarning("Microphone position is zero; no data captured.");
            }
            else
            {
                try
                {
                    int samplesToCopy = position * _recordingClip.channels;
                    float[] floatData = new float[samplesToCopy];
                    _recordingClip.GetData(floatData, 0);
                    for (int i = 0; i < floatData.Length; i++)
                    {
                        float clamped = Mathf.Clamp(floatData[i], -1f, 1f);
                        short sample = (short)(clamped * short.MaxValue);
                        _capturedSamples.Add(sample);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to read microphone data: {ex.Message}");
                }
            }

            OnRecordingStopped?.Invoke();
            SaveWavFile();
        }

        private void SaveWavFile()
        {
            if (_capturedSamples.Count == 0)
            {
                Debug.LogWarning("No audio samples captured; skipping file save.");
                return;
            }

        string filePath = Path.Combine(Application.persistentDataPath, _fileName);
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                using (var writer = new BinaryWriter(fs))
                {
            int sampleRate = _recordingClip != null ? _recordingClip.frequency : (int)_sampleRateInHz;
            int bitsPerSample = 16;
            int channels = _recordingClip != null ? _recordingClip.channels : _channels;
                    int byteRate = sampleRate * channels * bitsPerSample / 8;
                    byte[] dataBytes = new byte[_capturedSamples.Count * 2];
                    Buffer.BlockCopy(_capturedSamples.ToArray(), 0, dataBytes, 0, dataBytes.Length);
                    int subchunk2Size = dataBytes.Length;
                    int chunkSize = 36 + subchunk2Size;

                    // RIFF header
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write(chunkSize);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                    // fmt subchunk
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                    writer.Write(16); // PCM header length
                    writer.Write((short)1); // Audio format = PCM
                    writer.Write((short)channels);
                    writer.Write(sampleRate);
                    writer.Write(byteRate);
                    writer.Write((short)(channels * bitsPerSample / 8)); // block align
                    writer.Write((short)bitsPerSample);

                    // data subchunk
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                    writer.Write(subchunk2Size);
                    writer.Write(dataBytes);
                }
                LastSavedFilePath = filePath;
                OnRecordingSaved?.Invoke(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save WAV file: {ex.Message}");
            }
            finally
            {
                _capturedSamples.Clear();
            }
        }

        [ContextMenu("Refresh microphone list")]
        public void RefreshMicrophones()
        {
            _microphones = Microphone.devices;
            if (_microphones == null || _microphones.Length == 0)
            {
                Debug.LogWarning("No microphones detected after refresh.");
                _selectedMicrophoneIndex = -1;
            }
            else if (
                _selectedMicrophoneIndex < 0 || 
                _selectedMicrophoneIndex >= _microphones.Length)
            {
                _selectedMicrophoneIndex = 0;
            }
        }

        private int GetSupportedFrequency(string device, int requested)
        {
            int min, max;
            Microphone.GetDeviceCaps(device, out min, out max);
            // When both are 0, any frequency is supported
            if (min == 0 && max == 0) return requested;
            if (requested < min) return min;
            if (requested > max) return max;
            return requested;
        }
    }
}
