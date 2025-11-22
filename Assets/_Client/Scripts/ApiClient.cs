using System; 
using System.Collections;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.Networking;


namespace PPGIA.X540.Project3.API
{
    public class ApiClient : MonoBehaviour
    {
        #region -- Inspector Fields -------------------------------------------
        [Header("API URL Settings")]
        [SerializeField]
        private string _apiBaseUrl = "https://api.example.com";

        [SerializeField]
        private string _sessionInitEndpoint = "/session/init";

        [SerializeField]
        private string _sessionCloseEndpoint = "/session/close";

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

        #region -- Helper Methods ---------------------------------------------
        // Helper Method to build endpoint URLs
        string EndpointUrl(params string[] parts) =>
            _apiBaseUrl.TrimEnd('/') + '/' +
            string.Join("/", parts.Select(p => p.Trim('/')));

        // Helper Method to encode payloads as JSON byte arrays
        byte[] EncodePayload(object payload) =>
            Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

        IEnumerator WaitForTimeout(
            UnityWebRequestAsyncOperation operation,
            Action callbackIfTimeout = null)
        {
            float startTime = Time.realtimeSinceStartup;
            while (!operation.isDone)
            {
                if (Time.realtimeSinceStartup - startTime > _timeoutInSeconds)
                {
                    callbackIfTimeout?.Invoke();
                    yield break;
                }
                yield return null;
            }
        }

        IEnumerator CallEndpointWithGetCoroutine(
            string url, Action<UnityWebRequest> callback)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                var op = request.SendWebRequest();
                yield return WaitForTimeout(op, () =>
                {
                    Debug.LogError("Request timed out.");
                });

                callback?.Invoke(request);
            }
        }

        IEnumerator CallEndpointWithPostCoroutine(
            string url, object payload, Action<UnityWebRequest> callback)
        {
            using (var request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = EncodePayload(payload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                var op = request.SendWebRequest();
                yield return WaitForTimeout(op, () =>
                {
                    Debug.LogError("Request timed out.");
                });

                callback?.Invoke(request);
            }
        }
        #endregion -- Helper Methods ------------------------------------------

        #region -- API Calls --------------------------------------------------
        [ContextMenu("Test API Availability")]
        public void TestApiAvailability()
        {
            var url = EndpointUrl("");
            Debug.Log($"Testing API availability at: {url}");

            StartCoroutine(CallEndpointWithGetCoroutine(url, (request) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var body = request.downloadHandler?.text ?? string.Empty;
                    Debug.Log($"API call returned: {body}");
                }
                else
                {
                    Debug.LogError(
                        $"API availability check failed: {request.error} (HTTP {request.responseCode})");
                }
            }));
        }

        [ContextMenu("Initiate Session")]
        public string InitiateSession()
        {
            var url = EndpointUrl(_sessionInitEndpoint, _clientId);
            Debug.Log($"Initiating session at: {url}");

            StartCoroutine(CallEndpointWithPostCoroutine(url, null, (request) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var body = request.downloadHandler?.text ?? string.Empty;
                    var session = JsonUtility.FromJson<Session>(body);
                    _session = session;
                }
                else
                {
                    Debug.LogError(
                        $"Session init failed: {request.error} (HTTP {request.responseCode})");
                }
            }));

            return _session.SessionId;
        }
        #endregion -- API Calls ------------------------------------------------
    }
}