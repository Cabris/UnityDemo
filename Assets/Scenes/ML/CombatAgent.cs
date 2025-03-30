using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class CombatAgent : Agent
{
    /// <summary>
    /// The ground. The bounds are used to spawn the elements.
    /// </summary>
    public GameObject ground;
    public GameObject env;
    public GameObject target;
    public MapSettings mapSettings;
    public RayPerceptionSensorComponent3D rayPerceptionSensor;
    public Vector3 lastTargetPos;
    public float previousDistToTarget;
    public bool userControl = false;
    public float newRotation;
    private readonly float targetMemoryDuration = 1f; // 記憶維持時間（秒）
    private float timeSinceLastSeen = 0f;      // 距離上次看到目標的時間
    private bool hasLastTargetPos = false;
    private readonly Vector3 unknowTargetPos = new Vector3(1, 0f, 0f);
    private readonly float unknowTargetDist = 99999;

    [HideInInspector]
    public Bounds groundBounds;
    EnvironmentParameters m_ResetParams;
    Rigidbody m_AgentRb;  //cached on initialization
    private readonly string targetTag = "target";
    private readonly string wallTag = "wall";

    public override void Initialize()
    {
        base.Initialize();
        groundBounds = ground.GetComponent<Collider>().bounds;
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        m_AgentRb = GetComponent<Rigidbody>();
        if (mapSettings == null)
            mapSettings = FindFirstObjectByType<MapSettings>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);
        // 添加 AI 觀察數據 (敵人位置、子彈數量等)
        sensor.AddObservation(transform.position); // AI 位置
        sensor.AddObservation(transform.forward);  // AI 朝向
        sensor.AddObservation(hasLastTargetPos ? 1f : 0f);// 是否有看到目標
        sensor.AddObservation(lastTargetPos); // 目標位置
        //Debug.Log($"position: {transform.position}, eulerAngles: {transform.rotation.eulerAngles}");
        //Debug.Log($"hasLastTargetPos: {hasLastTargetPos}, lastTargetPos: {lastTargetPos}");
    }

    [SerializeField]
    private float _linearVelocity;

    [SerializeField]
    private bool _isForwardWall;


    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers.ContinuousActions);//[-1,1]

        if (CheckRayCast(out float seeTarget, out float nearestTargetDist, out bool isForwardWall, out float wallDist))
        {
            if (seeTarget > 0f)
            {
                float seeTargetReward = seeTarget * 1f;
                float nearTargetReward = (1 - nearestTargetDist) * 1f;
                float appreachTargetReward = 0;

                float dis = Vector3.Distance(lastTargetPos, transform.position);
                if (dis < previousDistToTarget)
                {
                    //approching target
                    if (previousDistToTarget == unknowTargetDist)//第一次看到目標 不給獎勵
                        appreachTargetReward = 0f;
                    else
                    {
                        float reward = (previousDistToTarget - dis) * 0.1f / previousDistToTarget;
                        if (reward < 0)
                            appreachTargetReward = 0;
                        else
                            appreachTargetReward = reward;
                    }

                }
                previousDistToTarget = dis;
                Debug.Log($"seeTargetReward: {seeTargetReward}," +
                $" nearTargetReward: {nearTargetReward}, appreachTargetReward: {appreachTargetReward}");
                AddReward((seeTargetReward + nearTargetReward + appreachTargetReward) * Time.deltaTime);
            }
            else if (hasLastTargetPos)// 如果沒看到目標 但有記憶
            {
                timeSinceLastSeen += Time.deltaTime; // 記憶時間累積
                if (timeSinceLastSeen >= targetMemoryDuration)
                {
                    lastTargetPos = unknowTargetPos; // 忘記目標
                    hasLastTargetPos = false;
                }
            }

            if (hasLastTargetPos)//add reward for facing target
            {
                Vector3 vctToTarget = lastTargetPos - transform.position;
                float angle = Vector3.Angle(vctToTarget, transform.forward);
                float angleReward = (1 - angle / 180f) * 10f;
                //Debug.Log($"angleReward: {angleReward}");
                AddReward(angleReward * Time.deltaTime);
            }

            if (isForwardWall)
            {
                float wallReward = (wallDist - 1) * 5;
                //Debug.Log($"wallReward: {wallReward}");
                AddReward(wallReward * Time.deltaTime);

                if (m_AgentRb.linearVelocity.sqrMagnitude < 0.01f)
                {
                    float wallStopReward = -5f;
                    Debug.Log($"wallStopReward: {wallStopReward}");
                    SetReward(wallStopReward);
                    EndEpisode(); // 強制重新開始
                }
            }
            _isForwardWall = isForwardWall;
            _linearVelocity = m_AgentRb.linearVelocity.sqrMagnitude;

        }
        AddReward(-1f / MaxStep);
    }

    private bool CheckRayCast(out float seeTarget, out float nearestTargetDist,
        out bool isForwardWall, out float wallDist)
    {
        //RayPerceptionOutput.RayOutput[] rayOutputs = rayPerceptionSensor.RaySensor.RayPerceptionOutput.RayOutputs;
        RayPerceptionOutput.RayOutput[] rayOutputs = RayPerceptionSensor.Perceive(
        rayPerceptionSensor.GetRayPerceptionInput(),
        rayPerceptionSensor.UseBatchedRaycasts).RayOutputs;

        isForwardWall = false;
        wallDist = 1;

        if (rayOutputs == null)
        {
            seeTarget = 0;
            nearestTargetDist = 1;
            return false;
        }
        int lengthOfRayOutputs = rayOutputs.Length;
        int seeTargetCount = 0;
        float nearestDistance = rayPerceptionSensor.RayLength;
        // Alternating Ray Order: it gives an order of
        // (0, -delta, delta, -2delta, 2delta, ..., -ndelta, ndelta)
        // index 0 indicates the center of raycasts
        for (int i = 0; i < lengthOfRayOutputs; i++)
        {
            GameObject goHit = rayOutputs[i].HitGameObject;
            if (goHit != null)
            {
                var rayDirection = rayOutputs[i].EndPositionWorld - rayOutputs[i].StartPositionWorld;
                var scaledRayLength = rayDirection.magnitude;
                float rayHitDistance = rayOutputs[i].HitFraction * scaledRayLength;

                if (goHit.CompareTag(targetTag))
                {
                    seeTargetCount++;
                    if (rayHitDistance < nearestDistance)
                        nearestDistance = rayHitDistance;
                    lastTargetPos = goHit.transform.position;
                    hasLastTargetPos = true;
                    timeSinceLastSeen = 0f; // 重置記憶計時器
                }

                if (i == 0 && goHit.CompareTag(wallTag))//forward
                {
                    isForwardWall = true;
                    wallDist = rayHitDistance / rayPerceptionSensor.RayLength;
                }
            }
        }
        seeTarget = (float)seeTargetCount / (float)lengthOfRayOutputs;
        nearestTargetDist = nearestDistance / rayPerceptionSensor.RayLength;

        return true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(targetTag))
        {
            Debug.Log("Hit target");
            SetReward(10f);
            EndEpisode();
        }

        if (collision.collider.CompareTag(wallTag))
        {
            Debug.Log("Hit wall");
            AddReward(-5f);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag(wallTag))
        {
            AddReward(-5f * Time.deltaTime);
        }
    }

    /// <summary>
    /// In the editor, if "Reset On Done" is checked then AgentReset() will be
    /// called automatically anytime we mark done = true in an agent script.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisodeBegin");

        var rotation = Random.Range(0, 4);
        var rotationAngle = rotation * 90f;
        env.transform.Rotate(new Vector3(0f, rotationAngle, 0f));

        ResetBlock();
        transform.position = GetRandomSpawnPos(gameObject);
        m_AgentRb.linearVelocity = Vector3.zero;
        m_AgentRb.angularVelocity = Vector3.zero;
        SetResetParameters();
    }

    void SetResetParameters()
    {
        SetGroundMaterialFriction();
        previousDistToTarget = unknowTargetDist;
        lastTargetPos = unknowTargetPos;
        timeSinceLastSeen = 0;
        hasLastTargetPos = false;
    }

    public void SetGroundMaterialFriction()
    {
        var groundCollider = ground.GetComponent<Collider>();
        //groundCollider.material.dynamicFriction = m_ResetParams.GetWithDefault("dynamic_friction", 0);
        //groundCollider.material.staticFriction = m_ResetParams.GetWithDefault("static_friction", 0);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.Log("Heuristic");

        var continuousActions = actionsOut.ContinuousActions;
        Vector2 inputDirection = GetUserInputDirection();
        continuousActions[0] = inputDirection.x;
        continuousActions[1] = inputDirection.y;
    }

    /// <summary>
    /// Resets the block position and velocities.
    /// </summary>
    void ResetBlock()
    {
        // Get a random position for the block.
        target.transform.position = GetRandomSpawnPos(target);
    }

    private void Update()
    {
        if (userControl)
        {
            Vector2 inputDirection = GetUserInputDirection();
            Movement(inputDirection);
        }

    }

    private static Vector2 GetUserInputDirection()
    {
        Vector2 inputDirection = new Vector2();
        if (Input.GetKey(KeyCode.D))
        {
            inputDirection.x = 1;
        }

        else if (Input.GetKey(KeyCode.A))
        {
            inputDirection.x = -1;
        }

        if (Input.GetKey(KeyCode.W))
        {
            inputDirection.y = 1;
        }

        else if (Input.GetKey(KeyCode.S))
        {
            inputDirection.y = -1;
        }

        return inputDirection;
    }

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
    public void MoveAgent(ActionSegment<float> act)
    {
        Vector2 inputDirection = new Vector2(act[0], act[1]);
        if (!(inputDirection.x == 0 && inputDirection.y == 0))
        {
            Movement(inputDirection);
        }
    }

    [SerializeField]
    Vector2 _inputDirection;

    private void Movement(Vector2 inputDirection)
    {
        _inputDirection = inputDirection;
        newRotation = Mathf.Atan2(inputDirection.x, inputDirection.y) * Mathf.Rad2Deg;
        float turnSpeed = mapSettings.agentRotationSpeed;
        transform.rotation = Quaternion.Lerp(transform.rotation,
            Quaternion.Euler(0.0f, newRotation, 0.0f), mapSettings.agentRotationSpeed * Time.deltaTime);
        Vector3 dirToGo = transform.forward * inputDirection.magnitude;
        Vector3 vel = Vector3.ClampMagnitude(dirToGo * mapSettings.agentRunSpeed, mapSettings.agentRunSpeed);
        m_AgentRb.linearVelocity = Vector3.Lerp(m_AgentRb.linearVelocity, vel, 2 * Time.deltaTime);

        //Vector3 dirToGo = transform.forward * inputDirection.magnitude * mapSettings.agentRunSpeed * Time.deltaTime;
        //transform.Rotate(Vector3.up, moveRotation * turnSpeed * Time.deltaTime);
        //m_AgentRb.MovePosition(m_AgentRb.position + dirToGo);
    }

    public Vector3 GetRandomSpawnPos(GameObject toPos)
    {
        Bounds bounds = toPos.GetComponent<Collider>().bounds;
        int retryCount = 0;
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-groundBounds.extents.x * mapSettings.spawnAreaMarginMultiplier,
                groundBounds.extents.x * mapSettings.spawnAreaMarginMultiplier);

            var randomPosZ = Random.Range(-groundBounds.extents.z * mapSettings.spawnAreaMarginMultiplier,
                groundBounds.extents.z * mapSettings.spawnAreaMarginMultiplier);
            randomSpawnPos = ground.transform.position +
                new Vector3(randomPosX, groundBounds.extents.y + bounds.extents.y + 0.1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, bounds.extents) == false)
            {
                foundNewSpawnLocation = true;
            }
            retryCount++;
            if (retryCount > 1000)
            {
                Debug.Log("GetRandomSpawnPos too many times");
                break;
            }
        }
        return randomSpawnPos;
    }

}
