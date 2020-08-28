using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NpcState 
{
    public enum STATE
    {
        IDLE , SCOUT ,APPROACH , HIDE , KILL 
    };

    // 'Events' - where we are in the running of a STATE.
    public enum EVENT
    {
        ENTER, UPDATE, EXIT
    };

    public STATE name; // To store the name of the STATE.
    protected EVENT stage; // To store the stage the EVENT is in.
    protected GameObject npc; // To store the NPC game object.
    protected Animator anim; // To store the Animator component.
    protected Transform player; // To store the transform of the player. This will let the guard know where the player is, so it can face the player and know whether it should be shooting or chasing (depending on the distance).
    protected NpcState nextState; // This is NOT the enum above, it's the state that gets to run after the one currently running (so if IDLE was then going to PATROL, nextState would be PATROL).
    protected NavMeshAgent agent; // To store the NPC NavMeshAgent component.

    float visDist = 10.0f; // When the player is within a distance of 10 from the NPC, then the NPC should be able to see it...
    float visAngle = 30.0f; // ...if the player is within 30 degrees of the line of sight.
    float shootDist = 7.0f; // When the player is within a distance of 7 from the NPC, then the NPC can go into an ATTACK state.
    float sneakdistance = 1.25f;
    
   
    // Constructor for State
    public NpcState(GameObject _npc, NavMeshAgent _agent, Animator _anim)
    {
        npc = _npc;
        agent = _agent;
        anim = _anim;
        stage = EVENT.ENTER;
        
    }

    // Phases as you go through the state.
    public virtual void Enter() { stage = EVENT.UPDATE; } // Runs first whenever you come into a state and sets the stage to whatever is next, so it will know later on in the process where it's going.
    public virtual void Update() { stage = EVENT.UPDATE; } // Once you are in UPDATE, you want to stay in UPDATE until it throws you out.
    public virtual void Exit() { stage = EVENT.EXIT; } // Uses EXIT so it knows what to run and clean up after itself.

    // The method that will get run from outside and progress the state through each of the different stages.
    public NpcState Process()
    {
        if (stage == EVENT.ENTER) Enter();
        if (stage == EVENT.UPDATE) Update();
        if (stage == EVENT.EXIT)
        {
            Exit();
            return nextState; // Notice that this method returns a 'state'.
        }
        return this; // If we're not returning the nextState, then return the same state.
    }
    public GameObject[] Knockout;
    public GameObject enem;
    public GameObject enem2;
    
    public GameObject GetSafe(GameObject enemy)
    {
        float lastdist = Mathf.Infinity;
        GameObject lastret = GameObject.FindGameObjectWithTag("Safe");
        GameObject[] safebox = GameObject.FindGameObjectsWithTag("Safe");
        foreach(GameObject safe in safebox)
        {
            float dist = Vector3.Distance(safe.transform.position, enemy.transform.position);
            if (dist < lastdist)
            {
                lastdist = dist;
                lastret = safe;
            }
        }
        return lastret;
    }
    public bool CanBeseen()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Npc");
        foreach(GameObject var in enemies)
        {
            Vector3 direction = npc.transform.position - var.transform.position;
            float angle = Vector3.Angle(direction, var.transform.forward);
            if (direction.magnitude < visDist && angle < visAngle)
            {
                return true; 
            }
        }
        return false;
    }

    public void Enemy()
    {
        float lastdist = Mathf.Infinity;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Npc");
        foreach(GameObject var in enemies)
        {
            
            float dist = Vector3.Distance(npc.transform.position, var.transform.position);
        
            if(dist < lastdist)
            {
                
                lastdist = dist;
                enem = var;
            }     
        }
        if (lastdist == Mathf.Infinity)
        {
            enem = null;
        }
    }
    public GameObject GetTarget()
    {
        return enem;
    }
    public bool EnemyCount()
    {
        GameObject[] cont = GameObject.FindGameObjectsWithTag("Npc");
        if(cont.Length > 0)
        {
            return true;
        }
        return false;
    }
}


public class NpcIdle : NpcState
{
  
    public NpcIdle(GameObject _npc, NavMeshAgent _agent, Animator _anim)
                : base(_npc, _agent, _anim)
    {
        name = STATE.IDLE;// Set name of current state.
        agent.isStopped = true;
        
    }

    public override void Enter()
    {
        anim.SetTrigger("isIdle");
        Debug.Log("State Idle");
       
        base.Enter();
    }
    public override void Update()
    {
        if (EnemyCount() && !CanBeseen())
        {
            nextState = new NpcScout(npc, agent, anim);
            stage = EVENT.EXIT;
        } 
   
        if (CanBeseen())
        {
            nextState = new NpcHide(npc, agent, anim);
            stage = EVENT.EXIT;
        }

        if (!EnemyCount())
        {
            base.Update();
        }
       /* else // used while debugging code to trigger states 
        {
            agent.SetDestination(GameObject.FindGameObjectWithTag("Player").transform.position);
            agent.speed = 5f;
        }*/

    }

    public override void Exit()
    {
        anim.ResetTrigger("isIdle");
        base.Exit();
    }
}

public class NpcScout : NpcState
{
    GameObject target;
    float distance;
    
    public NpcScout(GameObject _npc, NavMeshAgent _agent, Animator _anim)
                : base(_npc, _agent, _anim)
    {
        name = STATE.SCOUT;
        agent.speed = 5.5f;
        
    }

    public override void Enter()
    {
        Debug.Log("State Scout" );
      
        agent.isStopped = false;
        anim.SetTrigger("isRunning");
        Enemy();
        target = GetTarget();
        if(target == null)
        {
            nextState = new NpcIdle(npc, agent, anim);
            stage = EVENT.EXIT;
        }
        npc.transform.LookAt(target.transform.position);
        
        agent.isStopped = false;
        base.Enter();
    }

    public override void Update()
    {
        agent.SetDestination(target.transform.position);
        distance = Vector3.Distance(npc.transform.position, target.transform.position);
        if(distance < 5f )
        {
            anim.SetTrigger("isWalking");
            agent.speed = 3f;
            
        }
        
        if(distance < 1.25f)
        {
            nextState = new NpcKill(npc, agent, anim);
            stage = EVENT.EXIT;
        }
        if (CanBeseen())
        {
            nextState = new NpcHide(npc, agent, anim);
            stage = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        anim.speed = 1;
        anim.ResetTrigger("isWalking");
        anim.ResetTrigger("isRunning");
        base.Exit();
    }
}



public class NpcHide : NpcState
{
    
    bool activated = true;
    GameObject pursuer;
    public NpcHide(GameObject _npc, NavMeshAgent _agent, Animator _anim)
                : base(_npc, _agent, _anim)
    {
        agent.speed = 7f;
        name = STATE.HIDE;
        
    }

    public override void Enter()
    {
        Debug.Log("State Hide");
       
        agent.isStopped = false;
        anim.SetTrigger("isRunning");
        base.Enter();
    }

    public override void Update()
    {
        Enemy();
        pursuer = GetTarget();
        Vector3 direction = (npc.transform.position - pursuer.transform.position).normalized;
        if (Vector3.Distance(npc.transform.position, pursuer.transform.position) < 20)
        {
            agent.SetDestination(direction + agent.transform.position);
        }
        if (Vector3.Distance(npc.transform.position, pursuer.transform.position) >= 20 && activated)
        {
            Vector3 relativePos = pursuer.transform.position - npc.transform.position;
            Quaternion rotation = Quaternion.LookRotation(relativePos, Vector3.up);
            npc.transform.rotation = rotation;
            anim.SetTrigger("isIdle");
            anim.ResetTrigger("isRunning");
            agent.isStopped = true;
            FunctionTimer.Create(() => nextState = new NpcKill(npc, agent, anim), 3.5f);
            FunctionTimer.Create(() => stage = EVENT.EXIT, 3.5f);
            activated = false;
        }
    }

    public override void Exit()
    {
        anim.ResetTrigger("isIdle");
        anim.ResetTrigger("isRunning");
        base.Exit();
        
    }
}

public class NpcKill : NpcState
{
    
    public NpcKill(GameObject _npc, NavMeshAgent _agent, Animator _anim)
                : base(_npc, _agent, _anim)
    {
        name = STATE.KILL;
    }

    public override void Enter()
    {
        Debug.Log("Kill" );
        
        anim.SetTrigger("UpperCut");
        base.Enter();
     
        FunctionTimer.Create(() => nextState = new NpcApproach(npc, agent, anim), 4f);
        FunctionTimer.Create(() => stage = EVENT.EXIT, 4f);

    }

    public override void Update()
    {
        if (CanBeseen())
        {
            nextState = new NpcHide(npc, agent, anim);
            stage = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        anim.ResetTrigger("UpperCut");
        base.Exit();
    }
}
 
public class NpcApproach : NpcState
{
    bool look = true;
    bool activated = true;
    GameObject safe;
    GameObject target;
   
    public NpcApproach(GameObject _npc, NavMeshAgent _agent, Animator _anim)
               : base(_npc, _agent, _anim)
    {
        name = STATE.APPROACH;
        agent.isStopped = false;
    }

    public override void Enter()
    {
        Debug.Log("Approach" );
        anim.SetTrigger("isRunning");
        agent.speed = 4f;
        Enemy();
        target = GetTarget();
        if (target == null)
        {
          
            if (agent.remainingDistance < 2f)
            {
                Debug.Log("Game Finished");
                safe = GameObject.FindGameObjectWithTag("Finish");
                look = false;
            }
        }
        else
        {
            
            safe = GetSafe(target);
        }
        base.Enter();
    }

    public override void Update()
    {
        if (CanBeseen())
        {
            nextState = new NpcHide(npc, agent, anim);
            stage = EVENT.EXIT;
        }
        agent.SetDestination(safe.transform.position);
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            anim.SetTrigger("isIdle");
            agent.isStopped = true;
            if(look)
                npc.transform.LookAt(target.transform.position);
            if (activated)
            {
                FunctionTimer.Create(() => nextState = new NpcScout(npc, agent, anim), 5f);
                FunctionTimer.Create(() => stage = EVENT.EXIT, 5f);
                activated = false;
            }
            else
            {
                base.Update();
            }
        }

    }

    public override void Exit()
    {
        anim.speed = 1f;
        anim.ResetTrigger("isIdle");
        anim.SetTrigger("isRunning");
        base.Exit();
    }
}
