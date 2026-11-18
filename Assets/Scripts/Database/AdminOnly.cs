using UnityEngine;

//slap this on anything only admins should see, it hides itself for everyone else
public class AdminOnly : MonoBehaviour
{
    void Awake() => gameObject.SetActive(GameSession.IsAdmin);
}
