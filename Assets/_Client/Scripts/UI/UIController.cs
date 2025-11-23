using System;

using UnityEngine;
using UnityEngine.UIElements;


namespace PPGIA.X540.Project3
{
    [RequireComponent(typeof(UIController))]
    public class UIController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private VisualElement _root;

        private readonly string[] _sessionButtonLabels = { 
            "Iniciar Sessão", 
            "Encerrar Sessão" 
        };
        private Button _sessionButton;
        private Button _sendChatButton;
        private int _currentSessionState = 0;

        private TextField _chatInputField;
        private TextField _chatOutputField;

        public string ChatOutput
        {
            get => _chatOutputField.value;
            set => _chatOutputField.value = value;
        }

        public bool SessionActive {
            get => _currentSessionState == 1;
            set
            {
                _currentSessionState = value ? 1 : 0;
                UpdateStateForSession();
            }
        }

        public Action OnSessionButtonClicked { get; set; }
        public Action<string> OnSendChatButtonClicked { get; set; }
        public float Progress { get; set; }

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            _root = _uiDocument.rootVisualElement;

            _sessionButton = _root.Q<Button>("B_Session");
            _sendChatButton = _root.Q<Button>("B_SendChat");
            _chatInputField = _root.Q<TextField>("TF_ChatInput");
            _chatOutputField = _root.Q<TextField>("TF_ChatOutput");

            SessionActive = false;
        }

        void OnEnable()
        {            
            _sessionButton.clicked += OnSessionButtonClickedInternal;
            _sendChatButton.clicked += OnSendChatButtonClickedInternal;
        }

        void OnDisable()
        {
            _sessionButton.clicked -= OnSessionButtonClickedInternal;
            _sendChatButton.clicked -= OnSendChatButtonClickedInternal;
        }

        private void UpdateStateForSession()
        {
            _sessionButton.text = _sessionButtonLabels[_currentSessionState];
            
            var enable = _currentSessionState == 1;
            _chatInputField.SetEnabled(enable);
            _sendChatButton.SetEnabled(enable);
        }

        private void OnSessionButtonClickedInternal()
        {
            OnSessionButtonClicked?.Invoke();
            // SessionActive state will be updated externally
        }

        private void OnSendChatButtonClickedInternal()
        {
            OnSendChatButtonClicked?.Invoke(_chatInputField.value);
            _chatInputField.value = string.Empty;
        }
    }
}
