using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class AgentWorldLabel : MonoBehaviour
    {
        private Camera view;

        private void LateUpdate()
        {
            if (view == null) view = Camera.main;
            if (view != null) transform.rotation = view.transform.rotation;
        }
    }
}
