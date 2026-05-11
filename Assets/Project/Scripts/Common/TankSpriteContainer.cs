using BrmnModules.DataManagement;

public class TankSpriteContainer : SpriteContainer
{
    public static TankSpriteContainer Instance { get; private set; }

    protected override void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(gameObject); 
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        base.Awake();
    }
}