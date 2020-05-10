using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyRegex
{

    /// <summary>
    /// Create abstract syntax tree for a regex.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract class Tree<T>
    {
        protected T Value { get; }
        public Tree(T value) { Value = value; }
    }

    class Leaf<T> : Tree<T>
    {
        public Leaf(T value) : base(value) { }
        public override string ToString() => $"Value:{Value}";
    }

    class UnaryNode<T> : Tree<T>
    {
        public Tree<T> Node { get; private set; }
        public UnaryNode (T value, Tree<T> node) : base(value)
        {
            Node = node;
        }
        public UnaryNode(T value) : base(value) { }
        public void SetNode(Tree<T> node) { Node = node; }
        public override string ToString() => $"Unary(Value:{Value}, Node:{Node.ToString()})";
    }

    class BinaryNode<T> : Tree<T>
    {
        public Tree<T> Left { get; private set; }
        public Tree<T> Right { get; private set; }
        public BinaryNode(T value, Tree<T> left, Tree<T> right) : base(value)
        {
            Left = left;
            Right = right;
        }
        public BinaryNode(T value) : base(value) { }
        public void SetLeft(Tree<T> left) { Left = left; }
        public void SetRight(Tree<T> right) { Right = right; }
        public override string ToString() => $"Binary(Value:{Value}, Left:{Left.ToString()}, Right:{Right.ToString()})";
    }

    class ListNode<T> : Tree<T>
    {
        List<Tree<T>> Trees { get; }
        public ListNode(T value, List<Tree<T>> trees) : base(value)
        {
            Trees = trees;
        }
        public ListNode(T value) : base(value) { Trees = new List<Tree<T>>(); }
        public void Add(Tree<T> tree)
        {
            Trees.Add(tree);
        }
        public void Insert(Tree<T> tree, int index)
        {
            Trees.Insert(index, tree);
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder($"List({Value}");
            for (int i = 0; i < Trees.Count - 1; i++)
            {
                sb.Append($"Item{i + 1}:{Trees[i].ToString()}, ");
            }
            sb.Append(Trees.Last().ToString() + ")");
            return sb.ToString();
        }
    }
}
