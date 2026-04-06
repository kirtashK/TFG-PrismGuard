using UnityEngine;

public class WaveFlag : MonoBehaviour
{
    [SerializeField] private GameObject model;

    public void ToggleModel(bool toggle)
    {
        model.SetActive(toggle);
    }
}
