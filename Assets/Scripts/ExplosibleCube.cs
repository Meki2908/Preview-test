using UnityEngine;

public class ExplosibleCube : MonoBehaviour
{
    [SerializeField] private GameObject _cubeFragmentPrefab;

    public void Explode(float force, Vector3 center, float radius)
    {
        var fragmentCube = Instantiate(_cubeFragmentPrefab, transform.position, transform.rotation);
        var fragments = fragmentCube.GetComponentsInChildren<Rigidbody>();
        foreach (var rigid in fragments)
        {
            rigid.AddExplosionForce(force, center, radius);
        }
        Destroy(gameObject);
    }
}
