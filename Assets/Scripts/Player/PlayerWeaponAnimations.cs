
using UnityEngine;

public class PlayerWeaponAnimations : MonoBehaviour
{
    [Header("Equipment Manager")]
    [SerializeField]
    private PlayerEquipmentManager equipmentManager;

    private GameObject previousWeapon;

    private void Awake()
    {
        if (equipmentManager == null)
        {
            equipmentManager =
                GetComponent<PlayerEquipmentManager>();
        }
    }

    private void LateUpdate()
    {
        if (equipmentManager == null)
            return;

        // Refresh weapon visibility based on the active character.
        equipmentManager.RefreshWeaponVisuals();

        GameObject currentWeapon =
            equipmentManager.GetCurrentWeaponObject();

        if (currentWeapon == previousWeapon)
            return;

        if (previousWeapon != null)
        {
            Debug.Log("[Weapon] Disabled: " +
                      previousWeapon.name);
        }

        if (currentWeapon != null)
        {
            Debug.Log("[Weapon] Enabled: " +
                      currentWeapon.name);
        }

        previousWeapon = currentWeapon;
    }
}
