using System;

using UnityEngine;
using UnityEngine.UIElements;


namespace PPGIA.X540.Project3
{
    [RequireComponent(typeof(UIDocument))]
    public class UIController : MonoBehaviour
    {
        public enum UIState
        {
            Idle = 0,
            Recording = 1,
            Processing = 2
        }

        private UIDocument _uiDocument;
        private VisualElement _root;

        #region -- Fields & Properties ----------------------------------------
        private readonly string[] _sendChatButtonLabels = {
            "Falar...",
            "Enviar...",
            "Processando... Aguarde..."
        };

        // UI controls --------------------------------------------------------
        private Button _talkButton;

        private TextField _chatOutputField;
        public string ChatOutput
        {
            get => _chatOutputField.value;
            set => _chatOutputField.value = value;
        }

        private ProgressBar _progressBar;
        public float Progress
        {
            get => _progressBar.value;
            set => _progressBar.value = value;
        }

        // State management ---------------------------------------------------
        private UIState _currentState = UIState.Idle;
        public UIState CurrentState
        {
            get => _currentState;
            set
            {
                _currentState = value;
                _talkButton.text = _sendChatButtonLabels[(int)value];
                if (value == UIState.Processing)
                {
                    _talkButton.SetEnabled(false);
                    _progressBar.value = 0.5f;
                }
                else
                {
                    _talkButton.SetEnabled(true);
                    _progressBar.value = 0f;
                }
            }
        }

        // Event Handlers -----------------------------------------------------
        public event Action OnTalkButtonClicked;

        #endregion ------------------------------------------------------------

        #region -- MonoBehaviour Methods --------------------------------------
        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            _root = _uiDocument.rootVisualElement;

            _talkButton = _root.Q<Button>("B_Talk");
            if (_talkButton == null)
            {
                Debug.LogError("Talk Button not found in UI.");
            }

            _progressBar = _root.Q<ProgressBar>("PB_Progress");
            if (_progressBar == null)
            {
                Debug.LogError("Progress Bar not found in UI.");
            }

            _chatOutputField = _root.Q<TextField>("TF_Dialogue");
            if (_chatOutputField == null)
            {
                Debug.LogError("Chat Output Field not found in UI.");
            }

            CurrentState = UIState.Idle;
        }

        private void OnEnable()
        {
            _talkButton.clicked += OnTalkButtonClickedInternal;
        }

        private void OnDisable()
        {
            _talkButton.clicked -= OnTalkButtonClickedInternal;
        }
        #endregion ------------------------------------------------------------

        private void OnTalkButtonClickedInternal() => OnTalkButtonClicked?.Invoke();

        public void AppendChatOutput(string newText)
        {
            ChatOutput += newText;
        }
    }
}
