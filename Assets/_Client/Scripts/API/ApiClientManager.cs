using System;
using System.IO;
using System.Collections;
using System.Linq;

using UnityEngine;


namespace PPGIA.X540.Project3.API
{
    public class ApiClientManager : MonoBehaviour
    {
        #region -- Inspector Fields -------------------------------------------
        [Header("API Base URL Settings")]
        [SerializeField]
        private string _apiBaseUrlDev = "http://127.0.0.1:8000";

        [SerializeField]
        private string _apiBaseUrlProd = "https://api.example.com";

        [SerializeField]
        private Environment _environment = Environment.Development;

        [Header("API Endpoints")]
        [SerializeField]
        private string _sessionInitEndpoint = "/session/init";

        [SerializeField]
        private string _sessionCloseEndpoint = "/session/close";

        [SerializeField]
        private string _chatEndpoint = "/chat";

        [SerializeField]
        private string _llmAgentEndpoint = "/agent/ask";

        [SerializeField]
        private string _ttsEndpoint = "/tts/synthesize";

        [SerializeField]
        private string _sttUploadEndpoint = "/transcript/get-upload-url";

        [SerializeField]
        private string _sttStartEndpoint = "/transcript/start";

        [SerializeField]
        private string _sttDownloadEndpoint = "/transcript/download";

        [Header("API Settings & Workload")]
        [SerializeField]
        private string _clientId = "unity-client";

        [SerializeField]
        private float _timeoutInSeconds = 10f;

        [SerializeField, Multiline, TextArea(3, 10)]
        private string _query;

        [Header("API State Information")]
        [SerializeField]
        private Session _session;
        public bool IsSessionActive => _session != null;
        #endregion ------------------------------------------------------------

        #region -- Other Properties & Methods ---------------------------------
        [SerializeField]
        private AudioSource _audioSource;

        // Property to get the appropriate API base URL
        private string ApiBaseUrl =>
            _environment == Environment.Development ?
            _apiBaseUrlDev : _apiBaseUrlProd;

        // Helper Method to build endpoint URLs
        string EndpointUrl(params string[] parts) =>
            ApiBaseUrl.TrimEnd('/') + '/' +
            string.Join("/", parts.Select(p => p.Trim('/')));
        #endregion ------------------------------------------------------------

        void Awake()
        {
            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                Debug.LogWarning("AudioSource component is missing.");
        }

        #region -- API Calls --------------------------------------------------
        [ContextMenu("API Tests/API Availability")]
        public void TestApiAvailability()
        {
            var url = EndpointUrl("");

            StartCoroutine(ApiClient.CallEndpointWithGetCoroutine(
                url, _timeoutInSeconds, (request) =>
            {
                var body = request.downloadHandler?.text ?? string.Empty;
                Debug.Log($"API call returned: {body}");
            }));
        }

        [ContextMenu("Session/Initiate Session")]
        public void InitiateSession(Action sessionStartedCallback = null)
        {
            StopAllCoroutines();

            var url = EndpointUrl(_sessionInitEndpoint, _clientId);

            StartCoroutine(ApiClient.CallEndpointWithPostCoroutine(
                url, _timeoutInSeconds, null, (request) =>
            {
                var body = request.downloadHandler?.text ?? string.Empty;
                var session = JsonUtility.FromJson<Session>(body);
                _session = session;

                sessionStartedCallback?.Invoke();
            }));
        }

        [ContextMenu("Session/Close Session")]
        public void CloseSession(Action sessionClosedCallback = null)
        {
            if (_session == null)
            {
                Debug.LogWarning("No active session to close.");
                sessionClosedCallback?.Invoke();
                return;
            }

            StopAllCoroutines();

            var url = EndpointUrl(_sessionCloseEndpoint, _session.SessionId);

            StartCoroutine(ApiClient.CallEndpointWithDeleteCoroutine(
                url, _timeoutInSeconds, (request) =>
            {
                Debug.Log("Session closed successfully.");
                _session = null;
                sessionClosedCallback?.Invoke();
            }));
        }

        public void UploadAudioClip(
            string localFilePath, Action<string> uploadCompletedCallback = null)
        {
            if (_session == null)
            {
                Debug.LogWarning("No active session. Please initiate a session first.");
                return;
            }

            StopAllCoroutines();

            var url = EndpointUrl(_sttUploadEndpoint, _session.SessionId);
            var payload = new 
            {
                filename = Path.GetFileName(localFilePath),
                content_type = "audio/wav"
            };

            StartCoroutine(ApiClient.CallEndpointWithPostCoroutine(
                url, _timeoutInSeconds, payload, (request) =>
            {
                var body = request.downloadHandler?.text ?? string.Empty;
                var uploadUrl = JsonUtility.FromJson<STTUploadResponse>(body)?.UploadUrl;
                var s3Key = JsonUtility.FromJson<STTUploadResponse>(body)?.S3Key;
                if (uploadUrl == null)
                {
                    Debug.LogWarning("Failed to get upload URL.");
                    return;
                }

                StartCoroutine(ApiClient.UploadAudioDataCoroutine(
                    uploadUrl, localFilePath, _timeoutInSeconds, (uploadRequest) =>
                {
                    Debug.Log($"Audio upload complete: {uploadRequest.responseCode}");
                    uploadCompletedCallback?.Invoke(s3Key);
                }));
            }));
        }


        [ContextMenu("STT/Upload Audio Clip")]
        public void StartTranscript(string s3Key,
            Action<string> transcriptStartedCallback = null)
        {
            // Ensure there is an active session
            if (_session == null)
            {
                Debug.LogWarning("No active session. Please initiate a session first.");
                return;
            }
            if (string.IsNullOrEmpty(s3Key))
            {
                Debug.LogWarning("No file path provided for upload.");
                return;
            }

            StopAllCoroutines();

            // Build the endpoint URL
            var url = EndpointUrl(_sttStartEndpoint);
            var payload = new STTUploadResponse {
                s3_key = s3Key
            };

            // Make the API call to upload the audio clip
            StartCoroutine(ApiClient.CallEndpointWithPostCoroutine(
                url, _timeoutInSeconds, payload, (request) =>
            {
                var body = request.downloadHandler?.text ?? string.Empty;
                var response = ApiModel.FromJson<STTJobResponse>(body);
                var jobName = response?.JobName;

                Debug.Log($"Transcription job started: {jobName}");
                transcriptStartedCallback?.Invoke(jobName);
            }));
        }

        [ContextMenu("STT/Download Transcription")]
        public void DownloadTranscription(string jobName,
            Action<string> transcriptionReceivedCallback = null)
        {
            // Ensure there is an active session
            if (_session == null)
            {
                Debug.LogWarning("No active session. Please initiate a session first.");
                return;
            }

            StopAllCoroutines();

            StartCoroutine(KeepCallingCoroutine(
                EndpointUrl(_sttDownloadEndpoint, jobName), .5f,
                transcriptionReceivedCallback
            ));

        }

        private IEnumerator KeepCallingCoroutine(string url,
            float delayInSeconds, Action<string> callback)
        {
            // Make the API call to download the transcription
            var wait = new WaitForSeconds(delayInSeconds);

            bool keepCalling = true;
            while (keepCalling)
            {
                yield return wait;
                yield return ApiClient.CallEndpointWithGetCoroutine(
                    url, _timeoutInSeconds, (request) =>
                {
                    var body = request.downloadHandler?.text ?? string.Empty;
                    var response = ApiModel.FromJson<STTJobResponse>(body);

                    if (response.Status == "FAILED")
                    {
                        keepCalling = false;
                        Debug.LogError("Transcription job failed.");
                        callback?.Invoke(null);
                    }
                    else if (response.Status == "COMPLETED")
                    {
                        keepCalling = false;
                        callback?.Invoke(response?.Transcript);
                    }
                });
            }
        }


        [ContextMenu("Chat/Send Message")]
        public void SendChatMessage(string message = null,
            Action<string> responseReceivedCallback = null,
            Action speechFinishedCallback = null)
        {
            // Ensure there is an active session
            if (_session == null)
            {
                Debug.LogWarning("No active session. Please initiate a session first.");
                return;
            }

            if (message != null) _query = message;

            StopAllCoroutines();

            // Build the endpoint URL and payload
            var url = EndpointUrl(_chatEndpoint, _session.SessionId);
            var payload = new ChatServicePayload { message = _query };

            // Make the API call. Expect an audio response.
            StartCoroutine(ApiClient.CallEndpointWithPostCoroutine(
                url, _timeoutInSeconds, payload, (request) =>
            {
                var body = request.downloadHandler?.text ?? string.Empty;
                var response = ApiModel.FromJson<ChatServiceResponse>(body);

                var chatResponse = response?.Message;
                responseReceivedCallback?.Invoke(chatResponse);

                var audioUrl = response?.AudioUrl;
                if (string.IsNullOrEmpty(audioUrl))
                {
                    Debug.LogWarning("No audio URL in response.");
                    return;
                }

                Debug.Log($"Downloading audio from: {audioUrl}");
                StartCoroutine(ApiClient.DownloadAudioCoroutine(
                    audioUrl, _timeoutInSeconds, (audioClip) =>
                {
                    if (audioClip == null)
                    {
                        Debug.LogError("AudioClip is null after download.");
                        return;
                    }
                    StartCoroutine(PlayAudioAndNotifyCoroutine(
                        audioClip, speechFinishedCallback));
                }));
            }));
        }

        private IEnumerator PlayAudioAndNotifyCoroutine(
            AudioClip audioClip, Action onComplete)
        {
            if (_audioSource == null || audioClip == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            _audioSource.PlayOneShot(audioClip);
            yield return new WaitForSeconds(audioClip.length);

            onComplete?.Invoke();
        }
        #endregion -- API Calls ------------------------------------------------

        [ContextMenu("Debug/Play or Stop Test Audio")]
        public void PlayTestAudio()
        {
            if (_audioSource == null) return;

            if (_audioSource.isPlaying) _audioSource.Stop();
            else _audioSource?.Play();
        }
    }
}
