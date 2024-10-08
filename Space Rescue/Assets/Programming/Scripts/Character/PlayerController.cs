using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput = null;
    public PlayerInput PlayerInput => playerInput;

    [SerializeField] Animator _animator;

    [SerializeField] bool _isMoveAction;
    [SerializeField] Vector2 _currentMovementInput;
    [SerializeField] Vector3 _currentMovement;

    [SerializeField] bool _isMouseMovement;

    [SerializeField] bool _isThrow;

    [SerializeField] float _scrollTypes;

    [SerializeField] bool _isRecal;

    [SerializeField] LayerMask _throwLayer;
    [SerializeField] RaycastHit _throwHit;

    [SerializeField] GameObject _mouseReticle;

    [SerializeField] Transform _throwSpot;

    [SerializeField] int _robotTypeIndex;

    [SerializeField] GameObject _currentRobot;

    [SerializeField] LayerMask _walkLayerMask;

    [SerializeField] Rigidbody _rb;
    [SerializeField] Transform _playerModel;
    [SerializeField] Transform _orientation;
    [SerializeField] Camera _camera;

    [SerializeField] NavMeshAgent _agent;

    [SerializeField] float _moveForce;

    [SerializeField] float _playerRotationSpeed;

    [SerializeField] RobotManager _robotManager;


    [SerializeField] int _currentSquadNumber = 0;

    [SerializeField] RaycastHit hit1;
    [SerializeField] RaycastHit hit2;

    [SerializeField] float _baseSquadXOffset;
    [SerializeField] float _squadXoffset;

    [SerializeField] float _baseSquadSize;
    [SerializeField] float _squadSize;
    [SerializeField] float _extraRobotSize;


    [SerializeField] LayerMask _terrainLayer;
    [SerializeField] LayerMask _robotLayer;

    [SerializeField] Transform _squadRangePos;
    public Transform SquadRangePos
    { get { return _squadRangePos; } }


    private void Awake()
    {
        playerInput.actions.FindActionMap("Game").Enable();
        playerInput.actions.FindActionMap("Menu").Enable();
    }

    private void OnEnable()
    {
        playerInput.actions.FindAction("Move").started += OnMovement;
        playerInput.actions.FindAction("Move").performed += OnMovement;
        playerInput.actions.FindAction("Move").canceled += OnMovement;

        playerInput.actions.FindAction("MouseMove").started += OnMouseMovement;
        playerInput.actions.FindAction("MouseMove").canceled += OnMouseMovement;

        playerInput.actions.FindAction("Throw").started += StartThrow;
        playerInput.actions.FindAction("Throw").canceled += PerformThrow;

        playerInput.actions.FindAction("Recal").started += OnRecal;
        playerInput.actions.FindAction("Recal").performed += OnRecal;
        playerInput.actions.FindAction("Recal").canceled += OnRecal;

        playerInput.actions.FindAction("ScrollTypes").started += OnScroll;
        playerInput.actions.FindAction("ScrollTypes").performed += OnScroll;
        playerInput.actions.FindAction("ScrollTypes").canceled += OnScroll;
    }

    private void OnDisable()
    {
        playerInput.actions.FindAction("Move").started -= OnMovement;
        playerInput.actions.FindAction("Move").performed -= OnMovement;
        playerInput.actions.FindAction("Move").canceled -= OnMovement;

        playerInput.actions.FindAction("MouseMove").started -= OnMouseMovement;
        playerInput.actions.FindAction("MouseMove").canceled -= OnMouseMovement;

        playerInput.actions.FindAction("Throw").started -= StartThrow;
        playerInput.actions.FindAction("Throw").canceled -= PerformThrow;

        playerInput.actions.FindAction("Recal").started -= OnRecal;
        playerInput.actions.FindAction("Recal").performed -= OnRecal;
        playerInput.actions.FindAction("Recal").canceled -= OnRecal;

        playerInput.actions.FindAction("ScrollTypes").started -= OnScroll;
        playerInput.actions.FindAction("ScrollTypes").performed -= OnScroll;
        playerInput.actions.FindAction("ScrollTypes").canceled -= OnScroll;
    }

    private void Start()
    {
        _squadXoffset = _baseSquadXOffset; // Initialize squad X offset
        _squadSize = _baseSquadSize; // Set the initial squad range to the absolute value of _minSquadRange
        _robotManager = FindObjectOfType<RobotManager>();
    }

    void HandleSquadPosition()
    {
        // Check for ground below
        if (Physics.Raycast(transform.position, -transform.up, out hit1, 1.7f, _terrainLayer))
        {
            Debug.Log("hit ground");

            if (Physics.Raycast(transform.position, -_playerModel.forward, out hit2, 2f, _terrainLayer))
            {
                _squadRangePos.position = hit1.point + _playerModel.forward * _squadSize; // Move forward based on player's forward vector
            }
            else
            {
                _squadRangePos.position = hit1.point - _playerModel.forward * _squadXoffset; // Move backward based on player's forward vector
            }
        }
    }

    void HandleSquad()
    {
        // Get all colliders within the squad range using the overlap sphere
        Collider[] colliders = Physics.OverlapSphere(_squadRangePos.position, _squadSize, _robotLayer);

        List<RobotAI> robotsInRange = new List<RobotAI>();
        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent(out RobotAI robotAI) && robotAI._currentState == RobotAI.State.FOLLOW)
            {
                RobotAI newrobot = collider.GetComponent<RobotAI>();
                robotsInRange.Add(newrobot);

                if (!_robotManager.SquadContains(newrobot))
                {
                    _robotManager.AddRobotToSquad(newrobot);
                }
            }
        }

        List<RobotAI> robotsToRemove = new List<RobotAI>();
        foreach (RobotAI robot in _robotManager.unsortedSquad)
        {
            if (!robotsInRange.Contains(robot))
            {
                robotsToRemove.Add(robot);
            }
        }

        foreach (RobotAI robotToRemove in robotsToRemove)
        {
            _robotManager.RemoveRobotFromSquad(robotToRemove);
        }

        HandleSquadRange(); // Update squad range based on current squad composition
    }

    public void HandleSquadRange()
    {
        // Reset the squad offset and range
        _squadXoffset = _baseSquadXOffset; // Start with the initial offset behind the player
        _squadSize = _baseSquadSize; // Initialize the squad size (for overlap sphere)

        // Get the number of robots in the squad
        int robotCount = _robotManager.NumberOfRobotsInSquad;

        // Increase the squad offset and size for each additional robot
        for (int i = 0; i < robotCount; i++)
        {
            _squadXoffset += _extraRobotSize; // Extend the squad further back for each robot
            _squadSize += _extraRobotSize; // Increase the overlap sphere size
        }
    }

    private void Update()
    {
        _currentMovement = (_orientation.forward * _currentMovementInput.y) + (_orientation.right * _currentMovementInput.x);

        SpeedControl();

        if (_isMoveAction && !_isThrow)
        {
            _animator.SetBool("Walking", true);
            Quaternion lookRotation = Quaternion.LookRotation(_currentMovement, Vector3.up);
            _playerModel.transform.rotation = Quaternion.Slerp(_playerModel.transform.rotation, lookRotation, Time.deltaTime * _playerRotationSpeed);
        }
        else
        {
            _animator.SetBool("Walking", false);
        }

        if (_isThrow)
        {
            HoldThrow();
        }

        HandleMouseReticle();

        if (_isRecal) // can be better optimized to only happen on button press and not always in the update
        {
            _mouseReticle.GetComponent<TestRecal>().canCheck = true;
        }
        else
        {
            _mouseReticle.GetComponent<TestRecal>().canCheck = false;
        }

        HandleSquadPosition();

        HandleSquad();

        // if (_isMouseMovement) // Set Destanation to null if reached
        // {
        //     _isMouseMovement = false;

        //     Vector3 mousePos = Mouse.current.position.ReadValue();

        //     Ray ray = Camera.main.ScreenPointToRay(mousePos);

        //     if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _walkLayerMask))
        //     {
        //         Debug.Log($"Ray hit the ground at: {hit.point}");

        //         _agent.SetDestination(hit.point);
        //     }
        // }
    }

    public void HandleMouseReticle()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();

        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out _throwHit, Mathf.Infinity, _throwLayer))
        {
            _mouseReticle.transform.position = _throwHit.point;
        }
    }

    private void FixedUpdate()
    {
        MoveMent();
    }

    #region ROBOTS

    #endregion

    void MoveMent()
    {
        _rb.AddForce(_currentMovement.normalized * _moveForce * 10f, ForceMode.Force);
    }

    #region THROW

    void StartThrow(InputAction.CallbackContext context)
    {
        if (_robotManager.RobotsInSquad[_currentSquadNumber].Count > 0)
        {
            _currentRobot = _robotManager.RobotsInSquad[_currentSquadNumber][0].gameObject;
            _robotManager.RemoveRobotFromSquad(_robotManager.RobotsInSquad[_currentSquadNumber][0]);

            if (_robotManager.RobotsInSquad[_currentSquadNumber].Count == 0)
            {
                MoveToNextSquadWithRobots();
            }
        }

        if (_currentRobot != null)
        {
            _isThrow = true;

            _animator.SetTrigger("Hold");

            _currentRobot.GetComponent<Collider>().isTrigger = true; // maybe do in start throw
            _currentRobot.transform.position = _throwSpot.position;
            _currentRobot.GetComponent<RobotAI>().ChangeState(RobotAI.State.THROWN);
        }

    }

    private void MoveToNextSquadWithRobots()
    {
        int totalSquads = _robotManager.RobotsInSquad.Count;

        // Loop through squads to find the next one with robots
        for (int i = 1; i < totalSquads; i++)
        {
            int nextSquadNumber = (_currentSquadNumber + i) % totalSquads;

            if (_robotManager.RobotsInSquad[nextSquadNumber].Count > 0)
            {
                _currentSquadNumber = nextSquadNumber;
                Debug.Log("Switched to next squad with robots: Squad " + _currentSquadNumber);
                return; // Exit once we find the next valid squad
            }
        }

        // Optionally, handle the case where no squads have robots left
        Debug.Log("No squads left with robots.");
    }

    void HoldThrow() // maybe change robot state
    {
        _currentRobot.transform.position = _throwSpot.position;

        Quaternion lookRotation = Quaternion.LookRotation(_throwHit.point, Vector3.up);
        _playerModel.transform.rotation = Quaternion.Slerp(_playerModel.transform.rotation, lookRotation, Time.deltaTime * _playerRotationSpeed);
    }

    void PerformThrow(InputAction.CallbackContext context)
    {
        _mouseReticle.SetActive(true);

        if (_isThrow)
        {
            _isThrow = false;

            _animator.SetTrigger("Yeet");
            SimulatedArcThrow(_throwSpot.position, _throwHit.point, _currentRobot, 0.75f);
            _currentRobot = null;
        }
        else
        {
            _currentRobot = null;
        }
    }

    void CancleThrow() // put down robot instead of throwing
    {
        _currentRobot = null;

        _currentRobot.GetComponent<RobotAI>().ChangeState(RobotAI.State.FOLLOW);
    }

    public void SimulatedArcThrow(Vector3 start, Vector3 target, GameObject robot, float duration)
    {
        robot.GetComponent<NavMeshAgent>().enabled = false;

        StartCoroutine(ArcMovement(start, target, robot, duration));
    }

    private IEnumerator ArcMovement(Vector3 start, Vector3 target, GameObject robot, float duration)
    {
        float timeElapsed = 0f;
        Vector3 peak = (start + target) / 2f + Vector3.up * 5f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;

            float t = timeElapsed / duration;
            Vector3 currentPos = CalculateParabolicPosition(start, peak, target, t);

            robot.transform.position = currentPos;

            yield return null;
        }
    }

    private Vector3 CalculateParabolicPosition(Vector3 start, Vector3 peak, Vector3 end, float t)
    {
        Vector3 a = Vector3.Lerp(start, peak, t);
        Vector3 b = Vector3.Lerp(peak, end, t);
        return Vector3.Lerp(a, b, t);
    }

    #endregion

    void SpeedControl()
    {
        Vector3 flatVel = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);

        if (flatVel.magnitude > _moveForce)
        {
            Vector3 limitedVel = flatVel.normalized * _moveForce;
            _rb.velocity = new Vector3(limitedVel.x, _rb.velocity.y, limitedVel.z);
        }
    }

    private void OnMovement(InputAction.CallbackContext context)
    {
        _currentMovementInput = context.ReadValue<Vector2>().normalized;
        _isMoveAction = _currentMovementInput.x != 0 || _currentMovementInput.y != 0;
    }

    private void OnMouseMovement(InputAction.CallbackContext context)
    {
        _isMouseMovement = context.ReadValueAsButton();
    }

    private void OnRecal(InputAction.CallbackContext context)
    {
        _isRecal = context.ReadValueAsButton();
    }

    private void OnScroll(InputAction.CallbackContext context)
    {
        float scrollValue = context.ReadValue<float>();

        if (scrollValue > 0) // Scroll up
        {
            _currentSquadNumber = (_currentSquadNumber + 1) % _robotManager.RobotsInSquad.Count;
            _robotManager.CurrentSquad = _currentSquadNumber;
        }
        else if (scrollValue < 0) // Scroll down
        {
            _currentSquadNumber = (_currentSquadNumber - 1 + _robotManager.RobotsInSquad.Count) % _robotManager.RobotsInSquad.Count;
            _robotManager.CurrentSquad = _currentSquadNumber;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(_squadRangePos.position, _squadSize);
    }
}
