﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TPoolStatic
{
    public interface IPoolClass
    {
        void OnPoolCreate();
        void OnPoolInitialize();
        void OnPoolRecycle();
    }
    public static class TSPool<T> where T : new()
    {
        private static Stack<T> kPoolItems = new Stack<T>();
        public static T Spawn()
        {
            T item;
            if (kPoolItems.Count > 0)
            {
                item = kPoolItems.Pop();
            }
            else
            {
                item = new T();
                (item as IPoolClass)?.OnPoolCreate();
            }
            (item as IPoolClass)?.OnPoolInitialize();
            return item;
        }

        public static T Spawn(out T _data) => _data = Spawn();
        public static void Recycle(T item)
        {
            (item as IPoolClass)?.OnPoolRecycle();
            kPoolItems.Push(item);
        }
        public static void Dispose()
        {
            kPoolItems.Clear();
            kPoolItems = null;
        }

        public static void Clear() => kPoolItems.Clear();
    }

    public static class TSPool_Extend
    {
        public static void Recycle<T>(this T poolItem) where T : IPoolClass, new()
        {
            TSPool<T>.Recycle(poolItem);
            poolItem.OnPoolRecycle();
        }
    }
    public static class TSPoolObject<T> where T : UnityEngine.Object, new()
    {
        static Stack<T> m_PoolItems = new Stack<T>();
        public static T Spawn()
        {
            T item;
            if (m_PoolItems.Count > 0)
                item = m_PoolItems.Pop();
            else
                item = new T();
            return item;
        }
        public static void Recycle(T item)
        {
            m_PoolItems.Push(item);
        }
        public static void Dispose()
        {
            foreach (var item in m_PoolItems)
                UnityEngine.Object.DestroyImmediate(item);
            m_PoolItems.Clear();
            m_PoolItems = null;
        }
    }
#region Collections

    public static class TSPoolCollection<T> where T:new()
    {
        private static Stack<T> m_PoolItems { get; set; } = new Stack<T>();
        public static T Spawn(Action<T> _clear)
        {
            T collection=m_PoolItems.Count > 0?m_PoolItems.Pop():new T();
            _clear.Invoke(collection);
            return collection;
        }
        public static void Recycle(T item)
        {
            m_PoolItems.Push(item);
        }

        public static void Clear() => m_PoolItems.Clear();
    }

    public static class TSPoolList<T>
    {
        public static List<T> Spawn() => TSPoolCollection<List<T>>.Spawn(_p=>_p.Clear());
        public static void Spawn(out List<T> _list) =>_list=TSPoolCollection<List<T>>.Spawn(_p=>_p.Clear());
        public static void Recycle(List<T> _list) => TSPoolCollection<List<T>>.Recycle(_list);
        public static void Clear()=>TSPoolCollection<List<T>>.Clear();
    }
    public static class TSPoolStack<T> 
    {
        public static Stack<T> Spawn() => TSPoolCollection<Stack<T>>.Spawn(_p=>_p.Clear());
        public static void Spawn(out Stack<T> _list) =>_list=TSPoolCollection<Stack<T>>.Spawn(_p=>_p.Clear());
        public static void Recycle(Stack<T> _stack) => TSPoolCollection<Stack<T>>.Recycle(_stack);
    }
    public static class TSPoolQueue<T> 
    {
        public static Queue<T> Spawn() => TSPoolCollection<Queue<T>>.Spawn(_p=>_p.Clear());
        public static void Spawn(out Queue<T> _list) =>_list=TSPoolCollection<Queue<T>>.Spawn(_p=>_p.Clear());
        public static void Recycle(Queue<T> _queue) => TSPoolCollection<Queue<T>>.Recycle(_queue);
    }
    public static class TSPoolLinkedList<T> 
    {
        public static LinkedList<T> Spawn() => TSPoolCollection<LinkedList<T>>.Spawn(_p=>_p.Clear());
        public static void Spawn(out LinkedList<T> _list) =>_list=TSPoolCollection<LinkedList<T>>.Spawn(_p=>_p.Clear());
        public static void Recycle(LinkedList<T> _queue) => TSPoolCollection<LinkedList<T>>.Recycle(_queue);
    }
    public static class TSPoolHashset<T> 
    {
        public static HashSet<T> Spawn() => TSPoolCollection<HashSet<T>>.Spawn(_p=>_p.Clear());
        public static void Spawn(out HashSet<T> _list) =>_list=TSPoolCollection<HashSet<T>>.Spawn(_p=>_p.Clear());
        public static void Recycle(HashSet<T> _queue) => TSPoolCollection<HashSet<T>>.Recycle(_queue);
    }
#endregion
}
namespace TPool
{
    #region Reference 
    public class TPool<T> where T:class ,new()
    {
        public Stack<T> m_Pooled { get; private set; } = new Stack<T>();
        public Stack<T> m_Activated { get; private set; } = new Stack<T>();
        public T Pop()
        {
            if (m_Pooled.Count != 0)
                return m_Pooled.Pop();

            T newElement = new T();
            m_Activated.Push(newElement);
            return new T();
        }
        public void Push(T _poolElement)
        {
            if (!m_Activated.Contains(_poolElement))
                throw new Exception("None Pooled Item Found!"+_poolElement);
            m_Pooled.Push(_poolElement);
        }

        public void Clear()
        {
            foreach (T _activate in m_Activated)
                m_Pooled.Push(_activate);
            m_Activated.Clear();
        }

        public void Dispose()
        {
            Clear();
            m_Pooled = null;
            m_Activated = null;
            GC.SuppressFinalize(this);
        }
    }
    #endregion
    #region Object

    public static class ObjectPoolHelper
    {
        public static T Spawn<T>(this AObjectPool<int, T> _pool)=> _pool.Spawn(_pool.m_SpawnTimes);
        public static Y TrySpawn<T, Y>(this AObjectPool<T, Y> _pool, T _identity)=> _pool.Contains(_identity) ? _pool.Get(_identity) : _pool.Spawn(_identity);
    }
    public interface IPoolCallback<T>
    {
        void OnPoolCreate(Action<T> _doRecycle);
        void OnPoolSpawn(T _identity);
        void OnPoolRecycle();
        void OnPoolDispose();
    }
    
    public abstract class AObjectPool<T,Y>:IEnumerable<Y>
    {
        public readonly Dictionary<T, Y> m_Dic = new Dictionary<T, Y>();
        readonly Stack<Y> m_PooledItems = new Stack<Y>();
        public readonly Transform transform;
        private readonly GameObject m_PoolItem;
        public int Count => m_Dic.Count;
        public int m_SpawnTimes { get; private set; } = 0;

        protected AObjectPool(GameObject _pooledItem,Transform _transform)
        {
            transform = _transform;
            m_PoolItem = _pooledItem;
            m_PoolItem.gameObject.SetActive(false);
            m_SpawnTimes = 0;
        }
        public bool Contains(T identity) => m_Dic.ContainsKey(identity);
        public Y Get(T identity) => m_Dic[identity];
        public Y this[T identity] => m_Dic[identity];
        public Y Spawn(T identity)
        {
            Y targetItem;
            if (m_PooledItems.Count > 0)
                targetItem = m_PooledItems.Pop();
            else
            {
                targetItem = CreateNewItem(UnityEngine.Object.Instantiate(m_PoolItem, transform).transform);
                if(targetItem is IPoolCallback<T> iPoolInit)
                    iPoolInit.OnPoolCreate(DoRecycle);
            }
            if (m_Dic.ContainsKey(identity)){
                Debug.LogError(identity + "Already Exists In Grid Dic");
            }
            else
                m_Dic.Add(identity, targetItem);
            Transform trans = GetItemTransform(targetItem);
            trans.SetAsLastSibling();
            trans.name = identity.ToString();
            trans.SetActive(true);
            
            if(targetItem is IPoolCallback<T> iPoolSpawn)
                iPoolSpawn.OnPoolSpawn(identity);
            m_SpawnTimes++;
            return targetItem;
        }

        private void DoRecycle(T identity)
        {
            RecycleItem(m_Dic[identity]);
            m_Dic.Remove(identity);
        }

        void RecycleItem(Y item)
        {
            m_PooledItems.Push(item);
            Transform itemTransform = GetItemTransform(item);
            itemTransform.SetActive(false);
            itemTransform.SetParent(transform);
            if (item is IPoolCallback<T> iPoolItem)
                iPoolItem.OnPoolRecycle();
        }

        public Y Recycle(T identity)
        {
            Y item = m_Dic[identity];
            DoRecycle(identity);
            return item;
        }

        public bool TryGet(T _identity,out Y _element)
        {
            _element = default;
            if (!Contains(_identity))
                return false;
            _element = this[_identity];
            return true;
        }
        
        public bool TryRecycle(T _identity)
        {
            if (!Contains(_identity))
                return false;

            Recycle(_identity);
            return true;
        }

        public void Sort(Comparison<KeyValuePair<T,Y>> Compare)
        {
            List<KeyValuePair<T, Y>> list = m_Dic.ToList();
            list.Sort(Compare);
            m_Dic.Clear();
            foreach (var pair in list)
            {
                GetItemTransform(pair.Value).SetAsLastSibling();
                m_Dic.Add(pair.Key,pair.Value);
            }
        }

        public void Clear()
        {
            m_SpawnTimes = 0;
            foreach (var item in m_Dic.Values)
                RecycleItem(item);
            m_Dic.Clear();
        }

        public void Dispose()
        {
            Clear();
            foreach (var item in m_PooledItems)
            {
                if (item is IPoolCallback<T> iPoolItem)
                    iPoolItem.OnPoolDispose();
                UnityEngine.Object.Destroy(GetItemTransform(item).gameObject);
            }
        }

        protected virtual Y CreateNewItem(Transform _instantiateTrans)
        {
            Debug.LogError("Override This Please");
            return default(Y);
        }
        protected virtual Transform GetItemTransform(Y _targetItem)
        {
            Debug.LogError("Override This Please");
            return null;
        }

        public IEnumerator<Y> GetEnumerator()
        {
            foreach (var value in m_Dic.Values)
                yield return value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    public class ObjectPoolComponent<T> : AObjectPool<int,T> where T : Component
    {
        public ObjectPoolComponent(Transform poolItem) : base(poolItem.gameObject,poolItem.transform.parent) {  }
        
        protected override T CreateNewItem(Transform _instantiateTrans)=>_instantiateTrans.GetComponent<T>();
        protected override Transform GetItemTransform(T _targetItem) => _targetItem.transform;
    }
    public class ObjectPoolTransform : AObjectPool<int,Transform>
    {
        public ObjectPoolTransform(Transform _poolItem) : base(_poolItem.gameObject,_poolItem.transform.parent) {  }

        protected override Transform CreateNewItem(Transform _instantiateTrans)=> _instantiateTrans;
        protected override Transform GetItemTransform(Transform _targetItem) => _targetItem;
    } 
    public class ObjectPoolGameObject : AObjectPool<int,GameObject>
    {
        public ObjectPoolGameObject(Transform _poolItem) : base(_poolItem.gameObject,_poolItem.transform.parent) {  }

        protected override GameObject CreateNewItem(Transform _instantiateTrans)=> _instantiateTrans.gameObject;
        protected override Transform GetItemTransform(GameObject _targetItem) => _targetItem.transform;
    } 
    #endregion
    #region Implement
    public abstract class APoolTransform<T>:ITransformHandle,IPoolCallback<T>
    {
        public Transform Transform { get; }
        private Action<T> DoRecycle { get; set; }
        public T m_Identity { get; private set; }
        public APoolTransform(Transform _transform)
        {
            Transform = _transform;
        }
        public virtual void OnPoolCreate(Action<T> _doRecycle)=>this.DoRecycle = _doRecycle;
        public virtual void OnPoolSpawn(T _identity)=> m_Identity = _identity;
        public virtual void OnPoolRecycle()=>m_Identity = default;
        public virtual void OnPoolDispose() {}

        public void Recycle()=>DoRecycle(m_Identity);
    }
    

    public class ObjectPoolClass<T,Y> : AObjectPool<T,Y> where Y :ITransformHandle
    {
        private readonly Type m_Type;
        private readonly Func<object[]> CostructParameters;
        public ObjectPoolClass(Transform _pooledElement, Type type) : base(_pooledElement.gameObject,_pooledElement.parent) { m_Type = type; }

        public ObjectPoolClass(Transform _pooledElement, Func<object[]> _constructParameters = null) : this(_pooledElement,
            typeof(Y))
        {
            CostructParameters = _constructParameters;
        }
        protected override Y CreateNewItem(Transform _instantiateTrans)
        {
            if(CostructParameters!=null)
                return UReflection.CreateInstance<Y>(m_Type,  new object[]{ _instantiateTrans }.Add(CostructParameters.Invoke()));
            return UReflection.CreateInstance<Y>(m_Type, _instantiateTrans);
        }

        protected override Transform GetItemTransform(Y _targetItem) => _targetItem.Transform;
    }
    
    
    public abstract class PoolBehaviour<T> : MonoBehaviour,IPoolCallback<T>
    {
        protected bool m_Recycled { get; private set; }
        protected T m_PoolID { get; private set; }
        private Action<T> DoRecycle { get; set; }

        public virtual void OnPoolCreate(Action<T> _doRecycle)
        {
            this.DoRecycle = _doRecycle;
            m_Recycled = true;
        }

        public virtual void OnPoolSpawn(T _identity)
        {
            m_PoolID = _identity;
            m_Recycled = false;
        }

        public virtual void OnPoolRecycle()
        {
            m_Recycled = true;
        }

        public virtual void OnPoolDispose()
        {
            m_Recycled = true;
            m_PoolID = default;
            DoRecycle = null;
        }

        public void Recycle()=>DoRecycle(m_PoolID);
    }
    public class ObjectPoolMono<T,Y> : AObjectPool<T,Y> where Y : PoolBehaviour<T>
    {
        public ObjectPoolMono(Transform _poolTrans) : base(_poolTrans.gameObject,_poolTrans.parent) {  }
        protected override Y CreateNewItem(Transform _instantiateTrans)=>_instantiateTrans.GetComponent<Y>();
        protected override Transform GetItemTransform(Y _targetItem) => _targetItem.transform;
    }
    
    #endregion
}