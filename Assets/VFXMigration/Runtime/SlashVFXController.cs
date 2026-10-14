using UnityEngine;

namespace VFXMigration
{
    public sealed class SlashVFXController : MonoBehaviour
    {
        [SerializeField] private UIVFXSlash slash;

        public void PlaySlash() { if (slash != null) slash.Play(); }
        public void StopSlash() { if (slash != null) slash.Stop(); }
    }
}
