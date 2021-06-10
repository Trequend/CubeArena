using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

// BHV - Bounding Volume Hierarchy
// Simplified RTree
public class ArenaBHV
{
    private const int MaxEntries = 2;

    private class Node
    {
        private class Record
        {
            public AABB BoundingBox { get; set; }

            public object Data { get; set; }
        }

        private class NodeSplit
        {
            public IEnumerable<Record> Records { get; }

            public NodeSplit(Node node)
            {
                (int i, int j, int k) = FindBestRecordsPermutation(node._records);
                Node firstNode = new Node(node.Level, node._records[i]);
                Node secondNode = new Node(node.Level, node._records[j], node._records[k]);
                Records = new List<Record>()
                {
                    new Record()
                    {
                        BoundingBox = firstNode._boundingBox,
                        Data = firstNode
                    },
                    new Record()
                    {
                        BoundingBox = secondNode._boundingBox,
                        Data = secondNode
                    }
                };
            }

            private static (int i, int j, int k) FindBestRecordsPermutation(List<Record> records)
            {
                float minVolume = float.MaxValue;
                int index = -1;
                int count = MaxEntries + 1;
                for (int i = 0; i < count; i++)
                {
                    int j = (i + 1) % count;
                    int k = (i + 2) % count;
                    AABB firstBox = records[i].BoundingBox;
                    AABB secondBox = AABB.Merge(records[j].BoundingBox, records[k].BoundingBox);
                    float volume = firstBox.GetVolume() + secondBox.GetVolume();
                    if (volume < minVolume)
                    {
                        minVolume = volume;
                        index = i;
                    }
                }

                return (index, (index + 1) % count, (index + 2) % count);
            }
        }

        private readonly List<Record> _records = new List<Record>();
        private AABB _boundingBox = AABB.CreateEmpty();

        public int Level { get; private set; }

        public bool IsLeaf => Level == 0;

        public Node() { }

        private Node(int level, params Record[] records)
        {
            Level = level;
            _records.AddRange(records);
            foreach (Record record in records)
            {
                _boundingBox = AABB.Merge(_boundingBox, record.BoundingBox);
            }
        }

        public void Insert(Transform transform)
        {
            NodeSplit split = InsertRecursive(transform);
            if (split != null)
            {
                Level++;
                _records.Clear();
                _records.AddRange(split.Records);
                _boundingBox = AABB.CreateEmpty();
                foreach (Record record in split.Records)
                {
                    _boundingBox = AABB.Merge(_boundingBox, record.BoundingBox);
                }
            }
        }

        private NodeSplit InsertRecursive(Transform transform)
        {
            if (IsLeaf)
            {
                return InsertAsLeaf(transform);
            }
            else
            {
                return InsertAsInternal(transform);
            }
        }

        private NodeSplit InsertAsLeaf(Transform transform)
        {
            if (!_boundingBox.Contains(transform.position))
            {
                _boundingBox = AABB.Merge(_boundingBox, transform.position);
            }

            _records.Add(new Record()
            {
                BoundingBox = new AABB(transform.position),
                Data = transform
            });
            return _records.Count > MaxEntries ? new NodeSplit(this) : null;
        }

        private NodeSplit InsertAsInternal(Transform transform)
        {
            Node node;
            NodeSplit split;
            for (int i = 0; i < _records.Count; i++)
            {
                if (_records[i].BoundingBox.Contains(transform.position))
                {
                    node = (Node)_records[i].Data;
                    split = node.InsertRecursive(transform);
                    return ProcessSplit(i, split);
                }
            }

            float minVolumeChange = float.MaxValue;
            AABB minExtended = new AABB();
            int index = -1;
            for (int i = 0; i < _records.Count; i++)
            {
                AABB boundingBox = _records[i].BoundingBox;
                AABB extended = AABB.Merge(boundingBox, transform.position);
                float volumeChange = extended.GetVolume() - boundingBox.GetVolume();
                if (volumeChange < minVolumeChange)
                {
                    minVolumeChange = volumeChange;
                    minExtended = extended;
                    index = i;
                }
            }

            _boundingBox = AABB.Merge(_boundingBox, minExtended);
            Record record = _records[index];
            record.BoundingBox = minExtended;
            node = (Node)record.Data;
            split = node.InsertRecursive(transform);
            return ProcessSplit(index, split);
        }

        private NodeSplit ProcessSplit(int recordIndex, NodeSplit split)
        {
            if (split == null)
            {
                return null;
            }

            _records.RemoveAt(recordIndex);
            _records.AddRange(split.Records);
            foreach (Record record in split.Records)
            {
                _boundingBox = AABB.Merge(_boundingBox, record.BoundingBox);
            }

            return _records.Count > MaxEntries ? new NodeSplit(this) : null;
        }

        public void Foreach(Action<Transform> action)
        {
            if (IsLeaf)
            {
                foreach (Record record in _records)
                {
                    action((Transform)record.Data);
                }
            }
            else
            {
                foreach (Record record in _records)
                {
                    Node node = (Node)record.Data;
                    node.Foreach(action);
                }
            }
        }

        public void ForeachPair(Action<Transform, Transform> action)
        {
            Transform free = null;
            ForeachPair(action, ref free);
        }

        private void ForeachPair(Action<Transform, Transform> action, ref Transform free)
        {
            if (IsLeaf)
            {
                if (_records.Count < 2)
                {
                    Transform transform = (Transform)_records[0].Data;
                    if (free != null)
                    {
                        action(transform, free);
                        free = null;
                    }
                    else
                    {
                        free = transform;
                    }

                    return;
                }

                action((Transform)_records[0].Data, (Transform)_records[1].Data);
            }
            else
            {
                foreach (Record record in _records)
                {
                    Node node = (Node)record.Data;
                    node.ForeachPair(action, ref free);
                }
            }
        }

#if UNITY_EDITOR
        public void DebugDraw()
        {
            if (IsLeaf)
            {
                _boundingBox.DebugDraw(Color.yellow);
                foreach (Record record in _records)
                {
                    record.BoundingBox.DebugDraw(Color.red);
                }
            }
            else
            {
                _boundingBox.DebugDraw(Color.white);
                foreach (Record record in _records)
                {
                    Node node = (Node)record.Data;
                    node.DebugDraw();
                }
            }
        }
#endif
    }

    private Node _root = new Node();

    public int Count { get; private set; }

    public int Level => _root.Level;

    public void Add(Cube cube)
    {
        _root.Insert(cube.transform);
        Count++;
    }

    public void Clear()
    {
        _root = new Node();
        Count = 0;
    }

    public void Foreach(Action<Cube> action)
    {
        _root.Foreach(transform =>
        {
            Cube cube = transform.GetComponent<Cube>();
            action(cube);
        });
    }

    public void ForeachPair(Action<Cube, Cube> action)
    {
        _root.ForeachPair((firstTransform, secondTransform) =>
        {
            action(
                firstTransform.GetComponent<Cube>(),
                secondTransform.GetComponent<Cube>()
            );
        });
    }

#if UNITY_EDITOR
    public void DebugDraw()
    {
        _root.DebugDraw();
    }
#endif
}
