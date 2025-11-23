using UnityEngine;

using PPGIA.X540.Project3.API;


namespace PPGIA.X540.Project3
{
    public class AppManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private UIController _uiController;

        [SerializeField]
        private ApiClientManager _apiManager;

        private void Awake()
        {
            if (_uiController == null)
                _uiController = GetComponent<UIController>();
            if (_apiManager == null)
                _apiManager = GetComponent<ApiClientManager>();
        }

        void Start()
        {
            _apiManager.CloseSession(
                () => _uiController.SessionActive = _apiManager.IsSessionActive);
        }

        private void OnEnable()
        {
            _uiController.OnSessionButtonClicked += HandleSessionButtonClicked;
            _uiController.OnSendChatButtonClicked += HandleSendChatButtonClicked;
        }

        private void OnDisable()
        {
            _uiController.OnSessionButtonClicked -= HandleSessionButtonClicked;
            _uiController.OnSendChatButtonClicked -= HandleSendChatButtonClicked;
        }

        private void HandleSessionButtonClicked()
        {
            if (!_apiManager.IsSessionActive)
            {
                _apiManager.InitiateSession(
                    () => _uiController.SessionActive = _apiManager.IsSessionActive);
            }
            else
            {
                _apiManager.CloseSession(
                    () => _uiController.SessionActive = _apiManager.IsSessionActive
                );
            }
        }

        private void HandleSendChatButtonClicked(string message)
        {
            _apiManager.SendChatMessage(message, 
                (responseMessage) => 
            {
                _uiController.ChatOutput += $"User: {message}\n";
                _uiController.ChatOutput += $"Bot: {responseMessage}\n";
            },
                () =>
            {
                // Speech finished callback (optional)
            });
        }
    }
}
