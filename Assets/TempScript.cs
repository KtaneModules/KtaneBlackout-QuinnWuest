using UnityEngine;

public class TempScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMSelectable Sel;

    private void Start()
    {
        Sel.OnInteract += delegate () { Module.HandlePass(); return false; };
    }

}
