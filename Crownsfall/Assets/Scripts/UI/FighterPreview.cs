using Crownsfall.Characters;
using UnityEngine;
using UnityEngine.UI;

namespace Crownsfall.UI
{
    public class FighterPreview : MonoBehaviour
    {
        [SerializeField] private Image mountImage;
        [SerializeField] private Image bodyImage;
        [SerializeField] private Image weaponImage;
        [SerializeField] private Image headImage;

        private void Awake()
        {
            ConfigureImage(mountImage);
            ConfigureImage(bodyImage);
            ConfigureImage(weaponImage);
            ConfigureImage(headImage);
        }

        public void UpdatePreview(HeadSO head, BodySO body, WeaponSO weapon, MountSO mount)
        {
            SetLayer(headImage, head?.icon);
            SetLayer(bodyImage, body?.icon);
            SetLayer(weaponImage, weapon?.icon);
            SetLayer(mountImage, mount?.icon);
        }

        public void SetLayer(EquipmentType type, Sprite sprite)
        {
            switch (type)
            {
                case EquipmentType.Head:
                    SetLayer(headImage, sprite);
                    break;
                case EquipmentType.Body:
                    SetLayer(bodyImage, sprite);
                    break;
                case EquipmentType.Weapon:
                    SetLayer(weaponImage, sprite);
                    break;
                case EquipmentType.Mount:
                    SetLayer(mountImage, sprite);
                    break;
            }
        }

        private static void ConfigureImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static void SetLayer(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }
}
