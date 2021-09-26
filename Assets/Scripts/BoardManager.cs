using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoardManager : MonoBehaviour
{
    #region Singleton

    private static BoardManager _instance;

    public static BoardManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BoardManager>();

                if (_instance == null)
                {
                    Debug.LogError("BoardManager not found");
                }
            }

            return _instance;
        }
    }

    #endregion
    
    [Header("Board")]
    public Vector2Int size;
    public Vector2 offsetTile;
    public Vector2 offsetBoard;

    [Header("Tile")] 
    public List<Sprite> tileTypes = new List<Sprite>();
    public GameObject tilePrefab;

    private Vector2 _startPosition;
    private Vector2 _endPosition;
    private TileController[,] _tiles;
    private int _combo;
    
    public bool IsSwapping { get; set; }
    public bool IsProcessing { get; set; }

    public bool IsAnimating
    {
        get
        {
            return IsSwapping || IsProcessing;
        }
    }

    private void Start()
    {
        Vector2 tileSize = tilePrefab.GetComponent<SpriteRenderer>().size;
        CreateBoard(tileSize);
    }

    private void CreateBoard(Vector2 tileSize)
    {
        _tiles = new TileController[size.x, size.y];

        Vector2 totalSize = (tileSize + offsetTile) * (size - Vector2Int.one);

        _startPosition = (Vector2) transform.position - totalSize / 2 + offsetBoard;
        _endPosition = _startPosition + totalSize;

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                float xPos = _startPosition.x + (tileSize.x + offsetTile.x) * x;
                float yPos = _startPosition.y + (tileSize.y + offsetTile.y) * y;
                TileController newTile = Instantiate(tilePrefab, new Vector2(xPos, yPos), 
                    tilePrefab.transform.rotation, transform).GetComponent<TileController>();

                List<int> possibleId = GetStartingPossibleIdList(x, y);
                
                newTile.ChangeId(possibleId[Random.Range(0, possibleId.Count)], x, y);
                
                _tiles[x, y] = newTile;
            }
        }
    }

    private List<int> GetStartingPossibleIdList(int x, int y)
    {
        List<int> possibleId = new List<int>();

        for (int i = 0; i < tileTypes.Count; i++)
        {
            possibleId.Add(i);
        }

        if (x > 1 && _tiles[x-1, y].id == _tiles[x-2, y].id)
        {
            possibleId.Remove(_tiles[x - 1, y].id);
        }

        if (y > 1 && _tiles[x, y-1].id == _tiles[x, y-2].id)
        {
            possibleId.Remove(_tiles[x, y-1].id);
        }

        return possibleId;
    }

    #region Tile Swapping

    public IEnumerator SwapTilePosition(TileController tileA, TileController tileB, System.Action onCompleted)
    {
        IsSwapping = true;

        Vector2Int indexA = GetTileIndex(tileA);
        Vector2Int indexB = GetTileIndex(tileB);

        _tiles[indexA.x, indexA.y] = tileB;
        _tiles[indexB.x, indexB.y] = tileA;
        
        tileA.ChangeId(tileA.id, indexB.x, indexB.y);
        tileB.ChangeId(tileB.id, indexA.x, indexA.y);

        bool isRoutineACompleted = false;
        bool isRoutineBCompleted = false;

        StartCoroutine(
            tileA.MoveTilePosition(GetIndexPosition(indexB),
                () => { isRoutineACompleted = true; }
            ));
        StartCoroutine(
            tileB.MoveTilePosition(GetIndexPosition(indexA),
                () => { isRoutineBCompleted = true; }
            ));
        
        yield return new WaitUntil(() => isRoutineACompleted && isRoutineBCompleted);
        
        onCompleted?.Invoke();

        IsSwapping = false;
    }

    public Vector2Int GetTileIndex(TileController tile)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (tile == _tiles[x, y]) return new Vector2Int(x, y);
                {
                    
                }
            }
        }

        return new Vector2Int(-1, -1);
    }

    public Vector2 GetIndexPosition(Vector2Int index)
    {
        Vector2 tileSize = tilePrefab.GetComponent<SpriteRenderer>().size;
        
        return new Vector2(
            _startPosition.x + (tileSize.x + offsetTile.x) * index.x, 
            _startPosition.y + (tileSize.y + offsetTile.y) * index.y
            );
    }

    #endregion

    public List<TileController> GetAllMatches()
    {
        List<TileController> matchingTiles = new List<TileController>();
        
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                List<TileController> tileMatched = _tiles[x, y].GetAllMatch();

                if (tileMatched == null || tileMatched.Count == 0)
                {
                    continue;
                }

                foreach (TileController item in tileMatched)
                {
                    if (!matchingTiles.Contains(item))
                    {
                        matchingTiles.Add(item);
                    }
                }
            }
        }

        return matchingTiles;
    }

    #region Match Processing

    public void Process()
    {
        _combo = 0;
        IsProcessing = true;
        ProcessMatches();
    }

    public void ProcessMatches()
    {
        List<TileController> matchingTiles = GetAllMatches();

        if (matchingTiles == null || matchingTiles.Count == 0)
        {
            IsProcessing = false;
            return;
        }

        _combo++;
        ScoreManager.Instance.IncrementCurentScore(matchingTiles.Count, _combo);
        
        StartCoroutine(ClearMatches(matchingTiles, ProcessDrop));
    }

    private IEnumerator ClearMatches(List<TileController> matchingTiles, System.Action onCompleted)
    {
        List<bool> isCompleted = new List<bool>();

        for (int i = 0; i < matchingTiles.Count; i++)
        {
            isCompleted.Add(false);
        }
        
        for (int i = 0; i < matchingTiles.Count; i++)
        {
            int index = i;
            StartCoroutine(matchingTiles[i].SetDestroyed(
                    () => { isCompleted[index] = true; }));
        }

        yield return new WaitUntil((() => IsAllTrue(isCompleted)));
        
        onCompleted?.Invoke();
    }
    
    #endregion

    #region Match Drop

    private void ProcessDrop()
    {
        Dictionary<TileController, int> droppingTiles = GetAllDrop();

        StartCoroutine(DropTiles(droppingTiles, ProcessDestroyAndFill));
    }

    private Dictionary<TileController, int> GetAllDrop()
    {
        Dictionary<TileController, int> droppingTiles = new Dictionary<TileController, int>();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (_tiles[x,y].IsDestroyed)
                {
                    for (int i = y+1; i < size.y; i++)
                    {
                        if (_tiles[x,i].IsDestroyed)
                        {
                            continue;
                        }
                        
                        if (droppingTiles.ContainsKey(_tiles[x,i]))
                        {
                            droppingTiles[_tiles[x, i]]++;
                        }
                        else
                        {
                            droppingTiles.Add(_tiles[x,i], 1);
                        }
                    }
                }
            }
        }
        return droppingTiles;
    }

    private IEnumerator DropTiles(Dictionary<TileController, int> droppingTiles, System.Action onCompleted)
    {
        foreach (var pair in droppingTiles)
        {
            Vector2Int tileIndex = GetTileIndex(pair.Key);

            TileController temp = pair.Key;
            _tiles[tileIndex.x, tileIndex.y] = _tiles[tileIndex.x, tileIndex.y - pair.Value];
            _tiles[tileIndex.x, tileIndex.y - pair.Value] = temp;
            
            temp.ChangeId(temp.id, tileIndex.x, tileIndex.y - pair.Value);
        }

        yield return null;
        
        onCompleted?.Invoke();
    }

    #endregion

    #region Destroy and Fill

    private void ProcessDestroyAndFill()
    {
        List<TileController> destroyedTiles = GetAllDestroyed();
        StartCoroutine(DestroyAndFillTiles(destroyedTiles, ProcessReposition));
    }

    private List<TileController> GetAllDestroyed()
    {
        List<TileController> destroyedTiles = new List<TileController>();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (_tiles[x, y].IsDestroyed)
                {
                    destroyedTiles.Add(_tiles[x,y]);
                }
            }
        }

        return destroyedTiles;
    }

    private IEnumerator DestroyAndFillTiles(List<TileController> destroyedTiles, System.Action onCompleted)
    {
        List<int> highestIndex = new List<int>();

        for (int i = 0; i < size.x; i++)
        {
            highestIndex.Add(size.y - 1);
        }

        float sSpawnHeight = _endPosition.y + tilePrefab.GetComponent<SpriteRenderer>().size.y + offsetTile.y;

        foreach (var tile in destroyedTiles)
        {
            Vector2Int tileIndex = GetTileIndex(tile);
            Vector2Int targetIndex = new Vector2Int(tileIndex.x, highestIndex[tileIndex.x]);

            highestIndex[tileIndex.x]--;

            var transform1 = tile.transform;
            transform1.position = new Vector2(transform1.position.x, sSpawnHeight);
            tile.GenerateRandomTile(targetIndex.x, targetIndex.y);
        }

        yield return null;
        
        onCompleted?.Invoke();
    }

    #endregion

    #region Reposition

    private void ProcessReposition()
    {
        StartCoroutine(RepositionTiles(ProcessMatches));
    }

    private IEnumerator RepositionTiles(System.Action onCompleted)
    {
        List<bool> isCompleted = new List<bool>();

        int i = 0;

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2 targetPosition = GetIndexPosition(new Vector2Int(x, y));
                
                if ((Vector2)_tiles[x,y].transform.position == targetPosition)
                {
                    continue;
                }

                isCompleted.Add(false);

                int index = i;
                StartCoroutine(_tiles[x, y].MoveTilePosition(targetPosition, () => { isCompleted[index] = true; }));

                i++;
            }
        }
        yield return new WaitUntil((() => IsAllTrue(isCompleted)));
        
        onCompleted?.Invoke();
    }

    #endregion

    private bool IsAllTrue(List<bool> list)
    {
        foreach (var status in list)
        {
            if (!status) return false;
        }

        return true;
    }
}
