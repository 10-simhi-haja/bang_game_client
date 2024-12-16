using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEngine.AI;
using System;
using Unity.Multiplayer.Playmode;


public class Character : FSMController<CharacterState, CharacterFSM, CharacterDataSO>
{
    [SerializeField] public eCharacterType characterType;
    [SerializeField] private SpriteAnimation anim;
    [SerializeField] private Rigidbody2D rig;
    [SerializeField] private GameObject selectCircle;
    [SerializeField] private SpriteRenderer minimapIcon;
    [SerializeField] private GameObject range;
    [SerializeField] private GameObject targetMark;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private GameObject death;
    [SerializeField] private CircleCollider2D collider;
    [SerializeField] public GameObject stop;
    // gaugeBar
    [SerializeField] private Image gaugeBar;


    [SerializeField] private float speed;
    [SerializeField] private float baseSpeed = 3;
    [SerializeField] private float boostSpeed = 5;
    [SerializeField] private float debuffSpeed = 2;
    [SerializeField] private float maxGauge = 100;
    [SerializeField] private float gaugeDecayRate = 5;
    [SerializeField] private float debuffDuration = 2;
    // bullet
    [SerializeField] private GameObject bulletPrefab;  // 총알 프리팹
    [SerializeField] private Transform firePoint;      // 총알 발사 위치
    [SerializeField] private float bulletSpeed = 10f;  // 총알 속도

    private float currentGauge = 0;
    private bool isDebuffed = false;
    private float debuffTimer = 0;

    [HideInInspector] public UserInfo userInfo;

    public bool isPlayable { get => characterType == eCharacterType.playable; }
    public Vector2 dir;
    public float Speed { get => speed; }
    public bool isInside;

    private void Awake()
    {
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        if (characterType == eCharacterType.npc) minimapIcon.gameObject.SetActive(false);
    }

    public override async void Init(BaseDataSO data)
    {
        this.data = (CharacterDataSO)data;
        fsm = new CharacterFSM(CreateState<CharacterIdleState>().SetElement(anim, rig, this));
        minimapIcon.sprite = await ResourceManager.instance.LoadAsset<Sprite>(data.rcode, eAddressableType.Thumbnail);
    }

    public void SetCharacterType(eCharacterType characterType)
    {
        this.characterType = characterType;
        rig.mass = characterType == eCharacterType.playable ? 10 : 10000;
        agent.enabled = characterType != eCharacterType.playable;
        var tags = CurrentPlayer.ReadOnlyTags();
        if (tags.Length == 0)
        {
            tags = new string[1] { "player1" };
        }
        if (isPlayable)
        {
            if (tags[0].Equals("player1") &&
                (Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.OSXPlayer ||
                Application.platform == RuntimePlatform.WindowsEditor))
            {
                UIGame.instance.stick.gameObject.SetActive(false);
            }
            else
            {
                UIGame.instance.stick.OnHandleChanged += MoveCharacter;
            }
        }
    }

    public void SetMovePosition(Vector3 pos)
    {
        if (characterType == eCharacterType.playable) return;
        //rig.MovePosition(pos);
        agent.SetDestination(pos);
        var isLeft = agent.velocity.x < 0;
        isLeft = data.isLeft ? !isLeft : isLeft;
        if (agent.velocity.x != 0)
            anim.SetFlip(isLeft);
        if (agent.velocity == Vector3.zero)
            OnChangeState<CharacterIdleState>();
        else
            OnChangeState<CharacterWalkState>();
    }

    public void SetPosition(Vector3 pos)
    {
        agent.enabled = false;
        transform.position = pos;
        agent.enabled = true;
    }

    public void OnChangeState<T>() where T : CharacterState
    {
        if (states.ContainsKey(typeof(T).Name))
        {
            ChangeState<T>()?.SetElement(anim, rig, this);
        }
        else
        {
            CreateState<T>()?.SetElement(anim, rig, this);
        }
    }

    public bool IsState<T>()
    {
        return fsm.IsState<T>();
    }

    public void SetTargetMark()
    {
        targetMark.SetActive(true);
    }

    public void OnVisibleMinimapIcon(bool visible)
    {
        if (characterType == eCharacterType.non_playable)
            minimapIcon.gameObject.SetActive(visible && !isInside);
        else
            minimapIcon.gameObject.SetActive(false);
    }

    public void OnSelect()
    {
        selectCircle.SetActive(!selectCircle.activeInHierarchy);
    }

    public void OnVisibleRange()
    {
        range.SetActive(!range.activeInHierarchy);
    }

    float syncFrame = 0;
    public void MoveCharacter(Vector2 dir)
    {
        if (fsm.IsState<CharacterStopState>() || fsm.IsState<CharacterPrisonState>() || fsm.IsState<CharacterDeathState>()) return;
        this.dir = dir;
        var isLeft = dir.x < 0;
        isLeft = data.isLeft ? !isLeft : isLeft;
        if (dir.x != 0)
            anim.SetFlip(isLeft);
        if (dir == Vector2.zero) ChangeState<CharacterIdleState>().SetElement(anim, rig, this);
        else ChangeState<CharacterWalkState>().SetElement(anim, rig, this);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Map"))
        {
            if (characterType == eCharacterType.playable)
            {
                GameManager.instance.SetMapInside(true);
            }
            isInside = true;
            if (userInfo != null)
                OnVisibleMinimapIcon(Util.GetDistance(UserInfo.myInfo.index, userInfo.index, DataManager.instance.users.Count)
                    + userInfo.slotFar <= UserInfo.myInfo.slotRange && userInfo.id != UserInfo.myInfo.id); // ������ �Ÿ��� �ִ� ���� �����ܸ� ǥ��

        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Map"))
        {
            if (characterType == eCharacterType.playable)
            {
                GameManager.instance.SetMapInside(false);
            }
            isInside = false;
            if (userInfo != null)
                OnVisibleMinimapIcon(Util.GetDistance(UserInfo.myInfo.index, userInfo.index, DataManager.instance.users.Count)
                    + userInfo.slotFar <= UserInfo.myInfo.slotRange && userInfo.id != UserInfo.myInfo.id); // ������ �Ÿ��� �ִ� ���� �����ܸ� ǥ��
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent<Character>(out var character))
        {
            if (!SocketManager.instance.isConnected && character == GameManager.instance.userCharacter &&
                userInfo.handCards.Find(obj => obj.rcode == "CAD00001"))
            {
                GameManager.instance.SendSocketUseCard(character.userInfo, userInfo, "CAD00001");
            }
        }
    }

    private void UpdateGaugeUI()
    {
        if (gaugeBar != null)
        {
            gaugeBar.gameObject.SetActive(isPlayable);

            if (isPlayable)
            {
                gaugeBar.fillAmount = currentGauge / maxGauge;
            }
        }
    }

    private void Fire()
    {
        // 총알 프리팹 생성
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        
        // 총알 방향 설정
        Rigidbody2D bulletRigidbody = bullet.GetComponent<Rigidbody2D>();
        Vector2 fireDirection = dir.normalized; // 캐릭터가 바라보는 방향
        if (fireDirection == Vector2.zero)
        {
            // 정지 상태일 경우 기본 방향 지정 (오른쪽)
            fireDirection = Vector2.right;
        }
        bulletRigidbody.linearVelocity = fireDirection * bulletSpeed;

        // 총알 회전 설정 (발사 방향에 따라 회전)
        float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Update()
    {
        if (fsm != null)
            fsm.UpdateState();

        if (isPlayable)
        {
            if (Input.GetKeyDown(KeyCode.Space)) // Space 키 입력 감지
            {
                Fire();
            }
            if (isDebuffed)
            {
                gaugeBar.color = Color.red;
                debuffTimer += Time.deltaTime;
                if (debuffTimer >= debuffDuration)
                {
                    debuffTimer = 0f;
                    isDebuffed = false;
                    speed = baseSpeed;
                }
            }
            else
            {
                gaugeBar.color = Color.green;
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    speed = boostSpeed;
                    currentGauge += Time.deltaTime * 20f;
                    if (currentGauge >= maxGauge)
                    {
                        isDebuffed = true;
                        speed = debuffSpeed;
                    }
                }
                else
                {
                    speed = baseSpeed;
                    currentGauge -= Time.deltaTime * gaugeDecayRate;
                }

                currentGauge = Mathf.Clamp(currentGauge, 0, maxGauge);
            }
            UpdateGaugeUI();
        }
    }

    public async void SetDeath()
    {
        death.SetActive(true);
        collider.enabled = false;
        targetMark.SetActive(true);
        targetMark.GetComponent<SpriteRenderer>().sprite = await ResourceManager.instance.LoadAsset<Sprite>("Role_" + userInfo.roleType.ToString(), eAddressableType.Thumbnail);
        minimapIcon.gameObject.SetActive(false);
        ChangeState<CharacterDeathState>();
    }

    protected override T ChangeState<T>()
    {
        if (!IsState<CharacterDeathState>())
            return base.ChangeState<T>();
        else
            return null;
    }
}