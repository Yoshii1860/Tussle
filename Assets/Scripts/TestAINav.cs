using UnityEngine;
using UnityEngine.AI;

public class TestAINav : MonoBehaviour
{
    [SerializeField] private Transform target;

    NavMeshAgent agent;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    private void Update()
    {
        agent.SetDestination(target.position);
    }
}
