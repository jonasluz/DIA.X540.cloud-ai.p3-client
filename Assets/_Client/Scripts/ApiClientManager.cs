using System.Linq;

using UnityEngine;


namespace PPGIA.X540.Project3.API
{
    [RequireComponent(typeof(AudioSource))]
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
        private string _chatEndpoint = "/chat/";

        [SerializeField]
        private string _llmAgentEndpoint = "/agent/ask";

        [SerializeField]
        private string _ttsEndpoint = "/tts/synthesize";

        [SerializeField]
        private string _sttEndpoint = "/stt/upload";

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
            _audioSource = GetComponent<AudioSource>();
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
        public void InitiateSession()
        {
            StopAllCoroutines();

            var url = EndpointUrl(_sessionInitEndpoint, _clientId);

            StartCoroutine(ApiClient.CallEndpointWithPostCoroutine(
                url, _timeoutInSeconds, null, (request) =>
            {
                var body = request.downloadHandler?.text ?? string.Empty;
                var session = JsonUtility.FromJson<Session>(body);
                _session = session;
            }));
        }

        [ContextMenu("Session/Close Session")]
        public void CloseSession()
        {    
            if (_session == null)
            {
                Debug.LogWarning("No active session to close.");
                return;
            }

            StopAllCoroutines();

            var url = EndpointUrl(_sessionCloseEndpoint, _session.SessionId);

            StartCoroutine(ApiClient.CallEndpointWithDeleteCoroutine(
                url, _timeoutInSeconds, (request) =>
            {
                Debug.Log("Session closed successfully.");
                _session = null;
            }));
        }

        [ContextMenu("Chat/Send Message")]
        public void SendChatMessage()
        {
            // Ensure there is an active session
            if (_session == null)
            {
                Debug.LogWarning("No active session. Please initiate a session first.");
                return;
            }

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

                    _audioSource?.PlayOneShot(audioClip);
                }));
            }));
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
