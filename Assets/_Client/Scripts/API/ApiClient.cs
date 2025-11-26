using System;
using System.IO;
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

        internal static IEnumerator UploadAudioDataCoroutine(
            string url,
            string filePath,
            float timeoutInSeconds,
            Action<UnityWebRequest> callbackOnSuccess)
        {
            // PUT the audio data as binary
            byte[] audioData = File.ReadAllBytes(filePath);
            string fileName = Path.GetFileName(filePath);

            using (UnityWebRequest request = UnityWebRequest.Put(url, audioData))
            {
                request.SetRequestHeader("Content-Type", "audio/wav");
                
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
                    Debug.LogError($"Failed to upload audio data: {request.error} (HTTP {request.responseCode})\nBody: {body}");
                }
            }
        }

        internal static IEnumerator UploadAudioCoroutine(
            string url,
            AudioClip audioClip,
            float timeoutInSeconds,
            Action<UnityWebRequest> callbackOnSuccess)
        {
            // Convert AudioClip to WAV (PCM 16-bit little endian) without external utility.
            byte[] audioData = AudioClipToWavBytes(audioClip);
            string fileName = $"{audioClip.name}.wav";
            string fieldName = "file";

            yield return UploadFileCoroutine(
                url, audioData, fileName, fieldName, timeoutInSeconds, callbackOnSuccess);
        }

        // Writes a WAV file header + PCM 16-bit data for the provided AudioClip.
        // Supports mono or multi-channel clips. Assumes clip.samples * channels fits in int32.
        private static byte[] AudioClipToWavBytes(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogError("AudioClipToWavBytes: clip is null");
                return Array.Empty<byte>();
            }

            int channels = clip.channels;
            int sampleCount = clip.samples * channels; // total samples across channels
            int sampleRate = clip.frequency;

            // Get float data
            float[] floatData = new float[sampleCount];
            clip.GetData(floatData, 0);

            // Convert to 16-bit PCM
            // Each sample -> 2 bytes
            byte[] pcmData = new byte[sampleCount * 2];
            int pcmIndex = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                // Clamp just in case
                float f = Mathf.Clamp(floatData[i], -1f, 1f);
                short s = (short)Mathf.RoundToInt(f * 32767f);
                pcmData[pcmIndex++] = (byte)(s & 0xFF); // little endian
                pcmData[pcmIndex++] = (byte)((s >> 8) & 0xFF);
            }

            // WAV header size is 44 bytes
            int headerSize = 44;
            int fileSize = headerSize + pcmData.Length;
            byte[] wav = new byte[fileSize];

            // Helper local to write int/short little endian
            void WriteInt32LE(int offset, int value)
            {
                wav[offset] = (byte)(value & 0xFF);
                wav[offset + 1] = (byte)((value >> 8) & 0xFF);
                wav[offset + 2] = (byte)((value >> 16) & 0xFF);
                wav[offset + 3] = (byte)((value >> 24) & 0xFF);
            }
            void WriteInt16LE(int offset, short value)
            {
                wav[offset] = (byte)(value & 0xFF);
                wav[offset + 1] = (byte)((value >> 8) & 0xFF);
            }

            // ChunkID "RIFF"
            wav[0] = (byte)'R'; wav[1] = (byte)'I'; wav[2] = (byte)'F'; wav[3] = (byte)'F';
            // ChunkSize = 36 + Subchunk2Size
            int subchunk2Size = pcmData.Length; // NumSamples * NumChannels * BitsPerSample/8
            WriteInt32LE(4, 36 + subchunk2Size);
            // Format "WAVE"
            wav[8] = (byte)'W'; wav[9] = (byte)'A'; wav[10] = (byte)'V'; wav[11] = (byte)'E';
            // Subchunk1ID "fmt "
            wav[12] = (byte)'f'; wav[13] = (byte)'m'; wav[14] = (byte)'t'; wav[15] = (byte)' ';
            // Subchunk1Size (16 for PCM)
            WriteInt32LE(16, 16);
            // AudioFormat (1 = PCM)
            WriteInt16LE(20, 1);
            // NumChannels
            WriteInt16LE(22, (short)channels);
            // SampleRate
            WriteInt32LE(24, sampleRate);
            // ByteRate = SampleRate * NumChannels * BitsPerSample/8
            int byteRate = sampleRate * channels * 2;
            WriteInt32LE(28, byteRate);
            // BlockAlign = NumChannels * BitsPerSample/8
            WriteInt16LE(32, (short)(channels * 2));
            // BitsPerSample
            WriteInt16LE(34, 16);
            // Subchunk2ID "data"
            wav[36] = (byte)'d'; wav[37] = (byte)'a'; wav[38] = (byte)'t'; wav[39] = (byte)'a';
            // Subchunk2Size
            WriteInt32LE(40, subchunk2Size);

            // Copy PCM data after header
            Buffer.BlockCopy(pcmData, 0, wav, headerSize, pcmData.Length);

            return wav;
        }

        internal static IEnumerator UploadFileCoroutine(
            string url,
            byte[] fileData,
            string fileName,
            string fieldName,
            float timeoutInSeconds,
            Action<UnityWebRequest> callbackOnSuccess)
        {
            WWWForm form = new WWWForm();
            form.AddBinaryData(fieldName, fileData, fileName);

            using (UnityWebRequest request =
                    UnityWebRequest.Post(url, form))
            {
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
                    Debug.LogError(
                        $"Error uploading file: {request.error}");
                }
            }
        }

        internal static IEnumerator DownloadAudioCoroutine(
            string url,
            float timeoutInSeconds,
            Action<AudioClip> callbackOnSuccess)
        {
            using (UnityWebRequest mmRequest =
                    UnityWebRequestMultimedia.GetAudioClip(url, AudioType.OGGVORBIS))
            {

                var op = mmRequest.SendWebRequest();
                yield return WaitForTimeout(op, timeoutInSeconds, () =>
                {
                    Debug.LogError("Request timed out.");
                });

                if (mmRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error loading audio: {mmRequest.error}");
                    yield break;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(mmRequest);
                if (clip == null)
                {
                    Debug.LogError("AudioClip is null after download.");
                    yield break;
                }

                callbackOnSuccess?.Invoke(clip);
            }
        }
    }
}