using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class OwnerlessDogController : MonoBehaviour
{
    [Header("移動設定")]
    public float moveSpeed;
    public float waitTime;
    public float stopProbability;
    
    [Header("移動可能エリア")]
    public PolygonCollider2D movementArea;
    
    private Rigidbody2D _rb;
    private Animator _animator;
    private Vector2 _randomTarget = Vector2.zero;
    private Vector2 _lastDirection = Vector2.zero;
    private bool _isWaiting;
    
    private static readonly int DirectionX = Animator.StringToHash("DirectionX");
    private static readonly int DirectionY = Animator.StringToHash("DirectionY");
    private static readonly int Speed = Animator.StringToHash("Speed");
    
    private void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        StartCoroutine(MoveAndStop());
    }
    
    private void Update()
    {
        if (!_isWaiting)
        {
            MoveTowardsRandomTarget();
        }
        if (_rb.velocity.sqrMagnitude > 0.1f)
        {
            _lastDirection = _rb.velocity.normalized;
        }
    }
    
    IEnumerator MoveAndStop()
    {
        while (true)
        {
            // ランダムな目的地点を設定
            SetRandomTargetWithinPolygon();
            
            // 一定距離移動する
            while (Vector2.Distance(transform.position, _randomTarget) > 0.1f)
            {
                yield return null; // 1フレーム待つ
            }
            
            // ランダムで停止するかどうか決める
            if (Random.value < stopProbability)
            {
                _isWaiting = true;
                _rb.velocity = Vector2.zero;
                _animator.SetFloat(Speed, 0);
                yield return new WaitForSeconds(waitTime);
                _isWaiting = false;
            }
        }
    }
    
    private void MoveTowardsRandomTarget()
    {
        // ランダムな目的地点に向かって移動
        Vector2 direction = (_randomTarget - (Vector2)transform.position).normalized;
        _rb.velocity = direction * moveSpeed;
        
        // アニメーションの更新
        SetAnimationParameters(direction);
    }
    
    private void SetAnimationParameters(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.1f)
        {
            // 移動中
            _animator.SetFloat(DirectionX, direction.x);
            _animator.SetFloat(DirectionY, direction.y);
        }
        else
        {
            // 移動していない
            _animator.SetFloat(DirectionX, _lastDirection.x);
            _animator.SetFloat(DirectionY, _lastDirection.y);
        }
        _animator.SetFloat(Speed, _rb.velocity.sqrMagnitude);
    }
    
    private void SetRandomTargetWithinPolygon()
    {
        bool validPoint = false;
        Vector2 randomPoint = Vector2.zero;
        Vector2 currentPosition = transform.position;
        
        while (!validPoint)
        {
            bool moveHorizontal = Random.value < 0.5f;
            
            if (moveHorizontal)
            {
                randomPoint = new Vector2(
                    Random.Range(movementArea.bounds.min.x, movementArea.bounds.max.x),
                    currentPosition.y
                    );
            }
            else
            {
                randomPoint = new Vector2(
                    currentPosition.x,
                    Random.Range(movementArea.bounds.min.y, movementArea.bounds.max.y)
                    );
            }
            
            // ポリゴン内にあるかどうかチェック
            if (movementArea.OverlapPoint(randomPoint))
            {
                validPoint = true;
            }
        }
        
        _randomTarget = randomPoint;
    }
}
