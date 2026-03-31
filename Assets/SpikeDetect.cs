using UnityEngine;


public class DetectTrigger : MonoBehaviour
{
    private SpikeCop _cop;

    private void Awake()
    {
        _cop = GetComponentInParent<SpikeCop>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_cop != null)
            _cop.OnDetect(other);
    }
}
