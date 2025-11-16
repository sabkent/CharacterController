using Unity.Entities.Content;
using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    public UIDocument Menu;

    public WeakObjectSceneReference GameResourcesSubscene;

    private GameSession _gameSession;
    private MenuController _menuController;
    
    public void Initialize()
    {
        if(Instance != null)
            GameObject.Destroy(Instance.gameObject);
        
        Instance = this;
        GameObject.DontDestroyOnLoad(this.gameObject);
        
        GameInput.Initialize();
        
        _gameSession = GameSession.CreateClientServer();
        _gameSession.LoadIntoWorlds(GameResourcesSubscene);
        
        SetupUI();
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
