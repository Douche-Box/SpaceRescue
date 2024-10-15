using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;


public class EnemyAI : Entity
{
    [SerializeField] float _regenRate;
    [SerializeField] float _regenAmount;

    [Header("Initialized")]

    [SerializeField] bool _isInitialized;

    [Header("State")]

    [SerializeField] State _currentState;
    public State Currentstate
    { get { return _currentState; } }

    public enum State
    {
        IDLE,
        PATROL,
        CHASE,
        SEARCH,
        ATTACK,
    }



    [Header("Setup")]
    [SerializeField] PlayManager _playManager;

    [SerializeField] EnemySO _enemyInfo;
    [SerializeField] Animator _animator;
    public Animator Animator
    { get { return _animator; } }

    [SerializeField] NavMeshAgent _agent;
    public NavMeshAgent Agent
    { get { return _agent; } }

    [SerializeField] EnemyAttackController _attackController;
    public EnemyAttackController AttackController
    { get { return _attackController; } }

    [SerializeField] EnemyDetection _enemyDetection;

    [Header("General")]
    public bool isAlive;

    [SerializeField] bool _hasPoweredOn;
    public bool HasPoweredOn
    { get { return _hasPoweredOn; } set { _hasPoweredOn = value; } }

    [SerializeField] bool _canNaturalRegen;

    [SerializeField] Transform _targetTransform;
    public Transform TargetTransform
    { get { return _targetTransform; } set { _targetTransform = value; } }

    [SerializeField] List<Transform> _possibleTargets = new();
    public List<Transform> PossibleTargets
    { get { return _possibleTargets; } }

    [SerializeField] Vector3 _targetVector;

    [SerializeField] float _distanceFromTarget;
    public float DistanceFromTarget
    { get { return _distanceFromTarget; } set { _distanceFromTarget = value; } }

    [Header("Patrol")]
    [SerializeField] float _distanceFromPatrol;
    public float DistanceFromPatrol
    { get { return _distanceFromPatrol; } set { _distanceFromPatrol = value; } }

    [SerializeField] float _patrolReturnDistance;
    public float PatrolReturnDistance
    { get { return _patrolReturnDistance; } }

    [SerializeField] int _currentPatrol;
    public int CurrentPatrol
    { get { return _currentPatrol; } }

    [SerializeField] int _patrolPointCount;
    [SerializeField] float _patrolRadius;

    [SerializeField] NavMeshHit _navmeshHit;
    [SerializeField] List<Transform> _patrolPoints = new();
    public List<Transform> PatrolPoints
    { get { return _patrolPoints; } }

    [SerializeField] Slider _healthBar;
    public Slider HealthBar
    { get { return _healthBar; } set { _healthBar = value; } }

    [Header("Search")]

    [SerializeField] float _searchTime;
    [SerializeField] float _timeToSearch;

    [SerializeField] int _maxSearchCount;
    [SerializeField] int _searchCount;

    [SerializeField] Transform _detectionHolder;

    [SerializeField] Vector3 _originalDetectionScale;

    [SerializeField] string[] _detectableObjects;

    [SerializeField] float _scaleSpeed;

    [SerializeField] float _minScale;
    [SerializeField] float _maxScale;

    [SerializeField] Vector3 _startSearchPosition;

    [SerializeField] bool _newRandomPos;

    [SerializeField] float _searchRadius;

    [Header("Chase")]

    [Header("Attack")]
    [SerializeField] float _maxAttackRange;
    public float MaxAttackRange
    { get { return _maxAttackRange; } }
    [SerializeField] float _minAttackRange;
    public float MinAttackRange
    { get { return _minAttackRange; } }

    [SerializeField] LayerMask _preyMask;
    public LayerMask PreyMask
    { get { return _preyMask; } }

    [SerializeField] bool _isAttacking;
    public bool IsAttacking
    { get { return _isAttacking; } set { _isAttacking = value; } }

    [SerializeField] GameObject _deathEffectPrefab;

    [Header("Robots")]
    [SerializeField] List<RobotAI> _attachedRobots = new();

    public override void Start()
    {
        _playManager = FindObjectOfType<PlayManager>();

        InitializeEnemyInfo();

        if (health > 0)
        {
            isAlive = true;
        }

        GeneratePatrol(_patrolPointCount);

        _searchCount = _maxSearchCount;

        _targetTransform = _patrolPoints[0];

        _originalDetectionScale = _detectionHolder.localScale;

        _enemyDetection = GetComponentInChildren<EnemyDetection>();

        _enemyDetection.detectableObjects = _detectableObjects;

        _healthBar = GetComponentInChildren<Slider>();

        _healthBar.gameObject.SetActive(false);

        _healthBar.maxValue = maxHealth;
    }

    public override void Update()
    {
        _healthBar.value = health;

        if (_hasPoweredOn && isAlive)
        {
            CheckState();
        }
    }

    public override void Death()
    {
        if (isAlive)
        {
            isAlive = false;

            for (int i = 0; i < _attachedRobots.Count; i++)
            {
                if (_attachedRobots[i] != null)
                {
                    _attachedRobots[i].RemoveAttachMent();
                }
            }

            _playManager.OnEnemyDeath(this);

            Debug.Log("Die");
            _animator.SetTrigger("Die");

        }
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);

        _healthBar.gameObject.SetActive(true);
    }

    public virtual void AfterDeath()
    {
        GameObject effect = Instantiate(_deathEffectPrefab);

        effect.transform.position = transform.position;

        Destroy(effect, 1f);

        Destroy(gameObject);
    }

    void InitializeEnemyInfo()
    {
        if (_enemyInfo != null && !_isInitialized)
        {
            if (maxHealth == 0)
            {
                _isInitialized = false;
            }
            else
            {
                _isInitialized = true;
            }

            maxHealth = _enemyInfo.health;
            health = maxHealth;

            speed = _enemyInfo.speed;
            rotationSpeed = _enemyInfo.rotationSpeed;

            if (_agent != null)
            {
                _agent.speed = speed;
                _agent.angularSpeed = rotationSpeed;
            }

            damage = _enemyInfo.damage;

            _maxAttackRange = _enemyInfo.maxAttackRange;
            _minAttackRange = _enemyInfo.minAttackRange;

            _regenRate = _enemyInfo.regenRate;
            _regenAmount = _enemyInfo.regenAmount;

            _patrolRadius = _enemyInfo.patrolRadius;
            _patrolReturnDistance = _enemyInfo.patrolReturnDistance;
            _patrolPointCount = _enemyInfo.patrolPoints;

            _searchRadius = _enemyInfo.searchRadius;
            _timeToSearch = _enemyInfo.timeToSearch;
            _searchCount = _enemyInfo.searchCount;
        }
    }

    public virtual void ChangeState(State newState)
    {
        switch (_currentState)
        {
            case State.IDLE:
                StopIdle();
                break;
            case State.PATROL:
                StopPatrol();
                break;
            case State.CHASE:
                StopChase();
                break;
            case State.SEARCH:
                StopSearch();
                break;
            case State.ATTACK:
                StopAttack();
                break;
            default:
                Debug.Log($"{newState} is not supported on this robot");
                break;
        }

        switch (newState)
        {
            case State.IDLE:
                StartIdle();
                break;
            case State.PATROL:
                StartPatrol();
                break;
            case State.CHASE:
                StartChase();
                break;
            case State.SEARCH:
                StartSearch();
                break;
            case State.ATTACK:
                StartAttack();
                break;
            default:
                Debug.Log($"{newState} is not supported on this robot");
                break;
        }
    }

    public virtual void CheckState()
    {
        switch (_currentState)
        {
            case State.IDLE:
                Idle();
                break;
            case State.PATROL:
                Patrol();
                break;
            case State.CHASE:
                Chase();
                break;
            case State.SEARCH:
                Search();
                break;
            case State.ATTACK:
                Attack();
                break;
        }
    }

    public void AttachRobot(RobotAI newrobot)
    {
        _attachedRobots.Add(newrobot);
    }

    #region Idle

    public virtual void StartIdle()
    {
        _currentState = State.IDLE;
    }

    public virtual void Idle()
    {

    }

    public virtual void StopIdle()
    {

    }

    #endregion

    #region Patrol

    public virtual void StartPatrol()
    {
        _currentState = State.PATROL;

        _canNaturalRegen = true;

        Transform closestPatrolPoint = null;
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < _patrolPoints.Count; i++)
        {
            float distance = Vector3.Distance(transform.position, _patrolPoints[i].position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPatrolPoint = _patrolPoints[i];
                _currentPatrol = i;
            }
        }

        _targetTransform = closestPatrolPoint;
    }

    public virtual void Patrol()
    {
        if (_targetTransform != null)
        {
            _agent.SetDestination(_targetTransform.position);

            _animator.SetBool("Walking", true);

            Vector3 targetWithOffset = new Vector3(_targetTransform.position.x, transform.position.y, _targetTransform.position.z);

            _distanceFromTarget = Vector3.Distance(transform.position, targetWithOffset);
        }

        if (_agent.stoppingDistance >= _distanceFromTarget && !_agent.isStopped)
        {
            _currentPatrol++;
            if (_currentPatrol >= _patrolPointCount)
            {
                _currentPatrol = 0;
            }
            _targetTransform = _patrolPoints[_currentPatrol];
        }
        else
        {
            _agent.isStopped = false;
        }
    }

    public virtual void StopPatrol()
    {
        _canNaturalRegen = false;

        StopCoroutine(NaturalRegeneration());
    }

    #endregion

    #region Chase

    public virtual void StartChase()
    {
        _currentState = State.CHASE;
    }

    public virtual void Chase()
    {
        if (_targetTransform != null && _possibleTargets.Count > 0)
        {
            _agent.SetDestination(_targetTransform.position);

            _animator.SetBool("Walking", true);

            Vector3 targetWithOffset = new Vector3(_targetTransform.position.x, transform.position.y, _targetTransform.position.z);

            _distanceFromTarget = Vector3.Distance(transform.position, targetWithOffset);

            Vector3 patrolWithOffset = new Vector3(_patrolPoints[_currentPatrol].position.x, transform.position.y, _patrolPoints[_currentPatrol].position.z);

            _distanceFromPatrol = Vector3.Distance(transform.position, patrolWithOffset);
        }
        else if (_possibleTargets.Count > 0)
        {
            _targetTransform = _possibleTargets[0];
        }


        if (_patrolReturnDistance <= _distanceFromPatrol)
        {
            ChangeState(State.PATROL);
        }

        if (_maxAttackRange >= _distanceFromTarget)
        {
            ChangeState(State.ATTACK);
        }
    }

    public virtual void StopChase()
    {

    }

    #endregion

    #region Search

    public virtual void StartSearch()
    {
        _currentState = State.SEARCH;

        _searchCount = _maxSearchCount;

        _targetTransform = null;

        _startSearchPosition = transform.position;
    }

    public virtual void Search()
    {
        if (_newRandomPos)
        {
            _agent.SetDestination(_targetVector);

            _animator.SetBool("Walking", true);

            Vector3 targetWithOffset = new Vector3(_targetVector.x, transform.position.y, _targetVector.z);
            _distanceFromTarget = Vector3.Distance(transform.position, targetWithOffset);

            if (_agent.stoppingDistance >= _distanceFromTarget && !_agent.isStopped)
            {
                _newRandomPos = false;

                _animator.SetBool("Walking", false);
            }
        }
        else
        {
            DetectionPulse();

            if (_searchTime < _timeToSearch)
            {
                _searchTime += Time.deltaTime;
            }
            else if (_searchTime >= _timeToSearch && _searchCount > 0)
            {
                _searchCount--;

                _searchTime = 0;

                GetRandomSearchPosition();
            }
            else if (_searchCount <= 0)
            {
                ChangeState(State.PATROL);
            }
        }

    }

    public virtual void StopSearch()
    {

    }

    public void DetectionPulse()
    {
        float scale = Mathf.Lerp(_minScale, _maxScale, (Mathf.Sin(Time.time * _scaleSpeed) + 1.0f) / 2.0f);

        _detectionHolder.localScale = _originalDetectionScale * scale;

        _detectionHolder.localScale = new Vector3(_detectionHolder.localScale.x, 1, _detectionHolder.localScale.z);
    }

    public virtual void GetRandomSearchPosition()
    {
        _newRandomPos = true;

        Vector3 randomOffset = Random.insideUnitSphere * _searchRadius;
        randomOffset.y = 0;
        Vector3 randomPosition = _startSearchPosition + randomOffset;

        if (NavMesh.SamplePosition(randomPosition, out _navmeshHit, _searchRadius, NavMesh.AllAreas))
        {
            _targetVector = _navmeshHit.position;
        }
        else
        {
            Debug.LogWarning("Could not find a valid search position on the navmesh");
        }
    }

    public virtual void EnterDetection(Transform newDetected)
    {
        if (!_possibleTargets.Contains(newDetected))
        {
            _possibleTargets.Add(newDetected);
        }

        if (_currentState != State.CHASE && _currentState != State.ATTACK && _possibleTargets.Count > 0)
        {
            _targetTransform = _possibleTargets[0];

            ChangeState(State.CHASE);
        }
    }

    public virtual void ExitDetection(Transform exitDetected)
    {
        _possibleTargets.Remove(exitDetected);
        // ChangeState(State.SEARCH); // Differnt thing
    }

    #endregion

    #region Attack

    public virtual void StartAttack()
    {
        _currentState = State.ATTACK;
    }

    public virtual void Attack()
    {
        if (_targetTransform != null)
        {
            Vector3 targetWithOffset = new Vector3(_targetTransform.position.x, transform.position.y, _targetTransform.position.z);
            _distanceFromTarget = Vector3.Distance(transform.position, targetWithOffset);

            Vector3 patrolWithOffset = new Vector3(_patrolPoints[_currentPatrol].position.x, transform.position.y, _patrolPoints[_currentPatrol].position.z);
            _distanceFromPatrol = Vector3.Distance(transform.position, patrolWithOffset);

            if (!_isAttacking && _distanceFromTarget > _maxAttackRange)
            {
                Debug.Log("not in attack range");
                _agent.isStopped = false;
                _agent.SetDestination(_targetTransform.position);
                _animator.SetBool("Walking", true);
            }

            if (!_isAttacking && Physics.SphereCast(transform.position, 0.1f, transform.forward, out RaycastHit hitInfo, _maxAttackRange, _preyMask))
            {
                Debug.Log("Hits");

                if (DistanceFromTarget >= _minAttackRange && DistanceFromTarget <= _maxAttackRange)
                {
                    _isAttacking = true;

                    _agent.isStopped = true;
                    _agent.velocity = Vector3.zero;

                    _animator.SetBool("Walking", false);
                    _attackController.DoRandomAttack();
                }
            }
            else if (!_isAttacking)
            {
                _animator.SetBool("Walking", true);


                Vector3 directionToTarget = (targetWithOffset - transform.position).normalized;

                if (directionToTarget != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }
        else if (_targetTransform == null && _possibleTargets.Count > 0 && !_isAttacking)
        {
            for (int i = 0; i < _possibleTargets.Count; i++)
            {
                if (_possibleTargets[i] == null)
                {
                    _possibleTargets.RemoveAt(i);
                }
            }
            if (_possibleTargets.Count > 0)
            {
                _targetTransform = _possibleTargets[0];
            }
        }
        else if (_targetTransform == null && _possibleTargets.Count == 0)
        {
            ChangeState(State.SEARCH);
        }

        if (_distanceFromPatrol > _patrolReturnDistance)
        {
            ChangeState(State.PATROL);
        }
    }

    public virtual void StopAttack()
    {

    }

    #endregion

    public virtual void GeneratePatrol(int patrolPoints)
    {
        float angleStep = 360f / patrolPoints;
        float randomOffsetRadius = 2f;

        GameObject holdObj = new GameObject("Hold_Points");

        for (int i = 0; i < patrolPoints; i++)
        {
            bool validPositionFound = false;

            while (!validPositionFound)
            {
                float angle = i * angleStep;

                float x = Mathf.Cos(angle * Mathf.Deg2Rad) * _patrolRadius;
                float z = Mathf.Sin(angle * Mathf.Deg2Rad) * _patrolRadius;
                Vector3 basePosition = new Vector3(x, 0, z) + transform.position;

                Vector3 randomOffset = Random.insideUnitSphere * randomOffsetRadius;
                randomOffset.y = 0;
                Vector3 randomPosition = basePosition + randomOffset;

                if (NavMesh.SamplePosition(randomPosition, out _navmeshHit, _patrolRadius, NavMesh.AllAreas))
                {
                    validPositionFound = true;

                    GameObject patrolObj = new GameObject($"Hold_Pos ({i})");

                    patrolObj.transform.position = _navmeshHit.position;
                    patrolObj.transform.parent = transform;

                    patrolObj.transform.SetParent(holdObj.transform);

                    _patrolPoints.Add(patrolObj.transform);
                }
            }
        }
    }

    public virtual void Regeneration(float amount)
    {
        health += amount;

        if (health > maxHealth)
        {
            health = maxHealth;
            _canNaturalRegen = false;
        }
    }

    public virtual IEnumerator NaturalRegeneration()
    {
        yield return new WaitForSeconds(_regenRate);

        health += _regenAmount;

        if (health > maxHealth)
        {
            health = maxHealth;
            _canNaturalRegen = false;
        }

        if (_canNaturalRegen)
        {
            StartCoroutine(NaturalRegeneration());
        }
    }

    public void OnValidate()
    {
        InitializeEnemyInfo();
    }

    [SerializeField] float _gizmoSize;

    public void OnDrawGizmos()
    {
        GizmosLogic();
    }

    public virtual void GizmosLogic()
    {
        for (int i = 0; i < _patrolPoints.Count; i++)
        {

            foreach (Transform position in _patrolPoints)
            {
                if (position != null)
                {
                    Gizmos.DrawSphere(position.position, _gizmoSize);
                }
            }
        }
    }
}
