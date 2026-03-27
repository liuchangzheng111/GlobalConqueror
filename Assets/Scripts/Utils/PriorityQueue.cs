using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// 泛型优先队列（标准实现，基于最小堆）
/// </summary>
/// <typeparam name="TElement">队列元素类型</typeparam>
/// <typeparam name="TPriority">优先级类型（需实现IComparable接口，如int/float/double）</typeparam>
[DebuggerDisplay("Count = {Count}")]
public class PriorityQueue<TElement, TPriority> : IEnumerable<TElement>
    where TPriority : IComparable<TPriority>
{
    #region 私有字段
    // 存储堆元素（元素+优先级的键值对）
    private readonly List<(TElement Element, TPriority Priority)> _heap;
    // 自定义比较器（默认最小堆，可改为最大堆）
    private readonly IComparer<TPriority> _comparer;
    // 元素查找字典（用于快速判断元素是否存在，可选优化）
    private readonly Dictionary<TElement, int> _elementIndices;
    #endregion

    #region 公共属性
    /// <summary>
    /// 队列中元素的数量
    /// </summary>
    public int Count => _heap.Count;

    /// <summary>
    /// 队列是否为空
    /// </summary>
    public bool IsEmpty => _heap.Count == 0;
    #endregion

    #region 构造函数
    /// <summary>
    /// 初始化优先队列（默认最小堆）
    /// </summary>
    public PriorityQueue() : this(Comparer<TPriority>.Default) { }

    /// <summary>
    /// 初始化优先队列（自定义比较器）
    /// </summary>
    /// <param name="comparer">优先级比较器（默认Comparer&lt;TPriority&gt;.Default为最小堆）</param>
    public PriorityQueue(IComparer<TPriority> comparer)
    {
        _heap = new List<(TElement, TPriority)>();
        _comparer = comparer ?? Comparer<TPriority>.Default;
        _elementIndices = new Dictionary<TElement, int>();
    }

    /// <summary>
    /// 初始化优先队列（指定初始容量，减少扩容开销）
    /// </summary>
    /// <param name="initialCapacity">初始容量</param>
    /// <param name="comparer">优先级比较器</param>
    public PriorityQueue(int initialCapacity, IComparer<TPriority> comparer = null)
    {
        _heap = new List<(TElement, TPriority)>(initialCapacity);
        _comparer = comparer ?? Comparer<TPriority>.Default;
        _elementIndices = new Dictionary<TElement, int>(initialCapacity);
    }
    #endregion

    #region 核心方法
    /// <summary>
    /// 入队：添加元素并按优先级排序
    /// </summary>
    /// <param name="element">待添加的元素</param>
    /// <param name="priority">元素的优先级（值越小优先级越高，最小堆）</param>
    public void Enqueue(TElement element, TPriority priority)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element), "元素不能为null");

        // 如果元素已存在，更新优先级
        if (_elementIndices.ContainsKey(element))
        {
            UpdatePriority(element, priority);
            return;
        }

        // 1. 将元素添加到堆尾
        int index = _heap.Count;
        _heap.Add((element, priority));
        _elementIndices[element] = index;

        // 2. 上浮：将元素移到正确位置
        UpHeap(index);
    }

    /// <summary>
    /// 出队：移除并返回优先级最高的元素
    /// </summary>
    /// <returns>优先级最高的元素</returns>
    /// <exception cref="InvalidOperationException">队列为空时抛出</exception>
    public TElement Dequeue()
    {
        if (IsEmpty)
            throw new InvalidOperationException("优先队列为空，无法出队");

        // 1. 获取堆顶元素（优先级最高）
        var (Element, _) = _heap[0];
        RemoveAt(0);
        return Element;
    }

    /// <summary>
    /// 出队：移除并返回优先级最高的元素+优先级（支持解构赋值）
    /// </summary>
    /// <returns>(元素, 优先级)元组</returns>
    /// <exception cref="InvalidOperationException">队列为空时抛出</exception>
    public (TElement Element, TPriority Priority) DequeueWithPriority()
    {
        if (IsEmpty)
            throw new InvalidOperationException("优先队列为空，无法出队");

        // 1. 获取堆顶元素（优先级最高）
        var top = _heap[0];
        RemoveAt(0);
        return top; // 返回元组(Element, Priority)
    }

    /// <summary>
    /// 安全出队：返回元素+优先级，队列为空时返回false
    /// </summary>
    /// <param name="result">(元素, 优先级)输出元组</param>
    /// <returns>是否出队成功</returns>
    public bool TryDequeueWithPriority(out (TElement Element, TPriority Priority) result)
    {
        if (IsEmpty)
        {
            result = default;
            return false;
        }

        result = DequeueWithPriority();
        return true;
    }

    /// <summary>
    /// 安全出队：队列为空时返回false，否则返回true并输出元素
    /// </summary>
    /// <param name="element">输出的优先级最高元素</param>
    /// <returns>是否出队成功</returns>
    public bool TryDequeue(out TElement element)
    {
        if (IsEmpty)
        {
            element = default;
            return false;
        }

        element = Dequeue();
        return true;
    }

    /// <summary>
    /// 查看队首：返回优先级最高的元素（不移除）
    /// </summary>
    /// <returns>优先级最高的元素</returns>
    /// <exception cref="InvalidOperationException">队列为空时抛出</exception>
    public TElement Peek()
    {
        if (IsEmpty)
            throw new InvalidOperationException("优先队列为空，无法查看队首");

        return _heap[0].Element;
    }

    /// <summary>
    /// 安全查看队首：队列为空时返回false，否则返回true并输出元素
    /// </summary>
    /// <param name="element">输出的优先级最高元素</param>
    /// <returns>是否查看成功</returns>
    public bool TryPeek(out TElement element)
    {
        if (IsEmpty)
        {
            element = default;
            return false;
        }

        element = _heap[0].Element;
        return true;
    }

    /// <summary>
    /// 更新指定元素的优先级
    /// </summary>
    /// <param name="element">待更新的元素</param>
    /// <param name="newPriority">新优先级</param>
    /// <exception cref="KeyNotFoundException">元素不存在时抛出</exception>
    public void UpdatePriority(TElement element, TPriority newPriority)
    {
        if (!_elementIndices.TryGetValue(element, out int index))
            throw new KeyNotFoundException($"优先队列中不存在元素：{element}");

        // 1. 更新优先级
        var oldPriority = _heap[index].Priority;
        _heap[index] = (element, newPriority);

        // 2. 根据新旧优先级决定上浮/下沉
        int compareResult = _comparer.Compare(newPriority, oldPriority);
        if (compareResult < 0)
        {
            UpHeap(index); // 新优先级更高 → 上浮
        }
        else if (compareResult > 0)
        {
            DownHeap(index); // 新优先级更低 → 下沉
        }
    }

    /// <summary>
    /// 移除指定元素
    /// </summary>
    /// <param name="element">待移除的元素</param>
    /// <returns>是否移除成功</returns>
    public bool Remove(TElement element)
    {
        if (!_elementIndices.TryGetValue(element, out int index))
            return false;

        RemoveAt(index);
        return true;
    }

    /// <summary>
    /// 清空队列
    /// </summary>
    public void Clear()
    {
        _heap.Clear();
        _elementIndices.Clear();
    }

    /// <summary>
    /// 判断队列是否包含指定元素
    /// </summary>
    /// <param name="element">待检查的元素</param>
    /// <returns>是否包含</returns>
    public bool Contains(TElement element)
    {
        return _elementIndices.ContainsKey(element);
    }
    #endregion

    #region 堆维护辅助方法
    /// <summary>
    /// 上浮：将指定索引的元素向上移动到正确位置
    /// </summary>
    /// <param name="index">元素索引</param>
    private void UpHeap(int index)
    {
        var (_, Priority) = _heap[index];
        int parentIndex = (index - 1) / 2; // 父节点索引

        // 当当前节点优先级高于父节点时，交换位置
        while (index > 0 && _comparer.Compare(Priority, _heap[parentIndex].Priority) < 0)
        {
            Swap(index, parentIndex);
            index = parentIndex;
            parentIndex = (index - 1) / 2;
        }
    }

    /// <summary>
    /// 下沉：将指定索引的元素向下移动到正确位置
    /// </summary>
    /// <param name="index">元素索引</param>
    private void DownHeap(int index)
    {
        int lastIndex = _heap.Count - 1;
        while (true)
        {
            int leftChildIndex = index * 2 + 1; // 左子节点索引
            int rightChildIndex = index * 2 + 2; // 右子节点索引
            int smallestIndex = index;

            // 找到当前节点、左子、右子中优先级最高的节点
            if (leftChildIndex <= lastIndex && _comparer.Compare(_heap[leftChildIndex].Priority, _heap[smallestIndex].Priority) < 0)
            {
                smallestIndex = leftChildIndex;
            }
            if (rightChildIndex <= lastIndex && _comparer.Compare(_heap[rightChildIndex].Priority, _heap[smallestIndex].Priority) < 0)
            {
                smallestIndex = rightChildIndex;
            }

            // 如果当前节点已是优先级最高，停止下沉
            if (smallestIndex == index)
                break;

            // 否则交换位置，继续下沉
            Swap(index, smallestIndex);
            index = smallestIndex;
        }
    }

    /// <summary>
    /// 交换两个索引的元素，并更新元素索引字典
    /// </summary>
    /// <param name="i">索引1</param>
    /// <param name="j">索引2</param>
    private void Swap(int i, int j)
    {
        (_heap[j], _heap[i]) = (_heap[i], _heap[j]);

        // 更新元素索引字典
        _elementIndices[_heap[i].Element] = i;
        _elementIndices[_heap[j].Element] = j;
    }

    /// <summary>
    /// 移除指定索引的元素
    /// </summary>
    /// <param name="index">元素索引</param>
    private void RemoveAt(int index)
    {
        int lastIndex = _heap.Count - 1;
        _elementIndices.Remove(_heap[index].Element);

        // 如果是最后一个元素，直接移除
        if (index == lastIndex)
        {
            _heap.RemoveAt(lastIndex);
            return;
        }

        // 否则将最后一个元素移到当前位置，然后调整堆
        Swap(index, lastIndex);
        _heap.RemoveAt(lastIndex);

        // 尝试上浮或下沉，恢复堆结构
        UpHeap(index);
        DownHeap(index);
    }
    #endregion

    #region 枚举器（支持foreach遍历）
    public IEnumerator<TElement> GetEnumerator()
    {
        // 遍历的是堆的原始顺序（非优先级排序），如需排序遍历需先复制排序
        foreach (var (Element, _) in _heap)
        {
            yield return Element;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion
}

// 便捷工具类：快速创建最大堆比较器
public static class PriorityQueueHelpers
{
    /// <summary>
    /// 创建最大堆比较器（优先级值越大，出队优先级越高）
    /// </summary>
    /// <typeparam name="TPriority">优先级类型</typeparam>
    /// <returns>最大堆比较器</returns>
    public static IComparer<TPriority> CreateMaxHeapComparer<TPriority>() where TPriority : IComparable<TPriority>
    {
        return Comparer<TPriority>.Create((a, b) => b.CompareTo(a));
    }
}