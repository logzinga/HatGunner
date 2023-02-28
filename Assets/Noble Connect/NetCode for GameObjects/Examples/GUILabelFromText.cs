using Unity.Netcode;
using UnityEngine;

namespace NobleConnect.Examples.NetCodeForGameObjects
{
    public class GUILabelFromText : MonoBehaviour
    {
        public TextAsset textFile;
        TMPro.TMP_Text textComponent;
        string text;

        void Start()
        {
            text = textFile.text;
            textComponent = GetComponent<TMPro.TMP_Text>();
            textComponent.text = text;
        }

        private void Update()
        {
            if (NetworkManager.Singleton && textComponent)
            {
                textComponent.enabled = !NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient;
            }
        }
    }
}