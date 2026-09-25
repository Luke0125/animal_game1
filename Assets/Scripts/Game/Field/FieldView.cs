using System;
using System.Collections;
using System.Collections.Generic;
using FedAndFound.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

namespace FedAndFound.Game.Field
{
    /// <summary>4단계 탑뷰 필드. Phase가 Map일 때만 보이는 월드 공간 타일맵(지형 4종)과 파티 말.
    /// 방향키/WASD로 길을 따라 걷거나 노드를 클릭하면 자동으로 걸어가고, 다음 줄의 열린 노드에 도착하면
    /// GameManager.EnterNode를 호출한다. 진입 가능 여부는 Core(RunState.NodeOptions)만 판단한다.
    /// uGUI 화면(ScreenRouter)과 달리 매 갱신마다 다시 만들지 않고, 스테이지가 바뀔 때만 타일을 새로 깐다.</summary>
    public sealed class FieldView : MonoBehaviour
    {
        public static FieldView Instance { get; private set; }

        const float StepTime = 0.12f, CameraSize = 6f, DefaultCameraSize = 5f;

        GameManager _gm;
        Camera _cam;
        Color _defaultBackdrop;
        GameObject _world;
        Tilemap _ground, _deco;
        Transform _player;
        SpriteRenderer _playerBody;
        TextMesh _playerLabel;
        Transform _clouds;
        readonly Dictionary<Vector2Int, NodeMarker> _markers = new Dictionary<Vector2Int, NodeMarker>();
        readonly HashSet<Vector2Int> _visited = new HashSet<Vector2Int>();

        RunState _builtRun;
        int _builtStage = -1;
        Vector2Int _cell;
        Coroutine _walk;

        /// <summary>자동/수동 이동 중이면 true. 이 동안은 새 이동·노드 진입 요청을 무시한다.</summary>
        public bool Walking => _walk != null;
        bool Active => _gm != null && _gm.Run != null && _gm.Run.Phase == RunPhase.Map;
        int NextRow => _gm.Run.NodesMoved + 1; // EnterNode 후 NodesMoved가 1 오르므로 다음 줄 = NodesMoved + 1

        sealed class NodeMarker
        {
            public int Row; public NodeType Type;
            public SpriteRenderer Ring, Glow; public TextMesh Label;
        }

        public static FieldView Create(GameManager gm)
        {
            var go = new GameObject("FieldView");
            var fv = go.AddComponent<FieldView>();
            fv.Init(gm);
            return fv;
        }

        void Init(GameManager gm)
        {
            Instance = this;
            _gm = gm;
            _cam = Camera.main;
            if (_cam != null)
            {
                // Main.unity의 기본 카메라는 원근+스카이박스라 탑뷰 타일맵이 기울어져 보인다 → 직교·단색 배경으로 고정.
                // (다른 화면은 전부 Overlay Canvas라 영향 없음, 배경만 스카이박스 대신 Theme 배경색이 된다)
                _cam.orthographic = true;
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.backgroundColor = UI.Theme.Background;
                _cam.transform.rotation = Quaternion.identity;
                _defaultBackdrop = _cam.backgroundColor;
            }

            _world = new GameObject("FieldWorld");
            _world.transform.SetParent(transform, false);
            var grid = new GameObject("Grid").AddComponent<Grid>();
            grid.transform.SetParent(_world.transform, false);
            // 셀 (x,y)의 중심이 월드 좌표 (x,y)에 오도록 반 칸 당긴다
            grid.transform.localPosition = new Vector3(-0.5f, -0.5f, 0);
            _ground = NewTilemap(grid.transform, "Ground", 0);
            _deco = NewTilemap(grid.transform, "Deco", 1);

            // 파티 말 = 파티 대표 동물 그림 (맵 아트 2차)
            var playerGo = new GameObject("Party");
            playerGo.transform.SetParent(_world.transform, false);
            _player = playerGo.transform;
            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(_player, false);
            shadow.sprite = BattleView.BattleStageView.GlowSprite; shadow.sortingOrder = 19;
            shadow.color = new Color(0, 0, 0, 0.45f);
            shadow.transform.localPosition = new Vector3(0, -0.35f, 0);
            shadow.transform.localScale = new Vector3(1.2f, 0.3f, 1);
            _playerBody = new GameObject("Body").AddComponent<SpriteRenderer>();
            _playerBody.transform.SetParent(_player, false);
            _playerBody.sortingOrder = 20;
            _playerBody.transform.localScale = Vector3.one * 1.3f;
            _playerLabel = NewText(_player, "", new Vector3(0, 1.35f, 0), 22, new Color(1f, 1f, 1f), 21);

            gm.OnChanged += Sync;
            Sync();
        }

        void OnDestroy()
        {
            if (_gm != null) _gm.OnChanged -= Sync;
            if (Instance == this) Instance = null;
        }

        static Tilemap NewTilemap(Transform parent, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = order;
            return tm;
        }

        static TextMesh NewText(Transform parent, string text, Vector3 localPos, int fontSize, Color color, int order)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = UI.Theme.FontBold;
            tm.text = text;
            tm.fontSize = fontSize * 4;      // 크게 래스터라이즈하고 characterSize로 줄여야 글자가 흐리지 않다
            tm.characterSize = 0.025f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = tm.font.material;
            mr.sortingOrder = order;
            return tm;
        }

        // ================= 상태 동기화 =================

        void Sync()
        {
            bool active = Active;
            _world.SetActive(active);
            if (_cam != null)
            {
                _cam.orthographicSize = active ? CameraSize : DefaultCameraSize;
                if (!active) { _cam.backgroundColor = _defaultBackdrop; _cam.transform.position = new Vector3(0, 0, -10); }
            }
            if (!active) { StopWalk(); return; }

            var run = _gm.Run;
            if (run != _builtRun || run.Stage != _builtStage) BuildStage(run);
            RefreshMarkers();

            var leader = run.Party.Find(u => !u.Fainted) ?? (run.Party.Count > 0 ? run.Party[0] : null);
            _playerLabel.text = leader != null ? leader.Name + (run.Party.Count > 1 ? $" 외 {run.Party.Count - 1}" : "") : "";
            if (leader != null) _playerBody.sprite = BattleView.AnimalArt.Get(leader.Species.Id);
        }

        void BuildStage(RunState run)
        {
            StopWalk();
            _builtRun = run; _builtStage = run.Stage;
            _visited.Clear();
            _cell = FieldLayout.StartCell;
            _visited.Add(_cell);
            _player.localPosition = CellPos(_cell);
            SnapCamera();

            var tiles = TerrainTiles.Get(run.Terrain);
            if (_cam != null) _cam.backgroundColor = tiles.Sky; // 섬 바깥은 하늘 (레퍼런스: 하늘에 떠 있는 섬)
            _ground.ClearAllTiles(); _deco.ClearAllTiles();
            BuildClouds(run.Stage);

            // 장식 배치는 게임 난수(run.Rng)를 건드리지 않도록 별도 시드를 쓴다 — 전투 결과가 화면 때문에 바뀌면 안 된다
            var rng = new System.Random(run.Stage * 7919 + (int)run.Terrain * 131 + 7);
            var path = FieldLayout.PathCells;
            for (int y = FieldLayout.MinY; y <= FieldLayout.MaxY; y++)
                for (int x = FieldLayout.MinX; x <= FieldLayout.MaxX; x++)
                {
                    var c = new Vector2Int(x, y);
                    var pos = new Vector3Int(x, y, 0);
                    if (!InIsland(x, y, run.Stage)) continue;
                    if (path.Contains(c)) { _ground.SetTile(pos, tiles.Path); continue; }
                    bool nearPath = NearPath(c, path);
                    if (tiles.Water != null && !nearPath && rng.NextDouble() < 0.14)
                        _ground.SetTile(pos, tiles.Water);
                    else
                        _ground.SetTile(pos, tiles.Ground[rng.Next(tiles.Ground.Length)]);
                    if (!nearPath && rng.NextDouble() < 0.16)
                        _deco.SetTile(pos, tiles.Deco[rng.Next(tiles.Deco.Length)]);
                }

            // 섬 아래쪽 절벽: 열마다 가장 낮은 땅 밑으로 3칸
            for (int x = FieldLayout.MinX; x <= FieldLayout.MaxX; x++)
            {
                int low = int.MaxValue;
                for (int y = FieldLayout.MinY; y <= FieldLayout.MaxY; y++) if (InIsland(x, y, run.Stage)) { low = y; break; }
                if (low == int.MaxValue) continue;
                _ground.SetTile(new Vector3Int(x, low - 1, 0), tiles.Cliff);
                _ground.SetTile(new Vector3Int(x, low - 2, 0), tiles.CliffDark);
                if ((x & 1) == 0) _ground.SetTile(new Vector3Int(x, low - 3, 0), tiles.CliffDark); // 들쭉날쭉한 밑동
            }

            foreach (var m in _markers.Values) Destroy(m.Ring.gameObject);
            _markers.Clear();
            for (int row = 1; row < FieldLayout.BossRow; row++)
            {
                AddMarker(row, NodeType.Mob, tiles);
                AddMarker(row, NodeType.Event, tiles);
            }
            AddMarker(FieldLayout.BossRow, NodeType.Boss, tiles);
        }

        /// <summary>떠 있는 섬의 모양: 폭이 줄마다 물결치고 위·아래 끝이 둥글게 좁아진다. 길(x ±3)은 항상 안쪽.</summary>
        static bool InIsland(int x, int y, int stage)
        {
            const int bottom = -3, top = FieldLayout.BossRow * FieldLayout.RowSpacing + 3;
            if (y < bottom || y > top) return false;
            float p = stage * 1.7f;
            float half = 7.5f + 1.2f * Mathf.Sin(y * 0.55f + p) + 0.8f * Mathf.Sin(y * 1.3f + p * 2f);
            if (y < bottom + 2) half -= (bottom + 2 - y) * 1.8f;
            if (y > top - 2) half -= (y - (top - 2)) * 1.8f;
            return Mathf.Abs(x + 0.4f * Mathf.Sin(y * 0.4f + p)) <= Mathf.Max(4.2f, half);
        }

        void BuildClouds(int stage)
        {
            if (_clouds != null) Destroy(_clouds.gameObject);
            _clouds = new GameObject("Clouds").transform;
            _clouds.SetParent(_world.transform, false);
            var rng = new System.Random(stage * 31 + 5);
            for (int i = 0; i < 16; i++)
            {
                var cloud = new GameObject("Cloud").transform;
                cloud.SetParent(_clouds, false);
                cloud.localPosition = new Vector3(-16 + (float)rng.NextDouble() * 32, -9 + i * 2.3f, 0);
                for (int k = 0; k < 4; k++) // 동그라미 4개를 겹쳐 뭉게구름
                {
                    var puff = new GameObject("Puff").AddComponent<SpriteRenderer>();
                    puff.transform.SetParent(cloud, false);
                    puff.sprite = BattleView.BattleStageView.CircleSprite; puff.sortingOrder = -20;
                    puff.color = new Color(1, 1, 1, 0.85f - k * 0.05f);
                    float s = 1.2f + (float)rng.NextDouble() * 1.2f;
                    puff.transform.localPosition = new Vector3(k * 0.9f - 1.3f, (k % 2) * 0.35f, 0);
                    puff.transform.localScale = new Vector3(s, s * 0.7f, 1);
                }
            }
        }

        static bool NearPath(Vector2Int c, HashSet<Vector2Int> path)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if (path.Contains(new Vector2Int(c.x + dx, c.y + dy))) return true;
            return false;
        }

        void AddMarker(int row, NodeType type, TerrainTiles tiles)
        {
            var cell = FieldLayout.NodeCell(row, type);
            var go = new GameObject($"Node_{row}_{type}");
            go.transform.SetParent(_world.transform, false);
            go.transform.localPosition = CellPos(cell);
            // 레퍼런스처럼 둥근 돌 발판. 이벤트는 어두운 돌 + "?", 보스는 붉은 돌 + 붉은 기운 + "보스" 깃발
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = TerrainTiles.StoneDisc();
            sr.sortingOrder = 10;
            float scale = type == NodeType.Boss ? 1.6f : 1.15f;
            go.transform.localScale = Vector3.one * scale;
            var glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
            glow.transform.SetParent(go.transform, false);
            glow.sprite = BattleView.BattleStageView.GlowSprite; glow.sortingOrder = 9;
            glow.transform.localScale = new Vector3(2.2f, 1.6f, 1);
            var label = NewText(go.transform, "", new Vector3(0, 0.08f, 0), type == NodeType.Event ? 26 : 16, Color.white, 11);
            label.transform.localScale = Vector3.one / scale;
            if (type == NodeType.Boss)
            {
                var banner = new GameObject("Banner").AddComponent<SpriteRenderer>();
                banner.transform.SetParent(go.transform, false);
                banner.sprite = UI.UISkin.Rounded; banner.drawMode = SpriteDrawMode.Sliced;
                banner.size = new Vector2(1.3f, 0.42f);
                banner.color = new Color(0.72f, 0.12f, 0.12f);
                banner.sortingOrder = 12;
                banner.transform.localPosition = new Vector3(0, 0.9f, 0);
                var bt = NewText(banner.transform, "보스", Vector3.zero, 20, Color.white, 13);
                bt.transform.localScale = Vector3.one / scale;
            }
            _markers[cell] = new NodeMarker { Row = row, Type = type, Ring = sr, Glow = glow, Label = label };
        }

        void RefreshMarkers()
        {
            var options = _gm.Run.NodeOptions();
            foreach (var kv in _markers)
            {
                var m = kv.Value;
                bool open = m.Row == NextRow && options.Contains(m.Type);
                bool visited = _visited.Contains(kv.Key);
                Color baseC = m.Type switch
                {
                    NodeType.Mob => new Color(1f, 0.96f, 0.88f),      // 밝은 돌
                    NodeType.Event => new Color(0.3f, 0.27f, 0.25f),   // 어두운 돌 + ?
                    _ => new Color(0.75f, 0.25f, 0.22f),               // 붉은 돌
                };
                // 다음 줄의 열린 노드 = 밝게 + 금빛 기운(맥동은 Update), 지나온 노드 = 흐리게, 닫힌 노드 = 어둡게
                m.Ring.color = open ? baseC : visited ? Color.Lerp(baseC, new Color(0.6f, 0.6f, 0.6f), 0.5f) : Color.Lerp(baseC, new Color(0.25f, 0.25f, 0.28f), 0.6f);
                m.Glow.color = m.Type == NodeType.Boss ? new Color(0.9f, 0.1f, 0.1f, open ? 0.7f : 0.3f)
                             : open ? new Color(1f, 0.85f, 0.3f, 0.6f) : new Color(0, 0, 0, 0);
                m.Label.text = m.Type switch { NodeType.Mob => visited ? "" : "몹", NodeType.Event => "?", _ => "" }; // 나눔고딕에 ✓ 같은 기호가 없어 글자만 쓴다
                m.Label.color = m.Type == NodeType.Mob ? new Color(0.4f, 0.3f, 0.2f) : Color.white;
            }
        }

        static Vector3 CellPos(Vector2Int c) => new Vector3(c.x, c.y, 0);

        // ================= 입력·이동 =================

        void Update()
        {
            if (!Active) return;

            // 열린 노드는 살짝 맥동시켜 "여기로 가라"는 신호를 준다
            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 5f);
            var options = _gm.Run.NodeOptions();
            if (_clouds != null)
                foreach (Transform c in _clouds)
                {
                    var p = c.localPosition; p.x += Time.deltaTime * 0.25f; if (p.x > 17f) p.x = -17f; c.localPosition = p; // 구름이 흘러간다
                }
            // 파티 말: 걸을 때는 통통, 서 있을 때는 숨쉬기
            _playerBody.transform.localPosition = new Vector3(0, -0.4f + (Walking ? Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.12f : Mathf.Sin(Time.time * 2f) * 0.03f), 0);
            foreach (var m in _markers.Values)
            {
                float baseScale = m.Type == NodeType.Boss ? 1.6f : 1.15f;
                bool open = m.Row == NextRow && options.Contains(m.Type);
                m.Ring.transform.localScale = Vector3.one * baseScale * (open ? pulse : 1f);
            }

            if (Walking || UI.PauseMenu.IsOpen) return; // ESC 메뉴가 열려 있으면 이동 입력 무시

            var dir = ReadDirection();
            if (dir != Vector2Int.zero)
            {
                var next = _cell + dir;
                if (FieldLayout.IsWalkable(next, MaxWalkY())) _walk = StartCoroutine(WalkPath(new List<Vector2Int> { next }));
                return;
            }

            if (Input.GetMouseButtonDown(0) && _cam != null)
            {
                // uGUI(상단/하단 HUD) 위를 누른 건 필드 클릭으로 치지 않는다
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
                var w = _cam.ScreenToWorldPoint(Input.mousePosition);
                var target = new Vector2Int(Mathf.RoundToInt(w.x), Mathf.RoundToInt(w.y));
                WalkTo(target);
            }
        }

        void LateUpdate()
        {
            if (!Active || _cam == null) return;
            var target = CameraTarget();
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
        }

        Vector3 CameraTarget() =>
            // 하단 HUD가 상단보다 얇으므로 말이 화면 중앙보다 약간 아래 보이도록 카메라를 위로 조금 올린다
            new Vector3(0, Mathf.Clamp(_player.localPosition.y + 1.2f, 1.5f, FieldLayout.BossCell.y - 0.5f), -10);

        void SnapCamera() { if (_cam != null) _cam.transform.position = CameraTarget(); }

        static Vector2Int ReadDirection()
        {
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) return Vector2Int.up;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) return Vector2Int.down;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) return Vector2Int.left;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) return Vector2Int.right;
            return Vector2Int.zero;
        }

        /// <summary>다음 줄보다 위로는 못 간다 — 노드를 건너뛰고 보스로 직행하는 걸 막는다(§12 보스 전 3회 이동).</summary>
        int MaxWalkY() => FieldLayout.RowY(Mathf.Min(NextRow, FieldLayout.BossRow));

        /// <summary>하단 버튼용: 해당 노드까지 걸어간 뒤 진입. 걸어갈 수 없으면 false(호출자가 바로 EnterNode).</summary>
        public bool WalkToNode(NodeType type)
        {
            if (!Active || Walking || !_gm.Run.NodeOptions().Contains(type)) return false;
            var target = FieldLayout.NodeCell(Mathf.Min(NextRow, FieldLayout.BossRow), type);
            if (target == _cell) { TryEnter(target); return true; }
            return WalkTo(target);
        }

        bool WalkTo(Vector2Int target)
        {
            if (Walking) return false;
            var path = FieldLayout.FindPath(_cell, target, MaxWalkY());
            if (path.Count == 0) return false;
            _walk = StartCoroutine(WalkPath(path));
            return true;
        }

        IEnumerator WalkPath(List<Vector2Int> path)
        {
            foreach (var c in path)
            {
                var from = _player.localPosition;
                var to = CellPos(c);
                if (Mathf.Abs(to.x - from.x) > 0.01f) _playerBody.flipX = to.x < from.x; // 가는 방향을 본다
                for (float t = 0; t < StepTime; t += Time.deltaTime)
                {
                    _player.localPosition = Vector3.Lerp(from, to, t / StepTime);
                    yield return null;
                }
                _player.localPosition = to;
                _cell = c;
                Audio.SoundManager.Play(Audio.Sfx.Step, 0.5f);
                // 경로 중간에 열린 노드를 밟아도 거기서 진입한다(키보드 이동과 동일한 규칙)
                if (IsOpenNode(c)) { _walk = null; TryEnter(c); yield break; }
            }
            _walk = null;
        }

        void StopWalk()
        {
            if (_walk != null) StopCoroutine(_walk);
            _walk = null;
            if (_player != null) _player.localPosition = CellPos(_cell);
        }

        bool IsOpenNode(Vector2Int c) =>
            FieldLayout.TryGetNode(c, out int row, out var type) && row == NextRow && _gm.Run.NodeOptions().Contains(type);

        void TryEnter(Vector2Int c)
        {
            if (!FieldLayout.TryGetNode(c, out _, out var type) || !IsOpenNode(c)) return;
            _visited.Add(c);
            Audio.SoundManager.Play(Audio.Sfx.Node);
            try { _gm.EnterNode(type); }
            catch (Exception e) { Debug.LogError($"[FieldView] 노드 진입 실패: {e.Message}"); }
        }
    }
}
