using UnityEngine;

// Listens for the Attack action and tells the axe to swing.
//
// Put this on the Player object and drag the hand's
// ViewModel into the axeAnimator slot.
public class AxeSwingInput : MonoBehaviour
{
    [Header("References")]

    [SerializeField] private HandAxeAnimator axeAnimator;


    private PlayerControls controls;


    private void Awake()
    {
        controls = new PlayerControls();

        controls.Player.Attack.performed += _ => Swing();
    }


    private void OnEnable()
    {
        controls.Enable();
    }


    private void OnDisable()
    {
        controls.Disable();
    }


    private void Swing()
    {
        if (axeAnimator == null)
        {
            return;
        }

        axeAnimator.PlaySwing();
    }
}
