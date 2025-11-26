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

        #region -- Fields & Properties ----------------------------------------
        // Buttons ------------------------------------------------------------
        private readonly string[] _sessionButtonLabels = {
            "Iniciar Sessão",
            "Encerrar Sessão"
        };
        private Button _sessionButton;
        private Button _sendChatButton;

        private int _currentSessionState = 0;
        public bool SessionActive
        {
            get => _currentSessionState == 1;
            set
            {
                _currentSessionState = value ? 1 : 0;
                _sessionButton.text = _sessionButtonLabels[_currentSessionState];
                InputEnabled = value;
            }
        }

        // Chat Fields --------------------------------------------------------
        private TextField _chatInputField;
        public string ChatInput
        {
            get => _chatInputField.value;
            set => _chatInputField.value = value;
        }

        private TextField _chatOutputField;
        public string ChatOutput
        {
            get => _chatOutputField.value;
            set => _chatOutputField.value = value;
        }

        public bool InputEnabled
        {
            get
            {
                var value = _chatInputField.enabledSelf;
                _sendChatButton.SetEnabled(value);
                return value;
            }
            set
            {
                _chatInputField.SetEnabled(value);
                _sendChatButton.SetEnabled(value);
            }
        }

        // Progress Bar -------------------------------------------------------
        private ProgressBar _progressBar;
        public float Progress
        {
            get => _progressBar.value;
            set => _progressBar.value = value;
        }

        // Event Handlers -----------------------------------------------------
        public event Action OnSessionButtonClicked;
        public event Action<string> OnSendChatButtonClicked;

        #endregion ------------------------------------------------------------

        #region -- MonoBehaviour Methods --------------------------------------
        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            _root = _uiDocument.rootVisualElement;

            _sessionButton = _root.Q<Button>("B_Session");
            _sendChatButton = _root.Q<Button>("B_SendChat");
            _chatInputField = _root.Q<TextField>("TF_ChatInput");
            _chatOutputField = _root.Q<TextField>("TF_ChatOutput");
            _progressBar = _root.Q<ProgressBar>("PB_Progress");
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
        #endregion ------------------------------------------------------------

        private void OnSessionButtonClickedInternal()
        {
            OnSessionButtonClicked?.Invoke();
            // SessionActive state should be updated externally, no logic here.
        }

        private void OnSendChatButtonClickedInternal()
        {
            OnSendChatButtonClicked?.Invoke(_chatInputField.value);
            _chatInputField.value = string.Empty;
        }
    }
}
