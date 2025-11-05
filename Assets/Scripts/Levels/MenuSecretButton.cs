using UnityEngine; using UnityEngine.UI;
public class MenuSecretButton : MonoBehaviour {
  public Button secretButton;
  void Start(){ if(secretButton) secretButton.gameObject.SetActive(PlayerPrefs.GetInt("SecretUnlocked",0)==1); }
}
