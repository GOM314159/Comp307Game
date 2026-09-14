using UnityEngine;
using UnityEngine.UI;

public class ReticleController : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Reticle Image")]
    [SerializeField]
    private Image reticleImage;


    [Header("Player")]
    [SerializeField]
    private PlayerEquipmentManager equipmentManager;


    [Header("Reticle Sprites")]
    [SerializeField]
    private Sprite normalNoWeapon;

    [SerializeField]
    private Sprite normalWeapon;

    [SerializeField]
    private Sprite hardcore;


    // =========================================================
    // STATE
    // =========================================================

    private Sprite currentSprite;


    // =========================================================
    // UNITY
    // =========================================================

    private void Start()
    {
        RefreshReticle();
    }


    private void Update()
    {
        RefreshReticle();
    }


    // =========================================================
    // RETICLE UPDATE
    // =========================================================

    private void RefreshReticle()
    {
        if (reticleImage == null)
        {
            return;
        }


        // -----------------------------------------------------
        // GAME STATE
        // -----------------------------------------------------

        if (GameStateManager.Instance == null)
        {
            HideReticle();
            return;
        }


        // -----------------------------------------------------
        // ONLY SHOW DURING ACTUAL GAMEPLAY
        // -----------------------------------------------------

        if (!IsGameplay())
        {
            HideReticle();
            return;
        }


        // -----------------------------------------------------
        // HARDCORE
        // -----------------------------------------------------

        if (GameStateManager.Instance.IsHardcore())
        {
            ShowReticle(hardcore);
            return;
        }


        // -----------------------------------------------------
        // NORMAL
        // -----------------------------------------------------

        bool holdingWeapon =
            equipmentManager != null &&
            equipmentManager.GetCurrentWeapon() != null;


        if (holdingWeapon)
        {
            ShowReticle(normalWeapon);
        }
        else
        {
            ShowReticle(normalNoWeapon);
        }
    }


    // =========================================================
    // GAMEPLAY CHECK
    // =========================================================

    private bool IsGameplay()
    {
        GameStateManager.GameMode mode =
            GameStateManager.Instance.GetGameMode();


        switch (mode)
        {
            // Actual playable sections
            case GameStateManager.GameMode.Lobby:
            case GameStateManager.GameMode.Case:
            case GameStateManager.GameMode.BossFight:

                break;


            // Everything below is NOT gameplay
            case GameStateManager.GameMode.Loading:
            case GameStateManager.GameMode.MainMenu:
            case GameStateManager.GameMode.WatchingPlay:
            case GameStateManager.GameMode.Cutscene:
            case GameStateManager.GameMode.Credits:
            case GameStateManager.GameMode.Paused:

                return false;
        }


        // -----------------------------------------------------
        // PLAYER STATE
        // -----------------------------------------------------

        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.PlayerState state =
                PlayerStateManager.Instance.GetCurrentState();


            switch (state)
            {
                case PlayerStateManager.PlayerState.Inventory:
                case PlayerStateManager.PlayerState.Reading:
                case PlayerStateManager.PlayerState.ShopMenu:
                case PlayerStateManager.PlayerState.PauseMenu:
                case PlayerStateManager.PlayerState.Cutscene:
                case PlayerStateManager.PlayerState.MainMenu:
                case PlayerStateManager.PlayerState.RIP:
                case PlayerStateManager.PlayerState.NoMovementNoCamera:

                    return false;
            }
        }


        return true;
    }


    // =========================================================
    // SHOW
    // =========================================================

    private void ShowReticle(Sprite sprite)
    {
        if (sprite == null)
        {
            HideReticle();
            return;
        }


        if (currentSprite != sprite)
        {
            reticleImage.sprite = sprite;
            currentSprite = sprite;
        }


        if (!reticleImage.enabled)
        {
            reticleImage.enabled = true;
        }
    }


    // =========================================================
    // HIDE
    // =========================================================

    private void HideReticle()
    {
        if (reticleImage != null)
        {
            reticleImage.enabled = false;
        }

        currentSprite = null;
    }
}