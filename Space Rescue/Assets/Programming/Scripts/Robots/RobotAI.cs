using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RobotAI : Entity
{
    [SerializeField] RobotSO _robotInfo;

    [SerializeField] NavMeshAgent _agent;

    [SerializeField] Transform _target;

    [SerializeField] Transform _player;

    [SerializeField] Collider _collider;

    [SerializeField] Transform _scrapRobot;

    [SerializeField] bool _isAttacking;

    [SerializeField] float _distanceFromTarget;

    public override void Start()
    {
        if (_robotInfo != null)
        {
            _agent.speed = _robotInfo.speed;
        }
    }

    public override void Update()
    {
        CheckState(_currentState);

        if (Input.GetKeyDown(KeyCode.K))
        {
            if (_currentState == State.FOLLOW)
            {
                ChangeState(State.ATTACK);
            }
            else
            {
                ChangeState(State.FOLLOW);
            }
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            ChangeState(State.GATHER);
        }

    }

    public State _currentState;

    public enum State
    {
        IDLE,
        FOLLOW,
        ATTACK,
        GATHER,
        ATTACHED,
    }

    public void ChangeState(State newState)
    {
        switch (_currentState)
        {
            case State.IDLE:
                StopIdle();
                break;
            case State.FOLLOW:
                StopFollow();
                break;
            case State.ATTACK:
                StopAttack();
                break;
            case State.GATHER:
                StopGather();
                break;
            case State.ATTACHED:
                StopAttached();
                break;
        }

        switch (newState)
        {
            case State.IDLE:
                StartIdle();
                break;
            case State.FOLLOW:
                StartFollow();
                break;
            case State.ATTACK:
                StartAttack();
                break;
            case State.GATHER:
                StartGather();
                break;
            case State.ATTACHED:
                StartAttached();
                break;
        }
    }

    void CheckState(State state)
    {
        switch (state)
        {
            case State.IDLE:
                Idle();
                break;
            case State.FOLLOW:
                Follow();
                break;
            case State.ATTACK:
                Attack();
                break;
            case State.GATHER:
                Gather();
                break;
            case State.ATTACHED:
                Attached();
                break;
        }
    }

    #region Idle

    void StartIdle()
    {
        _currentState = State.IDLE;
    }

    void Idle()
    {

    }

    void StopIdle()
    {

    }

    #endregion

    #region Follow

    void StartFollow()
    {
        _currentState = State.FOLLOW;

        _target = _player.GetComponent<PlayerController>().restSpot;

        _agent.enabled = true;

        _agent.stoppingDistance = _robotInfo.followDistance;
    }

    void Follow()
    {
        _agent.SetDestination(_target.position);

        Vector3 targetWithOffset = new Vector3(_target.position.x, transform.position.y, _target.position.z);

        _distanceFromTarget = Vector3.Distance(transform.position, targetWithOffset);

        if (_agent.stoppingDistance > _distanceFromTarget && !_agent.isStopped)
        {
            _agent.isStopped = true;
        }
        else
        {
            _agent.isStopped = false;
        }
    }

    void StopFollow()
    {

    }

    #endregion

    #region Attack

    void StartAttack()
    {
        _currentState = State.ATTACK;

        _agent.stoppingDistance = _robotInfo.range;

        _target = GameObject.FindGameObjectWithTag("Enemy").transform;
    }

    void Attack()
    {
        if (_agent.isActiveAndEnabled)
        {
            _agent.SetDestination(GameObject.FindGameObjectWithTag("Enemy").transform.position);
        }

        _distanceFromTarget = Vector3.Distance(transform.position, _target.position);

        if (_agent.stoppingDistance > _distanceFromTarget && !_isAttacking)
        {
            if (_agent.isActiveAndEnabled)
            {
                _agent.isStopped = true;
            }

            StartCoroutine(StartAttacking());
        }
        else
        {
            if (_agent.isActiveAndEnabled)
            {
                _agent.isStopped = false;
            }
        }
    }

    void StopAttack()
    {
        StartCoroutine(StopAttacking());
    }

    #endregion

    #region Gather

    void StartGather()
    {
        _currentState = State.GATHER;

        GetNewGatherTargetPosition();

        _agent.stoppingDistance = 0.1f;
    }

    void Gather()
    {
        if (_agent.isActiveAndEnabled)
        {
            _agent.SetDestination(_target.position);
        }

        Vector3 targetWithOffset = new Vector3(_target.position.x, transform.position.y, _target.position.z);

        _distanceFromTarget = Vector3.Distance(transform.position, targetWithOffset);

        if (_agent.stoppingDistance > _distanceFromTarget && !_agent.isStopped)
        {
            Debug.Log("Stop");
            transform.SetParent(_target);
            _agent.isStopped = true;
        }
        else if (_agent.stoppingDistance < _distanceFromTarget)
        {
            Debug.Log("UnStop");
            _agent.isStopped = false;
        }
    }

    void StopGather()
    {

    }

    #endregion

    #region Attached

    void StartAttached()
    {
        _currentState = State.ATTACHED;

        transform.SetParent(_target.transform, true);
    }

    void Attached()
    {

    }

    void StopAttached()
    {
        transform.SetParent(null);

        _collider.isTrigger = false;

        _agent.enabled = true; // maybe when hits the floor
    }

    #endregion

    public TestPosition tempthing;

    public void GetNewGatherTargetPosition()
    {
        _target = _target.GetComponent<Scrap>().GetGatherPosition();
    }

    public IEnumerator StartAttacking()
    {
        _isAttacking = true;
        yield return new WaitForSeconds(_robotInfo.windUpTime);

        StartCoroutine(Attacking());
    }

    public IEnumerator Attacking() // make sure it stops attacking before changing state
    {
        if (_target != null)
        {
            _target.GetComponent<Entity>().TakeDamage(_robotInfo.damage); // this still happens with this because it tries to attack the player ( probably )
        }

        yield return new WaitForSeconds(_robotInfo.fireRate);

        if (_isAttacking)
        {
            StartCoroutine(Attacking());
        }
    }

    public IEnumerator StopAttacking()
    {
        yield return new WaitForSeconds(_robotInfo.windDownTime);
        _isAttacking = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("hit enemy");
            other.gameObject.GetComponent<Entity>().TakeDamage(_robotInfo.impactDamage);

            _target = other.transform;
            ChangeState(State.ATTACHED);
        }
    }
}