using System;
using System.Collections;
using System.Text;

using UnityEngine;
using UnityEngine.Networking;


namespace PPGIA.X540.Project3.API
{
    internal class ApiClient
    {
        internal static byte[] EncodePayload(object payload)
        {
            var json = JsonUtility.ToJson(payload);
            return Encoding.UTF8.GetBytes(json);
        }

        static IEnumerator WaitForTimeout(
            UnityWebRequestAsyncOperation operation,
            float timeoutInSeconds,
            Action callbackIfTimeout = null)
        {
            float startTime = Time.realtimeSinceStartup;
            while (!operation.isDone)
            {
                if (Time.realtimeSinceStartup - startTime > timeoutInSeconds)
                {
                    callbackIfTimeout?.Invoke();
                    yield break;
                }
                yield return null;
            }
        }

        static IEnumerator CallEndpointCoroutine(string url,
            string method,
            object payload,
            float timeoutInSeconds,
            Action<UnityWebRequest> callbackOnSuccess)
        {
            using (var request = new UnityWebRequest(url, method))
            {
                if (method == "POST" || method == "PUT")
                {
                    request.SetRequestHeader("Content-Type", "application/json");
                }

                if (payload != null)
                {
                    byte[] bodyRaw = EncodePayload(payload);
                    Debug.Log($"Payload size: {bodyRaw.Length} bytes - {payload}");
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                } 

                request.downloadHandler = new DownloadHandlerBuffer();

                // Debug.Log($"Sending {method} request to {url}");
                // Debug.Log(
                //     payload != null ?
                //     $"Payload: {JsonUtility.ToJson(payload)}" :
                //     "No payload.");

                var op = request.SendWebRequest();
                yield return WaitForTimeout(op, timeoutInSeconds, () =>
                {
                    Debug.LogError("Request timed out.");
                });

                if (request.result == UnityWebRequest.Result.Success)
                {
                    callbackOnSuccess?.Invoke(request);
                }
                else
                {
                    var body = request.downloadHandler?.text ?? string.Empty;
                    var errorTrace = @$"API call failed: {request.error} (HTTP {request.responseCode})
Request Method: {method}
Request URL: {url}
Request Payload: {JsonUtility.ToJson(payload)}
Response Body: {body}";
                    Debug.LogError(errorTrace);
                }
            }
        }

        internal static IEnumerator CallEndpointWithGetCoroutine(
            string url, float timeoutInSeconds, 
            Action<UnityWebRequest> callbackOnSuccess)
        {
            yield return CallEndpointCoroutine(
                url, "GET", null, timeoutInSeconds, callbackOnSuccess);
        }

        internal static IEnumerator CallEndpointWithPostCoroutine(
            string url, float timeoutInSeconds, object payload, 
            Action<UnityWebRequest> callbackOnSuccess)
        {
            yield return CallEndpointCoroutine(
                url, "POST", payload, timeoutInSeconds, callbackOnSuccess);
        }

        internal static IEnumerator CallEndpointWithPutCoroutine(
            string url, float timeoutInSeconds, object payload, 
            Action<UnityWebRequest> callbackOnSuccess)
        {
            yield return CallEndpointCoroutine(
                url, "PUT", payload, timeoutInSeconds, callbackOnSuccess);
        }

        internal static IEnumerator CallEndpointWithDeleteCoroutine(
            string url, float timeoutInSeconds, 
            Action<UnityWebRequest> callbackOnSuccess)
        {
            yield return CallEndpointCoroutine(
                url, "DELETE", null, timeoutInSeconds, callbackOnSuccess);
        }
    }

    internal enum Environment
    {
        Development,
        Production
    }

    [Serializable]
    internal struct ChatServicePayload
    {
        public string message;
    }
}