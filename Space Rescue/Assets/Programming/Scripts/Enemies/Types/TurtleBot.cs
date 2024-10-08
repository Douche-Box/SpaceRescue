using System.Collections.Generic;
using UnityEngine;

public class TurtleBot : EnemyAI
{
    [Header("Hiding")]
    [SerializeField] bool _isHiding;
    public bool IsHiding
    { get { return _isHiding; } set { _isHiding = value; } }

    [SerializeField] bool _preyInRange;

    [SerializeField] float _maxHideTime;
    [SerializeField] float _hideTime;

    [SerializeField] float _hideCheckRadius;

    [SerializeField] LayerMask _hideMask;


    [Header("Lights")]

    [SerializeField] Renderer _ledRenderer;
    public Renderer LedRenderer
    { get { return _ledRenderer; } set { _ledRenderer = value; } }

    [SerializeField] Material _onLed;
    public Material OnLed
    { get { return _onLed; } }

    [SerializeField] Material _offLed;
    public Material OffLed
    { get { return _offLed; } }

    [SerializeField] bool _newPosition;

    public override void Start()
    {
        base.Start();

        // TargetTransform = null;

        IsHiding = true;
    }

    public override void Update()
    {
        if (HasPoweredOn)
        {
            CheckState();
        }

        if (Currentstate == State.PATROL)
        {
            Patrol();
        }
    }

    public override void ChangeState(State newState)
    {
        base.ChangeState(newState);
    }

    public override void CheckState()
    {
        base.CheckState();
    }

    #region Idle

    public override void StartIdle()
    {
        base.StartIdle();
    }

    public override void Idle()
    {
        base.Idle();
    }

    public override void StopIdle()
    {
        base.StopIdle();
    }

    #endregion

    #region Patrol

    public override void StartPatrol()
    {
        base.StartPatrol();
    }

    public override void Patrol()
    {
        if (TargetTransform != null && !_isHiding)
        {
            _newPosition = false;
            Agent.SetDestination(TargetTransform.position);

            Animator.SetBool("Walking", true);

            Vector3 targetWithOffset = new Vector3(TargetTransform.position.x, transform.position.y, TargetTransform.position.z);

            DistanceFromTarget = Vector3.Distance(transform.position, targetWithOffset);
        }

        if (DistanceFromTarget == 0 && !_isHiding)
        {
            _hideTime = 0;

            Animator.SetBool("Walking", false);

            Animator.SetBool("Hide", true);
        }

        if (_isHiding && _hideTime < _maxHideTime && !_preyInRange && _isHiding)
        {
            _hideTime += Time.deltaTime;
        }


        // DOES NOT WORK RIGHT NOW ???
        if (_hideTime >= _maxHideTime && DistanceFromTarget == 0 && !_newPosition)
        {
            _newPosition = true;

            List<Transform> temps = new List<Transform>(PatrolPoints);

            if (TargetTransform != null)
            {
                if (temps.Contains(TargetTransform))
                {
                    temps.Remove(TargetTransform);
                }
            }
            else
            {
                return;
            }

            TargetTransform = temps[Random.Range(0, temps.Count)];

            Animator.SetBool("Power", true);

            Animator.SetBool("Hide", false);

            Animator.SetBool("Walking", true);
        }
    }

    public override void StopPatrol()
    {
        base.StopPatrol();
    }

    #endregion

    #region Chase

    public override void StartChase()
    {
        base.StartChase();
    }

    public override void Chase()
    {
        base.Chase();
    }

    public override void StopChase()
    {
        base.StopChase();
    }

    #endregion

    #region Search

    public override void StartSearch()
    {
        base.StartSearch();
    }

    public override void Search()
    {
        base.Search();
    }

    public override void StopSearch()
    {
        base.StopSearch();
    }

    public override void GetRandomSearchPosition()
    {
        base.GetRandomSearchPosition();
    }

    public override void EnterDetection(Transform newDetected)
    {
        base.EnterDetection(newDetected);

        Animator.SetBool("Power", true);
    }

    public override void ExitDetection(Transform exitDeteted)
    {
        base.ExitDetection(exitDeteted);
    }

    #endregion

    #region Attack

    public override void StartAttack()
    {
        base.StartAttack();
    }

    public override void Attack()
    {
        base.Attack();
    }

    public override void StopAttack()
    {
        base.StopAttack();
    }

    #endregion

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
    }

    public override void GeneratePatrol(int patrolPoints)
    {
        base.GeneratePatrol(patrolPoints);
    }

    public override void Regeneration(float amount)
    {
        base.Regeneration(amount);
    }

    public override void GizmosLogic()
    {
        Gizmos.color = Color.green;

        base.GizmosLogic();
    }
}
