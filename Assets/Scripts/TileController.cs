using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TileController : MonoBehaviour
{
    private static readonly Color SelectedColor = new Color(0.5f, 0.5f, 0.5f);
    private static readonly Color NormalColor = Color.white;
    
    private static readonly float MoveDuration = 0.5f;
    private static readonly float destroyBigDuration = 0.1f;
    private static readonly float destroySmallDuration = 0.4f;
    
    private static readonly Vector2 sizeBig = Vector2.one * 1.2f;
    private static readonly Vector2 sizeSmall = Vector2.zero;
    private static readonly Vector2 sizeNormal = Vector2.one;

    private static readonly Vector2[] AdjacentDirection = 
        new Vector2[] {Vector2.up, Vector2.down, Vector2.left, Vector2.right};

    private static TileController previousSelected = null;

    private bool _isSelected = false;
    public bool IsProcessing { get; set; }
    public bool IsSwapping { get; set; }
    public bool IsDestroyed { get; set; }
    
    public int id;

    private BoardManager _board;
    private GameFlowManager _game;
    private SpriteRenderer _render;

    private void Awake()
    {
        _board = BoardManager.Instance;
        _game = GameFlowManager.Instance;
        _render = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        IsProcessing = false;
        IsSwapping = false;
        IsDestroyed = false;
    }

    private void OnMouseUp()
    {
        if (_render.sprite == null || _board.IsAnimating || _game.IsGameOver)
        {
            return;
        }
        
        SoundManager.Instance.PlayTap();

        if (_isSelected)
        {
            Deselect();
        }
        else
        {
            if (previousSelected == null)
            {
                Select();
            }
            else
            {
                if (GetAllNeighborTiles().Contains(previousSelected))
                {
                    TileController otherTile = previousSelected;
                    previousSelected.Deselect();
                    
                    SwapTile(otherTile, 
                        () =>
                        {
                            if (_board.GetAllMatches().Count > 0)
                            {
                                _board.Process();
                            }
                            else
                            {
                                SoundManager.Instance.PlayWrong();
                                SwapTile(otherTile);
                            }
                        });
                }
                else
                {
                    previousSelected.Deselect();
                    Select();
                }
                
            }
        }
    }

    #region Select Deselect
    
    private void Select()
    {
        _isSelected = true;
        _render.color = SelectedColor;
        previousSelected = this;
    }

    private void Deselect()
    {
        _isSelected = false;
        _render.color = NormalColor;
        previousSelected = null;
    }
    
    #endregion

    #region Neighbor Check

    private TileController GetNeighborTile(Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir, _render.size.x);

        if (hit)
        {
            return hit.collider.GetComponent<TileController>();
        }

        return null;
    }

    public List<TileController> GetAllNeighborTiles()
    {
        List<TileController> neighborTiles = new List<TileController>();

        foreach (var direction in AdjacentDirection)
        {
            neighborTiles.Add(GetNeighborTile(direction));
        }

        return neighborTiles;
    }

    #endregion

    #region Match Check

    private List<TileController> GetMatch(Vector2 castDir)
    {
        List<TileController> matchingTiles = new List<TileController>();
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir, _render.size.x);

        while (hit)
        {
            TileController otherTile = hit.collider.GetComponent<TileController>();
            if (otherTile.id != id || otherTile.IsDestroyed == true)
            {
                break;
            }
            
            matchingTiles.Add(otherTile);
            hit = Physics2D.Raycast(otherTile.transform.position, castDir, _render.size.x);
        }

        return matchingTiles;
    }

    private List<TileController> GetOneLineMatch(Vector2[] paths)
    {
        List<TileController> matchingTiles = new List<TileController>();

        for (int i = 0; i < paths.Length; i++)
        {
            matchingTiles.AddRange(GetMatch(paths[i]));
        }

        if (matchingTiles.Count >= 2)
        {
            return matchingTiles;
        }

        return null;
    }

    public List<TileController> GetAllMatch()
    {
        if (IsDestroyed)
        {
            return null;
        }
        
        List<TileController> matchingTiles = new List<TileController>();

        List<TileController> horizontalMatchingTiles = GetOneLineMatch(new Vector2[2] {Vector2.up, Vector2.down});
        List<TileController> verticalMatchingTiles = GetOneLineMatch(new Vector2[2] {Vector2.left, Vector2.right});

        if (horizontalMatchingTiles != null)
        {
            matchingTiles.AddRange(horizontalMatchingTiles);
        }

        if (verticalMatchingTiles != null)
        {
            matchingTiles.AddRange(verticalMatchingTiles);
        }

        return matchingTiles;
    }

    #endregion

    public void SwapTile(TileController otherTile, System.Action onCompleted = null)
    {
        StartCoroutine(_board.SwapTilePosition(this, otherTile, onCompleted));
    }

    public IEnumerator MoveTilePosition(Vector2 targetPos, System.Action onCompleted)
    {
        Vector2 startPos = transform.position;
        float time = 0.0f;

        yield return new WaitForEndOfFrame();

        while (time < MoveDuration)
        {
            transform.position = Vector2.Lerp(startPos, targetPos, time / MoveDuration);
            time += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        transform.position = targetPos;
        
        onCompleted?.Invoke();
    }

    public void ChangeId(int newId, int x, int y)
    {
        _render.sprite = _board.tileTypes[newId];
        this.id = newId;

        name = $"TILE_{id} ({x}, {y})";
    }

    public IEnumerator SetDestroyed(System.Action onCompleted)
    {
        IsDestroyed = true;
        id = -1;
        name = "TILE_NULL";

        Vector2 startSize = transform.localScale;
        float time = 0.0f;
        
        while (time < destroyBigDuration)
        {
            transform.localScale = Vector2.Lerp(startSize, sizeBig, time / destroyBigDuration);
            time += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        transform.localScale = sizeBig;
        
        startSize = transform.localScale;
        time = 0.0f;
        
        while (time < destroySmallDuration)
        {
            transform.localScale = Vector2.Lerp(startSize, sizeSmall, time / destroySmallDuration);
            time += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }
        
        transform.localScale = sizeSmall;

        _render.sprite = null;
        
        onCompleted?.Invoke();
    }

    public void GenerateRandomTile(int x, int y)
    {
        transform.localScale = sizeNormal;
        IsDestroyed = false;
        
        ChangeId(Random.Range(0, _board.tileTypes.Count), x, y);
    }
}
