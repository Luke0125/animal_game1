using System.Collections.Generic;
using FedAndFound.Core;
using UnityEngine;

namespace FedAndFound.Game.Field
{
    /// <summary>탑뷰 필드의 칸 배치(§12). 한 스테이지 = 아래→위로 5줄:
    /// 0줄 출발점 → 1~3줄 갈림길(왼쪽 몹 / 오른쪽 이벤트) → 4줄 보스.
    /// 줄 사이는 사다리 모양 길로 연결되어 왼쪽↔오른쪽을 자유롭게 오갈 수 있다.
    /// 규칙(어느 노드에 들어갈 수 있는지)은 전부 Core의 RunState.NodeOptions()가 정한다 — 여기는 좌표만.</summary>
    public static class FieldLayout
    {
        public const int RowSpacing = 4, SideX = 3, BossRow = RunState.NodesBeforeBoss + 1;
        // 바닥 타일을 깔 범위(카메라가 가장자리로 가도 빈 배경이 안 보이도록 넉넉하게)
        public const int MinX = -12, MaxX = 12, MinY = -6, MaxY = BossRow * RowSpacing + 7;

        public static int RowY(int row) => row * RowSpacing;

        public static Vector2Int StartCell => new Vector2Int(0, 0);
        public static Vector2Int BossCell => new Vector2Int(0, RowY(BossRow));

        /// <summary>row(1~3)줄의 노드 칸. 몹은 왼쪽, 이벤트는 오른쪽.</summary>
        public static Vector2Int NodeCell(int row, NodeType n) =>
            n == NodeType.Boss ? BossCell : new Vector2Int(n == NodeType.Mob ? -SideX : SideX, RowY(row));

        /// <summary>칸 → (줄, 노드 종류). 노드 칸이 아니면 false.</summary>
        public static bool TryGetNode(Vector2Int c, out int row, out NodeType type)
        {
            row = 0; type = NodeType.Mob;
            if (c == BossCell) { row = BossRow; type = NodeType.Boss; return true; }
            if (c.y % RowSpacing != 0 || (c.x != -SideX && c.x != SideX)) return false;
            row = c.y / RowSpacing;
            if (row < 1 || row >= BossRow) return false;
            type = c.x < 0 ? NodeType.Mob : NodeType.Event;
            return true;
        }

        static HashSet<Vector2Int> _path;
        /// <summary>걸을 수 있는 길 칸 전체.</summary>
        public static HashSet<Vector2Int> PathCells => _path ??= BuildPath();

        static HashSet<Vector2Int> BuildPath()
        {
            var p = new HashSet<Vector2Int>();
            void V(int x, int y0, int y1) { for (int y = y0; y <= y1; y++) p.Add(new Vector2Int(x, y)); }
            void H(int y, int x0, int x1) { for (int x = x0; x <= x1; x++) p.Add(new Vector2Int(x, y)); }

            int half = RowSpacing / 2;
            V(0, RowY(0), RowY(0) + half);                              // 출발점 → 첫 갈림길
            for (int r = 0; r < BossRow; r++) H(RowY(r) + half, -SideX, SideX); // 줄 사이 가로 연결
            for (int r = 1; r < BossRow; r++)
            {
                V(-SideX, RowY(r) - half, RowY(r) + half);              // 왼쪽(몹) 세로 길
                V(SideX, RowY(r) - half, RowY(r) + half);               // 오른쪽(이벤트) 세로 길
            }
            V(0, RowY(BossRow) - half, RowY(BossRow));                  // 마지막 갈림길 → 보스
            return p;
        }

        /// <summary>길 위에서 from→to 최단 경로(BFS, from 제외). maxY보다 위 칸은 막힌 것으로 본다.</summary>
        public static List<Vector2Int> FindPath(Vector2Int from, Vector2Int to, int maxY)
        {
            var result = new List<Vector2Int>();
            if (from == to || !IsWalkable(to, maxY)) return result;
            var prev = new Dictionary<Vector2Int, Vector2Int> { [from] = from };
            var q = new Queue<Vector2Int>();
            q.Enqueue(from);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (c == to) break;
                foreach (var d in Dirs)
                {
                    var n = c + d;
                    if (prev.ContainsKey(n) || !IsWalkable(n, maxY)) continue;
                    prev[n] = c;
                    q.Enqueue(n);
                }
            }
            if (!prev.ContainsKey(to)) return result;
            for (var c = to; c != from; c = prev[c]) result.Add(c);
            result.Reverse();
            return result;
        }

        public static bool IsWalkable(Vector2Int c, int maxY) => c.y <= maxY && PathCells.Contains(c);

        public static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    }
}
