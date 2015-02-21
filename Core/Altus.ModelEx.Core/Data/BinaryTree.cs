#region Usings
using System;
using System.Collections.Generic;

#endregion

namespace Altus.Core.Data
{
    public enum FindMode
    {
        Exact,
        ExactOrAfter,
        ExactOrBefore,
        Before,
        After
    }

    public class BinaryTree<TKey, TValue> where TKey : IComparable<TKey>
    {
        private class Node
        {
            // node internal data
            internal int level;
            internal Node left;
            internal Node right;

            // user data
            internal TKey key;
            internal TValue value;

            // constuctor for the sentinel node
            internal Node()
            {
                this.level = 0;
                this.left = this;
                this.right = this;
            }

            // constuctor for regular nodes (that all start life as leaf nodes)
            internal Node(TKey key, TValue value, Node sentinel)
            {
                this.level = 1;
                this.left = sentinel;
                this.right = sentinel;
                this.key = key;
                this.value = value;
            }
        }

        Node root;
        Node sentinel;
        Node deleted;

        public BinaryTree()
        {
            root = sentinel = new Node();
            deleted = null;
            MinKey = root.key;
            MaxKey = root.key;
            MinValue = root.value;
            MaxValue = root.value;
        }

        private void Skew(ref Node node)
        {
            if (node.level == node.left.level)
            {
                // rotate right
                Node left = node.left;
                node.left = left.right;
                left.right = node;
                node = left;
            }
        }

        private void Split(ref Node node)
        {
            if (node.right.right.level == node.level)
            {
                // rotate left
                Node right = node.right;
                node.right = right.left;
                right.left = node;
                node = right;
                node.level++;
            }
        }

        private bool Insert(ref Node node, TKey key, TValue value)
        {
            if (node == sentinel)
            {
                node = new Node(key, value, sentinel);
                if (key.CompareTo(MinKey) <= 0)
                {
                    MinKey = key;
                    MinValue = value;
                }
                if (key.CompareTo(MaxKey) >= 0)
                {
                    MaxKey = key;
                    MaxValue = value;
                }
                Count++;
                return true;
            }

            int compare = key.CompareTo(node.key);
            if (compare < 0)
            {
                if (!Insert(ref node.left, key, value))
                {
                    return false;
                }
            }
            else if (compare > 0)
            {
                if (!Insert(ref node.right, key, value))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            Skew(ref node);
            Split(ref node);

            if (key.CompareTo(MinKey) < 0)
            {
                MinKey = key;
                MinValue = value;
            }
            else if (key.CompareTo(MaxKey) > 0)
            {
                MaxKey = key;
                MaxValue = value;
            }
            Count++;
            return true;
        }

        private bool Delete(ref Node node, TKey key)
        {
            if (node == sentinel)
            {
                return (deleted != null);
            }

            int compare = key.CompareTo(node.key);
            if (compare < 0)
            {
                if (!Delete(ref node.left, key))
                {
                    return false;
                }
            }
            else
            {
                if (compare == 0)
                {
                    deleted = node;
                }
                if (!Delete(ref node.right, key))
                {
                    return false;
                }
            }

            if (deleted != null)
            {
                deleted.key = node.key;
                deleted.value = node.value;
                deleted = null;
                node = node.right;
            }
            else if (node.left.level < node.level - 1
                    || node.right.level < node.level - 1)
            {
                --node.level;
                if (node.right.level > node.level)
                {
                    node.right.level = node.level;
                }
                Skew(ref node);
                Skew(ref node.right);
                Skew(ref node.right.right);
                Split(ref node);
                Split(ref node.right);
            }

            Count--;
            return true;
        }

        private Node Search(Node node, TKey key)
        {
            if (node == sentinel)
            {
                return null;
            }

            int compare = key.CompareTo(node.key);
            if (compare < 0)
            {
                return Search(node.left, key);
            }
            else if (compare > 0)
            {
                return Search(node.right, key);
            }
            else
            {
                return node;
            }
        }

        public TValue Find(TKey key, FindMode mode)
        {
            if (mode == FindMode.Exact)
            {
                Node node = Search(root, key);
                return node == null ? default(TValue) : node.value;
            }
            else if (mode == FindMode.ExactOrAfter)
            {
                Node node = SearchOnOrAfter(root, key);
                return node == null ? default(TValue) : node.value;
            }
            else
            {
                Node node = SearchOnOrBefore(root, key);
                return node == null ? default(TValue) : node.value;
            }
        }

        private Node SearchOnOrBefore(Node node, TKey key)
        {
            if (node == sentinel)
            {
                return null;
            }

            int compare = key.CompareTo(node.key);
            if (compare < 0)
            {
                return Search(node.left, key);
            }
            else if (compare > 0)
            {
                return Search(node.right, key);
            }
            else
            {
                return node;
            }
        }

        private Node SearchOnOrAfter(Node node, TKey key)
        {
            if (node == sentinel)
            {
                return null;
            }

            int compare = key.CompareTo(node.key);
            if (compare < 0)
            {
                return Search(node.left, key);
            }
            else if (compare > 0)
            {
                return Search(node.right, key);
            }
            else
            {
                return node;
            }
        }

        public bool Add(TKey key, TValue value)
        {
            return Insert(ref root, key, value);
        }

        public bool Remove(TKey key)
        {
            return Delete(ref root, key);
        }

        public TValue this[TKey key]
        {
            get
            {
                Node node = Search(root, key);
                return node == null ? default(TValue) : node.value;
            }
            set
            {
                Node node = Search(root, key);
                if (node == null)
                {
                    Add(key, value);
                }
                else
                {
                    node.value = value;
                }
            }
        }

        public int Count { get; private set; }
        public TKey MinKey { get; private set; }
        public TKey MaxKey { get; private set; }
        public TValue MinValue { get; private set; }
        public TValue MaxValue { get; private set; }
    }
}

