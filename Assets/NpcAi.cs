using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NpcAi : MonoBehaviour
{
    // Variables to handle what we need to send through to our state.
    NavMeshAgent agent; // To store the NPC NavMeshAgent component.
    Animator anim; // To store the Animator component.
      // To store the transform of the player. This will let the guard know where the player is, so it can face the player and know whether it should be shooting or chasing (depending on the distance).
    NpcState currentState;

    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>(); // Grab agents NavMeshAgent.
        anim = this.GetComponent<Animator>(); // Grab agents Animator component.
        currentState = new NpcIdle(this.gameObject, agent, anim); // Create our first state.
    }

    void Update()
    {
        currentState = currentState.Process(); // Calls Process method to ensure correct state is set.
    }
}
