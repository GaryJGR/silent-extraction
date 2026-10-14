using UnityEngine;
using UnityEngine.AI;

namespace SilentExtraction
{
    public enum GuardState { Patrol, Suspicious, Chase, Investigate, Search, Return }

    /// <summary>Perception and navigation are separate: only confirmed sightings update lastSeen.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class GuardBrain : MonoBehaviour
    {
        public Transform[] patrolPoints;
        public PlayerMotor player;
        public LayerMask obstructionMask = 1 << 8;
        [Range(10, 180)] public float fieldOfView = 90;
        public float viewDistance = 10;
        public float detectionSeconds = 1.2f;
        public float searchSeconds = 5f;
        public Renderer visor;
        public GuardState State { get; private set; }
        public float Suspicion { get; private set; }
        public bool SeesPlayer { get; private set; }
        public Vector3 LastKnownPosition => lastSeen;
        public Color StateColor => State == GuardState.Chase ? new Color(1, .22f, .3f) :
            State == GuardState.Suspicious ? new Color(1, .56f, .13f) :
            State == GuardState.Search || State == GuardState.Investigate ? new Color(.43f, .69f, 1) :
            new Color(1, .79f, .32f);

        NavMeshAgent agent;
        Vector3 lastSeen;
        int patrolIndex;
        float lostSightTimer, searchTimer, patrolWait, repathTimer;
        MaterialPropertyBlock properties;
        public NavMeshAgent Agent => agent != null ? agent : GetComponent<NavMeshAgent>();

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            properties = new MaterialPropertyBlock();
        }
        void Start()
        {
            if (agent.isOnNavMesh && patrolPoints.Length > 0)
                agent.SetDestination(patrolPoints[0].position);
        }

        public bool CanSeePlayer()
        {
            if (player == null) return false;
            Vector3 difference = player.EyePosition - (transform.position + Vector3.up * 1.45f);
            float range = player.IsCrouching ? viewDistance * .75f : viewDistance;
            if (difference.sqrMagnitude > range * range) return false;
            Vector3 horizontal = new Vector3(difference.x, 0, difference.z);
            if (Vector3.Angle(transform.forward, horizontal) > fieldOfView * .5f) return false;
            return !Physics.Raycast(transform.position + Vector3.up * 1.45f, difference.normalized,
                difference.magnitude, obstructionMask, QueryTriggerInteraction.Ignore);
        }

        void Update()
        {
            var mission = MissionController.Instance;
            if (!agent.isOnNavMesh) return;
            if (mission == null || mission.Phase != MissionPhase.Playing)
            {
                agent.isStopped = true;
                return;
            }
            agent.isStopped = false;
            repathTimer -= Time.deltaTime;
            SeesPlayer = CanSeePlayer();
            if (SeesPlayer)
            {
                lastSeen = player.transform.position;
                lostSightTimer = 0;
                float rate = player.IsCrouching ? .65f : 1f;
                Suspicion = Mathf.Clamp01(Suspicion + Time.deltaTime * rate / detectionSeconds);
                if (Suspicion >= 1 && State != GuardState.Chase)
                {
                    SetState(GuardState.Chase);
                    mission.RegisterDetection();
                }
                else if (State != GuardState.Chase) SetState(GuardState.Suspicious);
            }
            else
            {
                lostSightTimer += Time.deltaTime;
                Suspicion = Mathf.Max(0, Suspicion - Time.deltaTime * .32f);
                if (State == GuardState.Suspicious && Suspicion <= 0) BeginSearch();
            }

            switch (State)
            {
                case GuardState.Patrol:
                case GuardState.Return:
                    agent.speed = 2.1f;
                    if (patrolPoints.Length == 0) break;
                    Navigate(patrolPoints[patrolIndex].position);
                    if (Arrived())
                    {
                        agent.isStopped = true;
                        patrolWait += Time.deltaTime;
                        if (patrolWait >= 1.1f)
                        {
                            patrolWait = 0;
                            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                            SetState(GuardState.Patrol);
                            repathTimer = 0;
                        }
                    }
                    break;
                case GuardState.Suspicious:
                    agent.isStopped = true;
                    Face(lastSeen);
                    break;
                case GuardState.Chase:
                    agent.speed = 4.7f;
                    Navigate(lastSeen);
                    if (!SeesPlayer && lostSightTimer > 1.4f && Arrived()) BeginSearch();
                    break;
                case GuardState.Investigate:
                    agent.speed = 2.7f;
                    Navigate(lastSeen);
                    if (Arrived()) BeginSearch();
                    break;
                case GuardState.Search:
                    agent.isStopped = true;
                    transform.Rotate(0, 70 * Time.deltaTime, 0);
                    searchTimer -= Time.deltaTime;
                    if (searchTimer <= 0)
                    {
                        SetState(GuardState.Return);
                        repathTimer = 0;
                    }
                    break;
            }
            // Walls also prevent captures: distance alone must not catch someone through cover.
            if (Vector3.Distance(transform.position, player.transform.position) < 1.15f &&
                !Physics.Linecast(transform.position + Vector3.up, player.transform.position + Vector3.up,
                    obstructionMask, QueryTriggerInteraction.Ignore)) mission.Caught();

            if (visor != null)
            {
                properties.SetColor("_BaseColor", StateColor);
                properties.SetColor("_EmissionColor", StateColor * 2);
                visor.SetPropertyBlock(properties);
            }
        }

        void Navigate(Vector3 destination)
        {
            if (repathTimer > 0) return;
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 2, agent.areaMask))
                agent.SetDestination(hit.position);
            repathTimer = .18f;
        }
        bool Arrived() => !agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathComplete && agent.remainingDistance <= .5f;
        void Face(Vector3 point)
        {
            Vector3 direction = point - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude > .01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 180 * Time.deltaTime);
        }
        void BeginSearch()
        {
            searchTimer = searchSeconds;
            SetState(GuardState.Search);
        }
        void SetState(GuardState next)
        {
            if (State == next) return;
            State = next;
            repathTimer = 0;
        }
        public void HearNoise(Vector3 position, float radius)
        {
            if (State == GuardState.Chase || State == GuardState.Suspicious || SeesPlayer) return;
            if (Vector3.Distance(transform.position, position) > radius) return;
            lastSeen = position;
            SetState(GuardState.Investigate);
        }
    }
}
