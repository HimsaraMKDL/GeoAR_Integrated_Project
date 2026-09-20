using System.Collections.Generic;
using UnityEngine;

public static class CubeNetValidator
{
    private struct V3
    {
        public int x, y, z;

        public V3(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static V3 operator -(V3 a)
        {
            return new V3(-a.x, -a.y, -a.z);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is V3)) return false;
            V3 o = (V3)obj;
            return x == o.x && y == o.y && z == o.z;
        }

        public override int GetHashCode()
        {
            return x * 73856093 ^ y * 19349663 ^ z * 83492791;
        }
    }

    private struct Basis
    {
        public V3 right;
        public V3 up;
        public V3 normal;

        public Basis(V3 right, V3 up, V3 normal)
        {
            this.right = right;
            this.up = up;
            this.normal = normal;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Basis)) return false;
            Basis o = (Basis)obj;
            return right.Equals(o.right) && up.Equals(o.up) && normal.Equals(o.normal);
        }

        public override int GetHashCode()
        {
            return right.GetHashCode() ^ up.GetHashCode() ^ normal.GetHashCode();
        }
    }

    public static bool ValidateCubeNet(HashSet<Vector2Int> cells, out string message)
    {
        if (cells == null || cells.Count == 0)
        {
            message = "No faces detected.";
            return false;
        }

        if (cells.Count != 6)
        {
            message = "Invalid: Cube net must have exactly 6 square faces. Current faces: " + cells.Count;
            return false;
        }

        if (!IsConnected(cells))
        {
            message = "Invalid: All 6 faces must be connected.";
            return false;
        }

        bool foldValid = ValidateByFolding(cells);

        if (!foldValid)
        {
            message = "Invalid: This is not one of the 11 valid cube nets.";
            return false;
        }

        message = "Valid cube net! This matches one of the 11 possible cube net patterns.";
        return true;
    }

    private static bool IsConnected(HashSet<Vector2Int> cells)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        Vector2Int start = default;
        foreach (Vector2Int c in cells)
        {
            start = c;
            break;
        }

        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int[] dirs =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int d in dirs)
            {
                Vector2Int next = current + d;

                if (cells.Contains(next) && !visited.Contains(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }

        return visited.Count == cells.Count;
    }

    private static bool ValidateByFolding(HashSet<Vector2Int> cells)
    {
        Dictionary<Vector2Int, Basis> assigned = new Dictionary<Vector2Int, Basis>();
        HashSet<V3> usedNormals = new HashSet<V3>();

        Vector2Int start = default;
        foreach (Vector2Int c in cells)
        {
            start = c;
            break;
        }

        Basis startBasis = new Basis(
            new V3(1, 0, 0),
            new V3(0, 1, 0),
            new V3(0, 0, 1)
        );

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        assigned[start] = startBasis;
        usedNormals.Add(startBasis.normal);

        Vector2Int[] dirs =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            Basis currentBasis = assigned[current];

            foreach (Vector2Int dir in dirs)
            {
                Vector2Int next = current + dir;

                if (!cells.Contains(next))
                    continue;

                Basis nextBasis = FoldToNeighbor(currentBasis, dir);

                if (assigned.ContainsKey(next))
                {
                    if (!assigned[next].Equals(nextBasis))
                        return false;
                }
                else
                {
                    if (usedNormals.Contains(nextBasis.normal))
                        return false;

                    assigned[next] = nextBasis;
                    usedNormals.Add(nextBasis.normal);
                    queue.Enqueue(next);
                }
            }
        }

        return assigned.Count == 6 && usedNormals.Count == 6;
    }

    private static Basis FoldToNeighbor(Basis b, Vector2Int dir)
    {
        if (dir == Vector2Int.right)
        {
            return new Basis(-b.normal, b.up, b.right);
        }

        if (dir == Vector2Int.left)
        {
            return new Basis(b.normal, b.up, -b.right);
        }

        if (dir == Vector2Int.up)
        {
            return new Basis(b.right, -b.normal, b.up);
        }

        if (dir == Vector2Int.down)
        {
            return new Basis(b.right, b.normal, -b.up);
        }

        return b;
    }
}