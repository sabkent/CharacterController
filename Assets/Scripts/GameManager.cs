using System.Collections.Generic;
using Unity.Entities.Content;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Hash128 = Unity.Entities.Hash128;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public UIDocument Menu;

    public WeakObjectSceneReference GameResourcesSubscene;

    private GameSession _gameSession;
    private MenuController _menuController;
    private readonly HashSet<Hash128> _loadedSubsceneGuids = new();

    public void Initialize()
    {
        if(Instance != null)
            GameObject.Destroy(Instance.gameObject);

        Instance = this;
        GameObject.DontDestroyOnLoad(this.gameObject);

        GameInput.Initialize();

        _gameSession = GameSession.CreateClientServer();
        _gameSession.LoadIntoWorlds(GameResourcesSubscene);
        LoadActiveSubscenesIntoWorlds();
        SceneManager.sceneLoaded += OnSceneLoaded;

        SetupUI();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LoadActiveSubscenesIntoWorlds();
    }

    private void LoadActiveSubscenesIntoWorlds()
    {
        foreach (var subScene in FindObjectsByType<SubScene>(FindObjectsSortMode.None))
        {
            if (!subScene.isActiveAndEnabled || !subScene.AutoLoadScene || !subScene.SceneGUID.IsValid)
            {
                continue;
            }

            if (_loadedSubsceneGuids.Add(subScene.SceneGUID))
            {
                _gameSession.LoadIntoWorlds(subScene.SceneGUID);
            }
        }
    }

    private void SetupUI()
    {
        _menuController = new MenuController(Menu);
    }

    private void Update()
    {
        if (GameInput.InputActions.Player.ToggleMenu.WasPressedThisFrame())
        {
            _menuController.Toggle();

            Debug.Log($"toggle menu");
        }
    }
}

public class MenuController
{
    public MenuController(UIDocument menuDocument)
    {
        _menuDocument = menuDocument;

        _quitButton = _menuDocument.rootVisualElement.Q<Button>(ElementNames.QuitButton);

        _quitButton.RegisterCallback<ClickEvent>(OnQuit);

        _menuDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    private UIDocument _menuDocument;
    private bool _displayed = false;
    private Button _quitButton;

    public void Toggle()
    {
        _displayed = !_displayed;
        _menuDocument.rootVisualElement.style.display = _displayed ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnQuit(ClickEvent clickEvent)
    {
        Debug.Log("you clicked me you mofo");
    }

    private static class ElementNames
    {
        public const string QuitButton = "quitCommand";
    }
}
