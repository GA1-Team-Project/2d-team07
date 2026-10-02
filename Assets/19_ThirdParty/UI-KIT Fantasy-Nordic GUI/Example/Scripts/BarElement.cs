using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarElement : MonoBehaviour
{
    [SerializeField] private bool isActive;
    [SerializeField] private GameObject active;
    [SerializeField] private GameObject disabled;

    public void SetState(bool v)
    {
        isActive = v;
        active.SetActive(v);
        disabled.SetActive(!v);
    }
}
