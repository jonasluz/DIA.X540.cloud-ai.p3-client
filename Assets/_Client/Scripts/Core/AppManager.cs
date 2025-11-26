using UnityEngine;

using PPGIA.X540.Project3.API;


namespace PPGIA.X540.Project3
{
    public class AppManager : MonoBehaviour
    {
        // Singleton instance
        public static AppManager Instance { get; private set; }

        #region -- Fields & Properties ----------------------------------------
        [Header("References")]
        [SerializeField]
        private UIController _uiController;

        [SerializeField]
        private ApiClientManager _apiManager;

        [SerializeField]
        private AudioCapture _audioCapture;

        private AudioClip _recordedClip;
        #endregion ------------------------------------------------------------

        #region -- MonoBehaviour Methods --------------------------------------
        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (_uiController == null)
            {
                Debug.LogError("UIController reference is missing in AppManager.");
            }
            if (_apiManager == null)
            {
                Debug.LogError("ApiClientManager reference is missing in AppManager.");
            }
            if (_audioCapture == null)
            {
                Debug.LogError("AudioCapture reference is missing in AppManager.");
            }

            _uiController.OnTalkButtonClicked += HandleTalkButtonClicked;
            _audioCapture.OnRecordingSaved += HandleClipSaved;

        }

        private void OnEnable()
        {
            _apiManager.InitiateSession(() =>
            {
                Debug.Log("API session initiated successfully.");
            });
        }

        void OnDisable()
        {
            if (_apiManager != null && _apiManager.IsSessionActive)
            {
                _apiManager.CloseSession(() =>
                {
                    Debug.Log("API session closed successfully.");
                });
            }
        }

        private void OnDestroy()
        {
            _uiController.OnTalkButtonClicked -= HandleTalkButtonClicked;
            _audioCapture.OnRecordingSaved -= HandleClipSaved;
        }
        #endregion ------------------------------------------------------------

        private void HandleClipSaved(string filePath)
        {
            Debug.Log($"Audio clip saved at: {filePath}");

            _apiManager.UploadAudioClip(
                filePath,
                (s3Key) =>
                {
                    Debug.Log($"Clip uploaded to: {s3Key}");
                    _apiManager.StartTranscript(
                        s3Key,
                        (jobName) =>
                        {
                            Debug.Log($"Transcription job started: {jobName}");
                            _apiManager.DownloadTranscription(jobName, (transcript) =>
                            {
                                Debug.Log($"Transcription completed: {transcript}");
                                _uiController.AppendChatOutput($"\nUser: {transcript}\n");

                                _apiManager.SendChatMessage(transcript,
                                    (response) =>
                                    {
                                        _uiController.AppendChatOutput($"Bot: {response}\n");
                                    }, () =>
                                    {
                                        // Speech synthesis finished.
                                    });
                                });
                            });
                        });

            _uiController.CurrentState = UIController.UIState.Idle;
        }

        private void HandleTalkButtonClicked()
        {
            if (!_apiManager.IsSessionActive)
            {
                Debug.LogWarning("Session is not active. Cannot send message.");
                return;
            }

            switch (_uiController.CurrentState)
            {
                case UIController.UIState.Idle:
                    _audioCapture.StartRecording();
                    _uiController.CurrentState = UIController.UIState.Recording;
                    break;

                case UIController.UIState.Recording:
                    _audioCapture.StopRecording();
                    _uiController.CurrentState = UIController.UIState.Idle;
                    break;

                case UIController.UIState.Processing:
                    Debug.Log("Currently processing. Please wait.");
                    break;
            }
        }
    }
}
